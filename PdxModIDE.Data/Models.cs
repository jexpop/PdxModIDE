using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PdxModIDE.Data
{
    public class ProfileDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("game")]
        public string Game { get; set; } = "CK3";

        [JsonPropertyName("game_root")]
        public string GameRoot { get; set; } = "";

        [JsonPropertyName("mod_root")]
        public string ModRoot { get; set; } = "";

        [JsonPropertyName("backup_root")]
        public string BackupRoot { get; set; } = "";

        [JsonPropertyName("year_offset")]
        public int YearOffset { get; set; } = 10000;

        [JsonPropertyName("modules")]
        public List<string> Modules { get; set; } = new();

        [JsonPropertyName("files")]
        public List<string> Files { get; set; } = new();
    }

    public class Profile
    {
        public string Name { get; set; } = "";
        public string Game { get; set; } = "CK3";
        public string GameRoot { get; set; } = "";
        public string ModRoot { get; set; } = "";
        public string BackupRoot { get; set; } = "";
        public int YearOffset { get; set; } = 10000;
        public List<string> Modules { get; set; } = new();
        public List<string> Files { get; set; } = new();
    }

    public class ModuleConfig
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = "";

        [JsonPropertyName("ignore_ext")]
        public List<string> IgnoreExt { get; set; } = new();
    }

    public class FileConfig
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = "";

        [JsonPropertyName("map_to")]
        public string? MapTo { get; set; }
    }

    public class Settings
    {
        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "light";
    }

    public class LogFilters
    {
        [JsonPropertyName("filters")]
        public List<string> Filters { get; set; } = new();
    }
}
