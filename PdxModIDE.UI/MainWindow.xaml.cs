using System.Windows;
using PdxModIDE.Project;
using PdxModIDE.UI.ViewModels;

namespace PdxModIDE.UI
{
    public partial class MainWindow : Window
    {
        private readonly IProjectService _projectService;
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();

            _projectService = new ProjectManager();
            ViewModel = new MainViewModel(_projectService);
            DataContext = ViewModel;

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.CurrentProfile))
                    Dispatcher.BeginInvoke(new Action(() => ValidationTabControl.Refresh()));
            };

            if (ViewModel.Profiles.Count > 0)
            {
                ViewModel.CurrentProfile = ViewModel.Profiles[0];
            }

            ApplyTheme(ViewModel.Theme);
            ApplyLanguage(ViewModel.Language);
        }

        // Ruta del diccionario de tema y de idioma actualmente cargados.
        // Se mantienen por separado para poder recombinarlos sin que uno pise al otro.
        private string _currentThemePath = "Themes/LightTheme.xaml";
        private string _currentLanguagePath = "Languages/es.xaml";

        public void ApplyTheme(string theme)
        {
            ViewModel.Theme = theme;
            ViewModel.SaveSettings();

            _currentThemePath = theme switch
            {
                "dark" => "Themes/DarkTheme.xaml",
                "ck3" => "Themes/CK3Theme.xaml",
                "sepia" => "Themes/SepiaTheme.xaml",
                "contrast" => "Themes/ContrastTheme.xaml",
                "vscode-dark" => "Themes/VSCodeDarkTheme.xaml",
                "vscode-light" => "Themes/VSCodeLightTheme.xaml",
                _ => "Themes/LightTheme.xaml"
            };

            RefreshMergedDictionaries();
        }

        public void ApplyLanguage(string language)
        {
            ViewModel.Language = language;
            ViewModel.SaveSettings();

            _currentLanguagePath = language switch
            {
                "en" => "Languages/en.xaml",
                _ => "Languages/es.xaml"
            };

            RefreshMergedDictionaries();
        }

        /// <summary>
        /// Recombina el diccionario de tema y el de idioma en los recursos de la
        /// aplicación y de la ventana, de forma que cambiar uno no elimine el otro.
        /// </summary>
        private void RefreshMergedDictionaries()
        {
            var themeDict = new ResourceDictionary();
            var languageDict = new ResourceDictionary();

            try
            {
                themeDict.Source = new Uri(_currentThemePath, UriKind.Relative);
            }
            catch
            {
                themeDict.Source = new Uri("Themes/LightTheme.xaml", UriKind.Relative);
            }

            try
            {
                languageDict.Source = new Uri(_currentLanguagePath, UriKind.Relative);
            }
            catch
            {
                languageDict.Source = new Uri("Languages/es.xaml", UriKind.Relative);
            }

            System.Windows.Application.Current.Resources.MergedDictionaries.Clear();
            System.Windows.Application.Current.Resources.MergedDictionaries.Add(themeDict);
            System.Windows.Application.Current.Resources.MergedDictionaries.Add(languageDict);

            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(themeDict);
            Resources.MergedDictionaries.Add(languageDict);
        }

        private void BtnGeneralSettings_Click(object sender, RoutedEventArgs e)
        {
            var window = new GeneralSettingsWindow
            {
                Owner = this,
                DataContext = ViewModel
            };
            window.ShowDialog();
        }
    }
}
