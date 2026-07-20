using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using PdxModIDE.Domain;
using PdxModIDE.Project;
using PdxModIDE.Validation;

namespace PdxModIDE.UI.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IProjectService _projectService;
        private Domain.Profile? _currentProfile;
        private string _theme = "light";
        private string _language = "en";

        public IProjectService ProjectService => _projectService;

        public MainViewModel(IProjectService projectService)
        {
            _projectService = projectService;
            _projectService.Load();
            Profiles = new ObservableCollection<Domain.Profile>(_projectService.Profiles);
            Modules = new ObservableCollection<ModuleViewModel>();
            Files = new ObservableCollection<FileViewModel>();

            BrowseGameRootCommand = new RelayCommand(_ => BrowseFolder("GameRoot"));
            BrowseModRootCommand = new RelayCommand(_ => BrowseFolder("ModRoot"));
            BrowseBackupRootCommand = new RelayCommand(_ => BrowseFolder("BackupRoot"));
            SaveProfileCommand = new RelayCommand(_ => UpdateProfile());
            CreateProfileCommand = new RelayCommand(_ => CreateNewProfile());
            RenameProfileCommand = new RelayCommand(_ => RenameCurrentProfile());
            DeleteProfileCommand = new RelayCommand(_ => DeleteCurrentProfile());
            ProcessModulesCommand = new RelayCommand(_ => ProcessModules(), _ => CanProcess);
            SaveEndDateCommand = new RelayCommand(_ => SaveEndDate());
            AddModuleCommand = new RelayCommand(_ => AddNewModule());
            SaveModuleCommand = new RelayCommand(_ => SaveCurrentModule());
            DeleteModuleCommand = new RelayCommand(_ => DeleteCurrentModule());
            RefreshModulesCommand = new RelayCommand(_ => RefreshModules());
            ValidateAllCommand = new RelayCommand(async _ => await ValidateAllAsync(), _ => !IsValidating);
            LoadSettings();
        }

        public ObservableCollection<Domain.Profile> Profiles { get; }
        public ObservableCollection<ModuleViewModel> Modules { get; }
        public ObservableCollection<FileViewModel> Files { get; }

        public Domain.Profile? CurrentProfile
        {
            get => _currentProfile;
            set
            {
                if (SetProperty(ref _currentProfile, value))
                {
                    if (value != null)
                        _projectService.SelectProfile(value.Name);
                    OnPropertyChanged(nameof(CanProcess));
                    OnPropertyChanged(nameof(ShowTitleNames));
                    RefreshModules();
                    RefreshFiles();
                }
            }
        }

        public string Theme
        {
            get => _theme;
            set => SetProperty(ref _theme, value);
        }

        public string Language
        {
            get => _language;
            set => SetProperty(ref _language, value);
        }

        public int YearOffset
        {
            get => _currentProfile?.YearOffset ?? 10000;
            set
            {
                if (_currentProfile != null)
                {
                    _currentProfile.YearOffset = value;
                    OnPropertyChanged();
                    _projectService.UpdateProfile(_currentProfile);
                }
            }
        }

        public bool ShowTitleNames
        {
            get => _currentProfile?.ShowTitleNames ?? true;
            set
            {
                if (_currentProfile != null)
                {
                    _currentProfile.ShowTitleNames = value;
                    OnPropertyChanged();
                    _projectService.UpdateProfile(_currentProfile);
                }
            }
        }

        public bool CanProcess => CurrentProfile != null &&
            !string.IsNullOrEmpty(CurrentProfile.GameRoot) &&
            !string.IsNullOrEmpty(CurrentProfile.ModRoot) &&
            !string.IsNullOrEmpty(CurrentProfile.BackupRoot);

        public ICommand BrowseGameRootCommand { get; }
        public ICommand BrowseModRootCommand { get; }
        public ICommand BrowseBackupRootCommand { get; }
        public ICommand SaveProfileCommand { get; }
        public ICommand CreateProfileCommand { get; }
        public ICommand RenameProfileCommand { get; }
        public ICommand DeleteProfileCommand { get; }
        public ICommand ProcessModulesCommand { get; }
        public ICommand SaveEndDateCommand { get; }
        public ICommand AddModuleCommand { get; }
        public ICommand SaveModuleCommand { get; }
        public ICommand DeleteModuleCommand { get; }
        public ICommand RefreshModulesCommand { get; }
        public ICommand ValidateAllCommand { get; }

        private ModuleViewModel? _selectedModule;
        public ModuleViewModel? SelectedModule
        {
            get => _selectedModule;
            set => SetProperty(ref _selectedModule, value);
        }

        private void BrowseFolder(string propertyName)
        {
            if (CurrentProfile == null) return;

            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = propertyName switch
            {
                "GameRoot" => Res("Msg_BrowseGameRoot"),
                "ModRoot" => Res("Msg_BrowseModRoot"),
                "BackupRoot" => Res("Msg_BrowseBackupRoot"),
                _ => Res("Msg_BrowseFolder")
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = dialog.SelectedPath;
                switch (propertyName)
                {
                    case "GameRoot": CurrentProfile.GameRoot = path; break;
                    case "ModRoot": CurrentProfile.ModRoot = path; break;
                    case "BackupRoot": CurrentProfile.BackupRoot = path; break;
                }
                OnPropertyChanged(nameof(CurrentProfile));
                UpdateProfile();
            }
        }

        private void CreateNewProfile()
        {
            var dialog = new InputDialog(Res("Msg_NewProfileTitle"), Res("Msg_NewProfilePrompt"), Res("Msg_NewProfileTitle"));
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ResponseText))
            {
                BrowseAndCreateProfile(dialog.ResponseText);
            }
        }

        private void BrowseAndCreateProfile(string profileName)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = Res("Msg_BrowseGameRootForProfile");

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string gameRoot = dialog.SelectedPath;
                var profile = _projectService.CreateProfileWithGameDetection(profileName, gameRoot);
                Profiles.Add(profile);
                CurrentProfile = profile;

                // Set the GameRoot to the selected path
                CurrentProfile.GameRoot = gameRoot;
                _projectService.UpdateProfile(CurrentProfile);
            }
            else
            {
                // User cancelled folder selection, create with default CK3
                var profile = _projectService.CreateProfile(profileName);
                Profiles.Add(profile);
                CurrentProfile = profile;
            }
        }

        private void RenameCurrentProfile()
        {
            if (CurrentProfile == null) return;

            var dialog = new InputDialog(Res("Msg_RenameProfileTitle"), Res("Msg_RenameProfilePrompt"), CurrentProfile.Name);
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ResponseText))
            {
                if (_projectService.RenameProfile(CurrentProfile.Name, dialog.ResponseText))
                {
                    CurrentProfile.Name = dialog.ResponseText;
                    OnPropertyChanged(nameof(CurrentProfile));
                    RefreshProfiles();
                }
                else
                {
                    System.Windows.MessageBox.Show(Res("Msg_NameNotValid"), Res("App_ErrorTitle"),
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void DeleteCurrentProfile()
        {
            if (CurrentProfile == null) return;

            var result = System.Windows.MessageBox.Show(
                string.Format(Res("Msg_ConfirmDelete"), CurrentProfile.Name),
                Res("Msg_ConfirmTitle"), System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                DeleteProfile(CurrentProfile);
            }
        }

        public void LoadSettings()
        {
            Theme = _projectService.Theme;
            Language = _projectService.Language;
        }

        public void SaveSettings()
        {
            _projectService.Theme = Theme;
            _projectService.Language = Language;
            _projectService.SaveSettings();
        }

        public void RefreshProfiles()
        {
            Profiles.Clear();
            foreach (var p in _projectService.Profiles)
            {
                Profiles.Add(p);
            }
        }

        public void RefreshModules()
        {
            Modules.Clear();
            if (CurrentProfile == null) return;

            var gameModules = _projectService.GetGameModulesAsDomain(CurrentProfile.Game);
            foreach (var kvp in gameModules.OrderBy(k => k.Key))
            {
                var isActive = CurrentProfile.ModuleIds.Contains(kvp.Key);
                Modules.Add(new ModuleViewModel
                {
                    Name = kvp.Key,
                    Path = kvp.Value.Path,
                    IgnoreExt = string.Join(", ", kvp.Value.IgnoreExtensions),
                    IsActive = isActive
                });
            }
        }

        private void AddNewModule()
        {
            if (CurrentProfile == null) return;

            var dialog = new InputDialog(Res("Msg_NewModuleTitle"), Res("Msg_NewModulePrompt"), "");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ResponseText))
            {
                var name = dialog.ResponseText;
                var pathDialog = new InputDialog(Res("Msg_NewModulePathTitle"), Res("Msg_NewModulePathPrompt"), "");
                if (pathDialog.ShowDialog() != true) return;

                _projectService.AddModule(CurrentProfile.Game, name, pathDialog.ResponseText, new List<string>());
                RefreshModules();
            }
        }

        private void SaveCurrentModule()
        {
            if (CurrentProfile == null || SelectedModule == null) return;

            var ignoreExt = SelectedModule.IgnoreExt
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => e.StartsWith("."))
                .ToList();

            if (!ignoreExt.Any())
                ignoreExt.Add(".none");

            _projectService.UpdateModule(CurrentProfile.Game, SelectedModule.Name, SelectedModule.Path, ignoreExt);
            RefreshModules();
        }

        private void DeleteCurrentModule()
        {
            if (CurrentProfile == null || SelectedModule == null) return;

            var result = System.Windows.MessageBox.Show(
                string.Format(Res("Msg_DeleteModuleConfirm"), SelectedModule.Name),
                Res("Msg_ConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _projectService.DeleteModule(CurrentProfile.Game, SelectedModule.Name);
                RefreshModules();
            }
        }

        public ObservableCollection<ModuleValidationResult> ValidationResults { get; } = new();

        private bool _isValidating;
        public bool IsValidating
        {
            get => _isValidating;
            set
            {
                SetProperty(ref _isValidating, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public async Task ValidateAllAsync()
        {
            if (CurrentProfile == null) return;

            IsValidating = true;
            ValidationResults.Clear();

            try
            {
                var results = await _projectService.ValidateAllAsync();
                foreach (var r in results.OrderBy(r => r.ModuleName))
                    ValidationResults.Add(r);
            }
            finally
            {
                IsValidating = false;
            }
        }

        public void RefreshFiles()
        {
            Files.Clear();
            if (CurrentProfile == null) return;

            var session = _projectService.CurrentSession;
            if (session == null) return;

            var gameFiles = session.GetFilesForGame(CurrentProfile.Game);
            foreach (var kvp in gameFiles)
            {
                var isActive = CurrentProfile.FileIds.Contains(kvp.Key);
                Files.Add(new FileViewModel
                {
                    Name = kvp.Key,
                    Path = kvp.Value.Path,
                    MapTo = kvp.Value.MapTo ?? "",
                    IsActive = isActive
                });
            }
        }

        public void AddProfile(string name, string game = "CK3")
        {
            var profile = _projectService.CreateProfile(name, game);
            Profiles.Add(profile);
            CurrentProfile = profile;
        }

        public void UpdateProfile()
        {
            if (CurrentProfile != null)
            {
                CurrentProfile.ModuleIds.Clear();
                foreach (var m in Modules)
                {
                    if (m.IsActive)
                        CurrentProfile.ModuleIds.Add(m.Name);
                }

                _projectService.UpdateProfile(CurrentProfile);
            }
        }

        public void DeleteProfile(Domain.Profile profile)
        {
            if (_projectService.DeleteProfile(profile.Name))
            {
                Profiles.Remove(profile);
                if (CurrentProfile == profile)
                    CurrentProfile = Profiles.FirstOrDefault();
            }
        }

        public async void ProcessModules()
        {
            if (CurrentProfile == null) return;

            UpdateProfile();
            await _projectService.ProcessModulesAsync(YearOffset);
            System.Windows.MessageBox.Show(Res("Msg_ProcessComplete"), Res("Msg_ProcessOK"),
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public string? ReadEndDate()
        {
            if (CurrentProfile == null) return null;
            return _projectService.ReadEndDate(CurrentProfile.GameRoot);
        }

        public string? ReadModEndDate()
        {
            if (CurrentProfile == null) return null;
            return _projectService.ReadModEndDate(CurrentProfile.ModRoot);
        }

        public bool WriteEndDate(string newDate)
        {
            if (CurrentProfile == null) return false;
            return _projectService.WriteEndDate(newDate);
        }

        public void SaveEndDate()
        {
            if (CurrentProfile == null) return;

            var dialog = new InputDialog(Res("Msg_SaveEndDateTitle"), Res("Msg_SaveEndDatePrompt"),
                ReadModEndDate() ?? ReadEndDate() ?? "");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ResponseText))
            {
                if (WriteEndDate(dialog.ResponseText))
                {
                    System.Windows.MessageBox.Show(Res("Msg_EndDateSaved"), Res("Msg_EndDateOK"),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show(Res("Msg_EndDateError"), Res("App_ErrorTitle"),
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private static string Res(string key)
        {
            return System.Windows.Application.Current.TryFindResource(key) as string ?? key;
        }
    }

    public class ModuleViewModel : INotifyPropertyChanged
    {
        private bool _isActive;

        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string IgnoreExt { get; set; } = "";
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class FileViewModel : INotifyPropertyChanged
    {
        private bool _isActive;

        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string MapTo { get; set; } = "";
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
    }
}
