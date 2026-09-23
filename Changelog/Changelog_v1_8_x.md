# Changelog - PdxModIDE

All notable changes for the 1.8.x series of this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.8.0]

### Added
- **Bookmarks tab (MVP read-only, 1.8.0)** — new top-level tab `Bookmarks` / `Marcadores` / `Marcadors` (`MainWindow_Tab_Bookmarks`) between Cultures and Dates. Two-section TreeView:
  - **Mod bookmarks** (`BookmarksTab_SectionMod`): shows **all mod groups even when empty**, but hides base-only groups that have no mod bookmarks. Bookmarks with `Source=="Mod"` (merged `mod` overrides `base`; base bookmark with same key is not duplicated). `_ungrouped` node appears only when there are mod bookmarks without a `group`.
  - **Base game bookmarks (informative)** (`BookmarksTab_SectionBase`, read-only, gray): full `game` catalogue (`common/bookmarks/bookmarks/*.txt` + `common/bookmarks/groups/*.txt`) without merge filtering, for reference. `_ungrouped` appears only when there are base bookmarks without a group.
  - Both sections sort **groups chronologically by `default_start_date`** (`TryParseDate` y/m/d, `_ungrouped` last, tie-break by display name) and bookmarks inside each group by `start_date` + name. Stats panel shows groups / total / mod / base counts. Detail panels for bookmark (`start_date`, `group`, `is_playable`, `recommended`, `requires_dlc_flag`, `character = { }` list with sub-characters) and group (`default_start_date`, raw block). Cross-selection clears the other tree.
- **BookmarkLoader** (`PdxModIDE.MapEngine/BookmarkLoader.cs`): new `BookmarkInfo` / `BookmarkCharacter` / `BookmarkGroupInfo` model; parsers for `common/bookmarks/bookmarks/*.txt` and `common/bookmarks/groups/*.txt` (Clausewitz block parser, handles fallback flat `common/bookmarks/`, skips `_*.info`); `LoadGroups` / `LoadBookmarks` / `LoadMergedBookmarks(gameRoot, modRoot)` with `Source` / `IsModNew` and `DisplayName` resolution via `LoadBookmarkLocalization` (`localization/<ck3Lang>/**/*.yml` including `bookmarks/` subfolders, mod overwrites game). Future-ready for `ITranslationProvider` on write (not yet used — read-only MVP).
- **Bookmarks tab UI** (`PdxModIDE.UI/BookmarksTab.xaml` + `BookmarksTab.xaml.cs`): `BookmarkGroupVm` / `BookmarkVm` / `CharacterVm` view-models, `SourceBrush` (black Base / blue Mod), `BuildTrees()` with date ordering and filtering as above, `ShowBookmark` / `ShowGroup` detail renderers.
- i18n: ~25 keys `BookmarksTab_*` (`Refresh`, `BookmarksCount`, `Stats`, `Groups`, `Bookmarks`, `ModBookmarks`, `BaseBookmarks`, `Legend*`, `Details`, `SelectPrompt`, `Source*`, `BookmarkName`, `StartDate`, `Group`, `IsPlayable`, `Recommended`, `RequiresDlc`, `Characters`, `RawBlock`, `GroupDetails`, `GroupName`, `DefaultStartDate`, `NoProfile`, `NoGameRoot`, `LoadError`, `Ungrouped*`, `SectionMod`, `SectionBase`, `ModEmpty`, `BaseEmpty`) in `en.xaml` / `es.xaml` / `ca.xaml`.

### Changed
- Updated application title to version 1.8.0 in all language files (en, es, ca) — `MainWindow_Title`.

---

## [1.8.1]

### Added
- **Map date driven by bookmarks (1.8.1)** — `HistoryTab` no longer has an editable `YearBox`. New grouped control `BookmarkDateBorder` (`Group:` + `Bookmark:` + `Date:` + `Offset`) shows the date read-only and a two-level bookmark selector (group → bookmark). First-level `BookmarkGroupCombo` and second-level `BookmarkCombo` are ordered chronologically (`default_start_date` / `start_date`), filtered to hide groups without markers. `BookmarkGroupCombo_SelectionChanged` refreshes the bookmark list; `BookmarkCombo_SelectionChanged` sets `_currentFullDate` / `_currentYear`, updates `DateLabel` (`867.1.1`) and `OffsetLabel` (`year+offset.M.D`) and calls `ReapplyActiveMode()`.

### Changed
- **HistoryTab layout** — `WrapPanel Grid.Row="0"` split into `StackPanel` with two `WrapPanel` rows: first line `Zoom / Fit / BookmarkDateBorder / ViewSelector / TitleModePanel / ShowNames / ModeToggle / Split`; second line `Base / Mod` (as requested, `Base` and `Mod` on a second line).
- **Bookmark loading respects source mode** — `LoadBookmarkCombos()` checks `BaseSourceCheck` / `ModSourceCheck`: both → `LoadMergedBookmarks` (mod priority), only base → `LoadGroups/LoadBookmarks(game)`, only mod → `LoadGroups/LoadBookmarks(mod)`. `SourceModeChanged` now reloads the combos. Empty groups are hidden (`Where(groupsWithMarkers)`).
- **Bookmark combos hidden in General/Terrain** — `UpdateBookmarkDateBorderVisibility()` (`General`/`Terrain` → `Collapsed`, else `Visible`) called from `UpdateEditModeState()` and at the end of `SwitchToView()`.
- Updated application title to version 1.8.1 in all language files (en, es, ca) — `MainWindow_Title`.

### Fixed
- Added `_yearBoxShim` (`TextBox` shim) so legacy `int.TryParse(YearBox.Text)` code (culture editing, holder LUT) keeps working after `YearBox` removal from XAML; `UpdateDateDisplays()` syncs the shim.

### i18n
- New keys `HistoryTab_Date`, `HistoryTab_BookmarkGroup` / `BookmarkGroupTooltip`, `HistoryTab_BookmarkLabel` / `BookmarkTooltip` in `en/es/ca.xaml`.

---

## [1.8.2]

### Changed
- **HistoryTab layout — Base/Mod at same level as titles (1.8.2)** — `Base`/`Mod` moved from a second line below to the same line as titles (`Holder / County / Duchy / Kingdom / Empire`), separated by a vertical `Separator`, as requested. First attempt used a `StackPanel` with two `WrapPanel` rows; final layout uses a single `WrapPanel` (1.8.2 first iteration) and then a `StackPanel` with `Row0 Height="70"` fixed to keep the map from jumping between layers (General/Terrain collapsed vs Title/Cultural populated). `UpdateBookmarkDateBorderVisibility()` keeps `BookmarkDateBorder` hidden in `General`/`Terrain`.

### Fixed
- **Stable map height** — `Grid.RowDefinitions Row0 Height="70"` (top bar fixed) guarantees two-row space even when the second row is empty, so the map does not shift when switching between `General`/`Terrain` and `Title`/`Cultural`.
- Updated application title to version 1.8.2 in all language files (en, es, ca) — `MainWindow_Title`.

---

## [1.8.3]

### Changed
- **HistoryTab ViewSelector position (1.8.3)** — `ViewSelector` (layer combo `General` / `Title` / `Cultural` / `Terrain`) moved to the first row next to `Zoom` / `Fit`, removing the `Separator` that was before it. New order: `Zoom | Fit | ViewSelector | BookmarkDateBorder | Separator | Base/Mod | Titles…`. No logic or i18n changes.

### Fixed
- Updated application title to version 1.8.3 in all language files (en, es, ca) — `MainWindow_Title`.

---

## [1.8.4]

### Changed
- **HistoryTab ShowNames position (1.8.4)** — `ShowNamesCheck` moved to the first position of the second row, before `Base` / `Mod` and `TitleModePanel`. Order now: `ShowNames | Base | Mod | Holder/County/Duchy/Kingdom/Empire`.
- **Stable two-row header (1.8.4)** — `Grid.Row0 Height="70"` and both `WrapPanel Height="30"` fixed, so the two-row block does not resize with content (`BookmarkDateBorder` hidden in General/Terrain vs visible in Title/Cultural, `Base/Mod` collapsed vs visible). Map no longer shifts between layers.

### Fixed
- Updated application title to version 1.8.4 in all language files (en, es, ca) — `MainWindow_Title`.

