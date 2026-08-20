# Changelog - PdxModIDE

All notable changes for the 1.6.x series of this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.6.23]

### Changed

- **Parent culture selector sorted by display name (Cultures tab)**: the available options of the culture mother (`parents`) selector are now ordered alphabetically by their **localized display name** according to the app language (`GetCultureDisplayName` + `StringComparer.CurrentCultureIgnoreCase`) instead of by the raw culture id, so the visible order matches the language shown. The already-selected parents keep their order so the `parents = { ... }` block written to the file is not reordered.

---

## [1.6.22]

### Added

- **Heritage management (Cultures tab)**: a new "Heritages" sub-tab manages every heritage defined in `common/culture/pillars/*heritage.txt` (game + mod). The list shows each heritage with its localized name and source (`(Mod)`/`(Base)`), sorted alphabetically according to the current language; the first heritage is selected by default when the tab opens. The editor form has the id, an `audio_parameter` combo (options collected from the real heritages plus a typed fallback), the localized name and collective noun, and — for new heritages — the target file name.
- **Create heritage**: saving a new heritage writes the standard block `heritage_<id> = { type = heritage, is_shown = { heritage_is_shown_trigger = { HERITAGE = heritage_<id> } }, audio_parameter = X }` to a file under `common/culture/pillars/mod/`. The default file name comes from the new per-profile "Heritage file name" option (default `00_heritage.txt`); if the file already exists the block is inserted alphabetically, otherwise the file is created.
- **Edit heritage**: only mod-new heritages (files under `pillars/mod/`) are editable; base-game heritages are read-only. Saving rewrites the block in place in its source file.
- **Delete heritage**: only mod-new heritages can be deleted (confirmation dialog with the display name). Deleting removes the block, deletes the file when it ends up empty, and strips `heritage_<id>_name` and `heritage_<id>_collective_noun` from the mod's `cultural_heritages_l_<lang>.yml` files — under `localization/replace/` when the heritage exists in the base game, otherwise under `localization/`. The base game is never modified.
- **Heritage localization on save**: the name and collective noun are written to `culture/traditions/cultural_heritages_l_<lang>.yml` with the same selective per-field logic and translation providers as cultures, including the "translating…" status and the "localization saved" summary. After the translation finishes the list refreshes again so the translated name appears.
- **Live refresh**: after saving or deleting, the heritage list, the culture tree (grouped by heritage) and the culture editor heritage combo refresh immediately without restarting, showing the localized names.
- **Profile tab — "Heritage file name" option**: a per-profile option sets the default file name used when creating a new heritage, with a live preview of the resulting file name (no path) and a "Profile saved" feedback message after clicking Save.

### Changed

- `ProjectManager.MapToDomain` now guarantees the `heritage` file-name-prefix key (`00_heritage.txt` default) on loaded profiles, so the value persists and loads consistently.

### Fixed

- **Crash on heritage list refresh**: `RefreshHeritageList` mutated a plain bound `List` and reassigned the same instance as `ItemsSource`, desynchronizing the WPF item container generator (repeated `InvalidOperationException` that filled `logs/crash.log`). It now assigns a fresh sorted list on every refresh and restores/selects the item by name.
- **Profile "Heritage file name" change not saved**: the heritage preview did not refresh while typing (the setter did not notify `HeritageFileNamePreview`), so the change looked unsaved. The setter now notifies the preview, the profile-switch notifications include the heritage fields, and after saving the unsaved-changes highlight clears and the "Profile saved" message is shown.

---

## [1.6.21]

### Changed

- **Selective culture localization on save (Cultures tab)**: saving a new, copied or edited culture now translates and writes only the localization fields that actually changed (name, prefix, collective noun, history key/description), instead of always translating and upserting all of them. For a new culture, a field counts as changed only when it has text, so a completely empty localization section produces no translation at all (no translation-provider requests). If none of the fields changed, `SaveCultureLocalizationAsync` returns immediately without starting the translation. When editing or copying, a localization field that previously had content cannot be left blank: the save is blocked with a localized error listing the cleared fields (`CulturesTab_EditorLocBlank`). The `name → cultureId` default for new cultures was removed (a blank name is simply not written). The rest of the culture save (the culture file block) is unchanged.

---

## [1.6.20]

### Changed

- **Culture localization follows the `culture/` folder convention (Cultures tab)**: all localization reads and writes for the culture domain now respect the `culture/` subfolder used by the base game, both under `localization/{lang}/` and `localization/replace/{lang}/`. `LoadLocalization` searches recursively inside `localization/{lang}/culture/` and `localization/replace/{lang}/culture/` for the culture files (`cultures_l_*.yml`, `culture_history_l_*.yml`, `cultural_traditions_l_*.yml`, `cultural_heritages_l_*.yml`, `cultural_languages_l_*.yml`, `head_determination_l_*.yml`, `culture_name_lists_l_*.yml`, `culture_gfx_l_*.yml`), so mods that place them directly in the language folder (without `culture/`) were previously not read and the editor showed no current localization when editing. Saving now writes `cultures_l_{lang}.yml` inside `culture/` too (previously only `culture_history_l_{lang}.yml` used it), keeping both inside and outside `replace/` consistent.

---

## [1.6.19]

### Added

- **Name order convention (`name_order_convention`) in the culture editor (Cultures tab)**: the optional attribute that defines how character names are shown for a culture is now parsed into `CultureInfo.NameOrderConvention`. The editor has a combo box with the native presets (`default`, `dynasty_always_first`, `dynasty_first`, `japanese`) shown with their localized display names (from the `culture_aesthetics_naming_*` keys in `culture_gfx_l_*.yml`), plus a "Custom format" option that reveals a free-text field for formats with tokens such as `$DYNASTY$`, `$HOUSE$`, `$NAME$` and `$TIER$`. When editing a culture whose value is not a known preset, the custom field is preselected with the stored value. Saving writes `name_order_convention = <value>` after `name_list`, and unsaved changes are highlighted in red.
- **Name order convention in culture details**: cultures that define `name_order_convention` show a section in the Details panel with the localized preset name and the raw value.
- **`culture_gfx_l_*.yml` localization loaded**: `LoadLocalization` now loads the `culture_gfx` localization file, and resolves `$reference$` values so preset names display correctly (e.g. `japanese` → `dynasty_first`).

---

## [1.6.18]

### Added

- **DLC traditions (`dlc_tradition`) in the culture editor (Cultures tab)**: a new "DLC traditions" section, independent from the base Traditions section. Each row has the **trait** combo (DLC-only traditions) and a **fallback** combo (base-game traditions, optional) on its own line below. The items of both combos show the tradition's localized name and, inside the combo, its description; DLC traditions also show the required DLC in parentheses (e.g. `Coastal Warriors (DLC the_northern_lords)`). Saving writes one `dlc_tradition = { trait = ... requires_dlc_flag = ... fallback = ... }` block per row after the `traditions` block.
- **Automatic DLC detection**: `requires_dlc_flag` is no longer edited manually. It is derived automatically from the actual `dlc_tradition` usages in the loaded cultures (game + mod) and shown next to each DLC tradition; when a tradition is unknown, the value stored in the culture file is kept. DLC traditions are excluded from the base Traditions selector, and the `dlc_tradition` trait options are limited to DLC traditions (base traditions are only offered as fallback).
- **DLC traditions in culture details**: cultures that define `dlc_tradition` blocks show them in the Details panel as `trait (flag) → fallback`.

### Changed

- **DLC tradition classification**: a tradition is considered DLC when its definition file is not a base-game `00_*.txt` file, when its definition carries `requires_dlc_flag`, or when its id uses a known DLC prefix (`tradition_fp*_`, `tradition_ep*_`, `tradition_ce*_`, `tradition_tgp_`, `tradition_mpo_`, ...).

---

## [1.6.17]

### Changed

- **Delete culture extended (localization files)**: when deleting a culture removes its localization keys from the mod's `cultures_l_*.yml` / `culture_history_l_*.yml` files and a file is left without any remaining entry (only the `l_<lang>:` header, comments or blank lines), the file itself is now deleted instead of being left empty in place. Only the mod is affected; the base game is never modified.

---

## [1.6.16]

### Added

- **Lineage: `created` and `parents` (Cultures tab)**: the editor supports the optional `created` (creation date) and `parents` (list of parent culture IDs) attributes of a culture. `parents` uses a two-list selector (selected/available) with all culture keys of the game + mod, and is written as `parents = { ... }`. `created` is a text field validated as `Year.Month.Day` (a leading `-` is allowed for years before Christ); invalid values block the save with a localized error. Both are written after `color` and before `heritage`, matching the game file order.
- **Year offset per profile**: the `created` field works with the **calculated (real) date** while the files always store the **non-calculated value**. The offset comes from `Profile.YearOffset` (the same per-profile offset used by the History tab), applied automatically when loading, editing (a preview shows in real time the value that will be stored in the files) and saving. **New profiles now default to `YearOffset = 0`** (previously hardcoded `10000`), so every mod/profile keeps its own offset independently; existing profiles keep their saved value.
- **Lineage in culture details (Cultures tab)**: cultures that define `created` and/or `parents` show a new "Lineage" section in the Details panel with the creation date (`{calculated} ({file})`) and the parent cultures.
- **Localized culture names**: culture names in the parents selector and in the details are now shown in the **app language**, falling back to English and then to the raw key, instead of always showing the English identifier.

---

## [1.6.15]

### Added

- **History loc override (Cultures tab)**: a "History description" section in the editor lets you set the optional `history_loc_override` attribute of a culture, which overrides the culture history text the game shows for it. It has two fields: the **key** (the reference written into the culture block as `history_loc_override = <key>`) and the **description** (the actual text). When the description is filled but the key is empty, the key is auto-generated as `<cultureId>_history_loc` and written into the culture block, so the reference is never lost. The description is written to `culture/culture_history_l_<lang>.yml` (under `localization/replace/` when the culture exists in the base game, otherwise under `localization/`) and translated to every supported language with the existing translation providers. Existing values load from the localization when editing a culture. The section includes a short localized explanation of both fields.
- **History override in culture details**: cultures that define `history_loc_override` now show the key and its description in the Details panel.
- **Delete culture extended**: deleting a culture also removes its `history_loc_override` localization key from the mod's `culture_history_l_*.yml` files (inside or outside `replace/` depending on the culture). The base game is never modified.

### Changed

- `history_loc_override` is written **before the `traditions` block** in the culture file (it was written after `name_list`).

---

## [1.6.14]

### Added

- **Delete culture (Cultures tab)**: right-clicking a mod culture defined in `common/culture/cultures/mod` (or a subfolder) — the "green" ones — now offers a "Delete culture" option. A mandatory confirmation dialog shows the culture to delete (display name). Deleting removes the whole `cultureId = { ... }` block (with all its attributes) from the culture file in the mod; if the file ends up with no cultures left, the file itself is deleted. The mod localization entries for that culture (`cultureId`, `cultureId_prefix`, `cultureId_collective_noun`) are also removed from the mod's `cultures_l_<lang>.yml` files — under `localization/replace/` when the culture exists in the base game, otherwise under `localization/`. The base game is never modified. New localized status and confirmation messages were added in all languages.

---

## [1.6.13]

### Added

- **Automatic translation toggle (General settings → Translation)**: a new "Automatic translation" checkbox controls whether saving a culture translates its names into every CK3 language. When enabled (default), all CK3 language files are written — inside `localization/replace/` when the culture exists in the base game, otherwise under `localization/`. When disabled, only the app language is written (typed text, no translation) — in `localization/replace/` for base cultures, otherwise outside it — preparing the ground for a future manual translations section. Persisted through `Settings.AutoTranslate`.
- **Culture localization written for new cultures (Cultures tab)**: saving a new culture now always writes at least the culture name to the localization files (defaulting to the culture id when the "Culture name" field is empty), so new cultures get localization entries. The localized prefix and collective noun are written from the typed fields; those values are captured when the Save button is pressed so the editor reload triggered after saving cannot clear them.

### Changed

- **Heritage is now mandatory**: the culture editor refuses to save without a selected heritage (new localized message `CulturesTab_EditorNeedHeritage`).
- **Localization files sorted alphabetically**: `UpsertLocalizationFile` reorders the entries by key (case-insensitive) on every write, keeping the header line and comments at the top.
- **Ethnicity percentage validation relaxed**: the sum of the culture's ethnicities no longer has to equal 100%; it only must not exceed 100% (the game accepts lower totals). The validation error message was updated accordingly.

---

## [1.6.12]

### Added

- **Name list in the culture editor (Cultures tab)**: the editor now includes a "Name list" combo box right after the Traditions section. It lists every name list defined in `common/culture/name_lists` (game + mod), ordered by display name, and writes `name_list = <key>` matching the CK3 culture definition structure.
- **Graphics tags in the culture editor**: four new sections — Coat of arms (`coa_gfx`), Building (`building_gfx`), Clothing (`clothing_gfx`) and Unit (`unit_gfx`) — each with two side-by-side lists: "Added to this culture" (left, preserving order) and "Available" (right). The available lists load the distinct values used by every culture in the game and mod, and each row has its own button to move it between lists and to reorder (`↑`/`↓`). Saving writes the tags in the CK3 field order with `coa_gfx` first.
- **House coat-of-arms frame in the culture editor**: a "House coat-of-arms frame" combo box lists the available `house_coa_frame` values. When a frame is selected, the associated `house_coa_mask_offset` and `house_coa_mask_scale` (derived from the game's cultures) are shown read-only and written automatically; frames without a known mapping are saved with the frame only.
- **Ethnicities in the culture editor**: an "Ethnicities" section with one editable row per ethnicity: a percentage box and a combo box of the known ethnicity names (loaded from every culture in the game and mod, including any custom value found in the edited culture). Rows can be added ("Add ethnicity") and removed (`−`). Saving writes `ethnicities = { <weight> = <name> }` and validates that the percentages add up to 100 (tolerance ±0.5); rows left blank are ignored.
- **Automatic culture localization on save (Cultures tab)**: when saving a new or edited culture, the editor now translates the Name, Adjective (`_prefix`) and Collective noun (`_collective_noun`) into every CK3-supported language of the mod and writes them to the corresponding `cultures_l_<lang>.yml` localization files (under `localization/`, or `localization/replace/` when the culture already exists in the base game). Translations are produced by the configured translation provider(s); if a language fails to translate, the typed text is used as fallback. New status messages report translation progress, the number of languages saved, any fallback and any errors (`CulturesTab_EditorLocTranslating`, `CulturesTab_EditorLocSaved`, `CulturesTab_EditorLocError`, `CulturesTab_EditorLocFallback`).
- **Pluggable translation providers with random rotation and fallback**: translation is now handled through an `ITranslationProvider` abstraction with four implementations — MyMemory (default, no key), LibreTranslate (free, no key, configurable instance URL), Lingva (free, no key, configurable instance URL) and DeepL (free API key required). On each save the enabled providers are shuffled randomly and tried in order; the first successful translation wins, distributing load and providing automatic fallback if a provider is unavailable or rate-limited.
- **Translation settings (General settings)**: a new "Translation" section lets you enable/disable each provider (MyMemory is always on and locked), set the LibreTranslate/Lingva instance URL, and enter a DeepL API key with a "Validate" button that checks the key against the DeepL API. The selection is persisted in `data/settings.json` (`translationProviders`, `deeplApiKey`, `translationProviderUrls`).

### Changed

- **Editor combo boxes width**: the culture editor combo boxes are capped at half the panel width (`MaxWidth="500"`, left-aligned) so long lists do not stretch across the whole panel.
- **Save button fixed**: the Save culture button text was not visible because the `CulturesTab_EditorSave` localization key was missing; it is now defined in all languages, and the button (and Clear fields) have a minimum height and vertical padding so they are no longer clipped at the bottom in edit mode.
- **Color legend character fixed**: the `■` marker in the culture list color legend was corrupted (`â– `) by an encoding issue; the character was restored in `CulturesTab.xaml`.
- **Editor buttons disabled while translating**: the Save and Clear buttons (and a wait cursor) are now disabled during the localization translation and re-enabled when it finishes, preventing double-clicks.

---

## [1.6.11]

### Added

- **Traditions in the culture editor (Cultures tab)**: the editor now includes a "Traditions" section with two side-by-side lists: "Added to this culture" (left) and "Available" (right). The available list loads every tradition defined in `common/culture/traditions` (game + mod) ordered by display name, excluding those already added. Each row shows the tradition's display name and, when available, its localized description. Every row has its own button to move it between lists (`−` removes from the culture, `+` adds it); both lists scroll vertically with a reduced height. When editing or copying a culture, the source culture's traditions are preselected, and any change that is not saved is highlighted in red (same pattern as the rest of the editor fields).
- **Traditions written to culture files**: saving writes the `traditions = { ... }` block (one tradition key per line) right after `head_determination`, matching the CK3 culture definition structure.

### Changed

- **Culture editor button direction**: the "Traditions" add/remove buttons moved from the gap between the columns into each row, and the middle button column was removed.

---

## [1.6.10]

### Changed

- **Culture editor form layout (Cultures tab)**: the main group now follows the order Name → Color → Ethos → Heritage → Language → Martial custom → Head determination, matching the CK3 culture definition structure.
- **Culture name field**: the Name field is limited to 50 characters (`MaxLength`) and its input box is left-aligned with a reduced width.
- **Color as a picker**: the color field is no longer a free-text box. A preview swatch plus a "Choose color…" button opens the Windows color dialog. New cultures default to white. When editing/copying, the source culture's color (RGB or named colour reference) is preselected and the reference is preserved if not changed.
- **Ethos/Heritage in combo boxes**: Ethos and Heritage are now combo boxes listing the available definitions (localized display names, falling back to the definition key) including a "—" (none) option that is preselected for new cultures.
- **Language, Martial custom and Head determination fields added**: three new combo boxes with the same behaviour (values loaded from the corresponding definition files).
- **Color saved without the `rgb` keyword**: the color is written as `color = { r g b }` with normalized 0–1 values, matching the format used by the game files (the game never writes an explicit `rgb` keyword). Named colour references (`color = <name>`) are kept as-is when unchanged.

### Added

- **Unsaved changes highlighted in red**: when editing or copying a culture, fields whose value differs from the last saved state are shown in red (same pattern as the Profile tab) until the culture is saved.

---

## [1.6.9]

### Changed

- **Culture editor field labels and hints (Cultures tab)**: the main field is now labelled "Name" instead of "Identifier", and the editor hint no longer mentions that the culture is blocked when it already exists in the mod or that the identifier is locked in edit mode. In edit mode the hint reads "Modify the fields and press Save" instead of "Fill in the fields and press Save".
- **Edit mode hides the identifier and file name**: when editing an existing culture, the identifier field and the target file name row are no longer shown (both are read-only in edit mode); they remain visible when creating a new culture. The "Name" field label group no longer has a group header (the box border is kept).

### Removed

- **Culture editor "Name" group header**: the `CulturesTab_EditorName` localization key was removed (the field label now is "Name", localized as `CulturesTab_EditorCultureId`).

---

## [1.6.8]

### Added

- **Editable output file name in the culture editor (Cultures tab)**: when saving a new/copied culture, the target file name is now editable (defaults to `<prefix><id>.txt`). The name must end in `.txt`, otherwise a localized validation error is shown. The folder selector is restricted to `common\culture\cultures\mod` and its subfolders.

### Changed

- **Culture existence checked by content**: saving a new/copied culture now checks whether a culture with the same id already exists by reading the content of the culture files inside `common\culture\cultures\mod` (and subfolders), instead of relying on the file name. If the culture already exists in a file, the new block is inserted alphabetically in that file; otherwise a new file is created with the chosen name.
- **Culture id locked in edit mode**: when editing an existing culture, the id field is read-only so a culture cannot be renamed; save writes back to the culture's original file.
- **`CultureInfo` lookup generalized**: the raw key of a culture is now resolved through a file index first and a content scan as fallback, so editing locates the correct source file.

---

## [1.6.7]
### Added
- **Culture editor save (Cultures tab)**: the culture editor can now write culture files. New/copied cultures are saved as `<prefix><id>.txt` (using the profile file-naming prefix) inside `common\culture\cultures\mod` or any subfolder chosen with a folder selector (restricted to that folder). Editing a mod culture writes back to its original file. New cultures are only saved when the target file does not exist; a "Clear fields" button (new/copied mode) resets the form. After saving, the culture tree is refreshed so the new/edited culture appears in the list.
### Changed
- `CultureInfo` now tracks the source file (`SourceFile`) and the parsed key (`RawKey`) so editing can locate and rewrite the original file.

---

## [1.6.6]
### Added
- **File naming conventions (Profile tab)**: a new "File naming conventions" section lets you set a custom prefix for the files generated for culture files. A preview shows the final name (`<prefix>[culture name].txt`). The prefix is stored per profile and persisted with the profile.
### Changed
- **Profile settings no longer auto-save**: the profile paths (game/mod/backup roots), the year offset (Dates tab) and the "Show titles names" option (History tab) are no longer saved automatically. All changes are now saved with the "Save profile" button in the Profile tab.
- **Unsaved changes highlighted in red**: fields whose value differs from the last saved profile are shown in red (profile routes, culture file prefix, year offset and the "Show titles" checkbox) until the profile is saved.

---

## [1.6.5]
### Added
- **Ethnicities in the culture detail (Cultures tab)**: the detail panel now shows the culture's ethnicities (from `ethnicities = { <weight> = <ethnicity> ... }` at the end of the culture definition), each one on its own line in `name weight%` format (e.g. `caucasian_blond 25%`). The `weight` values are parsed as percentages as they appear in the game file. Ethnicity IDs have no localization in the game files, so they are shown as-is. The field is placed at the end of the detail panel, after the Graphics section.

---

## [1.6.4]
### Added
- **Culture editor (Cultures tab, in-memory)**: the Cultures tab now has two sub-tabs: the existing culture list and a new editor sub-tab (header "New culture" / "Edit: <name>") with fields for id, heritage, ethos, color and the building/clothing/unit `_gfx` tags. Opening it does not write any file yet (in-memory only).
- **Culture context menu**: right-clicking a culture shows a context menu with "Create culture copying this one" (available for every culture) and "Edit culture" (only shown when the selected culture is editable). Right-clicking a culture now selects it first.
- **Culture color legend (Cultures tab)**: a legend under the statistics shows the three source colors used in the list: base cultures (black), mod non-editable cultures (blue) and new/editable mod cultures (green).
### Changed
- **Culture list source colors**: culture names are now colored by their source: base game cultures in black (same as their group), mod cultures that are not editable in blue and new mod cultures (editable) in green, matching the new legend.
- **Editor sub-tab title initialized**: the editor sub-tab header is set to "New culture" when the tab loads, so it is visible from the start.
### Fixed
- **Culture names showed in black**: the `TreeView` item template applied to all levels, so the implicit `DataTemplate` for `CultureInfo` (which binds the source color) was never used. The child template is now nested in the `HierarchicalDataTemplate` and the source color is applied to the culture name.
- **"Edit culture" did nothing**: WPF does not select a `TreeViewItem` on right-click, so the selected item was not a `CultureInfo` and the handler returned early. A `PreviewMouseRightButtonDown` handler now selects the item under the mouse, and the context menu is cancelled when no culture is under the cursor.
- **Language files `es.xaml`/`ca.xaml` mojibake**: strings were double-encoded (UTF-8 interpreted as Windows-1252 and re-encoded), showing characters like `dinastÃ­a` instead of `dinastía`. Repaired the double-encoded sequences (accents and em-dashes/curly quotes) and normalized to a single UTF-8 BOM. `README.md` and the documentation/`CHANGELOG` files had the same corruption and were repaired too.

---

## [1.6.3]
### Added
- **Unit mesh grid resolved by the real CK3 chain (Cultures tab)**: `PdxUnitResolver` now resolves the `unit_gfx` tags of a culture through `common/graphical_unit_types/*.txt` (expanding group tags), the `entity_links` blocks (`00_{army,fleet,siege,travel}_entity_links.txt`, with `type`, `graphical_cultures`, `quality` and `entity`) and the unit `.asset` (`pdxmesh` + `meshsettings`) to the `.mesh` and its diffuse texture. The units grid shows each resolved mesh (army/fleet/siege/travel) with its diffuse, following the exact game resolution instead of the previous folder-prefix fallback.
- **`CulturesTab_Loading` localization key**: shared "Loading models…" message used by the three model sections.
### Changed
- **Model sections are independent and lazy-loaded**: the Building, Clothing and Unit grids now live each in its own `Expander`, collapsed by default, so models are not loaded when the tab opens or when switching cultures. Each section is resolved only the first time it is expanded; switching culture resets the sections, so re-expanding recalculates the current culture. A localized "Loading models…" indicator is shown while a section loads.
### Fixed
- **`unit_gfx` tags inside `graphical_cultures` blocks** are now parsed as block children (the entity links store them as bare tokens inside `{ }`), which previously yielded no unit meshes.
- **`@tier*_quality` macros**: the `quality` field in the entity links references file-level macros (`@tier2_quality = 2`, `@tier3_quality = 4`); the resolver now collects them so tier/quality resolves instead of staying 0.

---

## [1.6.2]
### Added
- **Deterministic clothing painter (`PdxClothingPainter`) (Cultures tab)**: the clothing grid now colors each garment by reconstructing the CK3 `portrait_attachment_pattern` shader offline (the binary `portrait.shader` is not shipped to users). `Paint(gameRoot, assetPath, meshPath)` decodes the base diffuse, the entity `pattern_mask` (RGBA), the variation's 4 colormasks and the 16-wide colour palette, sampling each active mask channel against its own colormask and indexing palette row 0 (deterministic hue family), weighting the tint and multiplying the base diffuse. Output is BGRA.
### Changed
- **Mesh UV-set-2 pattern sampling**: patterns are now sampled using the mesh's UV-set 2 (`u1`) instead of the diffuse UV0. `PdxClothingPainter.BuildXyzMapping(meshPath, pw, ph)` rasterizes the mesh triangles in UV0 space and interpolates uv1 per diffuse texel, falling back to UV0 when no mesh or triangle covers the texel. This makes the colored pattern follow the garment's real UV layout.
- **Colormask layout transform**: each channel's colormask UV is now transformed by its referenced `pattern_layout` (scale / rotation / offset) before sampling (`LoadLayouts`, `ParseLayoutBlock`, `ApplyLayout`), matching the game's "patterns are sampled using UV-set 2 with scale/rotation/offset" behaviour.
- **Clothing painter color reused in double-click preview**: the painted texture computed for the grid is also applied to the `MeshPreview` window opened on double-click, so the garment keeps its colored pattern everywhere.
### Fixed
- **Colormask paths resolved against the game root**: `ResolveTexture` now detects `gfx\`/`game\` relative paths in the accessory `.asset` and resolves them against the game root instead of the asset's folder, so the `pattern_mask` actually loads and the garment gets tinted (previously it was always gray).
- **Out-of-range pattern UVs handled**: mask and colormask indices are clamped to tolerate UV-set-2 coordinates outside [0,1), avoiding `IndexOutOfRangeException`.

---

## [1.6.1]
### Added
- **Full BC7/DX10 texture decode in the building grid (Cultures tab)**: `DdsDecoder` now decodes the DirectX10 (DX10/`DDS_HEADER_DXT10`) textures found on CK3 building meshes: all 8 BC7 modes (dxgi formats 98/99), as well as BC4 (74) and BC5 (76). BC6H (95/96) is detected and rejected with `NotSupportedException`. Previously these textures decoded as flat gray. This makes the `_unique.dds` textures used across ep3/tgp buildings display their correct colors.
### Changed
- **Per-mesh textured building grid (Cultures tab)**: the building grid now renders each submesh with its own diffuse + UV set + optional `_unique` texture resolved from the companion `.asset` (`texture = { file = "..._unique.dds" index = 5 }`), sampling the unique through UV1 and falling back to the diffuse atlas when there is no second UV set. Collision submeshes are skipped.
- **Fixed channel-order bug that made buildings appear blue**: pixel data from `DdsDecoder` is stored in RGBA order, but the WPF `BitmapSource` was built with `Bgra32`. `LoadTexture` now converts RGBA→BGRA before creating the bitmap, so red `_unique` textures (roofs, etc.) render red instead of blue.
- **Atlas + unique color blend (option 2)**: building submeshes now use the diffuse atlas (UV1) as the base detail texture tinted by the average color of the building's `_unique` texture via the `DiffuseMaterial`, replicating the game's `standard_atlas` shader (`Diffuse.rgb *= Unique`), with a 70% mix factor so detail stays visible.

---

## [1.6.0]
### Changed
- **Culture Coat-of-Arms preview (Cultures tab)**: the Coat-of-Arms block no longer renders a shield preview (neither 3D viewport nor image); it now shows only the GFX text (`coa_gfx = ...`). The 3D previews for `building_gfx`, `clothing_gfx` and `unit_gfx` remain unchanged.