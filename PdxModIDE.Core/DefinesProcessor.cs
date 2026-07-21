using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using PdxModIDE.Core.Games;
using PdxModIDE.Domain.Interfaces;
using PdxModIDE.IO;

namespace PdxModIDE.Core
{
    public static class DefinesProcessor
    {
        private static IGamePlugin? GetPlugin(string gameKey)
        {
            return GameRegistry.GetPlugin(gameKey);
        }

        public static string? ReadEndDate(string gameRoot, string gameKey)
        {
            var plugin = GetPlugin(gameKey);
            if (plugin == null)
                return null;

            string definesPath = plugin.GetDefinesPath(gameRoot);

            if (!FileOperations.FileExists(definesPath))
                return null;

            var lines = FileOperations.ReadTextLines(definesPath);

            foreach (var line in lines)
            {
                if (line.Contains(plugin.EndDateKey))
                {
                    var match = plugin.DefinesDateRegex.Match(line);
                    if (match.Success)
                        return match.Groups[1].Value;
                }
            }

            return null;
        }

        public static string? ReadModEndDate(string modRoot, string gameKey)
        {
            var plugin = GetPlugin(gameKey);
            if (plugin == null)
                return null;

            string definesPath = plugin.GetModDefinesPath(modRoot);

            if (!FileOperations.FileExists(definesPath))
                return null;

            var lines = FileOperations.ReadTextLines(definesPath);

            foreach (var line in lines)
            {
                if (line.Contains(plugin.EndDateKey))
                {
                    var match = plugin.DefinesDateRegex.Match(line);
                    if (match.Success)
                        return match.Groups[1].Value;
                }
            }

            return null;
        }

        public static bool WriteEndDate(string gameRoot, string modRoot, string backupRoot, string newDate, string gameKey)
        {
            var plugin = GetPlugin(gameKey);
            if (plugin == null)
                return false;

            string src = plugin.GetDefinesPath(gameRoot);
            string dstMod = plugin.GetModDefinesPath(modRoot);
            string dstBackup = plugin.GetBackupDefinesPath(backupRoot);

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
                if (line.Contains(plugin.EndDateKey))
                {
                    newLines.Add(string.Format(plugin.DefinesOutputFormat, newDate));
                    replaced = true;
                }
                else
                {
                    newLines.Add(line);
                }
            }

            if (!replaced)
            {
                newLines.Add(string.Format(plugin.DefinesOutputFormat, newDate));
            }

            if (FileOperations.FileExists(dstMod))
            {
                var existingLines = FileOperations.ReadTextLines(dstMod);
                if (existingLines.Length == newLines.Count)
                {
                    bool same = true;
                    for (int i = 0; i < newLines.Count; i++)
                    {
                        if (existingLines[i] != newLines[i])
                        {
                            same = false;
                            break;
                        }
                    }
                    if (same)
                        return true;
                }
            }

            FileOperations.RenameExistingFile(dstMod);
            File.WriteAllLines(dstMod, newLines, System.Text.Encoding.UTF8);

            return true;
        }
    }
}
