# PROJECT_CONTEXT.md - PdxModIDE

> Auto-generated technical context document. Do not edit manually except for major updates.

---

## 1. Overview

**PdxModIDE** — Desktop IDE (WPF, .NET 8, C#) for creating and managing mods for Paradox Interactive games (CK3, EU4, etc.).

**Main function**: Automate copying files from the base game to the mod directory applying a **year offset** to all found dates (regex per game), with diff game/mod/backup validation, map rendering, and profile management.

**Main Stack**:
- **.NET 8** / C# 12 / WPF (XAML + code-behind + manual ViewModels)
- **SkiaSharp** (map rendering, LUT, palettes)
- **System.Text.Json** (JSON persistence in `data/`)
- **Parallel / Task** (module processing, validation, map loading)
- **No DI container** (manual instantiation in `ProjectManager`)

**Current version**: 1.7.1 (see the `Changelog/` folder, one file per minor version). Solution: `PdxModIDE.sln` (9 projects).

---

## 2. Architecture

### 2.1 Project Structure (Solution)

```
PdxModIDE.sln
├── PdxModIDE.Domain          # Pure entities (Module, GameFile, Profile, EditingSession)
├── PdxModIDE.Data            # Repositories + DataLoader (JSON) + configs (ModuleConfig, FileConfig, Settings, LogFilters)
├── PdxModIDE.IO              # FS utilities (FileOperations, Paths)
├── PdxModIDE.Core            # Core logic: ModuleProcessor, DefinesProcessor, GameRegistry, IGamePlugin, CK3GamePlugin
├── PdxModIDE.MapEngine       # MapLoader, TitleHistoryLoader, ProvinceInfo, LUT cache
├── PdxModIDE.Rendering       # MapRenderer (SkiaSharp viewport, zoom, pan, tooltips)
├── PdxModIDE.Project         # IProjectService + ProjectManager (main orchestrator)
├── PdxModIDE.Validation      # ModuleValidator (recursive diff, byte/line comparison)
└── PdxModIDE.UI              # WPF App, MainWindow, ViewModels, Tabs, Themes, Dialogs
```

### 2.2 Project Dependencies

```
PdxModIDE.UI
    └── PdxModIDE.Project (IProjectService)
            ├── PdxModIDE.Core (ModuleProcessor, DefinesProcessor, GameRegistry)
            │       ├── PdxModIDE.Domain
            │       ├── PdxModIDE.Data (ModuleRepository)
            │       └── PdxModIDE.IO
            ├── PdxModIDE.MapEngine (MapLoader, TitleHistoryLoader)
            │       └── PdxModIDE.Domain (ProvinceInfo)
            ├── PdxModIDE.Rendering (MapRenderer)
            │       └── PdxModIDE.MapEngine
            └── PdxModIDE.Validation (ModuleValidator)
                    └── PdxModIDE.Domain
```

> **Note**: No automatic dependency injection. `ProjectManager` creates `new ModuleProcessor(new ModuleRepository())` in constructor.

### 2.3 Main Data Flow (Process Modules)

```
MainViewModel.ProcessModulesCommand
    → ProjectManager.ProcessModulesAsync(offsetOverride)
        → ModuleProcessor.ProcessModulesAsync(gameKey, modules, gameRoot, modRoot, backupRoot, offset, profileName)
            → Parallel.ForEach(moduleNames) → ProcessModule(...)
                → IGamePlugin.DateRegex.Replace(text, match => year+offset)
                → FileOperations.CopyFilePreserveTimestamps / WriteAllText
                → Log per module in logs/{profile}/{module}.log
```

**Synchronization**: `ModuleProcessor` caches modules in `_moduleCache` (thread-safe with `lock`). `InvalidateCache()` clears the cache.

---

## 3. Main Dependencies (NuGet)

| Project | Package | Version | Usage |
|---------|---------|---------|-------|
| `PdxModIDE.UI` | `SkiaSharp` / `SkiaSharp.Views.WPF` | 3.116.1 | Map render, LUT, palettes |
| `PdxModIDE.MapEngine` | `SkiaSharp` | 3.116.1 | Decode provinces.png, build LUT bitmap |
| `PdxModIDE.Core` | `Microsoft.Extensions.Logging.Abstractions` | 8.x | (Optional) abstracted logging |
| All | `System.Text.Json` | Built-in | Serialization `data/*.json` |
| `PdxModIDE.UI` | `Microsoft.Xaml.Behaviors.Wpf` | 1.1.x | (If used) XAML behaviors |

> `Directory.Build.props` centralizes `<TargetFramework>net8.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`.

---

## 4. Data Model

### 4.1 Domain Entities (`PdxModIDE.Domain.Models`)

| Class | Key Properties | Notes |
|-------|----------------|-------|
| `Module` | `Name`, `Path`, `IgnoreExtensions (IReadOnlyList<string>)` | Immutable (ctor only) |
| `GameFile` | `Name`, `Path`, `MapTo?` | `MapTo` allows mapping game path → different mod path |
| `Profile` | `Id (Guid)`, `Name`, `Game`, `GameRoot`, `ModRoot`, `BackupRoot`, `YearOffset`, `ModuleIds[]`, `FileIds[]`, `SelectedModules`, `SelectedFiles`, `FileNamePrefixes` | `Selected*` resolved in `EditingSession`; `FileNamePrefixes` stores per-file naming prefixes (e.g. `culture`) |
| `EditingSession` | `CurrentProfile`, `ModulesByGame`, `FilesByGame`, `AllModulesByName`, `AllFilesByName` | Built in `ProjectManager.BuildSession`; resolves `ModuleIds`→`Module` references |

### 4.2 Persistence Configs (`PdxModIDE.Data`)

| Class | JSON File | Description |
|-------|-----------|-------------|
| `DataProfile` | `data/profiles.json` | 1:1 mapping to `Domain.Profile` + serialization |
| `ModuleConfig` | `data/modules.json` | `{ Path, IgnoreExt[] }` per `gameKey → moduleName` |
| `FileConfig` | `data/files.json` | `{ Path, MapTo? }` per `gameKey → fileKey` |
| `Settings` | `data/settings.json` | `{ Theme, Language, TranslationProviders[], DeeplApiKey, TranslationProviderUrls{} }` |
| `LogFilters` | `data/logfilters.json` | Log filters per profile (not actively used) |

**ID Convention**: `moduleName` = key in JSON = relative folder name (e.g. `common/landed_titles`). `fileKey` = logical name (e.g. `defines`).

### 4.3 `data/` File Structure

```
data/
├── profiles.json       # List<DataProfile>
├── modules.json        # Dict<gameKey, Dict<moduleName, ModuleConfig>>
├── files.json          # Dict<gameKey, Dict<fileKey, FileConfig>>
├── settings.json       # Settings { Theme, Language, TranslationProviders, DeeplApiKey, TranslationProviderUrls }
└── logfilters.json     # LogFilters { ProfileFilters[] }
```

---

## 5. Key Modules and Components

### 5.1 `ModuleProcessor` (`PdxModIDE.Core`)

**Responsibility**: Recursive game→mod copy applying date offset.

```csharp
public void ProcessModule(string gameKey, string moduleName, 
    string gameRoot, string modRoot, string backupRoot, int offset, string profileName)
```

- Uses `IGamePlugin.DateRegex` (e.g. CK3: `\b(\d{1,4})\.(\d{1,2})\.(\d{1,2})\b`).
- `IGamePlugin.IsDateProcessableExtension(ext)` filters extensions (`.txt`, `.csv`, `.yml`).
- Pre-backup to `backupRoot/{relPath}` (preserves timestamps).
- Log per module: `logs/{profileName}/{moduleName}.log` (append).
- Parallelism: `Parallel.ForEach` with `MaxDegreeOfParallelism = Environment.ProcessorCount`.

**Key methods**:
- `ApplyOffset(string text, int offset, IGamePlugin)` → regex replace.
- `ProcessModulesAsync` → Task wrapper for UI async.

### 5.2 `DefinesProcessor` (`PdxModIDE.Core`)

**Responsibility**: Read/write `end_date` in `defines.txt`.

```csharp
ReadEndDate(gameRoot, gameKey)        // searches defines.txt in gameRoot
ReadModEndDate(modRoot, gameKey)      // searches in modRoot
WriteEndDate(gameRoot, modRoot, backupRoot, newDate, gameKey)
```

- Auto-backup before writing.
- Uses `IGamePlugin.GetDefinesPath()` → relative (e.g. `game/defines.txt`).
- Regex: `end_date\s*=\s*(\d{4})\.(\d{2})\.(\d{2})`.

### 5.3 `GameRegistry` + `IGamePlugin` (`PdxModIDE.Core.Games`)

**Pattern**: Plugin per game. Static registration `GameRegistry.Register(plugin)`.

```csharp
interface IGamePlugin {
    string GameKey { get; }
    string DisplayName { get; }
    Regex DateRegex { get; }
    bool IsDateProcessableExtension(string ext);
    string GetDefinesPath();
    bool CanHandleGame(string gameRoot);  // automatic detection
}
```

**Implemented**: `CK3GamePlugin` (`GameKey="CK3"`).
- `DateRegex`: `\b(\d{1,4})\.(\d{1,2})\.(\d{1,2})\b`
- `ProcessableExt`: `.txt`, `.csv`, `.yml`, `.lua`
- `DefinesPath`: `game/defines.txt`
- `CanHandleGame`: searches for `game/defines.txt` with `end_date` or CK3 `game_version`.

**Detection**: `GameRegistry.DetectGame(gameRoot)` iterates plugins sorted by key length descending.

### 5.4 `MapLoader` (`PdxModIDE.MapEngine`)

**Full CK3 map loading**:

| Step | File | Output |
|------|------|--------|
| `LoadDefinition` | `definition.csv` | `ProvincesById`, `ProvincesByColor`, `ProvinceToBarony` |
| `LoadDefaultMap` | `default.map` | `Sea`, `Lakes`, `Rivers`, `Impassable`, `ImpassableSeas` (HashSet<int>) |
| `LoadLandedTitles` | `common/landed_titles/*.txt` | `ProvinceToBarony`, `BaronyToCounty`, `CountyToDuchy`, `DuchyToKingdom`, `KingdomToEmpire` |
| `MarkTerrainTypes` | — | `ProvinceInfo.Type` ∈ {sea, lake, river, impassable, land, unknown} |
| `BuildOrLoadLut` | — | `Lut[16_777_216] byte` (MD5 cache definition.csv + default.map) |
| `BuildPixelData` | `provinces.png/bmp` | `ProvinceIdMap[int[]]` (w*h), `MapWidth`, `MapHeight` |

**LUT Cache**: `%LocalAppData%/PdxModIDE/lut_cache/{lut_types.bin, lut_meta.json}`. MD5 hash of sources.

**TitleHistoryLoader**: Parses `history/titles/*.txt` → `TitleHistory { Holders: SortedList<int, string> }` (year → holder). Used by `MapLoader.BuildHolderLut(year, history, out indexToHolder)`.

**County Mode**: `BuildCountyLut(out indexToCounty)` (no year parameter, borders don't change) maps province → barony (`ProvinceToBarony`) → county (`BaronyToCounty`). Generates 16M entry LUT coloring by county; uses `ushort[]` LUT (16-bit) supporting up to 65535 unique entries without wrap-around.

**Duchy/Kingdom/Empire Modes**: New methods `BuildDuchyLut`, `BuildKingdomLut`, `BuildEmpireLut` use the full hierarchy `CountyToDuchy` → `DuchyToKingdom` → `KingdomToEmpire` to color by each level. In the Map tab: mutually exclusive checkboxes (Tit./Cty./Dch./Kgd./Emp.) with tooltips.

**Culture Mode** (`CultureLoader`): `LoadCultures` parses culture definitions from `common/culture/cultures/*.txt` and resolves `color = <name>` references against `common/named_colors/*.txt` (game + mod roots). Title history (`history/titles/*.txt`) and character history (`history/characters/*.txt`) accept both numeric and string character IDs (eastern content: China/Japan/Korea), so provinces without a direct `culture =` inherit the county holder's culture correctly.

### 5.5 `ModuleValidator` (`PdxModIDE.Validation`)

**Non-recursive three-way diff** (top-level files only): Mod vs Backup, Game vs Backup, Game vs Mod.

```csharp
ValidateModuleSingle(moduleName, ComparisonType) → List<FileComparisonResult>
ValidateAllAsync() → List<ModuleValidationResult> (parallel)
```

`FileComparisonResult`: `{ RelativePath, Status (Equal/Modified/Added/Deleted), DiffLines? }`.
Line-by-line diff with bidirectional lookahead (up to 20 lines) for proper interleaving of additions and removals.

**IgnoreExt**: Configurable per module (`ModuleConfig.IgnoreExt`).

### 5.6 `ProjectManager` (`PdxModIDE.Project`)

**Main orchestrator** — implements `IProjectService`.

**State**:
- `_dataProfiles`, `_dataModules`, `_dataFiles`, `_dataSettings`, `_dataLogFilters` (JSON cache)
- `_domainProfiles`, `CurrentDataProfile`, `CurrentProfile`, `CurrentSession` (`EditingSession`)

**Key methods**:
| Method | Description |
|--------|-------------|
| `Load()` | Loads all JSON + `SyncDomainProfiles()` |
| `SaveAll()` | Persists all JSON |
| `SelectProfile(name)` | Changes active profile + `BuildSession` |
| `CreateProfile(name, game)` | New profile + persistence |
| `CreateProfileWithGameDetection(name, gameRoot)` | Detects game + creates |
| `ProcessModulesAsync(offset?)` | Delegates to `ModuleProcessor` |
| `ValidateAllAsync()` | Delegates to `ModuleValidator` (parallel) |
| `FindDateModules()` | Scans game root recursively for unconfigured folders containing date files (informational only) |
| `GetGameModules(gameKey)` | `ModuleConfig` dict |
| `GetAllModules()` | `Domain.Module` nested read-only dict |

**BuildSession**: Constructs `EditingSession` resolving `ModuleIds`/`FileIds` → actual `Module`/`GameFile` objects.

### 5.7 `DataLoader` (`PdxModIDE.Data`)

**Generic JSON Load/Save**:

```csharp
static T Load<T>(string file, T defaultValue)
static void Save<T>(string file, T data)
```

Files in `data/` (creates directory if it doesn't exist). `JsonSerializerOptions: WriteIndented=true`.

### 5.8 UI — `MainViewModel` + Tabs (`PdxModIDE.UI.ViewModels`)

**MainViewModel**: Complete UI state.
- `Profiles: ObservableCollection<ProfileViewModel>`
- `CurrentProfile`, `CurrentSession`
- `GameModules`, `GameFiles` (grouped by game)
- `SelectedModules`, `SelectedFiles` (checkboxes)
- `GameRoot`, `ModRoot`, `BackupRoot`, `YearOffset` (two-way bindings)
- `Theme` (change triggers `ApplyTheme` in `MainWindow`)
- Commands: `ProcessModulesCommand`, `ValidateAllCommand`, `SaveProfileCommand`, `DetectGameCommand`, `Browse*Command`.

**Tabs** (UserControls in `UI/`):
- `ProfileTab`: CRUD profiles, game detection, paths.
- `ModulesTab`: Module list by game, checkbox selection, add/edit/delete module.
- `FilesTab`: File list, checkbox, editable mapTo.
- `DatesTab`: Read end_date game/mod, write new end_date.
- `ValidationTab`: Validate all / individual module / individual file; results grid + side-by-side diff viewer (`DiffViewDialog`, Notepad++ Compare style with "Original"/"Modified" panels, line numbers, and color-coded backgrounds).
- `HistoryTab` (tab "Map", previously two tabs "History (Base)"/"History (Mod)" now unified): Interactive map (SkiaSharp). 5 mutually exclusive modes (checkboxes with tooltips):
  - **Holder** (Tit.): Colors by holder (character) at year in `YearBox` → `BuildHolderLut(year, TitleHistoryLoader)`.
  - **Counties** (Cty.): Colors by county borders (`c_xxx`) → `BuildCountyLut()`.
  - **Duchies** (Dch.): Colors by duchy borders (`d_xxx`) → `BuildDuchyLut()`.
  - **Kingdoms** (Kgd.): Colors by kingdom borders (`k_xxx`) → `BuildKingdomLut()`.
  - **Empires** (Emp.): Colors by empire borders (`e_xxx`) → `BuildEmpireLut()`.
  Click province → info panel shows Barony, County, Duchy, Kingdom, Empire, Holder, Liege according to mode.
  - **Technical note**: overlay is applied on CPU (workaround for `SKShader.CreateImage` bug as child shader). `RenderToBitmap` renders terrain+borders via shader (mode=0), then iterates pixels and applies palette color from the holder LUT. Uses `InvalidateRender()` for cache invalidation.
- `CulturesTab`: Culture browser with TreeView grouped by heritage. Loads culture definitions from `common/culture/cultures/*.txt` recursively (supports subdirectories like `mod/` for mod-added cultures). Parses Clausewitz blocks (handles `hsv { ... }`, comments, nested blocks). Merges mod over base. Displays localized names from CK3 localization files (`cultures_l_*.yml`, `cultural_heritages_l_*.yml`). Selection panel ordered Source, Name, Color, Ethos, Heritage shows the culture source (Base/Mod), name, color (numeric RGB + visual swatch; supports `hsv`/`hsv360`/`rgb` modes and `color = <name>` references resolved against `common/named_colors/*.txt`). The ethos is expandable and shows its parameters (e.g. `character_modifier`, `province_modifier`, `county_modifier`, `culture_modifier`, `parameters`, `ai_will_do`, `desc`) parsed from `common/culture/pillars/*_ethos.txt`. The heritage is also expandable and shows its parameters (`is_shown`, `audio_parameter`, etc.) parsed from `common/culture/pillars/*_heritage.txt`. The language is also expandable and shows its parameters (`is_shown`, `ai_will_do`, `color`, etc.) parsed from `common/culture/pillars/*language.txt` and localized via `cultural_languages_l_*.yml`. The martial custom is also expandable and shows its parameters (`parameters`, `can_pick`, `ai_will_do`, etc.) parsed from `common/culture/pillars/*martial_custom.txt` and localized via `martial_custom_<name>_name` keys in `cultural_traditions_l_*.yml`. The head determination is also expandable and shows its parameters (`head_determination_type`, etc.) parsed from `common/culture/pillars/*head_determination.txt` and localized via `head_determination_l_*.yml`. The traditions are also expandable, each one showing its localized name (`tradition_<name>_name`), its localized description (`tradition_<name>_desc`) and its parameters (`category`, `layers`, `can_pick`, `parameters`, `character_modifier`, `effects`, `cost`, etc.) parsed from `common/culture/traditions/*.txt` (path from `IGamePlugin.TraditionsRelativePath`). All pillar types are loaded with mod files taking priority over the game; each parameter includes a localized explanation (`EthosParam_*_Desc` / `HeritageParam_*_Desc` / `LanguageParam_*_Desc` / `MartialCustomParam_*_Desc` / `HeadDeterminationParam_*_Desc` / `TraditionParam_*_Desc` keys in `CK3.*.xaml`). The detail panel is inside a vertical `ScrollViewer`; the statistics panel sits at the top of the right column and is only shown when no culture is selected. Statistics panel.
- **Ethnicities in the culture detail (Cultures tab)**: the detail panel also shows the culture's ethnicities (from `ethnicities = { <weight> = <ethnicity> ... }`, the last block of the culture definition), each on its own line as `name weight%` (e.g. `caucasian_blond 25%`). `ExtractEthnicitiesAttribute`/`ParseEthnicityEntries` parse the weighted block; weights are shown as percentages as in the game file. Ethnicity IDs have no localization in the game files, so they are shown as-is. Placed at the end of the detail panel, after the Graphics section.
- `LogsTab`: Log filters (not fully implemented).
- `SettingsTab`: Theme, default paths.

- **Building GFX grid (Cultures tab)**: `RenderBuildingGrid` resolves each culture's `building_gfx` keys to `.mesh` files (via `GetBuildingDb` + `ResolveBuildingMeshes` + `GetMeshFileIndex`) and renders them in a `WrapPanel` (`BuildingGfxGrid`), each cell a `Viewport3D` built by `BuildMeshCellViewport`. Each submesh is textured independently: it skips `Collision*` shaders, resolves the diffuse atlas (UV0) and, when the companion `.asset` declares a `_unique` texture (`texture = { file = "..._unique.dds" index = 5 }`), uses UV1 + the unique texture. To reproduce the game's `standard_atlas` shader (`Diffuse.rgb *= Unique`), the diffuse atlas is the base and the unique's average color is applied as a `DiffuseMaterial` tint (70% mix factor). Textures are decoded by `DdsDecoder.Decode` (cached in `_textureDecodeCache`/`_textureBitmapCache`), which now supports BC1–BC5 and BC7 (all 8 modes) plus DX10 (DDS_HEADER_DXT10) formats; `LoadTexture` converts the decoder's RGBA pixel data to BGRA for `BitmapSource.Create` (avoids the R/B swap that made buildings look blue).

- **Clothing grid + deterministic clothing painter (Cultures tab)**: `RenderClothingGrid` resolves each culture's `clothing_gfx` keys to `.mesh` files through the CK3 chain (group → `portrait_modifiers` → `genes` → accessory → entity → `.asset`) and renders each in a `WrapPanel` (`ClothingGfxGrid`), each cell a `Viewport3D` built by `BuildMeshCellViewport`. `PdxClothingPainter.Paint(gameRoot, assetPath, meshPath)` reconstructs the CK3 `portrait_attachment_pattern` garment color offline (the binary `portrait.shader` is not shipped to users): it decodes the base diffuse, the entity `pattern_mask` (RGBA), the variation's 4 colormasks and the 16-wide colour palette, sampling each active mask channel against its own colormask and indexing the palette row 0 (deterministic). Fidelity features: patterns are sampled with the mesh **UV-set 2** (`u1`, rasterized per diffuse texel from the mesh triangles in UV0 space via `BuildXyzMapping`) instead of UV0, and each channel's colormask UV is transformed by its `pattern_layout` (scale/rotation/offset) before sampling. The painted texture is also reused in the double-click `MeshPreview` window so clothing shows its colored pattern everywhere. Decoded pixel data is BGRA.

- **Unit GFX grid resolved by the real CK3 chain (Cultures tab)**: `RenderUnitGrid` + `PdxUnitResolver` resolve each culture's `unit_gfx` tags to `.mesh` files through the CK3 chain: `common/graphical_unit_types/*.txt` (group tags expanded to the concrete `unit_gfx` tags), the `entity_links` blocks (`00_{army,fleet,siege,travel}_entity_links.txt`, with `type`, `graphical_cultures`, `quality` and `entity`) and the unit `.asset` (`pdxmesh` + `meshsettings`) → `.mesh` + diffuse texture. The grid renders each resolved mesh (army/fleet/siege/travel) in a `WrapPanel` (`UnitGfxGrid`), each cell a `Viewport3D` built by `BuildMeshCellViewport`. The `quality` field resolves file-level macros (`@tier2_quality`, `@tier3_quality`). If the resolver finds no meshes it falls back to the previous folder-prefix grid (`RenderGfxItemGrid`, kind "unit").

- **Lazy-loaded, independent model sections (Cultures tab)**: the Building, Clothing and Unit grids each live in their own `Expander` (collapsed by default), so no model is loaded when the tab opens or when switching cultures. Expanding a section for the first time triggers its render; switching culture resets the section flags, so re-expanding recalculates the current culture. A localized "Loading models…" indicator (`CulturesTab_Loading`) is shown while a section resolves.

- **Automatic culture localization on save (Cultures tab)**: saving a new or edited culture writes the Name, Adjective (`_prefix`) and Collective noun (`_collective_noun`) into the localization files. When `Settings.AutoTranslate` is enabled, they are translated into every CK3-supported game language (`GameSupportedLanguages`: english, french, german, japanese, korean, polish, russian, simp_chinese, spanish) and written to the matching `cultures_l_<lang>.yml` files (under `localization/`, or `localization/replace/` when the culture exists in the base game); when disabled, only the app language is written with the typed text. Translations go through the pluggable `ITranslationProvider` chain (`PdxModIDE.UI.Translation`); on failure the typed text is used as fallback. The localization values are captured in `EditorSave_Click` (so the editor reload after saving cannot clear them) and passed to `SaveCultureLocalizationAsync(cultureId, name, prefix, collective, historyKey, historyDescription)`, which disables the Save/Clear buttons via `SetEditorBusy(true)` and re-enables them in a `finally`. `UpsertLocalizationFile` reorders entries alphabetically by key (case-insensitive) on every write, keeping the header/comments at the top.

- **History loc override (Cultures tab)**: `CultureInfo.HistoryLocOverride` holds the optional `history_loc_override` attribute (parsed by `ExtractAttribute` in `ParseFile`), which overrides the in-game culture history text. The editor section "History description" has a key field (written as `history_loc_override = <key>` before the `traditions` block in `BuildCultureBlock`) and a description field. If the description is filled but the key is empty, `EditorSave_Click` auto-generates `<cultureId>_history_loc` and writes it into the block. `SaveCultureLocalizationAsync` translates the description with the same providers and upserts it into `culture/culture_history_l_<lang>.yml` (under `localization/replace/` when the culture exists in the base game, otherwise `localization/`). `LoadLocalization` includes that file (mod first, then game) so the description loads when editing. The Details panel shows the key and description for cultures that define it.

- **Delete culture (Cultures tab)**: only cultures defined in `ModRoot/common/culture/cultures/mod` (or a subfolder) — the "green" ones (`Source == "Mod"` and `IsModNew`) — can be deleted. `IsCultureDeletable` gates the new "Delete culture" item in the tree `ContextMenu`; `CtxDeleteCulture_Click` requires a Yes/No confirmation showing the culture's display name, then `DeleteCultureBlockFromFile` removes the whole `RawKey = { ... }` block from `CultureInfo.SourceFile` (mod only). If `CountCultureBlocks` returns 0 the culture file itself is deleted. `DeleteCultureLocalization` strips `cultureId`, `cultureId_prefix` and `cultureId_collective_noun` from the mod's `cultures_l_<lang>.yml` files — under `localization/replace/` when the culture exists in the base game (`_baseCultureRawKeys`), otherwise under `localization/`. When the culture defines `history_loc_override`, `DeleteCultureHistoryLocalization` also strips that key from the mod's `culture_history_l_*.yml` files. If a localization file ends up without any remaining entry (no `key: "value"` lines, only the `l_<lang>:` header, comments or blank lines), `RemoveLocalizationKeys` deletes the file itself (`HasLocalizationEntries` checks for remaining entries). The base game is never modified. The tree and the editor (when it was showing the deleted culture) are refreshed afterwards.

- **Lineage `created` and `parents` (Cultures tab)**: `CultureInfo.Created` (string) and `CultureInfo.Parents` (`List<string>`) hold the optional `created` and `parents` attributes of a culture. `ExtractParentsAttribute` parses `parents = { ... }` (reusing `ExtractTraditionKeys`) and `ExtractAttribute("created")` the creation date. `BuildCultureBlock` writes them after `color` and before `heritage` in the same order as the game files (`parents` first, then `created`). The editor has a `created` text field and a two-list parent selector (selected/available, following the traditions pattern) whose available options come from `BuildCultureOptions` (all culture keys, game + mod, sorted). The `created` value is validated on save against `^-?\d+\.\d+\.\d+$` (a leading `-` is allowed for years before Christ); invalid input blocks the save with a localized error.
- **Year offset per profile (`created`)**: the `created` field works with the **calculated (real) date**, while the files always store the **non-calculated value**. The offset is read per profile from `Profile.YearOffset` (`GetCreatedOffset()`), the same one the History tab uses (`Mod Date: {year + offset}`). `ShiftCreatedDate(date, offset)` shifts the year; `ParseFile` keeps the raw file value in `CultureInfo.Created`, the editor converts it to the calculated date (`-offset`) when loading, `BuildCultureBlock` converts it back (`+offset`) when writing, and `UpdateCreatedPreview` shows in real time the value that will be stored (`CulturesTab_EditorCreatedFilePreview`, format `({0})`). New profiles now default to `YearOffset = 0` (`DataProfile`, `Domain.Profile`, `ProjectManager.CreateProfile`); the default was previously hardcoded to `10000` — each profile/mod keeps its own offset independently (existing profiles keep their saved value).
- **Lineage in the culture details (Cultures tab)**: cultures that define `created` and/or `parents` show a "Lineage" section in the Details panel: `DetailCreatedValue` displays `Creada: {calculated} ({file})` and `DetailParentsValue` lists the parent cultures. Culture names in the parents selector and in the details are shown **localized to the app language** (`_editorCultureDisplayNames`, built by `BuildCultureDisplayNames` from the app-language localization with an English fallback, then the raw key) via `GetCultureDisplayName`, instead of the raw English keys.
- **DLC traditions `dlc_tradition` (Cultures tab)**: `CultureInfo.DlcTraditions` (`List<DlcTradition>`) holds the optional `dlc_tradition = { trait = ... requires_dlc_flag = ... fallback = ... }` blocks (parsed by `ParseDlcTraditionBlock`). A tradition is classified as DLC (`IsDlcTradition`) when its definition file is not a base-game `00_*.txt`, when its definition carries `requires_dlc_flag`, or when its id uses a known DLC prefix (`tradition_fp*_`, `tradition_ep*_`, `tradition_ce*_`, `tradition_tgp_`, `tradition_mpo_`, ...). The editor section "DLC traditions" has one row per block: a wide **trait** combo (DLC-only traditions; each item shows the localized name, the required DLC in parentheses and the description inside the combo) and a **fallback** combo (base traditions, with description in its items). `requires_dlc_flag` is **automatic**: `_traditionDlcFlagMap` (trait → flag) is built in `LoadCultures` from the loaded cultures' `DlcTraditions`, and `GetDlcFlagForTrait` looks it up (falling back to the value stored in the culture file), so the flag is never edited manually. `BuildCultureBlock` writes one `dlc_tradition` block per row after `traditions`, and the Details panel lists them as `trait (flag) → fallback` (`BuildDlcTraditionDetailText`). DLC traditions are excluded from the base Traditions selector, and base traditions are only offered as fallback in the `dlc_tradition` rows.
- **Name order convention `name_order_convention` (Cultures tab)**: `CultureInfo.NameOrderConvention` (string) holds the optional attribute that defines how character names are shown for a culture (parsed by `ExtractAttribute` in `ParseFile`). The editor has a combo (`EditorNameOrderConvention`) listing the native presets (`default`, `dynasty_always_first`, `dynasty_first`, `japanese`) with their localized display names and a "Custom format" option (`EditorNameOrderConventionCustom` text box, shown only when selected) for formats with tokens such as `$DYNASTY$`, `$HOUSE$`, `$NAME$` and `$TIER$`. `GetEditorNameOrderConvention` returns the selected preset, or the custom text when "custom" is selected. `BuildCultureBlock` writes `name_order_convention = <value>` after `name_list`; the Details panel shows a section with the localized preset name and the raw value when the culture defines it. `LoadLocalization` now also loads `culture_gfx_l_*.yml` and resolves `$reference$` values so preset names display correctly.
- **Culture localization `culture/` folder convention**: all culture localization reads and writes respect the `culture/` subfolder used by the base game. `LoadLocalization` searches the culture files recursively inside `localization/{lang}/culture/` and `localization/replace/{lang}/culture/` (mod before game, `replace/` before normal; first value wins), for the files `cultures_l_*.yml`, `culture_history_l_*.yml`, `cultural_traditions_l_*.yml`, `cultural_heritages_l_*.yml`, `cultural_languages_l_*.yml`, `head_determination_l_*.yml`, `culture_name_lists_l_*.yml` and `culture_gfx_l_*.yml`. `SaveCultureLocalizationAsync` writes both `cultures_l_{lang}.yml` and `culture_history_l_{lang}.yml` inside `culture/` (under `localization/` for new cultures, `localization/replace/` for base-game cultures). Mods that store these files directly in the language folder (without `culture/`) are not read.
- **Selective culture localization on save (Cultures tab)**: `EditorSave_Click` computes per-field change flags (`nameChanged`, `prefixChanged`, `collectiveChanged`, `historyChanged`) by comparing the current text with the saved baseline (`_editorSavedLocName`, `_editorSavedLocPrefix`, `_editorSavedLocCollective`, `_editorSavedHistoryLocOverride`, `_editorSavedHistoryLocDescription`); for a new culture a field counts as changed only when it has text. `SaveCultureLocalizationAsync(cultureId, nameChanged, name, prefixChanged, prefix, collectiveChanged, collective, historyChanged, historyKey, historyDescription)` returns immediately when nothing changed (no translation-provider requests), translates only the changed fields, and upserts only their keys into `cultures_l_{lang}.yml` / `culture_history_l_{lang}.yml`. When editing/copying, a localization field that had content cannot be left blank: the save is blocked with a localized error (`CulturesTab_EditorLocBlank`).
- **Heritage management (Cultures tab)**: a "Heritages" sub-tab (`HeritagesTabItem`) lists every heritage defined in `common/culture/pillars/*heritage.txt` (game + mod, recursively), each shown with its localized name and its source (`(Mod)`/`(Base)`, green/blue/black like cultures), sorted alphabetically with the current culture. The editor form shows the id, the `audio_parameter` combo (values collected from the real heritages: `byzantine`, `european`, `indian`, `mena`, `sea`, plus any typed value), the localization name/collective noun and, for new heritages, the target file name. Creating a new heritage writes the standard block `heritage_<id> = { type = heritage, is_shown = { heritage_is_shown_trigger = { HERITAGE = heritage_<id> } }, audio_parameter = X }` to a file under `common/culture/pillars/mod/` (default file name from the profile's "Heritage file name" option, `00_heritage.txt`; if the file already exists the block is inserted alphabetically, otherwise the file is created). Editing rewrites the block in place; only mod-new heritages (files under `pillars/mod/`) are editable and deletable, base-game heritages are read-only. Deleting removes the block (and the file when empty) and strips `heritage_<id>_name` / `heritage_<id>_collective_noun` from the mod's `cultural_heritages_l_<lang>.yml` files — under `localization/replace/` when the heritage exists in the base game, otherwise under `localization/`. `SaveHeritageLocalizationAsync` translates the changed fields with the same providers and provider chain as cultures (`CulturesTab_EditorLocTranslating`/`EditorLocSaved` status). After any save/delete `RefreshAfterHeritageChange` reloads the heritage definitions and the localization, reapplies the localized display names, and refreshes the heritage list, the culture tree (grouped by heritage) and the culture editor heritage combo without restarting; when the translation finishes the list refreshes again so the localized name appears.
- **Language management (Cultures tab, parity with heritage â€” 1.7.1)**: a "Languages" sub-tab (`LanguagesTabItem`) lists every language defined in `common/culture/pillars/*language.txt` (game + mod, recursively), each shown with its localized name (`language_<id>_name` from `cultural_languages_l_*.yml`) and its source (`(Mod)`/`(Base)`), sorted alphabetically. The editor form shows the id (without `language_` prefix), a color selector (`LanguageColor` `ComboBox IsEditable` listing named colors from `common/named_colors/*.txt` plus existing language colors, plus a color picker button that generates `hsv{ h s v }` invariant and a preview swatch via `UpdateLanguageColorPreview`/`HsvToRgb`), an optional `ai_will_do` section (`LanguageAiValue`/`LanguageAiMultiply` with hints `LanguageAiSection/Hint/MultiplyHint`; leaving both blank omits the block, filling one defaults the other to 10), the localization name (single key `language_<id>_name`, matching `game/localization/{lang}/culture/traditions/cultural_languages_l_*.yml`) and, for new languages, the target file name. Creating a new language writes the standard block `language_<id> = { type = language, is_shown = { language_is_shown_trigger = { LANGUAGE = language_<id> } }, ai_will_do = { value = X if = { limit = { has_cultural_pillar = language_<id> } multiply = Y } }, color = Z }` (ai block omitted if empty) to a file under `common/culture/pillars/mod/` (default file name from the profile''s "Language file name" option, `00_language.txt`; if the file already exists the block is inserted alphabetically, otherwise the file is created). Editing rewrites the block in place; only mod-new languages (files under `pillars/mod/`) are editable and deletable, base-game languages are read-only. Deleting removes the block (and the file when empty) and strips `language_<id>_name` from the mod''s `cultural_languages_l_<lang>.yml` files â€” under `localization/replace/` when the language exists in the base game, otherwise under `localization/`. `SaveLanguageLocalizationAsync` translates the name with the same provider chain as cultures; `DeleteLanguageLocalization` removes the key. `ParseLanguageParameters` correctly captures `hsv{ 0.6 0.5 0.7 }` as a single color token. After any save/delete `RefreshAfterLanguageChange` reloads the language definitions and the localization, reapplies display names, and refreshes the language list, the culture tree and the culture editor language combo without restarting; the color picker generates `hsv` with `InvariantCulture` and normalizes commas to dots on save, with validation `LanguageColorInvalid` for malformed `hsv{}`. The first heritage is selected by default when the tab opens. `LoadHeritageDefinitions`/`ParseHeritageFile` track `Source`, `SourceFile`, `IsModNew` and `AudioParameter` per heritage.
- **Parent culture selector sorted by display name (Cultures tab)**: the available options of the `parents` (culture mother) selector are sorted by the localized display name (`GetCultureDisplayName`, `StringComparer.CurrentCultureIgnoreCase`) instead of the raw culture id, so the visible order matches the alphabetical order of the app language. The selected parents keep their order to preserve the `parents = { ... }` file layout.

**Themes**: `ResourceDictionary` swap in `MainWindow.ApplyTheme(theme)`. Files in `Themes/*.xaml`.

### 5.9 `GeneralSettingsWindow` + Internationalization (`PdxModIDE.UI`)

**Application settings** (not tied to a profile/mod): modal window (`Window`, not `UserControl`) opened from a gear icon (⚙) in the top-right corner of `MainWindow` (`BtnGeneralSettings_Click`). Contains:

- **Visual theme**: same 7 themes that previously lived in the removed "Options" tab (`SettingsTab`, removed in 1.2.0).
- **Language**: new Spanish/English selector.
- **Translation**: a "Translation" section (added in 1.6.12) configures the translation providers used when saving a culture's localization. It lists MyMemory (always on and locked), LibreTranslate and Lingva (free, no key, each with an editable instance URL), and DeepL (requires a free API key with a "Validate" button that calls the DeepL API to verify it). The selection is persisted through `Settings.TranslationProviders`, `Settings.DeeplApiKey` and `Settings.TranslationProviderUrls` (`IProjectService` → `ProjectManager` → `MainViewModel`). Since 1.6.13 it also includes an "Automatic translation" checkbox (`Settings.AutoTranslate`): when off, only the app language is written instead of translating to all CK3 languages. `CulturesTab.BuildEnabledProviders` reads these at save time, shuffles the enabled providers randomly and tries them in order (`TranslateWithFallbackAsync`), falling back to the typed name if all fail.

**i18n mechanism**: XAML `ResourceDictionary`, same pattern as Themes. Folder `PdxModIDE.UI/Languages/` (`es.xaml`, `en.xaml`) with `system:String` keys (e.g. `Settings_Title`, `Settings_ThemeSection`). Consumed in XAML via `{DynamicResource Key}` to allow hot-switching without restart.

```
MainWindow.ApplyTheme(theme)      → updates _currentThemePath
MainWindow.ApplyLanguage(language) → updates _currentLanguagePath
    └─ RefreshMergedDictionaries()  → recombines BOTH dictionaries (theme + language)
                                       in Application.Resources and Window.Resources,
                                       so changing one does not remove the other.
```

**Persistence**: `Settings.Language` (`data/settings.json`, field `"language"`, default `"en"`) — same flow as `Theme`: `IProjectService.Language` → `ProjectManager.Language` → `MainViewModel.Language` → `MainViewModel.SaveSettings()`.

**Phase 2 (completed in 1.2.1)**: All UI texts have been extracted to language dictionaries (`es.xaml` / `en.xaml`) and all tabs (Profile, Map, Modules, Dates, Validation, Logs) and dialogs use `{DynamicResource ...}` in XAML or `Res("key")` in code-behind. Language change affects the entire application instantly.

**File architecture**: General application texts are in `es.xaml` / `en.xaml`. Game-specific texts go in separate `{GameKey}.{lang}.xaml` files (e.g. `CK3.es.xaml`, `CK3.en.xaml`), loaded automatically according to the active profile via `RefreshMergedDictionaries()`.

---

## 6. Conventions and Style

| Area | Convention |
|------|------------|
| Namespaces | `PdxModIDE.{Project}.{Feature}` |
| Naming | PascalCase (types), camelCase (props/params), UPPER_SNAKE (consts) |
| Immutability | `Domain` entities: `readonly` props, ctor only; `Data` configs: public setters for JSON |
| Async | `Task`/`Task<T>` in repositories and processors; `Parallel.ForEach` for mixed CPU-bound I/O |
| Logging | `File.AppendAllText(logs/...)` manual; `crash.log` in `App.OnStartup` |
| DI | Manual in `ProjectManager` constructor; no container |
| UI Pattern | Code-behind + ViewModel (no MVVM framework); manual `INotifyPropertyChanged` in `MainViewModel` |
| Serialization | `System.Text.Json`; `JsonPropertyName` not used (public props = JSON names) |
| Paths | Always `Path.Combine`; `FileOperations.EnsureDirectory` before writing |
| Error Handling | `try/catch` in UI commands → `MessageBox.Show`; global crash → `logs/crash.log` |

---

## 7. Key Design Decisions

| Decision | Justification | Trade-off / Debt |
|----------|---------------|------------------|
| **9 separate projects** | Clear domain/data/core/UI separation; testability | More boilerplate; slightly slower build |
| **No DI container** | Simplicity, zero extra dependencies | Coupling `ProjectManager`→concrete `ModuleProcessor` |
| **Flat JSON in `data/`** | No DB, portable, manually editable | Non-transactional; naive concurrency (last wins) |
| **Per-game date regex** | Flexibility (CK3/EU4 different formats) | Simple regex; doesn't parse context (e.g. `start_date` vs `end_date`) |
| **Auto-backup** | Safety against offset errors | Duplicates space; no automatic cleanup |
| **Cached 16M byte LUT** | Instant map render; avoids rebuild | 16 MB RAM + disk; invalidation only by source file hash |
| **CPU overlay instead of shader** | `SKShader.CreateImage` as child shader in `SKRuntimeEffect` returns 0 in `eval()` on SkiaSharp 3.116.1 (CPU raster). Workaround: render terrain+borders via shader, apply overlay (holder/county/duchy/etc) on CPU by iterating pixels with `Marshal.Copy`. | 100% CPU; if SkiaSharp fixes it, can migrate back to shader. |
| **16-bit LUT for unlimited items** | `BuildHolderLut`/`BuildCountyLut` use `ushort[]` (65535 max) instead of `byte[]` (255 max) | No wrap-around needed; palette dynamically sized |
| **Synchronous Parallel.ForEach in ProcessModule** | Leverages multi-core I/O | Blocks thread pool; `ProcessModulesAsync` does `await Task.CompletedTask` after `Parallel.ForEach` |
| **Manual ViewModels** | Full control, no magic | Boilerplate `OnPropertyChanged`; easy to introduce binding bugs |

---

## 8. Technical Debt and Prioritized TODOs

### 🔴 Critical
- [ ] **Race condition in `ModuleProcessor._moduleCache`**: `LoadModules()` calls `.GetAwaiter().GetResult()` on thread pool → possible deadlock if called from UI thread. **Fix**: make `LoadModulesAsync` + `await` in `ProcessModulesAsync`.
- [ ] **Synchronous `Parallel.ForEach` in `ProcessModulesAsync`**: blocks thread pool. **Fix**: `Parallel.ForEachAsync` (.NET 6+) or `Task.WhenAll` with `SemaphoreSlim`.
- [ ] **No path validation in `CreateProfile`**: `GameRoot`/`ModRoot`/`BackupRoot` can be empty → runtime error on processing.

### 🟠 Important
- [ ] **Introduce `Microsoft.Extensions.DependencyInjection`**: register `IModuleRepository`, `IProjectService`, `ModuleProcessor`, `DefinesProcessor`, `ModuleValidator`.
- [ ] **Base ViewModel with `CommunityToolkit.Mvvm`** (`[ObservableProperty]`, `[RelayCommand]`) → removes boilerplate `INotifyPropertyChanged`.
- [ ] **Unit tests** (xUnit):
  - `ModuleProcessor.ApplyOffset` (various date formats, negative offsets, no-match).
  - `DefinesProcessor.Read/WriteEndDate` (mock FS).
  - `MapLoader.LoadDefinition` (malformed CSV, duplicates).
  - `ModuleValidator.CompareFileContents` (equal, different, only in A, only in B).
- [ ] **Module/file list virtualization** (`VirtualizingStackPanel` + `ItemsControl` → `ListView` with `VirtualizingPanel.IsVirtualizing=True`).
- [ ] **Incremental LUT cache**: invalidate only modified provinces (diff `definition.csv`).
- [x] **Internationalization completed (1.2.1)**: all UI strings extracted to `es.xaml` / `en.xaml`. Tabs and dialogs use `DynamicResource` or `Res()`. Game-specific text translation to `{GameKey}.{lang}.xaml` pending.

### 🟢 Improvement
- [ ] **EU4/Imperator/HOI4/Vic3 plugins**: new `IGamePlugin` with specific regex and paths.
- [ ] **FileSystemWatcher** on `ModRoot` → auto-refresh validation.
- [ ] **Export validation report** (HTML/Markdown) from `ValidationTab`.
- [ ] **Semantic diff** (understand Clausewitz syntax) instead of line-by-line.
- [ ] **Performance profiling**: `BenchmarkDotNet` for `ModuleProcessor`, `MapLoader.BuildLutInMemory`.
- [ ] **Toast notifications** (e.g. `MaterialDesignThemes` Snackbar) instead of MessageBox for success/progress.

---

## 9. Security / Integrity Rules (Logic)

- **Profiles**: Isolation by `Profile.Name` (unique key). No shared data between profiles.
- **Backup**: Write always preceded by copy to `BackupRoot` (preserves timestamps).
- **Date offset**: Only extensions allowed by `IGamePlugin.IsDateProcessableExtension`.
- **Game detection**: `CanHandleGame` looks for characteristic files; fallback to user dialog.
- **Paths**: `Directory.Exists` validation in `DetectGame` and `Browse` dialogs.

---

## 10. Useful Commands

```bash
# Build solution (Release)
dotnet build PdxModIDE.sln -c Release

# Build UI only (for quick test)
dotnet build PdxModIDE.UI/PdxModIDE.UI.csproj -c Debug

# Run UI
dotnet run --project PdxModIDE.UI/PdxModIDE.UI.csproj

# Clean all
dotnet clean PdxModIDE.sln

# View dependency tree
dotnet msbuild PdxModIDE.sln /t:GenerateRestoreGraphFile /pp:restore.graph
```

**Build output structure**:
```
PdxModIDE.UI/bin/Debug/net8.0-windows/
├── PdxModIDE.UI.exe
├── data/                 # JSON configs (copied if not present)
├── logs/                 # Created at runtime
├── Themes/               # ResourceDictionaries
└── *.dll (Core, Domain, Data, IO, MapEngine, Project, Rendering, Validation)
```

---

## 11. Environment Variables / External Configuration

No mandatory environment variables. All configuration in `data/*.json`.

**Default paths** (if user does not configure):
- `GameRoot`: Detected via `GameRegistry.DetectGame` or dialog.
- `ModRoot`: `mod/` folder next to `GameRoot` (Paradox convention).
- `BackupRoot`: `backups/{ProfileName}/` under `ModRoot`.

---

## 12. Extensibility: Adding a New Game (e.g. EU4)

1. Create `PdxModIDE.Core.Games.EU4.EU4GamePlugin : IGamePlugin`:
   - `GameKey = "EU4"`
   - `DateRegex` adapted to EU4 format (e.g. `\b(\d{4})\.(\d{2})\.(\d{2})\b`)
   - `IsDateProcessableExtension` (add `.gfx`, `.gui` if applicable)
   - `GetDefinesPath()` → EU4 `defines.txt` location
   - `CanHandleGame` → looks for `eu4.exe` or `defines.txt` with EU4 `start_date`
2. Register in `App.OnStartup`: `GameRegistry.Register(new EU4GamePlugin());`
3. Add base modules/files in `data/modules.json` and `data/files.json` under key `"EU4"`.
4. (Optional) Extend `MapLoader` if map format differs (EU4 uses same `definition.csv` + `provinces.png`).

---

## 13. Key File Quick References

| File | Purpose |
|------|---------|
| `PdxModIDE.UI/App.xaml.cs` | Bootstrap: registers CK3, data/logs dirs, crash handler |
| `PdxModIDE.UI/MainWindow.xaml.cs` | Theme swap, DataContext=MainViewModel, initial profile |
| `PdxModIDE.UI/ViewModels/MainViewModel.cs` | Full UI state, commands, bindings |
| `PdxModIDE.Project/ProjectManager.cs` | Orchestrator: profiles, session, processing, validation, persistence |
| `PdxModIDE.Core/ModuleProcessor.cs` | Game→mod copy + date offset (parallel, logging) |
| `PdxModIDE.Core/DefinesProcessor.cs` | Read/Write `end_date` in defines.txt |
| `PdxModIDE.Core/Games/GameRegistry.cs` | Plugin registry + automatic game detection |
| `PdxModIDE.Core/Games/CK3/CK3GamePlugin.cs` | CK3 implementation: regex, extensions, defines path |
| `PdxModIDE.MapEngine/MapLoader.cs` | Full map loading + LUT cache + holders by year |
| `PdxModIDE.MapEngine/TitleHistoryLoader.cs` | Parse `history/titles/*.txt` → `TitleHistory` |
| `PdxModIDE.Rendering/MapRenderer.cs` | SkiaSharp viewport, zoom/pan, color picker, tooltips. CPU overlay (workaround child shader bug). |
| `PdxModIDE.Validation/ModuleValidator.cs` | 3-way diff (mod/game/backup) recursive, bidirectional lookahead for interleaved additions/removals |
| `PdxModIDE.UI/DiffViewDialog.cs` | Side-by-side diff viewer (Notepad++ Compare style, "Original"/"Modified" panels, line numbers, color-coded backgrounds) |
| `PdxModIDE.Data/DataLoader.cs` | Generic Load/Save JSON `data/*.json` |
| `PdxModIDE.Domain/Models.cs` | Pure entities (Module, GameFile, Profile, EditingSession) |
| `PdxModIDE.IO/FileOperations.cs` | CopyPreserveTimestamps, ReadTextFile, EnsureDirectory |

---

*Generated: 2026-08-31 | Project: PdxModIDE | Version: 1.7.1 | Stack: .NET 8 / WPF / SkiaSharp 3.116.1 / System.Text.Json*
