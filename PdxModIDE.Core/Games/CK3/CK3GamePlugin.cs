using System.Text.RegularExpressions;
using PdxModIDE.Domain.Interfaces;

namespace PdxModIDE.Core.Games.CK3
{
    public class CK3GamePlugin : IGamePlugin
    {
        public string GameKey => "CK3";
        public string DisplayName => "Crusader Kings III";

        public bool CanHandleGame(string gameRoot)
        {
            string definesPath = System.IO.Path.Combine(gameRoot, "common", "defines", "00_defines.txt");
            return System.IO.File.Exists(definesPath);
        }

        public string DefinesRelativePath => "common/defines/00_defines.txt";
        public string LandedTitlesRelativePath => "common/landed_titles";
        public string TitleHistoryRelativePath => "history/titles";
        public string MapDataRelativePath => "map_data";

        public string EndDateKey => "END_DATE";
        public string DefinesOutputFormat => "\tEND_DATE = \"{0}\"";
        public Regex DefinesDateRegex => new(@"""([^""]+)""");

        public string[] DateProcessExtensions => new[] { ".txt", ".yml" };
        public Regex DateRegex => new(@"\b(\d{3,4})\.(\d{1,2})\.(\d{1,2})\b");

        public string ProvinceMapFileName => "provinces.png";
        public string DefinitionFileName => "definition.csv";
        public string DefaultMapFileName => "default.map";

        public string[] TitlePrefixes => new[] { "b_", "c_", "d_", "k_", "e_" };
        public Regex TitleRegex => new(@"^\s*([becdk]_[A-Za-z0-9_]+)\s*=\s*\{");
        public Regex HolderRegex => new(@"holder\s*=\s*(\w+)");
        public Regex LiegeRegex => new(@"liege\s*=\s*(\w+)");
        public Regex TitleHistoryDateRegex => new(@"(\d+)\.(\d+)\.(\d+)");

        public string[] TerrainTypeKeys => new[]
        {
            "sea_zones", "lakes", "river_provinces",
            "impassable_mountains", "impassable_seas"
        };

        public string GetDefinesPath(string gameRoot)
            => System.IO.Path.Combine(gameRoot, DefinesRelativePath);

        public string GetModDefinesPath(string modRoot)
            => System.IO.Path.Combine(modRoot, DefinesRelativePath);

        public string GetBackupDefinesPath(string backupRoot)
            => System.IO.Path.Combine(backupRoot, DefinesRelativePath);

        public bool IsDateProcessableExtension(string extension)
        {
            foreach (var ext in DateProcessExtensions)
            {
                if (string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
