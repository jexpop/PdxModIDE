using System.Text.RegularExpressions;

namespace PdxModIDE.Domain.Interfaces
{
    public interface IGamePlugin
    {
        string GameKey { get; }
        string DisplayName { get; }

        bool CanHandleGame(string gameRoot);

        string DefinesRelativePath { get; }
        string LandedTitlesRelativePath { get; }
        string TitleHistoryRelativePath { get; }
        string MapDataRelativePath { get; }

        string EndDateKey { get; }
        string DefinesOutputFormat { get; }
        Regex DefinesDateRegex { get; }

        string[] DateProcessExtensions { get; }
        Regex DateRegex { get; }

        string ProvinceMapFileName { get; }
        string DefinitionFileName { get; }
        string DefaultMapFileName { get; }

        string[] TitlePrefixes { get; }
        Regex TitleRegex { get; }
        Regex HolderRegex { get; }
        Regex LiegeRegex { get; }
        Regex TitleHistoryDateRegex { get; }

        string[] TerrainTypeKeys { get; }
    }
}
