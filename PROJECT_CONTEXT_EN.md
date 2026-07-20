# PROJECT_CONTEXT.md - PdxModIDE

> Auto-generated technical context document. Do not edit manually except for major updates.

---

## 1. Overview

**PdxModIDE** — Desktop IDE (WPF, .NET 8, C#) for creating and managing mods for Paradox Interactive games (CK3, EU4, etc.).

**Main function**: Automate copying files from the base game to the mod directory applying a **year offset** to all found dates (regex per game), with diff game/mod/backup validation, map rendering, and profile management.

**Main Stack**:
- **.NET 8** / C# 12 / WPF (XAML + code-behind + manual ViewModels)
- **SkiaSharp** (map rendering, LUT, palettes)
- **System.Text.Json** (JSON persistence in `data/`)
- **Parallel / Task** (module processing, validation, map loading)
- **No DI container** (manual instantiation in `ProjectManager`)

**Current version**: 1.4.2 (see `CHANGELOG_EN.md`, `CHANGELOG_ES.md`, `CHANGELOG_CA.md`). Solution: `PdxModIDE.sln` (9 projects).

---

## 2. Architecture

### 2.1 Project Structure (Solution)

```
PdxModIDE.sln
├── PdxModIDE.Domain          # Pure entities (Module, GameFile, Profile, EditingSession)
├── PdxModIDE.Data            # Repositories + DataLoader (JSON) + configs (ModuleConfig, FileConfig, Settings, LogFilters)
├── PdxModIDE.IO              # FS utilities (FileOperations, Paths)
├── PdxModIDE.Core            # Core logic: ModuleProcessor, DefinesProcessor, GameRegistry, IGamePlugin, CK3GamePlugin
├── PdxModIDE.MapEngine       # MapLoader, TitleHistoryLoader, ProvinceInfo, LUT cache
├── PdxModIDE.Rendering       # MapRenderer (SkiaSharp viewport, zoom, pan, tooltips)
├── PdxModIDE.Project         # IProjectService + ProjectManager (main orchestrator)
├── PdxModIDE.Validation      # ModuleValidator (recursive diff, byte/line comparison)
└── PdxModIDE.UI              # WPF App, MainWindow, ViewModels, Tabs, Themes, Dialogs
```

### 2.2 Project Dependencies

```
PdxModIDE.UI
    └── PdxModIDE.Project (IProjectService)
            ├── PdxModIDE.Core (ModuleProcessor, DefinesProcessor, GameRegistry)
            │       ├── PdxModIDE.Domain
            │       ├── PdxModIDE.Data (ModuleRepository)
            │       └── PdxModIDE.IO
            ├── PdxModIDE.MapEngine (MapLoader, TitleHistoryLoader)
            │       └── PdxModIDE.Domain (ProvinceInfo)
            ├── PdxModIDE.Rendering (MapRenderer)
            │       └── PdxModIDE.MapEngine
            └── PdxModIDE.Validation (ModuleValidator)
                    └── PdxModIDE.Domain
```

> **Note**: No automatic dependency injection. `ProjectManager` creates `new ModuleProcessor(new ModuleRepository())` in constructor.

### 2.3 Main Data Flow (Process Modules)

```
MainViewModel.ProcessModulesCommand
    → ProjectManager.ProcessModulesAsync(offsetOverride)
        → ModuleProcessor.ProcessModulesAsync(gameKey, modules, gameRoot, modRoot, backupRoot, offset, profileName)
            → Parallel.ForEach(moduleNames) → ProcessModule(...)
                → IGamePlugin.DateRegex.Replace(text, match => year+offset)
                → FileOperations.CopyFilePreserveTimestamps / WriteAllText
                → Log per module in logs/{profile}/{module}.log
```

**Synchronization**: `ModuleProcessor` caches modules in `_moduleCache` (thread-safe with `lock`). `InvalidateCache()` clears the cache.

---

## 3. Main Dependencies (NuGet)

| Project | Package | Version | Usage |
|---------|---------|---------|-------|
| `PdxModIDE.UI` | `SkiaSharp` / `SkiaSharp.Views.WPF` | 3.116.1 | Map render, LUT, palettes |
| `PdxModIDE.MapEngine` | `SkiaSharp` | 3.116.1 | Decode provinces.png, build LUT bitmap |
| `PdxModIDE.Core` | `Microsoft.Extensions.Logging.Abstractions` | 8.x | (Optional) abstracted logging |
| All | `System.Text.Json` | Built-in | Serialization `data/*.json` |
| `PdxModIDE.UI` | `Microsoft.Xaml.Behaviors.Wpf` | 1.1.x | (If used) XAML behaviors |

> `Directory.Build.props` centralizes `<TargetFramework>net8.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`.

---

## 4. Data Model

### 4.1 Domain Entities (`PdxModIDE.Domain.Models`)

| Class | Key Properties | Notes |
|-------|----------------|-------|
| `Module` | `Name`, `Path`, `IgnoreExtensions (IReadOnlyList<string>)` | Immutable (ctor only) |
| `GameFile` | `Name`, `Path`, `MapTo?` | `MapTo` allows mapping game path → different mod path |
| `Profile` | `Id (Guid)`, `Name`, `Game`, `GameRoot`, `ModRoot`, `BackupRoot`, `YearOffset`, `ModuleIds[]`, `FileIds[]`, `SelectedModules`, `SelectedFiles` | `Selected*` resolved in `EditingSession` |
| `EditingSession` | `CurrentProfile`, `ModulesByGame`, `FilesByGame`, `AllModulesByName`, `AllFilesByName` | Built in `ProjectManager.BuildSession`; resolves `ModuleIds`→`Module` references |

### 4.2 Persistence Configs (`PdxModIDE.Data`)

| Class | JSON File | Description |
|-------|-----------|-------------|
| `DataProfile` | `data/profiles.json` | 1:1 mapping to `Domain.Profile` + serialization |
| `ModuleConfig` | `data/modules.json` | `{ Path, IgnoreExt[] }` per `gameKey → moduleName` |
| `FileConfig` | `data/files.json` | `{ Path, MapTo? }` per `gameKey → fileKey` |
| `Settings` | `data/settings.json` | `{ Theme }` |
| `LogFilters` | `data/logfilters.json` | Log filters per profile (not actively used) |

**ID Convention**: `moduleName` = key in JSON = relative folder name (e.g. `common/landed_titles`). `fileKey` = logical name (e.g. `defines`).

### 4.3 `data/` File Structure

```
data/
├── profiles.json       # List<DataProfile>
├── modules.json        # Dict<gameKey, Dict<moduleName, ModuleConfig>>
├── files.json          # Dict<gameKey, Dict<fileKey, FileConfig>>
├── settings.json       # Settings { Theme }
└── logfilters.json     # LogFilters { ProfileFilters[] }
```

---

## 5. Key Modules and Components

### 5.1 `ModuleProcessor` (`PdxModIDE.Core`)

**Responsibility**: Recursive game→mod copy applying date offset.

```csharp
public void ProcessModule(string gameKey, string moduleName, 
    string gameRoot, string modRoot, string backupRoot, int offset, string profileName)
```

- Uses `IGamePlugin.DateRegex` (e.g. CK3: `\b(\d{1,4})\.(\d{1,2})\.(\d{1,2})\b`).
- `IGamePlugin.IsDateProcessableExtension(ext)` filters extensions (`.txt`, `.csv`, `.yml`).
- Pre-backup to `backupRoot/{relPath}` (preserves timestamps).
- Log per module: `logs/{profileName}/{moduleName}.log` (append).
- Parallelism: `Parallel.ForEach` with `MaxDegreeOfParallelism = Environment.ProcessorCount`.

**Key methods**:
- `ApplyOffset(string text, int offset, IGamePlugin)` → regex replace.
- `ProcessModulesAsync` → Task wrapper for UI async.

### 5.2 `DefinesProcessor` (`PdxModIDE.Core`)

**Responsibility**: Read/write `end_date` in `defines.txt`.

```csharp
ReadEndDate(gameRoot, gameKey)        // searches defines.txt in gameRoot
ReadModEndDate(modRoot, gameKey)      // searches in modRoot
WriteEndDate(gameRoot, modRoot, backupRoot, newDate, gameKey)
```

- Auto-backup before writing.
- Uses `IGamePlugin.GetDefinesPath()` → relative (e.g. `game/defines.txt`).
- Regex: `end_date\s*=\s*(\d{4})\.(\d{2})\.(\d{2})`.

### 5.3 `GameRegistry` + `IGamePlugin` (`PdxModIDE.Core.Games`)

**Pattern**: Plugin per game. Static registration `GameRegistry.Register(plugin)`.

```csharp
interface IGamePlugin {
    string GameKey { get; }
    string DisplayName { get; }
    Regex DateRegex { get; }
    bool IsDateProcessableExtension(string ext);
    string GetDefinesPath();
    bool CanHandleGame(string gameRoot);  // automatic detection
}
```

**Implemented**: `CK3GamePlugin` (`GameKey="CK3"`).
- `DateRegex`: `\b(\d{1,4})\.(\d{1,2})\.(\d{1,2})\b`
- `ProcessableExt`: `.txt`, `.csv`, `.yml`, `.lua`
- `DefinesPath`: `game/defines.txt`
- `CanHandleGame`: searches for `game/defines.txt` with `end_date` or CK3 `game_version`.

**Detection**: `GameRegistry.DetectGame(gameRoot)` iterates plugins sorted by key length descending.

### 5.4 `MapLoader` (`PdxModIDE.MapEngine`)

**Full CK3 map loading**:

| Step | File | Output |
|------|------|--------|
| `LoadDefinition` | `definition.csv` | `ProvincesById`, `ProvincesByColor`, `ProvinceToBarony` |
| `LoadDefaultMap` | `default.map` | `Sea`, `Lakes`, `Rivers`, `Impassable`, `ImpassableSeas` (HashSet<int>) |
| `LoadLandedTitles` | `common/landed_titles/*.txt` | `ProvinceToBarony`, `BaronyToCounty`, `CountyToDuchy`, `DuchyToKingdom`, `KingdomToEmpire` |
| `MarkTerrainTypes` | — | `ProvinceInfo.Type` ∈ {sea, lake, river, impassable, land, unknown} |
| `BuildOrLoadLut` | — | `Lut[16_777_216] byte` (MD5 cache definition.csv + default.map) |
| `BuildPixelData` | `provinces.png/bmp` | `ProvinceIdMap[int[]]` (w*h), `MapWidth`, `MapHeight` |

**LUT Cache**: `%LocalAppData%/PdxModIDE/lut_cache/{lut_types.bin, lut_meta.json}`. MD5 hash of sources.

**TitleHistoryLoader**: Parses `history/titles/*.txt` → `TitleHistory { Holders: SortedList<int, string> }` (year → holder). Used by `MapLoader.BuildHolderLut(year, history, out indexToHolder)`.

**County Mode**: `BuildCountyLut(out indexToCounty)` (no year parameter, borders don't change) maps province → barony (`ProvinceToBarony`) → county (`BaronyToCounty`). Generates 16M entry LUT coloring by county; indices >255 wrap around (modulo 255) to avoid color collision.

**Duchy/Kingdom/Empire Modes**: New methods `BuildDuchyLut`, `BuildKingdomLut`, `BuildEmpireLut` use the full hierarchy `CountyToDuchy` → `DuchyToKingdom` → `KingdomToEmpire` to color by each level. In the Map tab: mutually exclusive checkboxes (Tit./Cty./Dch./Kgd./Emp.) with tooltips.

### 5.5 `ModuleValidator` (`PdxModIDE.Validation`)

**Three-way recursive diff**: Mod vs Backup, Game vs Backup, Game vs Mod.

```csharp
ValidateModuleSingle(moduleName, ComparisonType) → List<FileComparisonResult>
ValidateAllAsync() → List<ModuleValidationResult> (parallel)
```

`FileComparisonResult`: `{ RelativePath, Status (Equal/Modified/Added/Deleted), DiffLines? }`.
Line-by-line diff (simple, no LCS).

**IgnoreExt**: Configurable per module (`ModuleConfig.IgnoreExt`).

### 5.6 `ProjectManager` (`PdxModIDE.Project`)

**Main orchestrator** — implements `IProjectService`.

**State**:
- `_dataProfiles`, `_dataModules`, `_dataFiles`, `_dataSettings`, `_dataLogFilters` (JSON cache)
- `_domainProfiles`, `CurrentDataProfile`, `CurrentProfile`, `CurrentSession` (`EditingSession`)

**Key methods**:
| Method | Description |
|--------|-------------|
| `Load()` | Loads all JSON + `SyncDomainProfiles()` |
| `SaveAll()` | Persists all JSON |
| `SelectProfile(name)` | Changes active profile + `BuildSession` |
| `CreateProfile(name, game)` | New profile + persistence |
| `CreateProfileWithGameDetection(name, gameRoot)` | Detects game + creates |
| `ProcessModulesAsync(offset?)` | Delegates to `ModuleProcessor` |
| `ValidateAllAsync()` | Delegates to `ModuleValidator` (parallel) |
| `GetGameModules(gameKey)` | `ModuleConfig` dict |
| `GetAllModules()` | `Domain.Module` nested read-only dict |

**BuildSession**: Constructs `EditingSession` resolving `ModuleIds`/`FileIds` → actual `Module`/`GameFile` objects.

### 5.7 `DataLoader` (`PdxModIDE.Data`)

**Generic JSON Load/Save**:

```csharp
static T Load<T>(string file, T defaultValue)
static void Save<T>(string file, T data)
```

Files in `data/` (creates directory if it doesn't exist). `JsonSerializerOptions: WriteIndented=true`.

### 5.8 UI — `MainViewModel` + Tabs (`PdxModIDE.UI.ViewModels`)

**MainViewModel**: Complete UI state.
- `Profiles: ObservableCollection<ProfileViewModel>`
- `CurrentProfile`, `CurrentSession`
- `GameModules`, `GameFiles` (grouped by game)
- `SelectedModules`, `SelectedFiles` (checkboxes)
- `GameRoot`, `ModRoot`, `BackupRoot`, `YearOffset` (two-way bindings)
- `Theme` (change triggers `ApplyTheme` in `MainWindow`)
- Commands: `ProcessModulesCommand`, `ValidateAllCommand`, `SaveProfileCommand`, `DetectGameCommand`, `Browse*Command`.

**Tabs** (UserControls in `UI/`):
- `ProfileTab`: CRUD profiles, game detection, paths.
- `ModulesTab`: Module list by game, checkbox selection, add/edit/delete module.
- `FilesTab`: File list, checkbox, editable mapTo.
- `DatesTab`: Read end_date game/mod, write new end_date.
- `ValidationTab`: Validate all / individual module / individual file; results grid + diff viewer.
- `HistoryTab` (tab "Map", previously two tabs "History (Base)"/"History (Mod)" now unified): Interactive map (SkiaSharp). 5 mutually exclusive modes (checkboxes with tooltips):
  - **Holder** (Tit.): Colors by holder (character) at year in `YearBox` → `BuildHolderLut(year, TitleHistoryLoader)`.
  - **Counties** (Cty.): Colors by county borders (`c_xxx`) → `BuildCountyLut()`.
  - **Duchies** (Dch.): Colors by duchy borders (`d_xxx`) → `BuildDuchyLut()`.
  - **Kingdoms** (Kgd.): Colors by kingdom borders (`k_xxx`) → `BuildKingdomLut()`.
  - **Empires** (Emp.): Colors by empire borders (`e_xxx`) → `BuildEmpireLut()`.
  Click province → info panel shows Barony, County, Duchy, Kingdom, Empire, Holder, Liege according to mode.
  - **Technical note**: overlay is applied on CPU (workaround for `SKShader.CreateImage` bug as child shader). `RenderToBitmap` renders terrain+borders via shader (mode=0), then iterates pixels and applies palette color from the holder LUT. Uses `InvalidateRender()` for cache invalidation.
- `LogsTab`: Log filters (not fully implemented).
- `SettingsTab`: Theme, default paths.

**Themes**: `ResourceDictionary` swap in `MainWindow.ApplyTheme(theme)`. Files in `Themes/*.xaml`.

### 5.9 `GeneralSettingsWindow` + Internationalization (`PdxModIDE.UI`)

**Application settings** (not tied to a profile/mod): modal window (`Window`, not `UserControl`) opened from a gear icon (⚙) in the top-right corner of `MainWindow` (`BtnGeneralSettings_Click`). Contains:

- **Visual theme**: same 7 themes that previously lived in the removed "Options" tab (`SettingsTab`, removed in 1.2.0).
- **Language**: new Spanish/English selector.

**i18n mechanism**: XAML `ResourceDictionary`, same pattern as Themes. Folder `PdxModIDE.UI/Languages/` (`es.xaml`, `en.xaml`) with `system:String` keys (e.g. `Settings_Title`, `Settings_ThemeSection`). Consumed in XAML via `{DynamicResource Key}` to allow hot-switching without restart.

```
MainWindow.ApplyTheme(theme)      → updates _currentThemePath
MainWindow.ApplyLanguage(language) → updates _currentLanguagePath
    └─ RefreshMergedDictionaries()  → recombines BOTH dictionaries (theme + language)
                                       in Application.Resources and Window.Resources,
                                       so changing one does not remove the other.
```

**Persistence**: `Settings.Language` (`data/settings.json`, field `"language"`, default `"en"`) — same flow as `Theme`: `IProjectService.Language` → `ProjectManager.Language` → `MainViewModel.Language` → `MainViewModel.SaveSettings()`.

**Phase 2 (completed in 1.2.1)**: All UI texts have been extracted to language dictionaries (`es.xaml` / `en.xaml`) and all tabs (Profile, Map, Modules, Dates, Validation, Logs) and dialogs use `{DynamicResource ...}` in XAML or `Res("key")` in code-behind. Language change affects the entire application instantly.

**File architecture**: General application texts are in `es.xaml` / `en.xaml`. Game-specific texts go in separate `{GameKey}.{lang}.xaml` files (e.g. `CK3.es.xaml`, `CK3.en.xaml`), loaded automatically according to the active profile via `RefreshMergedDictionaries()`.

---

## 6. Conventions and Style

| Area | Convention |
|------|------------|
| Namespaces | `PdxModIDE.{Project}.{Feature}` |
| Naming | PascalCase (types), camelCase (props/params), UPPER_SNAKE (consts) |
| Immutability | `Domain` entities: `readonly` props, ctor only; `Data` configs: public setters for JSON |
| Async | `Task`/`Task<T>` in repositories and processors; `Parallel.ForEach` for mixed CPU-bound I/O |
| Logging | `File.AppendAllText(logs/...)` manual; `crash.log` in `App.OnStartup` |
| DI | Manual in `ProjectManager` constructor; no container |
| UI Pattern | Code-behind + ViewModel (no MVVM framework); manual `INotifyPropertyChanged` in `MainViewModel` |
| Serialization | `System.Text.Json`; `JsonPropertyName` not used (public props = JSON names) |
| Paths | Always `Path.Combine`; `FileOperations.EnsureDirectory` before writing |
| Error Handling | `try/catch` in UI commands → `MessageBox.Show`; global crash → `logs/crash.log` |

---

## 7. Key Design Decisions

| Decision | Justification | Trade-off / Debt |
|----------|---------------|------------------|
| **9 separate projects** | Clear domain/data/core/UI separation; testability | More boilerplate; slightly slower build |
| **No DI container** | Simplicity, zero extra dependencies | Coupling `ProjectManager`→concrete `ModuleProcessor` |
| **Flat JSON in `data/`** | No DB, portable, manually editable | Non-transactional; naive concurrency (last wins) |
| **Per-game date regex** | Flexibility (CK3/EU4 different formats) | Simple regex; doesn't parse context (e.g. `start_date` vs `end_date`) |
| **Auto-backup** | Safety against offset errors | Duplicates space; no automatic cleanup |
| **Cached 16M byte LUT** | Instant map render; avoids rebuild | 16 MB RAM + disk; invalidation only by source file hash |
| **CPU overlay instead of shader** | `SKShader.CreateImage` as child shader in `SKRuntimeEffect` returns 0 in `eval()` on SkiaSharp 3.116.1 (CPU raster). Workaround: render terrain+borders via shader, apply overlay (holder/county/duchy/etc) on CPU by iterating pixels with `Marshal.Copy`. | 100% CPU; if SkiaSharp fixes it, can migrate back to shader. |
| **Color cycle for >255 items** | `BuildHolderLut`/`BuildCountyLut` use `(idx-1)%255+1` for wrap-around | Before: index clamped at 255 → hundreds of green counties/holders |
| **Synchronous Parallel.ForEach in ProcessModule** | Leverages multi-core I/O | Blocks thread pool; `ProcessModulesAsync` does `await Task.CompletedTask` after `Parallel.ForEach` |
| **Manual ViewModels** | Full control, no magic | Boilerplate `OnPropertyChanged`; easy to introduce binding bugs |

---

## 8. Technical Debt and Prioritized TODOs

### 🔴 Critical
- [ ] **Race condition in `ModuleProcessor._moduleCache`**: `LoadModules()` calls `.GetAwaiter().GetResult()` on thread pool → possible deadlock if called from UI thread. **Fix**: make `LoadModulesAsync` + `await` in `ProcessModulesAsync`.
- [ ] **Synchronous `Parallel.ForEach` in `ProcessModulesAsync`**: blocks thread pool. **Fix**: `Parallel.ForEachAsync` (.NET 6+) or `Task.WhenAll` with `SemaphoreSlim`.
- [ ] **No path validation in `CreateProfile`**: `GameRoot`/`ModRoot`/`BackupRoot` can be empty → runtime error on processing.

### 🟠 Important
- [ ] **Introduce `Microsoft.Extensions.DependencyInjection`**: register `IModuleRepository`, `IProjectService`, `ModuleProcessor`, `DefinesProcessor`, `ModuleValidator`.
- [ ] **Base ViewModel with `CommunityToolkit.Mvvm`** (`[ObservableProperty]`, `[RelayCommand]`) → removes boilerplate `INotifyPropertyChanged`.
- [ ] **Unit tests** (xUnit):
  - `ModuleProcessor.ApplyOffset` (various date formats, negative offsets, no-match).
  - `DefinesProcessor.Read/WriteEndDate` (mock FS).
  - `MapLoader.LoadDefinition` (malformed CSV, duplicates).
  - `ModuleValidator.CompareFileContents` (equal, different, only in A, only in B).
- [ ] **Module/file list virtualization** (`VirtualizingStackPanel` + `ItemsControl` → `ListView` with `VirtualizingPanel.IsVirtualizing=True`).
- [ ] **Incremental LUT cache**: invalidate only modified provinces (diff `definition.csv`).
- [x] **Internationalization completed (1.2.1)**: all UI strings extracted to `es.xaml` / `en.xaml`. Tabs and dialogs use `DynamicResource` or `Res()`. Game-specific text translation to `{GameKey}.{lang}.xaml` pending.

### 🟢 Improvement
- [ ] **EU4/Imperator/HOI4/Vic3 plugins**: new `IGamePlugin` with specific regex and paths.
- [ ] **FileSystemWatcher** on `ModRoot` → auto-refresh validation.
- [ ] **Export validation report** (HTML/Markdown) from `ValidationTab`.
- [ ] **Semantic diff** (understand Clausewitz syntax) instead of line-by-line.
- [ ] **Performance profiling**: `BenchmarkDotNet` for `ModuleProcessor`, `MapLoader.BuildLutInMemory`.
- [ ] **Toast notifications** (e.g. `MaterialDesignThemes` Snackbar) instead of MessageBox for success/progress.

---

## 9. Security / Integrity Rules (Logic)

- **Profiles**: Isolation by `Profile.Name` (unique key). No shared data between profiles.
- **Backup**: Write always preceded by copy to `BackupRoot` (preserves timestamps).
- **Date offset**: Only extensions allowed by `IGamePlugin.IsDateProcessableExtension`.
- **Game detection**: `CanHandleGame` looks for characteristic files; fallback to user dialog.
- **Paths**: `Directory.Exists` validation in `DetectGame` and `Browse` dialogs.

---

## 10. Useful Commands

```bash
# Build solution (Release)
dotnet build PdxModIDE.sln -c Release

# Build UI only (for quick test)
dotnet build PdxModIDE.UI/PdxModIDE.UI.csproj -c Debug

# Run UI
dotnet run --project PdxModIDE.UI/PdxModIDE.UI.csproj

# Clean all
dotnet clean PdxModIDE.sln

# View dependency tree
dotnet msbuild PdxModIDE.sln /t:GenerateRestoreGraphFile /pp:restore.graph
```

**Build output structure**:
```
PdxModIDE.UI/bin/Debug/net8.0-windows/
├── PdxModIDE.UI.exe
├── data/                 # JSON configs (copied if not present)
├── logs/                 # Created at runtime
├── Themes/               # ResourceDictionaries
└── *.dll (Core, Domain, Data, IO, MapEngine, Project, Rendering, Validation)
```

---

## 11. Environment Variables / External Configuration

No mandatory environment variables. All configuration in `data/*.json`.

**Default paths** (if user does not configure):
- `GameRoot`: Detected via `GameRegistry.DetectGame` or dialog.
- `ModRoot`: `mod/` folder next to `GameRoot` (Paradox convention).
- `BackupRoot`: `backups/{ProfileName}/` under `ModRoot`.

---

## 12. Extensibility: Adding a New Game (e.g. EU4)

1. Create `PdxModIDE.Core.Games.EU4.EU4GamePlugin : IGamePlugin`:
   - `GameKey = "EU4"`
   - `DateRegex` adapted to EU4 format (e.g. `\b(\d{4})\.(\d{2})\.(\d{2})\b`)
   - `IsDateProcessableExtension` (add `.gfx`, `.gui` if applicable)
   - `GetDefinesPath()` → EU4 `defines.txt` location
   - `CanHandleGame` → looks for `eu4.exe` or `defines.txt` with EU4 `start_date`
2. Register in `App.OnStartup`: `GameRegistry.Register(new EU4GamePlugin());`
3. Add base modules/files in `data/modules.json` and `data/files.json` under key `"EU4"`.
4. (Optional) Extend `MapLoader` if map format differs (EU4 uses same `definition.csv` + `provinces.png`).

---

## 13. Key File Quick References

| File | Purpose |
|------|---------|
| `PdxModIDE.UI/App.xaml.cs` | Bootstrap: registers CK3, data/logs dirs, crash handler |
| `PdxModIDE.UI/MainWindow.xaml.cs` | Theme swap, DataContext=MainViewModel, initial profile |
| `PdxModIDE.UI/ViewModels/MainViewModel.cs` | Full UI state, commands, bindings |
| `PdxModIDE.Project/ProjectManager.cs` | Orchestrator: profiles, session, processing, validation, persistence |
| `PdxModIDE.Core/ModuleProcessor.cs` | Game→mod copy + date offset (parallel, logging) |
| `PdxModIDE.Core/DefinesProcessor.cs` | Read/Write `end_date` in defines.txt |
| `PdxModIDE.Core/Games/GameRegistry.cs` | Plugin registry + automatic game detection |
| `PdxModIDE.Core/Games/CK3/CK3GamePlugin.cs` | CK3 implementation: regex, extensions, defines path |
| `PdxModIDE.MapEngine/MapLoader.cs` | Full map loading + LUT cache + holders by year |
| `PdxModIDE.MapEngine/TitleHistoryLoader.cs` | Parse `history/titles/*.txt` → `TitleHistory` |
| `PdxModIDE.Rendering/MapRenderer.cs` | SkiaSharp viewport, zoom/pan, color picker, tooltips. CPU overlay (workaround child shader bug). |
| `PdxModIDE.Validation/ModuleValidator.cs` | 3-way diff (mod/game/backup) recursive |
| `PdxModIDE.Data/DataLoader.cs` | Generic Load/Save JSON `data/*.json` |
| `PdxModIDE.Domain/Models.cs` | Pure entities (Module, GameFile, Profile, EditingSession) |
| `PdxModIDE.IO/FileOperations.cs` | CopyPreserveTimestamps, ReadTextFile, EnsureDirectory |

---

*Generated: 2026-07-20 | Project: PdxModIDE | Version: 1.4.2 | Stack: .NET 8 / WPF / SkiaSharp 3.116.1 / System.Text.Json*
