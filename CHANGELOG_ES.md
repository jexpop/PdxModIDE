# Changelog - PdxModIDE

Todos los cambios notables de este proyecto se documentarán en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
y este proyecto se adhiere a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.3.0]

### Added

- **Panel de información contextual en pestaña Mapa**: el panel izquierdo de información de provincia/título ahora está oculto por defecto y solo se muestra al hacer clic en una provincia. El bloque "Título" (Barony, County, Holder, Liege) solo es visible cuando al menos uno de los checks "Base" o "Mod" está activo.

### Changed

- **Visibilidad dinámica del panel izquierdo**: se ha añadido `x:Name="InfoPanel"` al `StackPanel` del panel izquierdo en `HistoryTab.xaml`, con `Visibility="Collapsed"` inicial. Se muestra al hacer clic en provincia (`UpdateProvinceInfo`) y se oculta al hacer clic en zona sin provincia.
- **Título condicional a Base/Mod**: el `GroupBox` de Título (`TitleGroup`) solo se muestra si `HasActiveSource()` devuelve true (Base o Mod marcado). Se actualiza tanto al hacer clic en provincia como al cambiar el estado de Base/Mod mientras el panel está visible.

---

## [1.2.2]

### Added

- **Nuevo idioma: Català (ca)**: se añade el Català como tercer idioma disponible. Nuevo archivo `Languages/ca.xaml` con traducción completa de toda la interfaz, `Languages/CK3.ca.xaml` como placeholder, selector radio en `GeneralSettingsWindow`, y soporte en `ApplyLanguage` / `GetSelectedLanguage`.
- **Internacionalización completa de la UI (fase 2)**: ~140 nuevas claves i18n extraídas a `es.xaml` / `en.xaml` para todas las pestañas y cuadros de diálogo:
  - MainWindow (tooltips y headers de tabs)
  - ProfileTab (rutas, botones CRUD, grupo módulos)
  - ModulesTab (edición, botones add/save/delete)
  - DatesTab (offset, end_date, módulos a procesar)
  - HistoryTab (panel provincia/título, zoom, modos, tooltips)
  - ValidationTab (módulos, archivos, comparación, resultados)
  - LogsTab (visor, filtros, configuración)
  - InputDialog (botones Aceptar/Cancelar)
- **Separación de textos generales vs específicos de juego**: los textos generales de la aplicación residen en `es.xaml` / `en.xaml` / `ca.xaml`. Los textos específicos de cada juego van en `{GameKey}.{lang}.xaml` (ej. `CK3.es.xaml`, `CK3.en.xaml`, `CK3.ca.xaml`), cargados dinámicamente según el perfil activo.
- **`RefreshMergedDictionaries()` mejorado**: ahora carga tres diccionarios (tema + idioma general + idioma específico del juego) y se refresca al cambiar de perfil.
- **`GetGameLanguagePath()`**: nuevo método que genera la ruta `Languages/{GameKey}.{language}.xaml` para el diccionario específico del juego activo.
- **Método helper `Res(string key)`** en clases code-behind (MainViewModel, HistoryTab, ValidationTab, DatesTab, LogsTab, App) para resolver strings i18n desde C#.
- **Archivos placeholder**: `Languages/CK3.es.xaml`, `Languages/CK3.en.xaml` y `Languages/CK3.ca.xaml` para futuros textos específicos de CK3.

### Changed

- **Idioma por defecto**: el campo `Language` en `Settings` ahora por defecto es `"en"` (inglés) en lugar de `"es"` (español). La aplicación arranca en inglés si no hay `settings.json` previo.
- **Status codes de validación**: los códigos internos de estado del `ProjectManager` cambian de español a inglés (`"Modified"`, `"Added"`, `"Deleted"`, `"SAME"`, `"CHANGED"`) para consistencia con el idioma por defecto.
- **`ValidationTab`**: la comparación de módulos ahora usa `SelectedIndex` en lugar de comparar strings traducidos del ComboBox, evitando dependencia del idioma activo.
- **`MainWindow.xaml`**: la referencia inicial al diccionario de idioma pasa de `Languages/es.xaml` a `Languages/en.xaml`.
- **Status labels en HistoryTab**: los textos de modo de mapa y etiquetas de información de provincia se muestran en inglés por defecto.

### Fixed

- **Bug en `ApplyLanguage` (MainWindow.xaml.cs)**: el switch de selección de ruta del diccionario de idioma no tenía caso para `"es"`, por lo que al seleccionar Español siempre cargaba el diccionario de inglés.

### Notes

- Los códigos de estado de validación se han unificado a inglés como parte del cambio de idioma por defecto. Los diálogos DiffDialog, DiffChoiceDialog, DiffViewDialog y ValidationTab usan estos códigos para coloreado y filtrado.
- Los textos específicos de juego (CK3) están preparados estructuralmente pero aún vacíos; se poblarán en versiones futuras.

---

## [1.2.0]

### Added

- **Ventana de Ajustes Generales** (`GeneralSettingsWindow`): nueva ventana modal accesible mediante un icono de tuerca (⚙) en la esquina superior derecha de `MainWindow`, con la configuración de la aplicación que no depende de un perfil/mod concreto (Tema visual e Idioma).
- **Infraestructura de internacionalización (i18n)**: nuevo mecanismo de idiomas basado en `ResourceDictionary` XAML, siguiendo el mismo patrón ya usado para los Temas (`Themes/*.xaml` → swap dinámico de diccionario con `DynamicResource`). Carpeta `PdxModIDE.UI/Languages/` con `es.xaml` (por defecto) y `en.xaml`.
- **`Settings.Language`**: nuevo campo en `data/settings.json` (`"language"`, por defecto `"es"`), persistido igual que `Theme`. Propagado a través de `IProjectService.Language`, `ProjectManager.Language` y `MainViewModel.Language`.
- **`MainWindow.ApplyLanguage(string)`**: nuevo método público que recarga el diccionario de idioma sin perder el tema activo (y viceversa), mediante `RefreshMergedDictionaries()`, que recombina ambos diccionarios (tema + idioma) en los recursos de `Application` y de la ventana.
- Selector de idioma (Español/English) en `GeneralSettingsWindow`, con aplicación en caliente (sin reiniciar la aplicación).

### Changed

- **Pestaña "Opciones" eliminada del `TabControl`**: la configuración de Tema (antes en `SettingsTab`, dentro de las pestañas del proyecto) se ha trasladado a la nueva ventana modal `GeneralSettingsWindow`, ya que es configuración de aplicación, no de un mod/perfil concreto. `SettingsTab.xaml`/`.xaml.cs` eliminados.
- `PdxModIDE.UI.csproj`: añadido `<Content Include="Languages\**">` (igual que `Themes\**`) para copiar los diccionarios de idioma al directorio de salida/publicación.

### Notes

- Fase 1 de i18n: por ahora solo se traducen los textos de `GeneralSettingsWindow` (prueba de concepto del mecanismo de cambio de idioma en caliente). El resto de la interfaz (Perfil, Mapa, Fechas, Módulos, Validación, Logs) permanece en español hardcoded; su traducción se abordará en una fase posterior, reutilizando el mismo mecanismo de `ResourceDictionary`.

---

## [1.1.10]

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

## [1.1.9]

### Fixed
- **Parser de `common/landed_titles` perdía títulos con bloques intermedios no-título**: bloques como `cultural_names = { ... }`, `color = { ... }` o `definite_form = { ... }` dentro de un título hacían que su `}` solitario hiciera pop prematuro del título padre del stack. Esto impedía que las baronías siguientes se vincularan a su condado (`BaronyToCounty` quedaba vacío), por lo que `BuildCountyLut`/`BuildHolderLut` nunca encontraban el condado de esas provincias. Añadido contador `nonTitleDepth` que rastrea llaves de bloques no-título para ignorar sus cierres sin afectar al stack de títulos.

---

## [1.1.8]

### Fixed
- **Parser de `history/titles` ignoraba bloques de fecha "en una sola línea"**: formato muy habitual en baronías y bastantes condados de CK3, p.ej. `900.1.1={ holder=140000 liege=k_england }`. El contador de llaves cortaba el procesamiento de la línea (`continue`) en cuanto veía un `}`, sin comprobar si ese cierre correspondía al bloque de fecha (anidado) o al título completo, así que esas líneas nunca llegaban a leerse — afectaba igual a Base y a Mod. Reescrito el parser para calcular el balance neto de llaves de la línea y extraer siempre `holder=`/`liege=` antes de decidir si el título se cierra.
- De paso, se ignoran ahora los comentarios en línea (`# ...`) para evitar falsos positivos al buscar `holder=`/`liege=`.

---

## [1.1.7]

### Fixed
- **Búsqueda recursiva en `history/titles` y `common/landed_titles`**: `TitleHistoryLoader.LoadAll` y `MapLoader.LoadLandedTitles` solo escaneaban el nivel superior de la carpeta. El motor de Paradox procesa recursivamente cualquier subcarpeta dentro de esas rutas (con cualquier nombre, no solo carpetas literales "mod"), así que un mod que organiza sus ficheros de historia/títulos en subcarpetas propias no se estaba leyendo. Ahora ambos usan `SearchOption.AllDirectories`, de forma genérica tanto para Base como para Mod.

---

## [1.1.6]

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

## [1.1.5]

### Added
- **Checks "Base" y "Mod" en pestaña Mapa**: Nuevos checkboxes `BaseSourceCheck` y `ModSourceCheck`, no excluyentes entre sí, situados entre la fecha (con su "Fecha Mod" calculada) y los checks de Titular/Condado/Ducado/Reino/Imperio. Por ahora solo refrescan el mapa al cambiar (`SourceModeChanged`); la lógica de qué datos mostrar según Base/Mod se implementa en la versión 1.1.6.

---

## [1.1.4]

### Added
- **Fecha Mod calculada en pestaña Mapa**: Nueva etiqueta `OffsetLabel` junto al año (antes de los checks de titular/condado/etc.) que muestra la fecha resultante en el mod (`año + YearOffset` del perfil activo), mostrando ambos valores (año base y fecha mod) al mismo tiempo. Solo informativa, no editable; se actualiza al cargar la pestaña, al cambiar de perfil, al modificar el offset y al cambiar el año.

---

## [1.1.3]

### Changed
- **Unificación de pestañas Mapa**: Las dos pestañas "Historia (Base)" y "Historia (Mod)" se han fusionado en una única pestaña llamada "Mapa" (`local:HistoryTab` sin `Mode` fijo en `MainWindow.xaml`).

---

## [1.1.2]

### Changed
- **Texto informativo pestaña Historia**: Eliminado el prefijo "Vista: Mod/Juego Base" del texto mostrado tras cargar el mapa; ahora solo se muestra el recuento de provincias y títulos (`X prov, Y títulos`).

---

## [1.1.1]

### Added
- **Modos Ducados / Reinos / Imperios** en pestaña Historia: Checkboxes "Duc.", "Rey.", "Imp." para colorear mapa por límites de ducado (`d_xxx`), reino (`k_xxx`) e imperio (`e_xxx`).
- **Jerarquía completa de títulos**: `MapLoader.LoadLandedTitles()` ahora construye `CountyToDuchy`, `DuchyToKingdom`, `KingdomToEmpire` desde la pila de títulos anidados.
- **Nuevos LUTs**: `BuildDuchyLut()`, `BuildKingdomLut()`, `BuildEmpireLut()` con paletas y wrap-around de colores.
- **Mutua exclusión extendida**: Los 5 modos (Titular, Condados, Ducados, Reinos, Imperios) se desmarcan entre sí.
- **Labels compactos**: Checkboxes usan abreviaturas (Tit., Cond., Duc., Rey., Imp.) con tooltips para ahorrar espacio en la barra.

### Changed
- **Etiquetas en panel info**: Panel "Título" ahora muestra Baronía, Condado, Ducado, Reino, Imperio, Holder, Liege según modo activo.

---

## [1.1.0]

### Added
- **Modo Condados en pestaña Historia**: Nuevo checkbox "Condados" junto a "Titular" que colorea el mapa por límites de condado (`c_xxx`) en lugar de por holder (personaje). Usa `MapLoader.BuildCountyLut()` → mapea provincia → baronía → condado.
- **Ciclo de colores para >255 items**: En `BuildHolderLut` y `BuildCountyLut`, los índices >255 ahora hacen wrap-around (módulo 255) en lugar de clavarse en 255, evitando que cientos de condados/holders compartan el mismo color verde.
- **Mutua exclusión**: Checkboxes "Titular" y "Condados" se desmarcan mutuamente.

### Fixed
- **Condados verdes**: Al haber >255 condados en CK3, todos a partir del 256 usaban índice 255 (mismo color). Ahora ciclan 1-255.
- **Holders verdes**: Mismo fix aplicado a `BuildHolderLut` para >255 holders únicos.

---

## [1.0.0]

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
- **Internacionalización (i18n) - traducción completa de la UI**: la infraestructura base (`ResourceDictionary` XAML EN/ES) ya existe desde 1.2.0, pero solo cubre `GeneralSettingsWindow`. Falta extraer y traducir los strings hardcoded en español del resto de tabs (`ProfileTab`, `HistoryTab`, `DatesTab`, `ModulesTab`, `ValidationTab`, `LogsTab`) y de `MainViewModel`.

---

## Template for Future Entries

## [X.Y.Z]

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
