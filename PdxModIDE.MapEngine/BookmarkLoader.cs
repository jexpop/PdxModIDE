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

        // File helpers for CRUD (parity with CulturesTab)
        public static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder();
            foreach (char c in name)
                sb.Append(invalid.Contains(c) ? '_' : c);
            return sb.ToString();
        }

        public static bool BookmarkBlockExistsInFile(string filePath, string id) => BlockExistsInFile(filePath, id);
        public static bool GroupBlockExistsInFile(string filePath, string id) => BlockExistsInFile(filePath, id);

        private static bool BlockExistsInFile(string filePath, string id)
        {
            if (!File.Exists(filePath)) return false;
            var text = File.ReadAllText(filePath);
            int pos = 0;
            while (pos < text.Length)
            {
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length) break;
                string key = ReadKey(text, ref pos);
                if (string.IsNullOrEmpty(key)) break;
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '=') { SkipValueAndFollowingBlock(text, ref pos); continue; }
                pos++; SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '{') { SkipValueAndFollowingBlock(text, ref pos); continue; }
                if (string.Equals(key, id, StringComparison.OrdinalIgnoreCase)) return true;
                pos++; ReadBlock(text, ref pos);
            }
            return false;
        }

        public static bool DeleteBookmarkBlockFromFile(string filePath, string id) => DeleteBlockFromFile(filePath, id);
        public static bool DeleteGroupBlockFromFile(string filePath, string id) => DeleteBlockFromFile(filePath, id);

        private static bool DeleteBlockFromFile(string filePath, string id)
        {
            var text = File.ReadAllText(filePath);
            int pos = 0;
            while (pos < text.Length)
            {
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length) break;
                int keyStart = pos;
                string key = ReadKey(text, ref pos);
                if (string.IsNullOrEmpty(key)) break;
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '=') { SkipValueAndFollowingBlock(text, ref pos); continue; }
                pos++; SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '{') { SkipValueAndFollowingBlock(text, ref pos); continue; }
                int braceStart = pos;
                pos++; string inner = ReadBlock(text, ref pos);
                int blockEnd = pos;
                if (string.Equals(key, id, StringComparison.OrdinalIgnoreCase))
                {
                    // remove from keyStart to blockEnd, also strip preceding standalone comment lines (# real->file)
                    string before = text.Substring(0, keyStart);
                    string trimmedBefore = before.TrimEnd();
                    while (true)
                    {
                        int nl = trimmedBefore.LastIndexOf('\n');
                        string lastLine = nl >= 0 ? trimmedBefore.Substring(nl + 1) : trimmedBefore;
                        if (lastLine.TrimStart().StartsWith("#"))
                            trimmedBefore = (nl >= 0 ? trimmedBefore.Substring(0, nl) : "").TrimEnd();
                        else break;
                    }
                    string after = text.Substring(blockEnd);
                    string newText = trimmedBefore + (string.IsNullOrWhiteSpace(after) ? (trimmedBefore.Length > 0 ? "\n" : "") : "\n" + after.TrimStart());
                    if (string.IsNullOrWhiteSpace(newText)) newText = "";
                    File.WriteAllText(filePath, newText, new System.Text.UTF8Encoding(true));
                    return true;
                }
            }
            return false;
        }

        public static int CountBookmarkBlocks(string filePath) => CountBlocks(filePath);
        public static int CountGroupBlocks(string filePath) => CountBlocks(filePath);

        private static int CountBlocks(string filePath)
        {
            if (!File.Exists(filePath)) return 0;
            var text = File.ReadAllText(filePath);
            int pos = 0; int count = 0;
            while (pos < text.Length)
            {
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length) break;
                string key = ReadKey(text, ref pos);
                if (string.IsNullOrEmpty(key)) break;
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '=') { SkipValueAndFollowingBlock(text, ref pos); continue; }
                pos++; SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '{') { SkipValueAndFollowingBlock(text, ref pos); continue; }
                pos++; ReadBlock(text, ref pos); count++;
            }
            return count;
        }

        public static void ReplaceBlockInFile(string filePath, string id, string newBlock)
        {
            var text = File.ReadAllText(filePath);
            int pos = 0;
            while (pos < text.Length)
            {
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length) break;
                int keyStart = pos;
                string key = ReadKey(text, ref pos);
                if (string.IsNullOrEmpty(key)) break;
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '=') { SkipValueAndFollowingBlock(text, ref pos); continue; }
                pos++; SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '{') { SkipValueAndFollowingBlock(text, ref pos); continue; }
                int braceStart = pos;
                pos++; string inner = ReadBlock(text, ref pos);
                int blockEnd = pos;
                if (string.Equals(key, id, StringComparison.OrdinalIgnoreCase))
                {
                    string before = text.Substring(0, keyStart);
                    // remove preceding standalone comment lines (# ...) right before the block so old "(real)" references don't accumulate
                    string trimmedBefore = before.TrimEnd();
                    while (true)
                    {
                        int nl = trimmedBefore.LastIndexOf('\n');
                        string lastLine = nl >= 0 ? trimmedBefore.Substring(nl + 1) : trimmedBefore;
                        if (lastLine.TrimStart().StartsWith("#"))
                        {
                            trimmedBefore = (nl >= 0 ? trimmedBefore.Substring(0, nl) : "").TrimEnd();
                        }
                        else break;
                    }
                    string after = text.Substring(blockEnd);
                    string newText = trimmedBefore + (trimmedBefore.Length > 0 ? "\n" : "") + newBlock.TrimEnd() + "\n" + after.TrimStart();
                    File.WriteAllText(filePath, newText, new System.Text.UTF8Encoding(true));
                    return;
                }
            }
            throw new InvalidOperationException($"Block {id} not found in {filePath}");
        }

        public static void InsertBlockAlphabetically(string filePath, string id, string block)
        {
            var text = File.ReadAllText(filePath);
            var ids = new List<string>(); var positions = new List<int>();
            int pos = 0;
            while (pos < text.Length)
            {
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length) break;
                int keyStart = pos;
                string key = ReadKey(text, ref pos);
                if (string.IsNullOrEmpty(key)) break;
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '=') { SkipValueAndFollowingBlock(text, ref pos); continue; }
                pos++; SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '{') { SkipValueAndFollowingBlock(text, ref pos); continue; }
                pos++; ReadBlock(text, ref pos);
                ids.Add(key); positions.Add(keyStart);
            }
            int idx = 0;
            while (idx < ids.Count && string.Compare(ids[idx], id, StringComparison.OrdinalIgnoreCase) < 0) idx++;
            string blockText = block.TrimEnd() + "\n";
            string newText;
            if (idx >= ids.Count)
            {
                if (text.Length > 0 && !text.EndsWith("\n")) text += "\n";
                newText = text + blockText;
            }
            else
            {
                int at = positions[idx];
                newText = text.Substring(0, at) + blockText + text.Substring(at);
            }
            File.WriteAllText(filePath, newText, new System.Text.UTF8Encoding(true));
        }

        public static string BuildGroupBlock(string id, string defaultStartDate)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{id} = {{");
            if (!string.IsNullOrWhiteSpace(defaultStartDate))
                sb.AppendLine($"\tdefault_start_date = {defaultStartDate.Trim()}");
            sb.AppendLine("}");
            return sb.ToString();
        }

        public static string BuildGroupBlockWithOffset(string id, string realDate, int offset)
        {
            string fileDate = ShiftDate(realDate, offset) ?? realDate.Trim();
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrWhiteSpace(realDate) && offset != 0)
                sb.AppendLine($"# {realDate.Trim()} (real) -> {fileDate} (file, offset {offset})");
            sb.AppendLine($"{id} = {{");
            if (!string.IsNullOrWhiteSpace(fileDate))
                sb.AppendLine($"\tdefault_start_date = {fileDate}");
            sb.AppendLine("}");
            return sb.ToString();
        }

        public static string? ShiftDate(string date, int offset)
        {
            if (string.IsNullOrWhiteSpace(date)) return null;
            var raw = date.Trim();
            // allow leading '-' for BC years (e.g. "-500.1.1"); Split with RemoveEmptyEntries would drop it
            bool negative = raw.StartsWith("-");
            if (negative) raw = raw.Substring(1);
            var parts = raw.Split('.', System.StringSplitOptions.TrimEntries);
            if (parts.Length < 1 || string.IsNullOrWhiteSpace(parts[0]) || !int.TryParse(parts[0], out int y)) return null;
            if (negative) y = -y;
            int m = 1, d = 1;
            if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1])) int.TryParse(parts[1], out m);
            if (parts.Length >= 3 && !string.IsNullOrWhiteSpace(parts[2])) int.TryParse(parts[2], out d);
            return $"{y + offset}.{m}.{d}";
        }

        public static int CompareDates(string a, string b)
        {
            var pa = TryParseDate(a);
            var pb = TryParseDate(b);
            if (pa == null && pb == null) return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            if (pa == null) return 1;
            if (pb == null) return -1;
            int c = pa.Value.y.CompareTo(pb.Value.y);
            if (c != 0) return c;
            c = pa.Value.m.CompareTo(pb.Value.m);
            if (c != 0) return c;
            return pa.Value.d.CompareTo(pb.Value.d);
        }

        private static (int y, int m, int d)? TryParseDate(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var raw = s.Trim();
            bool negative = raw.StartsWith("-");
            if (negative) raw = raw.Substring(1);
            var p = raw.Split('.', System.StringSplitOptions.TrimEntries);
            if (p.Length < 1 || string.IsNullOrWhiteSpace(p[0]) || !int.TryParse(p[0], out int y)) return null;
            if (negative) y = -y;
            int m = 1, d = 1;
            if (p.Length >= 2 && !string.IsNullOrWhiteSpace(p[1])) int.TryParse(p[1], out m);
            if (p.Length >= 3 && !string.IsNullOrWhiteSpace(p[2])) int.TryParse(p[2], out d);
            return (y, m, d);
        }

        public static void InsertGroupChronologically(string filePath, string id, string block, string fileDate)
        {
            var text = File.Exists(filePath) ? File.ReadAllText(filePath) : "";
            if (string.IsNullOrWhiteSpace(text))
            {
                File.WriteAllText(filePath, block.TrimEnd() + "\n", new System.Text.UTF8Encoding(true));
                return;
            }
            // collect existing groups with their dates and positions
            var entries = new List<(string Id, string Date, int Pos)>();
            int pos = 0;
            while (pos < text.Length)
            {
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length) break;
                int keyStart = pos;
                string key = ReadKey(text, ref pos);
                if (string.IsNullOrEmpty(key)) break;
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '=') { SkipValueAndFollowingBlock(text, ref pos); continue; }
                pos++; SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '{') { SkipValueAndFollowingBlock(text, ref pos); continue; }
                pos++; string inner = ReadBlock(text, ref pos);
                string date = ExtractAttribute(inner, "default_start_date") ?? "";
                entries.Add((key, date, keyStart));
            }
            int idx = 0;
            while (idx < entries.Count && CompareDates(entries[idx].Date, fileDate) < 0) idx++;
            string blockText = block.TrimEnd() + "\n";
            string newText;
            if (idx >= entries.Count)
            {
                if (text.Length > 0 && !text.EndsWith("\n")) text += "\n";
                newText = text + blockText;
            }
            else
            {
                int at = entries[idx].Pos;
                newText = text.Substring(0, at) + blockText + text.Substring(at);
            }
            File.WriteAllText(filePath, newText, new System.Text.UTF8Encoding(true));
        }

        public static string BuildBookmarkBlockWithOffset(string id, string realStartDate, int offset, string group, string isPlayable, string recommended, string requiresDlc, string weightRaw, List<BookmarkCharacter> characters)
        {
            string fileDate = string.IsNullOrWhiteSpace(realStartDate) ? (realStartDate ?? "") : (ShiftDate(realStartDate, offset) ?? realStartDate.Trim());
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrWhiteSpace(realStartDate) && offset != 0)
                sb.AppendLine($"# {realStartDate.Trim()} (real) -> {fileDate} (file, offset {offset})");
            sb.Append(BuildBookmarkBlock(id, fileDate, group, isPlayable, recommended, requiresDlc, weightRaw, characters));
            return sb.ToString();
        }

        public static string BuildBookmarkBlock(string id, string startDate, string group, string isPlayable, string recommended, string requiresDlc, string weightRaw, List<BookmarkCharacter> characters)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{id} = {{");
            if (!string.IsNullOrWhiteSpace(startDate)) sb.AppendLine($"\tstart_date = {startDate.Trim()}");
            if (!string.IsNullOrWhiteSpace(isPlayable)) sb.AppendLine($"\tis_playable = {isPlayable.Trim()}");
            if (!string.IsNullOrWhiteSpace(group)) sb.AppendLine($"\tgroup = {group.Trim()}");
            if (!string.IsNullOrWhiteSpace(recommended)) sb.AppendLine($"\trecommended = {recommended.Trim()}");
            if (!string.IsNullOrWhiteSpace(requiresDlc)) sb.AppendLine($"\trequires_dlc_flag = {requiresDlc.Trim()}");
            if (!string.IsNullOrWhiteSpace(weightRaw))
            {
                // weightRaw already contains inner block content
                if (weightRaw.Trim().StartsWith("{"))
                    sb.AppendLine($"\tweight = {weightRaw.Trim()}");
                else
                    sb.AppendLine($"\tweight = {{ {weightRaw.Trim()} }}");
            }
            foreach (var ch in characters)
            {
                sb.AppendLine("\tcharacter = {");
                if (!string.IsNullOrWhiteSpace(ch.NameKey)) sb.AppendLine($"\t\tname = \"{ch.NameKey}\"");
                if (!string.IsNullOrWhiteSpace(ch.HistoryId)) sb.AppendLine($"\t\thistory_id = {ch.HistoryId}");
                if (!string.IsNullOrWhiteSpace(ch.BookmarkType)) sb.AppendLine($"\t\tbookmark_type = {ch.BookmarkType}");
                if (!string.IsNullOrWhiteSpace(ch.Dynasty)) sb.AppendLine($"\t\tdynasty = {ch.Dynasty}");
                if (!string.IsNullOrWhiteSpace(ch.DynastyHouse)) sb.AppendLine($"\t\tdynasty_house = {ch.DynastyHouse}");
                if (!string.IsNullOrWhiteSpace(ch.Title)) sb.AppendLine($"\t\ttitle = {ch.Title}");
                if (!string.IsNullOrWhiteSpace(ch.Government)) sb.AppendLine($"\t\tgovernment = {ch.Government}");
                if (!string.IsNullOrWhiteSpace(ch.Culture)) sb.AppendLine($"\t\tculture = {ch.Culture}");
                if (!string.IsNullOrWhiteSpace(ch.Religion)) sb.AppendLine($"\t\treligion = {ch.Religion}");
                if (!string.IsNullOrWhiteSpace(ch.Difficulty)) sb.AppendLine($"\t\tdifficulty = \"{ch.Difficulty}\"");
                if (!string.IsNullOrWhiteSpace(ch.Relation)) sb.AppendLine($"\t\trelation = \"{ch.Relation}\"");
                if (!string.IsNullOrWhiteSpace(ch.Type)) sb.AppendLine($"\t\ttype = {ch.Type}");
                if (!string.IsNullOrWhiteSpace(ch.Birth)) sb.AppendLine($"\t\tbirth = {ch.Birth}");
                foreach (var sub in ch.SubCharacters)
                {
                    sb.AppendLine("\t\tcharacter = {");
                    if (!string.IsNullOrWhiteSpace(sub.NameKey)) sb.AppendLine($"\t\t\tname = \"{sub.NameKey}\"");
                    if (!string.IsNullOrWhiteSpace(sub.HistoryId)) sb.AppendLine($"\t\t\thistory_id = {sub.HistoryId}");
                    if (!string.IsNullOrWhiteSpace(sub.Relation)) sb.AppendLine($"\t\t\trelation = \"{sub.Relation}\"");
                    if (!string.IsNullOrWhiteSpace(sub.Dynasty)) sb.AppendLine($"\t\t\tdynasty = {sub.Dynasty}");
                    if (!string.IsNullOrWhiteSpace(sub.DynastyHouse)) sb.AppendLine($"\t\t\tdynasty_house = {sub.DynastyHouse}");
                    if (!string.IsNullOrWhiteSpace(sub.Type)) sb.AppendLine($"\t\t\ttype = {sub.Type}");
                    if (!string.IsNullOrWhiteSpace(sub.Birth)) sb.AppendLine($"\t\t\tbirth = {sub.Birth}");
                    if (!string.IsNullOrWhiteSpace(sub.Culture)) sb.AppendLine($"\t\t\tculture = {sub.Culture}");
                    if (!string.IsNullOrWhiteSpace(sub.Religion)) sb.AppendLine($"\t\t\treligion = {sub.Religion}");
                    sb.AppendLine("\t\t}");
                }
                sb.AppendLine("\t}");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
