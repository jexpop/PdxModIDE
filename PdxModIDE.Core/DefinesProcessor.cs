using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using PdxModIDE.IO;

namespace PdxModIDE.Core
{
    public static class DefinesProcessor
    {
        public static string? ReadEndDate(string gameRoot)
        {
            string definesPath = Path.Combine(gameRoot, "common", "defines", "00_defines.txt");

            if (!FileOperations.FileExists(definesPath))
                return null;

            var lines = FileOperations.ReadTextLines(definesPath);

            foreach (var line in lines)
            {
                if (line.Contains("END_DATE"))
                {
                    var match = Regex.Match(line, @"""([^""]+)""");
                    if (match.Success)
                        return match.Groups[1].Value;
                }
            }

            return null;
        }

        public static string? ReadModEndDate(string modRoot)
        {
            string definesPath = Path.Combine(modRoot, "common", "defines", "00_defines.txt");

            if (!FileOperations.FileExists(definesPath))
                return null;

            var lines = FileOperations.ReadTextLines(definesPath);

            foreach (var line in lines)
            {
                if (line.Contains("END_DATE"))
                {
                    var match = Regex.Match(line, @"""([^""]+)""");
                    if (match.Success)
                        return match.Groups[1].Value;
                }
            }

            return null;
        }

        public static bool WriteEndDate(string gameRoot, string modRoot, string backupRoot, string newDate)
        {
            string src = Path.Combine(gameRoot, "common", "defines", "00_defines.txt");
            string dstMod = Path.Combine(modRoot, "common", "defines", "00_defines.txt");
            string dstBackup = Path.Combine(backupRoot, "common", "defines", "00_defines.txt");

            if (!FileOperations.FileExists(src))
                return false;

            FileOperations.EnsureDirectory(Path.GetDirectoryName(dstMod)!);
            FileOperations.EnsureDirectory(Path.GetDirectoryName(dstBackup)!);

            FileOperations.CopyFilePreserveTimestamps(src, dstBackup);

            var lines = FileOperations.ReadTextLines(src);
            var newLines = new List<string>();
            bool replaced = false;

            foreach (var line in lines)
            {
                if (line.Contains("END_DATE"))
                {
                    newLines.Add($"\tEND_DATE = \"{newDate}\"");
                    replaced = true;
                }
                else
                {
                    newLines.Add(line);
                }
            }

            if (!replaced)
            {
                newLines.Add($"\tEND_DATE = \"{newDate}\"");
            }

            File.WriteAllLines(dstMod, newLines, System.Text.Encoding.UTF8);

            return true;
        }
    }
}
