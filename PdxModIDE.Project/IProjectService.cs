using System.Collections.Generic;
using System.Threading.Tasks;
using PdxModIDE.Domain;
using PdxModIDE.Validation;

namespace PdxModIDE.Project
{
    public interface IProjectService
    {
        void Load();
        void SaveAll();

        IReadOnlyList<Domain.Profile> Profiles { get; }
        Domain.Profile? CurrentProfile { get; }
        bool SelectProfile(string name);
        Domain.Profile CreateProfile(string name, string game = "CK3");
        Domain.Profile CreateProfileWithGameDetection(string name, string gameRoot);
        bool UpdateProfile(Domain.Profile profile);
        bool DeleteProfile(string name);
        bool RenameProfile(string oldName, string newName);

        EditingSession? CurrentSession { get; }

        string? DetectGame(string gameRoot);
        string? DetectGameWithFallback(string gameRoot);

        IReadOnlyDictionary<string, IReadOnlyDictionary<string, Domain.Module>> GetAllModules();
        IReadOnlyDictionary<string, Domain.Module> GetGameModulesAsDomain(string gameKey);
        void AddModule(string gameKey, string moduleName, string path, List<string> ignoreExt);
        bool UpdateModule(string gameKey, string moduleName, string path, List<string> ignoreExt);
        bool DeleteModule(string gameKey, string moduleName);

        Task ProcessModulesAsync(int? offsetOverride = null);

        Task<List<ModuleValidationResult>> ValidateAllAsync();
        List<FileComparisonResult> ValidateModuleSingle(string moduleName, ComparisonType comparison);

        (string Status, List<string>? Diff, string RelativePath) ValidateFileSingle(string fileKey, bool compareToGame);
        Task<List<FileValidationResult>> ValidateAllFilesAsync();

        List<string> GetFileKeys();
        string? GetFilePath(string fileKey);
        string? GetFileMapTo(string fileKey);
        void AddFile(string gameKey, string fileKey, string path, string? mapTo);
        bool ActivateFile(string fileKey);
        bool DeactivateFile(string fileKey);
        bool SetFileMapTo(string fileKey, string? mapTo);

        string? ReadEndDate(string gameRoot);
        string? ReadModEndDate(string modRoot);
        bool WriteEndDate(string newDate);

        string Theme { get; set; }
        void SaveSettings();
    }
}
