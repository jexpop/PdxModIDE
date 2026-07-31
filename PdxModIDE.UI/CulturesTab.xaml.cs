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
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public bool HasColor { get; set; }
        public string ColorDisplay { get; set; } = "";
        public string ColorReference { get; set; } = "";
        public System.Windows.Media.Brush ColorBrush => HasColor
            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(R, G, B))
            : System.Windows.Media.Brushes.Transparent;
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

    public class NamedColor
    {
        public string Name { get; set; } = "";
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public bool HasColor { get; set; }
        public string ColorDisplay { get; set; } = "";
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

            var namedColors = LoadNamedColors(gameRoot, modRoot);

            foreach (var culture in allByName.Values)
            {
                if (!culture.HasColor && !string.IsNullOrEmpty(culture.ColorReference)
                    && namedColors.TryGetValue(culture.ColorReference, out var named)
                    && named.HasColor)
                {
                    culture.HasColor = true;
                    culture.R = named.R;
                    culture.G = named.G;
                    culture.B = named.B;
                    culture.ColorDisplay = named.ColorDisplay;
                }
            }

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

                ExtractColor(block, culture);

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

        private static void ExtractColor(string block, CultureInfo culture)
        {
            int pos = 0;
            while (pos < block.Length)
            {
                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length) break;

                string key = ReadKey(block, ref pos);
                if (string.IsNullOrEmpty(key)) break;

                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length || block[pos] != '=')
                {
                    SkipValueAndFollowingBlock(block, ref pos);
                    continue;
                }
                pos++;

                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length) break;

                if (key == "color")
                {
                    if (TryParseColorValue(block, ref pos, out byte r, out byte g, out byte b, out string display))
                    {
                        culture.HasColor = true;
                        culture.R = r;
                        culture.G = g;
                        culture.B = b;
                        culture.ColorDisplay = display;
                    }
                    else
                    {
                        culture.ColorReference = ReadKey(block, ref pos);
                    }
                    return;
                }

                SkipValueAndFollowingBlock(block, ref pos);
            }
        }

        private static string ReadBraceContent(string text, ref int pos)
        {
            int depth = 1;
            int start = pos + 1;
            pos++;
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

        private static bool TryParseColorValue(string block, ref int pos, out byte r, out byte g, out byte b, out string display)
        {
            r = g = b = 0;
            display = "";

            string? mode = null;
            if (block[pos] != '{')
            {
                int save = pos;
                string kw = ReadKey(block, ref pos);
                if (!string.IsNullOrEmpty(kw))
                {
                    SkipWhitespaceAndComments(block, ref pos);
                    if (pos < block.Length && block[pos] == '{')
                        mode = kw;
                    else
                        pos = save;
                }
            }

            if (pos < block.Length && block[pos] == '{')
            {
                string content = ReadBraceContent(block, ref pos);
                return TryParseColorValues(content, mode, out r, out g, out b, out display);
            }

            return false;
        }

        private static bool TryParseColorValues(string content, string? mode, out byte r, out byte g, out byte b, out string display)
        {
            r = g = b = 0;
            display = "";

            var numbers = new List<float>();
            int pos = 0;
            while (pos < content.Length)
            {
                SkipWhitespaceAndComments(content, ref pos);
                if (pos >= content.Length) break;

                int start = pos;
                while (pos < content.Length && (char.IsDigit(content[pos]) || content[pos] == '.' || content[pos] == '-'))
                    pos++;
                if (pos > start &&
                    float.TryParse(content.Substring(start, pos - start),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float v))
                    numbers.Add(v);
            }

            if (numbers.Count < 3) return false;

            display = mode != null
                ? $"{mode} {{ {FormatColorNumber(numbers[0])} {FormatColorNumber(numbers[1])} {FormatColorNumber(numbers[2])} }}"
                : $"{FormatColorNumber(numbers[0])} {FormatColorNumber(numbers[1])} {FormatColorNumber(numbers[2])}";

            float fr, fg, fb;
            switch (mode)
            {
                case "hsv":
                    (fr, fg, fb) = HsvToRgb(numbers[0], numbers[1], numbers[2]);
                    break;
                case "hsv360":
                    (fr, fg, fb) = HsvToRgb(numbers[0] / 360f, numbers[1] / 100f, numbers[2] / 100f);
                    break;
                case "rgb":
                    fr = numbers[0]; fg = numbers[1]; fb = numbers[2];
                    break;
                default:
                    fr = numbers[0]; fg = numbers[1]; fb = numbers[2];
                    if (fr <= 1f && fg <= 1f && fb <= 1f)
                    {
                        fr *= 255f; fg *= 255f; fb *= 255f;
                    }
                    break;
            }

            r = (byte)Math.Clamp(Math.Round(fr), 0, 255);
            g = (byte)Math.Clamp(Math.Round(fg), 0, 255);
            b = (byte)Math.Clamp(Math.Round(fb), 0, 255);
            return true;
        }

        private static string FormatColorNumber(float value)
        {
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static (float R, float G, float B) HsvToRgb(float h, float s, float v)
        {
            float c = v * s;
            float x = c * (1f - Math.Abs((h * 6f) % 2f - 1f));
            float m = v - c;
            float r = 0f, g = 0f, b = 0f;
            switch ((int)Math.Floor(h * 6f) % 6)
            {
                case 0: r = c; g = x; break;
                case 1: r = x; g = c; break;
                case 2: g = c; b = x; break;
                case 3: g = x; b = c; break;
                case 4: r = x; b = c; break;
                default: r = c; b = x; break;
            }
            return (r + m, g + m, b + m);
        }

        private static Dictionary<string, NamedColor> LoadNamedColors(string gameRoot, string modRoot)
        {
            var result = new Dictionary<string, NamedColor>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in new[] { gameRoot, modRoot })
            {
                if (string.IsNullOrEmpty(root)) continue;
                var dir = Path.Combine(root, "common", "named_colors");
                if (!Directory.Exists(dir)) continue;
                foreach (var file in Directory.GetFiles(dir, "*.txt"))
                    ParseNamedColorsFile(file, result);
            }
            return result;
        }

        private static void ParseNamedColorsFile(string path, Dictionary<string, NamedColor> output)
        {
            var text = File.ReadAllText(path);
            int pos = 0;
            while (pos < text.Length)
            {
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length) break;

                string key = ReadKey(text, ref pos);
                if (string.IsNullOrEmpty(key)) break;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '=')
                {
                    SkipValueAndFollowingBlock(text, ref pos);
                    continue;
                }
                pos++;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '{') continue;
                pos++;

                string block = ReadBlock(text, ref pos);
                if (key == "colors")
                    ParseNamedColorBlock(block, output);
            }
        }

        private static void ParseNamedColorBlock(string block, Dictionary<string, NamedColor> output)
        {
            int pos = 0;
            while (pos < block.Length)
            {
                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length) break;

                string name = ReadKey(block, ref pos);
                if (string.IsNullOrEmpty(name)) break;

                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length || block[pos] != '=')
                {
                    SkipValueAndFollowingBlock(block, ref pos);
                    continue;
                }
                pos++;

                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length) break;

                if (TryParseColorValue(block, ref pos, out byte r, out byte g, out byte b, out string display))
                {
                    output[name] = new NamedColor
                    {
                        Name = name,
                        R = r,
                        G = g,
                        B = b,
                        HasColor = true,
                        ColorDisplay = display
                    };
                }
            }
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

                if (culture.HasColor)
                {
                    DetailColorSwatch.Visibility = Visibility.Visible;
                    DetailColorValue.Visibility = Visibility.Visible;
                    DetailColorSwatch.Background = culture.ColorBrush;
                    DetailColorValue.Text = string.IsNullOrEmpty(culture.ColorReference)
                        ? culture.ColorDisplay
                        : $"{culture.ColorDisplay}  |  {Res("CulturesTab_ColorName")} {culture.ColorReference}";
                    DetailColorInternalText.Visibility = Visibility.Collapsed;
                }
                else
                {
                    DetailColorSwatch.Visibility = Visibility.Collapsed;
                    DetailColorValue.Visibility = Visibility.Collapsed;
                    DetailColorInternalText.Visibility = Visibility.Visible;
                }
            }
            else
            {
                DetailGroup.Visibility = Visibility.Collapsed;
                DetailEmptyText.Visibility = Visibility.Visible;
                DetailColorSwatch.Visibility = Visibility.Visible;
                DetailColorValue.Visibility = Visibility.Visible;
                DetailColorSwatch.Background = System.Windows.Media.Brushes.Transparent;
                DetailColorValue.Text = "";
                DetailColorInternalText.Visibility = Visibility.Collapsed;
            }
        }

        private static string Res(string key)
        {
            return System.Windows.Application.Current.TryFindResource(key) as string ?? key;
        }
    }
}
