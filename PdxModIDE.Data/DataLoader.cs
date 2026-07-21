using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PdxModIDE.Data;
using PdxModIDE.IO;

namespace PdxModIDE.Data
{
    public static class DataLoader
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static Dictionary<string, Dictionary<string, ModuleConfig>> LoadModules()
        {
            string path = "data/modules.json";
            if (!FileOperations.FileExists(path))
                return new Dictionary<string, Dictionary<string, ModuleConfig>>();

            string json = FileOperations.ReadTextFile(path);
            return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, ModuleConfig>>>(json, JsonOptions)
                   ?? new Dictionary<string, Dictionary<string, ModuleConfig>>();
        }

        public static void SaveModules(Dictionary<string, Dictionary<string, ModuleConfig>> modules)
        {
            FileOperations.EnsureDirectory("data");
            string json = JsonSerializer.Serialize(modules, JsonOptions);
            FileOperations.WriteTextFile("data/modules.json", json);
        }

        public static Dictionary<string, Dictionary<string, FileConfig>> LoadFiles()
        {
            string path = "data/files.json";
            if (!FileOperations.FileExists(path))
                return new Dictionary<string, Dictionary<string, FileConfig>>();

            string json = FileOperations.ReadTextFile(path);
            return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, FileConfig>>>(json, JsonOptions)
                   ?? new Dictionary<string, Dictionary<string, FileConfig>>();
        }

        public static void SaveFiles(Dictionary<string, Dictionary<string, FileConfig>> files)
        {
            FileOperations.EnsureDirectory("data");
            string json = JsonSerializer.Serialize(files, JsonOptions);
            FileOperations.WriteTextFile("data/files.json", json);
        }

        public static List<Profile> LoadProfiles()
        {
            string path = "data/profiles.json";
            if (!FileOperations.FileExists(path))
                return new List<Profile>();

            try
            {
                string json = FileOperations.ReadTextFile(path);
                var profiles = JsonSerializer.Deserialize<List<ProfileDto>>(json, JsonOptions) ?? new List<ProfileDto>();

                foreach (var p in profiles)
                {
                    p.Modules ??= new List<string>();
                    p.DatesModules ??= new List<string>();
                    p.Files ??= new List<string>();
                }

                return profiles.ConvertAll(p => new Profile
                {
                    Name = p.Name,
                    Game = p.Game,
                    GameRoot = p.GameRoot,
                    ModRoot = p.ModRoot,
                    BackupRoot = p.BackupRoot,
                    YearOffset = p.YearOffset,
                    Modules = p.Modules,
                    DatesModules = p.DatesModules,
                    Files = p.Files,
                    ShowTitleNames = p.ShowTitleNames
                });
            }
            catch (JsonException)
            {
                return new List<Profile>();
            }
        }

        public static void SaveProfiles(List<Profile> profiles)
        {
            FileOperations.EnsureDirectory("data");
            var dtos = profiles.ConvertAll(p => new ProfileDto
            {
                Name = p.Name,
                Game = p.Game,
                GameRoot = p.GameRoot,
                ModRoot = p.ModRoot,
                BackupRoot = p.BackupRoot,
                YearOffset = p.YearOffset,
                Modules = p.Modules,
                DatesModules = p.DatesModules,
                Files = p.Files,
                ShowTitleNames = p.ShowTitleNames
            });
            string json = JsonSerializer.Serialize(dtos, JsonOptions);
            FileOperations.WriteTextFile("data/profiles.json", json);
        }

        public static Settings LoadSettings()
        {
            string path = "data/settings.json";
            if (!FileOperations.FileExists(path))
                return new Settings();

            try
            {
                string json = FileOperations.ReadTextFile(path);
                return JsonSerializer.Deserialize<Settings>(json, JsonOptions) ?? new Settings();
            }
            catch (JsonException)
            {
                return new Settings();
            }
        }

        public static void SaveSettings(Settings settings)
        {
            FileOperations.EnsureDirectory("data");
            string json = JsonSerializer.Serialize(settings, JsonOptions);
            FileOperations.WriteTextFile("data/settings.json", json);
        }

        public static LogFilters LoadLogFilters()
        {
            string path = "data/log_filters.json";
            if (!FileOperations.FileExists(path))
                return new LogFilters();

            try
            {
                string json = FileOperations.ReadTextFile(path);
                return JsonSerializer.Deserialize<LogFilters>(json, JsonOptions) ?? new LogFilters();
            }
            catch (JsonException)
            {
                return new LogFilters();
            }
        }

        public static void SaveLogFilters(LogFilters filters)
        {
            FileOperations.EnsureDirectory("data");
            string json = JsonSerializer.Serialize(filters, JsonOptions);
            FileOperations.WriteTextFile("data/log_filters.json", json);
        }
    }
}
