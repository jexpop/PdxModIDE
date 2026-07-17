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
        }

        public void ApplyTheme(string theme)
        {
            ViewModel.Theme = theme;
            ViewModel.SaveSettings();

            var dict = new ResourceDictionary();
            string themePath = theme switch
            {
                "dark" => "Themes/DarkTheme.xaml",
                "ck3" => "Themes/CK3Theme.xaml",
                "sepia" => "Themes/SepiaTheme.xaml",
                "contrast" => "Themes/ContrastTheme.xaml",
                "vscode-dark" => "Themes/VSCodeDarkTheme.xaml",
                "vscode-light" => "Themes/VSCodeLightTheme.xaml",
                _ => "Themes/LightTheme.xaml"
            };

            try
            {
                dict.Source = new Uri(themePath, UriKind.Relative);
                System.Windows.Application.Current.Resources.MergedDictionaries.Clear();
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(dict);
                Resources.MergedDictionaries.Clear();
                Resources.MergedDictionaries.Add(dict);
            }
            catch
            {
                dict.Source = new Uri("Themes/LightTheme.xaml", UriKind.Relative);
                System.Windows.Application.Current.Resources.MergedDictionaries.Clear();
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(dict);
                Resources.MergedDictionaries.Clear();
                Resources.MergedDictionaries.Add(dict);
            }
        }
    }
}
