using System.Windows;
using PdxModIDE.UI.ViewModels;

namespace PdxModIDE.UI
{
    public partial class GeneralSettingsWindow : Window
    {
        private MainViewModel? _viewModel;

        public GeneralSettingsWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as MainViewModel;
            if (_viewModel == null) return;

            switch (_viewModel.Theme)
            {
                case "dark": RadioDark.IsChecked = true; break;
                case "ck3": RadioCk3.IsChecked = true; break;
                case "sepia": RadioSepia.IsChecked = true; break;
                case "contrast": RadioContrast.IsChecked = true; break;
                case "vscode-dark": RadioVscodeDark.IsChecked = true; break;
                case "vscode-light": RadioVscodeLight.IsChecked = true; break;
                default: RadioLight.IsChecked = true; break;
            }

            switch (_viewModel.Language)
            {
                case "en": RadioLangEn.IsChecked = true; break;
                default: RadioLangEs.IsChecked = true; break;
            }
        }

        private void LanguageChanged(object sender, RoutedEventArgs e)
        {
            // Preview only - se aplica al pulsar "Aplicar", igual que el tema.
        }

        private string GetSelectedTheme()
        {
            if (RadioDark.IsChecked == true) return "dark";
            if (RadioCk3.IsChecked == true) return "ck3";
            if (RadioSepia.IsChecked == true) return "sepia";
            if (RadioContrast.IsChecked == true) return "contrast";
            if (RadioVscodeDark.IsChecked == true) return "vscode-dark";
            if (RadioVscodeLight.IsChecked == true) return "vscode-light";
            return "light";
        }

        private string GetSelectedLanguage()
        {
            if (RadioLangEn.IsChecked == true) return "en";
            return "es";
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            var theme = GetSelectedTheme();
            var language = GetSelectedLanguage();

            _viewModel.Theme = theme;
            _viewModel.Language = language;
            _viewModel.SaveSettings();

            if (Owner is MainWindow mainWindow)
            {
                mainWindow.ApplyTheme(theme);
                mainWindow.ApplyLanguage(language);
            }

            System.Windows.MessageBox.Show(
                (string)System.Windows.Application.Current.Resources["Settings_AppliedMessage"],
                (string)System.Windows.Application.Current.Resources["Settings_AppliedTitle"],
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
