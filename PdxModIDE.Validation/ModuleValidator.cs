using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PdxModIDE.Core;
using PdxModIDE.IO;

namespace PdxModIDE.Validation
{
    public static class ModuleValidator
    {
        public static (Dictionary<string, string> GameFiles, Dictionary<string, string> BackupFiles)
            CollectModuleFiles(string gameRoot, string backupRoot, string relativePath)
        {
            string srcGame = Path.Combine(gameRoot, relativePath);
            string srcBackup = Path.Combine(backupRoot, relativePath);

            var gameFiles = new Dictionary<string, string>();
            var backupFiles = new Dictionary<string, string>();

            if (Directory.Exists(srcGame))
            {
                foreach (var file in Directory.EnumerateFiles(srcGame, "*.*", SearchOption.TopDirectoryOnly))
                {
                    string rel = Path.GetRelativePath(srcGame, file);
                    gameFiles[rel] = file;
                }
            }

            if (Directory.Exists(srcBackup))
            {
                foreach (var file in Directory.EnumerateFiles(srcBackup, "*.*", SearchOption.TopDirectoryOnly))
                {
                    string rel = Path.GetRelativePath(srcBackup, file);
                    backupFiles[rel] = file;
                }
            }

            return (gameFiles, backupFiles);
        }

        public static (bool AreEqual, List<string> DiffLines) CompareFileContents(string gamePath, string backupPath)
        {
            var gameLines = ReadFileLines(gamePath);
            var backupLines = ReadFileLines(backupPath);

            if (gameLines.SequenceEqual(backupLines))
            {
                return (true, new List<string>());
            }

            var diff = new List<string>();
            diff.Add($"--- {gamePath}");
            diff.Add($"+++ {backupPath}");

            int i = 0, j = 0;
            int lookAhead = 20;
            while (i < gameLines.Count || j < backupLines.Count)
            {
                if (i < gameLines.Count && j < backupLines.Count && gameLines[i] == backupLines[j])
                {
                    diff.Add($" {gameLines[i]}");
                    i++; j++;
                }
                else if (i < gameLines.Count && j < backupLines.Count)
                {
                    int foundInGame = -1;
                    for (int k = 1; k <= lookAhead && i + k < gameLines.Count; k++)
                    {
                        if (gameLines[i + k] == backupLines[j])
                        { foundInGame = i + k; break; }
                    }

                    int foundInBackup = -1;
                    for (int k = 1; k <= lookAhead && j + k < backupLines.Count; k++)
                    {
                        if (backupLines[j + k] == gameLines[i])
                        { foundInBackup = j + k; break; }
                    }

                    if (foundInGame >= 0 && (foundInBackup < 0 || (foundInGame - i) <= (foundInBackup - j)))
                    {
                        diff.Add($"-{gameLines[i]}");
                        i++;
                    }
                    else if (foundInBackup >= 0)
                    {
                        diff.Add($"+{backupLines[j]}");
                        j++;
                    }
                    else
                    {
                        diff.Add($"-{gameLines[i]}");
                        diff.Add($"+{backupLines[j]}");
                        i++; j++;
                    }
                }
                else if (i < gameLines.Count)
                {
                    diff.Add($"-{gameLines[i]}");
                    i++;
                }
                else
                {
                    diff.Add($"+{backupLines[j]}");
                    j++;
                }
            }

            return (false, diff);
        }

        public static List<string> ReadFileLines(string path)
        {
            return FileOperations.ReadTextLines(path).ToList();
        }

        public static List<string> ListFiles(string basePath)
        {
            var result = new List<string>();
            if (!Directory.Exists(basePath))
                return result;

            foreach (var file in Directory.EnumerateFiles(basePath, "*.*", SearchOption.TopDirectoryOnly))
            {
                string rel = Path.GetRelativePath(basePath, file).Replace("\\", "/");
                result.Add(rel);
            }

            return result;
        }
    }

    public enum ComparisonType
    {
        GameVsMod,
        ModVsBackup,
        GameVsBackup
    }

    public class FileComparisonResult
    {
        public string RelativePath { get; set; } = "";
        public string Status { get; set; } = "";
        public List<string>? DiffLines { get; set; }
    }

    public class ModuleValidationResult
    {
        public string ModuleName { get; set; } = "";
        public string ModVsBackupSummary { get; set; } = "";
        public string GameVsBackupSummary { get; set; } = "";
        public List<FileComparisonResult> ModVsBackupDetails { get; set; } = new();
        public List<FileComparisonResult> GameVsBackupDetails { get; set; } = new();
    }

    public class FileValidationResult
    {
        public string FileKey { get; set; } = "";
        public string ModVsBackupStatus { get; set; } = "";
        public string GameVsBackupStatus { get; set; } = "";
        public List<string>? ModVsBackupDiff { get; set; }
        public List<string>? GameVsBackupDiff { get; set; }
    }
}
