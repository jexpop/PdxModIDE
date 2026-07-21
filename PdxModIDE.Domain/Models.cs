using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PdxModIDE.Domain
{
    public class Module
    {
        public string Name { get; }
        public string Path { get; }
        public IReadOnlyList<string> IgnoreExtensions { get; }

        public Module(string name, string path, IEnumerable<string> ignoreExtensions)
        {
            Name = name;
            Path = path;
            IgnoreExtensions = new ReadOnlyCollection<string>(new List<string>(ignoreExtensions));
        }
    }

    public class GameFile
    {
        public string Name { get; }
        public string Path { get; }
        public string? MapTo { get; }

        public GameFile(string name, string path, string? mapTo = null)
        {
            Name = name;
            Path = path;
            MapTo = mapTo;
        }
    }

    public class Profile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string Game { get; set; } = "CK3";
        public string GameRoot { get; set; } = "";
        public string ModRoot { get; set; } = "";
        public string BackupRoot { get; set; } = "";
        public int YearOffset { get; set; } = 10000;
        public List<string> ModuleIds { get; set; } = new();
        public List<string> FileIds { get; set; } = new();
        public bool ShowTitleNames { get; set; } = true;
        public List<string> DatesModuleIds { get; set; } = new();

        public List<Module> SelectedModules { get; set; } = new();
        public List<GameFile> SelectedFiles { get; set; } = new();

        public IReadOnlyList<string> Modules => ModuleIds.AsReadOnly();
        public IReadOnlyList<string> Files => FileIds.AsReadOnly();

        public Profile() { }

        public Profile(string name, string game, string gameRoot, string modRoot,
                      string backupRoot, int yearOffset,
                      IEnumerable<string> modules, IEnumerable<string> files)
        {
            Name = name;
            Game = game;
            GameRoot = gameRoot;
            ModRoot = modRoot;
            BackupRoot = backupRoot;
            YearOffset = yearOffset;
            ModuleIds = new List<string>(modules);
            FileIds = new List<string>(files);
        }
    }

    public class EditingSession
    {
        public Profile CurrentProfile { get; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, Module>> ModulesByGame { get; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, GameFile>> FilesByGame { get; }

        private readonly Dictionary<string, Module> _allModulesByName;
        private readonly Dictionary<string, GameFile> _allFilesByName;

        public IReadOnlyDictionary<string, Module> AllModulesByName { get; }
        public IReadOnlyDictionary<string, GameFile> AllFilesByName { get; }

        public EditingSession(Profile currentProfile,
                              Dictionary<string, Dictionary<string, Module>> modulesByGame,
                              Dictionary<string, Dictionary<string, GameFile>> filesByGame)
        {
            CurrentProfile = currentProfile;
            ModulesByGame = ToReadOnlyNested(modulesByGame);
            FilesByGame = ToReadOnlyNested(filesByGame);

            _allModulesByName = modulesByGame
                .SelectMany(g => g.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            _allFilesByName = filesByGame
                .SelectMany(g => g.Value)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            AllModulesByName = _allModulesByName.AsReadOnly();
            AllFilesByName = _allFilesByName.AsReadOnly();

            ResolveRealReferences();
        }

        private void ResolveRealReferences()
        {
            CurrentProfile.SelectedModules.Clear();
            foreach (var moduleName in CurrentProfile.ModuleIds)
            {
                if (_allModulesByName.TryGetValue(moduleName, out var module))
                    CurrentProfile.SelectedModules.Add(module);
            }

            CurrentProfile.SelectedFiles.Clear();
            foreach (var fileName in CurrentProfile.FileIds)
            {
                if (_allFilesByName.TryGetValue(fileName, out var file))
                    CurrentProfile.SelectedFiles.Add(file);
            }
        }

        public Module? GetModule(string name)
        {
            return _allModulesByName.TryGetValue(name, out var m) ? m : null;
        }

        public GameFile? GetFile(string name)
        {
            return _allFilesByName.TryGetValue(name, out var f) ? f : null;
        }

        public IReadOnlyDictionary<string, Module> GetModulesForGame(string gameKey)
        {
            return ModulesByGame.TryGetValue(gameKey, out var modules)
                ? modules
                : new ReadOnlyDictionary<string, Module>(new Dictionary<string, Module>());
        }

        public IReadOnlyDictionary<string, GameFile> GetFilesForGame(string gameKey)
        {
            return FilesByGame.TryGetValue(gameKey, out var files)
                ? files
                : new ReadOnlyDictionary<string, GameFile>(new Dictionary<string, GameFile>());
        }

        public void RefreshSelectedReferences()
        {
            ResolveRealReferences();
        }

        public void SetSelectedModules(IEnumerable<Module> modules)
        {
            CurrentProfile.SelectedModules.Clear();
            CurrentProfile.ModuleIds.Clear();
            foreach (var m in modules)
            {
                CurrentProfile.SelectedModules.Add(m);
                CurrentProfile.ModuleIds.Add(m.Name);
            }
        }

        public void SetSelectedFiles(IEnumerable<GameFile> files)
        {
            CurrentProfile.SelectedFiles.Clear();
            CurrentProfile.FileIds.Clear();
            foreach (var f in files)
            {
                CurrentProfile.SelectedFiles.Add(f);
                CurrentProfile.FileIds.Add(f.Name);
            }
        }

        private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, T>> ToReadOnlyNested<T>(
            Dictionary<string, Dictionary<string, T>> source)
        {
            return new ReadOnlyDictionary<string, IReadOnlyDictionary<string, T>>(
                source.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new ReadOnlyDictionary<string, T>(
                        new Dictionary<string, T>(kvp.Value)) as IReadOnlyDictionary<string, T>
                ));
        }
    }
}
