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

---

## [1.8.5]

### Changed
- **HistoryTab ShowNames first (1.8.5)** — `ShowNamesCheck` moved to the first position of the second row, before `Base` / `Mod`. Second row order now: `ShowNames | Base | Mod | ModeToggle | Holder/County/Duchy/Kingdom/Empire`.
- **HistoryTab ModeToggle next to Base/Mod (1.8.5)** — `ModeToggleButton` (View/Edit) moved immediately after `Mod`, always next to `Base`/`Mod` instead of at the end of the second row.
- **HistoryTab stable header (1.8.5)** — second `WrapPanel Height="30"` + `StackPanel MinHeight` / `Grid Row0 Height="70"` fixes to keep the two-row header from resizing with content and to keep the map from shifting between `General`/`Terrain` and `Title`/`Cultural`.

### Fixed
- **ModeToggle visibility on first Title entry (1.8.5)** — `UpdateEditModeState()` now forces `ModeToggleButton.Visibility = Visible` (and `IsEnabled = modActive`) in the Title layer (both view and edit modes); previously it stayed `Collapsed` inherited from `General` on first entry.
- Updated application title to version 1.8.5 in all language files (en, es, ca) — `MainWindow_Title`.

---

## [1.8.6]

### Fixed
- **Bookmark group/marker refresh (1.8.6)** — `LoadBookmarkCombos()` now releases `_bookmarkLoading` before setting `BookmarkGroupCombo.SelectedIndex`, so `BookmarkGroupCombo_SelectionChanged` → `RefreshBookmarkComboForSelectedGroup()` → `BookmarkCombo.SelectedIndex = 0` → `BookmarkCombo_SelectionChanged` → `UpdateDateDisplays()` / `ReapplyActiveMode()` always fires. Changing group now shows its first marker and repaints the map.
- Updated application title to version 1.8.6 in all language files (en, es, ca) — `MainWindow_Title`.

---

## [1.8.7]

### Removed
- **Bookmarks refresh button (1.8.7)** — removed `BtnRefresh` (`BookmarksTab_Refresh`) from `BookmarksTab.xaml` (`Grid.Row="0"` now only `StatsText`) and its `BtnRefresh_Click` handler; reload is automatic on profile/language/source change.

### Fixed
- Updated application title to version 1.8.7 in all language files (en, es, ca) — `MainWindow_Title`.

---

## [1.8.8]

### Added
- **Bookmarks CRUD for groups and markers (1.8.8)** — `BookmarksTab` now has three sub-tabs: `List`, `New group` (always visible) and `Bookmarks` editor (on demand). Context menu on the Mod tree: `New group`, `New bookmark`, `Create by copying`, `Edit`, `Delete` (gated to `Source=="Mod"`; base is read-only and hidden on key clash). Groups saved to `common/bookmarks/groups/<profile file>` (default `00_bookmark_groups.txt`) and bookmarks to `common/bookmarks/bookmarks/<profile file>` (default `00_bookmarks.txt`); both file names configurable per profile in `ProfileTab` (`FileNamePrefixes["bookmark_group"/"bookmark"]`, persisted in `data/profiles.json`).
- **Group dates with offset (1.8.8)** — the group form takes the real date; the file stores `real + Profile.YearOffset` (`ShiftDate`, BC `-year` supported) with a `# real -> file (offset N)` reference comment. New groups are inserted chronologically (`InsertGroupChronologically` by `default_start_date`), not appended; editing the date reorders (delete + chronological insert). The editor shows the real date (`file - offset`).
- **Group localization via providers (1.8.8)** — `SaveGroupLocalizationIfChanged` writes the `bm_group_*` key to `localization/replace/<lang>/bookmarks_l_<lang>.yml` in the app language plus all CK3 languages via the `ITranslationProvider` chain when `AutoTranslate` is on. Group `common/` ids are normalized to lowercase English with spaces to `_` and no quotes (`NormalizeFileId`).
- **Validation and UX (1.8.8)** — required-field validation with `BookmarksTab_EditorFieldRequired: {0}` for group (id, name, date) and bookmark (id, start date, group, character name/history/title/culture/religion); `RequiresDlc` stays optional. Id is read-only when editing. `Clear` keeps the current title (`New...` vs `Edit...:`) and restores saved values in edit mode. Editor sub-tabs start collapsed and only appear on demand, except `New group`. Per-click diagnostics in `logs/bookmarks_debug.log` (plus `Documents/PdxModIDE_logs` fallback) and `EditorSave_Click` returns success so localization only runs after the `.txt` write.

### Changed
- `BookmarkLoader` extended with file helpers (`SanitizeFileName`, `BlockExists`, `DeleteBlock` with preceding `#` cleanup, `CountBlocks`, `ReplaceBlock` with `#` cleanup, `InsertBlockAlphabetically`, `BuildGroupBlock/WithOffset`, `BuildBookmarkBlock`, `ShiftDate`, `CompareDates`, `InsertGroupChronologically`).
- `DeleteGroup` also strips the group key from `localization/replace/**/bookmarks_l_*.yml` (plus legacy `localization/**`), deleting empty files.

### Fixed
- Fixed new-group save going through the edit branch (`_editorGroup==null` → `NullReference`, masked by localization status); `EditorSave_Click` now returns `bool` and `GroupEditorSave_Click` only localizes on success. Removed unreachable-code `CS0162`.
- Fixed negative (BC) dates in `ShiftDate`/`TryParseDate` and chronological group insert.
- Fixed unsaved-changes (red) indicator for the two new profile file names (`BookmarkGroupFileNameModified`/`BookmarkFileNameModified` notified in `UpdateProfile` and `CurrentProfile` setter; defaults backfilled in `MapToDomain`).

### i18n
- New keys in `en/es/ca.xaml`: `ProfileTab_BookmarkGroupFileName`, `ProfileTab_BookmarkFileName`, `BookmarksTab_SubTabGroup/NewGroup/Bookmark`, `EditorGroupName`, `Group/BookmarkEditorNewTitle/EditTitle/Hint`, `EditorFieldRequired`, `EditorLocTranslating/LocError`, plus the full editor/delete set (`CtxNewGroup/NewBookmark/Copy/Edit/Delete`, `Editor*NeedId/IdInvalid/DateInvalid/Exists/GroupNotFound/NoModRoot/Saved/SaveError`, `DeleteConfirm*/NotAllowed/BlockNotFound/GroupHasBookmarks/Success/Error`).
- Updated application title to version 1.8.8 in all language files (en, es, ca) — `MainWindow_Title`.

---

## [1.8.9]

### Changed
- **Bookmark context menu by selection (1.8.9)** — `CtxNewGroupMenuItem`/`CtxNewBookmarkMenuItem` visibility in `BookmarkTree_ContextMenuOpening`: marker selection shows only `New bookmark`; group or empty selection shows both `New group` + `New bookmark`.
- **Group default date with offset, formatted (1.8.9)** — `ShowGroup` now renders `GroupDefaultStartDateValue` as the real date (`file − Profile.YearOffset`) formatted day-month-year with the month name in the app language (`en-GB/es-ES/ca-ES`, e.g. `1 enero 4`); negative years shown as absolute value plus `BCE` (English) or `AEC` (Spanish/Catalan), positives with no suffix. The raw block (`GroupRawValue`) is unchanged with the file date.

### Fixed
- Updated application title to version 1.8.9 in all language files (en, es, ca) — `MainWindow_Title`.

