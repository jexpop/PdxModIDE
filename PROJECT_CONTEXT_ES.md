# PROJECT_CONTEXT.md - PdxModIDE

> Documento de contexto técnico generado automáticamente. No editar manualmente salvo actualizaciones mayores.

---

## 1. Visión General

**PdxModIDE** — IDE de escritorio (WPF, .NET 8, C#) para creación y gestión de mods de juegos Paradox Interactive (CK3, EU4, etc.).

**Función principal**: Automatizar la copia de archivos del juego base al directorio del mod aplicando un **offset de años** a todas las fechas encontradas (regex por juego), con validación diff game/mod/backup, renderizado de mapa y gestión de perfiles.

**Stack Principal**:
- **.NET 8** / C# 12 / WPF (XAML + code-behind + ViewModels manuales)
- **SkiaSharp** (renderizado mapa, LUT, paletas)
- **System.Text.Json** (persistencia JSON en `data/`)
- **Parallel / Task** (procesado módulos, validación, carga mapa)
- **No DI container** (instanciación manual en `ProjectManager`)

**Versión actual**: 1.3.4 (ver `CHANGELOG_ES.md`, `CHANGELOG_EN.md`, `CHANGELOG_CA.md`). Solution: `PdxModIDE.sln` (9 proyectos).

---

## 2. Arquitectura

### 2.1 Estructura de Proyectos (Solution)

```
PdxModIDE.sln
├── PdxModIDE.Domain          # Entidades puras (Module, GameFile, Profile, EditingSession)
├── PdxModIDE.Data            # Repositorios + DataLoader (JSON) + configs (ModuleConfig, FileConfig, Settings, LogFilters)
├── PdxModIDE.IO              # Utilidades FS (FileOperations, Paths)
├── PdxModIDE.Core            # Lógica núcleo: ModuleProcessor, DefinesProcessor, GameRegistry, IGamePlugin, CK3GamePlugin
├── PdxModIDE.MapEngine       # MapLoader, TitleHistoryLoader, ProvinceInfo, LUT cache
├── PdxModIDE.Rendering       # MapRenderer (SkiaSharp viewport, zoom, pan, tooltips)
├── PdxModIDE.Project         # IProjectService + ProjectManager (orquestador principal)
├── PdxModIDE.Validation      # ModuleValidator (diff recursivo, comparación byte/linea)
└── PdxModIDE.UI              # WPF App, MainWindow, ViewModels, Tabs, Temas, Idiomas, Dialogs
```

### 2.2 Dependencias entre Proyectos

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

> **Nota**: No hay inyección de dependencias automática. `ProjectManager` crea `new ModuleProcessor(new ModuleRepository())` en constructor.

### 2.3 Flujo de Datos Principal (Procesar Módulos)

```
MainViewModel.ProcessModulesCommand
    → ProjectManager.ProcessModulesAsync(offsetOverride)
        → ModuleProcessor.ProcessModulesAsync(gameKey, modules, gameRoot, modRoot, backupRoot, offset, profileName)
            → Parallel.ForEach(moduleNames) → ProcessModule(...)
                → IGamePlugin.DateRegex.Replace(text, match => year+offset)
                → FileOperations.CopyFilePreserveTimestamps / WriteAllText
                → Log por módulo en logs/{profile}/{module}.log
```

**Sincronización**: `ModuleProcessor` cachea módulos en `_moduleCache` (thread-safe con `lock`). `InvalidateCache()` limpia cache.

---

## 3. Dependencias Principales (NuGet)

| Proyecto | Paquete | Versión | Uso |
|----------|---------|---------|-----|
| `PdxModIDE.UI` | `SkiaSharp` / `SkiaSharp.Views.WPF` | 3.116.1 | Render mapa, LUT, paletas |
| `PdxModIDE.MapEngine` | `SkiaSharp` | 3.116.1 | Decode provinces.png, build LUT bitmap |
| `PdxModIDE.Core` | `Microsoft.Extensions.Logging.Abstractions` | 8.x | (Opcional) logging abstraido |
| Todos | `System.Text.Json` | Built-in | Serialización `data/*.json` |
| `PdxModIDE.UI` | `Microsoft.Xaml.Behaviors.Wpf` | 1.1.x | (Si se usa) behaviors XAML |

> `Directory.Build.props` centraliza `<TargetFramework>net8.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`.

---

## 4. Modelo de Datos

### 4.1 Entidades de Dominio (`PdxModIDE.Domain.Models`)

| Clase | Propiedades Clave | Notas |
|-------|-------------------|-------|
| `Module` | `Name`, `Path`, `IgnoreExtensions (IReadOnlyList<string>)` | Inmutable (ctor only) |
| `GameFile` | `Name`, `Path`, `MapTo?` | `MapTo` permite mapear path juego → path mod distinto |
| `Profile` | `Id (Guid)`, `Name`, `Game`, `GameRoot`, `ModRoot`, `BackupRoot`, `YearOffset`, `ModuleIds[]`, `FileIds[]`, `SelectedModules`, `SelectedFiles` | `Selected*` se resuelven en `EditingSession` |
| `EditingSession` | `CurrentProfile`, `ModulesByGame`, `FilesByGame`, `AllModulesByName`, `AllFilesByName` | Construida en `ProjectManager.BuildSession`; resuelve referencias `ModuleIds`→`Module` |

### 4.2 Configs de Persistencia (`PdxModIDE.Data`)

| Clase | Archivo JSON | Descripción |
|-------|--------------|-------------|
| `DataProfile` | `data/profiles.json` | Mapea 1:1 a `Domain.Profile` + serialización |
| `ModuleConfig` | `data/modules.json` | `{ Path, IgnoreExt[] }` por `gameKey → moduleName` |
| `FileConfig` | `data/files.json` | `{ Path, MapTo? }` por `gameKey → fileKey` |
| `Settings` | `data/settings.json` | `{ Theme, Language }` |
| `LogFilters` | `data/logfilters.json` | Filtros de log por perfil (no usado activamente) |

**Convención IDs**: `moduleName` = key en JSON = nombre carpeta relativa (ej. `common/landed_titles`). `fileKey` = nombre lógico (ej. `defines`).

### 4.3 Estructura Archivos `data/`

```
data/
├── profiles.json       # List<DataProfile>
├── modules.json        # Dict<gameKey, Dict<moduleName, ModuleConfig>>
├── files.json          # Dict<gameKey, Dict<fileKey, FileConfig>>
├── settings.json       # Settings { Theme, Language }
└── logfilters.json     # LogFilters { ProfileFilters[] }
```

---

## 5. Módulos y Componentes Clave

### 5.1 `ModuleProcessor` (`PdxModIDE.Core`)

**Responsabilidad**: Copia recursiva game→mod aplicando offset de fechas.

```csharp
public void ProcessModule(string gameKey, string moduleName, 
    string gameRoot, string modRoot, string backupRoot, int offset, string profileName)
```

- Usa `IGamePlugin.DateRegex` (ej. CK3: `\b(\d{1,4})\.(\d{1,2})\.(\d{1,2})\b`).
- `IGamePlugin.IsDateProcessableExtension(ext)` filtra extensiones (`.txt`, `.csv`, `.yml`).
- Backup previo en `backupRoot/{relPath}` (preserva timestamps).
- Log por módulo: `logs/{profileName}/{moduleName}.log` (append).
- Paralelismo: `Parallel.ForEach` con `MaxDegreeOfParallelism = Environment.ProcessorCount`.

**Métodos clave**:
- `ApplyOffset(string text, int offset, IGamePlugin)` → regex replace.
- `ProcessModulesAsync` → wrapper Task para UI async.

### 5.2 `DefinesProcessor` (`PdxModIDE.Core`)

**Responsabilidad**: Leer/escritura `end_date` en `defines.txt`.

```csharp
ReadEndDate(gameRoot, gameKey)        // busca defines.txt en gameRoot
ReadModEndDate(modRoot, gameKey)      // busca en modRoot
WriteEndDate(gameRoot, modRoot, backupRoot, newDate, gameKey)
```

- Backup automático antes de escribir.
- Usa `IGamePlugin.GetDefinesPath()` → relativo (ej. `game/defines.txt`).
- Regex: `end_date\s*=\s*(\d{4})\.(\d{2})\.(\d{2})`.

### 5.3 `GameRegistry` + `IGamePlugin` (`PdxModIDE.Core.Games`)

**Patrón**: Plugin por juego. Registro estático `GameRegistry.Register(plugin)`.

```csharp
interface IGamePlugin {
    string GameKey { get; }
    string DisplayName { get; }
    Regex DateRegex { get; }
    bool IsDateProcessableExtension(string ext);
    string GetDefinesPath();
    bool CanHandleGame(string gameRoot);  // detección automática
}
```

**Implementado**: `CK3GamePlugin` (`GameKey="CK3"`).
- `DateRegex`: `\b(\d{1,4})\.(\d{1,2})\.(\d{1,2})\b`
- `ProcessableExt`: `.txt`, `.csv`, `.yml`, `.lua`
- `DefinesPath`: `game/defines.txt`
- `CanHandleGame`: busca `game/defines.txt` con `end_date` o `game_version` CK3.

**Detección**: `GameRegistry.DetectGame(gameRoot)` itera plugins ordenados por longitud key desc.

### 5.4 `MapLoader` (`PdxModIDE.MapEngine`)

**Carga completa mapa CK3**:

| Paso | Archivo | Salida |
|------|---------|--------|
| `LoadDefinition` | `definition.csv` | `ProvincesById`, `ProvincesByColor`, `ProvinceToBarony` |
| `LoadDefaultMap` | `default.map` | `Sea`, `Lakes`, `Rivers`, `Impassable`, `ImpassableSeas` (HashSet<int>) |
| `LoadLandedTitles` | `common/landed_titles/*.txt` | `ProvinceToBarony`, `BaronyToCounty`, `CountyToDuchy`, `DuchyToKingdom`, `KingdomToEmpire` |
| `MarkTerrainTypes` | — | `ProvinceInfo.Type` ∈ {sea, lake, river, impassable, land, unknown} |
| `BuildOrLoadLut` | — | `Lut[16_777_216] byte` (cache MD5 definition.csv + default.map) |
| `BuildPixelData` | `provinces.png/bmp` | `ProvinceIdMap[int[]]` (w*h), `MapWidth`, `MapHeight` |

**LUT Cache**: `%LocalAppData%/PdxModIDE/lut_cache/{lut_types.bin, lut_meta.json}`. Hash MD5 de fuentes.

**TitleHistoryLoader**: Parsea `history/titles/*.txt` → `TitleHistory { Holders: SortedList<int, string> }` (año → holder). Usado por `MapLoader.BuildHolderLut(year, history, out indexToHolder)`.

**Modo Condados**: `BuildCountyLut(out indexToCounty)` (sin parámetro año, los límites no cambian) mapea provincia → baronía (`ProvinceToBarony`) → condado (`BaronyToCounty`). Genera LUT 16M entradas coloreando por condado; índices >255 hacen wrap-around (módulo 255) para evitar colisión de color.

**Modos Ducados/Reinos/Imperios**: Nuevos métodos `BuildDuchyLut`, `BuildKingdomLut`, `BuildEmpireLut` usan la jerarquía completa `CountyToDuchy` → `DuchyToKingdom` → `KingdomToEmpire` para colorear por cada nivel. En pestaña Mapa: checkboxes mutuamente excluyentes (Tit./Cond./Duc./Rey./Imp.) con tooltips.

### 5.5 `ModuleValidator` (`PdxModIDE.Validation`)

**Diff recursivo tres vías**: Mod vs Backup, Game vs Backup, Game vs Mod.

```csharp
ValidateModuleSingle(moduleName, ComparisonType) → List<FileComparisonResult>
ValidateAllAsync() → List<ModuleValidationResult> (paralelo)
```

`FileComparisonResult`: `{ RelativePath, Status (Modified/Added/Deleted/SAME/CHANGED), DiffLines? }`.
Diff línea a línea (simple, no LCS).

**IgnoreExt**: Configurable por módulo (`ModuleConfig.IgnoreExt`).

### 5.6 `ProjectManager` (`PdxModIDE.Project`)

**Orquestador principal** — implementa `IProjectService`.

**Estado**:
- `_dataProfiles`, `_dataModules`, `_dataFiles`, `_dataSettings`, `_dataLogFilters` (caché JSON)
- `_domainProfiles`, `CurrentDataProfile`, `CurrentProfile`, `CurrentSession` (`EditingSession`)

**Métodos clave**:
| Método | Descripción |
|--------|-------------|
| `Load()` | Carga todo JSON + `SyncDomainProfiles()` |
| `SaveAll()` | Persiste todos los JSON |
| `SelectProfile(name)` | Cambia perfil activo + `BuildSession` |
| `CreateProfile(name, game)` | Nuevo perfil + persistencia |
| `CreateProfileWithGameDetection(name, gameRoot)` | Detecta juego + crea |
| `ProcessModulesAsync(offset?)` | Delega a `ModuleProcessor` |
| `ValidateAllAsync()` | Delega a `ModuleValidator` (paralelo) |
| `GetGameModules(gameKey)` | `ModuleConfig` dict |
| `GetAllModules()` | `Domain.Module` dict nested read-only |

**BuildSession**: Construye `EditingSession` resolviendo `ModuleIds`/`FileIds` → objetos `Module`/`GameFile` reales.

### 5.7 `DataLoader` (`PdxModIDE.Data`)

**Genérico Load/Save JSON**:

```csharp
static T Load<T>(string file, T defaultValue)
static void Save<T>(string file, T data)
```

Archivos en `data/` (crea directorio si no existe). `JsonSerializerOptions: WriteIndented=true`.

### 5.8 UI — `MainViewModel` + Tabs (`PdxModIDE.UI.ViewModels`)

**MainViewModel**: Estado UI completo.
- `Profiles: ObservableCollection<ProfileViewModel>`
- `CurrentProfile`, `CurrentSession`
- `GameModules`, `GameFiles` (agrupados por juego)
- `SelectedModules`, `SelectedFiles` (checkboxes)
- `GameRoot`, `ModRoot`, `BackupRoot`, `YearOffset` (bindings two-way)
- `Theme` (cambio dispara `ApplyTheme` en `MainWindow`)
- `Language` (cambio dispara `ApplyLanguage` en `MainWindow`)
- Comandos: `ProcessModulesCommand`, `ValidateAllCommand`, `SaveProfileCommand`, `DetectGameCommand`, `Browse*Command`.

**Tabs** (UserControls en `UI/`):
- `ProfileTab`: CRUD perfiles, detección juego, paths.
- `ModulesTab`: Lista módulos por juego, checkbox selección, add/edit/delete module.
- `FilesTab`: Lista archivos, checkbox, mapTo editable.
- `DatesTab`: Leer end_date game/mod, escribir nuevo end_date.
- `ValidationTab`: Validar todo / módulo individual / archivo individual; grid resultados + diff viewer.
- `HistoryTab` (pestaña "Mapa", antes dos pestañas "Historia (Base)"/"Historia (Mod)" ahora unificadas): Mapa interactivo (SkiaSharp). 5 modos mutuamente excluyentes (checkboxes con tooltips):
  - **Titular** (Tit.): Colorea por holder (personaje) en año `YearBox` → `BuildHolderLut(year, TitleHistoryLoader)`.
  - **Condados** (Cond.): Colorea por límites de condado (`c_xxx`) → `BuildCountyLut()`.
  - **Ducados** (Duc.): Colorea por límites de ducado (`d_xxx`) → `BuildDuchyLut()`.
  - **Reinos** (Rey.): Colorea por límites de reino (`k_xxx`) → `BuildKingdomLut()`.
  - **Imperios** (Imp.): Colorea por límites de imperio (`e_xxx`) → `BuildEmpireLut()`.
  Click provincia → panel info muestra Baronía, Condado, Ducado, Reino, Imperio, Holder, Liege según modo.
  - **Nota técnica**: el overlay se aplica por CPU (workaround bug de `SKShader.CreateImage` como child shader). `RenderToBitmap` renderiza terreno+bordes via shader (mode=0), luego itera píxeles y aplica color de paleta según LUT de holder. Usa `InvalidateRender()` para invalidación de caché.
- `LogsTab`: Filtros log (no implementado completamente).

**Temas**: `ResourceDictionary` swap en `MainWindow.ApplyTheme(theme)`. Archivos en `Themes/*.xaml`.

### 5.9 `GeneralSettingsWindow` + Internacionalización (`PdxModIDE.UI`)

**Ajustes de aplicación** (no ligados a un perfil/mod): ventana modal (`Window`, no `UserControl`) abierta desde un icono de tuerca (⚙) en la esquina superior derecha de `MainWindow` (`BtnGeneralSettings_Click`). Contiene:

- **Tema visual**: mismos 7 temas que antes vivían en la desaparecida pestaña "Opciones" (`SettingsTab`, eliminada en 1.2.0).
- **Idioma**: selector Español / English / Català.

**Mecanismo i18n**: `ResourceDictionary` XAML, mismo patrón que Temas. Carpeta `PdxModIDE.UI/Languages/` (`es.xaml`, `en.xaml`, `ca.xaml`) con claves `system:String` (ej. `Settings_Title`, `Settings_ThemeSection`). Consumido en XAML vía `{DynamicResource Clave}` para permitir cambio en caliente sin reiniciar.

```
MainWindow.ApplyTheme(theme)      → actualiza _currentThemePath
MainWindow.ApplyLanguage(language) → actualiza _currentLanguagePath
    └─ RefreshMergedDictionaries()  → recombina AMBOS diccionarios (tema + idioma)
                                       en Application.Resources y Window.Resources,
                                       para que cambiar uno no elimine el otro.
```

**Persistencia**: `Settings.Language` (`data/settings.json`, campo `"language"`, default `"en"`) — mismo flujo que `Theme`: `IProjectService.Language` → `ProjectManager.Language` → `MainViewModel.Language` → `MainViewModel.SaveSettings()`.

**Fase 2 (completada en 1.2.2)**: Todos los textos de la interfaz han sido extraídos a los diccionarios de idioma (`es.xaml` / `en.xaml` / `ca.xaml`) y todas las pestañas (Perfil, Mapa, Módulos, Fechas, Validación, Logs) y cuadros de diálogo utilizan `{DynamicResource ...}` en XAML o `Res("key")` en code-behind. El cambio de idioma afecta a toda la aplicación al instante.

**Arquitectura de ficheros**: Los textos generales de la aplicación están en `es.xaml` / `en.xaml` / `ca.xaml`. Los textos específicos de cada juego van en ficheros separados `{GameKey}.{lang}.xaml` (ej. `CK3.es.xaml`, `CK3.en.xaml`, `CK3.ca.xaml`), cargados automáticamente según el perfil activo mediante `RefreshMergedDictionaries()`.

---

## 6. Convenciones y Estilo

| Área | Convención |
|------|------------|
| Namespaces | `PdxModIDE.{Project}.{Feature}` |
| Naming | PascalCase (tipos), camelCase (props/params), UPPER_SNAKE (consts) |
| Inmutabilidad | `Domain` entities: `readonly` props, ctor only; `Data` configs: setters públicos para JSON |
| Async | `Task`/`Task<T>` en repositorios y procesadores; `Parallel.ForEach` para CPU-bound I/O mixto |
| Logging | `File.AppendAllText(logs/...)` manual; `crash.log` en `App.OnStartup` |
| DI | Manual en `ProjectManager` constructor; no container |
| UI Pattern | Code-behind + ViewModel (sin framework MVVM); `INotifyPropertyChanged` manual en `MainViewModel` |
| Serialización | `System.Text.Json`; `JsonPropertyName` no usado (props públicas = nombres JSON) |
| Paths | `Path.Combine` siempre; `FileOperations.EnsureDirectory` antes de escribir |
| Error Handling | `try/catch` en UI commands → `MessageBox.Show`; crash global → `logs/crash.log` |

---

## 7. Decisiones de Diseño Clave

| Decisión | Justificación | Trade-off / Deuda |
|----------|---------------|-------------------|
| **9 proyectos separados** | Separación clara dominio/datos/núcleo/UI; testabilidad | Más boilerplate; build algo más lento |
| **Sin DI container** | Simplicidad, cero dependencias extra | Acoplamiento `ProjectManager`→`ModuleProcessor` concreto |
| **JSON plano en `data/`** | Sin BD, portable, editable a mano | No transaccional; concurrencia naïf (último gana) |
| **Regex fechas por juego** | Flexibilidad (CK3/EU4 formatos distintos) | Regex simple; no parsea contexto (ej. `start_date` vs `end_date`) |
| **Backup automático** | Seguridad ante errores offset | Duplica espacio; no limpieza automática |
| **LUT 16M bytes cacheado** | Render instantáneo mapa; evita rebuild | 16 MB RAM + disco; invalidación solo por hash archivos fuente |
| **Overlay CPU en vez de shader** | `SKShader.CreateImage` como child shader de `SKRuntimeEffect` devuelve 0 en `eval()` en SkiaSharp 3.116.1 (CPU raster). Workaround: renderizar terreno+bordes via shader, aplicar overlay (holder/condado/ducado/etc) en CPU iterando píxeles con `Marshal.Copy`. | Overlay 100% CPU; si SkiaSharp lo arregla, se puede migrar de vuelta al shader. |
| **Ciclo de colores >255 items** | `BuildHolderLut`/`BuildCountyLut` usan `(idx-1)%255+1` para wrap-around | Antes: índice clavado en 255 → cientos de condados/holders verdes |
| **Parallel.ForEach síncrono en ProcessModule** | Aprovecha multi-core I/O | Bloquea thread pool; `ProcessModulesAsync` hace `await Task.CompletedTask` tras `Parallel.ForEach` |
| **ViewModels manuales** | Control total, sin Magic | Boilerplate `OnPropertyChanged`; fácil introducir bugs binding |

---

## 8. Deuda Técnica y TODOs Priorizados

### 🔴 Crítico
- [ ] **Race condition cache `ModuleProcessor._moduleCache`**: `LoadModules()` hace `.GetAwaiter().GetResult()` en thread pool → posible deadlock si se llama desde UI thread. **Fix**: hacer `LoadModulesAsync` + `await` en `ProcessModulesAsync`.
- [ ] **`Parallel.ForEach` síncrono en `ProcessModulesAsync`**: bloquea thread pool. **Fix**: `Parallel.ForEachAsync` (.NET 6+) o `Task.WhenAll` con `SemaphoreSlim`.
- [ ] **Sin validación paths en `CreateProfile`**: `GameRoot`/`ModRoot`/`BackupRoot` pueden ser vacíos → error runtime en procesado.

### 🟠 Importante
- [ ] **Introducir `Microsoft.Extensions.DependencyInjection`**: registrar `IModuleRepository`, `IProjectService`, `ModuleProcessor`, `DefinesProcessor`, `ModuleValidator`.
- [ ] **ViewModel base con `CommunityToolkit.Mvvm`** (`[ObservableProperty]`, `[RelayCommand]`) → elimina boilerplate `INotifyPropertyChanged`.
- [ ] **Tests unitarios** (xUnit):
  - `ModuleProcessor.ApplyOffset` (varios formatos fecha, offsets negativos, no-match).
  - `DefinesProcessor.Read/WriteEndDate` (mock FS).
  - `MapLoader.LoadDefinition` (CSV malformado, duplicados).
  - `ModuleValidator.CompareFileContents` (igual, distinto, solo en A, solo en B).
- [ ] **Virtualización listas módulos/archivos** (`VirtualizingStackPanel` + `ItemsControl` → `ListView` con `VirtualizingPanel.IsVirtualizing=True`).
- [ ] **LUT cache incremental**: invalidar solo provincias modificadas (diff `definition.csv`).
- [x] **Internacionalización completada (1.2.2)**: todos los strings de la UI extraídos a `es.xaml` / `en.xaml` / `ca.xaml`. Las pestañas y diálogos usan `DynamicResource` o `Res()`. Pendiente traducción de textos específicos de juego a `{GameKey}.{lang}.xaml`.

### 🟢 Mejora
- [ ] **Plugins EU4/Imperator/HOI4/Vic3**: nuevos `IGamePlugin` con regex y paths específicos.
- [ ] **FileSystemWatcher** en `ModRoot` → auto-refresh validación.
- [ ] **Exportar reporte validación** (HTML/Markdown) desde `ValidationTab`.
- [ ] **Diff semántico** (entender sintaxis Clausewitz) en lugar de línea a línea.
- [ ] **Perfil rendimiento**: `BenchmarkDotNet` para `ModuleProcessor`, `MapLoader.BuildLutInMemory`.
- [ ] **Toast notifications** (ej. `MaterialDesignThemes` Snackbar) en vez de MessageBox para éxito/progreso.

---

## 9. Reglas de Seguridad / Integridad (Lógica)

- **Perfiles**: Aislamiento por `Profile.Name` (clave única). No hay datos compartidos entre perfiles.
- **Backup**: Escritura siempre precedida por copia a `BackupRoot` (preserva timestamps).
- **Offset fechas**: Solo extensiones permitidas por `IGamePlugin.IsDateProcessableExtension`.
- **Detección juego**: `CanHandleGame` busca archivos característicos; fallback a diálogo usuario.
- **Paths**: Validación `Directory.Exists` en `DetectGame` y `Browse` dialogs.

---

## 10. Comandos Útiles

```bash
# Build solution (Release)
dotnet build PdxModIDE.sln -c Release

# Build solo UI (para test rápido)
dotnet build PdxModIDE.UI/PdxModIDE.UI.csproj -c Debug

# Ejecutar UI
dotnet run --project PdxModIDE.UI/PdxModIDE.UI.csproj

# Limpiar todo
dotnet clean PdxModIDE.sln
```

**Estructura salida build**:
```
PdxModIDE.UI/bin/Debug/net8.0-windows/
├── PdxModIDE.UI.exe
├── data/                 # JSON configs (copiado si no existe)
├── logs/                 # Creado en runtime
├── Themes/               # ResourceDictionaries
├── Languages/            # ResourceDictionaries de idioma
└── *.dll (Core, Domain, Data, IO, MapEngine, Project, Rendering, Validation)
```

---

## 11. Variables de Entorno / Configuración Externa

Ninguna variable de entorno obligatoria. Toda configuración en `data/*.json`.

**Paths por defecto** (si usuario no configura):
- `GameRoot`: Detectado via `GameRegistry.DetectGame` o diálogo.
- `ModRoot`: Carpeta `mod/` junto a `GameRoot` (convención Paradox).
- `BackupRoot`: `backups/{ProfileName}/` bajo `ModRoot`.

---

## 12. Extensibilidad: Añadir Nuevo Juego (ej. EU4)

1. Crear `PdxModIDE.Core.Games.EU4.EU4GamePlugin : IGamePlugin`:
   - `GameKey = "EU4"`
   - `DateRegex` adaptado a formato EU4 (ej. `\b(\d{4})\.(\d{2})\.(\d{2})\b`)
   - `IsDateProcessableExtension` (añadir `.gfx`, `.gui` si aplica)
   - `GetDefinesPath()` → `defines.txt` ubicación EU4
   - `CanHandleGame` → busca `eu4.exe` o `defines.txt` con `start_date` EU4
2. Registrar en `App.OnStartup`: `GameRegistry.Register(new EU4GamePlugin());`
3. Añadir módulos/archivos base en `data/modules.json` y `data/files.json` bajo key `"EU4"`.
4. (Opcional) Extender `MapLoader` si formato mapa difiere (EU4 usa mismo `definition.csv` + `provinces.png`).

---

## 13. Referencias Rápidas Archivos Clave

| Archivo | Propósito |
|---------|-----------|
| `PdxModIDE.UI/App.xaml.cs` | Bootstrap: registra CK3, dirs data/logs, crash handler |
| `PdxModIDE.UI/MainWindow.xaml.cs` | Theme/Language swap, DataContext=MainViewModel, perfil inicial |
| `PdxModIDE.UI/ViewModels/MainViewModel.cs` | Estado UI completo, comandos, bindings |
| `PdxModIDE.Project/ProjectManager.cs` | Orquestador: perfiles, sesión, procesado, validación, persistencia |
| `PdxModIDE.Core/ModuleProcessor.cs` | Copia game→mod + offset fechas (paralelo, logging) |
| `PdxModIDE.Core/DefinesProcessor.cs` | Read/Write `end_date` en defines.txt |
| `PdxModIDE.Core/Games/GameRegistry.cs` | Plugin registry + detección automática juego |
| `PdxModIDE.Core/Games/CK3/CK3GamePlugin.cs` | Implementación CK3: regex, extensiones, defines path |
| `PdxModIDE.MapEngine/MapLoader.cs` | Carga mapa completo + LUT cache + titulares por año |
| `PdxModIDE.MapEngine/TitleHistoryLoader.cs` | Parse `history/titles/*.txt` → `TitleHistory` |
| `PdxModIDE.Rendering/MapRenderer.cs` | Viewport SkiaSharp, zoom/pan, color picker, tooltips. Overlay por CPU (workaround bug `SKShader.CreateImage` como child shader devuelve 0). |
| `PdxModIDE.Validation/ModuleValidator.cs` | Diff 3-vías (mod/game/backup) recursivo |
| `PdxModIDE.Data/DataLoader.cs` | Load/Save JSON genérico `data/*.json` |
| `PdxModIDE.Domain/Models.cs` | Entidades puras (Module, GameFile, Profile, EditingSession) |
| `PdxModIDE.IO/FileOperations.cs` | CopyPreserveTimestamps, ReadTextFile, EnsureDirectory |

---

*Generado: 2026-07-19 | Proyecto: PdxModIDE | Versión: 1.3.4 | Stack: .NET 8 / WPF / SkiaSharp 3.116.1 / System.Text.Json*
