# Changelog - PdxModIDE

All notable changes for the 1.6.x series of this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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