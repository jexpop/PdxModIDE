# Changelog - PdxModIDE

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0] - 2026-07-17

### Added
- **Arquitectura modular multi-proyecto**: 9 proyectos .NET 8 (Core, Domain, Data, IO, MapEngine, Project, Rendering, UI, Validation).
- **Sistema de perfiles**: Perfiles por mod con GameRoot, ModRoot, BackupRoot, YearOffset, módulos y archivos seleccionados.
- **Procesador de módulos paralelo**: `ModuleProcessor.ProcessModulesAsync` copia archivos juego→mod aplicando offset de fechas (regex por juego) con `Parallel.ForEach` y logging por módulo.
- **Plugin system para juegos**: `IGamePlugin` + `GameRegistry` con detección automática (`DetectGame`) y fallback a diálogo de selección. Implementado `CK3GamePlugin`.
- **Procesamiento de defines**: `DefinesProcessor` lee/escritura `end_date` en `defines.txt` (game + mod) con backup automático.
- **Map Engine completo**:
  - `MapLoader`: carga `definition.csv`, `default.map`, `landed_titles/*.txt`, `provinces.png/bmp`.
  - LUT cache (16M entradas) persistido en `%LocalAppData%/PdxModIDE/lut_cache` con hash MD5 de fuentes.
  - `TitleHistoryLoader`: parsea `history/titles/*.txt` → `TitleHistory { Holders: SortedList<int, string> }`.
  - `BuildHolderLut`: genera LUT de titulares por año para renderizado.
  - **Modo Condados**: `BuildCountyLut` colorea mapa por límites de condado (`c_xxx`) desde `landed_titles`.
- **Renderizado de mapa**: `MapRenderer` (SkiaSharp) con viewport, zoom/pan, color picker, tooltips provincia/titular.
- **Validación de módulos**: `ModuleValidator` compara recursivamente game/mod/backup; diff línea a línea; resumen por estado (Igual/Modificado/Añadido/Eliminado).
- **Persistencia JSON**: `DataLoader` genérico para profiles, modules, files, settings, logfilters en `data/*.json`.
- **UI WPF (MVVM ligero)**:
  - `MainWindow` + `MainViewModel`: tabs Perfil, Módulos, Archivos, Fechas, Validación, Historial, Logs, Ajustes.
  - Temas dinámicos: Light, Dark, CK3, Sepia, Contraste, VSCode Dark/Light (ResourceDictionary swap).
  - Gestión de perfiles (CRUD, renombrar, detección juego), selección módulos/archivos con checkboxes.
  - Procesado asíncrono con progreso, validación paralela, diff viewer en tabs.
- **Manejo de errores global**: `App.OnStartup` registra `UnhandledException` + `DispatcherUnhandledException` → `logs/crash.log` + MessageBox.

### Changed
- **Target Framework**: .NET 8.0, `Nullable=enable`, `ImplicitUsings=enable`.
- **Estructura de datos**: `Domain` entidades puras; `Data` configs JSON; mapeo bidireccional en `ProjectManager.SyncDomainProfiles`.
- **Inyección de dependencias manual**: `ProjectManager` instancia `ModuleProcessor(ModuleRepository())`; repositorios usan `DataLoader` estático.

### Deprecated
- (Ninguno - versión inicial)

### Removed
- (Ninguno - versión inicial)

### Fixed
- (Ninguno - versión inicial)

### Security
- No se almacenan secrets; paths de juego/mod/backup configurados por usuario en perfil.

---

## [1.1.3] - 2026-07-18

### Changed
- **Unificación de pestañas Mapa**: Las dos pestañas "Historia (Base)" y "Historia (Mod)" se han fusionado en una única pestaña llamada "Mapa" (`local:HistoryTab` sin `Mode` fijo en `MainWindow.xaml`).

---

## [1.1.2] - 2026-07-18

### Changed
- **Texto informativo pestaña Historia**: Eliminado el prefijo "Vista: Mod/Juego Base" del texto mostrado tras cargar el mapa; ahora solo se muestra el recuento de provincias y títulos (`X prov, Y títulos`).

---

## [1.1.0] - 2026-07-18

### Added
- **Modo Condados en pestaña Historia**: Nuevo checkbox "Condados" junto a "Titular" que colorea el mapa por límites de condado (`c_xxx`) en lugar de por holder (personaje). Usa `MapLoader.BuildCountyLut()` → mapea provincia → baronía → condado.
- **Ciclo de colores para >255 items**: En `BuildHolderLut` y `BuildCountyLut`, los índices >255 ahora hacen wrap-around (módulo 255) en lugar de clavarse en 255, evitando que cientos de condados/holders compartan el mismo color verde.
- **Mutua exclusión**: Checkboxes "Titular" y "Condados" se desmarcan mutuamente.

### Fixed
- **Condados verdes**: Al haber >255 condados en CK3, todos a partir del 256 usaban índice 255 (mismo color). Ahora ciclan 1-255.
- **Holders verdes**: Mismo fix aplicado a `BuildHolderLut` para >255 holders únicos.

---

## [1.1.1] - 2026-07-18

### Added
- **Modos Ducados / Reinos / Imperios** en pestaña Historia: Checkboxes "Duc.", "Rey.", "Imp." para colorear mapa por límites de ducado (`d_xxx`), reino (`k_xxx`) e imperio (`e_xxx`).
- **Jerarquía completa de títulos**: `MapLoader.LoadLandedTitles()` ahora construye `CountyToDuchy`, `DuchyToKingdom`, `KingdomToEmpire` desde la pila de títulos anidados.
- **Nuevos LUTs**: `BuildDuchyLut()`, `BuildKingdomLut()`, `BuildEmpireLut()` con paletas y wrap-around de colores.
- **Mutua exclusión extendida**: Los 5 modos (Titular, Condados, Ducados, Reinos, Imperios) se desmarcan entre sí.
- **Labels compactos**: Checkboxes usan abreviaturas (Tit., Cond., Duc., Rey., Imp.) con tooltips para ahorrar espacio en la barra.

### Changed
- **Etiquetas en panel info**: Panel "Título" ahora muestra Baronía, Condado, Ducado, Reino, Imperio, Holder, Liege según modo activo.

---

## [Unreleased]

### Planned
- **Soporte EU4 / Imperator / HOI4 / Victoria 3**: nuevos `IGamePlugin` con regex fechas, defines paths, extensiones procesables.
- **Migración a DI container** (Microsoft.Extensions.DependencyInjection) para `ProjectManager`, repositorios, procesadores.
- **ViewModels base con `INotifyPropertyChanged`** centralizado (actualmente implementación manual en `MainViewModel`).
- **Tests unitarios**: xUnit + Moq para `ModuleProcessor.ApplyOffset`, `DefinesProcessor`, `MapLoader.LoadDefinition`, `ModuleValidator.CompareFileContents`.
- **Paginación / virtualización** en listas de módulos/archivos (actualmente `ObservableCollection` completa).
- **Perfil de rendimiento**: benchmark `ProcessModulesAsync` con `BenchmarkDotNet`; optimizar I/O paralelo (actualmente `Parallel.ForEach` sincrónico sobre I/O).
- **LUT cache incremental**: invalidar solo provincias cambiadas en lugar de rebuild completo.
- **Notificaciones toast** en UI (actualmente MessageBox para errores).
- **Settings persistentes por usuario** (theme, último perfil, paths recientes) → ya en `Settings.json` pero extender.
- **Validación incremental**: watcher `FileSystemWatcher` en ModRoot para actualizar estado validación en tiempo real.
- **Exportación de diff**: HTML/Markdown report de validación.
- **Internacionalización (i18n)**: `Resources.resx` EN/ES para strings UI.

---

## Template for Future Entries

## [X.Y.Z] - YYYY-MM-DD

### Added
- Feature descriptions

### Changed
- Changes to existing functionality

### Deprecated
- Soon-to-be-removed features

### Removed
- Removed features

### Fixed
- Bug fixes

### Security
- Vulnerability patches