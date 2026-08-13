using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using PdxModIDE.UI.Translation;
using PdxModIDE.UI.ViewModels;

namespace PdxModIDE.UI
{
    public partial class GeneralSettingsWindow : Window
    {
        private MainViewModel? _viewModel;
        private static readonly HttpClient _validateHttp = new() { Timeout = TimeSpan.FromSeconds(15) };

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
                case "ca": RadioLangCa.IsChecked = true; break;
                case "en": RadioLangEn.IsChecked = true; break;
                default: RadioLangEs.IsChecked = true; break;
            }

            ChkAutoTranslate.IsChecked = _viewModel.AutoTranslate;

            var enabled = _viewModel.EnabledTranslationProviders ?? new List<string>();
            ChkLibreTranslate.IsChecked = enabled.Contains(TranslationProviderConstants.LibreTranslate);
            ChkLingva.IsChecked = enabled.Contains(TranslationProviderConstants.Lingva);
            ChkDeepl.IsChecked = enabled.Contains(TranslationProviderConstants.DeepL);

            var urls = _viewModel.TranslationProviderUrls ?? new Dictionary<string, string>();
            if (urls.TryGetValue(TranslationProviderConstants.LibreTranslate, out var lu)) TxtLibreUrl.Text = lu;
            if (urls.TryGetValue(TranslationProviderConstants.Lingva, out var lv)) TxtLingvaUrl.Text = lv;

            if (!string.IsNullOrEmpty(_viewModel.DeeplApiKey))
                TxtDeeplKey.Password = _viewModel.DeeplApiKey;
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
            if (RadioLangCa.IsChecked == true) return "ca";
            if (RadioLangEn.IsChecked == true) return "en";
            return "es";
        }

        private List<string> GetEnabledProviders()
        {
            var list = new List<string> { TranslationProviderConstants.MyMemory };
            if (ChkLibreTranslate.IsChecked == true) list.Add(TranslationProviderConstants.LibreTranslate);
            if (ChkLingva.IsChecked == true) list.Add(TranslationProviderConstants.Lingva);
            if (ChkDeepl.IsChecked == true) list.Add(TranslationProviderConstants.DeepL);
            return list;
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            var theme = GetSelectedTheme();
            var language = GetSelectedLanguage();

            if (Owner is MainWindow mainWindow)
            {
                mainWindow.ApplyTheme(theme);
                mainWindow.ApplyLanguage(language);
            }

            _viewModel.Theme = theme;
            _viewModel.Language = language;

            _viewModel.AutoTranslate = ChkAutoTranslate.IsChecked == true;

            _viewModel.EnabledTranslationProviders = GetEnabledProviders();
            _viewModel.TranslationProviderUrls = new Dictionary<string, string>
            {
                [TranslationProviderConstants.LibreTranslate] = TxtLibreUrl.Text.Trim(),
                [TranslationProviderConstants.Lingva] = TxtLingvaUrl.Text.Trim()
            };
            _viewModel.DeeplApiKey = TxtDeeplKey.Password;
            _viewModel.SaveSettings();

            System.Windows.MessageBox.Show(
                (string)System.Windows.Application.Current.Resources["Settings_AppliedMessage"],
                (string)System.Windows.Application.Current.Resources["Settings_AppliedTitle"],
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnValidateDeepl_Click(object sender, RoutedEventArgs e)
        {
            BtnValidateDeepl.IsEnabled = false;
            TxtDeeplStatus.Text = (string)System.Windows.Application.Current.Resources["Settings_DeeplValidating"];
            bool valid = await DeepLProvider.ValidateKeyAsync(_validateHttp, TxtDeeplKey.Password);
            TxtDeeplStatus.Text = valid
                ? (string)System.Windows.Application.Current.Resources["Settings_DeeplValid"]
                : (string)System.Windows.Application.Current.Resources["Settings_DeeplInvalid"];
            BtnValidateDeepl.IsEnabled = true;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
