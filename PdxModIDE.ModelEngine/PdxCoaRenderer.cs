using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PdxModIDE.ModelEngine;

public static class PdxCoaRenderer
{
    private static bool IsPureRef(CoaNode n) =>
        n.Ref != null && string.IsNullOrEmpty(n.Pattern) && !n.HasC1 && !n.HasC2 && !n.HasC3 &&
        n.Emblems.Count == 0 && n.Subs.Count == 0 && n.Instances.Count == 0;

    private static CoaNode ResolveConcrete(Dictionary<string, CoaNode> defs, string name, HashSet<string> stack)
    {
        var empty = new CoaNode();
        if (stack.Contains(name)) return empty;
        if (!defs.TryGetValue(name, out var node)) return empty;

        var guard = new HashSet<string>(stack) { name };

        if (IsPureRef(node) && node.Ref != null)
            return ResolveConcrete(defs, node.Ref, guard);

        var result = Clone(node);
        if (node.Ref != null)
        {
            var parent = ResolveConcrete(defs, node.Ref, guard);
            if (!result.HasC1) { result.HasC1 = parent.HasC1; result.C1 = parent.C1; }
            if (!result.HasC2) { result.HasC2 = parent.HasC2; result.C2 = parent.C2; }
            if (!result.HasC3) { result.HasC3 = parent.HasC3; result.C3 = parent.C3; }
            if (string.IsNullOrEmpty(result.Pattern)) result.Pattern = parent.Pattern;
            if (result.Emblems.Count == 0) result.Emblems = parent.Emblems;
            if (result.Subs.Count == 0) result.Subs = parent.Subs;
        }
        return result;
    }

    private static CoaNode Clone(CoaNode n) => new()
    {
        Ref = n.Ref,
        Pattern = n.Pattern,
        HasC1 = n.HasC1, C1 = n.C1,
        HasC2 = n.HasC2, C2 = n.C2,
        HasC3 = n.HasC3, C3 = n.C3,
        Emblems = n.Emblems,
        Subs = n.Subs,
        Instances = n.Instances,
    };

    /// <summary>Resolves a top-level CoA name into a draw list (draw order preserved).</summary>
    public static List<PdxCoaLayer> Resolve(Dictionary<string, CoaNode> defs, string name)
    {
        var layers = new List<PdxCoaLayer>();
        var node = ResolveConcrete(defs, name, new HashSet<string>());
        Flatten(node, new Tv(1, 1, 0, 0), layers);

        var ordered = layers
            .Select((l, i) => (l, i))
            .OrderBy(t => t.l.Instance == null ? 0 : t.l.Instance.Depth)
            .ThenBy(t => t.i)
            .Select(t => t.l)
            .ToList();
        return ordered;
    }

    private readonly record struct Tv(double Sx, double Sy, double Ox, double Oy);

    private static CoaInstance Box(Tv tv, double posX, double posY, double sX, double sY, double rot) => new()
    {
        X = tv.Ox + posX * tv.Sx,
        Y = tv.Oy + posY * tv.Sy,
        Sx = sX * tv.Sx,
        Sy = sY * tv.Sy,
        Rotation = rot,
    };

    private static void Flatten(CoaNode node, Tv tv, List<PdxCoaLayer> layers)
    {
        uint c1 = node.HasC1 ? node.C1 : 0xFF000000;
        uint c2 = node.HasC2 ? node.C2 : c1;
        uint c3 = node.HasC3 ? node.C3 : c2;

        if (!string.IsNullOrEmpty(node.Pattern))
            layers.Add(new PdxCoaLayer
            {
                PatternTexture = node.Pattern,
                HasColor = true, C1 = c1, C2 = c2, C3 = c3,
                Instance = Box(tv, 0.5, 0.5, 1, 1, 0),
            });

        foreach (var em in node.Emblems)
        {
            uint ec1 = em.HasC1 ? em.C1 : c1;
            uint ec2 = em.HasC2 ? em.C2 : c2;
            uint ec3 = em.HasC3 ? em.C3 : (node.HasC3 ? c3 : ec2);
            foreach (var inst in em.Instances)
                layers.Add(new PdxCoaLayer
                {
                    EmblemTexture = em.Texture,
                    Textured = em.Textured,
                    HasColor = true, C1 = ec1, C2 = ec2, C3 = ec3,
                    Instance = Box(tv, inst.X, inst.Y, inst.Sx, inst.Sy, inst.Rotation),
                });
        }

        foreach (var sub in node.Subs)
        {
            var child = new Tv(
                tv.Sx * (sub.Transform.Sx == 0 ? 1 : sub.Transform.Sx),
                tv.Sy * (sub.Transform.Sy == 0 ? 1 : sub.Transform.Sy),
                tv.Ox + sub.Transform.X * tv.Sx,
                tv.Oy + sub.Transform.Y * tv.Sy);
            Flatten(sub.Node, child, layers);
        }
    }

    private static string? FindTexture(string gameRoot, string folder, string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        string baseName = Path.GetFileNameWithoutExtension(name.Replace("\\", "/").Replace("/", "\\"));
        string dir = Path.Combine(gameRoot, "gfx", "coat_of_arms", folder);
        if (!Directory.Exists(dir)) return null;
        foreach (var f in Directory.EnumerateFiles(dir))
        {
            if (Path.GetFileNameWithoutExtension(f).Equals(baseName, StringComparison.OrdinalIgnoreCase))
                return f;
        }
        return null;
    }

    /// <summary>Renders resolved layers to a BGRA byte array (size x size).</summary>
    public static byte[]? Render(List<PdxCoaLayer> layers, string gameRoot, int size, Dictionary<string, DdsImage>? cache)
    {
        if (layers.Count == 0) return null;
        cache ??= new Dictionary<string, DdsImage>(StringComparer.OrdinalIgnoreCase);
        var canvas = new byte[size * size * 4];
        foreach (var layer in layers)
        {
            bool isPattern = layer.PatternTexture != null;
            string? texPath = layer.PatternTexture != null
                ? FindTexture(gameRoot, "patterns", layer.PatternTexture)
                : FindTexture(gameRoot, layer.Textured ? "textured_emblems" : "colored_emblems", layer.EmblemTexture ?? "");
            if (texPath == null) continue;

            if (!cache.TryGetValue(texPath, out DdsImage? dds))
            {
                try { dds = DdsDecoder.Decode(texPath) ?? new DdsImage(); } catch { dds = new DdsImage(); }
                cache[texPath] = dds ?? new DdsImage();
            }
            if (dds == null || dds.Data == null || dds.Data.Length == 0) continue;

            double centerX = (layer.Instance?.X ?? 0.5) * size;
            double centerY = (layer.Instance?.Y ?? 0.5) * size;
            double halfW = (layer.Instance?.Sx ?? 1.0) * size / 2.0;
            double halfH = (layer.Instance?.Sy ?? 1.0) * size / 2.0;
            double rot = -(layer.Instance?.Rotation ?? 0.0) * Math.PI / 180.0;
            double cosR = Math.Cos(rot), sinR = Math.Sin(rot);

            int iw = dds.Width, ih = dds.Height;
            byte[] src = dds.Data;

            int x0 = Math.Max(0, (int)Math.Floor(centerX - Math.Abs(halfW)));
            int x1 = Math.Min(size - 1, (int)Math.Ceiling(centerX + Math.Abs(halfW)));
            int y0 = Math.Max(0, (int)Math.Floor(centerY - Math.Abs(halfH)));
            int y1 = Math.Min(size - 1, (int)Math.Ceiling(centerY + Math.Abs(halfH)));

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    double px = x - centerX;
                    double py = y - centerY;
                    double rx = px * cosR - py * sinR;
                    double ry = px * sinR + py * cosR;
                    if (Math.Abs(rx) > halfW || Math.Abs(ry) > halfH) continue;

                    double tu = (rx / (2 * halfW) + 0.5) * iw;
                    double tv = (ry / (2 * halfH) + 0.5) * ih;
                    if (tu < 0 || tu > iw - 1 || tv < 0 || tv > ih - 1) continue;

                    int px0 = (int)Math.Floor(tu), py0 = (int)Math.Floor(tv);
                    int px1 = Math.Min(iw - 1, px0 + 1), py1 = Math.Min(ih - 1, py0 + 1);
                    double fx = tu - px0, fy = tv - py0;
                    int idx00 = (py0 * iw + px0) * 4;
                    int idx10 = (py0 * iw + px1) * 4;
                    int idx01 = (py1 * iw + px0) * 4;
                    int idx11 = (py1 * iw + px1) * 4;

                    int br = (int)(Bil(src, idx00 + 0, idx10 + 0, idx01 + 0, idx11 + 0, fx, fy) + 0.5);
                    int bg = (int)(Bil(src, idx00 + 1, idx10 + 1, idx01 + 1, idx11 + 1, fx, fy) + 0.5);
                    int bb = (int)(Bil(src, idx00 + 2, idx10 + 2, idx01 + 2, idx11 + 2, fx, fy) + 0.5);
                    double ba = Bil(src, idx00 + 3, idx10 + 3, idx01 + 3, idx11 + 3, fx, fy);

                    uint outCol;
                    double alpha;
                    if (layer.Textured || !layer.HasColor)
                    {
                        outCol = 0xFF000000u | ((uint)bg << 16) | ((uint)br << 8) | (uint)bb;
                        alpha = ba;
                    }
                    else
                    {
                        byte c1r = (byte)(layer.C1 >> 8), c1g = (byte)(layer.C1 >> 16), c1b = (byte)layer.C1;
                        byte c2r = (byte)(layer.C2 >> 8), c2g = (byte)(layer.C2 >> 16), c2b = (byte)layer.C2;
                        byte c3r = (byte)(layer.C3 >> 8), c3g = (byte)(layer.C3 >> 16), c3b = (byte)layer.C3;
                        double wr = br / 255.0, wg = bg / 255.0, wb = bb / 255.0;
                        int or_ = (int)(c1r * wr + c2r * wg + c3r * wb + 0.5);
                        int og = (int)(c1g * wr + c2g * wg + c3g * wb + 0.5);
                        int ob = (int)(c1b * wr + c2b * wg + c3b * wb + 0.5);
                        outCol = (uint)(0xFF000000 | ((uint)Math.Clamp(og, 0, 255) << 16) | ((uint)Math.Clamp(or_, 0, 255) << 8) | (uint)Math.Clamp(ob, 0, 255));
                        alpha = (ba / 255.0);
                    }

                    if (alpha > 0)
                    {
                        int oi = (y * size + x) * 4;
                        byte oa = canvas[oi + 3];
                        double srcA = Math.Clamp(alpha, 0, 1);
                        double dstA = oa / 255.0;
                        double outA = srcA + dstA * (1 - srcA);
                        if (outA <= 0) continue;
                        int sr = (int)(outCol >> 8) & 0xFF, sg = (int)(outCol >> 16) & 0xFF, sb = (int)outCol;
                        canvas[oi + 0] = (byte)Math.Round((sr * srcA + canvas[oi + 0] * dstA * (1 - srcA)) / outA);
                        canvas[oi + 1] = (byte)Math.Round((sg * srcA + canvas[oi + 1] * dstA * (1 - srcA)) / outA);
                        canvas[oi + 2] = (byte)Math.Round((sb * srcA + canvas[oi + 2] * dstA * (1 - srcA)) / outA);
                        canvas[oi + 3] = (byte)Math.Round(outA * 255);
                    }
                }
            }
        }
        return canvas;
    }

    private static double Bil(byte[] src, int a0, int a1, int a2, int a3, double fx, double fy) =>
        (src[a0] * (1 - fx) + src[a1] * fx) * (1 - fy) + (src[a2] * (1 - fx) + src[a3] * fx) * fy;

    /// <summary>Finds the first dynasty id of a culture from the dynasty files.</summary>
    public static long? FindDynastyOfCulture(string gameRoot, string culture)
    {
        string dir = Path.Combine(gameRoot, "common", "dynasties");
        if (!Directory.Exists(dir) || string.IsNullOrEmpty(culture)) return null;
        foreach (var file in Directory.EnumerateFiles(dir, "*.txt").OrderBy(f => f))
        {
            string content;
            try { content = File.ReadAllText(file); } catch { continue; }
            int pos = 0;
            while (true)
            {
                pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
                if (pos >= content.Length) break;
                string idTok = PdxCoaColor2.ReadKey(content, ref pos);
                if (idTok.Length == 0) break;
                pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
                if (pos >= content.Length) break;
                long id;
                if (content[pos] == '{')
                {
                    int end = PdxCoaColor2.FindMatchingBrace(content, pos);
                    string block = content.Substring(pos + 1, end - pos - 1);
                    if (long.TryParse(idTok, out id) && BlockHasCulture(block, culture)) return id;
                    pos = end + 1;
                    continue;
                }
                if (content[pos] != '=') { pos = PdxCoaColor2.SkipLine(content, pos); continue; }
                pos++;
                pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
                if (pos >= content.Length || content[pos] != '{') { pos = PdxCoaColor2.SkipLine(content, pos); continue; }
                int end2 = PdxCoaColor2.FindMatchingBrace(content, pos);
                string block2 = content.Substring(pos + 1, end2 - pos - 1);
                if (long.TryParse(idTok, out id) && BlockHasCulture(block2, culture)) return id;
                pos = end2 + 1;
            }
        }
        return null;
    }

    private static bool BlockHasCulture(string block, string culture)
    {
        int pos = 0;
        while (true)
        {
            pos = PdxCoaColor2.SkipWs(block, pos, block.Length);
            if (pos >= block.Length) break;
            string key = PdxCoaColor2.ReadKey(block, ref pos);
            if (key == "culture")
            {
                pos = PdxCoaColor2.SkipWs(block, pos, block.Length);
                if (pos < block.Length && block[pos] == '=') pos++;
                pos = PdxCoaColor2.SkipWs(block, pos, block.Length);
                string val = PdxCoaColor2.ReadToken(block, ref pos);
                val = val.Trim().Trim('"');
                if (val.Equals(culture, StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }
            pos = PdxCoaColor2.SkipLine(block, pos);
        }
        return false;
    }
}