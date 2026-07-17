using System.Windows;
using System.Windows.Controls;
using PdxModIDE.UI.ViewModels;

namespace PdxModIDE.UI
{
    public partial class SettingsTab : System.Windows.Controls.UserControl
    {
        private MainViewModel? _viewModel;

        public SettingsTab()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            BtnApply.Click += BtnApply_Click;
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
        }

        private void ThemeChanged(object sender, RoutedEventArgs e)
        {
            // Preview only - apply on button click
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

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            var theme = GetSelectedTheme();
            _viewModel.Theme = theme;
            _viewModel.SaveSettings();

            if (Window.GetWindow(this) is MainWindow mainWindow)
                mainWindow.ApplyTheme(theme);

            System.Windows.MessageBox.Show("Tema aplicado", "OK",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
