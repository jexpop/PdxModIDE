using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace PdxModIDE.ModelEngine;

/// <summary>One key=value entry parsed from a script block.</summary>
internal sealed class CoaEntry
{
    public string Key = "";
    public string? Value;
    public string? Inner;
}

/// <summary>A weighted list (pattern/color/emblem/template) with conditional special selections.</summary>
internal sealed class CoaWeightedList
{
    public List<(float Weight, string Value)> Base = new();
    public List<(string Trigger, List<(float Weight, string Value)> Entries)> Specials = new();
}

/// <summary>Trigger evaluation context (culture scope + representative faith religion).</summary>
public sealed class CoaTriggerCtx
{
    public HashSet<string> CoaGfx = new(StringComparer.OrdinalIgnoreCase);
    public string Religion = "";
    public string Family = "";
}

internal sealed class CoaTemplateEmblem
{
    public string? Texture;
    public bool Textured;
    public string? C1, C2, C3;
    public List<CoaInstance> Instances = new();
}

internal sealed class CoaTemplateSub
{
    public CoaInstance Transform = new() { Sx = 1, Sy = 1 };
    public CoaTemplate? Node;
}

internal sealed class CoaTemplate
{
    public string? Pattern;
    public string?[] Colors = new string?[4];
    public List<CoaTemplateEmblem> Emblems = new();
    public List<CoaTemplateSub> Subs = new();
}

/// <summary>Parsed culture definition (coa_gfx list + color).</summary>
public sealed class CoaCultureDef
{
    public HashSet<string> CoaGfx = new(StringComparer.OrdinalIgnoreCase);
    public string Color = "";
}

/// <summary>
/// Full CK3 coat-of-arms template engine: resolves the procedural default CoA
/// of a culture from coat_of_arms_template_lists, random templates and
/// pattern/color/emblem lists, evaluating scripted triggers (culture + faith).
/// </summary>
public static class PdxCoaTemplateEngine
{
    private static bool _loaded;
    private static string _root = "";
    private static readonly Dictionary<string, CoaWeightedList> PatternLists = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, CoaWeightedList> ColorLists = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, CoaWeightedList> EmblemLists = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, CoaWeightedList> TexturedEmblemLists = new(StringComparer.OrdinalIgnoreCase);
    private static CoaWeightedList _templateSelection = new();
    private static readonly Dictionary<string, CoaTemplate> Templates = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> ScriptedTriggers = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> ReligionFamily = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, CoaCultureDef> Cultures = new(StringComparer.OrdinalIgnoreCase);

    public static void EnsureLoaded(string gameRoot)
    {
        if (_loaded) return;
        _loaded = true;
        _root = gameRoot;
        PdxCoaColor.EnsureLoaded(gameRoot);
        try { LoadScriptedTriggers(gameRoot); } catch { }
        try { LoadTemplateLists(gameRoot); } catch { }
        try { LoadTemplates(gameRoot); } catch { }
        try { LoadReligionFamilies(gameRoot); } catch { }
        try { LoadCultures(gameRoot); } catch { }
    }

    public static CoaCultureDef? GetCulture(string gameRoot, string cultureId)
    {
        EnsureLoaded(gameRoot);
        return Cultures.TryGetValue(cultureId ?? "", out var c) ? c : null;
    }

    /// <summary>Generates the culture default CoA as draw layers (pattern + emblems), ready for Render().</summary>
    public static List<PdxCoaLayer>? GenerateCultureCoaLayers(string gameRoot, string cultureId)
    {
        EnsureLoaded(gameRoot);
        var culture = GetCulture(gameRoot, cultureId);
        if (culture == null) return null;
        var ctx = new CoaTriggerCtx { CoaGfx = culture.CoaGfx, Religion = CultureToReligion(culture) };
        ctx.Family = ReligionFamily.TryGetValue(ctx.Religion, out var fam) ? fam : "";
        string seed = cultureId + "|tpl";
        string templateName = Pick(_templateSelection, ctx, seed);
        if (string.IsNullOrEmpty(templateName) || !Templates.TryGetValue(templateName, out var tpl))
            return FallbackLayers(ctx, cultureId);
        return ResolveTemplate(tpl, ctx, cultureId);
    }

    // ---------------------------------------------------------------- parsing

    private static void LoadScriptedTriggers(string root)
    {
        string dir = Path.Combine(root, "common", "scripted_triggers");
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.EnumerateFiles(dir, "*.txt"))
        {
            string content = File.ReadAllText(file);
            int pos = PdxCoaColor2.SkipWs(content, 0, content.Length);
            while (true)
            {
                pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
                if (pos >= content.Length) break;
                string name = PdxCoaColor2.ReadKey(content, ref pos);
                if (name.Length == 0) break;
                pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
                if (pos >= content.Length || content[pos] != '=') { pos = PdxCoaColor2.SkipLine(content, pos); continue; }
                pos++;
                pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
                if (pos >= content.Length || content[pos] != '{') continue;
                int end = PdxCoaColor2.FindMatchingBrace(content, pos);
                ScriptedTriggers[name] = content.Substring(pos + 1, end - pos - 1);
                pos = end + 1;
            }
        }
    }

    private static void LoadTemplateLists(string root)
    {
        string dir = Path.Combine(root, "common", "coat_of_arms", "template_lists");
        if (!Directory.Exists(dir)) return;
        LoadContainer(Path.Combine(dir, "pattern_lists.txt"), "pattern_texture_lists", PatternLists);
        LoadContainer(Path.Combine(dir, "color_lists.txt"), "color_lists", ColorLists);
        LoadContainer(Path.Combine(dir, "colored_emblem_lists.txt"), "colored_emblem_texture_lists", EmblemLists);
        LoadContainer(Path.Combine(dir, "textured_emblem_lists.txt"), "textured_emblem_texture_lists", TexturedEmblemLists);

        string tplFile = Path.Combine(dir, "coa_templates.txt");
        if (File.Exists(tplFile))
        {
            string content = File.ReadAllText(tplFile);
            var lists = ParseContainerBlock(content, "coat_of_arms_template_lists");
            if (lists != null)
            {
                int a = lists.Value.Item1, b = lists.Value.Item2;
                string block = content.Substring(a + 1, b - a - 1);
                foreach (var e in ParseEntries(block))
                {
                    if (e.Key == "all" && e.Inner != null)
                    {
                        _templateSelection = ParseWeightedList(e.Inner);
                        break;
                    }
                }
            }
        }
    }

    private static void LoadContainer(string path, string container, Dictionary<string, CoaWeightedList> target)
    {
        if (!File.Exists(path)) return;
        string content = File.ReadAllText(path);
        var range = ParseContainerBlock(content, container);
        if (range == null) return;
        string block = content.Substring(range.Value.Item1 + 1, range.Value.Item2 - range.Value.Item1 - 1);
        foreach (var e in ParseEntries(block))
        {
            if (e.Key.Length == 0 || e.Inner == null) continue;
            target[e.Key] = ParseWeightedList(e.Inner);
        }
    }

    private static void LoadTemplates(string root)
    {
        string dir = Path.Combine(root, "common", "coat_of_arms", "coat_of_arms");
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.EnumerateFiles(dir, "*.txt"))
        {
            string content = File.ReadAllText(file);
            int pos = PdxCoaColor2.SkipWs(content, 0, content.Length);
            while (true)
            {
                pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
                if (pos >= content.Length) break;
                string name = PdxCoaColor2.ReadKey(content, ref pos);
                if (name.Length == 0) break;
                pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
                if (pos >= content.Length || content[pos] != '=') { pos = PdxCoaColor2.SkipLine(content, pos); continue; }
                pos++;
                pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
                if (pos >= content.Length || content[pos] != '{') continue;
                int end = PdxCoaColor2.FindMatchingBrace(content, pos);
                if (name == "template")
                {
                    string inner = content.Substring(pos + 1, end - pos - 1);
                    foreach (var te in ParseEntries(inner))
                    {
                        if (te.Inner == null) continue;
                        var t = ParseTemplate(te.Inner);
                        if (t != null) Templates[te.Key] = t;
                    }
                }
                pos = end + 1;
            }
        }
    }

    private static void LoadReligionFamilies(string root)
    {
        string dir = Path.Combine(root, "common", "religion", "religion_types");
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.EnumerateFiles(dir, "*.txt", SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(file);
            int pos = PdxCoaColor2.SkipWs(content, 0, content.Length);
            while (true)
            {
                pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
                if (pos >= content.Length) break;
                string name = PdxCoaColor2.ReadKey(content, ref pos);
                if (name.Length == 0) break;
                pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
                if (pos >= content.Length || content[pos] != '=') { pos = PdxCoaColor2.SkipLine(content, pos); continue; }
                pos++;
                pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
                if (pos >= content.Length || content[pos] != '{') continue;
                int end = PdxCoaColor2.FindMatchingBrace(content, pos);
                string inner = content.Substring(pos + 1, end - pos - 1);
                string family = "";
                foreach (var e in ParseEntries(inner))
                    if (e.Key == "family" && e.Value != null) { family = e.Value; break; }
                if (family.Length > 0) ReligionFamily[name] = family;
                pos = end + 1;
            }
        }
    }

    private static void LoadCultures(string root)
    {
        string dir = Path.Combine(root, "common", "culture", "cultures");
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.EnumerateFiles(dir, "*.txt", SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(file);
            int pos = PdxCoaColor2.SkipWs(content, 0, content.Length);
            while (true)
            {
                pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
                if (pos >= content.Length) break;
                string name = PdxCoaColor2.ReadKey(content, ref pos);
                if (name.Length == 0) break;
                pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
                if (pos >= content.Length || content[pos] != '=') { pos = PdxCoaColor2.SkipLine(content, pos); continue; }
                pos++;
                pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
                if (pos >= content.Length || content[pos] != '{') continue;
                int end = PdxCoaColor2.FindMatchingBrace(content, pos);
                string inner = content.Substring(pos + 1, end - pos - 1);
                var cd = new CoaCultureDef();
                foreach (var e in ParseEntries(inner))
                {
                    if (e.Key == "color" && e.Value != null) cd.Color = e.Value.Trim().Trim('"');
                    else if (e.Key == "coa_gfx" && e.Inner != null)
                    {
                        foreach (var g in ParseTokens(e.Inner))
                            if (g.Length > 0) cd.CoaGfx.Add(g);
                    }
                }
                Cultures[name] = cd;
                pos = end + 1;
            }
        }
    }

    private static (int, int)? ParseContainerBlock(string content, string container)
    {
        int pos = PdxCoaColor2.SkipWs(content, 0, content.Length);
        while (true)
        {
            pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
            if (pos >= content.Length) return null;
            string name = PdxCoaColor2.ReadKey(content, ref pos);
            if (name.Length == 0) return null;
            pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
            if (pos >= content.Length || content[pos] != '=') { pos = PdxCoaColor2.SkipLine(content, pos); continue; }
            pos++;
            pos = PdxCoaColor2.SkipWs(content, pos, content.Length);
            if (pos >= content.Length || content[pos] != '{') continue;
            if (name.Equals(container, StringComparison.OrdinalIgnoreCase)) return (pos, PdxCoaColor2.FindMatchingBrace(content, pos));
            pos = PdxCoaColor2.FindMatchingBrace(content, pos) + 1;
        }
    }

    private static CoaWeightedList ParseWeightedList(string block)
    {
        var list = new CoaWeightedList();
        foreach (var e in ParseEntries(block))
        {
            if (e.Key == "special_selection" && e.Inner != null)
            {
                string trigger = "";
                var entries = new List<(float, string)>();
                foreach (var sub in ParseEntries(e.Inner))
                {
                    if (sub.Key == "trigger" && sub.Inner != null) trigger = sub.Inner;
                    else if (sub.Value != null && float.TryParse(sub.Key, NumberStyles.Float, CultureInfo.InvariantCulture, out var w))
                        entries.Add((w, sub.Value.Trim().Trim('"')));
                }
                if (entries.Count > 0) list.Specials.Add((trigger, entries));
            }
            else if (e.Value != null && float.TryParse(e.Key, NumberStyles.Float, CultureInfo.InvariantCulture, out var w))
            {
                list.Base.Add((w, e.Value.Trim().Trim('"')));
            }
        }
        return list;
    }

    private static CoaTemplate? ParseTemplate(string block)
    {
        var t = new CoaTemplate();
        bool any = false;
        foreach (var e in ParseEntries(block))
        {
            switch (e.Key)
            {
                case "pattern": if (e.Value != null) { t.Pattern = e.Value.Trim().Trim('"'); any = true; } break;
                case "color1": case "color2": case "color3": case "color4":
                    if (e.Value != null)
                    {
                        int idx = e.Key[5] - '1';
                        t.Colors[idx] = e.Value.Trim().Trim('"');
                        any = true;
                    }
                    break;
                case "colored_emblem": if (e.Inner != null) { var em = ParseEmblem(e.Inner, false); if (em != null) { t.Emblems.Add(em); any = true; } } break;
                case "textured_emblem": if (e.Inner != null) { var em = ParseEmblem(e.Inner, true); if (em != null) { t.Emblems.Add(em); any = true; } } break;
                case "sub": if (e.Inner != null) { ParseSub(e.Inner, t); any = true; } break;
            }
        }
        return any ? t : null;
    }

    private static void ParseSub(string block, CoaTemplate parent)
    {
        var sub = new CoaTemplateSub();
        string? innerBlock = null;
        foreach (var e in ParseEntries(block))
        {
            if (e.Key == "instance" && e.Inner != null) sub.Transform = ParseInstance(e.Inner);
            else if (e.Inner != null) innerBlock = e.Inner;
        }
        var node = ParseTemplate(innerBlock ?? "");
        if (node == null) return;
        sub.Node = node;
        parent.Subs.Add(sub);
    }

    private static CoaTemplateEmblem? ParseEmblem(string block, bool textured)
    {
        var em = new CoaTemplateEmblem { Textured = textured };
        bool any = false;
        foreach (var e in ParseEntries(block))
        {
            switch (e.Key)
            {
                case "texture": if (e.Value != null) { em.Texture = e.Value.Trim().Trim('"'); any = true; } break;
                case "color1": if (e.Value != null) { em.C1 = e.Value.Trim().Trim('"'); any = true; } break;
                case "color2": if (e.Value != null) { em.C2 = e.Value.Trim().Trim('"'); any = true; } break;
                case "color3": if (e.Value != null) { em.C3 = e.Value.Trim().Trim('"'); any = true; } break;
                case "instance": if (e.Inner != null) { em.Instances.Add(ParseInstance(e.Inner)); any = true; } break;
            }
        }
        return any ? em : null;
    }

    private static CoaInstance ParseInstance(string block)
    {
        var inst = new CoaInstance { Sx = 1, Sy = 1 };
        foreach (var e in ParseEntries(block))
        {
            switch (e.Key)
            {
                case "position": if (e.Inner != null) { var n = Numbers(e.Inner); if (n.Count > 0) inst.X = n[0]; if (n.Count > 1) inst.Y = n[1]; } break;
                case "scale": if (e.Inner != null) { var n = Numbers(e.Inner); if (n.Count > 0) inst.Sx = n[0]; if (n.Count > 1) inst.Sy = n[1]; } break;
                case "rotation": if (e.Inner != null) { var n = Numbers(e.Inner); if (n.Count > 0) inst.Rotation = n[0]; } break;
                case "depth": if (e.Inner != null) { var n = Numbers(e.Inner); if (n.Count > 0) inst.Depth = n[0]; } break;
            }
        }
        return inst;
    }

    private static List<double> Numbers(string s)
    {
        var list = new List<double>();
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i <= s.Length; i++)
        {
            char ch = i < s.Length ? s[i] : ' ';
            if (char.IsDigit(ch) || ch == '.' || ch == '-' || ch == '+') sb.Append(ch);
            else if (sb.Length > 0)
            {
                if (double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) list.Add(v);
                sb.Clear();
            }
        }
        return list;
    }

    // ------------------------------------------------------- trigger engine

    private static List<string> ParseTokens(string block)
    {
        var list = new List<string>();
        int pos = PdxCoaColor2.SkipWs(block, 0, block.Length);
        while (true)
        {
            pos = PdxCoaColor2.SkipWs(block, pos, block.Length);
            if (pos >= block.Length) break;
            string tok = PdxCoaColor2.ReadKey(block, ref pos);
            if (tok.Length == 0) { pos = PdxCoaColor2.SkipLine(block, pos); continue; }
            list.Add(tok);
        }
        return list;
    }

    private static List<CoaEntry> ParseEntries(string block)
    {
        var list = new List<CoaEntry>();
        int pos = PdxCoaColor2.SkipWs(block, 0, block.Length);
        while (true)
        {
            pos = PdxCoaColor2.SkipWs(block, pos, block.Length);
            if (pos >= block.Length) break;
            string key = PdxCoaColor2.ReadKey(block, ref pos);
            if (key.Length == 0) break;
            pos = PdxCoaColor2.SkipWs(block, pos, block.Length);
            if (pos >= block.Length || block[pos] != '=') { pos = PdxCoaColor2.SkipLine(block, pos); continue; }
            pos++;
            pos = PdxCoaColor2.SkipWs(block, pos, block.Length);
            if (pos >= block.Length) break;
            var entry = new CoaEntry { Key = key };
            if (block[pos] == '{')
            {
                int end = PdxCoaColor2.FindMatchingBrace(block, pos);
                entry.Inner = block.Substring(pos + 1, end - pos - 1);
                pos = end + 1;
            }
            else
            {
                entry.Value = ReadValue(block, ref pos);
            }
            list.Add(entry);
        }
        return list;
    }

    /// <summary>Reads a multi-token value (e.g. `list "name"`) up to end of line / block / comment.</summary>
    private static string ReadValue(string s, ref int pos)
    {
        int start = pos;
        while (pos < s.Length)
        {
            char c = s[pos];
            if (c == '#') break;
            if (c == '\n' || c == '\r' || c == '}') break;
            pos++;
        }
        return s.Substring(start, pos - start).Trim();
    }

    private static bool EvalBlock(string block, CoaTriggerCtx ctx)
    {
        foreach (var e in ParseEntries(block))
            if (!EvalEntry(e, ctx)) return false;
        return true;
    }

    private static bool EvalEntry(CoaEntry e, CoaTriggerCtx ctx)
    {
        string k = e.Key;
        switch (k)
        {
            case "OR": return EvalAny(e.Inner, ctx);
            case "NOR": return !EvalAny(e.Inner, ctx);
            case "NOT": return e.Inner != null && !EvalBlock(e.Inner, ctx);
            case "scope:culture": case "scope:culture ?=": return e.Inner != null && EvalBlock(e.Inner, ctx);
            case "scope:faith": return e.Inner != null && EvalBlock(e.Inner, ctx);
            case "scope:faith.religion": case "scope:faith.religion ?=": return EvalReligion(e, ctx);
            case "scope:religion": case "scope:religion ?=": return EvalReligion(e, ctx);
            case "scope:title": case "scope:title ?=": return false;
            case "scope:character": case "scope:character ?=": return false;
            case "save_temporary_scope_as": return true;
            case "has_coa_gfx": return e.Value != null && ctx.CoaGfx.Contains(e.Value);
            case "this": return e.Value != null && e.Value.StartsWith("religion:", StringComparison.OrdinalIgnoreCase) && ctx.Religion.Equals(e.Value.Substring(9), StringComparison.OrdinalIgnoreCase);
            case "is_in_family": return e.Value != null && ctx.Family.Equals(e.Value, StringComparison.OrdinalIgnoreCase);
            case "religion": return e.Value != null && e.Value.StartsWith("religion:", StringComparison.OrdinalIgnoreCase) && ctx.Religion.Equals(e.Value.Substring(9), StringComparison.OrdinalIgnoreCase);
            default:
                if (e.Value != null && (e.Value == "yes" || e.Value == "true"))
                    return ScriptedTriggers.TryGetValue(k, out var inner) && EvalBlock(inner, ctx);
                if (e.Value != null && (e.Value == "no" || e.Value == "false")) return true;
                return false;
        }
    }

    private static bool EvalAny(string? inner, CoaTriggerCtx ctx)
    {
        if (inner == null) return false;
        foreach (var e in ParseEntries(inner))
            if (EvalEntry(e, ctx)) return true;
        return false;
    }

    private static bool EvalReligion(CoaEntry e, CoaTriggerCtx ctx)
    {
        if (e.Value != null && e.Value.StartsWith("religion:", StringComparison.OrdinalIgnoreCase))
            return ctx.Religion.Equals(e.Value.Substring(9), StringComparison.OrdinalIgnoreCase);
        return e.Inner != null && EvalBlock(e.Inner, ctx);
    }

    // ------------------------------------------------------- resolution

    private static string Pick(CoaWeightedList list, CoaTriggerCtx ctx, string seedKey)
    {
        double total = 0;
        var pool = new List<(double, string)>();
        foreach (var b in list.Base) { total += b.Weight; pool.Add((b.Weight, b.Value)); }
        foreach (var s in list.Specials)
        {
            if (EvalBlock(s.Trigger, ctx))
            {
                foreach (var en in s.Entries) { total += en.Weight; pool.Add((en.Weight, en.Value)); }
            }
        }
        if (pool.Count == 0) return "";
        ulong h = Hash(seedKey);
        double r = (double)(h % 1000000000UL) / 1000000000.0 * total;
        foreach (var p in pool) { r -= p.Item1; if (r <= 0) return p.Item2; }
        return pool[pool.Count - 1].Item2;
    }

    private static ulong Hash(string s)
    {
        ulong h = 14695981039346656037UL;
        foreach (char c in s) { h ^= c; h *= 1099511628211UL; }
        return h;
    }

    private static string CultureToReligion(CoaCultureDef c)
    {
        if (c.CoaGfx.Count == 0) return "christianity_religion";
        if (c.CoaGfx.Contains("west_african_group_coa_gfx") || c.CoaGfx.Contains("central_african_group_coa_gfx") || c.CoaGfx.Contains("east_african_coa_gfx"))
            return "akom_religion";
        if (c.CoaGfx.Contains("norse_coa_gfx")) return "germanic_religion";
        if (c.CoaGfx.Contains("baltic_group_coa_gfx")) return "baltic_religion";
        if (c.CoaGfx.Contains("slavic_coa_gfx") || c.CoaGfx.Contains("polish_coa_gfx")) return "slavic_religion";
        if (c.CoaGfx.Contains("arabic_group_coa_gfx") || c.CoaGfx.Contains("persian_coa_gfx") || c.CoaGfx.Contains("turkic_group_coa_gfx") || c.CoaGfx.Contains("berber_group_coa_gfx"))
            return "islam_religion";
        if (c.CoaGfx.Contains("mongol_coa_gfx") || c.CoaGfx.Contains("steppe_coa_gfx")) return "tengrism_religion";
        if (c.CoaGfx.Contains("indo_aryan_group_coa_gfx") || c.CoaGfx.Contains("indian_group_coa_gfx")) return "hinduism_religion";
        if (c.CoaGfx.Contains("chinese_group_coa_gfx")) return "buddhism_religion";
        return "christianity_religion";
    }

    private static List<PdxCoaLayer> ResolveTemplate(CoaTemplate t, CoaTriggerCtx ctx, string cultureId)
    {
        var layers = new List<PdxCoaLayer>();
        var colors = new uint[4];
        bool[] has = new bool[4];
        for (int i = 0; i < 4; i++)
        {
            if (string.IsNullOrEmpty(t.Colors[i])) continue;
            colors[i] = ResolveColorIndex(t, i, ctx, cultureId, new bool[4]);
            has[i] = true;
        }
        uint c1 = colors[0], c2 = has[1] ? colors[1] : c1, c3 = has[2] ? colors[2] : c2;

        string? pattern = ResolveTexture(t.Pattern, ctx, cultureId, true);
        if (pattern != null)
            layers.Add(new PdxCoaLayer
            {
                PatternTexture = pattern,
                HasColor = true, C1 = c1, C2 = c2, C3 = c3,
                Instance = new CoaInstance { X = 0.5, Y = 0.5, Sx = 1, Sy = 1 },
            });

        AddEmblems(t.Emblems, colors, has, ctx, cultureId, layers);

        var ordered = layers
            .Select((l, i) => (l, i))
            .OrderBy(x => x.l.Instance == null ? 0 : x.l.Instance.Depth)
            .ThenBy(x => x.i)
            .Select(x => x.l)
            .ToList();
        return ordered;
    }

    private static void AddEmblems(List<CoaTemplateEmblem> emblems, uint[] colors, bool[] has, CoaTriggerCtx ctx, string cultureId, List<PdxCoaLayer> layers)
    {
        foreach (var em in emblems)
        {
            string? tex = ResolveTexture(em.Texture, ctx, cultureId, em.Textured);
            if (tex == null) continue;
            uint ec1 = em.C1 != null ? ResolveEmblemColor(em.C1, colors, ctx, cultureId) : colors[0];
            uint ec2 = em.C2 != null ? ResolveEmblemColor(em.C2, colors, ctx, cultureId) : (has[1] ? colors[1] : ec1);
            uint ec3 = em.C3 != null ? ResolveEmblemColor(em.C3, colors, ctx, cultureId) : (has[2] ? colors[2] : ec2);
            foreach (var inst in em.Instances)
                layers.Add(new PdxCoaLayer
                {
                    EmblemTexture = tex,
                    Textured = em.Textured,
                    HasColor = true, C1 = ec1, C2 = ec2, C3 = ec3,
                    Instance = new CoaInstance
                    {
                        X = inst.X, Y = inst.Y, Sx = inst.Sx, Sy = inst.Sy,
                        Rotation = inst.Rotation, Depth = inst.Depth,
                    },
                });
        }
    }

    private static uint ResolveEmblemColor(string spec, uint[] colors, CoaTriggerCtx ctx, string cultureId)
    {
        string s = spec.Trim();
        if (s.StartsWith("color", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(s.Substring(5), out int refIdx) && refIdx >= 1 && refIdx <= 4)
            return colors[refIdx - 1];
        return ResolveColor(s, ctx, cultureId);
    }

    private static uint ResolveColorIndex(CoaTemplate t, int idx, CoaTriggerCtx ctx, string cultureId, bool[] visiting)
    {
        if (visiting[idx]) return 0xFF000000;
        string spec = t.Colors[idx] ?? "";
        if (spec.StartsWith("color", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(spec.Substring(5), out int refIdx) && refIdx >= 1 && refIdx <= 4)
        {
            if (string.IsNullOrEmpty(t.Colors[refIdx - 1])) return 0xFF000000;
            visiting[idx] = true;
            return ResolveColorIndex(t, refIdx - 1, ctx, cultureId, visiting);
        }
        return ResolveColor(spec, ctx, cultureId);
    }

    private static uint ResolveColor(string spec, CoaTriggerCtx ctx, string cultureId)
    {
        string s = spec.Trim();
        if (s.StartsWith("list", StringComparison.OrdinalIgnoreCase))
        {
            string listName = s.Length > 5 ? s.Substring(5).Trim().Trim('"') : "";
            if (listName.Length == 0) return 0xFF000000;
            if (ColorLists.TryGetValue(listName, out var list))
            {
                string picked = Pick(list, ctx, cultureId + "|c:" + listName);
                return PdxCoaColor.Parse(picked);
            }
            return 0xFF000000;
        }
        return PdxCoaColor.Parse(s);
    }

    private static string? ResolveTexture(string? spec, CoaTriggerCtx ctx, string cultureId, bool textured)
    {
        if (string.IsNullOrWhiteSpace(spec)) return null;
        string s = spec.Trim();
        if (s.StartsWith("list", StringComparison.OrdinalIgnoreCase))
        {
            string listName = s.Length > 5 ? s.Substring(5).Trim().Trim('"') : "";
            if (listName.Length == 0) return null;
            var lists = textured ? TexturedEmblemLists : EmblemLists;
            if (!lists.TryGetValue(listName, out var list)) return null;
            return Pick(list, ctx, cultureId + "|t:" + listName);
        }
        return s;
    }

    private static List<PdxCoaLayer> FallbackLayers(CoaTriggerCtx ctx, string cultureId)
    {
        var layers = new List<PdxCoaLayer>();
        uint baseColor = ColorLists.TryGetValue("normal_colors", out var cl)
            ? PdxCoaColor.Parse(Pick(cl, ctx, cultureId + "|c:normal_colors"))
            : 0xFF2244AA;
        uint metal = ColorLists.TryGetValue("metal_colors", out var ml)
            ? PdxCoaColor.Parse(Pick(ml, ctx, cultureId + "|c:metal_colors"))
            : 0xFFCCCCCC;
        layers.Add(new PdxCoaLayer
        {
            PatternTexture = "pattern_solid.dds",
            HasColor = true, C1 = baseColor, C2 = baseColor, C3 = baseColor,
            Instance = new CoaInstance { X = 0.5, Y = 0.5, Sx = 1, Sy = 1 },
        });
        string? charge = EmblemLists.TryGetValue("charge", out var el) ? Pick(el, ctx, cultureId + "|e:charge") : null;
        if (charge != null)
            layers.Add(new PdxCoaLayer
            {
                EmblemTexture = charge,
                HasColor = true, C1 = metal, C2 = metal, C3 = metal,
                Instance = new CoaInstance { X = 0.5, Y = 0.5, Sx = 0.5, Sy = 0.5 },
            });
        return layers;
    }
}
