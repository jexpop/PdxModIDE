# Changelog - PdxModIDE

All notable changes for the 1.8.x series of this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.8.0] - 2026-09-20

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

