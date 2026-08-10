# Changelog - PdxModIDE

All notable changes for the 1.1.x series of this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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