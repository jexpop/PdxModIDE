using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PdxModIDE.ModelEngine;

public sealed class PdxAssetModel
{
    public string EntityName { get; set; } = "";
    public string MeshName { get; set; } = "";
    public string MeshFile { get; set; } = "";
    public string DiffuseTexture { get; set; } = "";
    public string NormalTexture { get; set; } = "";
    public string SpecularTexture { get; set; } = "";
    public string SourceFile { get; set; } = "";
}

public sealed class ResolvedAsset
{
    public string MeshPath { get; set; } = "";
    public string? DiffusePath { get; set; }
    public string? NormalPath { get; set; }
    public string? SpecularPath { get; set; }
}

public sealed class PdxAssetResolver
{
    private readonly string _modelsRoot;
    private readonly Dictionary<string, PdxAssetModel> _entities;
    private readonly List<string> _meshPaths;
    private readonly List<string> _meshBaseNames;
    private readonly Dictionary<string, List<string>> _meshTokens;

    public PdxAssetResolver(string modelsRoot)
    {
        _modelsRoot = modelsRoot;
        _entities = new Dictionary<string, PdxAssetModel>(StringComparer.OrdinalIgnoreCase);
        _meshPaths = new List<string>();
        _meshBaseNames = new List<string>();
        _meshTokens = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(modelsRoot)) return;

        foreach (var file in Directory.EnumerateFiles(modelsRoot, "*.asset", SearchOption.AllDirectories))
        {
            string content;
            try { content = File.ReadAllText(file); }
            catch { continue; }
            PdxAssetParser.ParseAssetContent(content, file, _entities);
        }

        foreach (var mesh in Directory.EnumerateFiles(modelsRoot, "*.mesh", SearchOption.AllDirectories))
        {
            string baseName = Path.GetFileNameWithoutExtension(mesh);
            _meshPaths.Add(mesh);
            _meshBaseNames.Add(baseName);
            _meshTokens[baseName] = Tokenize(baseName);
        }
    }

    public int EntityCount => _entities.Count;
    public int MeshCount => _meshPaths.Count;

    public ResolvedAsset? Resolve(string gfxKey)
    {
        if (string.IsNullOrEmpty(gfxKey)) return null;

        if (_entities.TryGetValue(gfxKey, out var entity))
        {
            var resolved = BuildResolved(entity);
            if (resolved != null) return resolved;
        }

        string keyName = Path.GetFileName(gfxKey.Replace("\\", "/"));
        string keyBase = Path.GetFileNameWithoutExtension(keyName);

        int exact = _meshBaseNames.IndexOf(keyBase);
        if (exact >= 0)
            return new ResolvedAsset { MeshPath = _meshPaths[exact] };

        return ResolveByTokens(keyBase);
    }

    private ResolvedAsset? BuildResolved(PdxAssetModel entity)
    {
        string meshDir = Path.GetDirectoryName(entity.SourceFile) ?? _modelsRoot;
        string? meshPath = LocateFile(meshDir, entity.MeshFile);
        if (meshPath == null) return null;

        return new ResolvedAsset
        {
            MeshPath = meshPath,
            DiffusePath = ResolveTexture(meshDir, entity.DiffuseTexture),
            NormalPath = ResolveTexture(meshDir, entity.NormalTexture),
            SpecularPath = ResolveTexture(meshDir, entity.SpecularTexture)
        };
    }

    private string? ResolveTexture(string meshDir, string texture)
    {
        if (string.IsNullOrEmpty(texture)) return null;
        string path = Path.Combine(meshDir, Path.GetFileName(texture));
        return File.Exists(path) ? path : null;
    }

    private ResolvedAsset? ResolveByTokens(string keyBase)
    {
        var keyTokens = Tokenize(keyBase);
        if (keyTokens.Count == 0) return null;

        keyTokens.RemoveAll(t => t == "gfx" || t == "group");

        if (keyTokens.Count == 0) return null;

        int bestScore = 0;
        string bestBase = "";
        foreach (var entry in _meshTokens)
        {
            int score = 0;
            foreach (var kt in keyTokens)
            {
                if (entry.Value.Contains(kt))
                    score++;
            }
            if (score > bestScore)
            {
                bestScore = score;
                bestBase = entry.Key;
            }
        }

        if (bestScore > 0)
        {
            int idx = _meshBaseNames.IndexOf(bestBase);
            if (idx >= 0)
                return new ResolvedAsset { MeshPath = _meshPaths[idx] };
        }
        return null;
    }

    private static string? LocateFile(string meshDir, string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return null;
        string name = Path.GetFileName(fileName);
        string candidate = Path.Combine(meshDir, name);
        if (File.Exists(candidate)) return candidate;
        return null;
    }

    private static List<string> Tokenize(string s)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        foreach (char c in s)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(char.ToLowerInvariant(c));
            }
            else if (sb.Length > 0)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
        }
        if (sb.Length > 0) result.Add(sb.ToString());
        return result;
    }
}

public static class PdxAssetParser
{
    private sealed class CNode
    {
        public string Key = "";
        public string? Value;
        public List<CNode>? Children;
    }

    public static void ParseAssetContent(string content, string sourceFile, Dictionary<string, PdxAssetModel> registry)
    {
        int pos = 0;
        var pdxmeshByName = new Dictionary<string, PdxMeshAssetInfo>(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            SkipWs(content, ref pos);
            if (pos >= content.Length) break;

            string key = ReadKey(content, ref pos);
            if (string.IsNullOrEmpty(key)) break;

            SkipWs(content, ref pos);
            if (pos >= content.Length || content[pos] != '=')
            {
                SkipToNextStatement(content, ref pos);
                continue;
            }
            pos++;

            SkipWs(content, ref pos);
            if (pos >= content.Length) break;

            if (content[pos] == '{')
            {
                string block = ReadBlock(content, ref pos);
                if (key == "pdxmesh")
                {
                    var info = ParsePdxMeshBlock(block);
                    if (!string.IsNullOrEmpty(info.MeshName))
                        pdxmeshByName[info.MeshName] = info;
                }
                else if (key == "entity")
                {
                    var entity = ParseEntityBlock(block, sourceFile);
                    if (!string.IsNullOrEmpty(entity.EntityName))
                    {
                        if (!string.IsNullOrEmpty(entity.MeshName) && pdxmeshByName.TryGetValue(entity.MeshName, out var info))
                        {
                            entity.MeshFile = info.MeshFile;
                            entity.DiffuseTexture = info.DiffuseTexture;
                            entity.NormalTexture = info.NormalTexture;
                            entity.SpecularTexture = info.SpecularTexture;
                        }
                        registry[entity.EntityName] = entity;
                    }
                }
            }
            else
            {
                SkipScalar(content, ref pos);
            }
        }
    }

    private static PdxMeshAssetInfo ParsePdxMeshBlock(string block)
    {
        var nodes = ParseNodes(block);
        var info = new PdxMeshAssetInfo();
        foreach (var n in nodes)
        {
            if (n.Key == "name" && n.Value != null) info.MeshName = Unquote(n.Value);
            else if (n.Key == "file" && n.Value != null) info.MeshFile = Unquote(n.Value);
            else if (n.Key == "meshsettings" && n.Children != null)
            {
                foreach (var s in n.Children)
                {
                    if (s.Value == null) continue;
                    if (s.Key == "texture_diffuse") info.DiffuseTexture = Unquote(s.Value);
                    else if (s.Key == "texture_normal") info.NormalTexture = Unquote(s.Value);
                    else if (s.Key == "texture_specular") info.SpecularTexture = Unquote(s.Value);
                }
            }
        }
        return info;
    }

    private static PdxAssetModel ParseEntityBlock(string block, string sourceFile)
    {
        var nodes = ParseNodes(block);
        var entity = new PdxAssetModel { SourceFile = sourceFile };
        foreach (var n in nodes)
        {
            if (n.Key == "name" && n.Value != null) entity.EntityName = Unquote(n.Value);
            else if (n.Key == "pdxmesh" && n.Value != null) entity.MeshName = Unquote(n.Value);
        }
        return entity;
    }

    private static List<CNode> ParseNodes(string text)
    {
        var result = new List<CNode>();
        int pos = 0;
        while (true)
        {
            SkipWs(text, ref pos);
            if (pos >= text.Length) break;

            string key = ReadKey(text, ref pos);
            if (string.IsNullOrEmpty(key)) break;

            SkipWs(text, ref pos);
            if (pos >= text.Length || text[pos] != '=')
            {
                SkipToNextStatement(text, ref pos);
                continue;
            }
            pos++;
            SkipWs(text, ref pos);
            if (pos >= text.Length) break;

            var node = new CNode { Key = key };
            if (text[pos] == '{')
            {
                string block = ReadBlock(text, ref pos);
                node.Children = ParseNodes(block);
            }
            else
            {
                node.Value = ReadScalar(text, ref pos);
            }
            result.Add(node);
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
            else
            {
                if (c == '"') inString = true;
                else if (c == '{') depth++;
                else if (c == '}') { depth--; if (depth == 0) { pos++; return text.Substring(start, pos - start); } }
                else if (c == '#')
                {
                    while (pos < text.Length && text[pos] != '\n') pos++;
                    continue;
                }
            }
            pos++;
        }
        return text.Substring(start);
    }

    private static string ReadScalar(string text, ref int pos)
    {
        SkipWs(text, ref pos);
        if (pos >= text.Length) return "";
        if (text[pos] == '"')
        {
            int start = pos;
            pos++;
            while (pos < text.Length && text[pos] != '"') pos++;
            if (pos < text.Length) pos++;
            return text.Substring(start, pos - start);
        }
        int s2 = pos;
        while (pos < text.Length && !char.IsWhiteSpace(text[pos]) && text[pos] != '#' && text[pos] != '{' && text[pos] != '}')
            pos++;
        return text.Substring(s2, pos - s2);
    }

    private static void SkipScalar(string text, ref int pos)
    {
        ReadScalar(text, ref pos);
    }

    private static void SkipToNextStatement(string text, ref int pos)
    {
        while (pos < text.Length && text[pos] != '\n') pos++;
    }

    private static string ReadKey(string text, ref int pos)
    {
        int start = pos;
        while (pos < text.Length && !char.IsWhiteSpace(text[pos]) && text[pos] != '=' && text[pos] != '{' && text[pos] != '}' && text[pos] != '#')
            pos++;
        return text.Substring(start, pos - start);
    }

    private static void SkipWs(string text, ref int pos)
    {
        while (pos < text.Length)
        {
            char c = text[pos];
            if (char.IsWhiteSpace(c))
            {
                pos++;
            }
            else if (c == '#')
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
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            return value.Substring(1, value.Length - 2);
        return value;
    }

    private sealed class PdxMeshAssetInfo
    {
        public string MeshName { get; set; } = "";
        public string MeshFile { get; set; } = "";
        public string DiffuseTexture { get; set; } = "";
        public string NormalTexture { get; set; } = "";
        public string SpecularTexture { get; set; } = "";
    }
}
