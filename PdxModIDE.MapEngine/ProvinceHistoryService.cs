using System;
using System.IO;
using System.Text.RegularExpressions;

namespace PdxModIDE.MapEngine
{
    public enum ProvinceHistoryOrigin
    {
        NotFound,
        ModSingle,
        ModGrouped,
        Game
    }

    public sealed record ProvinceHistoryLocation(
        ProvinceHistoryOrigin Origin,
        string? FilePath,
        string? DisplayName);

    public enum ProvinceWriteResult
    {
        Written,
        RequiresSplit,
        SourceNotFound,
        InvalidArgs,
        IOError
    }

    public static class ProvinceHistoryService
    {
        public const string OffsetBackupDirName = "offset_backup";

        public static string GetSinglePath(string modRoot, int provinceId)
            => Path.Combine(modRoot, "history", "provinces", "mod", $"{provinceId}.txt");

        public static string GetNestedSinglePath(string modRoot, int provinceId, string? k, string? d, string? c)
        {
            string dir = Path.Combine(modRoot, "history", "provinces", "mod");
            if (!string.IsNullOrEmpty(k)) dir = Path.Combine(dir, SanitizeFolder(k));
            if (!string.IsNullOrEmpty(d)) dir = Path.Combine(dir, SanitizeFolder(d));
            if (!string.IsNullOrEmpty(c)) dir = Path.Combine(dir, SanitizeFolder(c));
            return Path.Combine(dir, $"{provinceId}.txt");
        }

        private static string SanitizeFolder(string name)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char ch in name)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_') sb.Append(ch);
            }
            string s = sb.ToString();
            return string.IsNullOrEmpty(s) ? "_" : s;
        }

        public static bool TryExtractProvinceHeaders(string fileText, int provinceId, out string? k, out string? d, out string? c)
        {
            k = d = c = null;
            var m = Regex.Match(fileText, @"(?m)^\s*" + provinceId + @"\s*=\s*\{");
            if (!m.Success) return false;
            string before = fileText.Substring(0, m.Index);
            string[] lines = before.Split('\n');
            foreach (string raw in lines)
            {
                string t = raw.Trim();
                if (!t.StartsWith("#")) continue;
                foreach (Match tm in Regex.Matches(t, @"\b([kdc]_[A-Za-z0-9_]+)\b"))
                {
                    string tok = tm.Groups[1].Value;
                    if (tok.StartsWith("k_")) k = tok;
                    else if (tok.StartsWith("d_")) d = tok;
                    else if (tok.StartsWith("c_")) c = tok;
                }
            }
            return k != null || d != null || c != null;
        }

        private static string? FindSingleFile(string modRoot, int provinceId)
        {
            try
            {
                string modDir = Path.Combine(modRoot, "history", "provinces", "mod");
                if (!Directory.Exists(modDir)) return null;
                foreach (string f in Directory.EnumerateFiles(modDir, $"{provinceId}.txt", SearchOption.AllDirectories))
                    return f;
            }
            catch { }
            return null;
        }

        public static string GetOffsetBackupDir(string modRoot)
            => Path.Combine(modRoot, "history", "provinces", OffsetBackupDirName);

        public static string MoveToOffsetBackup(string sourceFilePath, string modRoot)
        {
            string backupDir = GetOffsetBackupDir(modRoot);
            Directory.CreateDirectory(backupDir);
            string baseName = Path.GetFileNameWithoutExtension(sourceFilePath);
            string dest = Path.Combine(backupDir, baseName + ".bkp");
            int n = 1;
            while (File.Exists(dest))
            {
                dest = Path.Combine(backupDir, $"{baseName}_{n}.bkp");
                n++;
            }
            File.Move(sourceFilePath, dest);
            return dest;
        }

        public static bool TryExtractProvinceBlock(string fileText, int provinceId, out string block, out int start, out int end)
        {
            block = "";
            start = -1;
            end = -1;
            var m = Regex.Match(fileText, @"(?m)^\s*" + provinceId + @"\s*=\s*\{");
            if (!m.Success) return false;
            start = m.Index;
            int brace = fileText.IndexOf('{', m.Index);
            if (brace < 0) return false;
            int depth = 0;
            int pos = brace;
            while (pos < fileText.Length)
            {
                if (fileText[pos] == '{') depth++;
                else if (fileText[pos] == '}') { depth--; if (depth == 0) { end = pos + 1; break; } }
                pos++;
            }
            if (end <= start) return false;
            block = fileText.Substring(start, end - start);
            return true;
        }

        public static System.Collections.Generic.List<int> GetAllProvinceIds(string fileText)
        {
            var ids = new System.Collections.Generic.List<int>();
            foreach (Match m in Regex.Matches(fileText, @"(?m)^\s*(\d+)\s*=\s*\{"))
            {
                if (int.TryParse(m.Groups[1].Value, out int id))
                    ids.Add(id);
            }
            return ids;
        }

        private static bool TryParseDate(string s, out int y, out int m, out int d)
        {
            y = m = d = 0;
            var mm = Regex.Match(s.Trim(), @"^(-?\d+)\.(\d+)\.(\d+)$");
            if (!mm.Success) return false;
            return int.TryParse(mm.Groups[1].Value, out y)
                && int.TryParse(mm.Groups[2].Value, out m)
                && int.TryParse(mm.Groups[3].Value, out d);
        }

        private static int CompareDates((int y, int m, int d) a, (int y, int m, int d) b)
        {
            int c = a.y.CompareTo(b.y);
            if (c != 0) return c;
            c = a.m.CompareTo(b.m);
            if (c != 0) return c;
            return a.d.CompareTo(b.d);
        }

        public static string UpsertCultureInBlock(string blockText, string dateStr, string newCulture)
        {
            var datePat = new Regex(@"(?m)^\s*" + Regex.Escape(dateStr) + @"\s*=\s*\{");
            var dm = datePat.Match(blockText);
            if (dm.Success)
            {
                int brace = blockText.IndexOf('{', dm.Index);
                int depth = 0;
                int pos = brace;
                int close = -1;
                while (pos < blockText.Length)
                {
                    if (blockText[pos] == '{') depth++;
                    else if (blockText[pos] == '}') { depth--; if (depth == 0) { close = pos; break; } }
                    pos++;
                }
                if (close < 0) return blockText;
                string inner = blockText.Substring(brace + 1, close - brace - 1);
                var cm = Regex.Match(inner, @"culture\s*=\s*[A-Za-z0-9_]+");
                string updatedInner;
                if (cm.Success)
                    updatedInner = inner.Substring(0, cm.Index) + $"culture = {newCulture}" + inner.Substring(cm.Index + cm.Length);
                else
                    updatedInner = "\n\t\tculture = " + newCulture + inner;
                string updatedBlock = blockText.Substring(0, brace + 1) + updatedInner + blockText.Substring(close);
                // base coherence: if the edited date is the earliest of all, update undated base culture too (culture only)
                if (TryParseDate(dateStr, out int ey, out int em2, out int ed2))
                {
                    var datedRe2 = new Regex(@"(?m)^\s*(?<date>-?\d+\.\d+\.\d+)\s*=\s*\{");
                    bool earliest = true;
                    foreach (Match m2 in datedRe2.Matches(blockText))
                    {
                        if (m2.Index == dm.Index) continue;
                        if (TryParseDate(m2.Groups["date"].Value, out int oy, out int om, out int od))
                        {
                            if (CompareDates((oy, om, od), (ey, em2, ed2)) < 0) { earliest = false; break; }
                        }
                    }
                    if (earliest)
                    {
                        var baseRe = new Regex(@"culture\s*=\s*[A-Za-z0-9_]+");
                        var bm = baseRe.Match(updatedBlock);
                        var firstDateAfter = datedRe2.Match(updatedBlock);
                        if (bm.Success && (!firstDateAfter.Success || bm.Index < firstDateAfter.Index))
                            updatedBlock = updatedBlock.Substring(0, bm.Index) + $"culture = {newCulture}" + updatedBlock.Substring(bm.Index + bm.Length);
                        else if (!bm.Success)
                        {
                            int open = updatedBlock.IndexOf('{');
                            if (open >= 0)
                                updatedBlock = updatedBlock.Substring(0, open + 1) + $"\n\tculture = {newCulture}" + updatedBlock.Substring(open + 1);
                        }
                    }
                }
                return updatedBlock;
            }
            else
            {
                // collect existing dated blocks with positions
                var datedRe = new Regex(@"(?m)^(?<indent>\s*)(?<date>-?\d+\.\d+\.\d+)\s*=\s*\{");
                var dates = new System.Collections.Generic.List<(string date, int y, int m, int d, int lineStart)>();
                foreach (Match em in datedRe.Matches(blockText))
                {
                    if (TryParseDate(em.Groups["date"].Value, out int yy, out int mm2, out int dd))
                        dates.Add((em.Groups["date"].Value, yy, mm2, dd, em.Index));
                }
                if (!TryParseDate(dateStr, out int ny, out int nm, out int nd))
                {
                    int last = blockText.LastIndexOf('}');
                    if (last < 0) return blockText;
                    string dated0 = $"\n\t{dateStr} = {{ culture = {newCulture} }}\n";
                    return blockText.Substring(0, last) + dated0 + blockText.Substring(last);
                }
                var ndTuple = (ny, nm, nd);
                int insertAt = -1;
                bool isEarliest = false;
                if (dates.Count > 0)
                {
                    (int y, int m, int d) min = (dates[0].y, dates[0].m, dates[0].d);
                    foreach (var e in dates)
                    {
                        if (CompareDates((e.y, e.m, e.d), min) < 0) min = (e.y, e.m, e.d);
                        if (insertAt < 0 && CompareDates(ndTuple, (e.y, e.m, e.d)) < 0) insertAt = e.lineStart;
                    }
                    isEarliest = CompareDates(ndTuple, min) < 0;
                }
                string dated = $"\t{dateStr} = {{ culture = {newCulture} }}\n";
                string result;
                if (insertAt >= 0)
                    result = blockText.Substring(0, insertAt) + dated + blockText.Substring(insertAt);
                else
                {
                    int last = blockText.LastIndexOf('}');
                    if (last < 0) return blockText;
                    result = blockText.Substring(0, last) + "\n\t" + dated + blockText.Substring(last);
                }
                if (isEarliest)
                {
                    // update base (undated) culture: first culture= before first dated block
                    int firstDated = insertAt >= 0 ? insertAt : result.Length;
                    // find base region: from outer open to first dated (in result coordinates, dated just inserted at insertAt)
                    // simplest: replace first top-level culture before any date line
                    var baseRe = new Regex(@"culture\s*=\s*[A-Za-z0-9_]+");
                    var bm = baseRe.Match(result);
                    // ensure base match is before first dated block content (i.e., its index < position of first date after edit)
                    var firstDateAfter = datedRe.Match(result);
                    if (bm.Success && (!firstDateAfter.Success || bm.Index < firstDateAfter.Index))
                        result = result.Substring(0, bm.Index) + $"culture = {newCulture}" + result.Substring(bm.Index + bm.Length);
                    else if (!bm.Success)
                    {
                        int open = result.IndexOf('{');
                        if (open >= 0)
                            result = result.Substring(0, open + 1) + $"\n\tculture = {newCulture}" + result.Substring(open + 1);
                    }
                }
                return result;
            }
        }

        public static ProvinceWriteResult TryWriteSingleCulture(string modRoot, string gameRoot, int provinceId, string dateStr, string newCulture, out string writtenPath, out string error)
        {
            writtenPath = "";
            error = "";
            if (provinceId <= 0 || string.IsNullOrWhiteSpace(dateStr) || string.IsNullOrWhiteSpace(newCulture))
            {
                error = "InvalidArgs";
                return ProvinceWriteResult.InvalidArgs;
            }
            if (!Regex.IsMatch(dateStr.Trim(), @"^-?\d+\.\d+\.\d+$") || !Regex.IsMatch(newCulture.Trim(), @"^[A-Za-z0-9_]+$"))
            {
                error = "InvalidArgs";
                return ProvinceWriteResult.InvalidArgs;
            }
            dateStr = dateStr.Trim();
            newCulture = newCulture.Trim();
            try
            {
                var loc = Locate(provinceId, modRoot, gameRoot);
                if (loc.Origin == ProvinceHistoryOrigin.ModGrouped)
                {
                    error = "RequiresSplit";
                    return ProvinceWriteResult.RequiresSplit;
                }
                string? target = null;
                string? baseBlock = null;
                string? gameText = null;
                if (loc.Origin == ProvinceHistoryOrigin.ModSingle && loc.FilePath != null && File.Exists(loc.FilePath))
                {
                    target = loc.FilePath;
                    string t = File.ReadAllText(loc.FilePath);
                    if (TryExtractProvinceBlock(t, provinceId, out string b, out _, out _))
                        baseBlock = b;
                }
                else if (loc.Origin == ProvinceHistoryOrigin.Game && loc.FilePath != null && File.Exists(loc.FilePath))
                {
                    gameText = File.ReadAllText(loc.FilePath);
                    if (TryExtractProvinceBlock(gameText, provinceId, out string b, out _, out _))
                        baseBlock = b;
                    TryExtractProvinceHeaders(gameText, provinceId, out string? gk, out string? gd, out string? gc);
                    target = GetNestedSinglePath(modRoot, provinceId, gk, gd, gc);
                }
                string updated;
                if (baseBlock != null)
                    updated = UpsertCultureInBlock(baseBlock, dateStr, newCulture);
                else if (loc.Origin == ProvinceHistoryOrigin.NotFound)
                {
                    target = GetSinglePath(modRoot, provinceId);
                    updated = $"{provinceId} = {{\n\t{dateStr} = {{ culture = {newCulture} }}\n}}\n";
                }
                else
                {
                    error = "SourceNotFound";
                    return ProvinceWriteResult.SourceNotFound;
                }
                if (string.IsNullOrEmpty(target))
                    target = GetSinglePath(modRoot, provinceId);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.WriteAllText(target, updated, new System.Text.UTF8Encoding(true));
                writtenPath = target;
                return ProvinceWriteResult.Written;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return ProvinceWriteResult.IOError;
            }
        }

        public sealed record SplitGroupedOutcome(
            ProvinceWriteResult Result,
            System.Collections.Generic.List<string> WrittenFiles,
            string? BackupPath,
            string Error);

        public static SplitGroupedOutcome TrySplitGroupedFile(string modRoot, string gameRoot, string groupedPath, int targetId, string dateStr, string newCulture)
        {
            var written = new System.Collections.Generic.List<string>();
            if (string.IsNullOrWhiteSpace(modRoot) || string.IsNullOrWhiteSpace(groupedPath) || targetId <= 0 ||
                string.IsNullOrWhiteSpace(dateStr) || string.IsNullOrWhiteSpace(newCulture))
                return new SplitGroupedOutcome(ProvinceWriteResult.InvalidArgs, written, null, "InvalidArgs");
            dateStr = dateStr.Trim();
            newCulture = newCulture.Trim();
            if (!Regex.IsMatch(dateStr, @"^-?\d+\.\d+\.\d+$") || !Regex.IsMatch(newCulture, @"^[A-Za-z0-9_]+$"))
                return new SplitGroupedOutcome(ProvinceWriteResult.InvalidArgs, written, null, "InvalidArgs");
            try
            {
                if (!File.Exists(groupedPath))
                    return new SplitGroupedOutcome(ProvinceWriteResult.SourceNotFound, written, null, "SourceNotFound");
                string text = File.ReadAllText(groupedPath);
                var ids = GetAllProvinceIds(text);
                if (ids.Count == 0 || !ids.Contains(targetId))
                    return new SplitGroupedOutcome(ProvinceWriteResult.SourceNotFound, written, null, "SourceNotFound");
                var blocks = new System.Collections.Generic.Dictionary<int, (string block, string path)>();
                foreach (int id in ids)
                {
                    if (!TryExtractProvinceBlock(text, id, out string b, out _, out _))
                        return new SplitGroupedOutcome(ProvinceWriteResult.IOError, written, null, $"BlockNotFound:{id}");
                    string updatedBlock = id == targetId ? UpsertCultureInBlock(b, dateStr, newCulture) : b;
                    TryExtractProvinceHeaders(text, id, out string? hk, out string? hd, out string? hc);
                    string single = GetNestedSinglePath(modRoot, id, hk, hd, hc);
                    blocks[id] = (updatedBlock, single);
                }
                foreach (var kvp in blocks)
                {
                    string single = kvp.Value.path;
                    if (kvp.Key != targetId && (File.Exists(single) || FindSingleFile(modRoot, kvp.Key) != null))
                        continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(single)!);
                    File.WriteAllText(single, kvp.Value.block + "\n", new System.Text.UTF8Encoding(true));
                    written.Add(single);
                }
                string backup = MoveToOffsetBackup(groupedPath, modRoot);
                return new SplitGroupedOutcome(ProvinceWriteResult.Written, written, backup, "");
            }
            catch (Exception ex)
            {
                return new SplitGroupedOutcome(ProvinceWriteResult.IOError, written, null, ex.Message);
            }
        }

        public static ProvinceHistoryLocation Locate(int provinceId, string modRoot, string gameRoot)
        {
            if (!string.IsNullOrEmpty(modRoot) && Directory.Exists(modRoot))
            {
                string? single = FindSingleFile(modRoot, provinceId);
                if (single != null)
                {
                    string rel = single;
                    try
                    {
                        string baseDir = Path.Combine(modRoot, "history", "provinces", "mod");
                        rel = Path.GetRelativePath(baseDir, single).Replace('\\', '/');
                    }
                    catch { rel = $"{provinceId}.txt"; }
                    return new ProvinceHistoryLocation(ProvinceHistoryOrigin.ModSingle, single, rel);
                }

                string modDir = Path.Combine(modRoot, "history", "provinces");
                if (Directory.Exists(modDir))
                {
                    string? grouped = FindContainingFile(modDir, provinceId, excludeModSubfolder: true);
                    if (grouped != null)
                        return new ProvinceHistoryLocation(ProvinceHistoryOrigin.ModGrouped, grouped, Path.GetFileName(grouped));
                }
            }

            if (!string.IsNullOrEmpty(gameRoot) && Directory.Exists(gameRoot))
            {
                string gameDir = Path.Combine(gameRoot, "history", "provinces");
                if (Directory.Exists(gameDir))
                {
                    string? gameFile = FindContainingFile(gameDir, provinceId, excludeModSubfolder: false);
                    if (gameFile != null)
                        return new ProvinceHistoryLocation(ProvinceHistoryOrigin.Game, gameFile, Path.GetFileName(gameFile));
                }
            }

            return new ProvinceHistoryLocation(ProvinceHistoryOrigin.NotFound, null, null);
        }

        private static string? FindContainingFile(string dir, int provinceId, bool excludeModSubfolder)
        {
            Regex re = new(@"^\s*" + provinceId + @"\s*=\s*\{", RegexOptions.Compiled);
            try
            {
                foreach (string file in Directory.EnumerateFiles(dir, "*.txt", SearchOption.AllDirectories))
                {
                    if (excludeModSubfolder)
                    {
                        string relative = file[dir.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        if (relative.StartsWith("mod" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                            relative.StartsWith("mod" + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(relative, "mod", StringComparison.OrdinalIgnoreCase))
                            continue;
                    }
                    try
                    {
                        foreach (string raw in File.ReadLines(file))
                        {
                            string line = raw;
                            int hash = line.IndexOf('#');
                            if (hash >= 0)
                                line = line.Substring(0, hash);
                            if (re.IsMatch(line))
                                return file;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }
    }
}
