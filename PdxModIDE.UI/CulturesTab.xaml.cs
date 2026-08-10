using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using PdxModIDE.Core.Games;
using PdxModIDE.ModelEngine;
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
        public string SourceFile { get; set; } = "";
        public string RawKey { get; set; } = "";
        public string Heritage { get; set; } = "";
        public string HeritageDisplayName { get; set; } = "";
        public HeritageInfo? HeritageDefinition { get; set; }
        public string Ethos { get; set; } = "";
        public EthosInfo? EthosDefinition { get; set; }
        public string Language { get; set; } = "";
        public LanguageInfo? LanguageDefinition { get; set; }
        public string MartialCustom { get; set; } = "";
        public MartialCustomInfo? MartialCustomDefinition { get; set; }
        public string HeadDetermination { get; set; } = "";
        public HeadDeterminationInfo? HeadDeterminationDefinition { get; set; }
        public string NameList { get; set; } = "";
        public NameListInfo? NameListDefinition { get; set; }
        public List<string> TraditionKeys { get; set; } = new();
        public List<TraditionInfo> Traditions { get; set; } = new();
        public List<Ethnicity> Ethnicities { get; set; } = new();

        public List<string> CoaGfx { get; set; } = new();
        public List<string> BuildingGfx { get; set; } = new();
        public List<string> ClothingGfx { get; set; } = new();
        public List<string> UnitGfx { get; set; } = new();
        public string HouseCoaFrame { get; set; } = "";
        public string DynastyCoaFrame { get; set; } = "";
        public string HouseCoaMaskOffset { get; set; } = "";
        public string HouseCoaMaskScale { get; set; } = "";

        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public bool HasColor { get; set; }
        public string ColorDisplay { get; set; } = "";
        public string ColorReference { get; set; } = "";
        public bool IsModNew { get; set; }
        public System.Windows.Media.Brush ColorBrush => HasColor
            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(R, G, B))
            : System.Windows.Media.Brushes.Transparent;
        public System.Windows.Media.Brush SourceBrush => Source == "Mod"
            ? (IsModNew
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 140, 0))
                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212)))
            : System.Windows.Media.Brushes.Black;
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

    public class EthosParameter
    {
        public string Key { get; set; } = "";
        public string Content { get; set; } = "";
        public string Description { get; set; } = "";
        public bool HasDescription => !string.IsNullOrEmpty(Description);
    }

    public class EthosInfo
    {
        public string Name { get; set; } = "";
        public string DisplayName
        {
            get => !string.IsNullOrEmpty(_displayName) ? _displayName : Name;
            set => _displayName = value;
        }
        private string? _displayName;
        public List<EthosParameter> Parameters { get; set; } = new();
    }

    public class HeritageParameter
    {
        public string Key { get; set; } = "";
        public string Content { get; set; } = "";
        public string Description { get; set; } = "";
        public bool HasDescription => !string.IsNullOrEmpty(Description);
    }

    public class HeritageInfo
    {
        public string Name { get; set; } = "";
        public string DisplayName
        {
            get => !string.IsNullOrEmpty(_displayName) ? _displayName : Name;
            set => _displayName = value;
        }
        private string? _displayName;
        public List<HeritageParameter> Parameters { get; set; } = new();
    }

    public class LanguageParameter
    {
        public string Key { get; set; } = "";
        public string Content { get; set; } = "";
        public string Description { get; set; } = "";
        public bool HasDescription => !string.IsNullOrEmpty(Description);
    }

    public class LanguageInfo
    {
        public string Name { get; set; } = "";
        public string DisplayName
        {
            get => !string.IsNullOrEmpty(_displayName) ? _displayName : Name;
            set => _displayName = value;
        }
        private string? _displayName;
        public List<LanguageParameter> Parameters { get; set; } = new();
    }

    public class MartialCustomParameter
    {
        public string Key { get; set; } = "";
        public string Content { get; set; } = "";
        public string Description { get; set; } = "";
        public bool HasDescription => !string.IsNullOrEmpty(Description);
    }

    public class MartialCustomInfo
    {
        public string Name { get; set; } = "";
        public string DisplayName
        {
            get => !string.IsNullOrEmpty(_displayName) ? _displayName : Name;
            set => _displayName = value;
        }
        private string? _displayName;
        public List<MartialCustomParameter> Parameters { get; set; } = new();
    }

    public class HeadDeterminationParameter
    {
        public string Key { get; set; } = "";
        public string Content { get; set; } = "";
        public string Description { get; set; } = "";
        public bool HasDescription => !string.IsNullOrEmpty(Description);
    }

    public class HeadDeterminationInfo
    {
        public string Name { get; set; } = "";
        public string DisplayName
        {
            get => !string.IsNullOrEmpty(_displayName) ? _displayName : Name;
            set => _displayName = value;
        }
        private string? _displayName;
        public List<HeadDeterminationParameter> Parameters { get; set; } = new();
    }

    public class NameListParameter
    {
        public string Key { get; set; } = "";
        public string Content { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public bool HasDescription => !string.IsNullOrEmpty(Description);
    }

    public class NameListInfo
    {
        public string Name { get; set; } = "";
        public string DisplayName
        {
            get => !string.IsNullOrEmpty(_displayName) ? _displayName : Name;
            set => _displayName = value;
        }
        private string? _displayName;
        public List<NameListParameter> Parameters { get; set; } = new();
    }

    public class TraditionParameter
    {
        public string Key { get; set; } = "";
        public string Content { get; set; } = "";
        public string Description { get; set; } = "";
        public bool HasDescription => !string.IsNullOrEmpty(Description);
    }

    public class TraditionInfo
    {
        public string Name { get; set; } = "";
        public string DisplayName
        {
            get => !string.IsNullOrEmpty(_displayName) ? _displayName : Name;
            set => _displayName = value;
        }
        private string? _displayName;
        public string Description { get; set; } = "";
        public bool HasDescription => !string.IsNullOrEmpty(Description);
        public List<TraditionParameter> Parameters { get; set; } = new();
    }

    public class Ethnicity
    {
        public double Weight { get; set; }
        public string Name { get; set; } = "";
        public string DisplayName
        {
            get => !string.IsNullOrEmpty(_displayName) ? _displayName : Name;
            set => _displayName = value;
        }
        private string? _displayName;
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
        private PdxAssetResolver? _gfxResolver;
        private PdxModIDE.ModelEngine.PdxClothingResolver? _clothingResolver;
        private PdxModIDE.ModelEngine.PdxUnitResolver? _unitResolver;

        private CultureInfo? _editorCulture;
        private bool _editorIsNew;
        private string _editorTargetFolder = "";
        private bool _editorFileNameManual;
        private readonly Dictionary<string, string> _cultureFileIndex = new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, PdxModIDE.ModelEngine.DdsImage?> _textureDecodeCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, System.Windows.Media.Imaging.BitmapSource> _textureBitmapCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, PdxModIDE.ModelEngine.BuildingAssetDatabase> _buildingDbCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Dictionary<string, string>> _meshFileIndexCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Dictionary<string, string>> _textureFileIndexCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, PdxModIDE.ModelEngine.PdxModel> _modelCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<string>> _assetUniqueTextureCache = new(StringComparer.OrdinalIgnoreCase);

        private static PdxModIDE.ModelEngine.BuildingAssetDatabase? GetBuildingDb(string gameRoot)
        {
            if (string.IsNullOrEmpty(gameRoot)) return null;
            lock (_buildingDbCache)
            {
                if (_buildingDbCache.TryGetValue(gameRoot, out var db)) return db;
                db = PdxModIDE.ModelEngine.BuildingAssetDatabase.Load(gameRoot);
                _buildingDbCache[gameRoot] = db;
                return db;
            }
        }

        private static void LogGfx(string msg)
        {
            try
            {
                string dir = Path.Combine(AppContext.BaseDirectory, "logs");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "gfx_debug.log"),
                    $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\r\n");
            }
            catch { }
        }

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

            if (EditorTabHeaderText != null && string.IsNullOrEmpty(EditorTabHeaderText.Text))
            {
                EditorTabHeaderText.Text = Res("CulturesTab_EditorNewTitle");
                _editorIsNew = true;
                _editorCulture = null;
                var modRootForEditor = _viewModel.CurrentProfile.ModRoot;
                _editorTargetFolder = string.IsNullOrEmpty(modRootForEditor)
                    ? ""
                    : Path.Combine(modRootForEditor, "common", "culture", "cultures", "mod");
                UpdateEditorModeUi();
            }

            var gameRoot = _viewModel.CurrentProfile.GameRoot;
            var modRoot = _viewModel.CurrentProfile.ModRoot;
            var gameKey = _viewModel.CurrentProfile.Game;
            var appLang = _viewModel.Language;

            try
            {
                _gfxResolver = new PdxAssetResolver(Path.Combine(gameRoot, "gfx", "models"));
                LogGfx($"LoadCultures: gameRoot='{gameRoot}' resolver entities={_gfxResolver.EntityCount} meshes={_gfxResolver.MeshCount}");
                try { _clothingResolver = new PdxModIDE.ModelEngine.PdxClothingResolver(gameRoot); }
                catch { _clothingResolver = null; }
                try { _unitResolver = new PdxModIDE.ModelEngine.PdxUnitResolver(gameRoot); }
                catch { _unitResolver = null; }
            }
            catch (Exception ex)
            {
                _gfxResolver = null;
                LogGfx($"LoadCultures: resolver FAILED: {ex.Message}");
            }

            var plugin = GameRegistry.GetPlugin(gameKey);
            var culturePath = plugin?.CulturesRelativePath ?? "common/culture/cultures";

            var localization = LoadLocalization(gameRoot, modRoot, appLang);

            var modCultures = LoadCulturesFromDirectory(modRoot, culturePath, "Mod");
            var baseCultures = LoadCulturesFromDirectory(gameRoot, culturePath, "Base");

            _cultureFileIndex.Clear();
            foreach (var c in modCultures)
            {
                if (!string.IsNullOrEmpty(c.SourceFile) && !_cultureFileIndex.ContainsKey(c.RawKey))
                    _cultureFileIndex[c.RawKey] = c.SourceFile;
            }

            var allByName = new Dictionary<string, CultureInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in baseCultures)
                allByName[c.Name] = c;
            foreach (var c in modCultures)
                allByName[c.Name] = c;

            var namedColors = LoadNamedColors(gameRoot, modRoot);
            var ethosDefinitions = LoadEthosDefinitions(gameRoot, modRoot);
            var heritageDefinitions = LoadHeritageDefinitions(gameRoot, modRoot);
            var languageDefinitions = LoadLanguageDefinitions(gameRoot, modRoot);
            var martialCustomDefinitions = LoadMartialCustomDefinitions(gameRoot, modRoot);
            var headDeterminationDefinitions = LoadHeadDeterminationDefinitions(gameRoot, modRoot);
            var nameListDefinitions = LoadNameListDefinitions(gameRoot, modRoot);
            var traditionDefinitions = LoadTraditionDefinitions(gameRoot, modRoot);

            foreach (var ethos in ethosDefinitions.Values)
            {
                foreach (var parameter in ethos.Parameters)
                {
                    parameter.Description = ResOptional($"EthosParam_{parameter.Key}_Desc");
                }
            }

            foreach (var heritage in heritageDefinitions.Values)
            {
                foreach (var parameter in heritage.Parameters)
                {
                    parameter.Description = ResOptional($"HeritageParam_{parameter.Key}_Desc");
                }
            }

            foreach (var language in languageDefinitions.Values)
            {
                foreach (var parameter in language.Parameters)
                {
                    parameter.Description = ResOptional($"LanguageParam_{parameter.Key}_Desc");
                }
            }

            foreach (var martialCustom in martialCustomDefinitions.Values)
            {
                foreach (var parameter in martialCustom.Parameters)
                {
                    parameter.Description = ResOptional($"MartialCustomParam_{parameter.Key}_Desc");
                }
            }

            foreach (var headDetermination in headDeterminationDefinitions.Values)
            {
                foreach (var parameter in headDetermination.Parameters)
                {
                    parameter.Description = ResOptional($"HeadDeterminationParam_{parameter.Key}_Desc");
                }
            }

            foreach (var tradition in traditionDefinitions.Values)
            {
                if (localization.TryGetValue($"{tradition.Name}_name", out var tName))
                    tradition.DisplayName = tName;
                if (localization.TryGetValue($"{tradition.Name}_desc", out var tDesc))
                    tradition.Description = tDesc;
                foreach (var parameter in tradition.Parameters)
                {
                    parameter.Description = ResOptional($"TraditionParam_{parameter.Key}_Desc");
                }
            }

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

            foreach (var ethos in ethosDefinitions.Values)
            {
                if (localization.TryGetValue($"{ethos.Name}_name", out var ethosName))
                    ethos.DisplayName = ethosName;
            }

            foreach (var heritage in heritageDefinitions.Values)
            {
                if (localization.TryGetValue($"{heritage.Name}_name", out var heritageName))
                    heritage.DisplayName = heritageName;
            }

            foreach (var language in languageDefinitions.Values)
            {
                if (localization.TryGetValue($"{language.Name}_name", out var languageName))
                    language.DisplayName = languageName;
            }

            foreach (var martialCustom in martialCustomDefinitions.Values)
            {
                if (localization.TryGetValue($"{martialCustom.Name}_name", out var mcName))
                    martialCustom.DisplayName = mcName;
            }

            foreach (var headDetermination in headDeterminationDefinitions.Values)
            {
                if (localization.TryGetValue(headDetermination.Name, out var hdName))
                    headDetermination.DisplayName = hdName;
            }

            foreach (var nameList in nameListDefinitions.Values)
            {
                if (localization.TryGetValue(nameList.Name, out var nlName))
                    nameList.DisplayName = nlName;
                foreach (var parameter in nameList.Parameters)
                {
                    parameter.Category = Res($"NameListCategory_{GetNameListCategory(parameter.Key)}");
                    parameter.Description = ResOptional($"NameListParam_{parameter.Key}_Desc");
                }
            }

            foreach (var culture in allByName.Values)
            {
                if (!string.IsNullOrEmpty(culture.Ethos) && ethosDefinitions.TryGetValue(culture.Ethos, out var ethosDef))
                    culture.EthosDefinition = ethosDef;
            }

            foreach (var culture in allByName.Values)
            {
                if (!string.IsNullOrEmpty(culture.Heritage) && heritageDefinitions.TryGetValue(culture.Heritage, out var heritageDef))
                    culture.HeritageDefinition = heritageDef;
            }

            foreach (var culture in allByName.Values)
            {
                if (!string.IsNullOrEmpty(culture.Language) && languageDefinitions.TryGetValue(culture.Language, out var languageDef))
                    culture.LanguageDefinition = languageDef;
            }

            foreach (var culture in allByName.Values)
            {
                if (!string.IsNullOrEmpty(culture.MartialCustom) && martialCustomDefinitions.TryGetValue(culture.MartialCustom, out var mcDef))
                    culture.MartialCustomDefinition = mcDef;
            }

            foreach (var culture in allByName.Values)
            {
                if (!string.IsNullOrEmpty(culture.HeadDetermination) && headDeterminationDefinitions.TryGetValue(culture.HeadDetermination, out var hdDef))
                    culture.HeadDeterminationDefinition = hdDef;
            }

            foreach (var culture in allByName.Values)
            {
                if (!string.IsNullOrEmpty(culture.NameList) && nameListDefinitions.TryGetValue(culture.NameList, out var nlDef))
                    culture.NameListDefinition = nlDef;
            }

            foreach (var culture in allByName.Values)
            {
                foreach (var key in culture.TraditionKeys)
                {
                    if (traditionDefinitions.TryGetValue(key, out var traditionDef))
                        culture.Traditions.Add(traditionDef);
                }
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

        private void CtxCopyCulture_Click(object sender, RoutedEventArgs e)
        {
            if (CultureTree.SelectedItem is not CultureInfo culture) return;
            OpenEditor(culture, copyAsNew: true);
        }

        private void CultureTree_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var item = FindAncestor<System.Windows.Controls.TreeViewItem>(e.OriginalSource as DependencyObject);
            if (item != null)
            {
                item.IsSelected = true;
                item.Focus();
            }
        }

        private void CultureTree_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (CultureTree.SelectedItem is not CultureInfo culture)
            {
                e.Handled = true;
                return;
            }
            if (CtxEditMenuItem != null)
                CtxEditMenuItem.Visibility = culture.IsModNew
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null && current is not T)
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            return current as T;
        }


        private void CtxEditCulture_Click(object sender, RoutedEventArgs e)
        {
            if (CultureTree.SelectedItem is not CultureInfo culture) return;
            if (!culture.IsModNew)
            {
                EditorStatusText.Text = string.IsNullOrEmpty(culture.Name)
                    ? string.Empty
                    : $"\"{culture.Name}\" not editable (only new mod cultures can be edited).";
                return;
            }
            OpenEditor(culture, copyAsNew: false);
        }

        private void OpenEditor(CultureInfo culture, bool copyAsNew)
        {
            string editTarget = culture.DisplayName ?? culture.Name ?? "";
            if (copyAsNew)
            {
                EditorTabHeaderText.Text = Res("CulturesTab_EditorNewTitle");
                EditorModeText.Text = $"{Res("CulturesTab_EditorNewTitle")} ({editTarget})";
            }
            else
            {
                EditorTabHeaderText.Text = $"{Res("CulturesTab_EditorEditTitle")}: {editTarget}";
                EditorModeText.Text = $"{Res("CulturesTab_EditorEditTitle")}: {editTarget}";
            }

            EditorCultureId.Text = culture.Name ?? "";
            EditorHeritage.Text = culture.Heritage ?? "";
            EditorEthos.Text = culture.Ethos ?? "";
            EditorColor.Text = culture.HasColor
                ? $"{culture.R}, {culture.G}, {culture.B}"
                : (culture.ColorReference ?? "");
            EditorBuildingGfx.Text = string.Join(", ", culture.BuildingGfx ?? new List<string>());
            EditorClothingGfx.Text = string.Join(", ", culture.ClothingGfx ?? new List<string>());
            EditorUnitGfx.Text = string.Join(", ", culture.UnitGfx ?? new List<string>());

            _editorCulture = culture;
            _editorIsNew = copyAsNew;
            _editorFileNameManual = false;

            var modRoot = _viewModel?.CurrentProfile?.ModRoot;
            _editorTargetFolder = string.IsNullOrEmpty(modRoot)
                ? ""
                : Path.Combine(modRoot, "common", "culture", "cultures", "mod");

            UpdateEditorModeUi();

            if (CulturesSubTabs != null)
            {
                var editTab = EditorTabItem as System.Windows.Controls.TabItem;
                if (editTab != null)
                    CulturesSubTabs.SelectedItem = editTab;
            }
        }

        private void UpdateEditorModeUi()
        {
            if (EditorSaveButton != null)
                EditorSaveButton.Visibility = Visibility.Visible;
            if (EditorClearButton != null)
                EditorClearButton.Visibility = _editorIsNew ? Visibility.Visible : Visibility.Collapsed;
            if (EditorTargetRow != null)
                EditorTargetRow.Visibility = _editorIsNew ? Visibility.Visible : Visibility.Collapsed;
            if (EditorTargetFolderText != null)
            {
                EditorTargetFolderText.Text = string.IsNullOrEmpty(_editorTargetFolder)
                    ? Res("CulturesTab_EditorNoTarget")
                    : _editorTargetFolder;
            }
            if (EditorIdRow != null)
                EditorIdRow.Visibility = _editorIsNew ? Visibility.Visible : Visibility.Collapsed;
            if (EditorFileNameRow != null)
                EditorFileNameRow.Visibility = _editorIsNew ? Visibility.Visible : Visibility.Collapsed;
            if (EditorCultureId != null)
                EditorCultureId.IsReadOnly = !_editorIsNew;
            if (!_editorFileNameManual)
                UpdateDefaultEditorFileName();
            UpdateEditorHint();
        }

        private void UpdateEditorHint()
        {
            string hint = Res(_editorIsNew ? "CulturesTab_EditorHint" : "CulturesTab_EditorEditHint");
            if (EditorHintText != null)
                EditorHintText.Text = hint;
            if (EditorStatusText != null)
                EditorStatusText.Text = hint;
        }

        private void UpdateDefaultEditorFileName()
        {
            if (EditorFileName == null) return;
            string id = EditorCultureId?.Text?.Trim() ?? "";
            string prefix = _viewModel?.CurrentProfile?.FileNamePrefixes.TryGetValue("culture", out var p) == true ? (p ?? "") : "";
            string baseName = SanitizeFileName(string.IsNullOrEmpty(id) ? "culture" : id);
            EditorFileName.Text = $"{prefix}{baseName}.txt";
        }

        private void EditorBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel?.CurrentProfile == null) return;

            var modRoot = _viewModel.CurrentProfile.ModRoot;
            var baseFolder = string.IsNullOrEmpty(modRoot)
                ? ""
                : Path.Combine(modRoot, "common", "culture", "cultures", "mod");

            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = Res("CulturesTab_EditorSelectFolder");
            dialog.UseDescriptionForTitle = true;

            if (Directory.Exists(baseFolder))
                dialog.InitialDirectory = baseFolder;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string chosen = dialog.SelectedPath;
                if (!string.IsNullOrEmpty(baseFolder) && IsPathInside(chosen, baseFolder))
                {
                    _editorTargetFolder = chosen;
                }
                else
                {
                    EditorStatusText.Text = Res("CulturesTab_EditorFolderInvalid");
                    _editorTargetFolder = baseFolder;
                }
                UpdateEditorModeUi();
            }
        }

        private void EditorClear_Click(object sender, RoutedEventArgs e)
        {
            EditorCultureId.Text = "";
            EditorHeritage.Text = "";
            EditorEthos.Text = "";
            EditorColor.Text = "";
            EditorBuildingGfx.Text = "";
            EditorClothingGfx.Text = "";
            EditorUnitGfx.Text = "";
            _editorCulture = null;
            _editorFileNameManual = false;
            UpdateEditorModeUi();
        }

        private void EditorCultureId_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!_editorFileNameManual)
                UpdateDefaultEditorFileName();
        }

        private void EditorFileName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _editorFileNameManual = true;
        }

        private void EditorSave_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel?.CurrentProfile == null) return;

            string cultureId = EditorCultureId.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(cultureId))
            {
                EditorStatusText.Text = Res("CulturesTab_EditorNeedId");
                return;
            }

            string block = BuildCultureBlock(cultureId);

            if (_editorCulture == null || _editorIsNew)
            {
                SaveAsNewCulture(cultureId, block);
            }
            else
            {
                SaveExistingCulture(block);
            }
        }

        private void SaveAsNewCulture(string cultureId, string block)
        {
            var profile = _viewModel?.CurrentProfile;
            if (profile == null) return;

            var modRoot = profile.ModRoot;
            if (string.IsNullOrEmpty(modRoot))
            {
                EditorStatusText.Text = Res("CulturesTab_EditorNoModRoot");
                return;
            }

            if (!Directory.Exists(_editorTargetFolder))
            {
                try
                {
                    Directory.CreateDirectory(_editorTargetFolder);
                }
                catch
                {
                    EditorStatusText.Text = Res("CulturesTab_EditorFolderInvalid");
                    return;
                }
            }

            if (CultureExistsInMod(modRoot, cultureId, out string? existingFile))
            {
                EditorStatusText.Text = string.Format(Res("CulturesTab_EditorCultureExists"), cultureId);
                return;
            }

            string prefix = profile.FileNamePrefixes.TryGetValue("culture", out var p) ? p ?? "" : "";
            string defaultName = $"{prefix}{SanitizeFileName(cultureId)}.txt";
            string enteredName = EditorFileName?.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(enteredName) && !enteredName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                EditorStatusText.Text = Res("CulturesTab_EditorFileNameInvalid");
                return;
            }
            string fileName = string.IsNullOrEmpty(enteredName) ? defaultName : enteredName;
            fileName = SanitizeFileName(fileName);
            if (!fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                fileName += ".txt";

            string fullPath = Path.Combine(_editorTargetFolder, fileName);

            try
            {
                if (File.Exists(fullPath))
                {
                    InsertCultureIntoFileAlphabetically(fullPath, cultureId, block);
                    EditorStatusText.Text = string.Format(Res("CulturesTab_EditorAddedToFile"), fileName);
                }
                else
                {
                    File.WriteAllText(fullPath, block, new System.Text.UTF8Encoding(true));
                    EditorStatusText.Text = string.Format(Res("CulturesTab_EditorSaved"), fileName);
                }
                RefreshCultureTree();
            }
            catch (Exception ex)
            {
                EditorStatusText.Text = $"{Res("CulturesTab_EditorSaveError")}: {ex.Message}";
            }
        }

        private static bool CultureExistsInMod(string modRoot, string cultureId, out string? filePath)
        {
            filePath = null;
            string folder = Path.Combine(modRoot, "common", "culture", "cultures", "mod");
            if (!Directory.Exists(folder)) return false;

            foreach (var file in Directory.EnumerateFiles(folder, "*.txt", SearchOption.AllDirectories))
            {
                if (CultureBlockExistsInFile(file, cultureId))
                {
                    filePath = file;
                    return true;
                }
            }
            return false;
        }

        private static bool CultureBlockExistsInFile(string filePath, string cultureId)
        {
            var text = File.ReadAllText(filePath);
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
                if (pos >= text.Length || text[pos] != '{')
                {
                    SkipValueAndFollowingBlock(text, ref pos);
                    continue;
                }
                if (string.Equals(key, cultureId, StringComparison.OrdinalIgnoreCase))
                    return true;
                pos++;
                ReadBlock(text, ref pos);
            }
            return false;
        }

        private static void InsertCultureIntoFileAlphabetically(string filePath, string cultureId, string block)
        {
            var text = File.ReadAllText(filePath);
            var cultureIds = new List<string>();
            var positions = new List<int>();
            int pos = 0;
            while (pos < text.Length)
            {
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length) break;
                int keyStart = pos;
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
                if (pos >= text.Length || text[pos] != '{')
                {
                    SkipValueAndFollowingBlock(text, ref pos);
                    continue;
                }
                pos++;
                ReadBlock(text, ref pos);
                cultureIds.Add(key);
                positions.Add(keyStart);
            }

            int insertIndex = 0;
            while (insertIndex < cultureIds.Count &&
                   string.Compare(cultureIds[insertIndex], cultureId, StringComparison.OrdinalIgnoreCase) < 0)
            {
                insertIndex++;
            }

            var blockText = block.TrimEnd('\n', '\r');
            string newText;
            if (insertIndex >= cultureIds.Count)
            {
                if (text.Length > 0 && !text.EndsWith("\n", StringComparison.Ordinal))
                    text += Environment.NewLine;
                newText = text + blockText + Environment.NewLine;
            }
            else
            {
                int at = positions[insertIndex];
                string prefix = text.Substring(0, at);
                string suffix = text.Substring(at);
                newText = prefix + blockText + Environment.NewLine + suffix;
            }

            File.WriteAllText(filePath, newText, new System.Text.UTF8Encoding(true));
        }

        private void RefreshCultureTree()
        {
            if (CultureTree == null) return;
            CultureTree.ItemsSource = null;
            LoadCultures();
        }

        private void SaveExistingCulture(string block)
        {
            var profile = _viewModel?.CurrentProfile;
            var editorCulture = _editorCulture;
            if (editorCulture == null || profile == null)
            {
                EditorStatusText.Text = Res("CulturesTab_EditorNoSourceFile");
                return;
            }

            string rawKey = editorCulture.RawKey;
            string filePath = "";
            if (_cultureFileIndex.TryGetValue(rawKey, out var indexed))
            {
                filePath = indexed;
            }
            else if (CultureExistsInMod(profile.ModRoot, rawKey, out var found))
            {
                filePath = found ?? "";
            }
            else
            {
                filePath = editorCulture.SourceFile ?? "";
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                EditorStatusText.Text = Res("CulturesTab_EditorNoSourceFile");
                return;
            }

            try
            {
                ReplaceCultureInFile(filePath, rawKey, block);
                EditorStatusText.Text = string.Format(Res("CulturesTab_EditorSaved"), Path.GetFileName(filePath));
                RefreshCultureTree();
            }
            catch (Exception ex)
            {
                EditorStatusText.Text = $"{Res("CulturesTab_EditorSaveError")}: {ex.Message}";
            }
        }

        private string BuildCultureBlock(string cultureId)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{cultureId} = {{");

            string color = EditorColor.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(color))
            {
                var parts = color.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 3 &&
                    byte.TryParse(parts[0], out byte r) &&
                    byte.TryParse(parts[1], out byte g) &&
                    byte.TryParse(parts[2], out byte b))
                {
                    sb.AppendLine($"\tcolor = rgb {{ {r} {g} {b} }}");
                }
                else if (parts.Length == 1)
                {
                    sb.AppendLine($"\tcolor = {parts[0]}");
                }
            }

            string heritage = EditorHeritage.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(heritage))
                sb.AppendLine($"\theritage = {heritage}");

            string ethos = EditorEthos.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(ethos))
                sb.AppendLine($"\tethos = {ethos}");

            string building = EditorBuildingGfx.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(building))
                sb.AppendLine($"\tbuilding_gfx = {{ {SplitList(building)} }}");

            string clothing = EditorClothingGfx.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(clothing))
                sb.AppendLine($"\tclothing_gfx = {{ {SplitList(clothing)} }}");

            string unit = EditorUnitGfx.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(unit))
                sb.AppendLine($"\tunit_gfx = {{ {SplitList(unit)} }}");

            sb.AppendLine("}");
            sb.AppendLine();
            return sb.ToString();
        }

        private static string SplitList(string value)
            => string.Join(" ", value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        private static string SanitizeFileName(string name)
        {
            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        private static bool IsPathInside(string path, string baseDir)
        {
            try
            {
                string full = Path.GetFullPath(path);
                string baseFull = Path.GetFullPath(baseDir);
                return full.Equals(baseFull, StringComparison.OrdinalIgnoreCase) ||
                       full.StartsWith(baseFull.TrimEnd('\\', '/') + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static void ReplaceCultureInFile(string filePath, string cultureName, string newBlock)
        {
            string text = File.ReadAllText(filePath);
            string marker = $"{cultureName} = {{";
            int idx = text.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0)
                throw new InvalidOperationException("Culture block not found");

            int blockStart = idx + marker.Length;
            int bracket = text.IndexOf('{', blockStart - 1);
            int depth = 0;
            int pos = bracket;
            int end = -1;
            while (pos < text.Length)
            {
                char ch = text[pos];
                if (ch == '{') depth++;
                else if (ch == '}') { depth--; if (depth == 0) { end = pos; break; } }
                pos++;
            }
            if (end < 0)
                throw new InvalidOperationException("Culture block not found");

            var trimmedBlock = newBlock.TrimEnd();
            string newText = text.Substring(0, idx)
                + trimmedBlock
                + text.Substring(end + 1);
            File.WriteAllText(filePath, newText, new System.Text.UTF8Encoding(true));
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
                $"culture/traditions/cultural_heritages_l_{ck3Lang}.yml",
                $"culture/traditions/cultural_traditions_l_{ck3Lang}.yml",
                $"culture/traditions/cultural_languages_l_{ck3Lang}.yml",
                $"culture/head_determination_l_{ck3Lang}.yml",
                $"culture/culture_name_lists_l_{ck3Lang}.yml"
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
                bool isModNew = source == "Mod" && IsFileInModSubfolder(file, dir);
                result.AddRange(ParseFile(file, source, isModNew));
            }

            return result;
        }

        private static bool IsFileInModSubfolder(string filePath, string baseDir)
        {
            try
            {
                string full = Path.GetFullPath(filePath);
                string baseFull = Path.GetFullPath(baseDir);
                string rel = Path.GetRelativePath(baseFull, full);
                var segments = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return segments.Length > 0 && string.Equals(segments[0], "mod", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static Dictionary<string, EthosInfo> LoadEthosDefinitions(string gameRoot, string modRoot)
        {
            var result = new Dictionary<string, EthosInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var root in new[] { modRoot, gameRoot })
            {
                if (string.IsNullOrEmpty(root)) continue;
                var dir = Path.Combine(root, "common", "culture", "pillars");
                if (!Directory.Exists(dir)) continue;
                foreach (var file in Directory.GetFiles(dir, "*ethos.txt", SearchOption.AllDirectories))
                    ParseEthosFile(file, result);
            }

            return result;
        }

        private static void ParseEthosFile(string filePath, Dictionary<string, EthosInfo> output)
        {
            var text = File.ReadAllText(filePath);
            int pos = 0;

            while (pos < text.Length)
            {
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length) break;

                string ethosKey = ReadKey(text, ref pos);
                if (string.IsNullOrEmpty(ethosKey)) break;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '=') continue;
                pos++;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '{') continue;
                pos++;

                string block = ReadBlock(text, ref pos);
                var ethos = new EthosInfo { Name = ethosKey };
                ParseEthosParameters(block, ethos.Parameters);
                output[ethosKey] = ethos;
            }
        }

        private static void ParseEthosParameters(string block, List<EthosParameter> parameters)
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

                if (key == "type")
                {
                    SkipValueAndFollowingBlock(block, ref pos);
                    continue;
                }

                if (block[pos] == '{')
                {
                    string content = ReadBraceContent(block, ref pos);
                    parameters.Add(new EthosParameter { Key = key, Content = content });
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
                    parameters.Add(new EthosParameter
                    {
                        Key = key,
                        Content = block.Substring(start, pos - start)
                    });
                }
            }
        }

        private static Dictionary<string, LanguageInfo> LoadLanguageDefinitions(string gameRoot, string modRoot)
        {
            var result = new Dictionary<string, LanguageInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var root in new[] { modRoot, gameRoot })
            {
                if (string.IsNullOrEmpty(root)) continue;
                var dir = Path.Combine(root, "common", "culture", "pillars");
                if (!Directory.Exists(dir)) continue;
                foreach (var file in Directory.GetFiles(dir, "*language.txt", SearchOption.AllDirectories))
                    ParseLanguageFile(file, result);
            }

            return result;
        }

        private static void ParseLanguageFile(string filePath, Dictionary<string, LanguageInfo> output)
        {
            var text = File.ReadAllText(filePath);
            int pos = 0;

            while (pos < text.Length)
            {
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length) break;

                string languageKey = ReadKey(text, ref pos);
                if (string.IsNullOrEmpty(languageKey)) break;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '=') continue;
                pos++;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '{') continue;
                pos++;

                string block = ReadBlock(text, ref pos);
                var language = new LanguageInfo { Name = languageKey };
                ParseLanguageParameters(block, language.Parameters);
                output[languageKey] = language;
            }
        }

        private static void ParseLanguageParameters(string block, List<LanguageParameter> parameters)
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

                if (key == "type")
                {
                    SkipValueAndFollowingBlock(block, ref pos);
                    continue;
                }

                if (block[pos] == '{')
                {
                    string content = ReadBraceContent(block, ref pos);
                    parameters.Add(new LanguageParameter { Key = key, Content = content });
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
                    parameters.Add(new LanguageParameter
                    {
                        Key = key,
                        Content = block.Substring(start, pos - start)
                    });
                }
            }
        }

        private static Dictionary<string, HeritageInfo> LoadHeritageDefinitions(string gameRoot, string modRoot)
        {
            var result = new Dictionary<string, HeritageInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var root in new[] { modRoot, gameRoot })
            {
                if (string.IsNullOrEmpty(root)) continue;
                var dir = Path.Combine(root, "common", "culture", "pillars");
                if (!Directory.Exists(dir)) continue;
                foreach (var file in Directory.GetFiles(dir, "*heritage.txt", SearchOption.AllDirectories))
                    ParseHeritageFile(file, result);
            }

            return result;
        }

        private static void ParseHeritageFile(string filePath, Dictionary<string, HeritageInfo> output)
        {
            var text = File.ReadAllText(filePath);
            int pos = 0;

            while (pos < text.Length)
            {
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length) break;

                string heritageKey = ReadKey(text, ref pos);
                if (string.IsNullOrEmpty(heritageKey)) break;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '=') continue;
                pos++;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '{') continue;
                pos++;

                string block = ReadBlock(text, ref pos);
                var heritage = new HeritageInfo { Name = heritageKey };
                ParseHeritageParameters(block, heritage.Parameters);
                output[heritageKey] = heritage;
            }
        }

        private static void ParseHeritageParameters(string block, List<HeritageParameter> parameters)
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

                if (key == "type")
                {
                    SkipValueAndFollowingBlock(block, ref pos);
                    continue;
                }

                if (block[pos] == '{')
                {
                    string content = ReadBraceContent(block, ref pos);
                    parameters.Add(new HeritageParameter { Key = key, Content = content });
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
                    parameters.Add(new HeritageParameter
                    {
                        Key = key,
                        Content = block.Substring(start, pos - start)
                    });
                }
            }
        }

        private static Dictionary<string, MartialCustomInfo> LoadMartialCustomDefinitions(string gameRoot, string modRoot)
        {
            var result = new Dictionary<string, MartialCustomInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var root in new[] { modRoot, gameRoot })
            {
                if (string.IsNullOrEmpty(root)) continue;
                var dir = Path.Combine(root, "common", "culture", "pillars");
                if (!Directory.Exists(dir)) continue;
                foreach (var file in Directory.GetFiles(dir, "*martial_custom.txt", SearchOption.AllDirectories))
                    ParseMartialCustomFile(file, result);
            }

            return result;
        }

        private static void ParseMartialCustomFile(string filePath, Dictionary<string, MartialCustomInfo> output)
        {
            var text = File.ReadAllText(filePath);
            int pos = 0;

            while (pos < text.Length)
            {
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length) break;

                string key = ReadKey(text, ref pos);
                if (string.IsNullOrEmpty(key)) break;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '=') continue;
                pos++;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '{') continue;
                pos++;

                string block = ReadBlock(text, ref pos);
                var martialCustom = new MartialCustomInfo { Name = key };
                ParseMartialCustomParameters(block, martialCustom.Parameters);
                output[key] = martialCustom;
            }
        }

        private static void ParseMartialCustomParameters(string block, List<MartialCustomParameter> parameters)
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

                if (key == "type")
                {
                    SkipValueAndFollowingBlock(block, ref pos);
                    continue;
                }

                if (block[pos] == '{')
                {
                    string content = ReadBraceContent(block, ref pos);
                    parameters.Add(new MartialCustomParameter { Key = key, Content = content });
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
                    parameters.Add(new MartialCustomParameter
                    {
                        Key = key,
                        Content = block.Substring(start, pos - start)
                    });
                }
            }
        }

        private static Dictionary<string, HeadDeterminationInfo> LoadHeadDeterminationDefinitions(string gameRoot, string modRoot)
        {
            var result = new Dictionary<string, HeadDeterminationInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var root in new[] { modRoot, gameRoot })
            {
                if (string.IsNullOrEmpty(root)) continue;
                var dir = Path.Combine(root, "common", "culture", "pillars");
                if (!Directory.Exists(dir)) continue;
                foreach (var file in Directory.GetFiles(dir, "*head_determination.txt", SearchOption.AllDirectories))
                    ParseHeadDeterminationFile(file, result);
            }

            return result;
        }

        private static void ParseHeadDeterminationFile(string filePath, Dictionary<string, HeadDeterminationInfo> output)
        {
            var text = File.ReadAllText(filePath);
            int pos = 0;

            while (pos < text.Length)
            {
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length) break;

                string key = ReadKey(text, ref pos);
                if (string.IsNullOrEmpty(key)) break;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '=') continue;
                pos++;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '{') continue;
                pos++;

                string block = ReadBlock(text, ref pos);
                var headDetermination = new HeadDeterminationInfo { Name = key };
                ParseHeadDeterminationParameters(block, headDetermination.Parameters);
                output[key] = headDetermination;
            }
        }

        private static void ParseHeadDeterminationParameters(string block, List<HeadDeterminationParameter> parameters)
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

                if (key == "type")
                {
                    SkipValueAndFollowingBlock(block, ref pos);
                    continue;
                }

                if (block[pos] == '{')
                {
                    string content = ReadBraceContent(block, ref pos);
                    parameters.Add(new HeadDeterminationParameter { Key = key, Content = content });
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
                    parameters.Add(new HeadDeterminationParameter
                    {
                        Key = key,
                        Content = block.Substring(start, pos - start)
                    });
                }
            }
        }

        private static Dictionary<string, NameListInfo> LoadNameListDefinitions(string gameRoot, string modRoot)
        {
            var result = new Dictionary<string, NameListInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var root in new[] { modRoot, gameRoot })
            {
                if (string.IsNullOrEmpty(root)) continue;
                var dir = Path.Combine(root, "common", "culture", "name_lists");
                if (!Directory.Exists(dir)) continue;
                foreach (var file in Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories))
                    ParseNameListFile(file, result);
            }

            return result;
        }

        private static void ParseNameListFile(string filePath, Dictionary<string, NameListInfo> output)
        {
            var text = File.ReadAllText(filePath);
            int pos = 0;

            while (pos < text.Length)
            {
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length) break;

                string key = ReadKey(text, ref pos);
                if (string.IsNullOrEmpty(key)) break;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '=') continue;
                pos++;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '{') continue;
                pos++;

                string block = ReadBlock(text, ref pos);
                var nameList = new NameListInfo { Name = key };
                ParseNameListParameters(block, nameList.Parameters);
                output[key] = nameList;
            }
        }

        private static void ParseNameListParameters(string block, List<NameListParameter> parameters)
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
                if (pos >= block.Length) continue;

                if (block[pos] == '{')
                {
                    string content = ReadBraceContent(block, ref pos);
                    parameters.Add(new NameListParameter { Key = key, Content = content });
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
                    parameters.Add(new NameListParameter
                    {
                        Key = key,
                        Content = block.Substring(start, pos - start)
                    });
                }
            }
        }

        private static string GetNameListCategory(string key)
        {
            return key switch
            {
                "dynasty_name_first" or "founder_named_dynasties" or "house_based_map_names" or "suggest_family_names" or "suggest_ancestor_names" or "always_use_patronym" => "Options",
                "male_names" or "female_names" or "dynasty_names" or "cadet_dynasty_names" or "mercenary_names" => "NameLists",
                "pat_grf_name_chance" or "mat_grf_name_chance" or "father_name_chance" or "pat_grm_name_chance" or "mat_grm_name_chance" or "mother_name_chance" => "Chances",
                "patronym_prefix_male" or "patronym_prefix_male_vowel" or "patronym_prefix_female" or "patronym_prefix_female_vowel" or "patronym_suffix_male" or "patronym_suffix_female" or "dynasty_of_location_prefix" or "bastard_dynasty_prefix" => "PrefixesSuffixes",
                _ => "Other"
            };
        }

        private static Dictionary<string, TraditionInfo> LoadTraditionDefinitions(string gameRoot, string modRoot)
        {
            var result = new Dictionary<string, TraditionInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var root in new[] { modRoot, gameRoot })
            {
                if (string.IsNullOrEmpty(root)) continue;
                var dir = Path.Combine(root, "common", "culture", "traditions");
                if (!Directory.Exists(dir)) continue;
                foreach (var file in Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories))
                    ParseTraditionFile(file, result);
            }

            return result;
        }

        private static void ParseTraditionFile(string filePath, Dictionary<string, TraditionInfo> output)
        {
            var text = File.ReadAllText(filePath);
            int pos = 0;

            while (pos < text.Length)
            {
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length) break;

                string key = ReadKey(text, ref pos);
                if (string.IsNullOrEmpty(key)) break;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '=') continue;
                pos++;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '{') continue;
                pos++;

                string block = ReadBlock(text, ref pos);
                var tradition = new TraditionInfo { Name = key };
                ParseTraditionParameters(block, tradition.Parameters);
                output[key] = tradition;
            }
        }

        private static void ParseTraditionParameters(string block, List<TraditionParameter> parameters)
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

                if (block[pos] == '{')
                {
                    string content = ReadBraceContent(block, ref pos);
                    parameters.Add(new TraditionParameter { Key = key, Content = content });
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
                    parameters.Add(new TraditionParameter
                    {
                        Key = key,
                        Content = block.Substring(start, pos - start)
                    });
                }
            }
        }

        private static List<string> ExtractTraditionsAttribute(string block)
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

                if (key == "traditions")
                {
                    if (block[pos] == '{')
                    {
                        string content = ReadBraceContent(block, ref pos);
                        return ExtractTraditionKeys(content);
                    }
                    return new List<string>();
                }

                SkipValueAndFollowingBlock(block, ref pos);
            }

            return new List<string>();
        }

        private static List<string> ExtractTraditionKeys(string content)
        {
            var result = new List<string>();
            int pos = 0;
            while (pos < content.Length)
            {
                SkipWhitespaceAndComments(content, ref pos);
                if (pos >= content.Length) break;
                string key = ReadKey(content, ref pos);
                if (string.IsNullOrEmpty(key)) break;
                result.Add(key);
            }
            return result;
        }

        private static void ExtractEthnicitiesAttribute(string block, CultureInfo culture)
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

                if (key == "ethnicities")
                {
                    if (block[pos] == '{')
                    {
                        string content = ReadBraceContent(block, ref pos);
                        culture.Ethnicities = ParseEthnicityEntries(content);
                    }
                    return;
                }

                SkipValueAndFollowingBlock(block, ref pos);
            }
        }

        private static List<Ethnicity> ParseEthnicityEntries(string content)
        {
            var result = new List<Ethnicity>();
            int pos = 0;
            while (pos < content.Length)
            {
                SkipWhitespaceAndComments(content, ref pos);
                if (pos >= content.Length) break;

                string weightToken = ReadKey(content, ref pos);
                if (string.IsNullOrEmpty(weightToken)) break;

                SkipWhitespaceAndComments(content, ref pos);
                if (pos < content.Length && content[pos] == '=')
                {
                    pos++;
                    SkipWhitespaceAndComments(content, ref pos);
                    string name = ReadKey(content, ref pos);
                    if (!string.IsNullOrWhiteSpace(name) &&
                        double.TryParse(weightToken, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var weight))
                    {
                        result.Add(new Ethnicity { Weight = weight, Name = name });
                    }
                }
            }
            return result;
        }

        private static List<CultureInfo> ParseFile(string filePath, string source, bool isModNew = false)
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
                    RawKey = cultureKey,
                    Source = source,
                    IsModNew = isModNew,
                    SourceFile = filePath
                };

                string? nameAttr = ExtractAttribute(block, "name");
                if (nameAttr != null)
                    culture.Name = nameAttr;

                string? heritage = ExtractAttribute(block, "heritage");
                if (heritage != null)
                    culture.Heritage = heritage;

                string? ethos = ExtractAttribute(block, "ethos");
                if (ethos != null)
                    culture.Ethos = ethos;

                string? language = ExtractAttribute(block, "language");
                if (language != null)
                    culture.Language = language;

                string? martialCustom = ExtractAttribute(block, "martial_custom");
                if (martialCustom != null)
                    culture.MartialCustom = martialCustom;

                string? headDetermination = ExtractAttribute(block, "head_determination");
                if (headDetermination != null)
                    culture.HeadDetermination = headDetermination;

                string? nameList = ExtractAttribute(block, "name_list");
                if (nameList != null)
                    culture.NameList = nameList;

                culture.TraditionKeys = ExtractTraditionsAttribute(block);

                ExtractEthnicitiesAttribute(block, culture);

                ExtractGfxAttributes(block, culture);

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

        private void CultureTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is CultureInfo culture)
            {
                StatsGroup.Visibility = Visibility.Collapsed;
                DetailGroup.Visibility = Visibility.Visible;
                DetailEmptyText.Visibility = Visibility.Collapsed;
                DetailNameValue.Text = culture.DisplayName;
                DetailHeritageValue.Text = culture.HeritageDisplayName;
                DetailSourceValue.Text = culture.Source;

                if (culture.HeritageDefinition != null)
                {
                    if (culture.HeritageDefinition.Parameters.Count > 0)
                    {
                        HeritageParametersList.ItemsSource = culture.HeritageDefinition.Parameters;
                        DetailHeritageExpander.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        HeritageParametersList.ItemsSource = null;
                        DetailHeritageExpander.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    HeritageParametersList.ItemsSource = null;
                    DetailHeritageExpander.Visibility = Visibility.Collapsed;
                }

                if (culture.LanguageDefinition != null)
                {
                    DetailLanguageValue.Text = culture.LanguageDefinition.DisplayName;
                    if (culture.LanguageDefinition.Parameters.Count > 0)
                    {
                        LanguageParametersList.ItemsSource = culture.LanguageDefinition.Parameters;
                        DetailLanguageExpander.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        LanguageParametersList.ItemsSource = null;
                        DetailLanguageExpander.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    DetailLanguageValue.Text = string.IsNullOrEmpty(culture.Language) ? "-" : culture.Language;
                    LanguageParametersList.ItemsSource = null;
                    DetailLanguageExpander.Visibility = Visibility.Collapsed;
                }

                if (culture.Ethnicities.Count > 0)
                {
                    DetailEthnicitiesValue.Text = string.Join(Environment.NewLine,
                        culture.Ethnicities.Select(e => $"{e.Name} {e.Weight}%"));
                }
                else
                {
                    DetailEthnicitiesValue.Text = "-";
                }

                if (culture.EthosDefinition != null)
                {
                    DetailEthosValue.Text = culture.EthosDefinition.DisplayName;
                    if (culture.EthosDefinition.Parameters.Count > 0)
                    {
                        EthosParametersList.ItemsSource = culture.EthosDefinition.Parameters;
                        DetailEthosExpander.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        EthosParametersList.ItemsSource = null;
                        DetailEthosExpander.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    DetailEthosValue.Text = string.IsNullOrEmpty(culture.Ethos) ? "-" : culture.Ethos;
                    EthosParametersList.ItemsSource = null;
                    DetailEthosExpander.Visibility = Visibility.Collapsed;
                }

                if (culture.MartialCustomDefinition != null)
                {
                    DetailMartialCustomValue.Text = culture.MartialCustomDefinition.DisplayName;
                    if (culture.MartialCustomDefinition.Parameters.Count > 0)
                    {
                        MartialCustomParametersList.ItemsSource = culture.MartialCustomDefinition.Parameters;
                        DetailMartialCustomExpander.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MartialCustomParametersList.ItemsSource = null;
                        DetailMartialCustomExpander.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    DetailMartialCustomValue.Text = string.IsNullOrEmpty(culture.MartialCustom) ? "-" : culture.MartialCustom;
                    MartialCustomParametersList.ItemsSource = null;
                    DetailMartialCustomExpander.Visibility = Visibility.Collapsed;
                }

                if (culture.HeadDeterminationDefinition != null)
                {
                    DetailHeadDeterminationValue.Text = culture.HeadDeterminationDefinition.DisplayName;
                    if (culture.HeadDeterminationDefinition.Parameters.Count > 0)
                    {
                        HeadDeterminationParametersList.ItemsSource = culture.HeadDeterminationDefinition.Parameters;
                        DetailHeadDeterminationExpander.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        HeadDeterminationParametersList.ItemsSource = null;
                        DetailHeadDeterminationExpander.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    DetailHeadDeterminationValue.Text = string.IsNullOrEmpty(culture.HeadDetermination) ? "-" : culture.HeadDetermination;
                    HeadDeterminationParametersList.ItemsSource = null;
                    DetailHeadDeterminationExpander.Visibility = Visibility.Collapsed;
                }

                if (culture.NameListDefinition != null)
                {
                    DetailNameListValue.Text = culture.NameListDefinition.DisplayName;
                    if (culture.NameListDefinition.Parameters.Count > 0)
                    {
                        var view = new CollectionViewSource { Source = culture.NameListDefinition.Parameters };
                        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(NameListParameter.Category)));
                        NameListParametersList.ItemsSource = view.View;
                        DetailNameListExpander.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        NameListParametersList.ItemsSource = null;
                        DetailNameListExpander.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    DetailNameListValue.Text = string.IsNullOrEmpty(culture.NameList) ? "-" : culture.NameList;
                    NameListParametersList.ItemsSource = null;
                    DetailNameListExpander.Visibility = Visibility.Collapsed;
                }

                if (culture.Traditions.Count > 0)
                {
                    TraditionsList.ItemsSource = culture.Traditions;
                    DetailTraditionsExpander.Visibility = Visibility.Visible;
                }
                else
                {
                    TraditionsList.ItemsSource = null;
                    DetailTraditionsExpander.Visibility = Visibility.Collapsed;
                }

                bool hasGfx = culture.CoaGfx.Count > 0 || culture.BuildingGfx.Count > 0 || 
                              culture.ClothingGfx.Count > 0 || culture.UnitGfx.Count > 0 ||
                              !string.IsNullOrEmpty(culture.HouseCoaFrame) || !string.IsNullOrEmpty(culture.DynastyCoaFrame) ||
                              !string.IsNullOrEmpty(culture.HouseCoaMaskOffset) || !string.IsNullOrEmpty(culture.HouseCoaMaskScale);

                if (hasGfx)
                {
                    DetailCoaGfxValue.Text = culture.CoaGfx.Count > 0 ? string.Join(", ", culture.CoaGfx) : "-";
                    DetailBuildingGfxValue.Text = culture.BuildingGfx.Count > 0 ? string.Join(", ", culture.BuildingGfx) : "-";
                    DetailClothingGfxValue.Text = culture.ClothingGfx.Count > 0 ? string.Join(", ", culture.ClothingGfx) : "-";
                    DetailUnitGfxValue.Text = culture.UnitGfx.Count > 0 ? string.Join(", ", culture.UnitGfx) : "-";
                    DetailHouseCoaFrameValue.Text = string.IsNullOrEmpty(culture.HouseCoaFrame) ? "-" : culture.HouseCoaFrame;
                    DetailDynastyCoaFrameValue.Text = string.IsNullOrEmpty(culture.DynastyCoaFrame) ? "-" : culture.DynastyCoaFrame;
                    DetailHouseCoaMaskOffsetValue.Text = string.IsNullOrEmpty(culture.HouseCoaMaskOffset) ? "-" : $"{{{culture.HouseCoaMaskOffset}}}";
                    DetailHouseCoaMaskScaleValue.Text = string.IsNullOrEmpty(culture.HouseCoaMaskScale) ? "-" : $"{{{culture.HouseCoaMaskScale}}}";
                    DetailGfxExpander.Visibility = Visibility.Visible;
                    DetailGfxExpander.IsExpanded = true;
                    LoadGfxModel(culture);
                }
                else
                {
                    DetailGfxExpander.Visibility = Visibility.Collapsed;
                    ClearGfxViewport();
                }

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
                StatsGroup.Visibility = Visibility.Visible;
                DetailGroup.Visibility = Visibility.Collapsed;
                DetailEmptyText.Visibility = Visibility.Visible;
                DetailColorSwatch.Visibility = Visibility.Visible;
                DetailColorValue.Visibility = Visibility.Visible;
                DetailColorSwatch.Background = System.Windows.Media.Brushes.Transparent;
                DetailColorValue.Text = "";
                DetailColorInternalText.Visibility = Visibility.Collapsed;
                DetailEthosValue.Text = "";
                EthosParametersList.ItemsSource = null;
                DetailEthosExpander.Visibility = Visibility.Collapsed;
                HeritageParametersList.ItemsSource = null;
                DetailHeritageExpander.Visibility = Visibility.Collapsed;
                DetailLanguageValue.Text = "";
                LanguageParametersList.ItemsSource = null;
                DetailLanguageExpander.Visibility = Visibility.Collapsed;
                DetailEthnicitiesValue.Text = "";
                DetailMartialCustomValue.Text = "";
                MartialCustomParametersList.ItemsSource = null;
                DetailMartialCustomExpander.Visibility = Visibility.Collapsed;
                DetailHeadDeterminationValue.Text = "";
                HeadDeterminationParametersList.ItemsSource = null;
                DetailHeadDeterminationExpander.Visibility = Visibility.Collapsed;
                DetailNameListValue.Text = "";
                NameListParametersList.ItemsSource = null;
                DetailNameListExpander.Visibility = Visibility.Collapsed;
                TraditionsList.ItemsSource = null;
                DetailTraditionsExpander.Visibility = Visibility.Collapsed;
                DetailGfxExpander.Visibility = Visibility.Collapsed;
                ClearGfxViewport();
            }
        }

        private static List<string> ResolveBuildingMeshes(string gameRoot, List<string> keys)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();
            if (string.IsNullOrEmpty(gameRoot) || keys.Count == 0) return result;
            var db = GetBuildingDb(gameRoot);
            if (db == null) return result;
            foreach (var k in keys)
            {
                foreach (var m in db.ResolveMeshes(k))
                    if (set.Add(m))
                        result.Add(m);
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        private static Dictionary<string, string> GetMeshFileIndex(string gameRoot)
        {
            if (string.IsNullOrEmpty(gameRoot)) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            lock (_meshFileIndexCache)
            {
                if (_meshFileIndexCache.TryGetValue(gameRoot, out var idx)) return idx;
                idx = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string dir = Path.Combine(gameRoot, "gfx", "models");
                if (Directory.Exists(dir))
                {
                    foreach (var f in Directory.EnumerateFiles(dir, "*.mesh", SearchOption.AllDirectories))
                        idx[Path.GetFileNameWithoutExtension(f)] = f;
                }
                _meshFileIndexCache[gameRoot] = idx;
                return idx;
            }
        }

        private static PdxModIDE.ModelEngine.PdxModel? GetParsedModel(string meshPath)
        {
            if (string.IsNullOrEmpty(meshPath)) return null;
            lock (_modelCache)
            {
                if (_modelCache.TryGetValue(meshPath, out var cached)) return cached;
            }
            try
            {
                var model = PdxModIDE.ModelEngine.PdxMeshParser.ParseMeshFile(meshPath);
                lock (_modelCache) _modelCache[meshPath] = model;
                return model;
            }
            catch (Exception ex)
            {
                LogGfx($"meshes parse '{meshPath}': {ex.Message}");
                return null;
            }
        }

        private static Dictionary<string, string> GetTextureFileIndex(string gameRoot)
        {
            if (string.IsNullOrEmpty(gameRoot)) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            lock (_textureFileIndexCache)
            {
                if (_textureFileIndexCache.TryGetValue(gameRoot, out var idx)) return idx;
                idx = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string dir = Path.Combine(gameRoot, "gfx", "models");
                if (Directory.Exists(dir))
                {
                    foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                    {
                        string ext = Path.GetExtension(f);
                        if (ext.Equals(".dds", StringComparison.OrdinalIgnoreCase) || ext.Equals(".tga", StringComparison.OrdinalIgnoreCase))
                            idx[Path.GetFileName(f)] = f;
                    }
                }
                _textureFileIndexCache[gameRoot] = idx;
                return idx;
            }
        }

        private static List<string> GetAssetUniqueTextures(string gameRoot, string meshPath)
        {
            string key = meshPath;
            lock (_assetUniqueTextureCache)
            {
                if (_assetUniqueTextureCache.TryGetValue(key, out var cached)) return cached;
            }

            var result = new List<string>();
            try
            {
                string assetPath = Path.ChangeExtension(meshPath, ".asset");
                if (File.Exists(assetPath))
                {
                    string text = File.ReadAllText(assetPath);
                    var matches = System.Text.RegularExpressions.Regex.Matches(text, @"texture\s*=\s*\{\s*file\s*=\s*""([^""]+)""");
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        if (match.Groups.Count < 2) continue;
                        string? resolved = FindTextureFile(gameRoot, meshPath, match.Groups[1].Value);
                        if (resolved != null) result.Add(resolved);
                    }
                }
            }
            catch { }

            lock (_assetUniqueTextureCache) _assetUniqueTextureCache[key] = result;
            return result;
        }

        private static string? FindTextureFile(string gameRoot, string meshPath, string textureName)
        {
            if (string.IsNullOrEmpty(textureName)) return null;
            string? meshDir = Path.GetDirectoryName(meshPath);
            var texIndex = GetTextureFileIndex(gameRoot);
            string? resolved = meshDir != null ? Path.Combine(meshDir, textureName) : null;
            if (resolved == null || !File.Exists(resolved))
                resolved = texIndex.TryGetValue(Path.GetFileName(textureName), out var f) ? f : null;
            return resolved;
        }

        private static string? FindBuildingMeshFile(string gameRoot, string meshName)
        {
            string name = meshName.Trim();
            if (string.IsNullOrEmpty(name)) return null;
            string baseName = Path.GetFileNameWithoutExtension(Path.GetFileName(name.Replace("\\", "/")));
            if (baseName.EndsWith("_mesh", StringComparison.Ordinal))
                baseName = baseName.Substring(0, baseName.Length - 5);
            var idx = GetMeshFileIndex(gameRoot);
            return idx.TryGetValue(baseName, out var p) ? p : null;
        }

        private async void RenderBuildingGrid(string gameRoot, CultureInfo culture)
        {
            if (BuildingGfxGrid == null) return;
            BuildingGfxGrid.Children.Clear();
            BuildingGfxGridHost.Visibility = Visibility.Collapsed;
            if (BuildingGfxLoading != null) BuildingGfxLoading.Visibility = Visibility.Visible;

            var names = ResolveBuildingMeshes(gameRoot, culture.BuildingGfx);
            if (names.Count == 0)
            {
                if (BuildingGfxLoading != null) BuildingGfxLoading.Visibility = Visibility.Collapsed;
                _buildingSectionLoaded = true;
                return;
            }

            var items = new List<KeyValuePair<string, string>>();
            foreach (var name in names)
            {
                string? meshPath = FindBuildingMeshFile(gameRoot, name);
                if (meshPath == null) { LogGfx($"building no mesh file for '{name}'"); continue; }
                items.Add(new KeyValuePair<string, string>(name, meshPath));
            }
            if (items.Count == 0)
            {
                if (BuildingGfxLoading != null) BuildingGfxLoading.Visibility = Visibility.Collapsed;
                _buildingSectionLoaded = true;
                return;
            }

            var grid = BuildingGfxGrid;
            var host = BuildingGfxGridHost;
            _buildingLoadCts?.Cancel();
            var cts = _buildingLoadCts = new CancellationTokenSource();
            var token = cts.Token;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await Task.Run(() =>
                {
                    Parallel.ForEach(items, item =>
                    {
                        if (token.IsCancellationRequested) return;
                        try
                        {
                            var model = GetParsedModel(item.Value);
                            if (model != null)
                            {
                                foreach (var mesh in model.Meshes)
                                    FindDiffuseFile(gameRoot, item.Value, mesh.DiffuseTexture);
                            }
                        }
                        catch { }
                    });
                }, token);

                if (token.IsCancellationRequested || _buildingLoadCts != cts) return;

                int total = items.Count;
                for (int i = 0; i < total; i++)
                {
                    if (token.IsCancellationRequested || grid != BuildingGfxGrid) return;
                    string name = items[i].Key, meshPath = items[i].Value;
                    var vp = BuildMeshCellViewport(gameRoot, meshPath);
                    if (vp == null) continue;
                    if (token.IsCancellationRequested || grid != BuildingGfxGrid) return;
                    grid.Children.Add(BuildMeshCell(name, meshPath, vp, gameRoot));
                }

                if (grid.Children.Count > 0)
                    host.Visibility = Visibility.Visible;
                _buildingSectionLoaded = true;
                if (BuildingGfxLoading != null) BuildingGfxLoading.Visibility = Visibility.Collapsed;
                LogGfx($"building grid {total} celdas en {sw.ElapsedMilliseconds} ms");
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _buildingSectionLoaded = true;
                if (BuildingGfxLoading != null) BuildingGfxLoading.Visibility = Visibility.Collapsed;
                LogGfx($"building grid error: {ex.Message}");
            }
        }

        private CancellationTokenSource? _buildingLoadCts;
        private CultureInfo? _gfxCulture;
        private bool _buildingSectionLoaded;
        private bool _clothingSectionLoaded;
        private bool _unitSectionLoaded;

        private sealed class MeshPreviewData
        {
            public string MeshPath = "";
            public string? AssetPath;
            public System.Windows.Media.Brush? PaintedBrush;
        }

        private System.Windows.Controls.Border BuildMeshCell(string name, string meshPath, Viewport3D vp, string gameRoot, string? assetPath = null, System.Windows.Media.Brush? paintedBrush = null)
        {
            vp.Width = 146; vp.Height = 146; vp.Margin = new Thickness(6, 2, 6, 6);
            var tb = new TextBlock
            {
                Text = name,
                Margin = new Thickness(4, 4, 4, 2),
                FontSize = 11,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = System.Windows.TextAlignment.Center,
                MaxHeight = 34,
                ToolTip = name,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE8, 0xC9, 0x7E))
            };
            var sp = new StackPanel();
            sp.Children.Add(tb);
            sp.Children.Add(vp);

            var cell = new System.Windows.Controls.Border
            {
                Width = 166,
                Height = 198,
                Margin = new Thickness(4),
                CornerRadius = new CornerRadius(2),
                BorderBrush = (System.Windows.Media.Brush)(System.Windows.Application.Current?.TryFindResource("ControlBorder")
                    ?? System.Windows.Media.Brushes.Gray),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 32, 36))
            };
            cell.Child = sp;
            cell.Tag = new MeshPreviewData { MeshPath = meshPath, AssetPath = assetPath, PaintedBrush = paintedBrush };
            cell.MouseLeftButtonDown += Cell_MouseLeftButtonDown;
            return cell;
        }

        private void Cell_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2) return;
            if (sender is System.Windows.Controls.Border cell && cell.Tag is MeshPreviewData data)
            {
                string gameRoot = _viewModel?.CurrentProfile?.GameRoot ?? "";
                OpenMeshPreview(data, gameRoot);
            }
        }

        private void OpenMeshPreview(MeshPreviewData data, string gameRoot)
        {
            if (data == null || string.IsNullOrEmpty(gameRoot)) return;
            if (string.IsNullOrEmpty(data.MeshPath) && string.IsNullOrEmpty(data.AssetPath)) return;
            if (string.IsNullOrEmpty(data.MeshPath) || !File.Exists(data.MeshPath)) return;

            var vp = BuildMeshCellViewport(gameRoot, data.MeshPath, null, data.PaintedBrush);
            if (vp == null) return;
            vp.Width = 520; vp.Height = 480; vp.Margin = new Thickness(16, 12, 16, 16);

            string name = Path.GetFileNameWithoutExtension(data.MeshPath);

            var tb = new TextBlock
            {
                Text = name,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(16, 12, 16, 0),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xE8, 0xC9, 0x7E))
            };
            var sp = new StackPanel();
            sp.Children.Add(tb);
            sp.Children.Add(vp);

            var winContainer = new System.Windows.Controls.Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 32, 36)),
                Child = sp
            };

            var winWindow = new Window
            {
                Title = name,
                Width = 560,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
                Content = winContainer
            };
            winWindow.ShowDialog();
        }

        private static Viewport3D? BuildMeshCellViewport(string gameRoot, string meshPath, string? preferredDiffuse = null, System.Windows.Media.Brush? paintedBrush = null)
        {
            var model = GetParsedModel(meshPath);
            if (model == null) return null;

            double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;
            foreach (var m in model.Meshes)
            {
                if (IsCollisionShader(m.Shader)) continue;
                if (m.Positions == null || m.Positions.Length % 3 != 0) continue;
                for (int i = 0; i < m.Positions.Length; i += 3)
                {
                    if (m.Positions[i] < minX) minX = m.Positions[i]; if (m.Positions[i] > maxX) maxX = m.Positions[i];
                    if (m.Positions[i + 1] < minY) minY = m.Positions[i + 1]; if (m.Positions[i + 1] > maxY) maxY = m.Positions[i + 1];
                    if (m.Positions[i + 2] < minZ) minZ = m.Positions[i + 2]; if (m.Positions[i + 2] > maxZ) maxZ = m.Positions[i + 2];
                }
            }
            if (double.IsInfinity(minX)) return null;

            double size = Math.Max(maxX - minX, Math.Max(maxY - minY, maxZ - minZ));
            if (size < 1e-6) size = 1;
            double scale = 1.0 / size;
            double cx = (minX + maxX) / 2, cy = (minY + maxY) / 2, cz = (minZ + maxZ) / 2;
            double radius = 0;

            var modelGroup = new Model3DGroup();
            int validSubmeshes = 0;
            foreach (var m in model.Meshes)
            {
                if (IsCollisionShader(m.Shader)) continue;
                if (m.Positions == null || m.Positions.Length % 3 != 0) continue;
                if (m.Triangles == null || m.Triangles.Length == 0) continue;

                var positions = new Point3DCollection();
                int vertexCount = m.Positions.Length / 3;
                for (int i = 0; i < m.Positions.Length; i += 3)
                    positions.Add(new Point3D(m.Positions[i], m.Positions[i + 1], m.Positions[i + 2]));

                var triangleIndices = new Int32Collection();
                foreach (var t in m.Triangles)
                {
                    if (t < 0 || t >= vertexCount) continue;
                    triangleIndices.Add(t);
                }
                if (triangleIndices.Count == 0) continue;

                var textureCoordinates = new PointCollection();
                var textureCoords2 = new PointCollection();
                if (m.UVSets != null && m.UVSets.Count > 0 && m.UVSets[0] != null && m.UVSets[0].Length >= vertexCount * 2)
                {
                    var uv = m.UVSets[0];
                    for (int i = 0; i < vertexCount; i++)
                        textureCoordinates.Add(new System.Windows.Point(uv[i * 2], 1 - uv[i * 2 + 1]));
                }
                if (m.UVSets != null && m.UVSets.Count > 1 && m.UVSets[1] != null && m.UVSets[1].Length >= vertexCount * 2)
                {
                    var uv = m.UVSets[1];
                    for (int i = 0; i < vertexCount; i++)
                        textureCoords2.Add(new System.Windows.Point(uv[i * 2], 1 - uv[i * 2 + 1]));
                }

                for (int i = 0; i < positions.Count; i++)
                {
                    var p = positions[i];
                    positions[i] = new Point3D((p.X - cx) * scale, (p.Y - cy) * scale, (p.Z - cz) * scale);
                    var q = positions[i];
                    double l2 = q.X * q.X + q.Y * q.Y + q.Z * q.Z;
                    if (l2 > radius) radius = l2;
                }

                var normalsArr = new Vector3D[positions.Count];
                for (int i = 0; i + 2 < triangleIndices.Count; i += 3)
                {
                    int a = triangleIndices[i], b = triangleIndices[i + 1], c = triangleIndices[i + 2];
                    var n = Vector3D.CrossProduct(positions[b] - positions[a], positions[c] - positions[a]);
                    if (n.Length < 1e-12) continue;
                    n.Normalize();
                    normalsArr[a] += n; normalsArr[b] += n; normalsArr[c] += n;
                }
                var normalsColl = new Vector3DCollection();
                foreach (var n in normalsArr)
                {
                    var nn = n;
                    if (nn.LengthSquared > 1e-12) nn.Normalize();
                    normalsColl.Add(nn);
                }

                var meshGeometry = new MeshGeometry3D
                {
                    Positions = positions,
                    TriangleIndices = triangleIndices,
                    Normals = normalsColl
                };

                var materialGroup = new MaterialGroup();
                System.Windows.Media.Brush diffuseBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(175, 175, 180));
                System.Windows.Media.Color diffuseTint = System.Windows.Media.Color.FromRgb(255, 255, 255);
                var uniquePaths = GetAssetUniqueTextures(gameRoot, meshPath);
                string? chosenDiffusePath = ResolveMeshDiffuse(gameRoot, meshPath, model, preferredDiffuse ?? m.DiffuseTexture);
                bool useUnique = uniquePaths.Count > 0 && textureCoords2.Count == positions.Count;

                if (paintedBrush != null)
                {
                    diffuseBrush = paintedBrush;
                    useUnique = false;
                }
                else if (useUnique)
                {
                    string? up = uniquePaths.FirstOrDefault(p => File.Exists(p));
                    if (up != null)
                    {
                        var atlasTex = chosenDiffusePath != null && File.Exists(chosenDiffusePath) ? LoadTexture(chosenDiffusePath) : null;
                        if (atlasTex != null) { atlasTex.Freeze(); diffuseBrush = new ImageBrush(atlasTex); }
                        var tintRgb = AvgColorRgb(up);
                        if (tintRgb != null)
                            diffuseTint = System.Windows.Media.Color.FromRgb(
                                (byte)System.Math.Min(255, tintRgb.Value.R + (255 - tintRgb.Value.R) * 70 / 100),
                                (byte)System.Math.Min(255, tintRgb.Value.G + (255 - tintRgb.Value.G) * 70 / 100),
                                (byte)System.Math.Min(255, tintRgb.Value.B + (255 - tintRgb.Value.B) * 70 / 100));
                    }
                }
                else if (chosenDiffusePath != null && File.Exists(chosenDiffusePath))
                {
                    var tex = LoadTexture(chosenDiffusePath);
                    if (tex != null) { tex.Freeze(); diffuseBrush = new ImageBrush(tex); }
                }
                materialGroup.Children.Add(new DiffuseMaterial(diffuseBrush) { Color = diffuseTint });
                materialGroup.Children.Add(new SpecularMaterial(new SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60)), 40));

                var geom = new GeometryModel3D(meshGeometry, materialGroup) { BackMaterial = materialGroup };
                if (useUnique)
                {
                    if (textureCoords2.Count == positions.Count)
                        meshGeometry.TextureCoordinates = textureCoords2;
                }
                else
                {
                    if (textureCoordinates.Count == positions.Count)
                        meshGeometry.TextureCoordinates = textureCoordinates;
                }
                modelGroup.Children.Add(geom);
                validSubmeshes++;
            }

            if (validSubmeshes == 0) return null;

            radius = Math.Sqrt(radius);
            if (radius < 1e-4) radius = 0.5;

            var group = new Model3DGroup();
            group.Children.Add(new AmbientLight(System.Windows.Media.Color.FromRgb(205, 205, 205)));
            group.Children.Add(new DirectionalLight(System.Windows.Media.Color.FromRgb(245, 245, 245), new Vector3D(0.35, -0.25, 1)));
            group.Children.Add(new DirectionalLight(System.Windows.Media.Color.FromRgb(185, 200, 220), new Vector3D(-0.7, -0.2, -0.6)));
            group.Children.Add(new DirectionalLight(System.Windows.Media.Color.FromRgb(150, 165, 185), new Vector3D(1, 0.55, 0.5)));
            group.Children.Add(modelGroup);

            double fovDeg = 48;
            double half = fovDeg * 0.5 * Math.PI / 180.0;
            double distance = radius / Math.Tan(half) * 1.06;
            if (distance < 0.35) distance = 0.35;
            var camera = new PerspectiveCamera(
                new Point3D(0, 0, -distance), new Vector3D(0, 0, 1), new Vector3D(0, 1, 0), fovDeg)
            { NearPlaneDistance = 0.02, FarPlaneDistance = distance * 40 };

            var vp = new Viewport3D { Camera = camera };
            vp.Children.Add(new ModelVisual3D { Content = group });
            return vp;
        }

        private void LoadGfxModel(CultureInfo culture)
        {
            ClearGfxViewport();
            string gameRoot = _viewModel?.CurrentProfile?.GameRoot ?? "";
            _gfxCulture = culture;
            _buildingSectionLoaded = _clothingSectionLoaded = _unitSectionLoaded = false;
            _buildingLoadCts?.Cancel();
            _buildingLoadCts = null;

            if (string.IsNullOrEmpty(gameRoot) || !Directory.Exists(gameRoot))
                return;

            HideSection(BuildingGfxGrid, BuildingGfxGridHost, BuildingGfxLoading);
            HideSection(ClothingGfxGrid, ClothingGfxGridHost, ClothingGfxLoading);
            HideSection(UnitGfxGrid, UnitGfxGridHost, UnitGfxLoading);
        }

        private void ModelExpander_Expanded(object sender, System.Windows.RoutedEventArgs e)
        {
            string gameRoot = _viewModel?.CurrentProfile?.GameRoot ?? "";
            if (string.IsNullOrEmpty(gameRoot) || !Directory.Exists(gameRoot)) return;
            if (_gfxCulture == null) return;

            if (sender == (object)BuildingExpander && !_buildingSectionLoaded)
                RenderBuildingGrid(gameRoot, _gfxCulture);
            else if (sender == (object)ClothingExp && !_clothingSectionLoaded)
                RenderClothingGrid(gameRoot, _gfxCulture);
            else if (sender == (object)UnitExp && !_unitSectionLoaded)
                RenderUnitGrid(gameRoot, _gfxCulture);
        }

        private void ModelExpander_Collapsed(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender == (object)BuildingExpander)
            {
                _buildingLoadCts?.Cancel();
                if (BuildingGfxLoading != null) BuildingGfxLoading.Visibility = Visibility.Collapsed;
            }
        }

        private static void HideSection(WrapPanel grid, Border host, TextBlock loading)
        {
            if (grid != null) grid.Children.Clear();
            if (host != null) host.Visibility = Visibility.Collapsed;
            if (loading != null) loading.Visibility = Visibility.Collapsed;
        }

        private async void RenderUnitGrid(string gameRoot, CultureInfo culture)
        {
            if (UnitGfxGrid == null) return;
            var host = UnitGfxGridHost;
            UnitGfxGrid.Children.Clear();
            if (host != null) host.Visibility = Visibility.Collapsed;
            if (UnitGfxLoading != null) UnitGfxLoading.Visibility = Visibility.Visible;

            if (_unitResolver == null)
            {
                if (UnitGfxLoading != null) UnitGfxLoading.Visibility = Visibility.Collapsed;
                _unitSectionLoaded = true;
                RenderGfxItemGrid(gameRoot, culture.UnitGfx, UnitGfxGrid, host, "unit");
                return;
            }

            var resolvedMeshes = _unitResolver.ResolveUnits(culture.UnitGfx);
            if (resolvedMeshes.Count == 0)
            {
                LogGfx("unit resolver: 0 meshes, fallback a prefijo");
                if (UnitGfxLoading != null) UnitGfxLoading.Visibility = Visibility.Collapsed;
                _unitSectionLoaded = true;
                RenderGfxItemGrid(gameRoot, culture.UnitGfx, UnitGfxGrid, host, "unit");
                return;
            }

            try
            {
                await Task.Run(() =>
                {
                    Parallel.ForEach(resolvedMeshes, r =>
                    {
                        var model = GetParsedModel(r.MeshPath);
                        if (model != null)
                        {
                            foreach (var mesh in model.Meshes)
                                FindDiffuseFile(gameRoot, r.MeshPath, mesh.DiffuseTexture);
                        }
                    });
                });

                foreach (var r in resolvedMeshes)
                {
                    var vp = BuildMeshCellViewport(gameRoot, r.MeshPath, r.DiffusePath, null);
                    if (vp == null) continue;
                    UnitGfxGrid.Children.Add(BuildMeshCell(r.Name, r.MeshPath, vp, gameRoot, r.AssetPath, null));
                }

                if (UnitGfxGrid.Children.Count > 0 && host != null)
                    host.Visibility = Visibility.Visible;
                _unitSectionLoaded = true;
                if (UnitGfxLoading != null) UnitGfxLoading.Visibility = Visibility.Collapsed;
                LogGfx($"unit resolver grid: {UnitGfxGrid.Children.Count} celdas de {resolvedMeshes.Count}");
            }
            catch (Exception ex)
            {
                _unitSectionLoaded = true;
                if (UnitGfxLoading != null) UnitGfxLoading.Visibility = Visibility.Collapsed;
                LogGfx($"unit grid error: {ex.Message}");
            }
        }

        private async void RenderClothingGrid(string gameRoot, CultureInfo culture)
        {
            if (ClothingGfxGrid == null || _clothingResolver == null)
            {
                if (ClothingGfxLoading != null) ClothingGfxLoading.Visibility = Visibility.Collapsed;
                _clothingSectionLoaded = true;
                return;
            }
            ClothingGfxGrid.Children.Clear();
            ClothingGfxGridHost.Visibility = Visibility.Collapsed;
            if (ClothingGfxLoading != null) ClothingGfxLoading.Visibility = Visibility.Visible;

            var resolvedMeshes = _clothingResolver.ResolveClothing(culture.ClothingGfx);
            if (resolvedMeshes.Count == 0)
            {
                LogGfx("clothing resolver: 0 meshes, fallback a prefijo");
                if (ClothingGfxLoading != null) ClothingGfxLoading.Visibility = Visibility.Collapsed;
                _clothingSectionLoaded = true;
                RenderGfxItemGrid(gameRoot, culture.ClothingGfx, ClothingGfxGrid, ClothingGfxGridHost, "clothing");
                return;
            }

            try
            {
                await Task.Run(() =>
                {
                    Parallel.ForEach(resolvedMeshes, r =>
                    {
                        var model = GetParsedModel(r.MeshPath);
                        if (model != null)
                        {
                            foreach (var mesh in model.Meshes)
                                FindDiffuseFile(gameRoot, r.MeshPath, mesh.DiffuseTexture);
                        }
                    });
                });

                foreach (var r in resolvedMeshes)
                {
                    System.Windows.Media.Brush? painted = null;
                    if (!string.IsNullOrEmpty(r.AssetPath) && File.Exists(r.AssetPath))
                    {
                        try
                        {
                            var paintedDds = PdxModIDE.ModelEngine.PdxClothingPainter.Paint(gameRoot, r.AssetPath, r.MeshPath);
                            if (paintedDds != null)
                            {
                                var paintedBmp = ToBitmapSource(paintedDds);
                                if (paintedBmp != null)
                                {
                                    paintedBmp.Freeze();
                                    painted = new ImageBrush(paintedBmp);
                                }
                            }
                        }
                        catch { }
                    }
                    var vp = BuildMeshCellViewport(gameRoot, r.MeshPath, r.DiffusePath, painted);
                    if (vp == null) continue;
                    ClothingGfxGrid.Children.Add(BuildMeshCell(r.Name, r.MeshPath, vp, gameRoot, r.AssetPath, painted));
                }

                if (ClothingGfxGrid.Children.Count > 0)
                    ClothingGfxGridHost.Visibility = Visibility.Visible;
                _clothingSectionLoaded = true;
                if (ClothingGfxLoading != null) ClothingGfxLoading.Visibility = Visibility.Collapsed;
                LogGfx($"clothing resolver grid: {ClothingGfxGrid.Children.Count} celdas de {resolvedMeshes.Count}");
            }
            catch (Exception ex)
            {
                _clothingSectionLoaded = true;
                if (ClothingGfxLoading != null) ClothingGfxLoading.Visibility = Visibility.Collapsed;
                LogGfx($"clothing grid error: {ex.Message}");
            }
        }

        private List<string> ExpandMeshVariants(string gameRoot, string key)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(key)) return result;
            string keyName = Path.GetFileName(key.Replace("\\", "/"));
            string keyBase = Path.GetFileNameWithoutExtension(keyName);
            if (string.IsNullOrEmpty(keyBase)) return result;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_gfxResolver != null)
            {
                var resolved = _gfxResolver.Resolve(key);
                if (resolved != null && !string.IsNullOrEmpty(resolved.MeshPath) && File.Exists(resolved.MeshPath) && seen.Add(resolved.MeshPath))
                    result.Add(resolved.MeshPath);
            }
            string? exact = FindMeshFile(gameRoot, key);
            if (exact != null && File.Exists(exact) && seen.Add(exact)) result.Add(exact);

            foreach (var kv in GetMeshFileIndex(gameRoot))
            {
                if (!kv.Key.StartsWith(keyBase, StringComparison.OrdinalIgnoreCase)) continue;
                if (kv.Key.EndsWith("_lod", StringComparison.OrdinalIgnoreCase)) continue;
                if (File.Exists(kv.Value) && seen.Add(kv.Value)) result.Add(kv.Value);
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        private async void RenderGfxItemGrid(string gameRoot, List<string> keys, WrapPanel grid, Border? host, string kind)
        {
            if (grid == null) return;
            grid.Children.Clear();
            if (host != null) host.Visibility = Visibility.Collapsed;

            var items = new List<KeyValuePair<string, string>>();
            foreach (var key in keys)
            {
                if (string.IsNullOrEmpty(key)) continue;
                foreach (var meshPath in ExpandMeshVariants(gameRoot, key))
                    items.Add(new KeyValuePair<string, string>(Path.GetFileNameWithoutExtension(meshPath), meshPath));
            }
            if (items.Count == 0) return;

            try
            {
                await Task.Run(() =>
                {
                    Parallel.ForEach(items, item =>
                    {
                        var model = GetParsedModel(item.Value);
                        if (model != null)
                        {
                            foreach (var mesh in model.Meshes)
                                FindDiffuseFile(gameRoot, item.Value, mesh.DiffuseTexture);
                        }
                    });
                });

                int total = items.Count;
                for (int i = 0; i < total; i++)
                {
                    string name = items[i].Key, meshPath = items[i].Value;
                    var vp = BuildMeshCellViewport(gameRoot, meshPath);
                    if (vp == null) continue;
                    grid.Children.Add(BuildMeshCell(name, meshPath, vp, gameRoot));
                }

                if (grid.Children.Count > 0 && host != null)
                    host.Visibility = Visibility.Visible;
                LogGfx($"{kind} grid {grid.Children.Count} celdas de {total}");
            }
            catch (Exception ex)
            {
                LogGfx($"{kind} grid error: {ex.Message}");
            }
        }

        private static bool IsCollisionShader(string? shader)
        {
            if (string.IsNullOrEmpty(shader)) return false;
            return shader.StartsWith("Collision", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetFirstDiffuse(PdxModel model)
        {
            foreach (var m in model.Meshes)
            {
                if (!string.IsNullOrEmpty(m.DiffuseTexture))
                    return m.DiffuseTexture;
            }
            return "";
        }

        private static string? ResolveMeshDiffuse(string gameRoot, string meshPath, PdxModel model, string? preferred)
        {
            if (!string.IsNullOrEmpty(preferred) && File.Exists(preferred))
                return preferred;
            string? fromMesh = FindDiffuseFile(gameRoot, meshPath, GetFirstDiffuse(model));
            if (fromMesh != null) return fromMesh;
            string baseName = Path.GetFileNameWithoutExtension(meshPath);
            string convention = baseName + "_diffuse.dds";
            string? conventionPath = FindDiffuseFile(gameRoot, meshPath, convention);
            if (conventionPath != null) return conventionPath;
            return null;
        }

        private static string? FindDiffuseFile(string gameRoot, string meshPath, string diffuseTexture)
        {            if (string.IsNullOrEmpty(diffuseTexture)) return null;
            string? meshDir = Path.GetDirectoryName(meshPath);
            var texIndex = GetTextureFileIndex(gameRoot);

            string? resolvedPath = meshDir != null ? Path.Combine(meshDir, diffuseTexture) : null;
            if (resolvedPath == null || !File.Exists(resolvedPath))
            {
                resolvedPath = null;
                if (texIndex.TryGetValue(Path.GetFileName(diffuseTexture), out var f))
                    resolvedPath = f;
            }
            if (resolvedPath != null && File.Exists(resolvedPath) && IsGoodDiffuse(resolvedPath))
                return resolvedPath;
            return null;
        }

        private static PdxModIDE.ModelEngine.DdsImage? GetDecoded(string filePath)
        {
            lock (_textureDecodeCache)
            {
                if (_textureDecodeCache.TryGetValue(filePath, out var cached)) return cached;
                PdxModIDE.ModelEngine.DdsImage? dds = null;
                try { dds = DdsDecoder.Decode(filePath); } catch { }
                _textureDecodeCache[filePath] = dds;
                return dds;
            }
        }

        private static bool IsGoodDiffuse(string filePath)
        {
            try
            {
                var dds = GetDecoded(filePath);
                if (dds == null || dds.Data == null || dds.Data.Length < 4) return false;
                long total = dds.Data.Length / 4;
                long dark = 0;
                for (int i = 0; i + 3 < dds.Data.Length; i += 4)
                {
                    int r = dds.Data[i], g = dds.Data[i + 1], b = dds.Data[i + 2];
                    if (r + g + b < 24) dark++;
                }
                return total > 0 && (double)dark / total < 0.90;
            }
            catch { return false; }
        }

        private static string AvgColor(string filePath)
        {
            try
            {
                var dds = GetDecoded(filePath);
                if (dds == null || dds.Data == null || dds.Data.Length < 4) return "(decode-fail)";
                long r = 0, g = 0, b = 0;
                long n = 0;
                for (int i = 0; i + 3 < dds.Data.Length; i += 4) { r += dds.Data[i]; g += dds.Data[i + 1]; b += dds.Data[i + 2]; n++; }
                return n > 0 ? $"({r / n},{g / n},{b / n})" : "(empty)";
            }
            catch { return "(err)"; }
        }

        private static System.Windows.Media.Color? AvgColorRgb(string filePath)
        {
            try
            {
                var dds = GetDecoded(filePath);
                if (dds == null || dds.Data == null || dds.Data.Length < 4) return null;
                long r = 0, g = 0, b = 0;
                long n = 0;
                for (int i = 0; i + 3 < dds.Data.Length; i += 4) { r += dds.Data[i]; g += dds.Data[i + 1]; b += dds.Data[i + 2]; n++; }
                if (n == 0) return null;
                return System.Windows.Media.Color.FromRgb((byte)(r / n), (byte)(g / n), (byte)(b / n));
            }
            catch { return null; }
        }

        private static string? FindMeshFile(string gameRoot, string key)
        {
            string modelsDir = Path.Combine(gameRoot, "gfx", "models");
            if (!Directory.Exists(modelsDir)) return null;

            string name = Path.GetFileName(key.Replace("\\", "/")).Replace(".mesh", "").ToLowerInvariant();
            string baseFile = name + ".mesh";
            string lodFile = name + "_lod.mesh";

            foreach (var file in Directory.EnumerateFiles(modelsDir, "*.mesh", SearchOption.AllDirectories))
            {
                string baseName = Path.GetFileName(file).ToLowerInvariant();
                if (baseName == baseFile || baseName == lodFile)
                    return file;
            }
            return null;
        }

        private void ClearGfxViewport()
        {
            _buildingLoadCts?.Cancel();
            _buildingLoadCts = null;
            if (BuildingGfxGrid != null) BuildingGfxGrid.Children.Clear();
            if (BuildingGfxGridHost != null) BuildingGfxGridHost.Visibility = Visibility.Collapsed;
            if (ClothingGfxGrid != null) ClothingGfxGrid.Children.Clear();
            if (ClothingGfxGridHost != null) ClothingGfxGridHost.Visibility = Visibility.Collapsed;
            if (UnitGfxGrid != null) UnitGfxGrid.Children.Clear();
            if (UnitGfxGridHost != null) UnitGfxGridHost.Visibility = Visibility.Collapsed;
            foreach (var vp in new[] { CoaGfxViewport3D })
            {
                if (vp != null) { vp.Children.Clear(); vp.Camera = null; }
            }
            foreach (var b in new[] { CoaGfxViewportBorder })
            {
                if (b != null) b.Visibility = Visibility.Collapsed;
            }
        }

        private static System.Windows.Media.Imaging.BitmapSource? LoadTexture(string filePath)
        {
            lock (_textureBitmapCache)
            {
                if (_textureBitmapCache.TryGetValue(filePath, out var cached)) return cached;
            }
            try
            {
                var dds = GetDecoded(filePath);
                if (dds == null || dds.Data == null || dds.Data.Length == 0) return null;
                var bmp = ToBitmapSource(dds);
                if (bmp == null) return null;
                lock (_textureBitmapCache) _textureBitmapCache[filePath] = bmp;
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        private static System.Windows.Media.Imaging.BitmapSource? ToBitmapSource(PdxModIDE.ModelEngine.DdsImage dds)
        {
            if (dds.Data == null || dds.Data.Length == 0 || dds.Width <= 0 || dds.Height <= 0) return null;
            int stride = dds.Width * 4;
            byte[] src = dds.Data;
            byte[] pixels = new byte[src.Length];
            for (int i = 0; i + 3 < src.Length; i += 4)
            {
                pixels[i] = src[i + 2];
                pixels[i + 1] = src[i + 1];
                pixels[i + 2] = src[i];
                pixels[i + 3] = src[i + 3];
            }
            var bmp = System.Windows.Media.Imaging.BitmapSource.Create(
                dds.Width, dds.Height, 96, 96,
                System.Windows.Media.PixelFormats.Bgra32,
                null, pixels, stride);
            bmp.Freeze();
            return bmp;
        }

        private static void ExtractGfxAttributes(string block, CultureInfo culture)
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

                if (key == "coa_gfx")
                {
                    if (block[pos] == '{')
                    {
                        string content = ReadBraceContent(block, ref pos);
                        culture.CoaGfx = ExtractGfxList(content);
                    }
                    continue;
                }

                if (key == "building_gfx")
                {
                    if (block[pos] == '{')
                    {
                        string content = ReadBraceContent(block, ref pos);
                        culture.BuildingGfx = ExtractGfxList(content);
                    }
                    continue;
                }

                if (key == "clothing_gfx")
                {
                    if (block[pos] == '{')
                    {
                        string content = ReadBraceContent(block, ref pos);
                        culture.ClothingGfx = ExtractGfxList(content);
                    }
                    continue;
                }

                if (key == "unit_gfx")
                {
                    if (block[pos] == '{')
                    {
                        string content = ReadBraceContent(block, ref pos);
                        culture.UnitGfx = ExtractGfxList(content);
                    }
                    continue;
                }

                if (key == "house_coa_frame")
                {
                    culture.HouseCoaFrame = ExtractSimpleValue(block, ref pos);
                    continue;
                }

                if (key == "dynasty_coa_frame")
                {
                    culture.DynastyCoaFrame = ExtractSimpleValue(block, ref pos);
                    continue;
                }

                if (key == "house_coa_mask_offset")
                {
                    if (block[pos] == '{')
                    {
                        culture.HouseCoaMaskOffset = ReadBraceContent(block, ref pos);
                    }
                    continue;
                }

                if (key == "house_coa_mask_scale")
                {
                    if (block[pos] == '{')
                    {
                        culture.HouseCoaMaskScale = ReadBraceContent(block, ref pos);
                    }
                    continue;
                }

                SkipValueAndFollowingBlock(block, ref pos);
            }
        }

        private static List<string> ExtractGfxList(string content)
        {
            var result = new List<string>();
            int pos = 0;
            while (pos < content.Length)
            {
                SkipWhitespaceAndComments(content, ref pos);
                if (pos >= content.Length) break;
                string key = ReadKey(content, ref pos);
                if (string.IsNullOrEmpty(key)) break;
                result.Add(key);
            }
            return result;
        }

        private static string ExtractSimpleValue(string block, ref int pos)
        {
            if (block[pos] == '"')
            {
                pos++;
                int start = pos;
                while (pos < block.Length && block[pos] != '"') pos++;
                if (pos < block.Length) pos++;
                return block.Substring(start, pos - start - 1);
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

        private static string Res(string key)
        {
            return System.Windows.Application.Current.TryFindResource(key) as string ?? key;
        }

        private static string ResOptional(string key)
        {
            return System.Windows.Application.Current.TryFindResource(key) as string ?? "";
        }
    }
}
