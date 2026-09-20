using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PdxModIDE.MapEngine
{
    public class BookmarkCharacter
    {
        public string NameKey { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string HistoryId { get; set; } = "";
        public string Dynasty { get; set; } = "";
        public string DynastyHouse { get; set; } = "";
        public string Title { get; set; } = "";
        public string Government { get; set; } = "";
        public string Culture { get; set; } = "";
        public string Religion { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public string Relation { get; set; } = "";
        public string Type { get; set; } = "";
        public string Birth { get; set; } = "";
        public string BookmarkType { get; set; } = "";
        public string RawBlock { get; set; } = "";
        public List<BookmarkCharacter> SubCharacters { get; set; } = new();
    }

    public class BookmarkInfo
    {
        public string Name { get; set; } = "";
        public string RawKey { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Source { get; set; } = "Base";
        public string SourceFile { get; set; } = "";
        public bool IsModNew { get; set; }
        public string StartDate { get; set; } = "";
        public string IsPlayable { get; set; } = "";
        public string Group { get; set; } = "";
        public string GroupDisplayName { get; set; } = "";
        public string RequiresDlcFlag { get; set; } = "";
        public string Recommended { get; set; } = "";
        public string WeightRaw { get; set; } = "";
        public List<BookmarkCharacter> Characters { get; set; } = new();
        public string RawBlock { get; set; } = "";
    }

    public class BookmarkGroupInfo
    {
        public string Name { get; set; } = "";
        public string RawKey { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Source { get; set; } = "Base";
        public string SourceFile { get; set; } = "";
        public bool IsModNew { get; set; }
        public string DefaultStartDate { get; set; } = "";
        public string RawBlock { get; set; } = "";
    }

    public static class BookmarkLoader
    {
        public static Dictionary<string, BookmarkGroupInfo> LoadGroups(string gameRoot, string sourceLabel, HashSet<string>? baseKeys = null)
        {
            var result = new Dictionary<string, BookmarkGroupInfo>(StringComparer.OrdinalIgnoreCase);
            string folder = Path.Combine(gameRoot, "common", "bookmarks", "groups");
            if (!Directory.Exists(folder))
            {
                // fallback old flat path
                string flat = Path.Combine(gameRoot, "common", "bookmarks");
                if (File.Exists(Path.Combine(flat, "00_bookmark_groups.txt")))
                    folder = flat;
                else
                    return result;
            }

            foreach (string file in Directory.EnumerateFiles(folder, "*.txt", SearchOption.AllDirectories))
            {
                // skip _info files is ok, they contain no groups
                var parsed = ParseGroupFile(file);
                foreach (var kv in parsed)
                {
                    if (!result.ContainsKey(kv.Key))
                    {
                        kv.Value.Source = sourceLabel;
                        kv.Value.SourceFile = file;
                        kv.Value.IsModNew = sourceLabel == "Mod" && (baseKeys == null || !baseKeys.Contains(kv.Key));
                        result[kv.Key] = kv.Value;
                    }
                }
            }
            return result;
        }

        public static Dictionary<string, BookmarkInfo> LoadBookmarks(string gameRoot, string sourceLabel, HashSet<string>? baseKeys = null)
        {
            var result = new Dictionary<string, BookmarkInfo>(StringComparer.OrdinalIgnoreCase);
            string folder = Path.Combine(gameRoot, "common", "bookmarks", "bookmarks");
            string fallback = Path.Combine(gameRoot, "common", "bookmarks");
            bool useFallback = false;
            if (!Directory.Exists(folder))
            {
                if (Directory.Exists(fallback) && Directory.EnumerateFiles(fallback, "*.txt", SearchOption.TopDirectoryOnly).Any(f => Path.GetFileName(f).Contains("bookmark")))
                    useFallback = true;
                else
                    return result;
            }

            IEnumerable<string> files = useFallback
                ? Directory.EnumerateFiles(fallback, "*.txt", SearchOption.TopDirectoryOnly)
                : Directory.EnumerateFiles(folder, "*.txt", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                if (Path.GetFileName(file).StartsWith("_")) continue;
                var parsed = ParseBookmarkFile(file);
                foreach (var kv in parsed)
                {
                    if (!result.ContainsKey(kv.Key))
                    {
                        kv.Value.Source = sourceLabel;
                        kv.Value.SourceFile = file;
                        kv.Value.IsModNew = sourceLabel == "Mod" && (baseKeys == null || !baseKeys.Contains(kv.Key));
                        result[kv.Key] = kv.Value;
                    }
                }
            }
            return result;
        }

        public static Dictionary<string, BookmarkInfo> LoadMergedBookmarks(string gameRoot, string modRoot, out Dictionary<string, BookmarkGroupInfo> groups)
        {
            var baseGroups = LoadGroups(gameRoot, "Base");
            var baseKeys = new HashSet<string>(baseGroups.Keys, StringComparer.OrdinalIgnoreCase);
            var modGroups = LoadGroups(modRoot, "Mod", baseKeys);

            var mergedGroups = new Dictionary<string, BookmarkGroupInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in baseGroups) mergedGroups[kv.Key] = kv.Value;
            foreach (var kv in modGroups) mergedGroups[kv.Key] = kv.Value;
            groups = mergedGroups;

            var baseBms = LoadBookmarks(gameRoot, "Base");
            var baseBmKeys = new HashSet<string>(baseBms.Keys, StringComparer.OrdinalIgnoreCase);
            var modBms = LoadBookmarks(modRoot, "Mod", baseBmKeys);

            var merged = new Dictionary<string, BookmarkInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in baseBms) merged[kv.Key] = kv.Value;
            foreach (var kv in modBms) merged[kv.Key] = kv.Value;

            // resolve group display later, keep raw
            return merged;
        }

        public static Dictionary<string, string> LoadBookmarkLocalization(string gameRoot, string modRoot, string uiLang)
        {
            string ck3Lang = uiLang switch { "es" => "spanish", "ca" => "spanish", "fr" => "french", "de" => "german", "ru" => "russian", _ => "english" };
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in new[] { gameRoot, modRoot })
            {
                if (string.IsNullOrEmpty(root)) continue;
                // mod should overwrite game, so game first then mod
            }
            // game first
            if (!string.IsNullOrEmpty(gameRoot))
                CollectLoc(new[] { Path.Combine(gameRoot, "localization", ck3Lang), Path.Combine(gameRoot, "localization", ck3Lang, "bookmarks") }, result, false);
            if (!string.IsNullOrEmpty(modRoot))
                CollectLoc(new[] { Path.Combine(modRoot, "localization", ck3Lang), Path.Combine(modRoot, "localization", ck3Lang, "bookmarks"), Path.Combine(modRoot, "localization", "replace", ck3Lang) }, result, true);
            return result;
        }

        private static void CollectLoc(string[] folders, Dictionary<string, string> output, bool overwrite)
        {
            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder)) continue;
                foreach (var file in Directory.EnumerateFiles(folder, "*.yml", SearchOption.AllDirectories))
                {
                    try
                    {
                        foreach (var raw in File.ReadAllLines(file))
                        {
                            var line = raw.Trim();
                            if (string.IsNullOrEmpty(line) || line.StartsWith("l_") || line.StartsWith("#") || !line.Contains(":")) continue;
                            int colon = line.IndexOf(':');
                            if (colon <= 0) continue;
                            string key = line.Substring(0, colon).Trim();
                            if (string.IsNullOrEmpty(key) || key.Contains(' ')) continue;
                            string val = line.Substring(colon + 1).Trim();
                            // handle :0 "value"
                            int q = val.IndexOf('"');
                            if (q >= 0)
                            {
                                int q2 = val.LastIndexOf('"');
                                if (q2 > q) val = val.Substring(q + 1, q2 - q - 1);
                                else val = val.Substring(q + 1).Trim().Trim('"');
                            }
                            else
                            {
                                // strip leading digit
                                val = val.Trim().TrimStart('0').Trim();
                                if (val.StartsWith("\"")) val = val.Trim('"');
                            }
                            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(val)) continue;
                            if (overwrite || !output.ContainsKey(key))
                                output[key] = val;
                            else if (overwrite)
                                output[key] = val;
                        }
                    }
                    catch { }
                }
            }
        }

        public static Dictionary<string, BookmarkInfo> ParseBookmarkFile(string path)
        {
            var data = new Dictionary<string, BookmarkInfo>(StringComparer.OrdinalIgnoreCase);
            string text;
            try { text = File.ReadAllText(path); } catch { return data; }
            int pos = 0;
            while (pos < text.Length)
            {
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length) break;
                string key = ReadKey(text, ref pos);
                if (string.IsNullOrEmpty(key)) break;
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '=')
                {
                    SkipValueAndFollowingBlock(text, ref pos);
                    continue;
                }
                pos++;
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '{')
                {
                    SkipValueAndFollowingBlock(text, ref pos);
                    continue;
                }
                pos++;
                string block = ReadBlock(text, ref pos);
                if (data.ContainsKey(key)) continue;
                var info = ParseBookmarkBlock(key, block);
                data[key] = info;
            }
            return data;
        }

        private static BookmarkInfo ParseBookmarkBlock(string key, string block)
        {
            var info = new BookmarkInfo { Name = key, RawKey = key, RawBlock = block };
            info.StartDate = ExtractAttribute(block, "start_date") ?? "";
            info.IsPlayable = ExtractAttribute(block, "is_playable") ?? "";
            info.Group = ExtractAttribute(block, "group") ?? "";
            info.RequiresDlcFlag = ExtractAttribute(block, "requires_dlc_flag") ?? "";
            info.Recommended = ExtractAttribute(block, "recommended") ?? "";
            // weight = { ... } keep raw
            info.WeightRaw = ExtractBlockAttribute(block, "weight") ?? "";
            // characters: multiple character = { ... }
            info.Characters = ExtractCharacters(block);
            return info;
        }

        private static List<BookmarkCharacter> ExtractCharacters(string block)
        {
            var list = new List<BookmarkCharacter>();
            int pos = 0;
            while (pos < block.Length)
            {
                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length) break;
                string k = ReadKey(block, ref pos);
                if (string.IsNullOrEmpty(k))
                {
                    pos++;
                    continue;
                }
                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length || block[pos] != '=')
                {
                    SkipValueAndFollowingBlock(block, ref pos);
                    continue;
                }
                pos++;
                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length) break;
                string inner = "";
                if (block[pos] == '{')
                {
                    pos++;
                    inner = ReadBlock(block, ref pos);
                }
                else
                {
                    // simple value, skip
                    int s = pos;
                    while (pos < block.Length && !char.IsWhiteSpace(block[pos]) && block[pos] != '#' && block[pos] != '}') pos++;
                    continue;
                }
                if (k == "character")
                {
                    var ch = ParseCharacterBlock(inner);
                    list.Add(ch);
                }
            }
            return list;
        }

        private static BookmarkCharacter ParseCharacterBlock(string block)
        {
            var ch = new BookmarkCharacter { RawBlock = block };
            ch.NameKey = ExtractAttribute(block, "name") ?? "";
            ch.HistoryId = ExtractAttribute(block, "history_id") ?? "";
            ch.Dynasty = ExtractAttribute(block, "dynasty") ?? "";
            ch.DynastyHouse = ExtractAttribute(block, "dynasty_house") ?? "";
            ch.Title = ExtractAttribute(block, "title") ?? "";
            ch.Government = ExtractAttribute(block, "government") ?? "";
            ch.Culture = ExtractAttribute(block, "culture") ?? "";
            ch.Religion = ExtractAttribute(block, "religion") ?? "";
            ch.Difficulty = ExtractAttribute(block, "difficulty") ?? "";
            ch.Relation = ExtractAttribute(block, "relation") ?? "";
            ch.Type = ExtractAttribute(block, "type") ?? "";
            ch.Birth = ExtractAttribute(block, "birth") ?? "";
            ch.BookmarkType = ExtractAttribute(block, "bookmark_type") ?? "";
            // nested characters (alternates)
            ch.SubCharacters = ExtractCharacters(block);
            return ch;
        }

        public static Dictionary<string, BookmarkGroupInfo> ParseGroupFile(string path)
        {
            var data = new Dictionary<string, BookmarkGroupInfo>(StringComparer.OrdinalIgnoreCase);
            string text;
            try { text = File.ReadAllText(path); } catch { return data; }
            int pos = 0;
            while (pos < text.Length)
            {
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length) break;
                string key = ReadKey(text, ref pos);
                if (string.IsNullOrEmpty(key)) break;
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '=')
                {
                    SkipValueAndFollowingBlock(text, ref pos);
                    continue;
                }
                pos++;
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '{')
                {
                    SkipValueAndFollowingBlock(text, ref pos);
                    continue;
                }
                pos++;
                string block = ReadBlock(text, ref pos);
                if (data.ContainsKey(key)) continue;
                var info = new BookmarkGroupInfo { Name = key, RawKey = key, RawBlock = block };
                info.DefaultStartDate = ExtractAttribute(block, "default_start_date") ?? "";
                data[key] = info;
            }
            return data;
        }

        private static string? ExtractAttribute(string block, string attributeName)
        {
            int pos = 0;
            while (pos < block.Length)
            {
                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length) break;
                string key = ReadKey(block, ref pos);
                if (string.IsNullOrEmpty(key)) break;
                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length || block[pos] != '=') { SkipValueAndFollowingBlock(block, ref pos); continue; }
                pos++;
                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length) break;
                if (key == attributeName)
                {
                    if (block[pos] == '"')
                    {
                        pos++;
                        int start = pos;
                        while (pos < block.Length && block[pos] != '"') pos++;
                        string v = block.Substring(start, pos - start);
                        if (pos < block.Length) pos++;
                        return v;
                    }
                    else if (block[pos] == '{')
                    {
                        // block attribute, not simple string — skip
                        pos++;
                        string b = ReadBlock(block, ref pos);
                        return b.Trim();
                    }
                    else
                    {
                        int start = pos;
                        while (pos < block.Length && !char.IsWhiteSpace(block[pos]) && block[pos] != '}' && block[pos] != '#')
                        {
                            if (block[pos] == '-' && pos + 1 < block.Length && block[pos + 1] == '-') break;
                            pos++;
                        }
                        return block.Substring(start, pos - start).Trim().Trim('"');
                    }
                }
                SkipValueAndFollowingBlock(block, ref pos);
            }
            return null;
        }

        private static string? ExtractBlockAttribute(string block, string attributeName)
        {
            int pos = 0;
            while (pos < block.Length)
            {
                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length) break;
                string key = ReadKey(block, ref pos);
                if (string.IsNullOrEmpty(key)) break;
                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length || block[pos] != '=') { SkipValueAndFollowingBlock(block, ref pos); continue; }
                pos++;
                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length) break;
                if (key == attributeName)
                {
                    if (block[pos] == '{')
                    {
                        pos++;
                        string b = ReadBlock(block, ref pos);
                        return b;
                    }
                    return null;
                }
                SkipValueAndFollowingBlock(block, ref pos);
            }
            return null;
        }

        private static string ReadBlock(string text, ref int pos)
        {
            int depth = 1;
            int start = pos;
            while (pos < text.Length && depth > 0)
            {
                if (text[pos] == '{') depth++;
                else if (text[pos] == '}') depth--;
                if (depth > 0) pos++;
            }
            string result = text.Substring(start, pos - start);
            if (pos < text.Length) pos++;
            return result;
        }

        private static string ReadKey(string text, ref int pos)
        {
            int start = pos;
            while (pos < text.Length && (char.IsLetterOrDigit(text[pos]) || text[pos] == '_' || text[pos] == '@' || text[pos] == '.' || text[pos] == '-')) pos++;
            return pos > start ? text.Substring(start, pos - start) : "";
        }

        private static void SkipWhitespaceAndComments(string text, ref int pos)
        {
            while (pos < text.Length)
            {
                if (char.IsWhiteSpace(text[pos])) pos++;
                else if (text[pos] == '#') { while (pos < text.Length && text[pos] != '\n') pos++; }
                else if (text[pos] == '-' && pos + 1 < text.Length && text[pos + 1] == '-') { while (pos < text.Length && text[pos] != '\n') pos++; }
                else break;
            }
        }

        private static void SkipValueAndFollowingBlock(string block, ref int pos)
        {
            if (pos >= block.Length) return;
            if (block[pos] == '{') { pos++; ReadBlock(block, ref pos); }
            else if (block[pos] == '"') { pos++; while (pos < block.Length && block[pos] != '"') pos++; if (pos < block.Length) pos++; }
            else
            {
                while (pos < block.Length && !char.IsWhiteSpace(block[pos]) && block[pos] != '}' && block[pos] != '#')
                {
                    if (block[pos] == '-' && pos + 1 < block.Length && block[pos + 1] == '-') break;
                    pos++;
                }
                SkipWhitespaceAndComments(block, ref pos);
                if (pos < block.Length && block[pos] == '{') { pos++; ReadBlock(block, ref pos); }
            }
        }
    }
}
