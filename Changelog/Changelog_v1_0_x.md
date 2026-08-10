# Changelog - PdxModIDE

All notable changes for the 1.0.x series of this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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