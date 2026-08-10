# Changelog - PdxModIDE

All notable changes for the 1.4.x series of this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.4.18]
### Fixed
- **County colors correct for all counties (not just first 255)**: the LUT for all map overlay modes (Holder, County, Duchy, Kingdom, Empire) was upgraded from `byte[]` (256 max entries, wrap-around at 255) to `ushort[]` (65535 max entries, no wrap-around). Palettes are now dynamically sized instead of fixed at 256 entries. This fixes an issue where counties with indices >255 would overwrite earlier counties' entries in `indexToCounty`, causing the overwritten counties to display another county's color.
### Changed
- **All Build*Lut methods now return `ushort[]`**: `BuildHolderLut`, `BuildCombinedHolderLut`, `BuildCountyLut`, `BuildDuchyLut`, `BuildKingdomLut`, `BuildEmpireLut` all use 16-bit LUT values, removing the `(idx-1)%255+1` wrap-around logic.
- **Palette builders use dynamic size**: `BuildHolderPalette` and `BuildCountyPalette` create bitmaps sized to the actual max index instead of a fixed 256×1.

---

## [1.4.17]
### Added
- **County map mode uses actual landed_titles colors**: the County overlay mode now reads the `color = { r g b }` attribute from `common/landed_titles/*.txt` files and displays those colors on the map instead of procedural index-based colors (golden angle HSL).
### Changed
- **Color loading priority for titles**: colors are loaded with the following priority: `<modRoot>/common/landed_titles/mod/` (highest), then `<modRoot>/common/landed_titles/` (mod root), then `<gameRoot>/common/landed_titles/` (game base). Commented lines (`#color = { ... }`) are ignored. Counties without a defined `color = { ... }` in any source fall back to the procedural HueSatLum color.

---

## [1.4.16]
### Added
- **Default title and county keys in Split County window**: the "Title key" field now defaults to the first barony's name with `b_` replaced by `d_`, and the "County key" field defaults with `b_` replaced by `c_`.
### Changed
- **`##MOD_DEL` comments simplified**: the prefix no longer includes the new title key. Each commented line now starts with `##MOD_DEL ` followed only by the original line content.
- **New title file includes parent references**: the new title file now shows the original parent title as a comment (`#`) next to the new title header, and the original county key as a comment next to the new county header.
### Fixed
- **Split County no longer overwrites existing title files**: when the target title file (`d_xxx.txt` etc.) already exists in the mod directory, the new county is now appended inside the existing file instead of overwriting it.
- **Duplicate county detection in Split County**: the application now checks whether the new county key (`c_xxx`) already exists in any `.txt` file under the mod's `common/landed_titles/` directory, ignoring lines commented with `##MOD_DEL`. If the existing block is active, the operation is aborted with an error. If the block is dead (all content is `##MOD_DEL`), the operation proceeds and the dead block is marked with `##MOD_DEL`.
- **Title-level duplicate detection in Split County**: the application now checks whether the new title file (`d_xxx.txt` etc.) already exists in `common/landed_titles/`. If the file contains active content, the operation is aborted. If all its content is `##MOD_DEL`, it is allowed and the empty block is marked.
- **Empty original county cleanup**: after a split, if the original county is left with no active baronies, the entire county block is marked with `##MOD_DEL`.
- **Allow split when county/title key matches origin**: `KeyExists` is bypassed when the new county key matches the original (`newCountyKey == _countyKey`) and `WouldBlockRemainActive` confirms the original would be empty. Same for the title key when it matches the parent (`newTitleKey == _parentTitle`), where the new county is appended to the existing parent block in the source file instead of creating a mod override file.
- **`WouldBlockRemainActive` without file path restriction**: the check now runs regardless of which file `FindBlockInLandedTitles` found the block in.
- **`##MOD_DEL` lines filtered in new county files**: lines starting with `##MOD_DEL` from the original county's attributes are no longer copied into the newly created county file.
- **Original county marked with `##MOD_DEL` in CopiedFromGame same-origin split**: when splitting a game-origin county with the same county key, the original county block is now properly marked as dead in the mod copy.

---

## [1.4.15]
### Fixed
- **CS8625 — null passed to non-nullable parameter in `BuildCountyLut`**: changed `TitleHistoryLoader history` parameter to nullable `TitleHistoryLoader?` to allow the intentional null sentinel.
- **CS0414 — unused field `_lastHolderYear` in `MapRenderer`**: removed the field that was assigned but never read.
- **CS8602 — possible null dereference of `BaseSourceCheck`/`ModSourceCheck`**: added null-forgiving operator (`!`) on WPF control references guaranteed to be initialized by XAML.
- **CS8604 — possible null argument to `HashSet<string>.Contains`**: added explicit `prov.Type == null` guard before calling `Contains` on the land types set.
### Changed
- **Build warning-free**: solution now compiles with 0 warnings (down from 4).
- **Target folder picker in Split County window**: added a "Target folder" field with a Browse button that opens a folder selector rooted at `{ModRoot}/common/landed_titles/mod/`. The user can choose any subdirectory to write the new title file.

---

## [1.4.14]
### Added
- **Mode status indicator**: a centered label at the top of the main window shows the current mode (View/Edit), active hierarchy level (County, Duchy, etc.), and source (Base/Mod). Hidden when the Map tab is not active or no source is selected.
### Changed
- **"Mode View" / "Mode Edit" button renamed to show action**: the toggle button now displays "Go to Edit Mode" / "Go to View Mode" instead of the current mode name. Widened to 140px. Tooltip now shows the current mode name.
- **Split county preserves full barony and county data**: barony blocks are now parsed with proper brace-depth tracking. The new title file includes the full original barony blocks (attributes like `color`, `cultural_names`, etc.) and county-level attributes. The original county's attributes (except `capital`) are carried over to the new county.
- **`##MOD_DEL` comments cleaned**: no indentation preserved before `##MOD_DEL` markers. Empty/whitespace lines inside commented blocks are kept as-is without the prefix.
### Fixed
- **Map updates immediately after split**: `MapLoader.LoadModLandedTitles` is called after a successful split so the title hierarchy dictionaries reflect the changes. No application restart needed.
- **Map hierarchy integration is now updated in real time**: switching tabs and returning to the Map tab restores the mode status label.

---

## [1.4.13]
### Added
- **Split County window shows selected provinces with hierarchy**: clicking the "Split County" button now opens a new window (`SplitCountyWindow`) listing each selected province with its Province ID, Barony, County, and immediate higher title (duchy). Data is obtained directly from the loaded `MapLoader` hierarchy (CountyToDuchy).
- **MainWindow title now uses localization**: the window title "Paradox Mod IDE v.1.4.13" is now loaded from the language resource dictionaries via `{DynamicResource MainWindow_Title}`.

---

## [1.4.12]
### Added
- **"Split County" button in Map tab edit mode**: when in Edit mode with County view selected, a "Split County" button appears at the top when one or more land provinces of the same county are selected. The button uses localized text (EN/ES/CA).

---

## [1.4.11]
### Added
- **Multi-province selection in edit mode (History map)**: in Edit mode, clicking land provinces now toggles them into a multi-selection set. The info panel shows combined values when all selected provinces agree, or "(Multiple)" when they differ. Clicking a non-land province clears the selection and selects only that province. Clicking empty space clears all selections.
### Changed
- **Edit mode preserves title overlay and names**: the title overlay (holder/county/duchy/kingdom/empire) and "Show names" labels remain active on the map when entering Edit mode, using the last selected mode. The "Show names" checkbox is always visible; title mode checkboxes are hidden in Edit mode.
- **Mode toggle button now respects language setting**: the "Mode View" / "Mode Edit" button text and tooltip use `DynamicResource` resources (`HistoryTab_ModeView/Edit` and tooltip keys) available in EN, ES, and CA.
### Fixed
- **Multi-province highlight preserves borders**: the CPU highlight pass now skips border pixels, so province borders remain visible between selected provinces.
- **Non-land province no longer stays highlighted after clicking land**: when a non-land province is selected and then a land province is clicked, the non-land is removed from the selection set.

---

## [1.4.10]

---

## [1.4.9]
### Added
- **Info panel now shows full title hierarchy and holder names with dynasty**: the History tab province info panel replaces the old "Holder/Liege" rows with Duchy, Kingdom, and Empire level rows. Each level shows the character name resolved from `history/characters/*.txt` with dynasty surname from `common/dynasties/*.txt`, falling back to raw ID if not found.
- **Character and dynasty loaders**: new `CharacterHistoryLoader.cs` and `DynastyLoader.cs` parse character names and dynasty display names (including localized `.yml` files) from both the base game and mod directories.
- **On-map holder labels now show character names**: holder mode labels on the History map render the resolved character name (with dynasty) instead of the localized title name.
### Changed
- **Window title updated to "Paradox Mod IDE v.1.4.9"**: `MainWindow.xaml` title reflects the new version.
- **Province info panel refreshes on overlay mode switch**: all `Apply*Mode` methods now call `UpdateProvinceInfo(_lastProvinceId)` so the info panel updates immediately when toggling overlay modes.

---

## [1.4.8]
### Changed
- **Map labels now scale with province size**: province name labels on the History map are now rendered with a font size proportional to the province bounding box (`boxW * 0.14`, clamp 8px–30% of box width). Text automatically shrinks if it exceeds 85% of the province width.
- **Map label colors and style improved**: text fill changed from solid white-on-black-rectangle to dark grey (#666) drawn 3 times for boldness, with a semi-transparent white border outline (`SKColor(255,255,255,200)`) for a clean CK3-style look, removing the opaque black background rectangle.
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

---

## [1.4.7]
### Added
- **"Find unconfigured date modules" button in Validation tab**: new button that recursively scans the game root directory looking for folders not yet configured as modules that contain files with date patterns. Results are shown in an informational dialog (no changes are made to any configuration). Uses `Parallel.ForEach` and line-by-line reading with early exit for optimal performance, skipping files over 1 MB.
### Changed
- **Module validation no longer recurses into subdirectories**: both "All Modules" and single-module validation in the Validation tab now only list files directly in the module path without descending into subdirectories (`SearchOption.TopDirectoryOnly`). This makes validation consistent with the non-recursive processing introduced in 1.4.6 for the Dates tab.
### Fixed
- **Underscore characters (`_`) hidden in Dates tab module names**: WPF `CheckBox.Content` interprets underscores as access key mnemonics, hiding them. Module names like `common/landed_titles` appeared as `common/landedtitles`. Fixed by using a `TextBlock` inside the `CheckBox` instead of setting `Content` directly.
- **Module list in Dates tab limited to 6 columns**: the dynamic column count in `RecalculateLayout()` had no upper limit, causing text overlap at 7 columns. Capped at 6 columns.
- **Newly added modules not processed until app restart**: `ModuleProcessor._moduleCache` was never invalidated after adding, updating, or deleting modules, so new modules were invisible to the processing pipeline. Added `_moduleProcessor.InvalidateCache()` after each CRUD operation.

---

## [1.4.6]
### Changed
- **Non-recursive module processing in Dates tab**: the Dates tab now only processes files directly in the module path without recursing into subdirectories. Added `bool recurseSubdirectories` parameter threaded through `ProcessModulesAsync` / `ProcessModule` to control recursion behavior.
### Removed
- **Auto-backup on module processing**: removed automatic backup copies to the profile's backup folder during module processing. Backups are now a manual operation.

---

## [1.4.5]
### Added
- **Safe file versioning on module processing**: when a destination file already exists in the mod directory, it is now renamed with a `_v1`, `_v2`, etc. suffix instead of being overwritten. The new file keeps the original name. If the existing file content is identical to the new content, neither renaming nor writing occurs.
### Fixed
- **Duplicate "Process Complete" message box**: removed redundant `MessageBox.Show` in `DatesTab.xaml.cs` that caused two confirmation dialogs to appear after processing modules from the Dates tab.

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