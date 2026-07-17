# PdxModIDE

**IDE para gestión y procesamiento de mods de juegos Paradox Interactive (CK3, EU4, HOI4, etc.)**  
Aplicación WPF (.NET 8) que automatiza el copiado de archivos del juego al mod, aplica offset de fechas y valida diferencias.

**Versión actual:** 1.0.0

---

## Documentación

- **[PROJECT_CONTEXT.md](PROJECT_CONTEXT.md)** — Contexto técnico completo: arquitectura, modelo de datos, módulos clave, convenciones, deuda técnica y referencias rápidas.
- **[CHANGELOG.md](CHANGELOG.md)** — Historial de versiones y cambios (formato Keep a Changelog).

---

## Configuración del Proyecto

### Requisitos
- **.NET 8 SDK** (o runtime para ejecutar)
- **Windows 10/11** (WPF, Windows Forms para diálogos de selección de juego)
- Juego Paradox instalado (CK3 por defecto; plugins para otros juegos)

### Estructura de carpetas esperada
```
PdxModIDE/
├── data/                    # JSON persistentes (perfiles, módulos, archivos, settings, logfilters)
├── logs/                    # Logs de procesamiento por perfil + crash.log
├── Themes/                  # ResourceDictionaries XAML (Light, Dark, CK3, Sepia, Contrast, VSCode)
├── PdxModIDE.sln
├── PdxModIDE.Core/          # Lógica de procesamiento, plugins de juego, DefinesProcessor
├── PdxModIDE.Domain/        # Entidades puras (Module, GameFile, Profile, EditingSession)
├── PdxModIDE.Data/          # Repositorios JSON + DataLoader (persistencia)
├── PdxModIDE.IO/            # Utilidades de archivo (copy, read, timestamps)
├── PdxModIDE.MapEngine/     # Carga mapas Paradox (definition.csv, provinces.png, landed_titles)
├── PdxModIDE.Rendering/     # MapRenderer (SkiaSharp) - viewport, zoom, picker
├── PdxModIDE.Project/       # ProjectManager (orquestador, validación, perfiles)
├── PdxModIDE.UI/            # WPF MVVM (MainWindow, tabs, ViewModels, temas)
└── PdxModIDE.Validation/    # ModuleValidator (diff archivos mod/juego/backup)
```

### Build
```bash
# Debug
dotnet build PdxModIDE.sln --configuration Debug

# Release
dotnet build PdxModIDE.sln --configuration Release

# Publicar single-file (Windows)
dotnet publish PdxModIDE.UI/PdxModIDE.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Ejecutar
```bash
dotnet run --project PdxModIDE.UI/PdxModIDE.UI.csproj
```

---

## Stack Principal

| Capa | Tecnología |
|------|------------|
| **UI** | WPF (.NET 8), MVVM ligero (code-behind + ViewModels), XAML |
| **Core** | C# 12, `System.Threading.Tasks.Parallel`, `System.Text.Json` |
| **Persistencia** | JSON en `data/` (sin BD externa) |
| **Mapa/Render** | **SkiaSharp** (SKBitmap, SKCanvas, SKImage) |
| **Plugins de juego** | Interfaces `IGamePlugin`, `IDateProcessor` (extensible) |
| **DI** | Manual (instanciación directa en `ProjectManager`, `App.xaml.cs`) |
| **Logging** | `File.AppendAllText` a `logs/` + `MessageBox` para errores UI |

---

## Funcionalidades

- **Perfiles de mod**: Múltiples configuraciones (game root, mod root, backup root, year offset, módulos/archivos seleccionados).
- **Detección automática de juego**: `GameRegistry.DetectGame(gameRoot)` lee `game.json`/`launcher-settings.json` y matchea con plugin registrado.
- **Procesamiento de módulos**: Copia recursiva `game_root/path → mod_root/path`; aplica offset de años via regex (`yyyy.m.d` → `yyyy+offset.m.d`); backup automático en `backup_root/path`.
- **Procesamiento paralelo**: `Parallel.ForEach` con grado de paralelismo = núcleos CPU.
- **Validación de módulos/archivos**: Tres modos comparación (Juego vs Mod, Mod vs Backup, Juego vs Backup); diff línea a línea; resumen estadístico.
- **Motor de mapas**: Carga `definition.csv` (ID, RGB, nombre), `default.map` (sea/lakes/rivers/impassable), `landed_titles` (baronías → condados), `provinces.png` (lookup RGB → province ID). Cache LUT binario (16M entries) con hash MD5 de archivos fuente.
- **Visualización de titulares**: `BuildHolderLut(year, TitleHistoryLoader)` → palette 256 colores para renderizado de mapa político histórico.
- **Temas intercambiables**: 6 temas (Light, Dark, CK3, Sepia, Contrast, VSCode Dark/Light); persistencia en `Settings.json`.
- **Pestañas UI**: Perfil, Módulos, Archivos, Fechas, Historial, Validación, Logs, Configuración.

---

## Modelo de Datos (Domain)

```csharp
// PdxModIDE.Domain.Models
Module          { Name, Path, IgnoreExtensions[] }
GameFile        { Name, Path, MapTo? }
Profile         { Id, Name, Game, GameRoot, ModRoot, BackupRoot, YearOffset, ModuleIds[], FileIds[] }
EditingSession  { CurrentProfile, ModulesByGame[game][name], FilesByGame[game][name], AllModulesByName, AllFilesByName }
```

**Persistencia (Data/*.json)** ↔ **Domain** via `ProjectManager.MapToDomain/MapToData`.

---

## Arquitectura de Procesamiento (Módulos)

```
ProjectManager.ProcessModulesAsync(offset?)
    └─ ModuleProcessor.ProcessModulesAsync(Parallel.ForEach)
         └─ ModuleProcessor.ProcessModule(gameKey, moduleName, ...)
              ├─ LoadModules() (cache + IModuleRepository.GetAllAsync)
              ├─ GameRegistry.GetPlugin(gameKey) → IGamePlugin
              ├─ FileOperations.CopyFilePreserveTimestamps / WriteAllText
              ├─ IGamePlugin.DateRegex.Replace(text, match → year+offset)
              └─ Log a logs/{profile}/{module}.log
```

---

## Plugins de Juego (Extensibles)

```csharp
// PdxModIDE.Core.Games.Interfaces
IGamePlugin
{
    string GameKey { get; }           // "ck3", "eu4", ...
    string DisplayName { get; }       // "Crusader Kings III"
    bool CanHandleGame(string gameRoot);
    Regex DateRegex { get; }          // e.g. (\d{1,4})\.(\d{1,2})\.(\d{1,2})
    bool IsDateProcessableExtension(string ext); // .txt, .csv, etc.
    string? GetDefinesPath(string gameRoot);
    string? GetModDefinesPath(string modRoot);
}
```

Registrados en `App.OnStartup`: `GameRegistry.Register(new CK3GamePlugin())`.

---

## Estructura de Datos (Persistencia JSON)

```
data/
├── profiles.json        # List<Profile> (DataProfile)
├── modules.json         # Dict<gameKey, Dict<moduleName, ModuleConfig>>
├── files.json           # Dict<gameKey, Dict<fileKey, FileConfig>>
├── settings.json        # Settings { Theme }
└── logfilters.json      # LogFilters { Levels, Keywords }
```

**ModuleConfig** `{ Path, IgnoreExt[] }`  
**FileConfig** `{ Path, MapTo? }` (permite mapear `game/path.txt` → `mod/different_path.txt`)

---

## Build & Release

```bash
# Compilar todo
dotnet build PdxModIDE.sln -c Release

# Publicar standalone (single file, win-x64)
dotnet publish PdxModIDE.UI -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o ./publish
```

El ejecutable resultante incluye runtime .NET 8 (~70 MB).

---

## Extensibilidad: Añadir Nuevo Juego (ej. EU4)

1. Crear `PdxModIDE.Core.Games.EU4.EU4GamePlugin : IGamePlugin`
   - `GameKey = "eu4"`, `DisplayName = "Europa Universalis IV"`
   - `DateRegex` para formato fechas EU4
   - `CanHandleGame`: detectar `eu4.exe` o `game.json` con `game_id="eu4"`
   - `GetDefinesPath`: `common/defines.lua` (formato Lua, requiere parser distinto)
2. Registrar en `App.OnStartup`: `GameRegistry.Register(new EU4GamePlugin())`
3. Añadir definiciones de módulos/archivos base en `data/modules.json` y `data/files.json` para `eu4`.
4. (Opcional) Parser `DefinesProcessor` específico para Lua si difiere de CK3.

---

## Referencias Rápidas Archivos Clave

| Archivo | Propósito |
|---------|-----------|
| `PdxModIDE.UI/App.xaml.cs` | Bootstrap: registra plugins, logging global, crea carpetas `data/`, `logs/` |
| `PdxModIDE.UI/MainWindow.xaml.cs` | Ventana principal, VM binding, tema, selección perfil inicial |
| `PdxModIDE.Project/ProjectManager.cs` | Orquestador central: perfiles, sesión, procesamiento, validación, persistencia |
| `PdxModIDE.Core/ModuleProcessor.cs` | Lógica copia + offset fechas + logging por módulo (paralelo) |
| `PdxModIDE.Core/DefinesProcessor.cs` | Lectura/escritura `end_date` en `defines.txt` (game + mod) con backup |
| `PdxModIDE.Core/Games/GameRegistry.cs` | Registro/detección plugins `IGamePlugin` |
| `PdxModIDE.Core/Games/CK3/CK3GamePlugin.cs` | Implementación CK3: regex fechas, extensiones procesables, paths defines |
| `PdxModIDE.MapEngine/MapLoader.cs` | Carga definition.csv, default.map, landed_titles, provinces.png; LUT cache |
| `PdxModIDE.MapEngine/TitleHistoryLoader.cs` | Parse `history/titles/*.txt` → `TitleHistory { Holders: SortedList<year, holder> }` |
| `PdxModIDE.Rendering/MapRenderer.cs` | Viewport, zoom, pan, color picker, render holder LUT, tooltips |
| `PdxModIDE.Data/DataLoader.cs` | Load/Save JSON genérico para profiles, modules, files, settings, logfilters |
| `PdxModIDE.Validation/ModuleValidator.cs` | Diff recursivo directorios; comparación byte a byte + diff líneas |
| `PdxModIDE.Domain/Models.cs` | Entidades puras (Module, GameFile, Profile, EditingSession) |
| `PdxModIDE.UI/ViewModels/MainViewModel.cs` | Estado UI: perfiles, módulos/archivos seleccionados, comandos, tema, paths |