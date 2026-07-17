using System;
using System.IO;

namespace PdxModIDE.IO
{
    public static class Paths
    {
        public static string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return Path.GetFullPath(new Uri(path).LocalPath)
                   .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static string GamePath(Dictionary<string, object> profile, params string[] parts)
        {
            string root = Normalize(GetStringValue(profile, "game_root"));
            return CombinePath(root, parts);
        }

        public static string ModPath(Dictionary<string, object> profile, params string[] parts)
        {
            string root = Normalize(GetStringValue(profile, "mod_root"));
            return CombinePath(root, parts);
        }

        public static string BackupPath(Dictionary<string, object> profile, params string[] parts)
        {
            string root = Normalize(GetStringValue(profile, "backup_root"));
            return CombinePath(root, parts);
        }

        public static bool Exists(string path)
        {
            string normalizedPath = Normalize(path);
            return File.Exists(normalizedPath) || Directory.Exists(normalizedPath);
        }

        public static string ResolveRelative(string @base, string rel)
        {
            string normalizedBase = Normalize(@base);
            return Normalize(Path.Combine(normalizedBase, rel));
        }

        private static string GetStringValue(Dictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var value) && value is string strValue)
            {
                return strValue;
            }
            return string.Empty;
        }

        private static string CombinePath(string root, string[] parts)
        {
            if (string.IsNullOrEmpty(root))
            {
                return string.Join(Path.DirectorySeparatorChar.ToString(), parts);
            }

            return Normalize(Path.Combine(root, string.Join(Path.DirectorySeparatorChar.ToString(), parts)));
        }
    }
}
