using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace PdxModIDE.ModelEngine;

/// <summary>
/// Reconstructs the CK3 "portrait_attachment_pattern" garment color deterministically.
/// The real shader is not shipped to users, so this reproduces the documented algorithm:
///   - decode the base diffuse (gray cloth texture)
///   - decode the entity pattern_mask (RGBA) -> which pattern slots are active
///   - for each active channel, sample the variation colormask
///   - use the colormask brightness to index a column of the 16-wide colour palette
///   - multiply the base diffuse by the weighted tint
/// Output is BGRA so it can be used directly as a texture source.
/// </summary>
public static class PdxClothingPainter
{
    private sealed class Outline
    {
        public string Diffuse = "";
        public string Mask = "";
        public string Variation = "";
        public string MeshPath = "";
    }

    public static DdsImage? Paint(string gameRoot, string assetPath, string? meshPath = null)
    {
        if (string.IsNullOrEmpty(gameRoot) || !Directory.Exists(gameRoot) || string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath))
            return null;

        Outline o;
        try { o = ReadOutline(gameRoot, assetPath, meshPath); }
        catch { return null; }
        if (string.IsNullOrEmpty(o.Diffuse) || !File.Exists(o.Diffuse)) return null;

        DdsImage baseImg;
        try { baseImg = DdsDecoder.Decode(o.Diffuse); }
        catch { return null; }
        if (baseImg.Data == null || baseImg.Width <= 0 || baseImg.Height <= 0) return null;

        DdsImage? maskImg = null;
        if (!string.IsNullOrEmpty(o.Mask) && File.Exists(o.Mask))
        {
            try { maskImg = DdsDecoder.Decode(o.Mask); } catch { maskImg = null; }
        }

        // First palette (row 0 = the base hue row).
        DdsImage? paletteImg = LoadPalette(gameRoot, o.Variation);
        if (paletteImg == null) return null;

        var colormasks = LoadColormasks(gameRoot, o.Variation);
        var layouts = LoadLayouts(gameRoot, o.Variation);

        int pw = baseImg.Width, ph = baseImg.Height;
        byte[] outData = new byte[pw * ph * 4];
        var baseD = baseImg.Data;
        int mw = maskImg?.Width ?? 1, mh = maskImg?.Height ?? 1;
        var mxD = maskImg?.Data;
        int palW = paletteImg.Width, palH = paletteImg.Height;
        var palD = paletteImg.Data;

        // Row 0 of the palette is the deterministically chosen hue family.
        int row = 0;

        // Build the mesh UV-set 2 (uv1) per output pixel by rasterizing the mesh
        // triangles in UV0 space and interpolating uv1 for each covered texel.
        float[]? uv1Map = null;
        if (!string.IsNullOrEmpty(o.MeshPath) && File.Exists(o.MeshPath))
            uv1Map = BuildXyzMapping(o.MeshPath, pw, ph);

        // Per-channel colormask textures (R,G,B,A) + their sizes.
        var cmw = new int[4]; var cmh = new int[4];
        byte[]?[] cmxDs = new byte[4][];
        for (int c = 0; c < 4; c++)
        {
            if (colormasks[c] != null)
            {
                cmw[c] = colormasks[c]!.Width; cmh[c] = colormasks[c]!.Height;
                cmxDs[c] = colormasks[c]!.Data;
            }
            else { cmw[c] = 1; cmh[c] = 1; cmxDs[c] = null; }
        }

        for (int y = 0; y < ph; y++)
        {
            float v = (y + 0.5f) / ph;
            for (int x = 0; x < pw; x++)
            {
                float u = (x + 0.5f) / pw;
                int idx = (y * pw + x) * 4;

                float bd = baseD[idx] / 255f, gd = baseD[idx + 1] / 255f, rd = baseD[idx + 2] / 255f;

                // Patterns are sampled using the mesh UV-set 2 (uv1) when present,
                // otherwise fall back to the diffuse UV0.
                float su = u, sv = v;
                if (uv1Map != null)
                {
                    int pbase = (y * pw + x) * 2;
                    su = uv1Map[pbase];
                    sv = uv1Map[pbase + 1];
                }

                // Active mask channels. Decoded data is BGRA (byte0=B, byte1=G, byte2=R, byte3=A),
                // so remap to the logical R,G,B,A channels the variation refers to.
                // maskCh[0]=R, [1]=G, [2]=B, [3]=A.
                float[] maskCh = { 0f, 0f, 0f, 0f };
                if (mxD != null)
                {
                    int mi = Math.Max(0, Math.Min(mw - 1, (int)(su * (mw - 1))));
                    int mj = Math.Max(0, Math.Min(mh - 1, (int)(sv * (mh - 1))));
                    int mindex = (mj * mw + mi) * 4;
                    maskCh[0] = mxD[mindex + 2] / 255f; // R
                    maskCh[1] = mxD[mindex + 1] / 255f; // G
                    maskCh[2] = mxD[mindex] / 255f;     // B
                    maskCh[3] = mxD[mindex + 3] / 255f; // A
                }

                float presence = maskCh[0] + maskCh[1] + maskCh[2] + maskCh[3];
                float tr = 0, tg = 0, tb = 0, weight = 0;

                if (presence > 0)
                {
                    // Weighted tint: each active mask channel samples ITS OWN colormask.
                    // The 16-wide palette is split into 4 segments (4 cols each);
                    // channel c (R,G,B,A) owns segment [c*4 .. c*4+3].
                    int colsPerChannel = palW / 4;
                    if (colsPerChannel < 1) colsPerChannel = 1;
                    for (int c = 0; c < 4; c++)
                    {
                        float chw = maskCh[c];
                        if (chw <= 0.01f) continue;
                        var cxD = cmxDs[c];
                        if (cxD == null) continue;
                        int cw = cmw[c], ch = cmh[c];

                        // The colormask UV is the pattern UV transformed by the channel's layout
                        // (scale, rotation, offset). If no layout, sample directly.
                        float cu = su, cv = sv;
                        if (layouts[c] != null)
                            cu = ApplyLayout(cu, cv, layouts[c]!.Value, out cv);
                        cu = cu - (float)Math.Floor(cu);
                        cv = cv - (float)Math.Floor(cv);
                        int cmx = Math.Max(0, Math.Min(cw - 1, (int)(cu * (cw - 1))));
                        int cmj = Math.Max(0, Math.Min(ch - 1, (int)(cv * (ch - 1))));
                        int cindex = (cmj * cw + cmx) * 4;
                        float cmVal = (cxD![cindex] + cxD[cindex + 1] + cxD[cindex + 2]) / (3f * 255f);
                        int segBase = c * colsPerChannel;
                        int colidx = segBase + (int)(cmVal * (colsPerChannel - 1 + 0.999f));
                        if (colidx >= palW) colidx = palW - 1;
                        if (colidx < 0) colidx = 0;
                        // palette decoded BGRA: byte0=B, byte1=G, byte2=R.
                        float wB = palD[(row * palW + colidx) * 4] / 255f;
                        float wG = palD[(row * palW + colidx) * 4 + 1] / 255f;
                        float wR = palD[(row * palW + colidx) * 4 + 2] / 255f;
                        tr += chw * wR; tg += chw * wG; tb += chw * wB; weight += chw;
                    }
                    if (weight > 0) { tr /= weight; tg /= weight; tb /= weight; }
                }

                float fr = tr, fg = tg, fb = tb;
                if (weight <= 0) { fr = 1; fg = 1; fb = 1; }

                // Output BGRA (byte0=B, byte1=G, byte2=R).
                outData[idx] = Clamp((byte)(bd * fb * 255));
                outData[idx + 1] = Clamp((byte)(gd * fg * 255));
                outData[idx + 2] = Clamp((byte)(rd * fr * 255));
                outData[idx + 3] = baseD[idx + 3];
            }
        }

        return new DdsImage { Width = pw, Height = ph, Data = outData, HasAlpha = true };
    }

    // ---------- helpers ----------

    private static byte Clamp(int v) => (byte)Math.Max(0, Math.Min(255, v));

    private static Outline ReadOutline(string gameRoot, string assetPath, string? meshPath = null)
    {
        string content;
        try { content = File.ReadAllText(assetPath); }
        catch { throw; }
        string dir = Path.GetDirectoryName(assetPath) ?? "";
        return new Outline
        {
            Diffuse = ResolveTexture(gameRoot, dir, content, "texture_diffuse"),
            Mask = ResolveTexture(gameRoot, dir, content, "pattern_mask"),
            Variation = ReadQuoted(content, "variation"),
            MeshPath = meshPath ?? ""
        };
    }

    private static string ResolveTexture(string gameRoot, string dir, string content, string key)
    {
        string rel = ReadQuoted(content, key);
        if (string.IsNullOrEmpty(rel)) return "";
        string norm = rel.Replace('/', '\\');
        string abs;
        if (Path.IsPathRooted(rel))
            abs = rel;
        else if (norm.StartsWith("gfx\\", StringComparison.OrdinalIgnoreCase) ||
                 norm.StartsWith("game\\", StringComparison.OrdinalIgnoreCase))
            abs = Path.Combine(gameRoot, norm);
        else
            abs = Path.Combine(dir, norm);
        return File.Exists(abs) ? abs : "";
    }

    private static string ReadQuoted(string content, string key)
    {
        int i = content.IndexOf(key, StringComparison.Ordinal);
        if (i < 0) return "";
        int q1 = content.IndexOf('"', i);
        if (q1 < 0) return "";
        int q2 = content.IndexOf('"', q1 + 1);
        if (q2 < 0) return "";
        return content.Substring(q1 + 1, q2 - q1 - 1).Trim();
    }

    private static DdsImage? LoadPalette(string gameRoot, string variationName)
    {
        string? path = FindVariationTexture(gameRoot, variationName, "color_palette");
        if (path == null || !File.Exists(path)) return null;
        try { return DdsDecoder.Decode(path); }
        catch { return null; }
    }

    private static DdsImage?[] LoadColormasks(string gameRoot, string variationName)
    {
        var result = new DdsImage?[4];
        if (string.IsNullOrEmpty(variationName)) return result;
        string dir = Path.Combine(gameRoot, "gfx", "portraits", "accessory_variations");
        if (!Directory.Exists(dir)) return result;

        foreach (var file in Directory.EnumerateFiles(dir, "*.txt", SearchOption.TopDirectoryOnly))
        {
            string content;
            try { content = File.ReadAllText(file); } catch { continue; }
            int ni = content.IndexOf($"name = \"{variationName}\"", StringComparison.Ordinal);
            if (ni < 0) continue;
            int end = content.IndexOf("variation = {", ni, StringComparison.Ordinal);
            if (end < 0) end = content.Length;

            int pos = ni;
            char[] channels = { 'r', 'g', 'b', 'a' };
            while (pos < end)
            {
                int pi = content.IndexOf("pattern = {", pos, StringComparison.Ordinal);
                if (pi < 0 || pi >= end) break;
                int pj = FindClose(content, pi);
                if (pj < 0 || pj > end) break;
                string block = content.Substring(pi, pj - pi);
                // Use the first pattern's per-channel colormask names.
                for (int c = 0; c < 4; c++)
                {
                    if (result[c] != null) continue;
                    string needle = channels[c] + " = {";
                    int rIdx = block.IndexOf(needle, StringComparison.Ordinal);
                    string tex = rIdx >= 0 ? ReadInside(block, rIdx, "textures") : "";
                    if (!string.IsNullOrEmpty(tex)) result[c] = LoadColormaskFile(gameRoot, tex);
                }
                pos = pj;
            }
            return result;
        }
        return result;
    }

    private static string? FindVariationTexture(string gameRoot, string variationName, string kind)
    {
        string dir = Path.Combine(gameRoot, "gfx", "portraits", "accessory_variations");
        if (!Directory.Exists(dir)) return null;

        foreach (var file in Directory.EnumerateFiles(dir, "*.txt", SearchOption.TopDirectoryOnly))
        {
            string content;
            try { content = File.ReadAllText(file); } catch { continue; }
            int ni = content.IndexOf($"name = \"{variationName}\"", StringComparison.Ordinal);
            if (ni < 0) continue;
            int end = content.IndexOf("variation = {", ni, StringComparison.Ordinal);
            if (end < 0) end = content.Length;

            int pi = content.IndexOf($"{kind} = {{", ni, StringComparison.Ordinal);
            if (pi < 0 || pi > end) continue;
            string tex = ReadInside(content, pi, "texture");
            if (!string.IsNullOrEmpty(tex))
            {
                string abs = Path.IsPathRooted(tex) ? tex : Path.Combine(gameRoot, tex.Replace('/', '\\'));
                if (File.Exists(abs)) return abs;
            }
        }
        return null;
    }

    private static DdsImage? LoadColormaskFile(string gameRoot, string baseName)
    {
        foreach (var style in new[] { "western", "all", "catholic", "mena", "indian", "northern",
                                       "byzantine", "sub_saharan", "steppe", "fur", "afr", "crusaders" })
        {
            string p = Path.Combine(gameRoot, "gfx", "portraits", "accessory_variations",
                "textures", "patterns", style, baseName + "_masks.dds");
            if (File.Exists(p)) { try { return DdsDecoder.Decode(p); } catch { return null; } }
        }
        return null;
    }

    private static string ReadInside(string textOrContent, int fromIndex, string key)
    {
        int i = textOrContent.IndexOf(key, fromIndex, StringComparison.Ordinal);
        if (i < 0) return "";
        int q1 = textOrContent.IndexOf('"', i);
        if (q1 < 0) return "";
        int q2 = textOrContent.IndexOf('"', q1 + 1);
        if (q2 < 0) return "";
        return textOrContent.Substring(q1 + 1, q2 - q1 - 1).Trim();
    }

    private static int FindClose(string content, int openIdx)
    {
        int depth = 0;
        for (int i = openIdx; i < content.Length; i++)
        {
            char c = content[i];
            if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    private struct LayoutParams
    {
        public float ScaleX, ScaleY, Rotation, OffsetX, OffsetY;
    }

    /// <summary>
    /// Parses the variation's pattern_layout blocks and returns, per channel (R,G,B,A),
    /// the layout used for sampling that channel's colormask.
    /// </summary>
    private static LayoutParams?[] LoadLayouts(string gameRoot, string variationName)
    {
        var result = new LayoutParams?[4];
        if (string.IsNullOrEmpty(variationName)) return result;
        string dir = Path.Combine(gameRoot, "gfx", "portraits", "accessory_variations");
        if (!Directory.Exists(dir)) return result;

        // Layout name referenced by each channel of the first matching pattern block.
        char[] channels = { 'r', 'g', 'b', 'a' };
        foreach (var file in Directory.EnumerateFiles(dir, "*.txt", SearchOption.TopDirectoryOnly))
        {
            string content;
            try { content = File.ReadAllText(file); } catch { continue; }
            int ni = content.IndexOf($"name = \"{variationName}\"", StringComparison.Ordinal);
            if (ni < 0) continue;
            int end = content.IndexOf("variation = {", ni, StringComparison.Ordinal);
            if (end < 0) end = content.Length;

            var layoutNames = new string[4];
            int pi = content.IndexOf("pattern = {", ni, StringComparison.Ordinal);
            if (pi >= 0 && pi <= end)
            {
                int pj = FindClose(content, pi);
                if (pj >= 0 && pj <= end)
                {
                    string block = content.Substring(pi, pj - pi);
                    for (int c = 0; c < 4; c++)
                    {
                        string needle = channels[c] + " = {";
                        int rIdx = block.IndexOf(needle, StringComparison.Ordinal);
                        if (rIdx >= 0) layoutNames[c] = ReadInside(block, rIdx, "layout");
                    }
                }
            }

            // Collect all pattern_layout blocks in this file: name -> params.
            var defs = new Dictionary<string, LayoutParams>(StringComparer.Ordinal);
            int pos = 0;
            while (true)
            {
                int li = content.IndexOf("pattern_layout = {", pos, StringComparison.Ordinal);
                if (li < 0) break;
                int lj = FindClose(content, li);
                if (lj < 0) break;
                string lb = content.Substring(li, lj - li);
                string lname = ReadInside(lb, 0, "name");
                if (!string.IsNullOrEmpty(lname) && !defs.ContainsKey(lname))
                    defs[lname] = ParseLayoutBlock(lb);
                pos = lj + 1;
            }

            for (int c = 0; c < 4; c++)
            {
                string ln = layoutNames[c];
                if (!string.IsNullOrEmpty(ln) && defs.TryGetValue(ln, out var lp))
                    result[c] = lp;
            }
            return result;
        }
        return result;
    }

    private static LayoutParams ParseLayoutBlock(string block)
    {
        var lp = new LayoutParams
        {
            ScaleX = ReadNumberAfterOf(block, "scale"),
            ScaleY = ReadNumberAfterOf(block, "scale"),
            Rotation = ReadNumberAfterOf(block, "rotation"),
        };
        int oi = block.IndexOf("offset = {", StringComparison.Ordinal);
        if (oi >= 0)
        {
            int xi = block.IndexOf("x = {", oi, StringComparison.Ordinal);
            if (xi >= 0) lp.OffsetX = ReadNumberAfterOf(block.Substring(xi), "min");
            int yi = block.IndexOf("y = {", oi, StringComparison.Ordinal);
            if (yi >= 0) lp.OffsetY = ReadNumberAfterOf(block.Substring(yi), "min");
        }
        return lp;
    }

    // Reads the first number following "key = " in the given text (deterministic min value).
    private static float ReadNumberAfterOf(string text, string key)
    {
        int i = text.IndexOf(key, StringComparison.Ordinal);
        if (i < 0) return 0;
        int eq = text.IndexOf('=', i);
        if (eq < 0) return 0;
        var sb = new StringBuilder();
        int p = eq + 1;
        while (p < text.Length)
        {
            char ch = text[p];
            if (char.IsDigit(ch) || ch == '.' || ch == '-')
                sb.Append(ch);
            else if (sb.Length > 0) break;
            p++;
        }
        if (sb.Length == 0) return 0;
        return float.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var val) ? val : 0;
    }

    // Transforms a pattern UV with the given layout and returns the new U while setting outV.
    private static float ApplyLayout(float u, float v, LayoutParams lp, out float outV)
    {
        // Rotation about the texture origin, then scale, then offset.
        float rad = lp.Rotation * (float)Math.PI / 180f;
        float cos = (float)Math.Cos(rad), sin = (float)Math.Sin(rad);
        float ru = u * cos - v * sin;
        float rv = u * sin + v * cos;
        outV = rv * (lp.ScaleY <= 0 ? 1 : lp.ScaleY) + lp.OffsetY;
        return ru * (lp.ScaleX <= 0 ? 1 : lp.ScaleX) + lp.OffsetX;
    }

    /// <summary>
    /// Rasterizes the mesh triangles in UV0 space and stores, per diffuse texel,
    /// the interpolated UV-set-2 (uv1) coordinates used to sample patterns.
    /// Returns null when the mesh has no usable uv1 set.
    /// </summary>
    private static float[]? BuildXyzMapping(string meshPath, int pw, int ph)
    {
        if (pw <= 0 || ph <= 0) return null;
        PdxModel? model;
        try { model = PdxMeshParser.ParseMeshFile(meshPath); }
        catch { return null; }
        if (model == null || model.Meshes.Count == 0) return null;

        var map = new float[pw * ph * 2];
        var covered = new bool[pw * ph];
        bool any = false;

        foreach (var mesh in model.Meshes)
        {
            if (mesh.UVSets.Count < 2) continue;
            var uv0 = mesh.UVSets[0];
            var uv1 = mesh.UVSets[1];
            if (uv0 == null || uv1 == null || mesh.Triangles == null) continue;
            int verts = uv0.Length / 2;
            if (verts < 3) continue;

            for (int t = 0; t + 2 < mesh.Triangles.Length; t += 3)
            {
                int i0 = mesh.Triangles[t], i1 = mesh.Triangles[t + 1], i2 = mesh.Triangles[t + 2];
                if (i0 < 0 || i1 < 0 || i2 < 0 || i0 >= verts || i1 >= verts || i2 >= verts) continue;

                float ax = uv0[i0 * 2], ay = uv0[i0 * 2 + 1];
                float bx = uv0[i1 * 2], by = uv0[i1 * 2 + 1];
                float cx = uv0[i2 * 2], cy = uv0[i2 * 2 + 1];

                float a1 = uv1[i0 * 2], a2 = uv1[i0 * 2 + 1];
                float b1 = uv1[i1 * 2], b2 = uv1[i1 * 2 + 1];
                float c1 = uv1[i2 * 2], c2 = uv1[i2 * 2 + 1];

                float minU = Math.Min(ax, Math.Min(bx, cx)), maxU = Math.Max(ax, Math.Max(bx, cx));
                float minV = Math.Min(ay, Math.Min(by, cy)), maxV = Math.Max(ay, Math.Max(by, cy));

                int x0 = Math.Max(0, (int)Math.Floor(minU * pw)), x1 = Math.Min(pw - 1, (int)Math.Ceiling(maxU * pw));
                int y0 = Math.Max(0, (int)Math.Floor(minV * ph)), y1 = Math.Min(ph - 1, (int)Math.Ceiling(maxV * ph));

                float det = (bx - ax) * (cy - ay) - (cx - ax) * (by - ay);
                if (Math.Abs(det) < 1e-9f) continue;

                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        int pix = y * pw + x;
                        if (covered[pix]) continue;
                        float pu = (x + 0.5f) / pw, pv = (y + 0.5f) / ph;
                        float w0 = ((bx - pu) * (cy - pv) - (cx - pu) * (by - pv)) / det;
                        float w1 = ((cx - pu) * (ay - pv) - (ax - pu) * (cy - pv)) / det;
                        float w2 = 1f - w0 - w1;
                        if (w0 < -0.01f || w1 < -0.01f || w2 < -0.01f) continue;
                        map[pix * 2] = w0 * a1 + w1 * b1 + w2 * c1;
                        map[pix * 2 + 1] = w0 * a2 + w1 * b2 + w2 * c2;
                        covered[pix] = true;
                        any = true;
                    }
                }
            }
        }
        return any ? map : null;
    }
}