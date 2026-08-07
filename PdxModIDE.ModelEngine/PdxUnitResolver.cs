using System;
using System.Collections.Generic;
using System.IO;

namespace PdxModIDE.ModelEngine;

public sealed class UnitResolvedMesh
{
    public string Name { get; set; } = "";
    public string MeshPath { get; set; } = "";
    public string? DiffusePath { get; set; }
    public string? AssetPath { get; set; }
    public string Type { get; set; } = "";
    public int Quality { get; set; }
}

/// <summary>
/// Resolves unit meshes following the CK3 chain:
/// culture unit_gfx tag -> graphical_unit_types group -> gfx/models/units/entity_links
/// block (type / graphical_cultures / quality / entity) -> .asset (pdxmesh + meshsettings)
/// -> .mesh + diffuse.
/// </summary>
public sealed class PdxUnitResolver
{
    private readonly string _gameRoot;

    // unit_gfx tag group name -> the unit_gfx tags it gathers (from graphical_unit_types).
    private readonly Dictionary<string, List<string>> _tagsPerGuiType;
    // entity_links stored per link.
    private readonly List<UnitLink> _links;
    private readonly Dictionary<string, string> _filePerEntity;
    private readonly Dictionary<string, string?> _diffusePerEntity;

    public PdxUnitResolver(string gameRoot)
    {
        _gameRoot = gameRoot;
        _tagsPerGuiType = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        _links = new List<UnitLink>();
        _filePerEntity = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _diffusePerEntity = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(gameRoot) || !Directory.Exists(gameRoot)) return;
        LoadGraphicalUnitTypes();
        LoadEntityLinks();
        LoadAssetEntities();
    }

    // ---------- Public ----------

    public List<UnitResolvedMesh> ResolveUnits(List<string> unitGfxTags)
    {
        var result = new List<UnitResolvedMesh>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (unitGfxTags == null) return result;

        foreach (var tag in unitGfxTags)
        {
            foreach (var link in _links)
            {
                if (!link.MatchesTag(tag)) continue;
                if (string.IsNullOrEmpty(link.Entity)) continue;
                if (!_filePerEntity.TryGetValue(link.Entity, out var assetPath)) continue;

                string? meshPath = ResolveMeshFile(assetPath);
                if (string.IsNullOrEmpty(meshPath) || !File.Exists(meshPath)) continue;
                if (!seen.Add(meshPath)) continue;

                string? diffuse = null;
                if (_diffusePerEntity.TryGetValue(link.Entity, out var dp) && !string.IsNullOrEmpty(dp))
                    diffuse = dp;
                else
                {
                    string baseName = Path.GetFileNameWithoutExtension(meshPath);
                    string convention = Path.Combine(Path.GetDirectoryName(meshPath) ?? "", baseName + "_diffuse.dds");
                    if (File.Exists(convention)) diffuse = convention;
                }

                result.Add(new UnitResolvedMesh
                {
                    Name = link.Name,
                    MeshPath = meshPath,
                    DiffusePath = diffuse,
                    AssetPath = assetPath,
                    Type = link.Type,
                    Quality = link.Quality
                });
            }
        }
        return result;
    }

    // ---------- Loaders ----------

    private void LoadGraphicalUnitTypes()
    {
        string dir = Path.Combine(_gameRoot, "common", "graphical_unit_types");
        if (!Directory.Exists(dir)) return;

        foreach (var file in Directory.EnumerateFiles(dir, "*.txt", SearchOption.TopDirectoryOnly))
        {
            string content;
            try { content = File.ReadAllText(file); } catch { continue; }

            foreach (var (name, body) in ParseBlocks(content))
            {
                var tags = new List<string>();
                CollectGraphicalCultures(body, tags);
                if (tags.Count == 0) continue;
                if (_tagsPerGuiType.ContainsKey(name)) continue;
                _tagsPerGuiType[name] = tags;
            }
        }
    }

    private void LoadEntityLinks()
    {
        string dir = Path.Combine(_gameRoot, "gfx", "models", "units", "entity_links");
        if (!Directory.Exists(dir)) return;

        foreach (var file in Directory.EnumerateFiles(dir, "*.txt", SearchOption.TopDirectoryOnly))
        {
            string content;
            try { content = File.ReadAllText(file); } catch { continue; }

            var macros = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            CollectMacros(content, macros);

            foreach (var (name, body) in ParseBlocks(content))
            {
                string type = GetValue(body, "type");
                string entity = GetValue(body, "entity");
                if (string.IsNullOrEmpty(entity)) continue;

                var inlineTags = new List<string>();
                CollectGraphicalCultures(body, inlineTags);

                int quality = ParseQuality(body, macros);

                // Expand any group tags (from graphical_unit_types) into concrete unit_gfx tags.
                var expanded = ExpandTags(inlineTags);

                _links.Add(new UnitLink
                {
                    Name = name,
                    Type = type,
                    GraphicalCultures = expanded,
                    Entity = entity,
                    Quality = quality
                });
            }
        }
    }

    private void LoadAssetEntities()
    {
        string dir = Path.Combine(_gameRoot, "gfx", "models", "units");
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
                _diffusePerEntity[entityName] = d;
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
        pos++;
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

    private static string GetValue(BlockValue body, string key)
    {
        foreach (var (k, v) in body.Values)
            if (StringComparer.OrdinalIgnoreCase.Equals(k, key)) return v;
        return "";
    }

    private void CollectMacros(string content, Dictionary<string, string> outMacros)
    {
        int pos = 0;
        while (pos < content.Length)
        {
            SkipWsTo(content, ref pos);
            if (pos >= content.Length || content[pos] != '@') break;
            int nameStart = pos;
            while (pos < content.Length && content[pos] != '=' && content[pos] != '\n' && content[pos] != '\r') pos++;
            string name = content.Substring(nameStart, pos - nameStart).Trim();
            if (name.Length <= 1) break;
            SkipWsTo(content, ref pos);
            if (pos >= content.Length || content[pos] != '=') break;
            pos++;
            SkipWs(content, ref pos);
            string value = ReadValue(content, ref pos);
            outMacros[name.Substring(1)] = value;
        }
    }

    private static void SkipWsTo(string content, ref int pos)
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

    private static void CollectGraphicalCultures(BlockValue body, List<string> outTags)
    {
        if (body.Children.TryGetValue("graphical_cultures", out var listBlock))
        {
            foreach (var (k, v) in listBlock.Values)
            {
                if (outTags.Contains(k)) continue;
                if (!string.IsNullOrEmpty(k)) outTags.Add(k);
            }
            foreach (var (ck, cv) in listBlock.Children)
            {
                if (outTags.Contains(ck)) continue;
                if (!string.IsNullOrEmpty(ck)) outTags.Add(ck);
            }
        }
        foreach (var (k, v) in body.Values)
        {
            if (k != "graphical_cultures") continue;
            if (string.IsNullOrEmpty(v) || outTags.Contains(v)) continue;
            outTags.Add(v);
        }
        foreach (var child in body.Children.Values)
        {
            if (body.Children.TryGetValue("graphical_cultures", out var gc) && ReferenceEquals(child, gc))
                continue;
            CollectGraphicalCultures(child, outTags);
        }
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
            if (bn.StartsWith(baseName, StringComparison.OrdinalIgnoreCase))
                return mesh;
        }
        return null;
    }

    private List<string> ExpandTags(List<string> inlineTags)
    {
        var tags = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string t)
        {
            if (string.IsNullOrEmpty(t) || !seen.Add(t)) return;
            tags.Add(t);
            // If this tag is a graphical_unit_types group name, expand to its tags too.
            if (_tagsPerGuiType.TryGetValue(t, out var groupTags))
                foreach (var gt in groupTags) Add(gt);
        }

        foreach (var t in inlineTags) Add(t);
        return tags;
    }

    private int ParseQuality(BlockValue body, Dictionary<string, string> macros)
    {
        foreach (var (k, v) in body.Values)
        {
            if (!StringComparer.OrdinalIgnoreCase.Equals(k, "quality")) continue;
            string value = v;
            if (value.Length > 0 && value[0] == '@' && value.Length > 1 &&
                macros.TryGetValue(value.Substring(1), out var macroVal))
                value = macroVal;
            if (int.TryParse(value, out int q)) return q;
        }
        return 0;
    }
}

internal sealed class UnitLink
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public List<string> GraphicalCultures { get; set; } = new();
    public string Entity { get; set; } = "";
    public int Quality { get; set; }

    public bool MatchesTag(string tag)
    {
        foreach (var t in GraphicalCultures)
            if (StringComparer.OrdinalIgnoreCase.Equals(t, tag)) return true;
        return false;
    }
}