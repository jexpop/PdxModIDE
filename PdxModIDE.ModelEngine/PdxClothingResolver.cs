using System;
using System.Collections.Generic;
using System.IO;

namespace PdxModIDE.ModelEngine;

public sealed class ClothingResolvedMesh
{
    public string Name { get; set; } = "";
    public string MeshPath { get; set; } = "";
    public string? DiffusePath { get; set; }
    public string? AssetPath { get; set; }
}

/// <summary>
/// Resolves clothing meshes following the CK3 chain:
/// culture clothing_gfx group -> portrait_modifiers template -> genes accessory
/// -> accessories entity -> .asset -> .mesh + diffuse.
/// </summary>
public sealed class PdxClothingResolver
{
    private readonly string _gameRoot;

    private readonly Dictionary<string, List<string>> _templatesPerGroup;
    private readonly Dictionary<string, List<string>> _accessoriesPerTemplate;
    private readonly Dictionary<string, string> _entityPerAccessory;
    private readonly Dictionary<string, string> _filePerEntity;
    private readonly Dictionary<string, string> _diffusePerEntity;

    public PdxClothingResolver(string gameRoot)
    {
        _gameRoot = gameRoot;
        _templatesPerGroup = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        _accessoriesPerTemplate = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        _entityPerAccessory = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _filePerEntity = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _diffusePerEntity = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(gameRoot) || !Directory.Exists(gameRoot)) return;
        LoadPortraitModifiers();
        LoadGenes();
        LoadAccessories();
        LoadAssetEntities();
    }

    // ---------- Public ----------

    public List<ClothingResolvedMesh> ResolveClothing(List<string> clothingGfxGroups)
    {
        var result = new List<ClothingResolvedMesh>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (clothingGfxGroups == null) return result;

        foreach (var group in clothingGfxGroups)
        {
            foreach (var template in GetTemplates(group))
            {
                foreach (var accessory in GetAccessories(template))
                {
                    if (!_entityPerAccessory.TryGetValue(accessory, out var entity)) continue;
                    if (!_filePerEntity.TryGetValue(entity, out var assetPath)) continue;
                    string? meshPath = ResolveMeshFile(assetPath);
                    if (string.IsNullOrEmpty(meshPath) || !File.Exists(meshPath)) continue;
                    if (!seen.Add(meshPath)) continue;

                    string? diffuse = null;
                    if (_diffusePerEntity.TryGetValue(entity, out var dp))
                        diffuse = dp;
                    else
                    {
                        string baseName = Path.GetFileNameWithoutExtension(meshPath);
                        string convention = Path.Combine(Path.GetDirectoryName(meshPath) ?? "", baseName + "_diffuse.dds");
                        if (File.Exists(convention)) diffuse = convention;
                    }

                    result.Add(new ClothingResolvedMesh
                    {
                        Name = accessory,
                        MeshPath = meshPath,
                        DiffusePath = diffuse,
                        AssetPath = assetPath
                    });
                }
            }
        }
        return result;
    }

    // ---------- Loaders ----------

    private void LoadPortraitModifiers()
    {
        string dir = Path.Combine(_gameRoot, "gfx", "portraits", "portrait_modifiers");
        if (!Directory.Exists(dir)) return;

        foreach (var file in Directory.EnumerateFiles(dir, "*clothes*.txt", SearchOption.TopDirectoryOnly))
        {
            string content;
            try { content = File.ReadAllText(file); } catch { continue; }

            foreach (var (_, body) in ParseBlocks(content))
            {
                var templates = new List<string>();
                CollectTemplates(body, templates);
                if (templates.Count == 0) continue;

                var groups = new List<string>();
                CollectCultureGroups(body, groups);
                if (groups.Count == 0) continue;

                foreach (var g in groups)
                {
                    if (string.IsNullOrEmpty(g)) continue;
                    if (!_templatesPerGroup.TryGetValue(g, out var list))
                        _templatesPerGroup[g] = list = new List<string>();
                    foreach (var t in templates)
                        if (!list.Contains(t)) list.Add(t);
                }
            }
        }
    }

    private void LoadGenes()
    {
        string dir = Path.Combine(_gameRoot, "common", "genes");
        if (!Directory.Exists(dir)) return;

        foreach (var file in Directory.EnumerateFiles(dir, "*.txt", SearchOption.TopDirectoryOnly))
        {
            string content;
            try { content = File.ReadAllText(file); } catch { continue; }

            foreach (var (_, body) in ParseBlocks(content))
            {
                // special_genes { accessory_genes { clothes { template { male/female { .. } } } } }
                var clothesBlock = FindChild(body, "accessory_genes");
                if (clothesBlock == null) continue;
                foreach (var (subName, subBody) in FindChildChain(clothesBlock, "clothes"))
                {
                    foreach (var (tmplName, tmplBody) in subBody.Children)
                    {
                        var acc = new List<string>();
                        CollectAccessoryKeys(tmplBody, acc);
                        if (acc.Count == 0) continue;
                        if (!_accessoriesPerTemplate.ContainsKey(tmplName))
                            _accessoriesPerTemplate[tmplName] = acc;
                    }
                }
            }
        }
    }

    private void LoadAccessories()
    {
        string dir = Path.Combine(_gameRoot, "gfx", "portraits", "accessories");
        if (!Directory.Exists(dir)) return;

        foreach (var file in Directory.EnumerateFiles(dir, "*.txt", SearchOption.TopDirectoryOnly))
        {
            string content;
            try { content = File.ReadAllText(file); } catch { continue; }

            foreach (var (name, body) in ParseBlocks(content))
            {
                string entity = FindEntity(body);
                if (string.IsNullOrEmpty(entity)) continue;
                if (!_entityPerAccessory.ContainsKey(name))
                    _entityPerAccessory[name] = entity;
            }
        }
    }

    private void LoadAssetEntities()
    {
        string dir = Path.Combine(_gameRoot, "gfx", "models", "portraits");
        if (!Directory.Exists(dir)) return;

        foreach (var file in Directory.EnumerateFiles(dir, "*.asset", SearchOption.AllDirectories))
        {
            string content;
            try { content = File.ReadAllText(file); } catch { continue; }

            string? entityName = ExtractEntityName(content);
            if (entityName == null) continue;
            if (!_filePerEntity.ContainsKey(entityName))
                _filePerEntity[entityName] = file;

            if (!_diffusePerEntity.ContainsKey(entityName))
            {
                string? d = ExtractDiffusePath(content, Path.GetDirectoryName(file));
                if (d != null) _diffusePerEntity[entityName] = d;
            }
        }
    }

    // ---------- Parser ----------

    private IEnumerable<(string Name, BlockValue Body)> ParseBlocks(string content)
    {
        int pos = 0;
        while (pos < content.Length)
        {
            SkipWs(content, ref pos);
            if (pos >= content.Length) break;
            string key = ReadToken(content, ref pos);
            if (string.IsNullOrEmpty(key)) break;
            if (!SkipWss(content, ref pos)) break;
            if (pos >= content.Length || content[pos] != '{')
            {
                SkipLine(content, ref pos);
                continue;
            }
            var body = ParseBody(content, ref pos);
            yield return (key, body);
        }
    }

    private BlockValue ParseBody(string content, ref int pos)
    {
        pos++; // consume '{'
        var body = new BlockValue();
        while (pos < content.Length)
        {
            SkipWs(content, ref pos);
            if (pos >= content.Length) break;
            if (content[pos] == '}') { pos++; break; }
            string key = ReadToken(content, ref pos);
            if (string.IsNullOrEmpty(key)) break;
            if (!SkipWss(content, ref pos)) break;
            if (pos >= content.Length) break;
            if (content[pos] == '{')
            {
                var child = ParseBody(content, ref pos);
                if (!body.Children.ContainsKey(key))
                    body.Children[key] = child;
            }
            else
            {
                string value = ReadValue(content, ref pos);
                body.Values.Add((key, value));
            }
        }
        return body;
    }

    private void SkipLine(string content, ref int pos)
    {
        while (pos < content.Length && content[pos] != '\n') pos++;
    }

    private bool SkipWss(string content, ref int pos)
    {
        SkipWs(content, ref pos);
        if (pos < content.Length && content[pos] == '=')
        {
            pos++;
            SkipWs(content, ref pos);
        }
        return pos < content.Length;
    }

    private void SkipWs(string content, ref int pos)
    {
        while (pos < content.Length)
        {
            char c = content[pos];
            if (c == '#')
            {
                while (pos < content.Length && content[pos] != '\n') pos++;
                continue;
            }
            if (c == '\n' || c == '\r' || c == ' ' || c == '\t' || c == '\uFEFF')
            {
                pos++;
                continue;
            }
            break;
        }
    }

    private string ReadToken(string content, ref int pos)
    {
        int start = pos;
        while (pos < content.Length)
        {
            char c = content[pos];
            if (c == ' ' || c == '\t' || c == '\n' || c == '\r' ||
                c == '{' || c == '}' || c == '=' || c == '#' || c == '\uFEFF')
                break;
            pos++;
        }
        return content.Substring(start, pos - start);
    }

    private string ReadValue(string content, ref int pos)
    {
        if (pos < content.Length && content[pos] == '"')
        {
            int start = pos;
            pos++;
            while (pos < content.Length && content[pos] != '"') pos++;
            if (pos < content.Length) pos++;
            return content.Substring(start, pos - start).Trim();
        }

        int s0 = pos;
        while (pos < content.Length)
        {
            char c = content[pos];
            if (c == ' ' || c == '\t' || c == '\n' || c == '\r' ||
                c == '{' || c == '}' || c == '#' || c == '\uFEFF')
                break;
            pos++;
        }
        return content.Substring(s0, pos - s0);
    }

    // ---------- Traversal helpers ----------

    private static BlockValue? FindChild(BlockValue body, string key)
    {
        foreach (var (k, v) in body.Children)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(k, key)) return v;
        }
        return null;
    }

    private static IEnumerable<(string Name, BlockValue Body)> FindChildChain(BlockValue root, string key)
    {
        foreach (var (k, v) in root.Children)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(k, key))
                yield return (k, v);
        }
    }

    private static void CollectTemplates(BlockValue body, List<string> outTemplates)
    {
        foreach (var (k, v) in body.Values)
        {
            if (k == "template" && !string.IsNullOrEmpty(v) && !outTemplates.Contains(v))
                outTemplates.Add(v);
        }
        foreach (var child in body.Children.Values)
            CollectTemplates(child, outTemplates);
    }

    private static void CollectCultureGroups(BlockValue body, List<string> outGroups)
    {
        foreach (var (k, v) in body.Values)
        {
            if (string.IsNullOrEmpty(v)) continue;
            if ((k == "CULTURE_INPUT" || k == "has_clothing_gfx") && !outGroups.Contains(v))
                outGroups.Add(v);
        }
        foreach (var child in body.Children.Values)
            CollectCultureGroups(child, outGroups);
    }

    private static void CollectAccessoryKeys(BlockValue body, List<string> outKeys)
    {
        foreach (var (k, v) in body.Values)
        {
            if (string.IsNullOrEmpty(v)) continue;
            if (v.Contains("clothes", StringComparison.OrdinalIgnoreCase) && !outKeys.Contains(v))
                outKeys.Add(v);
        }
        foreach (var child in body.Children.Values)
            CollectAccessoryKeys(child, outKeys);
    }

    private static string FindEntity(BlockValue body)
    {
        foreach (var (k, v) in body.Children)
        {
            if (k != "entity") continue;
            foreach (var (ek, ev) in v.Values)
                if (ek == "entity") return ev;
        }
        return "";
    }

    private static string? ExtractEntityName(string content)
    {
        int idx = content.IndexOf("entity = {", StringComparison.Ordinal);
        if (idx < 0) return null;
        int nm = content.IndexOf("name =", idx, StringComparison.Ordinal);
        if (nm < 0) return null;
        nm += "name =".Length;
        while (nm < content.Length && (content[nm] == ' ' || content[nm] == '\t' || content[nm] == '"')) nm++;
        int end = nm;
        while (end < content.Length && content[end] != '"' && content[end] != '}') end++;
        string s = content.Substring(nm, end - nm).Trim();
        return s.Length > 0 ? s : null;
    }

    private static string? ExtractDiffusePath(string content, string? dir)
    {
        int idx = content.IndexOf("texture_diffuse", StringComparison.Ordinal);
        if (idx < 0 || dir == null) return null;
        int q1 = content.IndexOf('"', idx);
        if (q1 < 0) return null;
        int q2 = content.IndexOf('"', q1 + 1);
        if (q2 < 0) return null;
        string name = content.Substring(q1 + 1, q2 - q1 - 1);
        if (string.IsNullOrEmpty(name)) return null;
        string candidate = Path.Combine(dir, name);
        return File.Exists(candidate) ? candidate : null;
    }

    private string? ResolveMeshFile(string assetPath)
    {
        string? dir = Path.GetDirectoryName(assetPath);
        if (dir == null) return null;
        string baseName = Path.GetFileNameWithoutExtension(assetPath);

        string candidate = Path.Combine(dir, baseName + ".mesh");
        if (File.Exists(candidate)) return candidate;

        foreach (var mesh in Directory.EnumerateFiles(dir, "*.mesh", SearchOption.TopDirectoryOnly))
        {
            string bn = Path.GetFileNameWithoutExtension(mesh);
            if (bn.StartsWith(baseName, StringComparison.OrdinalIgnoreCase) &&
                !bn.Contains("_bs_", StringComparison.OrdinalIgnoreCase))
                return mesh;
        }
        return null;
    }

    private IEnumerable<string> GetTemplates(string group)
    {
        if (string.IsNullOrEmpty(group)) yield break;
        if (_templatesPerGroup.TryGetValue(group, out var t1))
            foreach (var t in t1) yield return t;

        string baseGroup = group.Replace("_clothing_gfx", "", StringComparison.OrdinalIgnoreCase);
        foreach (var kv in _templatesPerGroup)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(kv.Key, baseGroup))
                foreach (var t in kv.Value) yield return t;
        }
    }

    private IEnumerable<string> GetAccessories(string template)
    {
        if (_accessoriesPerTemplate.TryGetValue(template, out var acc))
            foreach (var a in acc) yield return a;
    }
}

internal sealed class BlockValue
{
    public List<(string Key, string Value)> Values { get; } = new();
    public Dictionary<string, BlockValue> Children { get; } = new(StringComparer.OrdinalIgnoreCase);
}
