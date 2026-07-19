using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using PdxModIDE.Validation;
using PdxModIDE.UI.ViewModels;

namespace PdxModIDE.UI
{
    public class ModuleFullRow
    {
        public string ModuleName { get; set; } = "";
        public string ModVsBackup { get; set; } = "";
        public string GameVsBackup { get; set; } = "";
        public ModuleValidationResult? Result { get; set; }
    }

    public class FileFullRow
    {
        public string FileKey { get; set; } = "";
        public string ModVsBackup { get; set; } = "";
        public string GameVsBackup { get; set; } = "";
        public FileValidationResult? Result { get; set; }
    }

    public partial class ValidationTab : System.Windows.Controls.UserControl
    {
        private MainViewModel? _viewModel;

        public ValidationTab()
        {
            InitializeComponent();
            Loaded += OnLoaded;

            BtnCompareModule.Click += BtnCompareModule_Click;
            BtnValidateAllModules.Click += BtnValidateAllModules_Click;
            BtnModuleDetails.Click += BtnModuleDetails_Click;
            BtnAddFile.Click += BtnAddFile_Click;
            BtnActivateFile.Click += BtnActivateFile_Click;
            BtnDeactivateFile.Click += BtnDeactivateFile_Click;
            BtnPathException.Click += BtnPathException_Click;
            BtnValidateFile.Click += BtnValidateFile_Click;
            BtnViewDiff.Click += BtnViewDiff_Click;
            BtnValidateAllFiles.Click += BtnValidateAllFiles_Click;
            BtnFileDiff.Click += BtnFileDiff_Click;
        }

        private MainViewModel? ViewModel => DataContext as MainViewModel;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel = ViewModel;
        }

        public void Refresh()
        {
            if (_viewModel?.CurrentProfile == null) return;

            var gameKey = _viewModel.CurrentProfile.Game;
            var modules = _viewModel.ProjectService.GetGameModulesAsDomain(gameKey);
            ComboModules.ItemsSource = modules.Keys.OrderBy(k => k).ToList();

            var fileKeys = _viewModel.ProjectService.GetFileKeys();
            ComboFiles.ItemsSource = fileKeys.OrderBy(k => k).ToList();

            LabelResult.Text = "";
            TreeModSingle.Items.Clear();
            ListViewModFull.Items.Clear();
            ListViewFileFull.Items.Clear();
        }

        private void BtnCompareModule_Click(object sender, RoutedEventArgs e)
        {
            TreeModSingle.Items.Clear();

            if (_viewModel?.CurrentProfile == null) return;

            var moduleName = ComboModules.SelectedItem as string;
            if (string.IsNullOrEmpty(moduleName)) return;

            var comparison = ComboCompare.SelectedIndex switch
            {
                0 => ComparisonType.GameVsMod,
                1 => ComparisonType.ModVsBackup,
                _ => ComparisonType.GameVsBackup
            };

            var results = _viewModel.ProjectService.ValidateModuleSingle(moduleName, comparison);
            foreach (var r in results)
            {
                var item = new System.Windows.Controls.TreeViewItem
                {
                    Header = $"[{r.Status}] {r.RelativePath}",
                    Tag = r.DiffLines
                };
                item.Foreground = r.Status switch
                {
                    "Modified" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xd1, 0x7b, 0x00)),
                    "Added" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green),
                    "Deleted" => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red),
                    _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray)
                };
                TreeModSingle.Items.Add(item);
            }

            if (results.Count == 0)
                TreeModSingle.Items.Add(new System.Windows.Controls.TreeViewItem { Header = Res("ValidationTab_NoDifferences") });
        }

        private async void BtnValidateAllModules_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            ListViewModFull.Items.Clear();

            await _viewModel.ValidateAllAsync();

            foreach (var r in _viewModel.ValidationResults)
            {
                ListViewModFull.Items.Add(new ModuleFullRow
                {
                    ModuleName = r.ModuleName,
                    ModVsBackup = r.ModVsBackupSummary,
                    GameVsBackup = r.GameVsBackupSummary,
                    Result = r
                });
            }
        }

        private void BtnModuleDetails_Click(object sender, RoutedEventArgs e)
        {
            ShowModuleDetails();
        }

        private void BtnAddFile_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel?.CurrentProfile == null) return;

            var dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.InitialDirectory = _viewModel.CurrentProfile.GameRoot;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var gameRoot = _viewModel.CurrentProfile.GameRoot;
                var rel = Path.GetRelativePath(gameRoot, dialog.FileName).Replace("\\", "/");
                var name = Path.GetFileNameWithoutExtension(rel);

                _viewModel.ProjectService.AddFile(_viewModel.CurrentProfile.Game, name, rel, null);
                _viewModel.ProjectService.ActivateFile(name);
                _viewModel.RefreshFiles();
                Refresh();
            }
        }

        private void BtnActivateFile_Click(object sender, RoutedEventArgs e)
        {
            var sel = ComboFiles.SelectedItem as string;
            if (string.IsNullOrEmpty(sel)) return;
            _viewModel?.ProjectService.ActivateFile(sel);
            _viewModel?.RefreshFiles();
        }

        private void BtnDeactivateFile_Click(object sender, RoutedEventArgs e)
        {
            var sel = ComboFiles.SelectedItem as string;
            if (string.IsNullOrEmpty(sel)) return;
            _viewModel?.ProjectService.DeactivateFile(sel);
            _viewModel?.RefreshFiles();
        }

        private void BtnPathException_Click(object sender, RoutedEventArgs e)
        {
            var sel = ComboFiles.SelectedItem as string;
            if (string.IsNullOrEmpty(sel) || _viewModel == null) return;

            var current = _viewModel.ProjectService.GetFileMapTo(sel);
            var dlg = new InputDialog("Path exception (MOD)", "Alternative path in MOD:", current ?? "");
            if (dlg.ShowDialog() == true)
            {
                _viewModel.ProjectService.SetFileMapTo(sel, string.IsNullOrWhiteSpace(dlg.ResponseText) ? null : dlg.ResponseText);
            }
        }

        private string _lastDiffFileKey = "";
        private List<string>? _currentDiff;

        private void BtnValidateFile_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel?.CurrentProfile == null) return;

            var sel = ComboFiles.SelectedItem as string;
            if (string.IsNullOrEmpty(sel)) return;

            bool compareToGame = RadioGameMode.IsChecked == true;
            var result = _viewModel.ProjectService.ValidateFileSingle(sel, compareToGame);
            _lastDiffFileKey = sel;
            _currentDiff = result.Diff;

            if (result.Status.Contains("SAME"))
                LabelResult.Foreground = TryFindResource("StatusGreen") as System.Windows.Media.SolidColorBrush
                    ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
            else if (result.Status.Contains("CHANGED"))
                LabelResult.Foreground = TryFindResource("StatusRed") as System.Windows.Media.SolidColorBrush
                    ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            else
                LabelResult.Foreground = TryFindResource("StatusBlue") as System.Windows.Media.SolidColorBrush
                    ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);

            LabelResult.Text = $"{result.Status} — {result.RelativePath}";
        }

        private void BtnViewDiff_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDiff == null || _currentDiff.Count == 0) return;

            var dlg = new DiffViewDialog(_lastDiffFileKey, _currentDiff);
            dlg.Owner = Window.GetWindow(this);
            dlg.ShowDialog();
        }

        private async void BtnValidateAllFiles_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel?.CurrentProfile == null) return;

            ListViewFileFull.Items.Clear();

            var results = await _viewModel.ProjectService.ValidateAllFilesAsync();
            foreach (var r in results)
            {
                ListViewFileFull.Items.Add(new FileFullRow
                {
                    FileKey = r.FileKey,
                    ModVsBackup = r.ModVsBackupStatus,
                    GameVsBackup = r.GameVsBackupStatus,
                    Result = r
                });
            }
        }

        private void BtnFileDiff_Click(object sender, RoutedEventArgs e)
        {
            ShowFileDiff();
        }

        private void ListViewModFull_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowModuleDetails();
        }

        private void ListViewFileFull_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowFileDiff();
        }

        private void ShowModuleDetails()
        {
            if (ListViewModFull.SelectedItem is ModuleFullRow row && row.Result != null)
            {
                var dlg = new DiffDialog(row.ModuleName, row.Result.ModVsBackupDetails, row.Result.GameVsBackupDetails);
                dlg.Owner = Window.GetWindow(this);
                dlg.ShowDialog();
            }
        }

        private void ShowFileDiff()
        {
            if (ListViewFileFull.SelectedItem is FileFullRow row && row.Result != null)
            {
                var dlg = new DiffChoiceDialog(row.FileKey, row.Result.ModVsBackupDiff, row.Result.GameVsBackupDiff);
                dlg.Owner = Window.GetWindow(this);
                dlg.ShowDialog();
            }
        }

        private static string Res(string key)
        {
            return System.Windows.Application.Current.TryFindResource(key) as string ?? key;
        }
    }
}
