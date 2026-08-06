using System;
using System.Collections.Generic;
using System.IO;

namespace PdxModIDE.ModelEngine;

public sealed class BuildingAssetDef
{
    public string Type { get; set; } = "";
    public List<string> Names { get; } = new();
    public List<string> GraphicalCultures { get; } = new();
}

public sealed class BuildingAssetDatabase
{
    private readonly Dictionary<string, List<string>> _byKey;

    public BuildingAssetDatabase()
    {
        _byKey = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, List<string>> ByKey => _byKey;

    public List<string> ResolveMeshes(string buildingKey)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(buildingKey)) return result;
        if (!_byKey.TryGetValue(buildingKey, out var list) || list == null) return result;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in list)
            if (!string.IsNullOrEmpty(m) && seen.Add(m))
                result.Add(m);
        result.Sort(StringComparer.OrdinalIgnoreCase);
        return result;
    }

    public static BuildingAssetDatabase Load(string gameRoot)
    {
        var db = new BuildingAssetDatabase();
        string dir = Path.Combine(gameRoot, "common", "buildings");
        if (!Directory.Exists(dir)) return db;

        foreach (var file in Directory.EnumerateFiles(dir, "*.txt", SearchOption.AllDirectories))
        {
            string content;
            try { content = File.ReadAllText(file); }
            catch { continue; }

            foreach (var asset in ParseAssets(content))
            {
                if (asset.Names.Count == 0) continue;
                foreach (var key in asset.GraphicalCultures)
                {
                    if (string.IsNullOrEmpty(key)) continue;
                    if (!db._byKey.TryGetValue(key, out var list))
                        db._byKey[key] = list = new List<string>();
                    foreach (var n in asset.Names)
                        if (!string.IsNullOrEmpty(n)) list.Add(n);
                }
            }
        }
        return db;
    }

    private sealed class Node
    {
        public string Key = "";
        public string? Value;
        public List<Node> Children = new();
        public List<string> Tokens = new();
    }

    private static List<BuildingAssetDef> ParseAssets(string content)
    {
        var result = new List<BuildingAssetDef>();
        int pos = 0;
        var root = new Node();
        ParseContent(content, ref pos, root);

        foreach (var assetsNode in FindBlocks(root, "assets"))
        {
            foreach (var assetNode in assetsNode.Children)
            {
                if (assetNode.Key != "asset") continue;

                var def = new BuildingAssetDef();
                foreach (var p in assetNode.Children)
                {
                    if (p.Key == "type" && p.Value != null)
                        def.Type = p.Value;
                    else if (p.Key == "name" && p.Value != null)
                        def.Names.Add(p.Value);
                    else if (p.Key == "names")
                    {
                        foreach (var t in p.Tokens)
                            def.Names.Add(t);
                    }
                    else if (p.Key == "graphical_cultures")
                    {
                        foreach (var t in p.Tokens)
                        {
                            if (t.StartsWith("#", StringComparison.Ordinal)) continue;
                            def.GraphicalCultures.Add(t);
                        }
                    }
                }

                if (def.GraphicalCultures.Count > 0)
                    result.Add(def);
            }
        }
        return result;
    }

    private static IEnumerable<Node> FindBlocks(Node node, string key)
    {
        foreach (var c in node.Children)
        {
            if (c.Key == key && c.Children.Count > 0)
                yield return c;
            foreach (var nested in FindBlocks(c, key))
                yield return nested;
        }
    }

    private static void ParseContent(string text, ref int pos, Node parent)
    {
        while (true)
        {
            SkipWs(text, ref pos);
            if (pos >= text.Length) break;

            string key = ReadKey(text, ref pos);
            if (string.IsNullOrEmpty(key)) break;

            SkipWs(text, ref pos);
            if (pos >= text.Length) break;

            if (text[pos] != '=')
            {
                parent.Tokens.Add(Unquote(key));
                continue;
            }

            pos++;
            SkipWs(text, ref pos);
            if (pos >= text.Length) break;

            var child = new Node { Key = key };
            if (text[pos] == '{')
            {
                string blockText = ReadBlock(text, ref pos);
                int p2 = 0;
                ParseContent(blockText, ref p2, child);
            }
            else
            {
                child.Value = Unquote(ReadScalar(text, ref pos));
            }
            parent.Children.Add(child);
        }
    }

    private static string ReadKey(string text, ref int pos)
    {
        int start = pos;
        while (pos < text.Length && !char.IsWhiteSpace(text[pos])
            && text[pos] != '=' && text[pos] != '{' && text[pos] != '}'
            && text[pos] != '#' && text[pos] != '"')
            pos++;
        return text.Substring(start, pos - start);
    }

    private static string ReadScalar(string text, ref int pos)
    {
        SkipWs(text, ref pos);
        if (pos >= text.Length) return "";
        string result;
        if (text[pos] == '"')
        {
            pos++;
            int start = pos;
            while (pos < text.Length && text[pos] != '"') pos++;
            result = text.Substring(start, pos - start);
            if (pos < text.Length) pos++;
        }
        else
        {
            int s2 = pos;
            while (pos < text.Length && !char.IsWhiteSpace(text[pos])
                && text[pos] != '{' && text[pos] != '}' && text[pos] != '#' && text[pos] != '=')
                pos++;
            result = text.Substring(s2, pos - s2);
        }
        return result;
    }

    private static string ReadBlock(string text, ref int pos)
    {
        int start = pos;
        int depth = 0;
        bool inString = false;
        while (pos < text.Length)
        {
            char c = text[pos];
            if (inString)
            {
                if (c == '"') inString = false;
            }
            else if (c == '"')
                inString = true;
            else if (c == '{')
                depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                {
                    pos++;
                    return text.Substring(start + 1, pos - start - 2);
                }
            }
            else if (c == '#')
            {
                while (pos < text.Length && text[pos] != '\n') pos++;
                continue;
            }
            pos++;
        }
        return text.Substring(start + 1);
    }

    private static void SkipWs(string text, ref int pos)
    {
        while (pos < text.Length)
        {
            if (char.IsWhiteSpace(text[pos]))
            {
                pos++;
            }
            else if (text[pos] == '#')
            {
                while (pos < text.Length && text[pos] != '\n') pos++;
            }
            else
            {
                break;
            }
        }
    }

    private static string Unquote(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
            return value.Substring(1, value.Length - 2);
        return value;
    }
}
