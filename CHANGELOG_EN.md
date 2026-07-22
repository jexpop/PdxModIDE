# Changelog - PdxModIDE

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.4.5]

### Added

- **Safe file versioning on module processing**: when a destination file already exists in the mod directory, it is now renamed with a `_v1`, `_v2`, etc. suffix instead of being overwritten. The new file keeps the original name. If the existing file content is identical to the new content, neither renaming nor writing occurs.

### Fixed

- **Duplicate "Process Complete" message box**: removed redundant `MessageBox.Show` in `DatesTab.xaml.cs` that caused two confirmation dialogs to appear after processing modules from the Dates tab.

---

## [1.4.6]

### Changed

- **Non-recursive module processing in Dates tab**: the Dates tab now only processes files directly in the module path without recursing into subdirectories. Added `bool recurseSubdirectories` parameter threaded through `ProcessModulesAsync` / `ProcessModule` to control recursion behavior.

### Removed

- **Auto-backup on module processing**: removed automatic backup copies to the profile's backup folder during module processing. Backups are now a manual operation.

---

## [1.4.4]

### Added

- **Independent module selection for Dates tab**: module selection is now split into two independent lists. The **Modules tab** controls global module selection (used by all tabs except Dates) with checkboxes. The **Dates tab** has its own independent module selector for processing. The old module selector in the Profile tab has been removed.
- **Informative text** in Modules tab and Dates tab explaining the scope of each module selection.

### Changed

- **ProjectManager.ProcessModulesAsync** now uses `DatesModules` instead of `Modules` from the profile, so processing only acts on modules selected in the Dates tab.
- **Auto-persistence**: toggling a module checkbox in either tab now immediately saves the selection to `data/profiles.json`.

---

## [1.4.3]

### Changed

- **Province name localization in Map tab info panel**: the province name field now uses `GetLocalizedTitleName()` to display the localized name from game YML files instead of the raw key from `definition.csv`. Applies to all province types (land, sea, impassable, etc.); falls back to the raw key when no localization entry exists.

---

## [1.4.2]

### Changed

- **Title panel in Map tab**: the title panel (Barony, County, Holder, Liege) is now only shown when the selected province type is `"land"`. For non-land provinces (sea, lake, river, impassable, unknown) the title panel remains hidden even if Base or Mod source is active.

---

## [1.4.1]

### Added

- **Title name localization in Map tab info panel**: barony and county names now display the real localized name instead of the raw title key (e.g. `b_*`). Names are loaded from the `name` field in `common/landed_titles/*.txt` and from YML localization files (`localization/{lang}/*.yml`). The language follows the app setting (English/Spanish/Catalan) with fallback to English when the language is unavailable in the game.
- **Map label localization**: overlay labels on the map also use localized names from the same sources.
- **Support for mod `localization/replace/` folder**: replacement localization files (`localization/replace/{lang}/*.yml`) override the mod's regular localization with Mod > Base priority.

### Fixed

- **YML localization parser**: now correctly handles the CK3 YML format (`key:0 "value"`) which includes a version number after the colon. Previously the version number and quotes were included in the displayed name (e.g. `0 "Tenerife"` instead of `Tenerife`).

---

## [1.4.0]

### Added

- **Title name labels on the History tab map**: new "Show names" checkbox (per profile, `ShowTitleNames`) draws territory names (county/duchy/kingdom/empire/holder) directly on the rendered bitmap using CPU SkiaSharp. Features: dynamic font size proportional to territory area × zoom (clamped 9–18px), rotation along the principal axis of the territory shape (±45° limit), overlap avoidance (largest territories first, 4px margin), and semi-transparent rounded background. Scale text to fill the bounding box when text is shorter than box width. Holder names use direct `TitleHistoryLoader.GetHolderAtYear` (avoiding LUT 255-wrap bug).

### Fixed

- **Baronies with a hyphen in the name were not detected in `landed_titles`**: the title-parsing regex (`MapLoader.LoadLandedTitlesFrom`) only allowed `[A-Za-z0-9_]+` in the identifier, so names like `b_dvur-chvojno` failed to match and the barony (and its associated province) was left out of `ProvinceToBarony`/`BaronyToCounty`, staying uncolored in the County/Duchy/Kingdom/Empire map modes. Fix: added the hyphen to the regex character class (`[A-Za-z0-9_-]+`).

---

## [1.3.4]

### Fixed

- **Holder/County/Duchy/etc overlay broken in Map tab**: provinces rendered gray in all overlay modes. Root cause: `SKShader.CreateImage` as child shader within `SKRuntimeEffect` returns 0 in `eval()` on SkiaSharp 3.116.1 (CPU raster backend). Workaround: CPU-based overlay in `RenderToBitmap` — per-pixel lookup of province color → holderIdx → palette color, preserving borders and highlight. See `docs/skia-image-shader-bug-workaround.md`.
- **Crash on map load**: `RenderToBitmap` returned a disposed `SKBitmap` due to an accidental `using var` on the returned bitmap.

### Changed

- **`RenderToBitmap`**: now renders terrain+borders via shader (mode=0) and applies overlay on CPU. Row-by-row pixel access via `GetPixels()` + `Marshal.Copy` for performance.
- **`SetHolderMode`**: no longer creates `SKImage` from the holder LUT; stores the `byte[]` for direct CPU use.
- **`BuildShaderCache`**: uses dummy `SKShader.CreateColor(SKColors.Black)` for `holderLut`/`palette` children (unused in mode=0).
- **`HistoryTab.xaml.cs`**: added `InvalidateRender()` for consistent cache invalidation; replaces manual `_cachedWidth = -1; QueueRender()` pattern.

### Removed

- **`_holderLutImage` and `_holderLutBackingBitmap`**: no longer needed since the shader is not used for overlay.
- **Diagnostic code**: removed `File.WriteAllText` and bitmap/image comparisons used during bug investigation.

---

## [1.3.3]

### Changed

- **Panel formatting in Map tab**: GroupBox headers "PROVINCE" and "TITLE" now render in bold with a larger font size to stand out from the subtitles.
- **Title panel restructured**: now follows the same format as the Province panel, with bold labels (Barony, County, Holder, Liege) and values on a separate line below. Uses `DynamicResource` for correct translation per active language.
- **Simplified Holder and Liege values**: removed the "in {year}" prefix from the displayed value; now shows only the holder name and source ([Mod]/[Base]).
- **Coherent translations**: new keys `HistoryTab_BaronyLabel`, `HistoryTab_CountyLabel`, `HistoryTab_HolderLabel`, `HistoryTab_LiegeLabel` in EN/ES/CA.

---

## [1.3.2]

### Added

- **i18n for province panel fields**: new resource keys `HistoryTab_IDLabel`, `HistoryTab_NameLabel`, `HistoryTab_ColorLabel`, `HistoryTab_TypeLabel` (label-only, no placeholder) and `MapTerrain_Land`, `MapTerrain_Sea`, `MapTerrain_Lake`, `MapTerrain_River`, `MapTerrain_Impassable`, `MapTerrain_Unknown` for terrain type translation in English, Spanish, and Catalan.

### Changed

- **Province info panel layout**: ID, Name, Color, and Type fields now display the label in bold with the value on a separate line below. Name uses `TextWrapping` for long values.
- **Language refresh order**: `ApplyLanguage` and `ApplyTheme` in `MainWindow.xaml.cs` now call `RefreshMergedDictionaries()` before setting the ViewModel property, ensuring `PropertyChanged` handlers read the already-updated resource dictionaries.

### Fixed

- **Off-by-one language refresh in Map tab**: terrain type values (`MapTerrain_*`) and province info values now update immediately when switching languages, instead of showing the previous language's translation.

---

## [1.3.1]

### Added

- **Informational placeholder panel in Map tab**: when no province is selected, the left column now shows a panel with instructions on map navigation (zoom buttons, mouse wheel, right-click drag, fit to window), province selection (click any province to view details), and layers (enable Base/Mod checkboxes and overlay modes). The panel is hidden when a province is clicked and reappears when clicking empty space.
- **New i18n keys**: `HistoryTab_Navigation`, `HistoryTab_Navigation_Text`, `HistoryTab_Selection`, `HistoryTab_Selection_Text`, `HistoryTab_Layers`, `HistoryTab_Layers_Text` in English, Spanish, and Catalan.

---

## [1.3.0]

### Added

- **Contextual info panel in Map tab**: the left province/title info panel is now hidden by default and only shown when clicking on a province. The "Title" block (Barony, County, Holder, Liege) is only visible when at least one of the "Base" or "Mod" checkboxes is active.

### Changed

- **Dynamic left panel visibility**: added `x:Name="InfoPanel"` to the left panel `StackPanel` in `HistoryTab.xaml`, with initial `Visibility="Collapsed"`. It is shown on province click (`UpdateProvinceInfo`) and hidden when clicking empty space.
- **Title conditional on Base/Mod**: the Title `GroupBox` (`TitleGroup`) is only shown if `HasActiveSource()` returns true (Base or Mod checked). It updates both on province click and when Base/Mod state changes while the panel is visible.

---

## [1.2.2]

### Added

- **New language: Català (ca)**: Catalan added as the third available language. New file `Languages/ca.xaml` with full UI translation, `Languages/CK3.ca.xaml` as placeholder, radio selector in `GeneralSettingsWindow`, and support in `ApplyLanguage` / `GetSelectedLanguage`.
- **Complete UI internationalization (phase 2)**: ~140 new i18n keys extracted to `es.xaml` / `en.xaml` for all tabs and dialogs:

- **Complete UI internationalization (phase 2)**: ~140 new i18n keys extracted to `es.xaml` / `en.xaml` for all tabs and dialogs:
  - MainWindow (tooltips and tab headers)
  - ProfileTab (paths, CRUD buttons, modules group)
  - ModulesTab (editing, add/save/delete buttons)
  - DatesTab (offset, end_date, modules to process)
  - HistoryTab (province/title panel, zoom, modes, tooltips)
  - ValidationTab (modules, files, comparison, results)
  - LogsTab (viewer, filters, configuration)
  - InputDialog (Accept/Cancel buttons)
- **Separation of general vs game-specific texts**: general application texts reside in `es.xaml` / `en.xaml`. Game-specific texts go in `{GameKey}.{lang}.xaml` (e.g. `CK3.es.xaml`, `CK3.en.xaml`), loaded dynamically based on the active profile.
- **Improved `RefreshMergedDictionaries()`**: now loads three dictionaries (theme + general language + game-specific language) and refreshes when changing profile.
- **`GetGameLanguagePath()`**: new method that generates the path `Languages/{GameKey}.{language}.xaml` for the active game-specific dictionary.
- **Helper method `Res(string key)`** in code-behind classes (MainViewModel, HistoryTab, ValidationTab, DatesTab, LogsTab, App) to resolve i18n strings from C#.
- **Placeholder files**: `Languages/CK3.es.xaml` and `Languages/CK3.en.xaml` for future CK3-specific texts.

### Changed

- **Default language**: the `Language` field in `Settings` now defaults to `"en"` (English) instead of `"es"` (Spanish). The application starts in English if no previous `settings.json` exists.
- **Validation status codes**: internal status codes in `ProjectManager` changed from Spanish to English (`"Modified"`, `"Added"`, `"Deleted"`, `"SAME"`, `"CHANGED"`) for consistency with the default language.
- **`ValidationTab`**: module comparison now uses `SelectedIndex` instead of comparing translated ComboBox strings, avoiding dependency on the active language.
- **`MainWindow.xaml`**: initial reference to the language dictionary changed from `Languages/es.xaml` to `Languages/en.xaml`.
- **Status labels in HistoryTab**: map mode texts and province info labels are displayed in English by default.

### Fixed

- **Bug in `ApplyLanguage` (MainWindow.xaml.cs)**: the language dictionary path selection switch had no case for `"es"`, so selecting Spanish always loaded the English dictionary.

### Notes

- Validation status codes have been unified to English as part of the default language change. DiffDialog, DiffChoiceDialog, DiffViewDialog and ValidationTab use these codes for coloring and filtering.
- Game-specific texts (CK3) are structurally prepared but still empty; they will be populated in future versions.

---

## [1.2.0]

### Added

- **General Settings Window** (`GeneralSettingsWindow`): new modal window accessible via a wrench icon (⚙) in the top-right corner of `MainWindow`, with application settings not tied to a specific profile/mod (Visual Theme and Language).
- **Internationalization infrastructure (i18n)**: new language mechanism based on XAML `ResourceDictionary`, following the same pattern already used for Themes (`Themes/*.xaml` → dynamic dictionary swap with `DynamicResource`). Folder `PdxModIDE.UI/Languages/` with `es.xaml` (default) and `en.xaml`.
- **`Settings.Language`**: new field in `data/settings.json` (`"language"`, default `"es"`), persisted same as `Theme`. Propagated through `IProjectService.Language`, `ProjectManager.Language` and `MainViewModel.Language`.
- **`MainWindow.ApplyLanguage(string)`**: new public method that reloads the language dictionary without losing the active theme (and vice versa), via `RefreshMergedDictionaries()`, which recombines both dictionaries (theme + language) in the resources of `Application` and the window.
- Language selector (Español/English) in `GeneralSettingsWindow`, with hot application (no restart required).

### Changed

- **"Options" tab removed from `TabControl`**: the Theme configuration (previously in `SettingsTab`, inside the project tabs) has been moved to the new modal window `GeneralSettingsWindow`, since it is application configuration, not specific to a mod/profile. `SettingsTab.xaml`/`.xaml.cs` removed.
- `PdxModIDE.UI.csproj`: added `<Content Include="Languages\**">` (same as `Themes\**`) to copy language dictionaries to the output/publish directory.

### Notes

- Phase 1 of i18n: for now only the texts in `GeneralSettingsWindow` are translated (proof of concept of the hot language switch mechanism). The rest of the interface (Profile, Map, Dates, Modules, Validation, Logs) remains hardcoded in Spanish; its translation will be addressed in a later phase, reusing the same `ResourceDictionary` mechanism.

---

## [1.1.10]

### Changed
- **Full names in title mode checkboxes**: Modes "Tit.", "Cond.", "Duc.", "Rey.", "Imp." are now displayed as "Titular", "County", "Duchy", "Kingdom", "Empire" respectively.
- **Conditional visibility of title modes**: Title mode checkboxes (Titular/County/Duchy/Kingdom/Empire) are only shown when at least one of the "Base" or "Mod" checks is active. If both are disabled, title modes are hidden.
- **Default selection**: When activating "Base" or "Mod" without any title mode active, "Titular" is automatically selected.

### Fixed
- **Always one active mode**: The last title mode can no longer be unchecked while "Base" or "Mod" is active. If the user tries to uncheck it, "Titular" is re-checked automatically.
- **Mode not applied after map load**: If the user activated "Base" or "Mod" before the map finished loading (async load), `SourceModeChanged` returned early due to `_mapLoaded == false` and the title mode was never applied. `ReapplyActiveMode()` is now called at the end of `DoLoad` when there is an active source.
- **Mod data overwritten by base copies in mod**: When the mod contained copies of base `history/titles` files plus a custom file, `TitleHistoryLoader.LoadAll` ignored duplicate titles (`if (!AllTitles.ContainsKey)`) and the alphabetically first one won — typically the base copy, not the custom data. Added `overwriteDuplicates` parameter so the mod always takes priority.
- **Landed_titles structure not updated when changing source**: `MapLoader` only loaded the landed_titles structure from the base game. When activating "Mod", the mod's barony/county/duchy etc. structure was not applied. Added `SaveBaseSnapshot()`, `LoadModLandedTitles(modRoot)` and `ResetToBase()` to switch the structure based on the active source (Base → base, Mod → mod, Both → mod).

---

## [1.1.9]

### Fixed
- **Parser of `common/landed_titles` lost titles with intermediate non-title blocks**: blocks like `cultural_names = { ... }`, `color = { ... }` or `definite_form = { ... }` inside a title caused their lone `}` to prematurely pop the parent title from the stack. This prevented subsequent baronies from being linked to their county (`BaronyToCounty` remained empty), so `BuildCountyLut`/`BuildHolderLut` never found the county for those provinces. Added `nonTitleDepth` counter that tracks non-title block braces to ignore their closing braces without affecting the title stack.

---

## [1.1.8]

### Fixed
- **Parser of `history/titles` ignored single-line date blocks**: a very common format in baronies and many CK3 counties, e.g. `900.1.1={ holder=140000 liege=k_england }`. The brace counter cut off line processing (`continue`) as soon as it saw a `}`, without checking whether that closing brace belonged to the (nested) date block or the full title, so those lines were never read — affecting both Base and Mod. Rewritten the parser to calculate the net brace balance of the line and always extract `holder=`/`liege=` before deciding if the title closes.
- Also, inline comments (`# ...`) are now ignored to avoid false positives when searching for `holder=`/`liege=`.

---

## [1.1.7]

### Fixed
- **Recursive search in `history/titles` and `common/landed_titles`**: `TitleHistoryLoader.LoadAll` and `MapLoader.LoadLandedTitles` only scanned the top level of the folder. The Paradox engine recursively processes any subfolder inside those paths (with any name, not just literal "mod" folders), so a mod that organizes its history/title files in its own subfolders was not being read. Both now use `SearchOption.AllDirectories`, generically for both Base and Mod.

---

## [1.1.6]

### Added
- **Functional logic of "Base"/"Mod" checks**: They now determine where the holder information displayed on the map comes from (Map tab):
  - **Only Base**: uses `history/titles` from the base game, with the year as-is in the date `TextBox`.
  - **Only Mod**: uses `history/titles` from the mod, applying the profile's offset (year + `YearOffset`) so the searched date matches the already-shifted dates in the mod files.
  - **Both active**: Mod data takes priority (with offset); if no holder exists for that date in the mod, the base game data is used (without offset).
  - **Neither active**: the default land/sea map is displayed, same as before this feature, regardless of whether Titular/County/Duchy/Kingdom/Empire is checked.
  - Also applies to County/Duchy/Kingdom/Empire modes (same gating; their structural information does not vary between base and mod).
- **"No data" colors in LUT mode**: when a title mode is active but a province has no data (holder/county/etc.), land is now painted gray and sea blue (previously everything appeared in a uniform flat gray, without distinguishing sea). Change in the `MapRenderer` shader.
- **`MapLoader.BuildCombinedHolderLut`**: new method that combines Base and Mod holders per province with the Mod > Base priority described above.
- **Province info panel**: when clicking a province, the "Holder"/"Liege" displayed now respect the active Base/Mod checks (with offset for Mod) and indicate between brackets which source they come from (`[Mod]` / `[Base]`).

---

## [1.1.5]

### Added
- **"Base" and "Mod" checks in Map tab**: New `BaseSourceCheck` and `ModSourceCheck` checkboxes, not mutually exclusive, placed between the date (with its calculated "Mod Date") and the Titular/County/Duchy/Kingdom/Empire checks. For now they only refresh the map when changed (`SourceModeChanged`); the logic for what data to display based on Base/Mod is implemented in version 1.1.6.

---

## [1.1.4]

### Added
- **Calculated Mod date in Map tab**: New `OffsetLabel` next to the year (before the titular/county/etc. checks) showing the resulting date in the mod (`year + YearOffset` of the active profile), displaying both values (base year and mod date) simultaneously. Informational only, not editable; updates on tab load, profile change, offset change, and year change.

---

## [1.1.3]

### Changed
- **Map tabs unification**: The two tabs "History (Base)" and "History (Mod)" have been merged into a single tab called "Map" (`local:HistoryTab` without a fixed `Mode` in `MainWindow.xaml`).

---

## [1.1.2]

### Changed
- **History tab informative text**: Removed the "View: Mod/Base Game" prefix from the text displayed after map loading; now only the province and title count is shown (`X prov, Y titles`).

---

## [1.1.1]

### Added
- **Duchies / Kingdoms / Empires modes** in History tab: Checkboxes "Duchy", "Kingdom", "Empire" to color the map by duchy (`d_xxx`), kingdom (`k_xxx`) and empire (`e_xxx`) boundaries.
- **Complete title hierarchy**: `MapLoader.LoadLandedTitles()` now builds `CountyToDuchy`, `DuchyToKingdom`, `KingdomToEmpire` from the nested title stack.
- **New LUTs**: `BuildDuchyLut()`, `BuildKingdomLut()`, `BuildEmpireLut()` with palettes and color wrap-around.
- **Extended mutual exclusion**: All 5 modes (Titular, Counties, Duchies, Kingdoms, Empires) uncheck each other.
- **Compact labels**: Checkboxes use abbreviations (Tit., Cty., Dch., Kgd., Emp.) with tooltips to save space in the bar.

### Changed
- **Info panel labels**: "Title" panel now shows Barony, County, Duchy, Kingdom, Empire, Holder, Liege according to the active mode.

---

## [1.1.0]

### Added
- **Counties mode in History tab**: New "Counties" checkbox alongside "Titular" that colors the map by county boundaries (`c_xxx`) instead of by holder (character). Uses `MapLoader.BuildCountyLut()` → maps province → barony → county.
- **Color cycle for >255 items**: In `BuildHolderLut` and `BuildCountyLut`, indices >255 now wrap around (modulo 255) instead of capping at 255, preventing hundreds of counties/holders from sharing the same green color.
- **Mutual exclusion**: "Titular" and "Counties" checkboxes uncheck each other.

### Fixed
- **Green counties**: With >255 counties in CK3, all counties from 256 onward used index 255 (same color). They now cycle 1-255.
- **Green holders**: Same fix applied to `BuildHolderLut` for >255 unique holders.

---

## [1.0.0]

### Added
- **Multi-project modular architecture**: 9 .NET 8 projects (Core, Domain, Data, IO, MapEngine, Project, Rendering, UI, Validation).
- **Profile system**: Mod profiles with GameRoot, ModRoot, BackupRoot, YearOffset, modules and selected files.
- **Parallel module processor**: `ModuleProcessor.ProcessModulesAsync` copies game→mod files applying date offset (per-game regex) with `Parallel.ForEach` and per-module logging.
- **Plugin system for games**: `IGamePlugin` + `GameRegistry` with automatic detection (`DetectGame`) and fallback to selection dialog. Implemented `CK3GamePlugin`.
- **Defines processing**: `DefinesProcessor` reads/writes `end_date` in `defines.txt` (game + mod) with automatic backup.
- **Complete Map Engine**:
  - `MapLoader`: loads `definition.csv`, `default.map`, `landed_titles/*.txt`, `provinces.png/bmp`.
  - LUT cache (16M entries) persisted in `%LocalAppData%/PdxModIDE/lut_cache` with MD5 hash of sources.
  - `TitleHistoryLoader`: parses `history/titles/*.txt` → `TitleHistory { Holders: SortedList<int, string> }`.
  - `BuildHolderLut`: generates holder LUT by year for rendering.
  - **Counties mode**: `BuildCountyLut` colors map by county boundaries (`c_xxx`) from `landed_titles`.
- **Map rendering**: `MapRenderer` (SkiaSharp) with viewport, zoom/pan, color picker, province/holder tooltips.
- **Module validation**: `ModuleValidator` recursively compares game/mod/backup; line-by-line diff; summary by status (Equal/Modified/Added/Deleted).
- **JSON persistence**: Generic `DataLoader` for profiles, modules, files, settings, logfilters in `data/*.json`.
- **WPF UI (lightweight MVVM)**:
  - `MainWindow` + `MainViewModel`: tabs Profile, Modules, Files, Dates, Validation, History, Logs, Settings.
  - Dynamic themes: Light, Dark, CK3, Sepia, Contrast, VSCode Dark/Light (ResourceDictionary swap).
  - Profile management (CRUD, rename, game detection), module/file selection with checkboxes.
  - Async processing with progress, parallel validation, diff viewer in tabs.
- **Global error handling**: `App.OnStartup` registers `UnhandledException` + `DispatcherUnhandledException` → `logs/crash.log` + MessageBox.

### Changed
- **Target Framework**: .NET 8.0, `Nullable=enable`, `ImplicitUsings=enable`.
- **Data structure**: `Domain` pure entities; `Data` JSON configs; bidirectional mapping in `ProjectManager.SyncDomainProfiles`.
- **Manual dependency injection**: `ProjectManager` instantiates `ModuleProcessor(ModuleRepository())`; repositories use static `DataLoader`.

### Deprecated
- (None - initial version)

### Removed
- (None - initial version)

### Fixed
- (None - initial version)

### Security
- No secrets stored; game/mod/backup paths configured by user in profile.

---

## [1.4.7]

### Added

- **"Find unconfigured date modules" button in Validation tab**: new button that recursively scans the game root directory looking for folders not yet configured as modules that contain files with date patterns. Results are shown in an informational dialog (no changes are made to any configuration). Uses `Parallel.ForEach` and line-by-line reading with early exit for optimal performance, skipping files over 1 MB.

### Changed

- **Module validation no longer recurses into subdirectories**: both "All Modules" and single-module validation in the Validation tab now only list files directly in the module path without descending into subdirectories (`SearchOption.TopDirectoryOnly`). This makes validation consistent with the non-recursive processing introduced in 1.4.6 for the Dates tab.

---

## [Unreleased]

### Changed

- **Improved diff algorithm in `ModuleValidator.CompareFileContents`**: replaced the basic 3-line lookahead heuristic with a bidirectional lookahead (up to 20 lines) that produces properly interleaved additions and removals instead of outputting all additions first then all removals.
- **Side-by-side diff viewer (`DiffViewDialog`)**: replaced the unified-format text viewer with a side-by-side view similar to Notepad++ Compare plugin. Shows "Original" and "Modified" panels with line numbers on both sides, color-coded backgrounds (green for additions, red for removals), and paired modification rows when a removal is immediately followed by an addition.

### Planned
- **EU4 / Imperator / HOI4 / Victoria 3 support**: new `IGamePlugin` with date regex, defines paths, processable extensions.
- **Migration to DI container** (Microsoft.Extensions.DependencyInjection) for `ProjectManager`, repositories, processors.
- **Base ViewModels with `INotifyPropertyChanged`** centralized (currently manual implementation in `MainViewModel`).
- **Unit tests**: xUnit + Moq for `ModuleProcessor.ApplyOffset`, `DefinesProcessor`, `MapLoader.LoadDefinition`, `ModuleValidator.CompareFileContents`.
- **Pagination / virtualization** in module/file lists (currently full `ObservableCollection`).
- **Performance profiling**: benchmark `ProcessModulesAsync` with `BenchmarkDotNet`; optimize parallel I/O (currently synchronous `Parallel.ForEach` on I/O).
- **Incremental LUT cache**: invalidate only changed provinces instead of full rebuild.
- **Toast notifications** in UI (currently MessageBox for errors).
- **Persistent per-user settings** (theme, last profile, recent paths) → already in `Settings.json` but extend.
- **Incremental validation**: `FileSystemWatcher` on ModRoot to update validation status in real time.
- **Diff export**: HTML/Markdown validation report.
- **Internationalization (i18n) - full UI translation**: the base infrastructure (`ResourceDictionary` XAML EN/ES) already exists since 1.2.0, but only covers `GeneralSettingsWindow`. The hardcoded Spanish strings in the remaining tabs (`ProfileTab`, `HistoryTab`, `DatesTab`, `ModulesTab`, `ValidationTab`, `LogsTab`) and `MainViewModel` still need to be extracted and translated.

---

## Template for Future Entries

## [X.Y.Z]

### Added
- Feature descriptions

### Changed
- Changes to existing functionality

### Deprecated
- Soon-to-be-removed features

### Removed
- Removed features

### Fixed
- Bug fixes

### Security
- Vulnerability patches
