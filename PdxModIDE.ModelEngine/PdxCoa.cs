using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace PdxModIDE.ModelEngine;

public sealed class CoaInstance
{
    public double X, Y;
    public double Sx = 1, Sy = 1;
    public double Rotation;
    public double Depth;
}

public sealed class CoaEmblem
{
    public string? Texture;
    public bool Textured;
    public bool HasC1, HasC2, HasC3;
    public uint C1, C2, C3;
    public List<CoaInstance> Instances = new();
}

public sealed class CoaSubNode
{
    public CoaInstance Transform = new() { Sx = 1, Sy = 1 };
    public CoaNode Node = new();
}

public sealed class CoaNode
{
    public string? Ref;
    public string? Pattern;
    public bool HasC1, HasC2, HasC3;
    public uint C1, C2, C3;
    public List<CoaEmblem> Emblems = new();
    public List<CoaSubNode> Subs = new();
    public List<CoaInstance> Instances = new();
}

/// <summary>One flattened draw operation (pattern or one emblem instance).</summary>
public sealed class PdxCoaLayer
{
    public string? PatternTexture;
    public string? EmblemTexture;
    public bool Textured;
    public bool HasColor;
    public uint C1, C2, C3;
    public CoaInstance? Instance;
}

public static class PdxCoaParser
{
    public static Dictionary<string, CoaNode> LoadDefinitions(string gameRoot)
    {
        var defs = new Dictionary<string, CoaNode>(StringComparer.OrdinalIgnoreCase);
        string dir = Path.Combine(gameRoot, "common", "coat_of_arms", "coat_of_arms");
        if (!Directory.Exists(dir)) return defs;
        foreach (var file in Directory.EnumerateFiles(dir, "*.txt"))
        {
            try
            {
                ParseFile(File.ReadAllText(file), defs);
            }
            catch { }
        }
        return defs;
    }

    private static void ParseFile(string content, Dictionary<string, CoaNode> defs)
    {
        var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        int pos = PdxCoaColor2.SkipWs(content, 0, content.Length);
        while (true)
        {
            pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
            if (pos >= content.Length) break;
            string name = PdxCoaColor2.ReadKey(content, ref pos);
            if (name.Length == 0) break;
            if (name[0] == '@')
            {
                pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
                if (pos < content.Length && content[pos] == '=')
                {
                    pos++;
                    pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
                    string val = PdxCoaColor2.ReadToken(content, ref pos);
                    vars[name] = val;
                }
                else pos = PdxCoaColor2.SkipLine(content, pos);
                continue;
            }
            pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
            if (pos >= content.Length || content[pos] != '=')
            {
                pos = PdxCoaColor2.SkipLine(content, pos);
                continue;
            }
            pos++;
            pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
            if (pos >= content.Length) break;
            if (content[pos] == '{')
            {
                int end = PdxCoaColor2.FindMatchingBrace(content, pos);
                string block = content.Substring(pos + 1, end - pos - 1);
                var node = ParseNode(block, vars);
                defs[name] = node;
                pos = end + 1;
            }
            else
            {
                string val = PdxCoaColor2.ReadTokenSub(content, ref pos, vars);
                defs[name] = new CoaNode { Ref = val };
            }
        }
    }

    private static CoaNode ParseNode(string block, Dictionary<string, string> vars)
    {
        var node = new CoaNode();
        int pos = PdxCoaColor2.SkipWs(block, 0, block.Length);
        while (true)
        {
            pos = PdxCoaColor2.SkipWs(block, pos, block.Length);
            if (pos >= block.Length) break;
            string key = PdxCoaColor2.ReadKey(block, ref pos);
            if (key.Length == 0) break;
            pos = PdxCoaColor2.SkipWs(block, pos, block.Length);
            if (pos >= block.Length || block[pos] != '=')
            {
                pos = PdxCoaColor2.SkipLine(block, pos);
                continue;
            }
            pos++;
            pos = PdxCoaColor2.SkipWs(block, pos, block.Length);
            if (pos >= block.Length) break;

            if (block[pos] == '{')
            {
                int end = PdxCoaColor2.FindMatchingBrace(block, pos);
                string inner = block.Substring(pos + 1, end - pos - 1);
                if (key == "colored_emblem" || key == "textured_emblem")
                    node.Emblems.Add(ParseEmblem(inner, vars, key == "textured_emblem"));
                else if (key == "sub")
                    node.Subs.Add(new CoaSubNode { Node = ParseNode(inner, vars) });
                else if (key == "instance")
                    node.Instances.Add(ParseInstance(inner, vars));
                pos = end + 1;
                continue;
            }

            string scalar = PdxCoaColor2.ReadTokenSub(block, ref pos, vars);
            switch (key)
            {
                case "pattern": node.Pattern = Scalar(scalar, vars); break;
                case "color1": node.C1 = ColorVal(scalar, vars); node.HasC1 = true; break;
                case "color2": node.C2 = ColorVal(scalar, vars); node.HasC2 = true; break;
                case "color3": node.C3 = ColorVal(scalar, vars); node.HasC3 = true; break;
                case "parent": node.Ref = Scalar(scalar, vars); break;
            }
        }
        return node;
    }

    private static CoaEmblem ParseEmblem(string inner, Dictionary<string, string> vars, bool textured)
    {
        var e = new CoaEmblem { Textured = textured };
        int pos = PdxCoaColor2.SkipWs(inner, 0, inner.Length);
        while (true)
        {
            pos = PdxCoaColor2.SkipWs(inner, pos, inner.Length);
            if (pos >= inner.Length) break;
            string key = PdxCoaColor2.ReadKey(inner, ref pos);
            if (key.Length == 0) break;
            pos = PdxCoaColor2.SkipWs(inner, pos, inner.Length);
            if (pos >= inner.Length || inner[pos] != '=')
            {
                pos = PdxCoaColor2.SkipLine(inner, pos);
                continue;
            }
            pos++;
            pos = PdxCoaColor2.SkipWs(inner, pos, inner.Length);
            if (pos >= inner.Length) break;
            if (inner[pos] == '{')
            {
                int end = PdxCoaColor2.FindMatchingBrace(inner, pos);
                string inst = inner.Substring(pos + 1, end - pos - 1);
                if (key == "instance") e.Instances.Add(ParseInstance(inst, vars));
                pos = end + 1;
            }
            else
            {
                string scalar = PdxCoaColor2.ReadTokenSub(inner, ref pos, vars);
                switch (key)
                {
                    case "texture": e.Texture = Scalar(scalar, vars); break;
                    case "color1": e.C1 = ColorVal(scalar, vars); e.HasC1 = true; break;
                    case "color2": e.C2 = ColorVal(scalar, vars); e.HasC2 = true; break;
                    case "color3": e.C3 = ColorVal(scalar, vars); e.HasC3 = true; break;
                }
            }
        }
        if (e.Instances.Count == 0) e.Instances.Add(new CoaInstance() { Sx = 1, Sy = 1 });
        return e;
    }

    private static CoaInstance ParseInstance(string inner, Dictionary<string, string> vars)
    {
        var inst = new CoaInstance { Sx = 1, Sy = 1 };
        int pos = PdxCoaColor2.SkipWs(inner, 0, inner.Length);
        while (true)
        {
            pos = PdxCoaColor2.SkipWs(inner, pos, inner.Length);
            if (pos >= inner.Length) break;
            string key = PdxCoaColor2.ReadKey(inner, ref pos);
            if (key.Length == 0) break;
            pos = PdxCoaColor2.SkipWs(inner, pos, inner.Length);
            if (pos >= inner.Length || inner[pos] != '=')
            {
                pos = PdxCoaColor2.SkipLine(inner, pos);
                continue;
            }
            pos++;
            pos = PdxCoaColor2.SkipWs(inner, pos, inner.Length);
            if (pos >= inner.Length) break;
            if (inner[pos] == '{')
            {
                int end = PdxCoaColor2.FindMatchingBrace(inner, pos);
                string pair = inner.Substring(pos + 1, end - pos - 1);
                var nums = PdxCoaColor2.NumbersSub(pair, vars);
                if (key == "position" && nums.Count >= 2) { inst.X = nums[0]; inst.Y = nums[1]; }
                else if (key == "scale" && nums.Count >= 2) { inst.Sx = nums[0]; inst.Sy = nums[1]; }
                pos = end + 1;
            }
            else
            {
                string scalar = PdxCoaColor2.ReadTokenSub(inner, ref pos, vars);
                if (key == "rotation" && double.TryParse(scalar, NumberStyles.Float, CultureInfo.InvariantCulture, out var r)) inst.Rotation = r;
                else if (key == "depth" && double.TryParse(scalar, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) inst.Depth = d;
            }
        }
        return inst;
    }

    private static string Scalar(string token, Dictionary<string, string> vars)
    {
        token = token.Trim();
        if (token.Length >= 2 && token[0] == '"' && token[^1] == '"') return token.Substring(1, token.Length - 2);
        if (token.StartsWith("@", StringComparison.Ordinal) && vars.TryGetValue(token, out var v)) return v;
        return token;
    }

    private static uint ColorVal(string token, Dictionary<string, string> vars) =>
        PdxCoaColor.Parse(Scalar(token, vars), 0xFF000000);
}

/// <summary>Internal tokens/helpers shared with parser.</summary>
public static class PdxCoaColor2
{
    public static int SkipWs(string s, int pos, int end)
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

    public static int SkipLine(string s, int pos)
    {
        while (pos < s.Length && s[pos] != '\n') pos++;
        return pos;
    }

    public static string ReadKey(string s, ref int pos)
    {
        int start = pos;
        while (pos < s.Length && !char.IsWhiteSpace(s[pos]) && s[pos] != '=' && s[pos] != '{' && s[pos] != '}' && s[pos] != '#')
            pos++;
        return s.Substring(start, pos - start);
    }

    public static string ReadToken(string s, ref int pos)
    {
        int start = pos;
        bool quote = pos < s.Length && s[pos] == '"';
        if (quote) pos++;
        while (pos < s.Length)
        {
            char c = s[pos];
            if (quote) { if (c == '"') { pos++; break; } }
            else if (char.IsWhiteSpace(c) || c == '{' || c == '}' || c == '#') break;
            pos++;
        }
        return s.Substring(start, pos - start);
    }

    public static string ReadTokenSub(string s, ref int pos, Dictionary<string, string> vars)
    {
        string t = ReadToken(s, ref pos);
        if (t.Length > 0 && t[0] == '@' && vars.TryGetValue(t, out var v)) return v;
        return t;
    }

    public static int FindMatchingBrace(string s, int open)
    {
        int depth = 0;
        bool inStr = false;
        for (int i = open; i < s.Length; i++)
        {
            char c = s[i];
            if (inStr) { if (c == '"') inStr = false; }
            else if (c == '"') inStr = true;
            else if (c == '{') depth++;
            else if (c == '}') { depth--; if (depth == 0) return i; }
            else if (c == '#') { while (i < s.Length && s[i] != '\n') i++; }
        }
        return s.Length - 1;
    }

    public static List<double> NumbersSub(string s, Dictionary<string, string> vars)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '@')
            {
                int j = i + 1;
                while (j < s.Length && (char.IsLetterOrDigit(s[j]) || s[j] == '_')) j++;
                string name = s.Substring(i, j - i);
                if (vars.TryGetValue(name, out var v)) sb.Append(' ').Append(v);
                i = j - 1;
            }
            else sb.Append(s[i]);
        }
        var list = new List<double>();
        var num = new StringBuilder();
        string t = sb.ToString();
        for (int i = 0; i <= t.Length; i++)
        {
            char ch = i < t.Length ? t[i] : ' ';
            if (char.IsDigit(ch) || ch == '.' || ch == '-' || ch == '+') num.Append(ch);
            else if (num.Length > 0)
            {
                if (double.TryParse(num.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) list.Add(v);
                num.Clear();
            }
        }
        return list;
    }
}