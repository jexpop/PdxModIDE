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

    public static class ProvinceHistoryService
    {
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
