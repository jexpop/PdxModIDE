# PROJECT_CONTEXT.md - PdxModIDE

> Document de context tècnic generat automàticament. No editar manualment excepte actualitzacions majors.

---

## 1. Visió General

**PdxModIDE** — IDE d'escriptori (WPF, .NET 8, C#) per a creació i gestió de mods de jocs Paradox Interactive (CK3, EU4, etc.).

**Funció principal**: Automatitzar la còpia de fitxers del joc base al directori del mod aplicant un **offset d'anys** a totes les dates trobades (regex per joc), amb validació diff game/mod/backup, renderització de mapa i gestió de perfils.

**Stack Principal**:
- **.NET 8** / C# 12 / WPF (XAML + code-behind + ViewModels manuals)
- **SkiaSharp** (renderització mapa, LUT, paletes)
- **System.Text.Json** (persistència JSON a `data/`)
- **Parallel / Task** (processat mòduls, validació, càrrega mapa)
- **No DI container** (instanciació manual a `ProjectManager`)

**Versió actual**: 1.4.7 (veure `CHANGELOG_CA.md`, `CHANGELOG_ES.md`, `CHANGELOG_EN.md`). Solution: `PdxModIDE.sln` (9 projectes).

---

## 2. Arquitectura

### 2.1 Estructura de Projectes (Solution)

```
PdxModIDE.sln
├── PdxModIDE.Domain          # Entitats pures (Module, GameFile, Profile, EditingSession)
├── PdxModIDE.Data            # Repositoris + DataLoader (JSON) + configs (ModuleConfig, FileConfig, Settings, LogFilters)
├── PdxModIDE.IO              # Utilitats FS (FileOperations, Paths)
├── PdxModIDE.Core            # Lògica nucli: ModuleProcessor, DefinesProcessor, GameRegistry, IGamePlugin, CK3GamePlugin
├── PdxModIDE.MapEngine       # MapLoader, TitleHistoryLoader, ProvinceInfo, LUT cache
├── PdxModIDE.Rendering       # MapRenderer (SkiaSharp viewport, zoom, pan, tooltips)
├── PdxModIDE.Project         # IProjectService + ProjectManager (orquestador principal)
├── PdxModIDE.Validation      # ModuleValidator (diff recursiu, comparació byte/linia)
└── PdxModIDE.UI              # WPF App, MainWindow, ViewModels, Tabs, Temes, Dialegs
```

### 2.2 Dependències entre Projectes

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

> **Nota**: No hi ha injecció de dependències automàtica. `ProjectManager` crea `new ModuleProcessor(new ModuleRepository())` al constructor.

### 2.3 Flux de Dades Principal (Processar Mòduls)

```
MainViewModel.ProcessModulesCommand
    → ProjectManager.ProcessModulesAsync(offsetOverride)
        → ModuleProcessor.ProcessModulesAsync(gameKey, modules, gameRoot, modRoot, backupRoot, offset, profileName)
            → Parallel.ForEach(moduleNames) → ProcessModule(...)
                → IGamePlugin.DateRegex.Replace(text, match => year+offset)
                → FileOperations.CopyFilePreserveTimestamps / WriteAllText
                → Log per mòdul a logs/{profile}/{module}.log
```

**Sincronització**: `ModuleProcessor` cacheja mòduls a `_moduleCache` (thread-safe amb `lock`). `InvalidateCache()` neteja cache.

---

## 3. Dependències Principals (NuGet)

| Projecte | Paquet | Versió | Ús |
|----------|--------|--------|-----|
| `PdxModIDE.UI` | `SkiaSharp` / `SkiaSharp.Views.WPF` | 3.116.1 | Render mapa, LUT, paletes |
| `PdxModIDE.MapEngine` | `SkiaSharp` | 3.116.1 | Decode provinces.png, build LUT bitmap |
| `PdxModIDE.Core` | `Microsoft.Extensions.Logging.Abstractions` | 8.x | (Opcional) logging abstraït |
| Tots | `System.Text.Json` | Built-in | Serialització `data/*.json` |
| `PdxModIDE.UI` | `Microsoft.Xaml.Behaviors.Wpf` | 1.1.x | (Si s'usa) behaviors XAML |

> `Directory.Build.props` centralitza `<TargetFramework>net8.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`.

---

## 4. Model de Dades

### 4.1 Entitats de Domini (`PdxModIDE.Domain.Models`)

| Classe | Propietats Clau | Notes |
|-------|-----------------|-------|
| `Module` | `Name`, `Path`, `IgnoreExtensions (IReadOnlyList<string>)` | Immutable (ctor only) |
| `GameFile` | `Name`, `Path`, `MapTo?` | `MapTo` permet mapejar path joc → path mod diferent |
| `Profile` | `Id (Guid)`, `Name`, `Game`, `GameRoot`, `ModRoot`, `BackupRoot`, `YearOffset`, `ModuleIds[]`, `FileIds[]`, `SelectedModules`, `SelectedFiles` | `Selected*` es resolen a `EditingSession` |
| `EditingSession` | `CurrentProfile`, `ModulesByGame`, `FilesByGame`, `AllModulesByName`, `AllFilesByName` | Construïda a `ProjectManager.BuildSession`; resol referències `ModuleIds`→`Module` |

### 4.2 Configs de Persistència (`PdxModIDE.Data`)

| Classe | Fitxer JSON | Descripció |
|-------|-------------|-------------|
| `DataProfile` | `data/profiles.json` | Mapeja 1:1 a `Domain.Profile` + serialització |
| `ModuleConfig` | `data/modules.json` | `{ Path, IgnoreExt[] }` per `gameKey → moduleName` |
| `FileConfig` | `data/files.json` | `{ Path, MapTo? }` per `gameKey → fileKey` |
| `Settings` | `data/settings.json` | `{ Theme }` |
| `LogFilters` | `data/logfilters.json` | Filtres de log per perfil (no usat activament) |

**Convenció IDs**: `moduleName` = key al JSON = nom carpeta relativa (ex. `common/landed_titles`). `fileKey` = nom lògic (ex. `defines`).

### 4.3 Estructura Fitxers `data/`

```
data/
├── profiles.json       # List<DataProfile>
├── modules.json        # Dict<gameKey, Dict<moduleName, ModuleConfig>>
├── files.json          # Dict<gameKey, Dict<fileKey, FileConfig>>
├── settings.json       # Settings { Theme }
└── logfilters.json     # LogFilters { ProfileFilters[] }
```

---

## 5. Mòduls i Components Clau

### 5.1 `ModuleProcessor` (`PdxModIDE.Core`)

**Responsabilitat**: Còpia recursiva game→mod aplicant offset de dates.

```csharp
public void ProcessModule(string gameKey, string moduleName, 
    string gameRoot, string modRoot, string backupRoot, int offset, string profileName)
```

- Usa `IGamePlugin.DateRegex` (ex. CK3: `\b(\d{1,4})\.(\d{1,2})\.(\d{1,2})\b`).
- `IGamePlugin.IsDateProcessableExtension(ext)` filtra extensions (`.txt`, `.csv`, `.yml`).
- Còpia de seguretat prèvia a `backupRoot/{relPath}` (preserva timestamps).
- Log per mòdul: `logs/{profileName}/{moduleName}.log` (append).
- Paral·lelisme: `Parallel.ForEach` amb `MaxDegreeOfParallelism = Environment.ProcessorCount`.

**Mètodes clau**:
- `ApplyOffset(string text, int offset, IGamePlugin)` → regex replace.
- `ProcessModulesAsync` → wrapper Task per a UI async.

### 5.2 `DefinesProcessor` (`PdxModIDE.Core`)

**Responsabilitat**: Lectura/escriptura `end_date` a `defines.txt`.

```csharp
ReadEndDate(gameRoot, gameKey)        // cerca defines.txt a gameRoot
ReadModEndDate(modRoot, gameKey)      // cerca a modRoot
WriteEndDate(gameRoot, modRoot, backupRoot, newDate, gameKey)
```

- Còpia de seguretat automàtica abans d'escriure.
- Usa `IGamePlugin.GetDefinesPath()` → relatiu (ex. `game/defines.txt`).
- Regex: `end_date\s*=\s*(\d{4})\.(\d{2})\.(\d{2})`.

### 5.3 `GameRegistry` + `IGamePlugin` (`PdxModIDE.Core.Games`)

**Patró**: Plugin per joc. Registre estàtic `GameRegistry.Register(plugin)`.

```csharp
interface IGamePlugin {
    string GameKey { get; }
    string DisplayName { get; }
    Regex DateRegex { get; }
    bool IsDateProcessableExtension(string ext);
    string GetDefinesPath();
    bool CanHandleGame(string gameRoot);  // detecció automàtica
}
```

**Implementat**: `CK3GamePlugin` (`GameKey="CK3"`).
- `DateRegex`: `\b(\d{1,4})\.(\d{1,2})\.(\d{1,2})\b`
- `ProcessableExt`: `.txt`, `.csv`, `.yml`, `.lua`
- `DefinesPath`: `game/defines.txt`
- `CanHandleGame`: cerca `game/defines.txt` amb `end_date` o `game_version` CK3.

**Detecció**: `GameRegistry.DetectGame(gameRoot)` itera plugins ordenats per longitud key desc.

### 5.4 `MapLoader` (`PdxModIDE.MapEngine`)

**Càrrega completa mapa CK3**:

| Pas | Fitxer | Sortida |
|-----|--------|---------|
| `LoadDefinition` | `definition.csv` | `ProvincesById`, `ProvincesByColor`, `ProvinceToBarony` |
| `LoadDefaultMap` | `default.map` | `Sea`, `Lakes`, `Rivers`, `Impassable`, `ImpassableSeas` (HashSet<int>) |
| `LoadLandedTitles` | `common/landed_titles/*.txt` | `ProvinceToBarony`, `BaronyToCounty`, `CountyToDuchy`, `DuchyToKingdom`, `KingdomToEmpire` |
| `MarkTerrainTypes` | — | `ProvinceInfo.Type` ∈ {sea, lake, river, impassable, land, unknown} |
| `BuildOrLoadLut` | — | `Lut[16_777_216] byte` (cache MD5 definition.csv + default.map) |
| `BuildPixelData` | `provinces.png/bmp` | `ProvinceIdMap[int[]]` (w*h), `MapWidth`, `MapHeight` |

**LUT Cache**: `%LocalAppData%/PdxModIDE/lut_cache/{lut_types.bin, lut_meta.json}`. Hash MD5 de fonts.

**TitleHistoryLoader**: Parseja `history/titles/*.txt` → `TitleHistory { Holders: SortedList<int, string> }` (any → holder). Usat per `MapLoader.BuildHolderLut(year, history, out indexToHolder)`.

**Mode Comtats**: `BuildCountyLut(out indexToCounty)` (sense paràmetre any, els límits no canvien) mapeja província → baronia (`ProvinceToBarony`) → comtat (`BaronyToCounty`). Genera LUT 16M entrades acolorint per comtat; índexs >255 fan wrap-around (mòdul 255) per evitar col·lisió de color.

**Mods Ducats/Regnes/Imperis**: Nous mètodes `BuildDuchyLut`, `BuildKingdomLut`, `BuildEmpireLut` usen la jerarquia completa `CountyToDuchy` → `DuchyToKingdom` → `KingdomToEmpire` per acolorir per cada nivell. A la pestanya Mapa: checkboxes mútuament excloents (Tit./Cond./Duc./Rey./Imp.) amb tooltips.

### 5.5 `ModuleValidator` (`PdxModIDE.Validation`)

**Diff tres vies no recursiu** (només fitxers de primer nivell): Mod vs Backup, Game vs Backup, Game vs Mod.

```csharp
ValidateModuleSingle(moduleName, ComparisonType) → List<FileComparisonResult>
ValidateAllAsync() → List<ModuleValidationResult> (paral·lel)
```

`FileComparisonResult`: `{ RelativePath, Status (Igual/Modificat/Afegit/Eliminat), DiffLines? }`.
Diff línia a línia amb cerca bidireccional (fins a 20 línies) per entrellaçar correctament addicions i eliminacions.

**IgnoreExt**: Configurable per mòdul (`ModuleConfig.IgnoreExt`).

### 5.6 `ProjectManager` (`PdxModIDE.Project`)

**Orquestador principal** — implementa `IProjectService`.

**Estat**:
- `_dataProfiles`, `_dataModules`, `_dataFiles`, `_dataSettings`, `_dataLogFilters` (cache JSON)
- `_domainProfiles`, `CurrentDataProfile`, `CurrentProfile`, `CurrentSession` (`EditingSession`)

**Mètodes clau**:
| Mètode | Descripció |
|--------|------------|
| `Load()` | Carrega tot JSON + `SyncDomainProfiles()` |
| `SaveAll()` | Persisteix tots els JSON |
| `SelectProfile(name)` | Canvia perfil actiu + `BuildSession` |
| `CreateProfile(name, game)` | Nou perfil + persistència |
| `CreateProfileWithGameDetection(name, gameRoot)` | Detecta joc + crea |
| `ProcessModulesAsync(offset?)` | Delega a `ModuleProcessor` |
| `ValidateAllAsync()` | Delega a `ModuleValidator` (paral·lel) |
| `FindDateModules()` | Escaneja l'arrel del joc recursivament cercant carpetes no configurades amb fitxers de data (només informatiu) |
| `GetGameModules(gameKey)` | `ModuleConfig` dict |
| `GetAllModules()` | `Domain.Module` dict nested read-only |

**BuildSession**: Construeix `EditingSession` resolent `ModuleIds`/`FileIds` → objectes `Module`/`GameFile` reals.

### 5.7 `DataLoader` (`PdxModIDE.Data`)

**Genèric Load/Save JSON**:

```csharp
static T Load<T>(string file, T defaultValue)
static void Save<T>(string file, T data)
```

Fitxers a `data/` (crea directori si no existeix). `JsonSerializerOptions: WriteIndented=true`.

### 5.8 UI — `MainViewModel` + Tabs (`PdxModIDE.UI.ViewModels`)

**MainViewModel**: Estat UI complet.
- `Profiles: ObservableCollection<ProfileViewModel>`
- `CurrentProfile`, `CurrentSession`
- `GameModules`, `GameFiles` (agrupats per joc)
- `SelectedModules`, `SelectedFiles` (checkboxes)
- `GameRoot`, `ModRoot`, `BackupRoot`, `YearOffset` (bindings two-way)
- `Theme` (canvi dispara `ApplyTheme` a `MainWindow`)
- Comandes: `ProcessModulesCommand`, `ValidateAllCommand`, `SaveProfileCommand`, `DetectGameCommand`, `Browse*Command`.

**Tabs** (UserControls a `UI/`):
- `ProfileTab`: CRUD perfils, detecció joc, paths.
- `ModulesTab`: Llista mòduls per joc, checkbox selecció, add/edit/delete module.
- `FilesTab`: Llista fitxers, checkbox, mapTo editable.
- `DatesTab`: Llegir end_date game/mod, escriure nou end_date.
- `ValidationTab`: Validar tot / mòdul individual / fitxer individual; grid resultats + visor diff costat a costat (`DiffViewDialog`, estil Compare del Notepad++ amb panells "Original"/"Modified", números de línia i fons de color).
- `HistoryTab` (pestanya "Mapa", abans dos pestanyes "Història (Base)"/"Història (Mod)" ara unificades): Mapa interactiu (SkiaSharp). 5 modes mútuament excloents (checkboxes amb tooltips):
  - **Titular** (Tit.): Aceloreix per holder (personatge) a l'any `YearBox` → `BuildHolderLut(year, TitleHistoryLoader)`.
  - **Comtats** (Cond.): Aceloreix per límits de comtat (`c_xxx`) → `BuildCountyLut()`.
  - **Ducats** (Duc.): Aceloreix per límits de ducat (`d_xxx`) → `BuildDuchyLut()`.
  - **Regnes** (Rey.): Aceloreix per límits de regne (`k_xxx`) → `BuildKingdomLut()`.
  - **Imperis** (Imp.): Aceloreix per límits d'imperi (`e_xxx`) → `BuildEmpireLut()`.
  Click província → panell informació mostra Baronia, Comtat, Ducat, Regne, Imperi, Holder, Liege segons mode.
  - **Nota tècnica**: l'overlay s'aplica per CPU (workaround del bug de `SKShader.CreateImage` com a child shader). `RenderToBitmap` renderitza terreny+vores via shader (mode=0), després itera píxels i aplica color de paleta segons LUT d'holder. Utilitza `InvalidateRender()` per invalidació de cache.
- `LogsTab`: Filtres log (no implementat completament).
- `SettingsTab`: Tema, paths defaults.

**Temes**: `ResourceDictionary` swap a `MainWindow.ApplyTheme(theme)`. Fitxers a `Themes/*.xaml`.

### 5.9 `GeneralSettingsWindow` + Internacionalització (`PdxModIDE.UI`)

**Ajustos d'aplicació** (no lligats a un perfil/mod): finestra modal (`Window`, no `UserControl`) oberta des d'una icona d'engranatge (⚙) a la cantonada superior dreta de `MainWindow` (`BtnGeneralSettings_Click`). Conté:

- **Tema visual**: mateixos 7 temes que abans vivien a la desapareguda pestanya "Opcions" (`SettingsTab`, eliminada a 1.2.0).
- **Idioma**: nou selector Español/English.

**Mecanisme i18n**: `ResourceDictionary` XAML, mateix patró que Temes. Carpeta `PdxModIDE.UI/Languages/` (`es.xaml`, `en.xaml`) amb claus `system:String` (ex. `Settings_Title`, `Settings_ThemeSection`). Consumit a XAML via `{DynamicResource Clave}` per permetre canvi en calent sense reiniciar.

```
MainWindow.ApplyTheme(theme)      → actualitza _currentThemePath
MainWindow.ApplyLanguage(language) → actualitza _currentLanguagePath
    └─ RefreshMergedDictionaries()  → recombina AMBDÓS diccionaris (tema + idioma)
                                       a Application.Resources i Window.Resources,
                                       perquè canviar un no elimini l'altre.
```

**Persistència**: `Settings.Language` (`data/settings.json`, camp `"language"`, default `"en"`) — mateix flux que `Theme`: `IProjectService.Language` → `ProjectManager.Language` → `MainViewModel.Language` → `MainViewModel.SaveSettings()`.

**Fase 2 (completada a 1.2.1)**: Tots els textos de la interfície han estat extrets als diccionaris d'idioma (`es.xaml` / `en.xaml`) i totes les pestanyes (Perfil, Mapa, Mòduls, Dates, Validació, Logs) i quadres de diàleg utilitzen `{DynamicResource ...}` a XAML o `Res("key")` a code-behind. El canvi d'idioma afecta a tota l'aplicació a l'instant.

**Arquitectura de fitxers**: Els textos generals de l'aplicació estan a `es.xaml` / `en.xaml`. Els textos específics de cada joc van a fitxers separats `{GameKey}.{lang}.xaml` (ex. `CK3.es.xaml`, `CK3.en.xaml`), carregats automàticament segons el perfil actiu mitjançant `RefreshMergedDictionaries()`.

---

## 6. Convencions i Estil

| Àrea | Convenció |
|------|-----------|
| Namespaces | `PdxModIDE.{Project}.{Feature}` |
| Naming | PascalCase (tipus), camelCase (props/params), UPPER_SNAKE (consts) |
| Immutabilitat | `Domain` entities: `readonly` props, ctor only; `Data` configs: setters públics per a JSON |
| Async | `Task`/`Task<T>` en repositoris i processadors; `Parallel.ForEach` per a CPU-bound I/O mixt |
| Logging | `File.AppendAllText(logs/...)` manual; `crash.log` a `App.OnStartup` |
| DI | Manual a `ProjectManager` constructor; no container |
| UI Pattern | Code-behind + ViewModel (sense framework MVVM); `INotifyPropertyChanged` manual a `MainViewModel` |
| Serialització | `System.Text.Json`; `JsonPropertyName` no usat (props públiques = noms JSON) |
| Paths | `Path.Combine` sempre; `FileOperations.EnsureDirectory` abans d'escriure |
| Gestió d'Errors | `try/catch` en UI commands → `MessageBox.Show`; crash global → `logs/crash.log` |

---

## 7. Decisions de Disseny Clau

| Decisió | Justificació | Trade-off / Deute |
|---------|--------------|-------------------|
| **9 projectes separats** | Separació clara domini/dades/nucli/UI; testabilitat | Més boilerplate; build una mica més lent |
| **Sense DI container** | Simplicitat, zero dependències extra | Acoblament `ProjectManager`→`ModuleProcessor` concret |
| **JSON pla a `data/`** | Sense BD, portable, editable a mà | No transaccional; concurrència naïf (l'últim guanya) |
| **Regex dates per joc** | Flexibilitat (CK3/EU4 formats diferents) | Regex simple; no parseja context (ex. `start_date` vs `end_date`) |
| **Còpia de seguretat automàtica** | Seguretat davant errors d'offset | Duplica espai; no neteja automàtica |
| **LUT 16M bytes cachejat** | Render instantani mapa; evita rebuild | 16 MB RAM + disc; invalidació només per hash fitxers font |
| **Overlay per CPU en lloc de shader** | `SKShader.CreateImage` com a child shader dins `SKRuntimeEffect` retorna 0 en `eval()` a SkiaSharp 3.116.1 (CPU raster). Workaround: renderitzar terreny+vores via shader, aplicar overlay (holder/comtat/ducat/etc) per CPU iterant píxels amb `Marshal.Copy`. | Overlay 100% CPU; si SkiaSharp ho arregla, es pot migrar de tornada al shader. |
| **Cicle de colors >255 items** | `BuildHolderLut`/`BuildCountyLut` usen `(idx-1)%255+1` per wrap-around | Abans: índex clavat a 255 → centenars de comtats/holders verds |
| **Parallel.ForEach síncron a ProcessModule** | Aprofita multi-core I/O | Bloqueja thread pool; `ProcessModulesAsync` fa `await Task.CompletedTask` després de `Parallel.ForEach` |
| **ViewModels manuals** | Control total, sense Magic | Boilerplate `OnPropertyChanged`; fàcil introduir bugs binding |

---

## 8. Deute Tècnic i TODOs Prioritzats

### 🔴 Crític
- [ ] **Race condition cache `ModuleProcessor._moduleCache`**: `LoadModules()` fa `.GetAwaiter().GetResult()` al thread pool → possible deadlock si es crida des del UI thread. **Fix**: fer `LoadModulesAsync` + `await` a `ProcessModulesAsync`.
- [ ] **`Parallel.ForEach` síncron a `ProcessModulesAsync`**: bloqueja thread pool. **Fix**: `Parallel.ForEachAsync` (.NET 6+) o `Task.WhenAll` amb `SemaphoreSlim`.
- [ ] **Sense validació paths a `CreateProfile`**: `GameRoot`/`ModRoot`/`BackupRoot` poden ser buits → error runtime en processat.

### 🟠 Important
- [ ] **Introduir `Microsoft.Extensions.DependencyInjection`**: registrar `IModuleRepository`, `IProjectService`, `ModuleProcessor`, `DefinesProcessor`, `ModuleValidator`.
- [ ] **ViewModel base amb `CommunityToolkit.Mvvm`** (`[ObservableProperty]`, `[RelayCommand]`) → elimina boilerplate `INotifyPropertyChanged`.
- [ ] **Tests unitaris** (xUnit):
  - `ModuleProcessor.ApplyOffset` (various date formats, negative offsets, no-match).
  - `DefinesProcessor.Read/WriteEndDate` (mock FS).
  - `MapLoader.LoadDefinition` (CSW malformat, duplicats).
  - `ModuleValidator.CompareFileContents` (igual, diferent, només a A, només a B).
- [ ] **Virtualització llistes mòduls/fitxers** (`VirtualizingStackPanel` + `ItemsControl` → `ListView` amb `VirtualizingPanel.IsVirtualizing=True`).
- [ ] **LUT cache incremental**: invalidar només províncies modificades (diff `definition.csv`).
- [x] **Internacionalització completada (1.2.1)**: tots els strings de la UI extrets a `es.xaml` / `en.xaml`. Les pestanyes i diàlegs usen `DynamicResource` o `Res()`. Pendent traducció de textos específics de joc a `{GameKey}.{lang}.xaml`.

### 🟢 Millora
- [ ] **Plugins EU4/Imperator/HOI4/Vic3**: nous `IGamePlugin` amb regex i paths específics.
- [ ] **FileSystemWatcher** a `ModRoot` → auto-refresh validació.
- [ ] **Exportar informe validació** (HTML/Markdown) des de `ValidationTab`.
- [ ] **Diff semàntic** (entendre sintaxi Clausewitz) en lloc de línia a línia.
- [ ] **Perfil rendiment**: `BenchmarkDotNet` per a `ModuleProcessor`, `MapLoader.BuildLutInMemory`.
- [ ] **Toast notifications** (ex. `MaterialDesignThemes` Snackbar) en lloc de MessageBox per èxit/progrés.

---

## 9. Regles de Seguretat / Integritat (Lògica)

- **Perfils**: Aïllament per `Profile.Name` (clau única). No hi ha dades compartides entre perfils.
- **Còpia de seguretat**: Escriptura sempre precedida per còpia a `BackupRoot` (preserva timestamps).
- **Offset dates**: Només extensions permeses per `IGamePlugin.IsDateProcessableExtension`.
- **Detecció joc**: `CanHandleGame` cerca fitxers característics; fallback a diàleg d'usuari.
- **Paths**: Validació `Directory.Exists` a `DetectGame` i `Browse` dialogs.

---

## 10. Comandes Útils

```bash
# Build solution (Release)
dotnet build PdxModIDE.sln -c Release

# Build només UI (per test ràpid)
dotnet build PdxModIDE.UI/PdxModIDE.UI.csproj -c Debug

# Executar UI
dotnet run --project PdxModIDE.UI/PdxModIDE.UI.csproj

# Netejar tot
dotnet clean PdxModIDE.sln

# Veure arbre dependències
dotnet msbuild PdxModIDE.sln /t:GenerateRestoreGraphFile /pp:restore.graph
```

**Estructura sortida build**:
```
PdxModIDE.UI/bin/Debug/net8.0-windows/
├── PdxModIDE.UI.exe
├── data/                 # JSON configs (copiat si no existeix)
├── logs/                 # Creat en runtime
├── Themes/               # ResourceDictionaries
└── *.dll (Core, Domain, Data, IO, MapEngine, Project, Rendering, Validation)
```

---

## 11. Variables d'Entorn / Configuració Externa

Cap variable d'entorn obligatòria. Tota configuració a `data/*.json`.

**Paths per defecte** (si l'usuari no configura):
- `GameRoot`: Detectat via `GameRegistry.DetectGame` o diàleg.
- `ModRoot`: Carpeta `mod/` junt a `GameRoot` (convenció Paradox).
- `BackupRoot`: `backups/{ProfileName}/` sota `ModRoot`.

---

## 12. Extensibilitat: Afegir Nou Joc (ex. EU4)

1. Crear `PdxModIDE.Core.Games.EU4.EU4GamePlugin : IGamePlugin`:
   - `GameKey = "EU4"`
   - `DateRegex` adaptat a format EU4 (ex. `\b(\d{4})\.(\d{2})\.(\d{2})\b`)
   - `IsDateProcessableExtension` (afegir `.gfx`, `.gui` si aplica)
   - `GetDefinesPath()` → `defines.txt` ubicació EU4
   - `CanHandleGame` → cerca `eu4.exe` o `defines.txt` amb `start_date` EU4
2. Registrar a `App.OnStartup`: `GameRegistry.Register(new EU4GamePlugin());`
3. Afegir mòduls/fitxers base a `data/modules.json` i `data/files.json` sota key `"EU4"`.
4. (Opcional) Estendre `MapLoader` si format mapa difereix (EU4 usa mateix `definition.csv` + `provinces.png`).

---

## 13. Referències Ràpides Arxius Clau

| Fitxer | Propòsit |
|--------|----------|
| `PdxModIDE.UI/App.xaml.cs` | Bootstrap: registra CK3, dirs data/logs, crash handler |
| `PdxModIDE.UI/MainWindow.xaml.cs` | Theme swap, DataContext=MainViewModel, perfil inicial |
| `PdxModIDE.UI/ViewModels/MainViewModel.cs` | Estat UI complet, comandes, bindings |
| `PdxModIDE.Project/ProjectManager.cs` | Orquestador: perfils, sessió, processat, validació, persistència |
| `PdxModIDE.Core/ModuleProcessor.cs` | Còpia game→mod + offset dates (paral·lel, logging) |
| `PdxModIDE.Core/DefinesProcessor.cs` | Read/Write `end_date` a defines.txt |
| `PdxModIDE.Core/Games/GameRegistry.cs` | Plugin registry + detecció automàtica joc |
| `PdxModIDE.Core/Games/CK3/CK3GamePlugin.cs` | Implementació CK3: regex, extensions, defines path |
| `PdxModIDE.MapEngine/MapLoader.cs` | Càrrega mapa complet + LUT cache + titulars per any |
| `PdxModIDE.MapEngine/TitleHistoryLoader.cs` | Parse `history/titles/*.txt` → `TitleHistory` |
| `PdxModIDE.Rendering/MapRenderer.cs` | Viewport SkiaSharp, zoom/pan, color picker, tooltips. Overlay per CPU (workaround bug child shader). |
| `PdxModIDE.Validation/ModuleValidator.cs` | Diff 3-vies (mod/game/backup) recursiu, cerca bidireccional per entrellaçar addicions/eliminacions |
| `PdxModIDE.UI/DiffViewDialog.cs` | Visor diff costat a costat (estil Compare Notepad++, panells "Original"/"Modified", números de línia, fons de color) |
| `PdxModIDE.Data/DataLoader.cs` | Load/Save JSON genèric `data/*.json` |
| `PdxModIDE.Domain/Models.cs` | Entitats pures (Module, GameFile, Profile, EditingSession) |
| `PdxModIDE.IO/FileOperations.cs` | CopyPreserveTimestamps, ReadTextFile, EnsureDirectory |

---

*Generat: 2026-07-23 | Projecte: PdxModIDE | Versió: 1.4.9 | Stack: .NET 8 / WPF / SkiaSharp 3.116.1 / System.Text.Json*
