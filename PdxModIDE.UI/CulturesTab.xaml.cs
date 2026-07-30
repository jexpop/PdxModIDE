using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PdxModIDE.Core.Games;
using PdxModIDE.UI.ViewModels;

namespace PdxModIDE.UI
{
    public class CultureInfo
    {
        public string Name { get; set; } = "";
        public string DisplayName
        {
            get => !string.IsNullOrEmpty(_displayName) ? _displayName : Name;
            set => _displayName = value;
        }
        private string? _displayName;
        public string Source { get; set; } = "Base";
        public string Heritage { get; set; } = "";
        public string HeritageDisplayName { get; set; } = "";
        public System.Windows.Media.Brush SourceBrush => Source == "Mod"
            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212))
            : new SolidColorBrush(System.Windows.Media.Color.FromRgb(150, 150, 150));
    }

    public class CultureGroup
    {
        public string Name { get; set; } = "";
        public string DisplayName
        {
            get => !string.IsNullOrEmpty(_displayName) ? _displayName : Name;
            set => _displayName = value;
        }
        private string? _displayName;
        public ObservableCollection<CultureInfo> Cultures { get; set; } = new();
        public int ModCount => Cultures.Count(c => c.Source == "Mod");
    }

    public partial class CulturesTab : System.Windows.Controls.UserControl
    {
        private MainViewModel? _viewModel;

        public CulturesTab()
        {
            InitializeComponent();
            Loaded += CulturesTab_Loaded;
        }

        private void CulturesTab_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as MainViewModel;
            LoadCultures();
        }

        private void LoadCultures()
        {
            if (_viewModel?.CurrentProfile == null) return;

            var gameRoot = _viewModel.CurrentProfile.GameRoot;
            var modRoot = _viewModel.CurrentProfile.ModRoot;
            var gameKey = _viewModel.CurrentProfile.Game;
            var appLang = _viewModel.Language;

            var plugin = GameRegistry.GetPlugin(gameKey);
            var culturePath = plugin?.CulturesRelativePath ?? "common/culture/cultures";

            var localization = LoadLocalization(gameRoot, modRoot, appLang);

            var modCultures = LoadCulturesFromDirectory(modRoot, culturePath, "Mod");
            var baseCultures = LoadCulturesFromDirectory(gameRoot, culturePath, "Base");

            var allByName = new Dictionary<string, CultureInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in baseCultures)
                allByName[c.Name] = c;
            foreach (var c in modCultures)
                allByName[c.Name] = c;

            foreach (var culture in allByName.Values)
            {
                if (localization.TryGetValue(culture.Name, out var displayName))
                    culture.DisplayName = displayName;
            }

            var groups = new Dictionary<string, CultureGroup>(StringComparer.OrdinalIgnoreCase);
            foreach (var culture in allByName.Values.OrderBy(c => c.Name))
            {
                var heritageKey = string.IsNullOrEmpty(culture.Heritage) ? "unknown" : culture.Heritage;
                if (!groups.TryGetValue(heritageKey, out var group))
                {
                    group = new CultureGroup { Name = heritageKey };
                    groups[heritageKey] = group;
                }
                group.Cultures.Add(culture);
            }

            foreach (var group in groups.Values)
            {
                var heritageLocKey = $"{group.Name}_name";
                if (localization.TryGetValue(heritageLocKey, out var heritageDisplayName))
                    group.DisplayName = heritageDisplayName;
            }

            foreach (var culture in allByName.Values)
            {
                if (groups.TryGetValue(culture.Heritage, out var group))
                    culture.HeritageDisplayName = group.DisplayName;
            }

            var sorted = new ObservableCollection<CultureGroup>(
                groups.Values.OrderBy(g => g.DisplayName ?? g.Name));

            CultureTree.ItemsSource = sorted;

            int totalGroups = sorted.Count;
            int modGroupsCount = groups.Values.Count(g => g.ModCount > 0);
            int totalCultures = allByName.Count;
            int modCulturesCount = modCultures.Count;

            StatsGroupsText.Text = $"{Res("CulturesTab_Groups")}: {totalGroups}";
            StatsModGroupsText.Text = $"{Res("CulturesTab_ModGroups")}: {modGroupsCount}";
            StatsModCulturesText.Text = $"{Res("CulturesTab_ModCultures")}: {modCulturesCount}";
            StatsBaseCulturesText.Text = $"{Res("CulturesTab_BaseCultures")}: {totalCultures - modCulturesCount}";
        }

        private static Dictionary<string, string> LoadLocalization(string gameRoot, string modRoot, string appLang)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var ck3Lang = appLang switch
            {
                "es" => "spanish",
                "ca" => "english",
                _ => "english"
            };

            var files = new[]
            {
                $"culture/cultures_l_{ck3Lang}.yml",
                $"culture/traditions/cultural_heritages_l_{ck3Lang}.yml"
            };

            foreach (var root in new[] { modRoot, gameRoot })
            {
                if (string.IsNullOrEmpty(root)) continue;
                foreach (var relativePath in files)
                {
                    var path = Path.Combine(root, "localization", ck3Lang, relativePath);
                    if (File.Exists(path))
                        ParseLocalizationFile(path, result);
                }
            }

            return result;
        }

        private static void ParseLocalizationFile(string path, Dictionary<string, string> result)
        {
            foreach (var line in File.ReadAllLines(path))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                if (trimmed.StartsWith('#')) continue;
                if (trimmed.StartsWith("l_")) continue;

                int colonIdx = trimmed.IndexOf(':');
                if (colonIdx < 0) continue;

                var key = trimmed.Substring(0, colonIdx).Trim();
                var rest = trimmed.Substring(colonIdx + 1).Trim();

                int quoteStart = rest.IndexOf('"');
                if (quoteStart < 0) continue;
                int quoteEnd = rest.LastIndexOf('"');
                if (quoteEnd <= quoteStart) continue;

                var value = rest.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);

                if (!string.IsNullOrEmpty(key) && !result.ContainsKey(key))
                    result[key] = value;
            }
        }

        private static List<CultureInfo> LoadCulturesFromDirectory(string rootPath, string relativePath, string source)
        {
            var result = new List<CultureInfo>();
            if (string.IsNullOrEmpty(rootPath)) return result;

            var dir = Path.Combine(rootPath, relativePath);
            if (!Directory.Exists(dir)) return result;

            foreach (var file in Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories))
            {
                result.AddRange(ParseFile(file, source));
            }

            return result;
        }

        private static List<CultureInfo> ParseFile(string filePath, string source)
        {
            var cultures = new List<CultureInfo>();
            var text = File.ReadAllText(filePath);
            int pos = 0;

            while (pos < text.Length)
            {
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length) break;

                string cultureKey = ReadKey(text, ref pos);
                if (string.IsNullOrEmpty(cultureKey)) break;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '=') continue;
                pos++;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '{') continue;
                pos++;

                string block = ReadBlock(text, ref pos);
                var culture = new CultureInfo
                {
                    Name = cultureKey,
                    Source = source
                };

                string? nameAttr = ExtractAttribute(block, "name");
                if (nameAttr != null)
                    culture.Name = nameAttr;

                string? heritage = ExtractAttribute(block, "heritage");
                if (heritage != null)
                    culture.Heritage = heritage;

                cultures.Add(culture);
            }

            return cultures;
        }

        private static string? ExtractAttribute(string block, string attributeName)
        {
            int pos = 0;
            while (pos < block.Length)
            {
                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length) break;

                string key = ReadKey(block, ref pos);
                if (string.IsNullOrEmpty(key)) break;

                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length || block[pos] != '=') continue;
                pos++;

                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length) break;

                if (key == attributeName)
                {
                    if (block[pos] == '"')
                    {
                        pos++;
                        int start = pos;
                        while (pos < block.Length && block[pos] != '"') pos++;
                        if (pos < block.Length)
                            return block.Substring(start, pos - start);
                    }
                    else
                    {
                        int start = pos;
                        while (pos < block.Length && !char.IsWhiteSpace(block[pos]) && block[pos] != '}' && block[pos] != '#')
                        {
                            if (block[pos] == '-' && pos + 1 < block.Length && block[pos + 1] == '-')
                                break;
                            pos++;
                        }
                        return block.Substring(start, pos - start);
                    }
                }

                SkipValueAndFollowingBlock(block, ref pos);
            }

            return null;
        }

        private static void SkipValueAndFollowingBlock(string block, ref int pos)
        {
            if (pos >= block.Length) return;

            if (block[pos] == '{')
            {
                pos++;
                ReadBlock(block, ref pos);
            }
            else if (block[pos] == '"')
            {
                pos++;
                while (pos < block.Length && block[pos] != '"') pos++;
                if (pos < block.Length) pos++;
            }
            else
            {
                while (pos < block.Length && !char.IsWhiteSpace(block[pos]) && block[pos] != '}' && block[pos] != '#')
                {
                    if (block[pos] == '-' && pos + 1 < block.Length && block[pos + 1] == '-')
                        break;
                    pos++;
                }
                SkipWhitespaceAndComments(block, ref pos);
                if (pos < block.Length && block[pos] == '{')
                {
                    pos++;
                    ReadBlock(block, ref pos);
                }
            }
        }

        private static void SkipWhitespaceAndComments(string text, ref int pos)
        {
            while (pos < text.Length)
            {
                if (char.IsWhiteSpace(text[pos]))
                {
                    pos++;
                }
                else if (text[pos] == '#' || (text[pos] == '-' && pos + 1 < text.Length && text[pos + 1] == '-'))
                {
                    while (pos < text.Length && text[pos] != '\n') pos++;
                }
                else
                {
                    break;
                }
            }
        }

        private static string ReadKey(string text, ref int pos)
        {
            int start = pos;
            while (pos < text.Length && (char.IsLetterOrDigit(text[pos]) || text[pos] == '_' || text[pos] == '@'))
                pos++;
            return pos > start ? text.Substring(start, pos - start) : "";
        }

        private static string ReadBlock(string text, ref int pos)
        {
            int depth = 1;
            int start = pos;
            while (pos < text.Length && depth > 0)
            {
                if (text[pos] == '{') depth++;
                else if (text[pos] == '}') depth--;
                if (depth > 0) pos++;
            }
            string result = text.Substring(start, pos - start);
            if (pos < text.Length) pos++;
            return result;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadCultures();
        }

        private void CultureTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is CultureInfo culture)
            {
                DetailGroup.Visibility = Visibility.Visible;
                DetailEmptyText.Visibility = Visibility.Collapsed;
                DetailNameValue.Text = culture.DisplayName;
                DetailHeritageValue.Text = culture.HeritageDisplayName;
                DetailSourceValue.Text = culture.Source;
            }
            else
            {
                DetailGroup.Visibility = Visibility.Collapsed;
                DetailEmptyText.Visibility = Visibility.Visible;
            }
        }

        private static string Res(string key)
        {
            return System.Windows.Application.Current.TryFindResource(key) as string ?? key;
        }
    }
}
