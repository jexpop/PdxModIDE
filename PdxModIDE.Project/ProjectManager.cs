using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PdxModIDE.Core;
using PdxModIDE.Core.Games;
using PdxModIDE.Domain;
using PdxModIDE.Domain.Interfaces;
using DataProfile = PdxModIDE.Data.Profile;
using DataModuleConfig = PdxModIDE.Data.ModuleConfig;
using DataFileConfig = PdxModIDE.Data.FileConfig;
using DataSettings = PdxModIDE.Data.Settings;
using DataLogFilters = PdxModIDE.Data.LogFilters;
using PdxModIDE.Validation;
using PdxModIDE.Data;

namespace PdxModIDE.Project
{
    public class ProjectManager : IProjectService
    {
        private List<DataProfile> _dataProfiles = new();
        private Dictionary<string, Dictionary<string, DataModuleConfig>> _dataModules = new();
        private Dictionary<string, Dictionary<string, DataFileConfig>> _dataFiles = new();
        private DataSettings _dataSettings = new();
        private DataLogFilters _dataLogFilters = new();
        private readonly ModuleProcessor _moduleProcessor;

        public DataProfile? CurrentDataProfile { get; private set; }

        public IReadOnlyList<Domain.Profile> Profiles => _domainProfiles.AsReadOnly();
        public Domain.Profile? CurrentProfile { get; private set; }
        public EditingSession? CurrentSession { get; private set; }

        private List<Domain.Profile> _domainProfiles = new();

        public string Theme
        {
            get => _dataSettings.Theme;
            set => _dataSettings.Theme = value;
        }

        public string Language
        {
            get => _dataSettings.Language;
            set => _dataSettings.Language = value;
        }

        public ProjectManager()
        {
            _moduleProcessor = new ModuleProcessor(new ModuleRepository());
        }

        public void Load()
        {
            _dataProfiles = DataLoader.LoadProfiles();
            _dataModules = DataLoader.LoadModules();
            _dataFiles = DataLoader.LoadFiles();
            _dataSettings = DataLoader.LoadSettings();
            _dataLogFilters = DataLoader.LoadLogFilters();
            SyncDomainProfiles();
        }

        public string? DetectGame(string gameRoot)
        {
            if (string.IsNullOrEmpty(gameRoot) || !Directory.Exists(gameRoot))
                return null;

            var plugin = GameRegistry.DetectGame(gameRoot);
            return plugin?.GameKey;
        }

        public string? DetectGameWithFallback(string gameRoot)
        {
            var detected = DetectGame(gameRoot);
            if (detected != null)
                return detected;

            // Show dialog for user to select game
            return ShowGameSelectionDialog();
        }

        private static string? ShowGameSelectionDialog()
        {
            var plugins = GameRegistry.AllPlugins.Values.OrderBy(p => p.DisplayName).ToList();
            if (!plugins.Any())
                return null;

            var form = new Form
            {
                Text = "Seleccionar juego base",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false,
                Width = 400,
                Height = 300
            };

            var listBox = new ListBox
            {
                Dock = DockStyle.Fill,
                DisplayMember = "DisplayName"
            };
            listBox.Items.AddRange(plugins.Cast<object>().ToArray());

            var panel = new Panel { Dock = DockStyle.Bottom, Height = 50 };
            var btnOk = new Button { Text = "Aceptar", DialogResult = DialogResult.OK, Dock = DockStyle.Right, Width = 80 };
            var btnCancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Dock = DockStyle.Right, Width = 80 };
            panel.Controls.Add(btnOk);
            panel.Controls.Add(btnCancel);
            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

            form.Controls.Add(listBox);
            form.Controls.Add(panel);

            if (form.ShowDialog() == DialogResult.OK && listBox.SelectedItem is IGamePlugin selected)
            {
                return selected.GameKey;
            }

            return null;
        }

        public void SaveAll()
        {
            DataLoader.SaveProfiles(_dataProfiles);
            DataLoader.SaveModules(_dataModules);
            DataLoader.SaveFiles(_dataFiles);
            DataLoader.SaveSettings(_dataSettings);
            DataLoader.SaveLogFilters(_dataLogFilters);
        }

        public void SaveSettings()
        {
            DataLoader.SaveSettings(_dataSettings);
        }

        public bool SelectProfile(string name)
        {
            var dataProfile = _dataProfiles.FirstOrDefault(p => p.Name == name);
            if (dataProfile != null)
            {
                CurrentDataProfile = dataProfile;
                CurrentProfile = _domainProfiles.FirstOrDefault(p => p.Name == name);
                CurrentSession = BuildSession(CurrentProfile);
                return true;
            }
            return false;
        }

        public Domain.Profile CreateProfile(string name, string game = "CK3")
        {
            var dataProfile = new DataProfile
            {
                Name = name,
                Game = game,
                GameRoot = "",
                ModRoot = "",
                BackupRoot = "",
                YearOffset = 10000,
                Modules = new List<string>(),
                DatesModules = new List<string>(),
                Files = new List<string>(),
                ShowTitleNames = true
            };

            _dataProfiles.Add(dataProfile);
            var domainProfile = MapToDomain(dataProfile);
            _domainProfiles.Add(domainProfile);
            DataLoader.SaveProfiles(_dataProfiles);
            return domainProfile;
        }

        public Domain.Profile CreateProfileWithGameDetection(string name, string gameRoot)
        {
            string game = "CK3";
            if (!string.IsNullOrEmpty(gameRoot))
            {
                var detected = DetectGameWithFallback(gameRoot);
                if (detected != null)
                    game = detected;
            }

            return CreateProfile(name, game);
        }

        public bool UpdateProfile(Domain.Profile profile)
        {
            var dataProfile = _dataProfiles.FirstOrDefault(p => p.Name == profile.Name);
            if (dataProfile != null)
            {
                MapToData(profile, dataProfile);
                var index = _dataProfiles.IndexOf(dataProfile);
                _dataProfiles[index] = dataProfile;
                if (CurrentProfile?.Name == profile.Name)
                {
                    CurrentDataProfile = dataProfile;
                    CurrentProfile = profile;
                }
                DataLoader.SaveProfiles(_dataProfiles);
                return true;
            }
            return false;
        }

        public bool DeleteProfile(string name)
        {
            var dataProfile = _dataProfiles.FirstOrDefault(p => p.Name == name);
            if (dataProfile != null)
            {
                _dataProfiles.Remove(dataProfile);
                _domainProfiles.RemoveAll(p => p.Name == name);
                if (CurrentProfile?.Name == name)
                {
                    CurrentProfile = null;
                    CurrentDataProfile = null;
                    CurrentSession = null;
                }
                DataLoader.SaveProfiles(_dataProfiles);
                return true;
            }
            return false;
        }

        public bool RenameProfile(string oldName, string newName)
        {
            var dataProfile = _dataProfiles.FirstOrDefault(p => p.Name == oldName);
            if (dataProfile != null && !_dataProfiles.Any(p => p.Name == newName))
            {
                dataProfile.Name = newName;
                var domainProfile = _domainProfiles.FirstOrDefault(p => p.Name == oldName);
                if (domainProfile != null)
                    domainProfile.Name = newName;
                DataLoader.SaveProfiles(_dataProfiles);
                return true;
            }
            return false;
        }

        public async Task ProcessModulesAsync(int? offsetOverride = null)
        {
            if (CurrentDataProfile == null)
                return;

            int offset = offsetOverride ?? CurrentDataProfile.YearOffset;

            string logPath = Path.Combine("logs", CurrentDataProfile.Name);
            IO.FileOperations.EnsureDirectory(logPath);

            await _moduleProcessor.ProcessModulesAsync(
                CurrentDataProfile.Game,
                CurrentDataProfile.DatesModules,
                CurrentDataProfile.GameRoot,
                CurrentDataProfile.ModRoot,
                CurrentDataProfile.BackupRoot,
                offset,
                CurrentDataProfile.Name
            );
        }

        public Dictionary<string, DataModuleConfig> GetGameModules(string gameKey)
        {
            return _dataModules.TryGetValue(gameKey, out var modules)
                ? modules
                : new Dictionary<string, DataModuleConfig>();
        }

        public Dictionary<string, DataFileConfig> GetGameFiles(string gameKey)
        {
            return _dataFiles.TryGetValue(gameKey, out var files)
                ? files
                : new Dictionary<string, DataFileConfig>();
        }

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, Domain.Module>> GetAllModules()
        {
            var result = new Dictionary<string, IReadOnlyDictionary<string, Domain.Module>>();
            foreach (var kvp in _dataModules)
            {
                var modules = new Dictionary<string, Domain.Module>();
                foreach (var m in kvp.Value)
                    modules[m.Key] = new Domain.Module(m.Key, m.Value.Path, m.Value.IgnoreExt);
                result[kvp.Key] = modules.AsReadOnly();
            }
            return result.AsReadOnly();
        }

        public IReadOnlyDictionary<string, Domain.Module> GetGameModulesAsDomain(string gameKey)
        {
            if (!_dataModules.TryGetValue(gameKey, out var configs))
                return new Dictionary<string, Domain.Module>().AsReadOnly();

            var modules = new Dictionary<string, Domain.Module>();
            foreach (var kvp in configs)
                modules[kvp.Key] = new Domain.Module(kvp.Key, kvp.Value.Path, kvp.Value.IgnoreExt);
            return modules.AsReadOnly();
        }

        public void AddModule(string gameKey, string moduleName, string path, List<string> ignoreExt)
        {
            if (!_dataModules.ContainsKey(gameKey))
                _dataModules[gameKey] = new Dictionary<string, DataModuleConfig>();

            _dataModules[gameKey][moduleName] = new DataModuleConfig
            {
                Path = path,
                IgnoreExt = ignoreExt
            };
            DataLoader.SaveModules(_dataModules);
        }

        public bool UpdateModule(string gameKey, string moduleName, string path, List<string> ignoreExt)
        {
            if (_dataModules.ContainsKey(gameKey) && _dataModules[gameKey].ContainsKey(moduleName))
            {
                _dataModules[gameKey][moduleName] = new DataModuleConfig
                {
                    Path = path,
                    IgnoreExt = ignoreExt
                };
                DataLoader.SaveModules(_dataModules);
                return true;
            }
            return false;
        }

        public bool DeleteModule(string gameKey, string moduleName)
        {
            if (_dataModules.ContainsKey(gameKey) && _dataModules[gameKey].ContainsKey(moduleName))
            {
                _dataModules[gameKey].Remove(moduleName);
                DataLoader.SaveModules(_dataModules);
                return true;
            }
            return false;
        }

        public string? ReadEndDate(string gameRoot)
        {
            if (CurrentDataProfile == null)
                return null;
            return DefinesProcessor.ReadEndDate(gameRoot, CurrentDataProfile.Game);
        }

        public string? ReadModEndDate(string modRoot)
        {
            if (CurrentDataProfile == null)
                return null;
            return DefinesProcessor.ReadModEndDate(modRoot, CurrentDataProfile.Game);
        }

        public bool WriteEndDate(string newDate)
        {
            if (CurrentDataProfile == null)
                return false;

            return DefinesProcessor.WriteEndDate(
                CurrentDataProfile.GameRoot,
                CurrentDataProfile.ModRoot,
                CurrentDataProfile.BackupRoot,
                newDate,
                CurrentDataProfile.Game
            );
        }

        public async Task<List<ModuleValidationResult>> ValidateAllAsync()
        {
            var results = new List<ModuleValidationResult>();
            if (CurrentDataProfile == null) return results;

            await Task.Run(() =>
            {
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };

                var lockObj = new object();

                Parallel.ForEach(CurrentDataProfile.Modules, options, moduleName =>
                {
                    if (!_dataModules.TryGetValue(CurrentDataProfile.Game, out var modules))
                        return;
                    if (!modules.TryGetValue(moduleName, out var config))
                        return;

                    string rel = config.Path;
                    var ignoreExt = new HashSet<string>(config.IgnoreExt ?? new List<string>());

                    string gameDir = Path.Combine(CurrentDataProfile.GameRoot, rel);
                    string modDir = Path.Combine(CurrentDataProfile.ModRoot, rel);
                    string backupDir = Path.Combine(CurrentDataProfile.BackupRoot, rel);

                    var filesMod = ModuleValidator.ListFilesRecursive(modDir);
                    var filesGame = ModuleValidator.ListFilesRecursive(gameDir);
                    var filesBackup = ModuleValidator.ListFilesRecursive(backupDir);

                    var allFiles = new HashSet<string>(filesMod);
                    allFiles.UnionWith(filesGame);
                    allFiles.UnionWith(filesBackup);
                    var sortedFiles = allFiles.OrderBy(f => f).ToList();

                    var modVsBackup = new List<FileComparisonResult>();
                    var gameVsBackup = new List<FileComparisonResult>();

                    int modEqual = 0, modChanged = 0, modOnly = 0, backupOnlyMod = 0;
                    int gameEqual = 0, gameChanged = 0, gameOnly = 0, backupOnlyGame = 0;

                    foreach (var f in sortedFiles)
                    {
                        string ext = Path.GetExtension(f).ToLower();
                        if (ignoreExt.Contains(ext)) continue;

                        string modPath = Path.Combine(modDir, f);
                        string gamePath = Path.Combine(gameDir, f);
                        string backupPath = Path.Combine(backupDir, f);

                        if (!File.Exists(modPath))
                        {
                            if (File.Exists(backupPath))
                            {
                                modVsBackup.Add(new FileComparisonResult
                                {
                                    RelativePath = f,
                                    Status = "Deleted",
                                    DiffLines = null
                                });
                                backupOnlyMod++;
                            }
                        }
                        else if (!File.Exists(backupPath))
                        {
                            modVsBackup.Add(new FileComparisonResult
                            {
                                RelativePath = f,
                                Status = "Added",
                                DiffLines = null
                            });
                            modOnly++;
                        }
                        else
                        {
                            var (same, diff) = ModuleValidator.CompareFileContents(modPath, backupPath);
                            if (same)
                            {
                                modEqual++;
                            }
                            else
                            {
                                modVsBackup.Add(new FileComparisonResult
                                {
                                    RelativePath = f,
                                    Status = "Modified",
                                    DiffLines = diff
                                });
                                modChanged++;
                            }
                        }

                        if (!File.Exists(gamePath))
                        {
                            if (File.Exists(backupPath))
                            {
                                gameVsBackup.Add(new FileComparisonResult
                                {
                                    RelativePath = f,
                                    Status = "Deleted",
                                    DiffLines = null
                                });
                                backupOnlyGame++;
                            }
                        }
                        else if (!File.Exists(backupPath))
                        {
                            gameVsBackup.Add(new FileComparisonResult
                            {
                                RelativePath = f,
                                Status = "Added",
                                DiffLines = null
                            });
                            gameOnly++;
                        }
                        else
                        {
                            var (same, diff) = ModuleValidator.CompareFileContents(gamePath, backupPath);
                            if (same)
                            {
                                gameEqual++;
                            }
                            else
                            {
                                gameVsBackup.Add(new FileComparisonResult
                                {
                                    RelativePath = f,
                                    Status = "Modified",
                                    DiffLines = diff
                                });
                                gameChanged++;
                            }
                        }
                    }

                    int totalMod = modEqual + modChanged + modOnly + backupOnlyMod;
                    int totalGame = gameEqual + gameChanged + gameOnly + backupOnlyGame;

                    var result = new ModuleValidationResult
                    {
                        ModuleName = moduleName,
                        ModVsBackupSummary =
                            $"{totalMod} files — {modChanged} modified, " +
                            $"{modEqual} identical, {modOnly} added, {backupOnlyMod} deleted",
                        GameVsBackupSummary =
                            $"{totalGame} files — {gameChanged} modified, " +
                            $"{gameEqual} identical, {gameOnly} added, {backupOnlyGame} deleted",
                        ModVsBackupDetails = modVsBackup,
                        GameVsBackupDetails = gameVsBackup
                    };

                    lock (lockObj)
                    {
                        results.Add(result);
                    }
                });
            });

            return results;
        }

        public List<FileComparisonResult> ValidateModuleSingle(string moduleName, ComparisonType comparison)
        {
            var results = new List<FileComparisonResult>();
            if (CurrentDataProfile == null) return results;

            if (!_dataModules.TryGetValue(CurrentDataProfile.Game, out var modules))
                return results;
            if (!modules.TryGetValue(moduleName, out var config))
                return results;

            string relPath = config.Path;
            string gameRoot = CurrentDataProfile.GameRoot;
            string modRoot = CurrentDataProfile.ModRoot;
            string backupRoot = CurrentDataProfile.BackupRoot;

            var (gameFiles, backupFiles) = ModuleValidator.CollectModuleFiles(gameRoot, backupRoot, relPath);
            var (modFiles, _) = ModuleValidator.CollectModuleFiles(modRoot, modRoot, relPath);

            Dictionary<string, string> left, right;
            if (comparison == ComparisonType.GameVsMod)
            {
                left = gameFiles; right = modFiles;
            }
            else if (comparison == ComparisonType.ModVsBackup)
            {
                left = modFiles; right = backupFiles;
            }
            else
            {
                left = gameFiles; right = backupFiles;
            }

            var allKeys = new HashSet<string>(left.Keys);
            allKeys.UnionWith(right.Keys);

            var ignoreExt = new HashSet<string>(config.IgnoreExt ?? new List<string>());

            foreach (var key in allKeys.OrderBy(k => k))
            {
                string ext = Path.GetExtension(key).ToLower();
                if (ignoreExt.Contains(ext)) continue;

                bool hasLeft = left.TryGetValue(key, out var lPath);
                bool hasRight = right.TryGetValue(key, out var rPath);

                string status;
                List<string>? diff = null;

                if (hasLeft && hasRight)
                {
                    var (same, lines) = ModuleValidator.CompareFileContents(lPath!, rPath!);
                    if (same) continue;
                    status = "Modified";
                    diff = lines;
                }
                else if (hasLeft && !hasRight)
                {
                    status = "Deleted";
                }
                else
                {
                    status = "Added";
                }

                results.Add(new FileComparisonResult
                {
                    RelativePath = key,
                    Status = status,
                    DiffLines = diff
                });
            }

            return results;
        }

        public (string Status, List<string>? Diff, string RelativePath) ValidateFileSingle(string fileKey, bool compareToGame)
        {
            if (CurrentDataProfile == null) return ("No profile", null, "");

            if (!_dataFiles.TryGetValue(CurrentDataProfile.Game, out var files))
                return ("No files", null, "");
            if (!files.TryGetValue(fileKey, out var config))
                return ("File not found", null, "");

            string relGame = config.Path;
            string relMod = config.MapTo ?? relGame;

            string gameRoot = CurrentDataProfile.GameRoot;
            string modRoot = CurrentDataProfile.ModRoot;
            string backupRoot = CurrentDataProfile.BackupRoot;

            string leftPath;
            string leftLabel;
            string relDisplay;

            if (compareToGame)
            {
                leftPath = Path.Combine(gameRoot, relGame);
                leftLabel = "GAME";
                relDisplay = relGame;
            }
            else
            {
                leftPath = Path.Combine(modRoot, relMod);
                leftLabel = "MOD";
                relDisplay = relMod;
            }

            string backupPath = Path.Combine(backupRoot, relGame);

            if (!File.Exists(leftPath))
                return ($"[+] ONLY IN {leftLabel}", null, relDisplay);
            if (!File.Exists(backupPath))
                return ("[-] ONLY IN BACKUP", null, relGame);

            var (same, diff) = ModuleValidator.CompareFileContents(leftPath, backupPath);
            if (same)
                return ("[=] SAME", null, relDisplay);
            else
                return ("[!] CHANGED", diff, relDisplay);
        }

        public async Task<List<FileValidationResult>> ValidateAllFilesAsync()
        {
            var results = new List<FileValidationResult>();
            if (CurrentDataProfile == null) return results;

            var fileKeys = new List<string>(CurrentDataProfile.Files);

            await Task.Run(() =>
            {
                var lockObj = new object();

                Parallel.ForEach(fileKeys, fileKey =>
                {
                    if (!_dataFiles.TryGetValue(CurrentDataProfile.Game, out var files))
                        return;
                    if (!files.TryGetValue(fileKey, out var config))
                        return;

                    string relGame = config.Path;
                    string relMod = config.MapTo ?? relGame;

                    string gamePath = Path.Combine(CurrentDataProfile.GameRoot, relGame);
                    string modPath = Path.Combine(CurrentDataProfile.ModRoot, relMod);
                    string backupPath = Path.Combine(CurrentDataProfile.BackupRoot, relGame);

                    string estadoMod;
                    List<string>? diffMod = null;

                    if (!File.Exists(modPath))
                        estadoMod = "[+] Only in GAME";
                    else if (!File.Exists(backupPath))
                        estadoMod = "[-] Only in BACKUP";
                    else
                    {
                        var (same, d) = ModuleValidator.CompareFileContents(modPath, backupPath);
                        estadoMod = same ? "[=] Same" : "[!] Changed";
                        diffMod = same ? null : d;
                    }

                    string estadoGame;
                    List<string>? diffGame = null;

                    if (!File.Exists(gamePath))
                        estadoGame = "[+] Only in MOD";
                    else if (!File.Exists(backupPath))
                        estadoGame = "[-] Only in BACKUP";
                    else
                    {
                        var (same, d) = ModuleValidator.CompareFileContents(gamePath, backupPath);
                        estadoGame = same ? "[=] Same" : "[!] Changed";
                        diffGame = same ? null : d;
                    }

                    var result = new FileValidationResult
                    {
                        FileKey = fileKey,
                        ModVsBackupStatus = estadoMod,
                        GameVsBackupStatus = estadoGame,
                        ModVsBackupDiff = diffMod,
                        GameVsBackupDiff = diffGame
                    };

                    lock (lockObj) { results.Add(result); }
                });
            });

            return results.OrderBy(r => r.FileKey).ToList();
        }

        public List<string> GetFileKeys()
        {
            if (CurrentDataProfile == null) return new List<string>();
            return _dataFiles.TryGetValue(CurrentDataProfile.Game, out var files)
                ? files.Keys.OrderBy(k => k).ToList()
                : new List<string>();
        }

        public string? GetFilePath(string fileKey)
        {
            if (CurrentDataProfile == null) return null;
            return _dataFiles.TryGetValue(CurrentDataProfile.Game, out var files) &&
                   files.TryGetValue(fileKey, out var config)
                ? config.Path : null;
        }

        public string? GetFileMapTo(string fileKey)
        {
            if (CurrentDataProfile == null) return null;
            return _dataFiles.TryGetValue(CurrentDataProfile.Game, out var files) &&
                   files.TryGetValue(fileKey, out var config)
                ? config.MapTo : null;
        }

        public void AddFile(string gameKey, string fileKey, string path, string? mapTo)
        {
            if (!_dataFiles.ContainsKey(gameKey))
                _dataFiles[gameKey] = new Dictionary<string, DataFileConfig>();

            _dataFiles[gameKey][fileKey] = new DataFileConfig
            {
                Path = path,
                MapTo = mapTo
            };
            DataLoader.SaveFiles(_dataFiles);
        }

        public bool ActivateFile(string fileKey)
        {
            if (CurrentDataProfile == null) return false;
            if (!CurrentDataProfile.Files.Contains(fileKey))
            {
                CurrentDataProfile.Files.Add(fileKey);
                DataLoader.SaveProfiles(_dataProfiles);
                SyncDomainProfiles();
                return true;
            }
            return false;
        }

        public bool DeactivateFile(string fileKey)
        {
            if (CurrentDataProfile == null) return false;
            if (CurrentDataProfile.Files.Remove(fileKey))
            {
                DataLoader.SaveProfiles(_dataProfiles);
                SyncDomainProfiles();
                return true;
            }
            return false;
        }

        public bool SetFileMapTo(string fileKey, string? mapTo)
        {
            if (CurrentDataProfile == null) return false;
            if (!_dataFiles.TryGetValue(CurrentDataProfile.Game, out var files))
                return false;
            if (!files.TryGetValue(fileKey, out var config))
                return false;

            config.MapTo = string.IsNullOrEmpty(mapTo) ? null : mapTo;
            DataLoader.SaveFiles(_dataFiles);
            return true;
        }

        private void SyncDomainProfiles()
        {
            _domainProfiles = _dataProfiles.Select(MapToDomain).ToList();
        }

        private Domain.Profile MapToDomain(DataProfile dp)
        {
            return new Domain.Profile
            {
                Id = dp.Name,
                Name = dp.Name,
                Game = dp.Game,
                GameRoot = dp.GameRoot,
                ModRoot = dp.ModRoot,
                BackupRoot = dp.BackupRoot,
                YearOffset = dp.YearOffset,
                ModuleIds = new List<string>(dp.Modules),
                DatesModuleIds = new List<string>(dp.DatesModules),
                FileIds = new List<string>(dp.Files),
                ShowTitleNames = dp.ShowTitleNames
            };
        }

        private static void MapToData(Domain.Profile domain, DataProfile data)
        {
            data.Name = domain.Name;
            data.Game = domain.Game;
            data.GameRoot = domain.GameRoot;
            data.ModRoot = domain.ModRoot;
            data.BackupRoot = domain.BackupRoot;
            data.YearOffset = domain.YearOffset;
            data.Modules = new List<string>(domain.ModuleIds);
            data.DatesModules = new List<string>(domain.DatesModuleIds);
            data.Files = new List<string>(domain.FileIds);
            data.ShowTitleNames = domain.ShowTitleNames;
        }

        private EditingSession? BuildSession(Domain.Profile? profile)
        {
            if (profile == null) return null;

            var modulesByGame = new Dictionary<string, Dictionary<string, Domain.Module>>();
            var filesByGame = new Dictionary<string, Dictionary<string, Domain.GameFile>>();

            foreach (var kvp in _dataModules)
            {
                var modules = new Dictionary<string, Domain.Module>();
                foreach (var m in kvp.Value)
                {
                    modules[m.Key] = new Domain.Module(m.Key, m.Value.Path, m.Value.IgnoreExt);
                }
                modulesByGame[kvp.Key] = modules;
            }

            foreach (var kvp in _dataFiles)
            {
                var files = new Dictionary<string, Domain.GameFile>();
                foreach (var f in kvp.Value)
                {
                    files[f.Key] = new Domain.GameFile(f.Key, f.Value.Path, f.Value.MapTo);
                }
                filesByGame[kvp.Key] = files;
            }

            return new EditingSession(profile, modulesByGame, filesByGame);
        }
    }
}
