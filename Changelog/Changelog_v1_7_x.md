# Changelog - PdxModIDE

All notable changes for the 1.7.x series of this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.7.1] - 2026-08-31

### Added
- **Language management (Cultures tab, parity with heritage)** — new **Languages** sub-tab (`LanguagesTabItem`) lists every language defined in `common/culture/pillars/*language.txt` (game + mod, recursively), each shown with its localized name and source (`(Mod)`/`(Base)`, green/blue/black like heritages), sorted alphabetically. Creating a new language writes the standard block `language_<id> = { type = language, is_shown = { language_is_shown_trigger = { LANGUAGE = language_<id> } }, ai_will_do = { value = X if = { limit = { has_cultural_pillar = language_<id> } multiply = Y } }, color = Z }` to a file under `common/culture/pillars/mod/` (default file name from the profile's "Language file name" option, `00_language.txt`; if the file already exists the block is inserted alphabetically, otherwise the file is created). Editing rewrites the block in place; only mod-new languages (files under `pillars/mod/`) are editable and deletable, base-game languages are read-only. Deleting removes the block (and the file when empty) and strips `language_<id>_name` from the mod's `cultural_languages_l_<lang>.yml` files — under `localization/replace/` when the language exists in the base game, otherwise under `localization/`.
- **Language color selector** — color field combines a named-color `ComboBox` (resolved from `common/named_colors/*.txt` via `LoadNamedColors`) with an `hsv` picker (`ColorDialog` → `hsv{ h s v }` invariant). Preview swatch updates for both named and `hsv` values; `UpdateLanguageColorPreview` handles `hsv{}` parsing with comma/dot tolerance. Editing a language loads its current `color` (named or `hsv`) into the combo and preview.
- **Optional ai_will_do (Languages)** — `ai_will_do` is now an optional section in the Gifts tab editor: `value` (base AI weight, default 10) and `multiply` (bonus when `has_cultural_pillar = language_<id>`, default 10) are two independent `TextBox` fields with explanatory hints (`LanguageAiSection/Hint/MultiplyHint`). Leaving both blank omits the entire `ai_will_do` block; filling one defaults the other to 10. `BuildLanguageBlock` generates the block conditionally and `ParseLanguageParameters` correctly reads `hsv{}` colors as a single token. Validation shows `LanguageAiInvalid` for non-integer input.
- **Language localization (single key)** — `cultural_languages_l_<lang>.yml` now correctly uses only `language_<id>_name:0 "Name"` (single entry, not `_collective_noun`), matching `game/localization/{lang}/culture/traditions/cultural_languages_l_*.yml` (`D:\GAMES\CONTENT\Crusader Kings III\game\localization\spanish\culture\traditions`). `SaveLanguageLocalizationAsync`/`DeleteLanguageLocalization` handle only `_name` with the same selective per-field logic and translation provider chain as heritages (`CulturesTab_EditorLocTranslating`/`EditorLocSaved`). `LoadLanguageDefinitions` now distinguishes `Mod`/`Base` with `Source`/`SourceFile`/`IsModNew` and tracks `_baseLanguageRawKeys`.
- **Language file name per profile** — new `FileNamePrefixes["language"]` (`00_language.txt` default) in `ProjectManager.CreateProfile`/`MapToDomain`, exposed as `LanguageFileName`/`LanguageFileNamePreview` in `MainViewModel` and editable in `ProfileTab` (new row `LanguageFileName`). `RefreshAfterLanguageChange` reloads definitions, localization, culture tree and editor combo after any save/delete.
- i18n: new keys `ProfileTab_LanguageFileName`, `CulturesTab_SubTabLanguages`, `LanguageNew/Editor/Id/Color/ColorHint/ColorInvalid/Save/Delete/NewTitle/EditTitle/ReadOnly/NeedId/IdInvalid/NeedColor/Exists/DeleteConfirm/DeleteNotAllowed/Deleted`, `LanguageAiSection/Hint/Value/Multiply/MultiplyHint/AiInvalid`, `LanguageLocName` in `en.xaml`/`es.xaml`/`ca.xaml` (with `CK3.*.xaml` `LanguageParam_*_Desc` already present).

### Changed
- Updated application title to version 1.7.1 in all language files (en, es, ca)
- `LanguageInfo` extended with `Source`, `SourceFile`, `IsModNew`, `Color` and `SourceBrush` (parity with `HeritageInfo`)
- `ParseLanguageParameters` now correctly captures `hsv{ 0.6 0.5 0.7 }` / `rgb{}` as a single `color` token instead of truncating to `hsv{`
- Picker now generates `hsv` with `InvariantCulture` (`0.5` not `0,5`) and normalizes commas to dots on save; preview handles both.

### Fixed
- Fixed `App.xaml.cs:14` `CS8618` warning by initializing `_logPath = ""` (0 warnings)
- Fixed language color display truncated to `hsv{` in editor/details
- Fixed language picker generating `hsv` with locale comma (e.g. `0,5`) which failed validation and file format
- Fixed `LanguageColorInvalid` feedback when `hsv{}` is incomplete or malformed

---

## [1.7.0] - 2026-08-24

### Added
- New **Terrain** map view mode in History tab to visualize CK3 provinces colored by terrain type (plains, hills, mountains, forest, desert, wetlands, taiga, farmlands, floodplains, oasis, desert mountains, coastal sea, sea)
- Terrain data loading from CK3 game files:
  - `common/terrain_types/00_terrains.txt` — terrain HSV color definitions (converted to RGB)
  - `common/province_terrain/00_province_terrain.txt` — province-to-terrain mappings
- HSV to RGB color conversion with locale-invariant decimal parsing
- Localized terrain type names in English, Spanish, and Catalan for info panel display
- Trace logging to `logs/debug.log` for terrain loading diagnostics (works in Release builds)

### Changed
- Updated application title to version 1.7.0 in all language files (en, es, ca)
- View selector combo box now properly handles "Terrain" tag

### Fixed
- Fixed ComboBox SelectionChanged handler missing "Terrain" case (was defaulting to General view)
- Fixed MapRenderer.SetHolderMode not updating GPU shader holderLut texture (caused single-color terrain display)
- Fixed HSV color regex to handle 3 or 4 values (optional alpha channel)
- Fixed province terrain parser to only read `00_province_terrain.txt` (ignoring `01_province_properties.txt` which has different format)
- Removed HasActiveSource() requirement for Terrain view (terrain is base game data, not mod-dependent)
- UI now hides Base/Mod checkboxes when Terrain view is active
- Fixed duplicate localization key `HistoryTab_NoTerrainData`

### Technical
- MapLoader: Added `ProvinceToTerrain` dictionary, `TerrainColors` dictionary, `LoadTerrain()`, `BuildTerrainLut()`, `BuildTerrainPalette()`, `HsvToRgb()`
- MapRenderer: Added `_holderLutImage` field and GPU texture update in `SetHolderMode()`
- HistoryTab: Added `MapViewType.Terrain` enum, `ApplyTerrainMode()`, UI state management for terrain view
- App.xaml.cs: Added file-based trace logging for debug output in Release builds