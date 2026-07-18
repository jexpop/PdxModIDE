# Changelog - PdxModIDE

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.1.10] - 2026-07-18

### Changed
- **Nombres completos en checkboxes de modo de título**: Los modos "Tit.", "Cond.", "Duc.", "Rey.", "Imp." ahora se muestran como "Titular", "Condado", "Ducado", "Reino", "Imperio" respectivamente.
- **Visibilidad condicional de modos de título**: Los checkboxes de modo (Titular/Condado/Ducado/Reino/Imperio) solo se muestran cuando al menos uno de los checks "Base" o "Mod" está activo. Si se desactivan ambos, los modos de título se ocultan.
- **Selección por defecto**: Al activar "Base" o "Mod" sin ningún modo de título activo, se selecciona automáticamente "Titular".

### Fixed
- **Siempre un modo activo**: Ahora no se puede desmarcar el último modo de título mientras "Base" o "Mod" esté activo. Si el usuario intenta desmarcarlo, se re-marca "Titular" automáticamente.
- **Modo no aplicado tras carga de mapa**: Si el usuario activaba "Base" o "Mod" antes de que el mapa terminara de cargarse (carga asíncrona), `SourceModeChanged` retornaba temprano por `_mapLoaded == false` y nunca se aplicaba el modo de título. Al finalizar `DoLoad` ahora se llama a `ReapplyActiveMode()` si hay una fuente activa.
- **Datos del mod sobrescritos por copias base en mod**: Cuando el mod contenía copias de archivos base de `history/titles` más un archivo personalizado, `TitleHistoryLoader.LoadAll` ignoraba los títulos duplicados (`if (!AllTitles.ContainsKey)`) y el primero en orden alfabético ganaba — normalmente la copia base, no el dato personalizado. Añadido parámetro `overwriteDuplicates` para que el mod siempre tenga prioridad.
- **Estructura de landed_titles no se actualizaba al cambiar fuente**: `MapLoader` solo cargaba la estructura de landed_titles del juego base. Al activar "Mod", la estructura de baronías/condados/ducados etc. del mod no se aplicaba. Añadido `SaveBaseSnapshot()`, `LoadModLandedTitles(modRoot)` y `ResetToBase()` para cambiar la estructura según la fuente activa (Base → base, Mod → mod, Ambos → mod).

---

## [1.1.9] - 2026-07-18

### Fixed
- **Parser de `common/landed_titles` perdía títulos con bloques intermedios no-título**: bloques como `cultural_names = { ... }`, `color = { ... }` o `definite_form = { ... }` dentro de un título hacían que su `}` solitario hiciera pop prematuro del título padre del stack. Esto impedía que las baronías siguientes se vincularan a su condado (`BaronyToCounty` quedaba vacío), por lo que `BuildCountyLut`/`BuildHolderLut` nunca encontraban el condado de esas provincias. Añadido contador `nonTitleDepth` que rastrea llaves de bloques no-título para ignorar sus cierres sin afectar al stack de títulos.

---

## [1.1.8] - 2026-07-18

### Fixed
- **Parser de `history/titles` ignoraba bloques de fecha "en una sola línea"**: formato muy habitual en baronías y bastantes condados de CK3, p.ej. `900.1.1={ holder=140000 liege=k_england }`. El contador de llaves cortaba el procesamiento de la línea (`continue`) en cuanto veía un `}`, sin comprobar si ese cierre correspondía al bloque de fecha (anidado) o al título completo, así que esas líneas nunca llegaban a leerse — afectaba igual a Base y a Mod. Reescrito el parser para calcular el balance neto de llaves de la línea y extraer siempre `holder=`/`liege=` antes de decidir si el título se cierra.
- De paso, se ignoran ahora los comentarios en línea (`# ...`) para evitar falsos positivos al buscar `holder=`/`liege=`.

---

## [1.1.7] - 2026-07-18

### Fixed
- **Búsqueda recursiva en `history/titles` y `common/landed_titles`**: `TitleHistoryLoader.LoadAll` y `MapLoader.LoadLandedTitles` solo escaneaban el nivel superior de la carpeta. El motor de Paradox procesa recursivamente cualquier subcarpeta dentro de esas rutas (con cualquier nombre, no solo carpetas literales "mod"), así que un mod que organiza sus ficheros de historia/títulos en subcarpetas propias no se estaba leyendo. Ahora ambos usan `SearchOption.AllDirectories`, de forma genérica tanto para Base como para Mod.

---

## [1.1.6] - 2026-07-18

### Added
- **Lógica funcional de los checks "Base"/"Mod"**: Ahora determinan de dónde sale la información de titulares mostrada en el mapa (pestaña Mapa):
  - **Solo Base**: usa `history/titles` del juego base, con el año tal cual está en el `TextBox` de fecha.
  - **Solo Mod**: usa `history/titles` del mod, aplicando el offset del perfil (año + `YearOffset`) para que la fecha buscada coincida con las fechas ya desplazadas en los ficheros del mod.
  - **Ambos activos**: prioridad al dato del Mod (con offset); si no hay holder para esa fecha en el mod, se usa el del juego base (sin offset).
  - **Ninguno activo**: se muestra el mapa general de tierra/mar por defecto, igual que antes de esta función, independientemente de si Titular/Condado/Ducado/Reino/Imperio está marcado.
  - Aplica también a los modos Condado/Ducado/Reino/Imperio (mismo gating; su información estructural no varía entre base y mod).
- **Colores de "sin datos" en modo LUT**: cuando un modo de título está activo pero una provincia no tiene dato (titular/condado/etc.), ahora se pinta tierra en gris y mar en azul (antes todo salía en un gris plano uniforme, sin distinguir mar). Cambio en el shader de `MapRenderer`.
- **`MapLoader.BuildCombinedHolderLut`**: nuevo método que combina el holder de Base y de Mod por provincia con la prioridad Mod > Base descrita arriba.
- **Panel de información de provincia**: al hacer clic en una provincia, el "Holder"/"Liege" mostrados ahora respetan los checks Base/Mod activos (con offset para Mod) e indican entre corchetes de qué fuente proceden (`[Mod]` / `[Base]`).

---

## [1.1.5] - 2026-07-18

### Added
- **Checks "Base" y "Mod" en pestaña Mapa**: Nuevos checkboxes `BaseSourceCheck` y `ModSourceCheck`, no excluyentes entre sí, situados entre la fecha (con su "Fecha Mod" calculada) y los checks de Titular/Condado/Ducado/Reino/Imperio. Por ahora solo refrescan el mapa al cambiar (`SourceModeChanged`); la lógica de qué datos mostrar según Base/Mod se implementa en la versión 1.1.6.

---

## [1.1.4] - 2026-07-18

### Added
- **Fecha Mod calculada en pestaña Mapa**: Nueva etiqueta `OffsetLabel` junto al año (antes de los checks de titular/condado/etc.) que muestra la fecha resultante en el mod (`año + YearOffset` del perfil activo), mostrando ambos valores (año base y fecha mod) al mismo tiempo. Solo informativa, no editable; se actualiza al cargar la pestaña, al cambiar de perfil, al modificar el offset y al cambiar el año.

---

## [1.1.3] - 2026-07-18

### Changed
- **Unificación de pestañas Mapa**: Las dos pestañas "Historia (Base)" y "Historia (Mod)" se han fusionado en una única pestaña llamada "Mapa" (`local:HistoryTab` sin `Mode` fijo en `MainWindow.xaml`).

---

## [1.1.2] - 2026-07-18

### Changed
- **Texto informativo pestaña Historia**: Eliminado el prefijo "Vista: Mod/Juego Base" del texto mostrado tras cargar el mapa; ahora solo se muestra el recuento de provincias y títulos (`X prov, Y títulos`).

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

## [1.1.0] - 2026-07-18

### Added
- **Modo Condados en pestaña Historia**: Nuevo checkbox "Condados" junto a "Titular" que colorea el mapa por límites de condado (`c_xxx`) en lugar de por holder (personaje). Usa `MapLoader.BuildCountyLut()` → mapea provincia → baronía → condado.
- **Ciclo de colores para >255 items**: En `BuildHolderLut` y `BuildCountyLut`, los índices >255 ahora hacen wrap-around (módulo 255) en lugar de clavarse en 255, evitando que cientos de condados/holders compartan el mismo color verde.
- **Mutua exclusión**: Checkboxes "Titular" y "Condados" se desmarcan mutuamente.

### Fixed
- **Condados verdes**: Al haber >255 condados en CK3, todos a partir del 256 usaban índice 255 (mismo color). Ahora ciclan 1-255.
- **Holders verdes**: Mismo fix aplicado a `BuildHolderLut` para >255 holders únicos.

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
