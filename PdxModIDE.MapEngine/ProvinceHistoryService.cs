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
                return blockText.Substring(0, brace + 1) + updatedInner + blockText.Substring(close);
            }
            else
            {
                int last = blockText.LastIndexOf('}');
                if (last < 0) return blockText;
                string dated = $"\n\t{dateStr} = {{ culture = {newCulture} }}\n";
                return blockText.Substring(0, last) + dated + blockText.Substring(last);
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
                string target = GetSinglePath(modRoot, provinceId);
                string? baseBlock = null;
                if (loc.Origin == ProvinceHistoryOrigin.ModSingle && loc.FilePath != null && File.Exists(loc.FilePath))
                {
                    string t = File.ReadAllText(loc.FilePath);
                    if (TryExtractProvinceBlock(t, provinceId, out string b, out _, out _))
                        baseBlock = b;
                }
                else if (loc.Origin == ProvinceHistoryOrigin.Game && loc.FilePath != null && File.Exists(loc.FilePath))
                {
                    string t = File.ReadAllText(loc.FilePath);
                    if (TryExtractProvinceBlock(t, provinceId, out string b, out _, out _))
                        baseBlock = b;
                }
                string updated;
                if (baseBlock != null)
                    updated = UpsertCultureInBlock(baseBlock, dateStr, newCulture);
                else if (loc.Origin == ProvinceHistoryOrigin.NotFound)
                    updated = $"{provinceId} = {{\n\t{dateStr} = {{ culture = {newCulture} }}\n}}\n";
                else
                {
                    error = "SourceNotFound";
                    return ProvinceWriteResult.SourceNotFound;
                }
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

        public static ProvinceHistoryLocation Locate(int provinceId, string modRoot, string gameRoot)
        {
            if (!string.IsNullOrEmpty(modRoot) && Directory.Exists(modRoot))
            {
                string single = Path.Combine(modRoot, "history", "provinces", "mod", $"{provinceId}.txt");
                if (File.Exists(single))
                    return new ProvinceHistoryLocation(ProvinceHistoryOrigin.ModSingle, single, $"{provinceId}.txt");

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
