using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
using PdxModIDE.UI.Translation;
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
        public string HistoryLocOverride { get; set; } = "";
        public string Created { get; set; } = "";
        public List<string> Parents { get; set; } = new();
        public List<string> TraditionKeys { get; set; } = new();
        public List<DlcTradition> DlcTraditions { get; set; } = new();
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
        public string NameOrderConvention { get; set; } = "";

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
        public string Source { get; set; } = "Base";
        public string SourceFile { get; set; } = "";
        public bool IsModNew { get; set; }
        public string AudioParameter
        {
            get
            {
                if (!string.IsNullOrEmpty(_audioParameter)) return _audioParameter;
                var param = Parameters.FirstOrDefault(p => string.Equals(p.Key, "audio_parameter", StringComparison.OrdinalIgnoreCase));
                return param?.Content ?? "";
            }
            set => _audioParameter = value ?? "";
        }
        private string? _audioParameter;
        public System.Windows.Media.Brush SourceBrush => Source == "Mod"
            ? (IsModNew
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 140, 0))
                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212)))
            : System.Windows.Media.Brushes.Black;
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
        public string RequiresDlcFlag { get; set; } = "";
        public bool IsDlc { get; set; }
        public List<TraditionParameter> Parameters { get; set; } = new();
    }

    public class DlcTradition
    {
        public string Trait { get; set; } = "";
        public string RequiresDlcFlag { get; set; } = "";
        public string Fallback { get; set; } = "";
    }

    internal class DlcTraditionRowUi
    {
        public System.Windows.Controls.ComboBox Trait = null!;
        public System.Windows.Controls.ComboBox Fallback = null!;
        public DlcTradition? Parsed;
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

        private Dictionary<string, EthosInfo> _editorEthosDefs = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, HeritageInfo> _editorHeritageDefs = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, LanguageInfo> _editorLanguageDefs = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, MartialCustomInfo> _editorMartialCustomDefs = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, HeadDeterminationInfo> _editorHeadDeterminationDefs = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, NamedColor> _editorNamedColors = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, TraditionInfo> _editorTraditionDefs = new(StringComparer.OrdinalIgnoreCase);
        private List<string> _editorTraditionOptions = new();
        private List<string> _editorDlcTraditionOptions = new();
        private Dictionary<string, string> _traditionDlcFlagMap = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, NameListInfo> _editorNameListDefs = new(StringComparer.OrdinalIgnoreCase);

        private Dictionary<string, List<string>> _editorGfxValues = new(StringComparer.OrdinalIgnoreCase);
        private List<string> _editorHouseCoaFrames = new();
        private Dictionary<string, (string Offset, string Scale)> _editorHouseCoaOffsetScale = new(StringComparer.OrdinalIgnoreCase);
        private List<string> _editorEthnicityOptions = new();
        private List<string> _editorCultureOptions = new();
        private Dictionary<string, string> _editorCultureDisplayNames = new(StringComparer.OrdinalIgnoreCase);

        private bool _editorColorTouched;
        private byte _editorColorR = 255;
        private byte _editorColorG = 255;
        private byte _editorColorB = 255;
        private string _editorColorReferenceName = "";

        private bool _editorHasSavedState;
        private string _editorSavedCultureId = "";
        private string _editorSavedEthos = "";
        private string _editorSavedHeritage = "";
        private string _editorSavedLanguage = "";
        private string _editorSavedMartialCustom = "";
        private string _editorSavedHeadDetermination = "";
        private string _editorSavedColor = "";
        private List<string> _editorSavedTraditions = new();
        private List<DlcTradition> _editorSavedDlcTraditions = new();
        private string _editorSavedNameList = "";
        private string _editorSavedNameOrderConvention = "";
        private List<string> _editorSavedBuildingGfx = new();
        private List<string> _editorSavedClothingGfx = new();
        private List<string> _editorSavedUnitGfx = new();
        private List<string> _editorSavedCoaGfx = new();
        private string _editorSavedHouseCoaFrame = "";
        private List<Ethnicity> _editorSavedEthnicities = new();
        private string _editorSavedLocName = "";
        private string _editorSavedLocPrefix = "";
        private string _editorSavedLocCollective = "";
        private string _editorSavedHistoryLocOverride = "";
        private string _editorSavedHistoryLocDescription = "";
        private string _editorSavedCreated = "";
        private List<string> _editorSavedParents = new();

        private HeritageInfo? _editorHeritage;
        private bool _editorHeritageIsNew;
        private bool _heritageHasSavedState;
        private string _savedHeritageLocName = "";
        private string _savedHeritageLocCollective = "";
        private static readonly string[] HeritageAudioFallback = { "byzantine", "european", "indian", "mena", "sea" };

        private readonly HashSet<string> _baseCultureRawKeys = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _baseHeritageRawKeys = new(StringComparer.OrdinalIgnoreCase);
        private static readonly HttpClient _translationHttp = new() { Timeout = TimeSpan.FromSeconds(30) };
        private static readonly System.Text.RegularExpressions.Regex _createdDateRegex =
            new(@"^-?\d+\.\d+\.\d+$", System.Text.RegularExpressions.RegexOptions.Compiled);

        private int GetCreatedOffset() => _viewModel?.CurrentProfile?.YearOffset ?? 0;
        private static Dictionary<string, string>? _editorLocalization;

        private static readonly (string Folder, string Code)[] GameSupportedLanguages =
        {
            ("english", "en"),
            ("french", "fr"),
            ("german", "de"),
            ("japanese", "ja"),
            ("korean", "ko"),
            ("polish", "pl"),
            ("russian", "ru"),
            ("simp_chinese", "zh-CN"),
            ("spanish", "es")
        };

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
            _editorLocalization = localization;

            var modCultures = LoadCulturesFromDirectory(modRoot, culturePath, "Mod");
            var baseCultures = LoadCulturesFromDirectory(gameRoot, culturePath, "Base");

            _baseCultureRawKeys.Clear();
            foreach (var c in baseCultures)
            {
                if (!string.IsNullOrEmpty(c.RawKey))
                    _baseCultureRawKeys.Add(c.RawKey);
            }

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
            var heritageDefinitions = LoadHeritageDefinitions(gameRoot, modRoot, _baseHeritageRawKeys);
            var languageDefinitions = LoadLanguageDefinitions(gameRoot, modRoot);
            var martialCustomDefinitions = LoadMartialCustomDefinitions(gameRoot, modRoot);
            var headDeterminationDefinitions = LoadHeadDeterminationDefinitions(gameRoot, modRoot);
            var nameListDefinitions = LoadNameListDefinitions(gameRoot, modRoot);
            var traditionDefinitions = LoadTraditionDefinitions(gameRoot, modRoot);

            _editorNamedColors = namedColors;
            _editorEthosDefs = ethosDefinitions;
            _editorHeritageDefs = heritageDefinitions;
            _editorLanguageDefs = languageDefinitions;
            _editorMartialCustomDefs = martialCustomDefinitions;
            _editorHeadDeterminationDefs = headDeterminationDefinitions;
            _editorTraditionDefs = traditionDefinitions;
            _editorNameListDefs = nameListDefinitions;
            var dlcFlagMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in allByName.Values)
            {
                foreach (var d in c.DlcTraditions ?? new List<DlcTradition>())
                {
                    if (!string.IsNullOrEmpty(d.Trait) && !string.IsNullOrEmpty(d.RequiresDlcFlag))
                        dlcFlagMap[d.Trait] = d.RequiresDlcFlag;
                }
            }
            _traditionDlcFlagMap = dlcFlagMap;
            BuildTraditionOptions();

            foreach (var tradition in traditionDefinitions.Values)
            {
                if (localization.TryGetValue($"{tradition.Name}_name", out var tName))
                    tradition.DisplayName = tName;
                if (localization.TryGetValue($"{tradition.Name}_desc", out var tDesc))
                    tradition.Description = tDesc;
            }

            _editorGfxValues = BuildGfxValues(allByName.Values);
            _editorEthnicityOptions = BuildEthnicityOptions(allByName.Values);
            _editorCultureOptions = BuildCultureOptions(allByName.Values);
            BuildCultureDisplayNames(allByName, localization);
            LoadHouseCoaMapping(baseCultures, modCultures);

            if (_editorCulture == null && EditorEthos != null)
                ResetEditorForNewCulture();

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

            RefreshHeritageList();
            RefreshHeritageAudioOptions();

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
                var lookupKey = heritageKey;
                if (_editorHeritageDefs != null)
                {
                    if (!_editorHeritageDefs.ContainsKey(lookupKey) && _editorHeritageDefs.ContainsKey("heritage_" + lookupKey))
                        lookupKey = "heritage_" + lookupKey;
                }
                if (!groups.TryGetValue(lookupKey, out var group))
                {
                    group = new CultureGroup { Name = lookupKey };
                    groups[lookupKey] = group;
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
                var heritageKey = string.IsNullOrEmpty(culture.Heritage) ? "unknown" : culture.Heritage;
                var lookupKey = heritageKey;
                if (_editorHeritageDefs != null)
                {
                    if (!_editorHeritageDefs.ContainsKey(lookupKey) && _editorHeritageDefs.ContainsKey("heritage_" + lookupKey))
                        lookupKey = "heritage_" + lookupKey;
                }
                if (groups.TryGetValue(lookupKey, out var group))
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
            if (CtxDeleteMenuItem != null)
                CtxDeleteMenuItem.Visibility = IsCultureDeletable(culture)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private static bool IsCultureDeletable(CultureInfo culture)
            => culture.Source == "Mod" && culture.IsModNew && !string.IsNullOrEmpty(culture.SourceFile);

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

        private void CtxDeleteCulture_Click(object sender, RoutedEventArgs e)
        {
            if (CultureTree.SelectedItem is not CultureInfo culture) return;
            if (!IsCultureDeletable(culture))
            {
                EditorStatusText.Text = Res("CulturesTab_DeleteNotAllowed");
                return;
            }

            string cultureId = culture.RawKey ?? culture.Name ?? "";
            string displayName = culture.DisplayName ?? culture.Name ?? cultureId;

            var confirm = System.Windows.MessageBox.Show(
                string.Format(Res("CulturesTab_DeleteConfirm"), displayName),
                Res("CulturesTab_DeleteConfirmTitle"),
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            if (confirm != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                string filePath = culture.SourceFile ?? "";
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    EditorStatusText.Text = Res("CulturesTab_DeleteFileNotFound");
                    return;
                }

                if (!DeleteCultureBlockFromFile(filePath, cultureId))
                {
                    EditorStatusText.Text = Res("CulturesTab_DeleteBlockNotFound");
                    return;
                }

                bool fileDeleted = false;
                if (CountCultureBlocks(filePath) == 0)
                {
                    File.Delete(filePath);
                    fileDeleted = true;
                }

                var profile = _viewModel?.CurrentProfile;
                if (profile != null && !string.IsNullOrEmpty(profile.ModRoot))
                {
                    bool existsInBase = _baseCultureRawKeys.Contains(cultureId);
                    DeleteCultureLocalization(profile.ModRoot, cultureId, existsInBase);
                    if (!string.IsNullOrEmpty(culture.HistoryLocOverride))
                        DeleteCultureHistoryLocalization(profile.ModRoot, culture.HistoryLocOverride, existsInBase);
                }

                if (_editorCulture != null &&
                    string.Equals(_editorCulture.RawKey ?? "", cultureId, StringComparison.OrdinalIgnoreCase))
                {
                    ResetEditorForNewCulture();
                    _editorCulture = null;
                    _editorFileNameManual = false;
                    if (EditorCultureId != null) EditorCultureId.Text = "";
                    _editorHasSavedState = false;
                    UpdateEditorModeUi();
                }

                RefreshCultureTree();

                string removedNote = fileDeleted ? $" {Res("CulturesTab_DeleteFileRemoved")}" : "";
                EditorStatusText.Text = string.Format(Res("CulturesTab_DeleteSuccess"), displayName) + removedNote;
            }
            catch (Exception ex)
            {
                EditorStatusText.Text = $"{Res("CulturesTab_DeleteError")}: {ex.Message}";
            }
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
            PopulateEditorCombo(EditorEthos, GetEthosOptions(), culture.Ethos ?? "");
            PopulateEditorCombo(EditorHeritage, GetHeritageOptions(), culture.Heritage ?? "");
            PopulateEditorCombo(EditorLanguage, GetLanguageOptions(), culture.Language ?? "");
            PopulateEditorCombo(EditorMartialCustom, GetMartialCustomOptions(), culture.MartialCustom ?? "");
            PopulateEditorCombo(EditorHeadDetermination, GetHeadDeterminationOptions(), culture.HeadDetermination ?? "");
            PopulateTraditionLists(culture.TraditionKeys ?? new List<string>());
            PopulateEditorCombo(EditorNameList, GetNameListOptions(), culture.NameList ?? "");
            PopulateNameOrderConvention(culture.NameOrderConvention ?? "");
            PopulateGfxLists("coa", culture.CoaGfx ?? new List<string>());
            PopulateGfxLists("building", culture.BuildingGfx ?? new List<string>());
            PopulateGfxLists("clothing", culture.ClothingGfx ?? new List<string>());
            PopulateGfxLists("unit", culture.UnitGfx ?? new List<string>());
            PopulateHouseCoaFrame(culture.HouseCoaFrame ?? "");
            PopulateEthnicityRows(culture.Ethnicities ?? new List<Ethnicity>());
            PopulateDlcTraditionRows(culture.DlcTraditions ?? new List<DlcTradition>());
            PopulateEditorLocalizationFields(culture);

            _editorColorReferenceName = culture.ColorReference ?? "";
            if (culture.HasColor)
            {
                _editorColorR = culture.R;
                _editorColorG = culture.G;
                _editorColorB = culture.B;
                _editorColorTouched = false;
            }
            else if (!string.IsNullOrEmpty(_editorColorReferenceName)
                     && _editorNamedColors.TryGetValue(_editorColorReferenceName, out var named)
                     && named.HasColor)
            {
                _editorColorR = named.R;
                _editorColorG = named.G;
                _editorColorB = named.B;
                _editorColorTouched = false;
            }
            else
            {
                _editorColorR = 255;
                _editorColorG = 255;
                _editorColorB = 255;
                _editorColorTouched = !string.IsNullOrEmpty(_editorColorReferenceName);
            }
            if (EditorColorPreview != null)
                EditorColorPreview.Background = new SolidColorBrush(GetEditorArgb(255, _editorColorR, _editorColorG, _editorColorB));

            _editorCulture = culture;
            _editorIsNew = copyAsNew;
            _editorFileNameManual = false;

            CaptureEditorSavedState();
            UpdateEditorDirtyState();

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
            ResetEditorForNewCulture();
            _editorCulture = null;
            _editorFileNameManual = false;
            EditorCultureId.Text = "";
            _editorHasSavedState = false;
            UpdateEditorModeUi();
        }

        private void EditorCultureId_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!_editorFileNameManual)
                UpdateDefaultEditorFileName();
            UpdateEditorDirtyState();
        }

        private void EditorField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReferenceEquals(sender, EditorHouseCoaFrame))
                UpdateHouseCoaMaskText();
            if (ReferenceEquals(sender, EditorNameOrderConvention))
                UpdateNameOrderConventionCustomVisibility();
            UpdateEditorDirtyState();
        }

        private void EditorLoc_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateEditorDirtyState();
        }

        private void EditorCreated_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateCreatedPreview();
            UpdateEditorDirtyState();
        }

        private void EditorColor_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.ColorDialog();
            dialog.FullOpen = true;
            dialog.Color = System.Drawing.Color.FromArgb(_editorColorR, _editorColorG, _editorColorB);
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _editorColorR = dialog.Color.R;
                _editorColorG = dialog.Color.G;
                _editorColorB = dialog.Color.B;
                _editorColorTouched = true;
                _editorColorReferenceName = "";
                if (EditorColorPreview != null)
                    EditorColorPreview.Background = new SolidColorBrush(GetEditorArgb(255, _editorColorR, _editorColorG, _editorColorB));
                UpdateEditorDirtyState();
            }
        }

        private IEnumerable<(string Key, string Display)> GetEthosOptions()
        {
            foreach (var def in _editorEthosDefs.Values.OrderBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase))
                yield return (def.Name, def.DisplayName);
        }

        private IEnumerable<(string Key, string Display)> GetHeritageOptions()
        {
            foreach (var def in _editorHeritageDefs.Values.OrderBy(d => d.DisplayName, StringComparer.CurrentCultureIgnoreCase))
                yield return (def.Name, def.DisplayName);
        }

        private IEnumerable<(string Key, string Display)> GetLanguageOptions()
        {
            foreach (var def in _editorLanguageDefs.Values.OrderBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase))
                yield return (def.Name, def.DisplayName);
        }

        private IEnumerable<(string Key, string Display)> GetMartialCustomOptions()
        {
            foreach (var def in _editorMartialCustomDefs.Values.OrderBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase))
                yield return (def.Name, def.DisplayName);
        }

        private IEnumerable<(string Key, string Display)> GetHeadDeterminationOptions()
        {
            foreach (var def in _editorHeadDeterminationDefs.Values.OrderBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase))
                yield return (def.Name, def.DisplayName);
        }

        private IEnumerable<(string Key, string Display)> GetNameListOptions()
        {
            foreach (var def in _editorNameListDefs.Values.OrderBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase))
                yield return (def.Name, def.DisplayName);
        }

        private IEnumerable<(string Key, string Display)> GetNameOrderConventionOptions()
        {
            string[] presets = { "default", "dynasty_always_first", "dynasty_first", "japanese" };
            foreach (var preset in presets)
                yield return (preset, GetNameOrderConventionDisplay(preset));
            yield return ("custom", Res("CulturesTab_EditorNameOrderConventionCustom"));
        }

        private string GetNameOrderConventionDisplay(string value)
        {
            if (!string.IsNullOrEmpty(value)
                && _editorLocalization != null
                && _editorLocalization.TryGetValue($"culture_aesthetics_naming_{value}", out var display)
                && !string.IsNullOrWhiteSpace(display))
                return display;
            return value;
        }

        private static void PopulateEditorCombo(System.Windows.Controls.ComboBox combo, IEnumerable<(string Key, string Display)> options, string currentValue)
        {
            if (combo == null) return;
            combo.Items.Clear();
            combo.Items.Add(new ComboBoxItem { Tag = "", Content = Res("CulturesTab_EditorNone") });
            foreach (var (key, display) in options)
                combo.Items.Add(new ComboBoxItem { Tag = key, Content = display });

            string normalized = (currentValue ?? "").Trim();
            foreach (ComboBoxItem item in combo.Items)
            {
                if (string.Equals((item.Tag as string) ?? "", normalized, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
            combo.SelectedIndex = 0;
        }

        private static string GetSelectedOption(System.Windows.Controls.ComboBox combo)
        {
            if (combo?.SelectedItem is ComboBoxItem item)
                return (item.Tag as string) ?? "";
            return "";
        }

        private void PopulateNameOrderConvention(string currentValue)
        {
            if (EditorNameOrderConvention == null) return;
            var options = GetNameOrderConventionOptions().ToList();
            EditorNameOrderConvention.Items.Clear();
            EditorNameOrderConvention.Items.Add(new ComboBoxItem { Tag = "", Content = Res("CulturesTab_EditorNone") });
            foreach (var (key, display) in options)
                EditorNameOrderConvention.Items.Add(new ComboBoxItem { Tag = key, Content = display });
            if (EditorNameOrderConventionCustom != null)
                EditorNameOrderConventionCustom.Text = "";

            string normalized = (currentValue ?? "").Trim();
            if (!string.IsNullOrEmpty(normalized))
            {
                var preset = options.FirstOrDefault(o => string.Equals(o.Key, normalized, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(preset.Key))
                {
                    foreach (ComboBoxItem item in EditorNameOrderConvention.Items)
                        if (string.Equals((item.Tag as string) ?? "", preset.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            EditorNameOrderConvention.SelectedItem = item;
                            UpdateNameOrderConventionCustomVisibility();
                            return;
                        }
                }
                else
                {
                    foreach (ComboBoxItem item in EditorNameOrderConvention.Items)
                        if (string.Equals((item.Tag as string) ?? "", "custom", StringComparison.OrdinalIgnoreCase))
                        {
                            EditorNameOrderConvention.SelectedItem = item;
                            break;
                        }
                    if (EditorNameOrderConventionCustom != null)
                        EditorNameOrderConventionCustom.Text = normalized;
                    UpdateNameOrderConventionCustomVisibility();
                    return;
                }
            }
            EditorNameOrderConvention.SelectedIndex = 0;
            UpdateNameOrderConventionCustomVisibility();
        }

        private string GetEditorNameOrderConvention()
        {
            if (EditorNameOrderConvention == null) return "";
            string selected = GetSelectedOption(EditorNameOrderConvention);
            if (string.Equals(selected, "custom", StringComparison.OrdinalIgnoreCase))
                return EditorNameOrderConventionCustom?.Text?.Trim() ?? "";
            return selected;
        }

        private void UpdateNameOrderConventionCustomVisibility()
        {
            if (EditorNameOrderConventionCustom == null) return;
            EditorNameOrderConventionCustom.Visibility =
                string.Equals(GetSelectedOption(EditorNameOrderConvention), "custom", StringComparison.OrdinalIgnoreCase)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void EditorNameOrderConventionCustom_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateEditorDirtyState();
        }

        private List<string> GetSelectedTraditions()
        {
            var result = new List<string>();
            if (EditorTraditionsSelected == null) return result;
            foreach (object obj in EditorTraditionsSelected.Items)
                if (obj is System.Windows.Controls.ListBoxItem it && (it.Tag as string) is string k && !string.IsNullOrEmpty(k))
                    result.Add(k);
            return result;
        }

        private void PopulateTraditionLists(IEnumerable<string>? selectedKeys = null)
        {
            if (EditorTraditionsAvailable == null || EditorTraditionsSelected == null) return;
            var selected = selectedKeys != null
                ? new List<string>(selectedKeys)
                : GetSelectedTraditions();

            EditorTraditionsAvailable.Items.Clear();
            EditorTraditionsSelected.Items.Clear();

foreach (var def in _editorTraditionDefs.Values
                     .Where(d => !IsDlcTradition(d) && !selected.Contains(d.Name))
                     .OrderBy(d => d.DisplayName, StringComparer.CurrentCultureIgnoreCase))
                EditorTraditionsAvailable.Items.Add(CreateTraditionListItem(def.Name, false));

            foreach (var key in selected)
                EditorTraditionsSelected.Items.Add(CreateTraditionListItem(key, true));
        }

        private System.Windows.Controls.ListBoxItem CreateTraditionListItem(string key, bool isSelected)
        {
            string display = key;
            string? description = null;
            if (_editorTraditionDefs.TryGetValue(key, out var def))
            {
                display = def.DisplayName;
                description = string.IsNullOrEmpty(def.Description) ? null : def.Description;
            }

            var row = new System.Windows.Controls.StackPanel();
            var header = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal
            };
            var btn = new System.Windows.Controls.Button
            {
                Content = isSelected ? "−" : "+",
                Tag = key,
                Width = 26,
                Height = 22,
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold
            };
            btn.Click += isSelected ? EditorTraditionsRemove : EditorTraditionsAdd;
            header.Children.Add(btn);
            header.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = display,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            });
            row.Children.Add(header);

            if (!string.IsNullOrEmpty(description))
            {
                row.Children.Add(new System.Windows.Controls.TextBlock
                {
                    Text = description,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(32, 2, 4, 0)
                });
            }

            return new System.Windows.Controls.ListBoxItem { Tag = key, Content = row };
        }

        private static System.Windows.Controls.ListBoxItem? FindParentListBoxItem(DependencyObject? child)
        {
            while (child != null)
            {
                if (child is System.Windows.Controls.ListBoxItem item)
                    return item;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private void EditorTraditionsAdd(object sender, RoutedEventArgs e)
        {
            if (FindParentListBoxItem(sender as DependencyObject) is not System.Windows.Controls.ListBoxItem item) return;
            if (item.Tag is not string key) return;
            var selected = GetSelectedTraditions();
            if (selected.Contains(key)) return;
            selected.Add(key);
            PopulateTraditionLists(selected);
            UpdateEditorDirtyState();
        }

        private void EditorTraditionsRemove(object sender, RoutedEventArgs e)
        {
            if (FindParentListBoxItem(sender as DependencyObject) is not System.Windows.Controls.ListBoxItem item) return;
            if (item.Tag is not string key) return;
            var selected = GetSelectedTraditions();
            selected.Remove(key);
            PopulateTraditionLists(selected);
            UpdateEditorDirtyState();
        }

        private List<string> GetSelectedParents()
        {
            var result = new List<string>();
            if (EditorParentsSelected == null) return result;
            foreach (object obj in EditorParentsSelected.Items)
                if (obj is System.Windows.Controls.ListBoxItem it && (it.Tag as string) is string k && !string.IsNullOrEmpty(k))
                    result.Add(k);
            return result;
        }

        private void PopulateParentLists(IEnumerable<string>? selectedKeys = null)
        {
            if (EditorParentsAvailable == null || EditorParentsSelected == null) return;
            var selected = selectedKeys != null
                ? new List<string>(selectedKeys)
                : GetSelectedParents();

            EditorParentsAvailable.Items.Clear();
            EditorParentsSelected.Items.Clear();

foreach (var key in _editorCultureOptions
                     .Where(k => !selected.Contains(k))
                     .OrderBy(k => GetCultureDisplayName(k), StringComparer.CurrentCultureIgnoreCase))
                EditorParentsAvailable.Items.Add(CreateParentListItem(key, false));

            foreach (var key in selected)
                EditorParentsSelected.Items.Add(CreateParentListItem(key, true));
        }

        private System.Windows.Controls.ListBoxItem CreateParentListItem(string key, bool isSelected)
        {
            var row = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal
            };
            var btn = new System.Windows.Controls.Button
            {
                Content = isSelected ? "−" : "+",
                Tag = key,
                Width = 26,
                Height = 22,
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold
            };
            btn.Click += isSelected ? EditorParentsRemove : EditorParentsAdd;
            row.Children.Add(btn);
            row.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = GetCultureDisplayName(key),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            });
            return new System.Windows.Controls.ListBoxItem { Tag = key, Content = row };
        }

        private void EditorParentsAdd(object sender, RoutedEventArgs e)
        {
            if (FindParentListBoxItem(sender as DependencyObject) is not System.Windows.Controls.ListBoxItem item) return;
            if (item.Tag is not string key) return;
            var selected = GetSelectedParents();
            if (selected.Contains(key)) return;
            selected.Add(key);
            PopulateParentLists(selected);
            UpdateEditorDirtyState();
        }

        private void EditorParentsRemove(object sender, RoutedEventArgs e)
        {
            if (FindParentListBoxItem(sender as DependencyObject) is not System.Windows.Controls.ListBoxItem item) return;
            if (item.Tag is not string key) return;
            var selected = GetSelectedParents();
            selected.Remove(key);
            PopulateParentLists(selected);
            UpdateEditorDirtyState();
        }

        private (System.Windows.Controls.ListBox? selected, System.Windows.Controls.ListBox? available) GetGfxLists(string category)
        {
            return category switch
            {
                "coa" => (EditorCoaGfxSelected, EditorCoaGfxAvailable),
                "building" => (EditorBuildingGfxSelected, EditorBuildingGfxAvailable),
                "clothing" => (EditorClothingGfxSelected, EditorClothingGfxAvailable),
                "unit" => (EditorUnitGfxSelected, EditorUnitGfxAvailable),
                _ => (null, null)
            };
        }

        private List<string> GetSelectedGfx(string category)
        {
            var result = new List<string>();
            var (selected, _) = GetGfxLists(category);
            if (selected == null) return result;
            foreach (object obj in selected.Items)
                if (obj is System.Windows.Controls.ListBoxItem it && (it.Tag as string) is string key && !string.IsNullOrEmpty(key))
                    result.Add(key);
            return result;
        }

        private void PopulateGfxLists(string category, IEnumerable<string> selectedKeys)
        {
            var (selected, available) = GetGfxLists(category);
            if (selected == null || available == null) return;
            var selectedList = new List<string>(selectedKeys);

            available.Items.Clear();
            selected.Items.Clear();
            available.Tag = category;
            selected.Tag = category;

            if (_editorGfxValues.TryGetValue(category, out var allValues))
            {
                foreach (var value in allValues.Where(v => !selectedList.Contains(v)))
                    available.Items.Add(CreateGfxListItem(value, category, false));
            }

            foreach (var value in selectedList)
                selected.Items.Add(CreateGfxListItem(value, category, true));
        }

        private System.Windows.Controls.ListBoxItem CreateGfxListItem(string value, string category, bool isSelected)
        {
            var row = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal
            };

            if (isSelected)
            {
                var removeBtn = MakeSquareButton("−", value, EditorGfxRemove);
                var upBtn = MakeSquareButton("↑", value, EditorGfxMoveUp);
                var downBtn = MakeSquareButton("↓", value, EditorGfxMoveDown);
                row.Children.Add(removeBtn);
                row.Children.Add(upBtn);
                row.Children.Add(downBtn);
            }
            else
            {
                var addBtn = MakeSquareButton("+", value, EditorGfxAdd);
                row.Children.Add(addBtn);
            }

            row.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = value,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            });

            return new System.Windows.Controls.ListBoxItem { Tag = value, Content = row };
        }

        private static System.Windows.Controls.Button MakeSquareButton(string content, string tag, RoutedEventHandler handler)
        {
            var btn = new System.Windows.Controls.Button
            {
                Content = content,
                Tag = tag,
                Width = 24,
                Height = 22,
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 3, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold,
                FontSize = 12
            };
            btn.Click += handler;
            return btn;
        }

        private void EditorGfxAdd(object sender, RoutedEventArgs e)
        {
            if (FindParentListBoxItem(sender as DependencyObject) is not System.Windows.Controls.ListBoxItem item) return;
            if (item.Tag is not string value) return;
            var category = GetGfxCategory(item);
            if (string.IsNullOrEmpty(category)) return;
            var selected = GetSelectedGfx(category);
            if (selected.Contains(value)) return;
            selected.Add(value);
            PopulateGfxLists(category, selected);
            UpdateEditorDirtyState();
        }

        private void EditorGfxRemove(object sender, RoutedEventArgs e)
        {
            if (FindParentListBoxItem(sender as DependencyObject) is not System.Windows.Controls.ListBoxItem item) return;
            if (item.Tag is not string value) return;
            var category = GetGfxCategory(item);
            if (string.IsNullOrEmpty(category)) return;
            var selected = GetSelectedGfx(category);
            selected.Remove(value);
            PopulateGfxLists(category, selected);
            UpdateEditorDirtyState();
        }

        private void EditorGfxMoveUp(object sender, RoutedEventArgs e)
        {
            if (FindParentListBoxItem(sender as DependencyObject) is not System.Windows.Controls.ListBoxItem item) return;
            if (item.Tag is not string value) return;
            var category = GetGfxCategory(item);
            if (string.IsNullOrEmpty(category)) return;
            var selected = GetSelectedGfx(category);
            int index = selected.IndexOf(value);
            if (index <= 0) return;
            (selected[index - 1], selected[index]) = (selected[index], selected[index - 1]);
            PopulateGfxLists(category, selected);
            UpdateEditorDirtyState();
        }

        private void EditorGfxMoveDown(object sender, RoutedEventArgs e)
        {
            if (FindParentListBoxItem(sender as DependencyObject) is not System.Windows.Controls.ListBoxItem item) return;
            if (item.Tag is not string value) return;
            var category = GetGfxCategory(item);
            if (string.IsNullOrEmpty(category)) return;
            var selected = GetSelectedGfx(category);
            int index = selected.IndexOf(value);
            if (index < 0 || index >= selected.Count - 1) return;
            (selected[index], selected[index + 1]) = (selected[index + 1], selected[index]);
            PopulateGfxLists(category, selected);
            UpdateEditorDirtyState();
        }

        private static string GetGfxCategory(System.Windows.Controls.ListBoxItem item)
        {
            var container = System.Windows.Controls.ItemsControl.ItemsControlFromItemContainer(item) as System.Windows.Controls.ListBox;
            return container?.Tag as string ?? "";
        }

        private void PopulateHouseCoaFrame(string currentFrame)
        {
            if (EditorHouseCoaFrame == null) return;
            EditorHouseCoaFrame.Items.Clear();
            EditorHouseCoaFrame.Items.Add(new ComboBoxItem { Tag = "", Content = Res("CulturesTab_EditorNone") });
            foreach (var frame in _editorHouseCoaFrames)
                EditorHouseCoaFrame.Items.Add(new ComboBoxItem { Tag = frame, Content = frame });

            string normalized = (currentFrame ?? "").Trim();
            foreach (ComboBoxItem item in EditorHouseCoaFrame.Items)
            {
                if (string.Equals((item.Tag as string) ?? "", normalized, StringComparison.OrdinalIgnoreCase))
                {
                    EditorHouseCoaFrame.SelectedItem = item;
                    return;
                }
            }
            EditorHouseCoaFrame.SelectedIndex = 0;
            UpdateHouseCoaMaskText();
        }

        private void UpdateHouseCoaMaskText()
        {
            if (EditorHouseCoaMaskText == null) return;
            string frame = GetSelectedOption(EditorHouseCoaFrame);
            if (!string.IsNullOrEmpty(frame) && _editorHouseCoaOffsetScale.TryGetValue(frame, out var map))
                EditorHouseCoaMaskText.Text = $"offset: {{ {map.Offset} }}   scale: {{ {map.Scale} }}";
            else
                EditorHouseCoaMaskText.Text = "";
        }

        private void PopulateEthnicityRows(IEnumerable<Ethnicity> entries)
        {
            if (EditorEthnicitiesRows == null) return;
            EditorEthnicitiesRows.Items.Clear();
            foreach (var entry in entries ?? new List<Ethnicity>())
                AddEthnicityRow(entry);
        }

        private void AddEthnicityRow(Ethnicity? entry)
        {
            if (EditorEthnicitiesRows == null) return;
            var row = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 3)
            };
            var percentBox = new System.Windows.Controls.TextBox
            {
                Width = 70,
                Text = entry == null ? "" : FormatPercent(entry.Weight),
                Tag = "percent"
            };
            percentBox.TextChanged += EditorEthnicityPercent_TextChanged;
            row.Children.Add(percentBox);
            row.Children.Add(new System.Windows.Controls.TextBlock { Text = "  ", VerticalAlignment = VerticalAlignment.Center });
            var nameCombo = new System.Windows.Controls.ComboBox
            {
                Width = 190,
                IsEditable = false,
                ItemsSource = _editorEthnicityOptions
            };
            if (entry != null)
            {
                foreach (var item in nameCombo.Items)
                    if (string.Equals(item as string, entry.Name, StringComparison.OrdinalIgnoreCase))
                    { nameCombo.SelectedItem = item; break; }
                if (nameCombo.SelectedIndex < 0)
                {
                    if (!_editorEthnicityOptions.Contains(entry.Name))
                        _editorEthnicityOptions.Add(entry.Name);
                    nameCombo.SelectedItem = entry.Name;
                }
            }
            nameCombo.SelectionChanged += EditorEthnicityPercent_SelectionChanged;
            row.Children.Add(nameCombo);
            row.Children.Add(CreateEthnicityRemoveButton(row));
            EditorEthnicitiesRows.Items.Add(row);
        }

        private System.Windows.Controls.Button CreateEthnicityRemoveButton(System.Windows.Controls.StackPanel row)
        {
            var button = new System.Windows.Controls.Button
            {
                Content = "−",
                Width = 22,
                Height = 22,
                Margin = new Thickness(4, 0, 0, 0),
                Padding = new Thickness(0),
                Tag = row
            };
            button.Click += (_, _) =>
            {
                (row.Parent as System.Windows.Controls.ItemsControl)?.Items.Remove(row);
                UpdateEditorDirtyState();
            };
            return button;
        }

        private List<Ethnicity> GetEthnicityEntries()
        {
            var result = new List<Ethnicity>();
            if (EditorEthnicitiesRows == null) return result;
            foreach (object obj in EditorEthnicitiesRows.Items)
            {
                if (obj is not System.Windows.Controls.StackPanel row) continue;
                var percentBox = row.Children.OfType<System.Windows.Controls.TextBox>().FirstOrDefault();
                var nameCombo = row.Children.OfType<System.Windows.Controls.ComboBox>().FirstOrDefault();
                if (percentBox == null || nameCombo == null) continue;
                string percent = percentBox.Text?.Trim() ?? "";
                string name = (nameCombo.SelectedItem as string)?.Trim() ?? "";
                if (string.IsNullOrEmpty(percent) || string.IsNullOrEmpty(name)) continue;
                if (!double.TryParse(percent, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double weight))
                    continue;
                result.Add(new Ethnicity { Weight = weight, Name = name });
            }
            return result;
        }

        private void EditorEthnicityPercent_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateEditorDirtyState();
        }

        private void EditorEthnicityPercent_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateEditorDirtyState();
        }

        private void EditorEthnicityAdd_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AddEthnicityRow(null);
            UpdateEditorDirtyState();
        }

        private static string FormatPercent(double weight)
            => weight.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        private static bool EthnicityListsEqual(List<Ethnicity> a, List<Ethnicity> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!string.Equals(a[i].Name, b[i].Name, StringComparison.OrdinalIgnoreCase)) return false;
                if (Math.Abs(a[i].Weight - b[i].Weight) > 0.000001) return false;
            }
            return true;
        }

        private void PopulateDlcTraditionRows(IEnumerable<DlcTradition> entries)
        {
            if (EditorDlcTraditionsRows == null) return;
            EditorDlcTraditionsRows.Items.Clear();
            foreach (var entry in entries ?? new List<DlcTradition>())
                AddDlcTraditionRow(entry);
        }

        private void AddDlcTraditionRow(DlcTradition? entry)
        {
            if (EditorDlcTraditionsRows == null) return;
            var row = new System.Windows.Controls.StackPanel
            {
                Margin = new Thickness(0, 0, 0, 6)
            };

            var traitCombo = CreateEditableTraditionCombo(entry?.Trait ?? "", isDlc: true);
            var fallbackCombo = CreateEditableTraditionCombo(entry?.Fallback ?? "", isDlc: false);
            fallbackCombo.Width = 300;

            row.Tag = new DlcTraditionRowUi
            {
                Trait = traitCombo,
                Fallback = fallbackCombo,
                Parsed = entry
            };

            traitCombo.SelectionChanged += EditorDlcTradition_SelectionChanged;
            traitCombo.AddHandler(System.Windows.Controls.TextBox.TextChangedEvent,
                new System.Windows.Controls.TextChangedEventHandler(EditorDlcTradition_TextChanged));

            var line1 = new Grid();
            line1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            line1.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            traitCombo.SetValue(Grid.ColumnProperty, 0);
            line1.Children.Add(traitCombo);
            var removeButton = CreateDlcTraditionRemoveButton(row);
            removeButton.Margin = new Thickness(8, 0, 0, 0);
            removeButton.SetValue(Grid.ColumnProperty, 1);
            line1.Children.Add(removeButton);
            row.Children.Add(line1);

            var line2 = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 3, 0, 0)
            };
            line2.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = Res("CulturesTab_EditorDlcTraditionFallback") + ":",
                Foreground = System.Windows.Media.Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            });
            fallbackCombo.SelectionChanged += EditorDlcTradition_SelectionChanged;
            fallbackCombo.AddHandler(System.Windows.Controls.TextBox.TextChangedEvent,
                new System.Windows.Controls.TextChangedEventHandler(EditorDlcTradition_TextChanged));
            line2.Children.Add(fallbackCombo);
            row.Children.Add(line2);

            EditorDlcTraditionsRows.Items.Add(row);
        }

        private System.Windows.Controls.ComboBox CreateEditableTraditionCombo(string value, bool isDlc)
        {
            var options = isDlc ? _editorDlcTraditionOptions : _editorTraditionOptions;
            var combo = new System.Windows.Controls.ComboBox
            {
                IsEditable = true,
                IsTextSearchEnabled = false
            };
            foreach (var key in options)
            {
                var item = new System.Windows.Controls.ComboBoxItem { Tag = key };
                string desc = _editorTraditionDefs.TryGetValue(key, out var def) ? (def.Description ?? "") : "";
                if (isDlc)
                {
                    var content = new Grid();
                    content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    var nameText = new System.Windows.Controls.TextBlock
                    {
                        Text = GetTraditionDisplayName(key) + GetDlcFlagSuffix(key),
                        MaxWidth = 210,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    nameText.SetValue(Grid.ColumnProperty, 0);
                    content.Children.Add(nameText);
                    if (!string.IsNullOrEmpty(desc))
                    {
                        var descText = new System.Windows.Controls.TextBlock
                        {
                            Text = desc,
                            Foreground = System.Windows.Media.Brushes.Gray,
                            FontSize = 11,
                            TextWrapping = TextWrapping.Wrap,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(8, 0, 0, 0)
                        };
                        descText.SetValue(Grid.ColumnProperty, 1);
                        content.Children.Add(descText);
                    }
                    item.Content = content;
                }
                else
                {
                    var content = new System.Windows.Controls.StackPanel();
                    content.Children.Add(new System.Windows.Controls.TextBlock { Text = GetTraditionDisplayName(key) });
                    if (!string.IsNullOrEmpty(desc))
                    {
                        content.Children.Add(new System.Windows.Controls.TextBlock
                        {
                            Text = desc,
                            Foreground = System.Windows.Media.Brushes.Gray,
                            FontSize = 11,
                            TextWrapping = TextWrapping.Wrap,
                            MaxWidth = 280
                        });
                    }
                    item.Content = content;
                }
                combo.Items.Add(item);
                if (string.Equals(key, value, StringComparison.OrdinalIgnoreCase))
                    combo.SelectedItem = item;
            }
            if (combo.SelectedItem == null && !string.IsNullOrEmpty(value))
                combo.Text = GetTraditionDisplayName(value);
            return combo;
        }

        private string GetDlcFlagSuffix(string key)
        {
            if (string.IsNullOrEmpty(key)
                || !_traditionDlcFlagMap.TryGetValue(key, out var flag)
                || string.IsNullOrEmpty(flag))
                return "";
            return $" (DLC {flag})";
        }

        private string GetTraditionDisplayName(string key)
        {
            if (_editorTraditionDefs.TryGetValue(key, out var def))
                return def.DisplayName;
            return key;
        }

        private System.Windows.Controls.Button CreateDlcTraditionRemoveButton(System.Windows.Controls.StackPanel row)
        {
            var button = new System.Windows.Controls.Button
            {
                Content = "−",
                Width = 22,
                Height = 22,
                Margin = new Thickness(4, 0, 0, 0),
                Padding = new Thickness(0),
                Tag = row
            };
            button.Click += (_, _) =>
            {
                (row.Parent as System.Windows.Controls.ItemsControl)?.Items.Remove(row);
                UpdateEditorDirtyState();
            };
            return button;
        }

        private List<DlcTradition> GetDlcTraditions()
        {
            var result = new List<DlcTradition>();
            if (EditorDlcTraditionsRows == null) return result;
            foreach (object obj in EditorDlcTraditionsRows.Items)
            {
                if (obj is not System.Windows.Controls.StackPanel row) continue;
                if (row.Tag is not DlcTraditionRowUi ui) continue;
                string trait = ReadDlcComboValue(ui.Trait);
                string fallback = ReadDlcComboValue(ui.Fallback);
                if (string.IsNullOrEmpty(trait)) continue;
                string flag = GetDlcFlagForTrait(trait, ui.Parsed?.RequiresDlcFlag);
                result.Add(new DlcTradition { Trait = trait, RequiresDlcFlag = flag, Fallback = fallback });
            }
            return result;
        }

        private string GetDlcFlagForTrait(string trait, string? parsedFallback)
        {
            if (!string.IsNullOrEmpty(trait)
                && _traditionDlcFlagMap.TryGetValue(trait, out var flag)
                && !string.IsNullOrEmpty(flag))
                return flag;
            return parsedFallback ?? "";
        }

        private static string ReadDlcComboValue(System.Windows.Controls.ComboBox combo)
        {
            if (combo.SelectedItem is System.Windows.Controls.ComboBoxItem item && item.Tag is string tag)
                return tag;
            return combo.Text?.Trim() ?? "";
        }

        private static bool DlcTraditionsEqual(List<DlcTradition> a, List<DlcTradition> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!string.Equals(a[i].Trait, b[i].Trait, StringComparison.OrdinalIgnoreCase)) return false;
                if (!string.Equals(a[i].RequiresDlcFlag, b[i].RequiresDlcFlag, StringComparison.OrdinalIgnoreCase)) return false;
                if (!string.Equals(a[i].Fallback, b[i].Fallback, StringComparison.OrdinalIgnoreCase)) return false;
            }
            return true;
        }

        private string BuildDlcTraditionDetailText(DlcTradition d)
        {
            string s = GetTraditionDisplayName(d.Trait);
            if (!string.IsNullOrEmpty(d.RequiresDlcFlag))
                s += $" ({d.RequiresDlcFlag})";
            if (!string.IsNullOrEmpty(d.Fallback))
                s += $" → {GetTraditionDisplayName(d.Fallback)}";
            return s;
        }

        private void EditorDlcTraditionAdd_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AddDlcTraditionRow(null);
            UpdateEditorDirtyState();
        }

        private void EditorDlcTradition_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateEditorDirtyState();
        }

        private void EditorDlcTradition_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateEditorDirtyState();
        }

        private string GetEditorColorString()
        {
            if (_editorColorTouched || string.IsNullOrEmpty(_editorColorReferenceName))
                return $"rgb {_editorColorR} {_editorColorG} {_editorColorB}";
            return _editorColorReferenceName;
        }

        private static System.Windows.Media.Color GetEditorArgb(byte a, byte r, byte g, byte b)
            => System.Windows.Media.Color.FromArgb(a, r, g, b);

        private void ResetEditorForNewCulture()
        {
            if (EditorEthos != null) PopulateEditorCombo(EditorEthos, GetEthosOptions(), "");
            if (EditorHeritage != null) PopulateEditorCombo(EditorHeritage, GetHeritageOptions(), "");
            if (EditorLanguage != null) PopulateEditorCombo(EditorLanguage, GetLanguageOptions(), "");
            if (EditorMartialCustom != null) PopulateEditorCombo(EditorMartialCustom, GetMartialCustomOptions(), "");
            if (EditorHeadDetermination != null) PopulateEditorCombo(EditorHeadDetermination, GetHeadDeterminationOptions(), "");
            PopulateTraditionLists(null);
            if (EditorNameList != null) PopulateEditorCombo(EditorNameList, GetNameListOptions(), "");
            PopulateNameOrderConvention("");
            PopulateGfxLists("coa", new List<string>());
            PopulateGfxLists("building", new List<string>());
            PopulateGfxLists("clothing", new List<string>());
            PopulateGfxLists("unit", new List<string>());
            PopulateHouseCoaFrame("");
            PopulateEthnicityRows(new List<Ethnicity>());
            PopulateDlcTraditionRows(new List<DlcTradition>());
            ClearEditorLocalizationFields();
            _editorColorR = 255;
            _editorColorG = 255;
            _editorColorB = 255;
            _editorColorTouched = false;
            _editorColorReferenceName = "";
            if (EditorColorPreview != null)
                EditorColorPreview.Background = new SolidColorBrush(GetEditorArgb(255, 255, 255, 255));
        }

        private void ClearEditorLocalizationFields()
        {
            if (EditorLocName != null) EditorLocName.Text = "";
            if (EditorLocPrefix != null) EditorLocPrefix.Text = "";
            if (EditorLocCollective != null) EditorLocCollective.Text = "";
            if (EditorHistoryLocOverride != null) EditorHistoryLocOverride.Text = "";
            if (EditorHistoryLocDescription != null) EditorHistoryLocDescription.Text = "";
            if (EditorCreated != null) EditorCreated.Text = "";
            if (EditorCreatedPreview != null) EditorCreatedPreview.Text = "";
            PopulateParentLists(new List<string>());
        }

        private void PopulateEditorLocalizationFields(CultureInfo culture)
        {
            if (EditorLocName == null || EditorLocPrefix == null || EditorLocCollective == null) return;
            string rawKey = culture.RawKey ?? culture.Name ?? "";
            EditorLocName.Text = LookupLocalizationValue(rawKey) ?? "";
            EditorLocPrefix.Text = LookupLocalizationValue($"{rawKey}_prefix") ?? "";
            EditorLocCollective.Text = LookupLocalizationValue($"{rawKey}_collective_noun") ?? "";
            string historyKey = culture.HistoryLocOverride ?? "";
            if (EditorHistoryLocOverride != null) EditorHistoryLocOverride.Text = historyKey;
            if (EditorHistoryLocDescription != null)
                EditorHistoryLocDescription.Text = string.IsNullOrEmpty(historyKey)
                    ? ""
                    : (LookupLocalizationValue(historyKey) ?? "");
            if (EditorCreated != null)
            {
                var rawCreated = culture.Created ?? "";
                EditorCreated.Text = string.IsNullOrEmpty(rawCreated)
                    ? ""
                    : (ShiftCreatedDate(rawCreated, -GetCreatedOffset()) ?? rawCreated);
            }
            UpdateCreatedPreview();
            PopulateParentLists(culture.Parents ?? new List<string>());
        }

        private static string? LookupLocalizationValue(string key)
        {
            if (string.IsNullOrEmpty(key) || _editorLocalization == null) return null;
            return _editorLocalization.TryGetValue(key, out var value) ? value : null;
        }

        private void CaptureEditorSavedState()
        {
            if (_editorCulture == null)
            {
                _editorHasSavedState = false;
                return;
            }
            _editorHasSavedState = true;
            _editorSavedCultureId = _editorCulture.Name ?? "";
            _editorSavedEthos = _editorCulture.Ethos ?? "";
            _editorSavedHeritage = _editorCulture.Heritage ?? "";
            _editorSavedLanguage = _editorCulture.Language ?? "";
            _editorSavedMartialCustom = _editorCulture.MartialCustom ?? "";
            _editorSavedHeadDetermination = _editorCulture.HeadDetermination ?? "";
            _editorSavedColor = GetEditorColorString();
            _editorSavedTraditions = new List<string>(_editorCulture.TraditionKeys ?? new List<string>());
            _editorSavedDlcTraditions = new List<DlcTradition>(_editorCulture.DlcTraditions ?? new List<DlcTradition>());
            _editorSavedNameList = _editorCulture.NameList ?? "";
            _editorSavedNameOrderConvention = _editorCulture.NameOrderConvention ?? "";
            _editorSavedBuildingGfx = new List<string>(_editorCulture.BuildingGfx ?? new List<string>());
            _editorSavedClothingGfx = new List<string>(_editorCulture.ClothingGfx ?? new List<string>());
            _editorSavedUnitGfx = new List<string>(_editorCulture.UnitGfx ?? new List<string>());
            _editorSavedCoaGfx = new List<string>(_editorCulture.CoaGfx ?? new List<string>());
            _editorSavedHouseCoaFrame = _editorCulture.HouseCoaFrame ?? "";
            _editorSavedEthnicities = new List<Ethnicity>(_editorCulture.Ethnicities ?? new List<Ethnicity>());
            _editorSavedLocName = EditorLocName?.Text?.Trim() ?? "";
            _editorSavedLocPrefix = EditorLocPrefix?.Text?.Trim() ?? "";
            _editorSavedLocCollective = EditorLocCollective?.Text?.Trim() ?? "";
            _editorSavedHistoryLocOverride = _editorCulture.HistoryLocOverride ?? "";
            _editorSavedHistoryLocDescription = EditorHistoryLocDescription?.Text?.Trim() ?? "";
            _editorSavedCreated = _editorCulture.Created ?? "";
            _editorSavedParents = new List<string>(_editorCulture.Parents ?? new List<string>());
        }

        private void MarkEditorAsSaved()
        {
            _editorHasSavedState = true;
            _editorSavedCultureId = EditorCultureId?.Text?.Trim() ?? "";
            _editorSavedEthos = GetSelectedOption(EditorEthos);
            _editorSavedHeritage = GetSelectedOption(EditorHeritage);
            _editorSavedLanguage = GetSelectedOption(EditorLanguage);
            _editorSavedMartialCustom = GetSelectedOption(EditorMartialCustom);
            _editorSavedHeadDetermination = GetSelectedOption(EditorHeadDetermination);
            _editorSavedColor = GetEditorColorString();
            _editorSavedTraditions = GetSelectedTraditions();
            _editorSavedDlcTraditions = GetDlcTraditions();
            _editorSavedNameList = GetSelectedOption(EditorNameList);
            _editorSavedNameOrderConvention = GetEditorNameOrderConvention();
            _editorSavedBuildingGfx = GetSelectedGfx("building");
            _editorSavedClothingGfx = GetSelectedGfx("clothing");
            _editorSavedUnitGfx = GetSelectedGfx("unit");
            _editorSavedCoaGfx = GetSelectedGfx("coa");
            _editorSavedHouseCoaFrame = GetSelectedOption(EditorHouseCoaFrame);
            _editorSavedEthnicities = GetEthnicityEntries();
            _editorSavedLocName = EditorLocName?.Text?.Trim() ?? "";
            _editorSavedLocPrefix = EditorLocPrefix?.Text?.Trim() ?? "";
            _editorSavedLocCollective = EditorLocCollective?.Text?.Trim() ?? "";
            _editorSavedHistoryLocOverride = EditorHistoryLocOverride?.Text?.Trim() ?? "";
            _editorSavedHistoryLocDescription = EditorHistoryLocDescription?.Text?.Trim() ?? "";
            _editorSavedCreated = GetEditorCreatedFileValue();
            _editorSavedParents = GetSelectedParents();
            UpdateEditorDirtyState();
        }

        private void UpdateEditorDirtyState()
        {
            if (!_editorHasSavedState) return;
            SetLabelDirty(EditorIdLabel, (EditorCultureId?.Text?.Trim() ?? "") != _editorSavedCultureId);
            SetLabelDirty(EditorEthosLabel, GetSelectedOption(EditorEthos) != _editorSavedEthos);
            SetLabelDirty(EditorHeritageLabel, GetSelectedOption(EditorHeritage) != _editorSavedHeritage);
            SetLabelDirty(EditorLanguageLabel, GetSelectedOption(EditorLanguage) != _editorSavedLanguage);
            SetLabelDirty(EditorMartialCustomLabel, GetSelectedOption(EditorMartialCustom) != _editorSavedMartialCustom);
            SetLabelDirty(EditorHeadDeterminationLabel, GetSelectedOption(EditorHeadDetermination) != _editorSavedHeadDetermination);
            SetLabelDirty(EditorColorLabel, GetEditorColorString() != _editorSavedColor);
            SetLabelDirty(EditorTraditionsLabel, !TraditionListsEqual(GetSelectedTraditions(), _editorSavedTraditions));
            SetLabelDirty(EditorDlcTraditionsLabel, !DlcTraditionsEqual(GetDlcTraditions(), _editorSavedDlcTraditions));
            SetLabelDirty(EditorNameListLabel, GetSelectedOption(EditorNameList) != _editorSavedNameList);
            SetLabelDirty(EditorNameOrderConventionLabel, GetEditorNameOrderConvention() != _editorSavedNameOrderConvention);
            SetLabelDirty(EditorBuildingGfxLabel, !OrderedStringListsEqual(GetSelectedGfx("building"), _editorSavedBuildingGfx));
            SetLabelDirty(EditorClothingGfxLabel, !OrderedStringListsEqual(GetSelectedGfx("clothing"), _editorSavedClothingGfx));
            SetLabelDirty(EditorUnitGfxLabel, !OrderedStringListsEqual(GetSelectedGfx("unit"), _editorSavedUnitGfx));
            SetLabelDirty(EditorCoaGfxLabel, !OrderedStringListsEqual(GetSelectedGfx("coa"), _editorSavedCoaGfx));
            SetLabelDirty(EditorHouseCoaLabel, GetSelectedOption(EditorHouseCoaFrame) != _editorSavedHouseCoaFrame);
            SetLabelDirty(EditorEthnicitiesLabel, !EthnicityListsEqual(GetEthnicityEntries(), _editorSavedEthnicities));
            SetLabelDirty(EditorLocNameLabel, (EditorLocName?.Text?.Trim() ?? "") != _editorSavedLocName);
            SetLabelDirty(EditorLocPrefixLabel, (EditorLocPrefix?.Text?.Trim() ?? "") != _editorSavedLocPrefix);
            SetLabelDirty(EditorLocCollectiveLabel, (EditorLocCollective?.Text?.Trim() ?? "") != _editorSavedLocCollective);
            SetLabelDirty(EditorHistoryLocOverrideLabel, (EditorHistoryLocOverride?.Text?.Trim() ?? "") != _editorSavedHistoryLocOverride);
            SetLabelDirty(EditorHistoryLocDescriptionLabel, (EditorHistoryLocDescription?.Text?.Trim() ?? "") != _editorSavedHistoryLocDescription);
            SetLabelDirty(EditorCreatedLabel, GetEditorCreatedFileValue() != _editorSavedCreated);
            SetLabelDirty(EditorParentsLabel, !OrderedStringListsEqual(GetSelectedParents(), _editorSavedParents));
        }

        private static bool TraditionListsEqual(List<string> a, List<string> b)
            => a.Count == b.Count && a.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                                    .SequenceEqual(b.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

        private static bool OrderedStringListsEqual(List<string> a, List<string> b)
            => a.Count == b.Count && a.SequenceEqual(b, StringComparer.OrdinalIgnoreCase);

        private static void SetLabelDirty(System.Windows.Controls.Label? label, bool dirty)
        {
            if (label != null)
                label.Tag = dirty;
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

            var ethnicityEntries = GetEthnicityEntries();
            double totalWeight = ethnicityEntries.Sum(e => e.Weight);
            if (ethnicityEntries.Count > 0 && totalWeight > 100.0 + 0.001)
            {
                EditorStatusText.Text = string.Format(Res("CulturesTab_EditorEthnicitiesTotal"), FormatPercent(totalWeight));
                return;
            }

            string heritage = GetSelectedOption(EditorHeritage);
            if (string.IsNullOrEmpty(heritage))
            {
                EditorStatusText.Text = Res("CulturesTab_EditorNeedHeritage");
                return;
            }

            string created = EditorCreated?.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(created) && !_createdDateRegex.IsMatch(created))
            {
                EditorStatusText.Text = Res("CulturesTab_EditorCreatedInvalid");
                return;
            }

            string locName = EditorLocName?.Text?.Trim() ?? "";
            string locPrefix = EditorLocPrefix?.Text?.Trim() ?? "";
            string locCollective = EditorLocCollective?.Text?.Trim() ?? "";
            string locHistoryKey = EditorHistoryLocOverride?.Text?.Trim() ?? "";
            string locHistoryDescription = EditorHistoryLocDescription?.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(locHistoryDescription) && string.IsNullOrEmpty(locHistoryKey))
                locHistoryKey = $"{cultureId}_history_loc";

            bool hasLocBaseline = _editorHasSavedState;
            bool nameChanged = hasLocBaseline ? locName != _editorSavedLocName : !string.IsNullOrEmpty(locName);
            bool prefixChanged = hasLocBaseline ? locPrefix != _editorSavedLocPrefix : !string.IsNullOrEmpty(locPrefix);
            bool collectiveChanged = hasLocBaseline ? locCollective != _editorSavedLocCollective : !string.IsNullOrEmpty(locCollective);
            bool historyChanged = hasLocBaseline
                ? (locHistoryKey != _editorSavedHistoryLocOverride || locHistoryDescription != _editorSavedHistoryLocDescription)
                : !string.IsNullOrEmpty(locHistoryDescription);

            if (hasLocBaseline)
            {
                var blankFields = new List<string>();
                if (!string.IsNullOrEmpty(_editorSavedLocName) && string.IsNullOrEmpty(locName))
                    blankFields.Add(Res("CulturesTab_EditorLocName"));
                if (!string.IsNullOrEmpty(_editorSavedLocPrefix) && string.IsNullOrEmpty(locPrefix))
                    blankFields.Add(Res("CulturesTab_EditorLocPrefix"));
                if (!string.IsNullOrEmpty(_editorSavedLocCollective) && string.IsNullOrEmpty(locCollective))
                    blankFields.Add(Res("CulturesTab_EditorLocCollective"));
                if (!string.IsNullOrEmpty(_editorSavedHistoryLocDescription) && string.IsNullOrEmpty(locHistoryDescription))
                    blankFields.Add(Res("CulturesTab_EditorHistoryLocDescription"));
                if (blankFields.Count > 0)
                {
                    EditorStatusText.Text = string.Format(Res("CulturesTab_EditorLocBlank"), string.Join(", ", blankFields));
                    return;
                }
            }

            string block = BuildCultureBlock(cultureId, locHistoryKey);

            if (_editorCulture == null || _editorIsNew)
            {
                SaveAsNewCulture(cultureId, block, (cid) => SaveCultureLocalizationAsync(cid, nameChanged, locName, prefixChanged, locPrefix, collectiveChanged, locCollective, historyChanged, locHistoryKey, locHistoryDescription));
            }
            else
            {
                SaveExistingCulture(block, (cid) => SaveCultureLocalizationAsync(cid, nameChanged, locName, prefixChanged, locPrefix, collectiveChanged, locCollective, historyChanged, locHistoryKey, locHistoryDescription));
            }
        }

        private async Task SaveCultureLocalizationAsync(string cultureId, bool nameChanged, string name, bool prefixChanged, string prefix, bool collectiveChanged, string collective, bool historyChanged, string historyKey, string historyDescription)
        {
            var profile = _viewModel?.CurrentProfile;
            if (profile == null) return;
            string modRoot = profile.ModRoot ?? "";
            if (string.IsNullOrEmpty(modRoot)) return;

            if (!nameChanged && !prefixChanged && !collectiveChanged && !historyChanged) return;

            if (!string.IsNullOrEmpty(historyDescription) && string.IsNullOrEmpty(historyKey))
                historyKey = $"{cultureId}_history_loc";

            bool autoTranslate = _viewModel?.AutoTranslate ?? true;
            EditorStatusText.Text = autoTranslate ? Res("CulturesTab_EditorLocTranslating") : Res("CulturesTab_EditorLocWriting");
            SetEditorBusy(true);
            try
            {
            string appLang = _viewModel?.Language ?? "en";
            string? directFolder = appLang switch
            {
                "es" => "spanish",
                "en" => "english",
                _ => null
            };

            bool existsInBase = _baseCultureRawKeys.Contains(cultureId);
            string baseLocPath = Path.Combine(modRoot, "localization");
            if (existsInBase)
                baseLocPath = Path.Combine(baseLocPath, "replace");

            string srcCode = appLang.ToLowerInvariant() switch { "es" => "es", "en" => "en", _ => "ca" };

            var providers = autoTranslate ? BuildEnabledProviders() : new List<ITranslationProvider>();

            List<(string Folder, string Code)> targets;
            if (autoTranslate)
                targets = GameSupportedLanguages.Select(f => (f.Folder, f.Code)).ToList();
            else if (directFolder != null)
                targets = new List<(string Folder, string Code)> { (directFolder, srcCode) };
            else
                targets = new List<(string Folder, string Code)>();

            int saved = 0;
            var errors = new List<string>();
            var fallbackLangs = new List<string>();
            foreach (var (ck3Folder, code) in targets)
            {
                string locName = name;
                string locPrefix = prefix;
                string locCollective = collective;
                string locHistoryKey = historyKey;
                string locHistoryDescription = historyDescription;

                bool usedFallback = false;
                if (autoTranslate && ck3Folder != directFolder)
                {
                    if (nameChanged)
                    {
                        var (trName, okName) = await TranslateWithFallbackAsync(name, srcCode, code, providers);
                        locName = string.IsNullOrEmpty(trName) ? name : trName;
                        usedFallback |= !okName;
                    }
                    if (prefixChanged)
                    {
                        var (trPrefix, okPrefix) = await TranslateWithFallbackAsync(prefix, srcCode, code, providers);
                        locPrefix = string.IsNullOrEmpty(trPrefix) ? prefix : trPrefix;
                        usedFallback |= !okPrefix;
                    }
                    if (collectiveChanged)
                    {
                        var (trCollective, okCollective) = await TranslateWithFallbackAsync(collective, srcCode, code, providers);
                        locCollective = string.IsNullOrEmpty(trCollective) ? collective : trCollective;
                        usedFallback |= !okCollective;
                    }
                    if (historyChanged)
                    {
                        var (trHistory, okHistory) = await TranslateWithFallbackAsync(historyDescription, srcCode, code, providers);
                        locHistoryDescription = string.IsNullOrEmpty(trHistory) ? historyDescription : trHistory;
                        usedFallback |= !okHistory;
                    }
                }

                string folderPath = Path.Combine(baseLocPath, ck3Folder);
                try
                {
                    string cultureDir = Path.Combine(folderPath, "culture");
                    Directory.CreateDirectory(cultureDir);
                    string filePath = Path.Combine(cultureDir, $"cultures_l_{ck3Folder}.yml");
                    var entries = new List<(string Key, string Value)>();
                    if (nameChanged && !string.IsNullOrEmpty(locName))
                        entries.Add((cultureId, locName));
                    if (prefixChanged && !string.IsNullOrEmpty(locPrefix))
                        entries.Add(($"{cultureId}_prefix", locPrefix));
                    if (collectiveChanged && !string.IsNullOrEmpty(locCollective))
                        entries.Add(($"{cultureId}_collective_noun", locCollective));
                    if (entries.Count > 0)
                        UpsertLocalizationFile(filePath, $"l_{ck3Folder}:", entries);

                    if (historyChanged && !string.IsNullOrEmpty(locHistoryKey) && !string.IsNullOrEmpty(locHistoryDescription))
                    {
                        string historyFilePath = Path.Combine(cultureDir, $"culture_history_l_{ck3Folder}.yml");
                        UpsertLocalizationFile(historyFilePath, $"l_{ck3Folder}:",
                            new List<(string Key, string Value)> { (locHistoryKey, locHistoryDescription) });
                    }

                    saved++;
                    if (usedFallback)
                        fallbackLangs.Add(ck3Folder);
                }
                catch
                {
                    errors.Add(ck3Folder);
                }
            }

            if (errors.Count > 0)
            {
                EditorStatusText.Text = $"{string.Format(Res("CulturesTab_EditorLocSaved"), saved)} {Res("CulturesTab_EditorLocError")}: {string.Join(", ", errors)}";
            }
            else if (fallbackLangs.Count > 0)
            {
                EditorStatusText.Text = $"{string.Format(Res("CulturesTab_EditorLocSaved"), saved)} {string.Format(Res("CulturesTab_EditorLocFallback"), string.Join(", ", fallbackLangs))}";
            }
            else if (saved == 0)
            {
                EditorStatusText.Text = Res("CulturesTab_EditorLocDisabled");
            }
            else
            {
                EditorStatusText.Text = string.Format(Res("CulturesTab_EditorLocSaved"), saved);
            }
            }
            finally
            {
                SetEditorBusy(false);
            }
        }

        private void SetEditorBusy(bool busy)
        {
            EditorSaveButton.IsEnabled = !busy;
            EditorClearButton.IsEnabled = !busy;
            this.Cursor = busy ? System.Windows.Input.Cursors.Wait : System.Windows.Input.Cursors.Arrow;
        }

        private static async Task<(string? Text, bool Ok)> TranslateWithFallbackAsync(string text, string sourceCode, string targetCode, List<ITranslationProvider> providers)
        {
            if (string.IsNullOrWhiteSpace(text)) return (text, true);
            foreach (var provider in providers)
            {
                var (result, ok) = await provider.TranslateAsync(text, sourceCode, targetCode);
                if (ok && !string.IsNullOrWhiteSpace(result))
                    return (result, true);
            }
            return (text, false);
        }

        private List<ITranslationProvider> BuildEnabledProviders()
        {
            var enabled = _viewModel?.EnabledTranslationProviders ?? new List<string> { TranslationProviderConstants.MyMemory };
            var urls = _viewModel?.TranslationProviderUrls ?? new Dictionary<string, string>();
            var key = _viewModel?.DeeplApiKey;

            var list = new List<ITranslationProvider>();
            foreach (var id in enabled)
            {
                switch (id)
                {
                    case TranslationProviderConstants.MyMemory:
                        list.Add(new MyMemoryProvider(_translationHttp));
                        break;
                    case TranslationProviderConstants.LibreTranslate:
                        var ltUrl = urls.TryGetValue(TranslationProviderConstants.LibreTranslate, out var lu) && !string.IsNullOrWhiteSpace(lu)
                            ? lu! : TranslationProviderConstants.DefaultLibreTranslateUrl;
                        list.Add(new LibreTranslateProvider(_translationHttp, ltUrl));
                        break;
                    case TranslationProviderConstants.Lingva:
                        var lvUrl = urls.TryGetValue(TranslationProviderConstants.Lingva, out var lv) && !string.IsNullOrWhiteSpace(lv)
                            ? lv! : TranslationProviderConstants.DefaultLingvaUrl;
                        list.Add(new LingvaProvider(_translationHttp, lvUrl));
                        break;
                    case TranslationProviderConstants.DeepL:
                        if (!string.IsNullOrWhiteSpace(key))
                            list.Add(new DeepLProvider(_translationHttp, key));
                        break;
                }
            }

            if (list.Count == 0)
                list.Add(new MyMemoryProvider(_translationHttp));

            var rng = new Random();
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }

        private static void UpsertLocalizationFile(string filePath, string header, List<(string Key, string Value)> entries)
        {
            var sb = new StringBuilder();
            if (!File.Exists(filePath))
            {
                sb.AppendLine(header);
                sb.AppendLine();
                foreach (var e in entries.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase))
                    sb.AppendLine($"{e.Key}:0 \"{e.Value.Replace("\"", "\\\"")}\"");
                File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true));
                return;
            }

            var lines = new List<string>(File.ReadAllLines(filePath));
            var toInsert = new List<string>();
            foreach (var e in entries)
            {
                bool found = false;
                for (int i = 0; i < lines.Count; i++)
                {
                    string trimmed = lines[i].TrimStart();
                    if (trimmed.StartsWith(e.Key + ":", StringComparison.Ordinal))
                    {
                        lines[i] = $"{e.Key}:0 \"{e.Value.Replace("\"", "\\\"")}\"";
                        found = true;
                        break;
                    }
                }
                if (!found)
                    toInsert.Add($"{e.Key}:0 \"{e.Value.Replace("\"", "\\\"")}\"");
            }

            if (toInsert.Count > 0)
            {
                int insertAt = 1;
                if (insertAt < lines.Count && string.IsNullOrWhiteSpace(lines[insertAt]))
                    insertAt++;
                lines.InsertRange(insertAt, toInsert.Select(x => x));
            }

            var headLines = new List<string>();
            var entryLines = new List<string>();
            foreach (var line in lines)
            {
                if (IsLocalizationEntryLine(line))
                    entryLines.Add(line);
                else
                    headLines.Add(line);
            }
            entryLines.Sort(CompareLocalizationEntries);

            File.WriteAllLines(filePath, headLines.Concat(entryLines), new UTF8Encoding(true));
        }

        private static bool IsLocalizationEntryLine(string line)
        {
            string t = line.Trim();
            if (string.IsNullOrEmpty(t) || t.StartsWith("#")) return false;
            int colon = t.IndexOf(':');
            if (colon <= 0) return false;
            int i = colon + 1;
            while (i < t.Length && t[i] == ' ') i++;
            if (i >= t.Length || !char.IsDigit(t[i])) return false;
            while (i < t.Length && char.IsDigit(t[i])) i++;
            while (i < t.Length && t[i] == ' ') i++;
            return i < t.Length && t[i] == '"';
        }

        private static string GetLocalizationKey(string line)
        {
            string t = line.TrimStart();
            int colon = t.IndexOf(':');
            return colon > 0 ? t.Substring(0, colon) : t;
        }

        private static int CompareLocalizationEntries(string a, string b)
            => string.Compare(GetLocalizationKey(a), GetLocalizationKey(b), StringComparison.OrdinalIgnoreCase);

        private void SaveAsNewCulture(string cultureId, string block, Func<string, Task> afterSave)
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
                MarkEditorAsSaved();
                _ = afterSave(cultureId);
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

        private void SaveExistingCulture(string block, Func<string, Task> afterSave)
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
                MarkEditorAsSaved();
                _ = afterSave(rawKey);
            }
            catch (Exception ex)
            {
                EditorStatusText.Text = $"{Res("CulturesTab_EditorSaveError")}: {ex.Message}";
            }
        }

        private string BuildCultureBlock(string cultureId, string historyLocOverride)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{cultureId} = {{");

            string color = GetEditorColorString();
            if (!string.IsNullOrEmpty(color))
            {
                if (color.StartsWith("rgb ", StringComparison.Ordinal))
                {
                    var parts = color.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (parts.Length == 4 &&
                        byte.TryParse(parts[1], out byte r) &&
                        byte.TryParse(parts[2], out byte g) &&
                        byte.TryParse(parts[3], out byte b))
                    {
                        sb.AppendLine($"\tcolor = {{ {FormatNormalizedColor(r)} {FormatNormalizedColor(g)} {FormatNormalizedColor(b)} }}");
                    }
                }
                else
                {
                    sb.AppendLine($"\tcolor = {color}");
                }
            }

            var parents = GetSelectedParents();
            if (parents.Count > 0)
                sb.AppendLine($"\tparents = {{ {string.Join(" ", parents)} }}");

            string created = EditorCreated?.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(created))
            {
                string fileCreated = ShiftCreatedDate(created, GetCreatedOffset()) ?? created;
                sb.AppendLine($"\tcreated = {fileCreated}");
            }

            string heritage = GetSelectedOption(EditorHeritage);
            if (!string.IsNullOrEmpty(heritage))
                sb.AppendLine($"\theritage = {heritage}");

            string ethos = GetSelectedOption(EditorEthos);
            if (!string.IsNullOrEmpty(ethos))
                sb.AppendLine($"\tethos = {ethos}");

            string language = GetSelectedOption(EditorLanguage);
            if (!string.IsNullOrEmpty(language))
                sb.AppendLine($"\tlanguage = {language}");

            string martialCustom = GetSelectedOption(EditorMartialCustom);
            if (!string.IsNullOrEmpty(martialCustom))
                sb.AppendLine($"\tmartial_custom = {martialCustom}");

            string headDetermination = GetSelectedOption(EditorHeadDetermination);
            if (!string.IsNullOrEmpty(headDetermination))
                sb.AppendLine($"\thead_determination = {headDetermination}");

            if (!string.IsNullOrEmpty(historyLocOverride))
                sb.AppendLine($"\thistory_loc_override = {historyLocOverride}");

            var traditions = GetSelectedTraditions();
            if (traditions.Count > 0)
            {
                sb.AppendLine("\ttraditions = {");
                foreach (var tradition in traditions)
                    sb.AppendLine($"\t\t{tradition}");
                sb.AppendLine("\t}");
            }

            foreach (var dlcTradition in GetDlcTraditions())
            {
                sb.AppendLine("\tdlc_tradition = {");
                if (!string.IsNullOrEmpty(dlcTradition.Trait))
                    sb.AppendLine($"\t\ttrait = {dlcTradition.Trait}");
                if (!string.IsNullOrEmpty(dlcTradition.RequiresDlcFlag))
                    sb.AppendLine($"\t\trequires_dlc_flag = {dlcTradition.RequiresDlcFlag}");
                if (!string.IsNullOrEmpty(dlcTradition.Fallback))
                    sb.AppendLine($"\t\tfallback = {dlcTradition.Fallback}");
                sb.AppendLine("\t}");
            }

            string nameList = GetSelectedOption(EditorNameList);
            if (!string.IsNullOrEmpty(nameList))
                sb.AppendLine($"\tname_list = {nameList}");

            string nameOrderConvention = GetEditorNameOrderConvention();
            if (!string.IsNullOrEmpty(nameOrderConvention))
                sb.AppendLine($"\tname_order_convention = {nameOrderConvention}");

            var coaGfx = GetSelectedGfx("coa");
            if (coaGfx.Count > 0)
                sb.AppendLine($"\tcoa_gfx = {{ {string.Join(" ", coaGfx)} }}");

            var buildingGfx = GetSelectedGfx("building");
            if (buildingGfx.Count > 0)
                sb.AppendLine($"\tbuilding_gfx = {{ {string.Join(" ", buildingGfx)} }}");

            var clothingGfx = GetSelectedGfx("clothing");
            if (clothingGfx.Count > 0)
                sb.AppendLine($"\tclothing_gfx = {{ {string.Join(" ", clothingGfx)} }}");

            var unitGfx = GetSelectedGfx("unit");
            if (unitGfx.Count > 0)
                sb.AppendLine($"\tunit_gfx = {{ {string.Join(" ", unitGfx)} }}");

            string houseCoaFrame = GetSelectedOption(EditorHouseCoaFrame);
            if (!string.IsNullOrEmpty(houseCoaFrame))
            {
                sb.AppendLine($"\thouse_coa_frame = {houseCoaFrame}");
                if (_editorHouseCoaOffsetScale.TryGetValue(houseCoaFrame, out var houseCoaMap))
                {
                    if (!string.IsNullOrEmpty(houseCoaMap.Offset))
                        sb.AppendLine($"\thouse_coa_mask_offset = {{ {houseCoaMap.Offset} }}");
                    if (!string.IsNullOrEmpty(houseCoaMap.Scale))
                        sb.AppendLine($"\thouse_coa_mask_scale = {{ {houseCoaMap.Scale} }}");
                }
            }

            var ethnicities = GetEthnicityEntries();
            if (ethnicities.Count > 0)
            {
                sb.AppendLine("\tethnicities = {");
                foreach (var ethnicity in ethnicities)
                    sb.AppendLine($"\t\t{FormatPercent(ethnicity.Weight)} = {ethnicity.Name}");
                sb.AppendLine("\t}");
            }

            sb.AppendLine("}");
            sb.AppendLine();
            return sb.ToString();
        }

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

        private static bool DeleteCultureBlockFromFile(string filePath, string cultureId)
        {
            string text = File.ReadAllText(filePath);
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

                if (string.Equals(key, cultureId, StringComparison.OrdinalIgnoreCase))
                {
                    int entryEnd = pos;
                    string newText = text.Remove(keyStart, entryEnd - keyStart);
                    File.WriteAllText(filePath, newText, new System.Text.UTF8Encoding(true));
                    return true;
                }
            }
            return false;
        }

        private static int CountCultureBlocks(string filePath)
        {
            string text = File.ReadAllText(filePath);
            int pos = 0;
            int count = 0;
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
                pos++;
                ReadBlock(text, ref pos);
                count++;
            }
            return count;
        }

        private static void DeleteCultureLocalization(string modRoot, string cultureId, bool existsInBase)
        {
            var keysToRemove = new[] { cultureId, $"{cultureId}_prefix", $"{cultureId}_collective_noun" };
            string baseLocPath = Path.Combine(modRoot, "localization");
            if (existsInBase)
                baseLocPath = Path.Combine(baseLocPath, "replace");

            if (!Directory.Exists(baseLocPath)) return;
            foreach (var file in Directory.GetFiles(baseLocPath, "cultures_l_*.yml", SearchOption.AllDirectories))
                RemoveLocalizationKeys(file, keysToRemove);
        }

        private static void DeleteCultureHistoryLocalization(string modRoot, string historyKey, bool existsInBase)
        {
            string baseLocPath = Path.Combine(modRoot, "localization");
            if (existsInBase)
                baseLocPath = Path.Combine(baseLocPath, "replace");

            if (!Directory.Exists(baseLocPath)) return;
            foreach (var file in Directory.GetFiles(baseLocPath, "culture_history_l_*.yml", SearchOption.AllDirectories))
                RemoveLocalizationKeys(file, new[] { historyKey });
        }

        private static void RemoveLocalizationKeys(string filePath, string[] keys)
        {
            var lines = new List<string>(File.ReadAllLines(filePath));
            bool changed = false;
            lines.RemoveAll(line =>
            {
                string t = line.TrimStart();
                foreach (var key in keys)
                {
                    if (t.StartsWith(key + ":", StringComparison.OrdinalIgnoreCase))
                    {
                        changed = true;
                        return true;
                    }
                }
                return false;
            });

            if (changed)
            {
                if (!HasLocalizationEntries(lines))
                    File.Delete(filePath);
                else
                    File.WriteAllLines(filePath, lines, new UTF8Encoding(true));
            }
        }

        private static bool HasLocalizationEntries(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                string t = line.Trim();
                if (string.IsNullOrEmpty(t)) continue;
                if (t.StartsWith('#')) continue;
                if (t.IndexOf(':') >= 0 && t.IndexOf('"') >= 0)
                    return true;
            }
            return false;
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

            var fileNames = new[]
            {
                "cultures_l",
                "cultural_heritages_l",
                "cultural_traditions_l",
                "cultural_languages_l",
                "head_determination_l",
                "culture_name_lists_l",
                "culture_history_l",
                "culture_gfx_l"
            };

            foreach (var root in new[] { modRoot, gameRoot })
            {
                if (string.IsNullOrEmpty(root)) continue;
                foreach (var langRoot in new[]
                {
                    Path.Combine(root, "localization", "replace", ck3Lang),
                    Path.Combine(root, "localization", ck3Lang)
                })
                {
                    var cultureDir = Path.Combine(langRoot, "culture");
                    if (!Directory.Exists(cultureDir)) continue;
                    foreach (var name in fileNames)
                    {
                        foreach (var path in Directory.GetFiles(cultureDir, $"{name}_{ck3Lang}.yml", SearchOption.AllDirectories))
                            ParseLocalizationFile(path, result);
                    }
                }
            }

            foreach (var kvp in result.ToList())
            {
                var value = kvp.Value;
                if (value.Length > 2 && value.StartsWith('$') && value.EndsWith('$'))
                {
                    var inner = value.Substring(1, value.Length - 2);
                    if (result.TryGetValue(inner, out var resolved) && !string.IsNullOrEmpty(resolved))
                        result[kvp.Key] = resolved;
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

        private static Dictionary<string, HeritageInfo> LoadHeritageDefinitions(string gameRoot, string modRoot, HashSet<string>? baseKeys = null)
        {
            var result = new Dictionary<string, HeritageInfo>(StringComparer.OrdinalIgnoreCase);
            string modSub = Path.Combine(modRoot, "common", "culture", "pillars", "mod");

            foreach (var (root, source) in new[] { (modRoot, "Mod"), (gameRoot, "Base") })
            {
                if (string.IsNullOrEmpty(root)) continue;
                var dir = Path.Combine(root, "common", "culture", "pillars");
                if (!Directory.Exists(dir)) continue;
                foreach (var file in Directory.GetFiles(dir, "*heritage.txt", SearchOption.AllDirectories))
                {
                    bool isModNew = source == "Mod" && IsPathInside(file, modSub);
                    if (source == "Base" && baseKeys != null)
                    {
                        var temp = new Dictionary<string, HeritageInfo>(StringComparer.OrdinalIgnoreCase);
                        ParseHeritageFile(file, temp, "Base", false);
                        foreach (var key in temp.Keys)
                            baseKeys.Add(key);
                    }
                    ParseHeritageFile(file, result, source, isModNew);
                }
            }

            return result;
        }

        private static void ParseHeritageFile(string filePath, Dictionary<string, HeritageInfo> output, string source, bool isModNew)
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
                var heritage = new HeritageInfo { Name = heritageKey, Source = source, SourceFile = filePath, IsModNew = isModNew };
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

        private static Dictionary<string, List<string>> BuildGfxValues(IEnumerable<CultureInfo> cultures)
        {
            var categories = new[] { "coa", "building", "clothing", "unit" };
            var sets = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var category in categories)
                sets[category] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var culture in cultures)
            {
                if (culture == null) continue;
                AddGfxSet(sets["coa"], culture.CoaGfx);
                AddGfxSet(sets["building"], culture.BuildingGfx);
                AddGfxSet(sets["clothing"], culture.ClothingGfx);
                AddGfxSet(sets["unit"], culture.UnitGfx);
            }

            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var category in categories)
                result[category] = sets[category].OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToList();
            return result;
        }

        private static List<string> BuildEthnicityOptions(IEnumerable<CultureInfo> cultures)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var culture in cultures)
            {
                if (culture == null) continue;
                foreach (var ethnicity in culture.Ethnicities ?? new List<Ethnicity>())
                    if (!string.IsNullOrWhiteSpace(ethnicity.Name))
                        set.Add(ethnicity.Name.Trim());
            }
            return set.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<string> BuildCultureOptions(IEnumerable<CultureInfo> cultures)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var culture in cultures)
            {
                if (culture == null) continue;
                if (!string.IsNullOrWhiteSpace(culture.Name))
                    set.Add(culture.Name.Trim());
            }
            return set.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private void BuildCultureDisplayNames(Dictionary<string, CultureInfo> allByName, Dictionary<string, string> appLocalization)
        {
            Dictionary<string, string> englishLocalization;
            try
            {
                var gameRoot = _viewModel?.CurrentProfile?.GameRoot ?? "";
                var modRoot = _viewModel?.CurrentProfile?.ModRoot ?? "";
                englishLocalization = LoadLocalization(gameRoot, modRoot, "en");
            }
            catch
            {
                englishLocalization = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            _editorCultureDisplayNames.Clear();
            foreach (var key in allByName.Keys)
            {
                if (appLocalization.TryGetValue(key, out var appName))
                    _editorCultureDisplayNames[key] = appName;
                else if (englishLocalization.TryGetValue(key, out var enName))
                    _editorCultureDisplayNames[key] = enName;
                else
                    _editorCultureDisplayNames[key] = key;
            }
        }

        private void BuildTraditionOptions()
        {
            var baseList = new List<string>();
            var dlcList = new List<string>();
            foreach (var def in _editorTraditionDefs.Values)
            {
                if (IsDlcTradition(def))
                    dlcList.Add(def.Name);
                else
                    baseList.Add(def.Name);
            }
            _editorTraditionOptions = baseList.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
            _editorDlcTraditionOptions = dlcList.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static readonly string[] _dlcTraditionCodes =
        {
            "fp1", "fp2", "fp3", "fp4",
            "ep1", "ep2", "ep3", "ep4",
            "ce1", "ce2",
            "tgp", "mpo", "ach", "cote"
        };

        private static bool IsDlcTradition(TraditionInfo def)
        {
            if (def.IsDlc) return true;
            if (!string.IsNullOrEmpty(def.RequiresDlcFlag)) return true;
            string name = def.Name;
            if (!name.StartsWith("tradition_", StringComparison.OrdinalIgnoreCase)) return false;
            foreach (var code in _dlcTraditionCodes)
            {
                if (name.StartsWith("tradition_" + code + "_", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private string GetCultureDisplayName(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;
            return _editorCultureDisplayNames.TryGetValue(key, out var displayName)
                ? displayName
                : key;
        }

        private static string? ShiftCreatedDate(string date, int offset)
        {
            var parts = date.Split('.');
            if (parts.Length != 3) return null;
            if (!int.TryParse(parts[0], out var year)) return null;
            return $"{year + offset}.{parts[1]}.{parts[2]}";
        }

        private void UpdateCreatedPreview()
        {
            if (EditorCreatedPreview == null) return;
            string calculated = EditorCreated?.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(calculated))
            {
                EditorCreatedPreview.Text = "";
                return;
            }
            var fileValue = ShiftCreatedDate(calculated, GetCreatedOffset());
            EditorCreatedPreview.Text = fileValue == null
                ? ""
                : string.Format(Res("CulturesTab_EditorCreatedFilePreview"), fileValue);
        }

        private string GetEditorCreatedFileValue()
        {
            string calculated = EditorCreated?.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(calculated)) return "";
            return ShiftCreatedDate(calculated, GetCreatedOffset()) ?? calculated;
        }

        private static void AddGfxSet(HashSet<string> set, IEnumerable<string> values)
        {
            foreach (var value in values ?? Enumerable.Empty<string>())
                if (!string.IsNullOrWhiteSpace(value))
                    set.Add(value.Trim());
        }

        private void LoadHouseCoaMapping(List<CultureInfo> baseCultures, List<CultureInfo> modCultures)
        {
            var frames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var offsetByFrame = new Dictionary<string, (string Offset, string Scale)>(StringComparer.OrdinalIgnoreCase);

            foreach (var culture in baseCultures.Concat(modCultures))
            {
                if (culture == null) continue;
                var frame = culture.HouseCoaFrame?.Trim() ?? "";
                if (string.IsNullOrEmpty(frame)) continue;
                frames.Add(frame);

                var off = culture.HouseCoaMaskOffset?.Trim() ?? "";
                var scale = culture.HouseCoaMaskScale?.Trim() ?? "";
                if (string.IsNullOrEmpty(off) || string.IsNullOrEmpty(scale)) continue;
                if (!offsetByFrame.ContainsKey(frame))
                    offsetByFrame[frame] = (off, scale);
            }

            _editorHouseCoaFrames = frames.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
            _editorHouseCoaOffsetScale = offsetByFrame;
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
            bool isDlcFile = !Path.GetFileName(filePath).StartsWith("00_", StringComparison.OrdinalIgnoreCase);
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
                var tradition = new TraditionInfo { Name = key, IsDlc = isDlcFile };
                ParseTraditionParameters(block, tradition.Parameters, tradition);
                output[key] = tradition;
            }
        }

        private static void ParseTraditionParameters(string block, List<TraditionParameter> parameters, TraditionInfo tradition)
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
                    string scalarValue = block.Substring(start, pos - start);
                    if (string.Equals(key, "requires_dlc_flag", StringComparison.OrdinalIgnoreCase))
                        tradition.RequiresDlcFlag = scalarValue;
                    parameters.Add(new TraditionParameter
                    {
                        Key = key,
                        Content = scalarValue
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

        private static List<string> ExtractParentsAttribute(string block)
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

                if (key == "parents")
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

        private static List<DlcTradition> ExtractDlcTraditionsAttribute(string block)
        {
            var result = new List<DlcTradition>();
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

                if (string.Equals(key, "dlc_tradition", StringComparison.OrdinalIgnoreCase))
                {
                    if (block[pos] == '{')
                    {
                        string content = ReadBraceContent(block, ref pos);
                        var entry = ParseDlcTraditionEntry(content);
                        if (entry != null) result.Add(entry);
                    }
                    else
                    {
                        SkipValueAndFollowingBlock(block, ref pos);
                    }
                }
                else
                {
                    SkipValueAndFollowingBlock(block, ref pos);
                }
            }

            return result;
        }

        private static DlcTradition? ParseDlcTraditionEntry(string content)
        {
            string trait = "", flag = "", fallback = "";
            int pos = 0;
            while (pos < content.Length)
            {
                SkipWhitespaceAndComments(content, ref pos);
                if (pos >= content.Length) break;

                string key = ReadKey(content, ref pos);
                if (string.IsNullOrEmpty(key)) break;

                SkipWhitespaceAndComments(content, ref pos);
                if (pos >= content.Length || content[pos] != '=')
                {
                    SkipValueAndFollowingBlock(content, ref pos);
                    continue;
                }
                pos++;

                SkipWhitespaceAndComments(content, ref pos);
                if (pos >= content.Length) break;

                string scalar = ReadKey(content, ref pos);
                if (string.Equals(key, "trait", StringComparison.OrdinalIgnoreCase)) trait = scalar;
                else if (string.Equals(key, "requires_dlc_flag", StringComparison.OrdinalIgnoreCase)) flag = scalar;
                else if (string.Equals(key, "fallback", StringComparison.OrdinalIgnoreCase)) fallback = scalar;
            }

            if (string.IsNullOrEmpty(trait)) return null;
            return new DlcTradition { Trait = trait, RequiresDlcFlag = flag, Fallback = fallback };
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

                culture.HistoryLocOverride = ExtractAttribute(block, "history_loc_override") ?? "";
                culture.Created = ExtractAttribute(block, "created") ?? "";
                culture.Parents = ExtractParentsAttribute(block);
                culture.NameOrderConvention = ExtractAttribute(block, "name_order_convention") ?? "";

                culture.TraditionKeys = ExtractTraditionsAttribute(block);
                culture.DlcTraditions = ExtractDlcTraditionsAttribute(block);

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
                        var result = block.Substring(start, pos - start);
                        return result;
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

        private static string FormatNormalizedColor(byte value)
        {
            return (value / 255f).ToString("0.0##", System.Globalization.CultureInfo.InvariantCulture);
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

            // Handle HSV color format: hsv{...} or hsv { ... }
            if (pos + 3 < block.Length && 
                char.ToLowerInvariant(block[pos]) == 'h' &&
                char.ToLowerInvariant(block[pos + 1]) == 's' &&
                char.ToLowerInvariant(block[pos + 2]) == 'v')
            {
                // Skip "hsv" 
                pos += 3;
                SkipWhitespaceAndComments(block, ref pos);
                if (pos < block.Length && block[pos] == '{')
                {
                    pos++; // skip '{'
                    int depth = 1;
                    while (pos < block.Length && depth > 0)
                    {
                        if (block[pos] == '{') depth++;
                        else if (block[pos] == '}') depth--;
                        pos++;
                    }
                }
                else
                {
                    // hsv without braces - skip until whitespace or }
                    while (pos < block.Length && !char.IsWhiteSpace(block[pos]) && block[pos] != '}') pos++;
                }
                SkipWhitespaceAndComments(block, ref pos);
                return;
            }

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

                if ((culture.DlcTraditions?.Count ?? 0) > 0)
                {
                    DlcTraditionsList.ItemsSource = (culture.DlcTraditions ?? new List<DlcTradition>()).Select(d => BuildDlcTraditionDetailText(d)).ToList();
                    DetailDlcTraditionsPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    DlcTraditionsList.ItemsSource = null;
                    DetailDlcTraditionsPanel.Visibility = Visibility.Collapsed;
                }

                if (!string.IsNullOrEmpty(culture.HistoryLocOverride))
                {
                    DetailHistoryLocOverrideValue.Text = culture.HistoryLocOverride;
                    DetailHistoryDescriptionValue.Text = LookupLocalizationValue(culture.HistoryLocOverride) ?? "-";
                    DetailHistoryLocPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    DetailHistoryLocPanel.Visibility = Visibility.Collapsed;
                }

                bool hasLineage = !string.IsNullOrEmpty(culture.Created) || (culture.Parents?.Count ?? 0) > 0;
                if (hasLineage)
                {
                    DetailCreatedValue.Text = string.IsNullOrEmpty(culture.Created)
                        ? "-"
                        : string.Format(Res("CulturesTab_DetailCreatedValue"),
                            ShiftCreatedDate(culture.Created, -GetCreatedOffset()) ?? culture.Created,
                            culture.Created);
                    DetailParentsValue.Text = (culture.Parents?.Count ?? 0) == 0
                        ? "-"
                        : string.Format(Res("CulturesTab_DetailParentsValue"),
                            string.Join(", ", (culture.Parents ?? new List<string>()).Select(p => GetCultureDisplayName(p))));
                    DetailLineagePanel.Visibility = Visibility.Visible;
                }
                else
                {
                    DetailLineagePanel.Visibility = Visibility.Collapsed;
                }

                if (!string.IsNullOrEmpty(culture.NameOrderConvention))
                {
                    DetailNameOrderConventionValue.Text = $"{GetNameOrderConventionDisplay(culture.NameOrderConvention)}  ({culture.NameOrderConvention})";
                    DetailNameOrderConventionPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    DetailNameOrderConventionPanel.Visibility = Visibility.Collapsed;
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
                DlcTraditionsList.ItemsSource = null;
                DetailDlcTraditionsPanel.Visibility = Visibility.Collapsed;
                DetailHistoryLocPanel.Visibility = Visibility.Collapsed;
                DetailLineagePanel.Visibility = Visibility.Collapsed;
                DetailNameOrderConventionPanel.Visibility = Visibility.Collapsed;
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

        // ---------- Heritage management ----------

        private void RefreshHeritageList()
        {
            if (HeritageList == null) return;
            string? selectedName = (HeritageList.SelectedItem as HeritageInfo)?.Name;
            var items = _editorHeritageDefs.Values
                .OrderBy(d => d.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
HeritageList.ItemsSource = items;
// Ensure DisplayName is properly set from localization (after async localization finished)
foreach (var heritage in items)
{
    if (_editorLocalization != null && _editorLocalization.TryGetValue($"{heritage.Name}_name", out var ln))
        heritage.DisplayName = ln;
}
if (selectedName != null)
{
    var match = items.FirstOrDefault(h => string.Equals(h.Name, selectedName, StringComparison.OrdinalIgnoreCase));
    if (match != null)
        HeritageList.SelectedItem = match;
}
else if (items.Count > 0)
{
    HeritageList.SelectedIndex = 0;
}
        }

        private void RefreshHeritageAudioOptions()
        {
            if (HeritageAudioParameter == null) return;
            string? current = GetSelectedOption(HeritageAudioParameter);
            PopulateEditorCombo(HeritageAudioParameter, GetAudioParameterOptions(), current ?? "");
            if (current == null && _editorHeritage == null && !_editorHeritageIsNew)
            {
                if (HeritageAudioParameter.Items.Count > 1)
                    HeritageAudioParameter.SelectedIndex = 1;
            }
        }

        private IEnumerable<(string Key, string Display)> GetAudioParameterOptions()
        {
            var values = _editorHeritageDefs.Values
                .Select(h => h.AudioParameter)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var v in values)
                yield return (v, v);
            foreach (var fallback in HeritageAudioFallback)
            {
                if (!values.Contains(fallback, StringComparer.OrdinalIgnoreCase))
                    yield return (fallback, fallback);
            }
        }

        private void HeritageNew_Click(object sender, RoutedEventArgs e)
        {
            if (HeritageId != null) HeritageId.Text = "";
            if (HeritageLocName != null) HeritageLocName.Text = "";
            if (HeritageLocCollective != null) HeritageLocCollective.Text = "";
            if (HeritageAudioParameter != null)
            {
                if (HeritageAudioParameter.Items.Count > 1)
                    HeritageAudioParameter.SelectedIndex = 1;
                else
                    HeritageAudioParameter.SelectedIndex = -1;
            }
            _editorHeritage = null;
            _editorHeritageIsNew = true;
            _heritageHasSavedState = false;
            _savedHeritageLocName = "";
            _savedHeritageLocCollective = "";
            UpdateHeritageModeUi();
            if (HeritageStatusText != null)
                HeritageStatusText.Text = Res("CulturesTab_EditorHint");
        }

        private void HeritageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var heritage = HeritageList.SelectedItem as HeritageInfo;
            if (heritage == null)
            {
                _editorHeritage = null;
                _editorHeritageIsNew = false;
                _heritageHasSavedState = false;
                UpdateHeritageModeUi();
                return;
            }

            _editorHeritage = heritage;
            _editorHeritageIsNew = false;
            if (HeritageId != null) HeritageId.Text = heritage.Name.Replace("heritage_", "", StringComparison.OrdinalIgnoreCase);
            if (HeritageAudioParameter != null)
                PopulateEditorCombo(HeritageAudioParameter, GetAudioParameterOptions(), heritage.AudioParameter);
            if (HeritageLocName != null) HeritageLocName.Text = LookupLocalizationValue($"{heritage.Name}_name") ?? "";
            if (HeritageLocCollective != null) HeritageLocCollective.Text = LookupLocalizationValue($"{heritage.Name}_collective_noun") ?? "";
            _heritageHasSavedState = true;
            _savedHeritageLocName = HeritageLocName?.Text?.Trim() ?? "";
            _savedHeritageLocCollective = HeritageLocCollective?.Text?.Trim() ?? "";
            UpdateHeritageModeUi();

            if (!heritage.IsModNew && HeritageStatusText != null)
                HeritageStatusText.Text = Res("CulturesTab_HeritageReadOnly");
        }

        private void UpdateHeritageModeUi()
        {
            bool isNew = _editorHeritageIsNew && _editorHeritage == null;
            if (HeritageId != null)
            {
                HeritageId.IsReadOnly = !isNew;
                HeritageIdRow.Visibility = isNew ? Visibility.Visible : Visibility.Collapsed;
            }
            if (HeritageFileNameRow != null)
                HeritageFileNameRow.Visibility = isNew ? Visibility.Visible : Visibility.Collapsed;
            if (HeritageSaveButton != null)
                HeritageSaveButton.IsEnabled = isNew || (_editorHeritage != null && _editorHeritage.IsModNew);
            if (HeritageDeleteButton != null)
                HeritageDeleteButton.IsEnabled = _editorHeritage != null && _editorHeritage.IsModNew;

            if (isNew)
            {
                if (HeritageModeText != null) HeritageModeText.Text = Res("CulturesTab_HeritageNewTitle");
                if (HeritageHintText != null) HeritageHintText.Text = Res("CulturesTab_EditorHint");
                UpdateHeritageDefaultFileName();
            }
            else if (_editorHeritage != null)
            {
                if (HeritageModeText != null) HeritageModeText.Text = $"{Res("CulturesTab_HeritageEditTitle")}: {_editorHeritage.DisplayName}";
                if (HeritageHintText != null) HeritageHintText.Text = Res("CulturesTab_EditorEditHint");
            }
            else
            {
                if (HeritageModeText != null) HeritageModeText.Text = "";
                if (HeritageHintText != null) HeritageHintText.Text = "";
            }
        }

        private void UpdateHeritageDefaultFileName()
        {
            if (HeritageFileName == null) return;
            string profileName = _viewModel?.CurrentProfile?.FileNamePrefixes.TryGetValue("heritage", out var p) == true && !string.IsNullOrEmpty(p)
                ? p!
                : "00_heritage.txt";
            string name = SanitizeFileName(profileName);
            if (!name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                name += ".txt";
            HeritageFileName.Text = name;
        }

        private void HeritageSave_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel?.CurrentProfile == null) return;
            string modRoot = _viewModel.CurrentProfile.ModRoot ?? "";
            if (string.IsNullOrEmpty(modRoot))
            {
                if (HeritageStatusText != null) HeritageStatusText.Text = Res("CulturesTab_EditorNoModRoot");
                return;
            }

            string heritageId = HeritageId?.Text?.Trim() ?? "";
            string heritageKey = $"heritage_{heritageId}";
            if (string.IsNullOrEmpty(heritageId))
            {
                if (HeritageStatusText != null) HeritageStatusText.Text = Res("CulturesTab_HeritageNeedId");
                return;
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(heritageId, @"^[a-zA-Z0-9_]+$"))
            {
                if (HeritageStatusText != null) HeritageStatusText.Text = Res("CulturesTab_HeritageIdInvalid");
                return;
            }

            string audio = GetSelectedOption(HeritageAudioParameter);
            if (string.IsNullOrEmpty(audio))
            {
                if (HeritageStatusText != null) HeritageStatusText.Text = Res("CulturesTab_HeritageNeedAudio");
                return;
            }

            string locName = HeritageLocName?.Text?.Trim() ?? "";
            string locCollective = HeritageLocCollective?.Text?.Trim() ?? "";

            bool nameChanged = _heritageHasSavedState ? locName != _savedHeritageLocName : !string.IsNullOrEmpty(locName);
            bool collectiveChanged = _heritageHasSavedState ? locCollective != _savedHeritageLocCollective : !string.IsNullOrEmpty(locCollective);

            if (_heritageHasSavedState)
            {
                var blankFields = new List<string>();
                if (!string.IsNullOrEmpty(_savedHeritageLocName) && string.IsNullOrEmpty(locName))
                    blankFields.Add(Res("CulturesTab_EditorLocName"));
                if (!string.IsNullOrEmpty(_savedHeritageLocCollective) && string.IsNullOrEmpty(locCollective))
                    blankFields.Add(Res("CulturesTab_EditorLocCollective"));
                if (blankFields.Count > 0)
                {
                    if (HeritageStatusText != null)
                        HeritageStatusText.Text = string.Format(Res("CulturesTab_EditorLocBlank"), string.Join(", ", blankFields));
                    return;
                }
            }

            if (_editorHeritageIsNew)
            {
                if (_editorHeritageDefs.ContainsKey(heritageKey))
                {
                    if (HeritageStatusText != null) HeritageStatusText.Text = string.Format(Res("CulturesTab_HeritageExists"), heritageId);
                    return;
                }
                SaveAsNewHeritage(heritageKey, BuildHeritageBlock(heritageKey, audio),
                    (hk) => SaveHeritageLocalizationAsync(hk, nameChanged, locName, collectiveChanged, locCollective));
            }
            else if (_editorHeritage != null)
            {
                SaveExistingHeritage(BuildHeritageBlock(_editorHeritage.Name, audio),
                    (hk) => SaveHeritageLocalizationAsync(hk, nameChanged, locName, collectiveChanged, locCollective));
            }
        }

        private static string BuildHeritageBlock(string heritageKey, string audio)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{heritageKey} = {{");
            sb.AppendLine("\ttype = heritage");
            sb.AppendLine("\tis_shown = {");
            sb.AppendLine("\t\theritage_is_shown_trigger = {");
            sb.AppendLine($"\t\t\tHERITAGE = {heritageKey}");
            sb.AppendLine("\t\t}");
            sb.AppendLine("\t}");
            sb.AppendLine($"\taudio_parameter = {audio}");
            sb.Append("}");
            return sb.ToString();
        }

        private void SaveAsNewHeritage(string heritageKey, string block, Func<string, Task> afterSave)
        {
            var profile = _viewModel?.CurrentProfile;
            if (profile == null) return;
            string modRoot = profile.ModRoot ?? "";
            if (string.IsNullOrEmpty(modRoot))
            {
                if (HeritageStatusText != null) HeritageStatusText.Text = Res("CulturesTab_EditorNoModRoot");
                return;
            }

            string folder = Path.Combine(modRoot, "common", "culture", "pillars", "mod");
            try
            {
                Directory.CreateDirectory(folder);
            }
            catch
            {
                if (HeritageStatusText != null) HeritageStatusText.Text = Res("CulturesTab_EditorFolderInvalid");
                return;
            }

            if (HeritageExistsInMod(modRoot, heritageKey, out _))
            {
                if (HeritageStatusText != null) HeritageStatusText.Text = string.Format(Res("CulturesTab_HeritageExists"), heritageKey.Replace("heritage_", "", StringComparison.OrdinalIgnoreCase));
                return;
            }

            string enteredName = HeritageFileName?.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(enteredName) && !enteredName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                if (HeritageStatusText != null) HeritageStatusText.Text = Res("CulturesTab_EditorFileNameInvalid");
                return;
            }
            string fileName = string.IsNullOrEmpty(enteredName) ? GetHeritageDefaultFileName() : enteredName;
            fileName = SanitizeFileName(fileName);
            if (!fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                fileName += ".txt";

            string fullPath = Path.Combine(folder, fileName);

            try
            {
                if (File.Exists(fullPath))
                {
                    InsertHeritageIntoFileAlphabetically(fullPath, heritageKey, block);
                    if (HeritageStatusText != null) HeritageStatusText.Text = string.Format(Res("CulturesTab_EditorAddedToFile"), fileName);
                }
                else
                {
                    File.WriteAllText(fullPath, block, new UTF8Encoding(true));
                    if (HeritageStatusText != null) HeritageStatusText.Text = string.Format(Res("CulturesTab_EditorSaved"), fileName);
                }
                MarkHeritageAsSaved();
                _ = afterSave(heritageKey);
                RefreshAfterHeritageChange(heritageKey);
            }
            catch (Exception ex)
            {
                if (HeritageStatusText != null) HeritageStatusText.Text = $"{Res("CulturesTab_EditorSaveError")}: {ex.Message}";
            }
        }

        private void SaveExistingHeritage(string block, Func<string, Task> afterSave)
        {
            var heritage = _editorHeritage;
            if (heritage == null || !heritage.IsModNew || string.IsNullOrEmpty(heritage.SourceFile) || !File.Exists(heritage.SourceFile))
            {
                if (HeritageStatusText != null) HeritageStatusText.Text = Res("CulturesTab_HeritageReadOnly");
                return;
            }

            try
            {
                ReplaceHeritageInFile(heritage.SourceFile, heritage.Name, block);
                if (HeritageStatusText != null) HeritageStatusText.Text = string.Format(Res("CulturesTab_EditorSaved"), Path.GetFileName(heritage.SourceFile));
                MarkHeritageAsSaved();
                _ = afterSave(heritage.Name);
                RefreshAfterHeritageChange(heritage.Name);
            }
            catch (Exception ex)
            {
                if (HeritageStatusText != null) HeritageStatusText.Text = $"{Res("CulturesTab_EditorSaveError")}: {ex.Message}";
            }
        }

        private void MarkHeritageAsSaved()
        {
            _editorHeritageIsNew = false;
            _heritageHasSavedState = true;
            _savedHeritageLocName = HeritageLocName?.Text?.Trim() ?? "";
            _savedHeritageLocCollective = HeritageLocCollective?.Text?.Trim() ?? "";
        }

        private string GetHeritageDefaultFileName()
        {
            string profileName = _viewModel?.CurrentProfile?.FileNamePrefixes.TryGetValue("heritage", out var p) == true && !string.IsNullOrEmpty(p)
                ? p!
                : "00_heritage.txt";
            string name = SanitizeFileName(profileName);
            if (!name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                name += ".txt";
            return name;
        }

        private void RefreshAfterHeritageChange(string? selectKey = null)
        {
            if (HeritageList == null) return;
            var profile = _viewModel?.CurrentProfile;
            if (profile != null)
            {
                string appLang = _viewModel?.Language ?? "en";
                _editorLocalization = LoadLocalization(profile.GameRoot ?? "", profile.ModRoot ?? "", appLang);
                _editorHeritageDefs = LoadHeritageDefinitions(profile.GameRoot ?? "", profile.ModRoot ?? "", _baseHeritageRawKeys);
                foreach (var heritage in _editorHeritageDefs.Values)
                {
                    if (_editorLocalization != null && _editorLocalization.TryGetValue($"{heritage.Name}_name", out var heritageName))
                        heritage.DisplayName = heritageName;
                }
            }
            RefreshCultureTree();
            if (EditorHeritage != null)
                PopulateEditorCombo(EditorHeritage, GetHeritageOptions(), GetSelectedOption(EditorHeritage));
            RefreshHeritageAudioOptions();
            if (!string.IsNullOrEmpty(selectKey) && _editorHeritageDefs.TryGetValue(selectKey, out var updated))
                _editorHeritage = updated;
            else if (_editorHeritage != null)
                _editorHeritage = _editorHeritageDefs.TryGetValue(_editorHeritage.Name, out var match) ? match : null;
            RefreshHeritageList();
            UpdateHeritageModeUi();
        }

        private void HeritageDelete_Click(object sender, RoutedEventArgs e)
        {
            var heritage = _editorHeritage;
            if (heritage == null) return;
            if (!heritage.IsModNew || string.IsNullOrEmpty(heritage.SourceFile))
            {
                if (HeritageStatusText != null) HeritageStatusText.Text = Res("CulturesTab_HeritageDeleteNotAllowed");
                return;
            }

            string display = heritage.DisplayName ?? heritage.Name ?? "";
            if (System.Windows.MessageBox.Show(string.Format(Res("CulturesTab_HeritageDeleteConfirm"), display),
                                display, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes)
                return;

            try
            {
                string filePath = heritage.SourceFile;
                if (!DeleteHeritageBlockFromFile(filePath, heritage.Name!))
                {
                    if (HeritageStatusText != null) HeritageStatusText.Text = Res("CulturesTab_EditorSaveError");
                    return;
                }
                if (CountHeritageBlocks(filePath) == 0)
                    File.Delete(filePath);

                bool existsInBase = _baseHeritageRawKeys.Contains(heritage.Name!);
                DeleteHeritageLocalization(_viewModel?.CurrentProfile?.ModRoot ?? "", heritage.Name!, existsInBase);

                if (HeritageStatusText != null) HeritageStatusText.Text = string.Format(Res("CulturesTab_HeritageDeleted"), display);
                _editorHeritage = null;
                _editorHeritageIsNew = false;
                _heritageHasSavedState = false;
                ResetHeritageForm();
                RefreshAfterHeritageChange();
            }
            catch (Exception ex)
            {
                if (HeritageStatusText != null) HeritageStatusText.Text = $"{Res("CulturesTab_EditorSaveError")}: {ex.Message}";
            }
        }

        private void ResetHeritageForm()
        {
            if (HeritageId != null) HeritageId.Text = "";
            if (HeritageLocName != null) HeritageLocName.Text = "";
            if (HeritageLocCollective != null) HeritageLocCollective.Text = "";
            if (HeritageAudioParameter != null && HeritageAudioParameter.Items.Count > 1)
                HeritageAudioParameter.SelectedIndex = 1;
            UpdateHeritageModeUi();
        }

        private static bool HeritageExistsInMod(string modRoot, string heritageKey, out string? filePath)
        {
            filePath = null;
            string folder = Path.Combine(modRoot, "common", "culture", "pillars", "mod");
            if (!Directory.Exists(folder)) return false;

            foreach (var file in Directory.EnumerateFiles(folder, "*.txt", SearchOption.AllDirectories))
            {
                if (HeritageBlockExistsInFile(file, heritageKey))
                {
                    filePath = file;
                    return true;
                }
            }
            return false;
        }

        private static bool HeritageBlockExistsInFile(string filePath, string heritageKey)
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
                if (string.Equals(key, heritageKey, StringComparison.OrdinalIgnoreCase))
                    return true;
                pos++;
                ReadBlock(text, ref pos);
            }
            return false;
        }

        private static void InsertHeritageIntoFileAlphabetically(string filePath, string heritageKey, string block)
        {
            var text = File.ReadAllText(filePath);
            var keys = new List<string>();
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
                keys.Add(key);
                positions.Add(keyStart);
            }

            int insertIndex = 0;
            while (insertIndex < keys.Count &&
                   string.Compare(keys[insertIndex], heritageKey, StringComparison.OrdinalIgnoreCase) < 0)
            {
                insertIndex++;
            }

            var blockText = block.TrimEnd('\n', '\r');
            string newText;
            if (insertIndex >= keys.Count)
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

            File.WriteAllText(filePath, newText, new UTF8Encoding(true));
        }

        private static void ReplaceHeritageInFile(string filePath, string heritageKey, string newBlock)
        {
            string text = File.ReadAllText(filePath);
            string marker = $"{heritageKey} = {{";
            int idx = text.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0)
                throw new InvalidOperationException("Heritage block not found");

            int bracket = text.IndexOf('{', idx);
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
                throw new InvalidOperationException("Heritage block not found");

            var trimmedBlock = newBlock.TrimEnd();
            string newText = text.Substring(0, idx)
                + trimmedBlock
                + text.Substring(end + 1);
            File.WriteAllText(filePath, newText, new UTF8Encoding(true));
        }

        private static bool DeleteHeritageBlockFromFile(string filePath, string heritageKey)
        {
            string text = File.ReadAllText(filePath);
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

                if (string.Equals(key, heritageKey, StringComparison.OrdinalIgnoreCase))
                {
                    int entryEnd = pos;
                    string newText = text.Remove(keyStart, entryEnd - keyStart);
                    File.WriteAllText(filePath, newText, new UTF8Encoding(true));
                    return true;
                }
            }
            return false;
        }

        private static int CountHeritageBlocks(string filePath)
        {
            string text = File.ReadAllText(filePath);
            int pos = 0;
            int count = 0;
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
                pos++;
                ReadBlock(text, ref pos);
                count++;
            }
            return count;
        }

        private static void DeleteHeritageLocalization(string modRoot, string heritageKey, bool existsInBase)
        {
            if (string.IsNullOrEmpty(modRoot)) return;
            var keysToRemove = new[] { $"{heritageKey}_name", $"{heritageKey}_collective_noun" };
            string baseLocPath = Path.Combine(modRoot, "localization");
            if (existsInBase)
                baseLocPath = Path.Combine(baseLocPath, "replace");

            if (!Directory.Exists(baseLocPath)) return;
            foreach (var file in Directory.GetFiles(baseLocPath, "cultural_heritages_l_*.yml", SearchOption.AllDirectories))
                RemoveLocalizationKeys(file, keysToRemove);
        }

        private async Task SaveHeritageLocalizationAsync(string heritageKey, bool nameChanged, string name, bool collectiveChanged, string collective)
        {
            var profile = _viewModel?.CurrentProfile;
            if (profile == null) return;
            string modRoot = profile.ModRoot ?? "";
            if (string.IsNullOrEmpty(modRoot)) return;

            if (!nameChanged && !collectiveChanged) return;

            bool autoTranslate = _viewModel?.AutoTranslate ?? true;
            if (HeritageStatusText != null)
                HeritageStatusText.Text = autoTranslate ? Res("CulturesTab_EditorLocTranslating") : Res("CulturesTab_EditorLocWriting");
            SetEditorBusy(true);
            try
            {
            string appLang = _viewModel?.Language ?? "en";
            string? directFolder = appLang switch
            {
                "es" => "spanish",
                "en" => "english",
                _ => null
            };

            bool existsInBase = _baseHeritageRawKeys.Contains(heritageKey);
            string baseLocPath = Path.Combine(modRoot, "localization");
            if (existsInBase)
                baseLocPath = Path.Combine(baseLocPath, "replace");

            string srcCode = appLang.ToLowerInvariant() switch { "es" => "es", "en" => "en", _ => "ca" };

            var providers = autoTranslate ? BuildEnabledProviders() : new List<ITranslationProvider>();

            List<(string Folder, string Code)> targets;
            if (autoTranslate)
                targets = GameSupportedLanguages.Select(f => (f.Folder, f.Code)).ToList();
            else if (directFolder != null)
                targets = new List<(string Folder, string Code)> { (directFolder, srcCode) };
            else
                targets = new List<(string Folder, string Code)>();

            int saved = 0;
            var errors = new List<string>();
            var fallbackLangs = new List<string>();
            foreach (var (ck3Folder, code) in targets)
            {
                string locName = name;
                string locCollective = collective;

                bool usedFallback = false;
                if (autoTranslate && ck3Folder != directFolder)
                {
                    if (nameChanged)
                    {
                        var (trName, okName) = await TranslateWithFallbackAsync(name, srcCode, code, providers);
                        locName = string.IsNullOrEmpty(trName) ? name : trName;
                        usedFallback |= !okName;
                    }
                    if (collectiveChanged)
                    {
                        var (trCollective, okCollective) = await TranslateWithFallbackAsync(collective, srcCode, code, providers);
                        locCollective = string.IsNullOrEmpty(trCollective) ? collective : trCollective;
                        usedFallback |= !okCollective;
                    }
                }

                string folderPath = Path.Combine(baseLocPath, ck3Folder);
                try
                {
                    string traditionsDir = Path.Combine(folderPath, "culture", "traditions");
                    Directory.CreateDirectory(traditionsDir);
                    string filePath = Path.Combine(traditionsDir, $"cultural_heritages_l_{ck3Folder}.yml");
                    var entries = new List<(string Key, string Value)>();
                    if (nameChanged && !string.IsNullOrEmpty(locName))
                        entries.Add(($"{heritageKey}_name", locName));
                    if (collectiveChanged && !string.IsNullOrEmpty(locCollective))
                        entries.Add(($"{heritageKey}_collective_noun", locCollective));
                    if (entries.Count > 0)
                        UpsertLocalizationFile(filePath, $"l_{ck3Folder}:", entries);

                    saved++;
                    if (usedFallback)
                        fallbackLangs.Add(ck3Folder);
                }
                catch
                {
                    errors.Add(ck3Folder);
                }
            }

            if (HeritageStatusText == null) return;
            if (errors.Count > 0)
            {
                HeritageStatusText.Text = $"{string.Format(Res("CulturesTab_EditorLocSaved"), saved)} {Res("CulturesTab_EditorLocError")}: {string.Join(", ", errors)}";
            }
            else if (fallbackLangs.Count > 0)
            {
                HeritageStatusText.Text = $"{string.Format(Res("CulturesTab_EditorLocSaved"), saved)} {string.Format(Res("CulturesTab_EditorLocFallback"), string.Join(", ", fallbackLangs))}";
            }
            else if (saved == 0)
            {
                HeritageStatusText.Text = Res("CulturesTab_EditorLocDisabled");
            }
            else
            {
                HeritageStatusText.Text = string.Format(Res("CulturesTab_EditorLocSaved"), saved);
            }
            }
            finally
            {
                SetEditorBusy(false);
                RefreshAfterHeritageChange(heritageKey);
            }
        }
    }
}
