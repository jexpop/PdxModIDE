# Changelog - PdxModIDE

All notable changes for the 1.5.x series of this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.5.9]
### Added
- **Name list in culture details (Cultures tab)**: the detail panel now shows the culture's name list (from `name_list = name_list_xxx` in the culture definition), with its localized display name and an expandable section with all its parameters grouped by category: **Options** (boolean flags like `dynasty_name_first`, `founder_named_dynasties`, `house_based_map_names`, `suggest_family_names`, `suggest_ancestor_names`, `always_use_patronym`), **Name lists** (`male_names`, `female_names`, `dynasty_names`, `cadet_dynasty_names`, `mercenary_names`), **Chances** (`pat_grf_name_chance`, `mat_grf_name_chance`, `father_name_chance`, `pat_grm_name_chance`, `mat_grm_name_chance`, `mother_name_chance`), **Prefixes &amp; suffixes** (`patronym_prefix_*`, `patronym_suffix_*`, `dynasty_of_location_prefix`, `bastard_dynasty_prefix`), and **Other** (`grammar_transform`). Definitions are parsed from `common/culture/name_lists/*.txt`, with mod files taking priority over the game. Each parameter includes a localized explanation (`NameListParam_*_Desc` keys in `en/es/ca.xaml`).
### Changed
- **Culture details panel order**: the Name list block is now placed after Traditions (at the end of the detail panel).
### Added
- **Traditions in culture details (Cultures tab)**: the detail panel now also shows the culture's traditions (from `traditions = { ... }` in the culture definition), each with its localized name (`tradition_<name>_name`), its localized description (`tradition_<name>_desc`), and an expandable section with its parameters (`category`, `layers`, `can_pick`, `can_pick_for_hybridization`, `parameters`, `character_modifier`, `province_modifier`, `county_modifier`, `culture_modifier`, `effects`, `cost`, `ai_will_do`, `desc`, etc.), mirroring the ethos/heritage/language/martial custom/head determination features. Definitions are parsed from `common/culture/traditions/*.txt`, with mod files taking priority over the game. Each parameter includes a localized explanation (`TraditionParam_*_Desc` keys in `CK3.*.xaml`).
- **`IGamePlugin.TraditionsRelativePath`**: new interface property for the traditions directory path (CK3: `common/culture/traditions`).
### Changed
- **Culture details panel layout**: the detail panel is now inside a vertical `ScrollViewer` so it no longer overflows the window when many sections are expanded. The statistics panel moved to the top of the right column and is only shown when no culture is selected. The gap between the culture tree and the detail panel was increased.

---

## [1.5.7]
### Added
- **Martial custom in culture details (Cultures tab)**: the detail panel now also shows the culture's martial custom (from `martial_custom = martial_custom_xxx`, localized via `martial_custom_<name>_name` keys in `cultural_traditions_l_*.yml`), with an expandable section with its parameters (`parameters`, `can_pick`, `ai_will_do`, etc.), mirroring the ethos/heritage/language features. Definitions are parsed from `common/culture/pillars/*martial_custom.txt`, with mod files taking priority over the game. Each parameter includes a localized explanation (`MartialCustomParam_*_Desc` keys in `CK3.*.xaml`).
- **Head determination in culture details (Cultures tab)**: the detail panel now also shows the culture's head determination (from `head_determination = head_determination_xxx`, localized via `head_determination_l_*.yml`), with an expandable section with its parameters (`head_determination_type`, etc.). Definitions are parsed from `common/culture/pillars/*head_determination.txt`, with mod files taking priority over the game. Each parameter includes a localized explanation (`HeadDeterminationParam_*_Desc` keys in `CK3.*.xaml`). Both new fields are placed after the language field, following the order of the culture definition file.

---

## [1.5.6]
### Added
- **Language in culture details (Cultures tab)**: the detail panel now also shows the culture's language details (from `language = language_xxx`, localized via `cultural_languages_l_*.yml`), with an expandable section with its parameters (`is_shown`, `ai_will_do`, `color`, etc.), mirroring the ethos/heritage features. Language definitions are parsed from `common/culture/pillars/*language.txt`, with mod files taking priority over the game. Each parameter includes a localized explanation (`LanguageParam_*_Desc` keys in `CK3.*.xaml`). The language field is placed at the end of the detail panel.

---

## [1.5.5]
### Added
- **Heritage in culture details (Cultures tab)**: the detail panel now also shows the culture's heritage details (from `heritage = heritage_xxx`), with an expandable section with its parameters (`is_shown`, `audio_parameter`, etc.), mirroring the ethos feature. Heritage definitions are parsed from `common/culture/pillars/*_heritage.txt`, with mod files taking priority over the game. Each parameter includes a localized explanation (`HeritageParam_*_Desc` keys in `CK3.*.xaml`).
### Changed
- **Culture details panel order**: fields are now ordered Source, Name, Color, Ethos, Heritage (from top to bottom).

---

## [1.5.4]
### Added
- **Ethos in culture details (Cultures tab)**: the detail panel now shows the culture's ethos (from `ethos = ethos_xxx`, localized via `cultural_traditions_l_*.yml`) and an expandable section with its parameters (`character_modifier`, `province_modifier`, `county_modifier`, `culture_modifier`, `parameters`, `ai_will_do`, `desc`, etc.). Ethos definitions are parsed from `common/culture/pillars/*_ethos.txt`, with mod files taking priority over the game. Each parameter includes a localized explanation (`EthosParam_*_Desc` keys in `CK3.*.xaml`).

---

## [1.5.3]
### Added
- **Culture color display (Cultures tab)**: the detail panel now shows the culture color as a numeric RGB value with a visual swatch. Supports `hsv`, `hsv360`, and `rgb` modes, and resolves `color = <name>` references against `common/named_colors/*.txt` (referenced colors display their source name).
### Changed
- **`CultureLoader` rewritten as a block-based parser**: `ParseCultureFile` no longer creates spurious entries for `color = { ... }` blocks; the color is assigned to the containing culture. Handles `hsv`/`hsv360`/`rgb` and integer/float RGB values.
- **Named color resolution in map cultures**: `LoadCultures` resolves `color = <name>` references against `common/named_colors/*.txt` from both the game and the mod roots, so every base and mod culture obtains a color.
- **Culture inheritance supports string character IDs**: eastern content (China/Japan/Korea, e.g. `holder = tuyuhun0006`, `japanese_yamato_1 = { ... }`) uses named character IDs instead of numeric ones. Title holder history and character culture parsing now accept both numeric and string IDs, so eastern provinces without a direct `culture =` inherit the county holder's culture correctly on the map.
### Fixed
- **Culture files with malformed lines (no `=`) stopped parsing**: lines like `khitan { ... }` interrupted the line-based parser, losing every subsequent culture. The block-based parser no longer stops.

---

## [1.5.2]
### Added
- **Cultures tab**: new tab to the right of Map that displays cultures grouped by heritage in a TreeView, prioritizing mod over base game data.
- **Culture file parsing**: Clausewitz parser reads `common/culture/cultures/*.txt` recursively, supporting nested blocks, comments, and complex value types (hsv, quoted strings).
- **Culture localization**: display names loaded from CK3 localization files (`cultures_l_*.yml` and `cultural_heritages_l_*.yml`) for English and Spanish; Catalan falls back to English.
- **Culture detail panel**: selecting a culture shows its localized name, heritage, and source (Base/Mod).
- **Statistics panel**: shows total heritage groups, groups with mod changes, mod cultures, and base cultures.
- **IGamePlugin.CulturesRelativePath**: new interface property for culture directory path (CK3: `common/culture/cultures`).
### Changed
- **Version updated to 1.5.2**: `MainWindow_Title` resource updated in all language files, Catalan version unified to 1.5.2.
### Fixed
- **Culture parser robustness**: `ExtractAttribute` now correctly skips values with trailing blocks (e.g. `color = hsv { 0.72 0.6 0.76 }`) instead of breaking.

---

## [1.5.1]
### Added
- **Cultural map view (full implementation)**: new Cultural view renders each province with its culture name and color, including Base/Mod/Ambos source priority and year-based history lookup.
- **Culture data loading**: new `CultureLoader` parses culture definitions (`common/culture/cultures/*.txt`), province history (`history/provinces/*.txt`), title holder history (`history/titles/*.txt`), and character cultures (`history/characters/*.txt`).
- **Culture inheritance**: provinces without explicit `culture =` in their history file inherit from the county title holder's culture (holder resolved from title history, culture from character history).
- **Culture localization**: culture display names loaded from CK3 localization files (`cultures_l_*.yml`) for all supported languages.
- **Inherited culture display format**: inherited cultures shown as `"Anglosajona (Chelmsford)"` with the capital province name in parentheses.
- **ShowNamesCheck visibility**: `ShowNamesCheck` moved outside `TitleModePanel` in XAML so it stays visible in Cultural view.
### Changed
- **Version updated to 1.5.1**: `MainWindow_Title` resource updated in all language files.
### Fixed
- **Culture parsing for nested date blocks**: parser now uses depth + dateStack to correctly capture `culture =` on separate lines inside date blocks in province history files.
- **Culture definition parsing at any depth**: detects culture blocks nested within culture groups using `BlockRe` and single-stack approach.
- **Float color parsing**: `TryParseFloatColor()` added to handle RGB values with decimal points in culture color definitions.
- **`Math.Clamp` crash avoidance**: `maxFontSize` computed as `Math.Max(8f, boxW * 0.3f)` instead of `Math.Clamp` to prevent crash when `boxW < 27`.
- **Map label visibility**: lowered map label bounding-box filter from `30x20` to `20x12` to show more culture labels on the map.

---

## [1.5.0]
### Added
- **Map view selector**: new dropdown (ComboBox) in the Map tab to switch between three views: General (terrain only, no Base/Mod overlay), Title (current title map behavior with holder/county/duchy/kingdom/empire modes and edit button), and Cultural (placeholder for future implementation).
- **View-specific UI visibility**: in General view, Base/Mod checkboxes, title mode panel, and edit button are hidden. In Title view, all controls are visible. In Cultural view, Base/Mod are visible but title modes and edit button are hidden.
- **Minimum source enforcement**: in non-General views, at least one of Base/Mod must always remain checked — unchecking the last active source is a no-op.
### Changed
- **Version updated to 1.5.0**: `MainWindow_Title` resource updated in all language files.