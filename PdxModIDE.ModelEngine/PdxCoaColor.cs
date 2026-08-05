using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace PdxModIDE.ModelEngine;

/// <summary>Resolves named / inline CoA colors to BGRA.</summary>
public static class PdxCoaColor
{
    private static readonly Dictionary<string, uint> Palette = new(StringComparer.OrdinalIgnoreCase);
    private static bool _loaded;

    public static int PaletteCount => Palette.Count;
    public static bool HasColor(string name) => Palette.ContainsKey(name);
    public static IEnumerable<string> PaletteKeys => Palette.Keys;

    public static void EnsureLoaded(string gameRoot)
    {
        if (_loaded) return;
        _loaded = true;
        string dir = Path.Combine(gameRoot, "common", "named_colors");
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.EnumerateFiles(dir, "*.txt", SearchOption.AllDirectories))
        {
            try
            {
                string content = File.ReadAllText(file);
                ParseColorFile(content);
            }
            catch { }
        }
    }

    private static void ParseColorFile(string content)
    {
        int open = content.IndexOf("colors", StringComparison.OrdinalIgnoreCase);
        if (open < 0) return;
        string sub = content.Substring(open);
        int brace = sub.IndexOf('{');
        if (brace < 0) return;
        int close = FindMatchingBrace(sub, brace);
        string inner = sub.Substring(brace + 1, close - brace - 1);
        int pos = SkipWs(inner, 0, inner.Length);
        while (true)
        {
            pos = SkipWs(inner, pos, inner.Length);
            if (pos >= inner.Length) break;
            string name = ReadKey(inner, ref pos);
            if (name.Length == 0) break;
            pos = SkipWs(inner, pos, inner.Length);
            if (pos >= inner.Length || inner[pos] != '=') { pos = SkipLine(inner, pos); continue; }
            pos++;
            pos = SkipWs(inner, pos, inner.Length);
            if (pos >= inner.Length) break;
            uint color;
            if (!TryParseColorSpec(inner, ref pos, out color)) continue;
            Palette[name] = color;
        }
    }

    private static int FindOpeningBrace(string content, int start)
    {
        int idx = content.IndexOf('{', start);
        return idx < 0 ? content.Length : idx + 1;
    }

    public static uint Parse(string? spec, uint fallback = 0xFF000000)
    {
        if (string.IsNullOrWhiteSpace(spec)) return fallback;
        string s = spec.Trim();
        if (Palette.TryGetValue(s, out var c)) return c;
        if (s.Length == 6 && TryParseHex(s, out c)) return c;
        return fallback;
    }

    private static bool TryParseHex(string s, out uint color)
    {
        color = 0;
        foreach (char ch in s)
        {
            int d = Hex(ch);
            if (d < 0) return false;
            color = (color << 4) | (uint)d;
        }
        return s.Length == 6 || s.Length == 8;
    }

    private static int Hex(char c) =>
        c >= '0' && c <= '9' ? c - '0' :
        c >= 'a' && c <= 'f' ? c - 'a' + 10 :
        c >= 'A' && c <= 'F' ? c - 'A' + 10 : -1;

    private static bool TryParseColorSpec(string content, ref int pos, out uint color)
    {
        color = 0xFF000000;
        pos = SkipWs(content, pos, content.Length);
        if (pos >= content.Length) return false;

        string tag = ReadKey(content, ref pos);
        pos = SkipWs(content, pos, content.Length);

        if (tag.Equals("hsv", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("hsv360", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("rgb", StringComparison.OrdinalIgnoreCase) ||
            tag.Equals("hex", StringComparison.OrdinalIgnoreCase))
        {
            if (pos < content.Length && content[pos] == '{')
            {
                int end = FindMatchingBrace(content, pos);
                string inner = content.Substring(pos + 1, end - pos - 1);
                var nums = ReadNumbers(inner);
                if (nums.Count >= 3)
                {
                    double a = nums[0], b = nums[1], c = nums[2];
                    if (tag.Equals("hsv", StringComparison.OrdinalIgnoreCase))
                        color = HsvToBgra(a, b, c);
                    else if (tag.Equals("hsv360", StringComparison.OrdinalIgnoreCase))
                        color = HsvToBgra(a / 360.0, b / 100.0, c / 100.0);
                    else if (tag.Equals("rgb", StringComparison.OrdinalIgnoreCase))
                        color = ClampByteToBgra(a, b, c);
                    else
                        color = HsvToBgra(a, b, c);
                }
                pos = end + 1;
                return true;
            }
            return false;
        }

        if (pos < content.Length && content[pos] == '{')
        {
            int end = FindMatchingBrace(content, pos);
            string inner = content.Substring(pos + 1, end - pos - 1);
            var nums = ReadNumbers(inner);
            if (nums.Count >= 3)
            {
                int r = Clamp01(nums[0]);
                int g = Clamp01(nums[1]);
                int b = Clamp01(nums[2]);
                color = (uint)(0xFF000000 | (uint)((g << 16) | (r << 8) | b));
            }
            pos = end + 1;
            return true;
        }

        return false;
    }

    private static int Clamp01(double v) => v <= 1.0 ? (int)Math.Round(v * 255) : (int)v;

    private static uint ClampByteToBgra(double r, double g, double b)
    {
        byte rr = (byte)Math.Clamp(r, 0, 255), gg = (byte)Math.Clamp(g, 0, 255), bb = (byte)Math.Clamp(b, 0, 255);
        return (uint)(0xFF000000 | ((uint)gg << 16) | ((uint)rr << 8) | bb);
    }

    public static uint HsvToBgra(double h, double s, double v)
    {
        h = ((h % 1.0) + 1.0) % 1.0;
        s = Math.Clamp(s, 0, 1);
        v = Math.Clamp(v, 0, 1);
        double r, g, b;
        if (s < 1e-9) { r = g = b = v; }
        else
        {
            double hh = h * 6.0;
            int i = (int)Math.Floor(hh);
            double f = hh - i;
            double p = v * (1 - s);
            double q = v * (1 - s * f);
            double t = v * (1 - s * (1 - f));
            switch (i % 6)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                default: r = v; g = p; b = q; break;
            }
        }
        byte rr = (byte)Math.Round(r * 255), gg = (byte)Math.Round(g * 255), bb = (byte)Math.Round(b * 255);
        return (uint)(0xFF000000 | ((uint)gg << 16) | ((uint)rr << 8) | bb);
    }

    private static List<double> ReadNumbers(string s)
    {
        var list = new List<double>();
        var sb = new StringBuilder();
        for (int i = 0; i <= s.Length; i++)
        {
            char ch = i < s.Length ? s[i] : ' ';
            if (char.IsDigit(ch) || ch == '.' || ch == '-' || ch == '+')
                sb.Append(ch);
            else if (sb.Length > 0)
            {
                if (double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                    list.Add(v);
                sb.Clear();
            }
        }
        return list;
    }

    private static int FindMatchingBrace(string s, int open)
    {
        int depth = 0;
        for (int i = open; i < s.Length; i++)
        {
            if (s[i] == '{') depth++;
            else if (s[i] == '}') { depth--; if (depth == 0) return i; }
        }
        return s.Length - 1;
    }

    private static int SkipWs(string s, int pos, int end)
    {
        while (pos < end)
        {
            char c = s[pos];
            if (char.IsWhiteSpace(c)) pos++;
            else if (c == '#') { while (pos < end && s[pos] != '\n') pos++; }
            else break;
        }
        return pos;
    }

    private static int SkipLine(string s, int pos)
    {
        while (pos < s.Length && s[pos] != '\n') pos++;
        return pos;
    }

    private static string ReadKey(string s, ref int pos)
    {
        int start = pos;
        while (pos < s.Length && !char.IsWhiteSpace(s[pos]) && s[pos] != '=' && s[pos] != '{' && s[pos] != '}' && s[pos] != '#')
            pos++;
        return s.Substring(start, pos - start);
    }
}