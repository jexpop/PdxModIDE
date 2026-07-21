using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PdxModIDE.Core.Games;
using PdxModIDE.Data;
using PdxModIDE.Domain;
using PdxModIDE.Domain.Interfaces;
using PdxModIDE.IO;
using SystemFile = System.IO.File;

namespace PdxModIDE.Core
{
    public class ModuleProcessor
    {
        private readonly IModuleRepository _moduleRepository;
        private Dictionary<string, Dictionary<string, Module>>? _moduleCache;
        private readonly object _cacheLock = new();

        public ModuleProcessor(IModuleRepository moduleRepository)
        {
            _moduleRepository = moduleRepository ?? throw new ArgumentNullException(nameof(moduleRepository));
        }

        private Dictionary<string, Dictionary<string, Module>> LoadModules()
        {
            if (_moduleCache != null)
                return _moduleCache;

            lock (_cacheLock)
            {
                if (_moduleCache != null)
                    return _moduleCache;

                _moduleCache = _moduleRepository.GetAllAsync().GetAwaiter().GetResult();
                return _moduleCache;
            }
        }

        public void InvalidateCache()
        {
            lock (_cacheLock)
            {
                _moduleCache = null;
            }
        }

        private static IGamePlugin? GetPlugin(string gameKey)
        {
            return GameRegistry.GetPlugin(gameKey);
        }

        public void ProcessModule(string gameKey, string moduleName, string gameRoot, string modRoot, string backupRoot, int offset, string profileName)
        {
            var modules = LoadModules();
            if (!modules.ContainsKey(gameKey) || !modules[gameKey].ContainsKey(moduleName))
                return;

            var plugin = GetPlugin(gameKey);
            if (plugin == null)
                return;

            var config = modules[gameKey][moduleName];
            string relPath = config.Path;
            List<string> ignoreExt = config.IgnoreExtensions.ToList();

            string src = Path.Combine(gameRoot, relPath);
            string dstMod = Path.Combine(modRoot, relPath);
            string dstBackup = Path.Combine(backupRoot, relPath);

            FileOperations.EnsureDirectory(dstBackup);

            string logPath = Path.Combine("logs", profileName);
            FileOperations.EnsureDirectory(logPath);

            string logFile = Path.Combine(logPath, $"{moduleName}.log");
            var logDir = Path.GetDirectoryName(logFile);
            if (logDir != null)
                FileOperations.EnsureDirectory(logDir);

            using (StreamWriter log = SystemFile.AppendText(logFile))
            {
                log.WriteLine($"\n--- Processed {moduleName} --- {DateTime.Now}");

                if (!Directory.Exists(src))
                {
                    log.WriteLine($"ERROR: Game folder does not exist: {src}");
                    return;
                }

                foreach (var file in Directory.EnumerateFiles(src, "*.*", SearchOption.AllDirectories))
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ignoreExt.Contains(ext))
                        continue;

                    string rel = Path.GetRelativePath(src, file);
                    string fullMod = Path.Combine(dstMod, rel);
                    string fullBackup = Path.Combine(dstBackup, rel);

                    FileOperations.EnsureDirectory(Path.GetDirectoryName(fullBackup)!);
                    FileOperations.CopyFilePreserveTimestamps(file, fullBackup);

                    if (plugin.IsDateProcessableExtension(ext))
                    {
                        string original = FileOperations.ReadTextFile(file);
                        string processed = ApplyOffset(original, offset, plugin);

                        if (processed != original)
                        {
                            FileOperations.EnsureDirectory(Path.GetDirectoryName(fullMod)!);

                            if (SystemFile.Exists(fullMod) && FileOperations.ReadTextFile(fullMod) == processed)
                            {
                                log.WriteLine($"No date change (same as mod): {rel}");
                            }
                            else
                            {
                                FileOperations.RenameExistingFile(fullMod);
                                SystemFile.WriteAllText(fullMod, processed, Encoding.UTF8);
                                log.WriteLine($"Processed (date changed): {rel}");
                            }
                        }
                        else
                        {
                            log.WriteLine($"No date change (not copied to mod): {rel}");
                        }
                    }
                    else
                    {
                        FileOperations.EnsureDirectory(Path.GetDirectoryName(fullMod)!);

                        if (SystemFile.Exists(fullMod) && FileOperations.FilesAreEqual(file, fullMod))
                        {
                            log.WriteLine($"No change (same as mod): {rel}");
                        }
                        else
                        {
                            FileOperations.RenameExistingFile(fullMod);
                            FileOperations.CopyFilePreserveTimestamps(file, fullMod);
                            log.WriteLine($"Copied (no change): {rel}");
                        }
                    }
                }
            }
        }

        public string ApplyOffset(string text, int offset, IGamePlugin plugin)
        {
            return plugin.DateRegex.Replace(text, match =>
            {
                int year = int.Parse(match.Groups[1].Value) + offset;
                return $"{year}.{match.Groups[2].Value}.{match.Groups[3].Value}";
            });
        }

        public string ApplyOffsetToFile(string path, int offset, string gameKey)
        {
            var plugin = GetPlugin(gameKey);
            if (plugin == null)
                return FileOperations.ReadTextFile(path);

            string text = FileOperations.ReadTextFile(path);
            return ApplyOffset(text, offset, plugin);
        }

        public async Task ProcessModulesAsync(string gameKey, IEnumerable<string> moduleNames, string gameRoot, string modRoot, string backupRoot, int offset, string profileName)
        {
            var modules = LoadModules();
            if (!modules.ContainsKey(gameKey))
                return;

            var gameModules = modules[gameKey];

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            Parallel.ForEach(moduleNames, options, moduleName =>
            {
                if (!gameModules.ContainsKey(moduleName))
                    return;

                ProcessModule(gameKey, moduleName, gameRoot, modRoot, backupRoot, offset, profileName);
            });

            await Task.CompletedTask;
        }
    }
}
