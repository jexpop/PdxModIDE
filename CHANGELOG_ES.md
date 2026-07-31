# Changelog - PdxModIDE

Todos los cambios notables de este proyecto se documentarán en este archivo.

El formato está basado en [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
y este proyecto se adhiere a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.5.6]

### Añadido

- **Idioma en los detalles de cultura (pestaña Culturas)**: el panel de detalles ahora también muestra los detalles del idioma de la cultura (de `language = language_xxx`, localizado vía `cultural_languages_l_*.yml`), con una sección desplegable con sus parámetros (`is_shown`, `ai_will_do`, `color`, etc.), replicando la funcionalidad del ethos/herencia. Las definiciones de idioma se parsean desde `common/culture/pillars/*language.txt`, con prioridad de los ficheros del mod sobre el juego. Cada parámetro incluye una explicación localizada (claves `LanguageParam_*_Desc` en `CK3.*.xaml`). El campo del idioma se coloca al final del panel de detalles.

---

## [1.5.5]

### Añadido

- **Herencia en los detalles de cultura (pestaña Culturas)**: el panel de detalles ahora también muestra los detalles de la herencia de la cultura (de `heritage = heritage_xxx`), con una sección desplegable con sus parámetros (`is_shown`, `audio_parameter`, etc.), replicando la funcionalidad del ethos. Las definiciones de herencia se parsean desde `common/culture/pillars/*_heritage.txt`, con prioridad de los ficheros del mod sobre el juego. Cada parámetro incluye una explicación localizada (claves `HeritageParam_*_Desc` en `CK3.*.xaml`).

### Cambiado

- **Orden del panel de detalles de cultura**: los campos ahora se ordenan Origen, Nombre, Color, Ethos, Herencia (de arriba a abajo).

---

## [1.5.4]

### Añadido

- **Ethos en los detalles de cultura (pestaña Culturas)**: el panel de detalles ahora muestra el ethos de la cultura (de `ethos = ethos_xxx`, localizado vía `cultural_traditions_l_*.yml`) y una sección desplegable con sus parámetros (`character_modifier`, `province_modifier`, `county_modifier`, `culture_modifier`, `parameters`, `ai_will_do`, `desc`, etc.). Las definiciones de ethos se parsean desde `common/culture/pillars/*_ethos.txt`, con prioridad de los ficheros del mod sobre el juego. Cada parámetro incluye una explicación localizada (claves `EthosParam_*_Desc` en `CK3.*.xaml`).

---

## [1.5.3]

### Añadido

- **Visualización del color de cultura (pestaña Culturas)**: el panel de detalles ahora muestra el color de la cultura como valor RGB numérico con un swatch visual. Soporta modos `hsv`, `hsv360` y `rgb`, y resuelve referencias `color = <nombre>` contra `common/named_colors/*.txt` (los colores referenciados muestran su nombre de origen).

### Cambiado

- **`CultureLoader` reescrito como parser basado en bloques**: `ParseCultureFile` ya no crea entradas espurias para bloques `color = { ... }`; el color se asigna a la cultura contenedora. Maneja `hsv`/`hsv360`/`rgb` y valores RGB enteros/flotantes.
- **Resolución de colores con nombre en el mapa**: `LoadCultures` resuelve referencias `color = <nombre>` contra `common/named_colors/*.txt` tanto de la raíz del juego como del mod, por lo que todas las culturas base y del mod obtienen color.
- **Herencia de cultura con IDs de personaje tipo string**: el contenido oriental (China/Japón/Corea, ej. `holder = tuyuhun0006`, `japanese_yamato_1 = { ... }`) usa IDs de personaje con nombre en lugar de numéricos. El historial de titulares y las culturas de personajes ahora aceptan tanto IDs numéricos como string, por lo que las provincias orientales sin `culture =` directo heredan correctamente la cultura del titular del condado en el mapa.

### Corregido

- **Archivos de cultura con líneas malformadas (sin `=`) detenían el parseo**: líneas como `khitan { ... }` interrumpían el parser línea a línea, perdiendo todas las culturas posteriores. El parser por bloques ya no se detiene.

---

## [1.5.2]

### Añadido

- **Pestaña Culturas**: nueva pestaña a la derecha de Mapa que muestra culturas agrupadas por herencia en un árbol, priorizando el mod sobre el juego base.
- **Parseo de archivos de cultura**: parser Clausewitz que lee `common/culture/cultures/*.txt` recursivamente, soportando bloques anidados, comentarios y tipos de valor complejos (hsv, cadenas).
- **Localización de culturas**: nombres mostrados cargados de archivos de localización de CK3 (`cultures_l_*.yml` y `cultural_heritages_l_*.yml`) para inglés y español; catalán usa inglés como fallback.
- **Panel de detalles**: al seleccionar una cultura se muestra su nombre localizado, herencia y origen (Base/Mod).
- **Panel de estadísticas**: muestra grupos de herencia totales, grupos con cambios del mod, culturas del mod y del juego base.
- **IGamePlugin.CulturesRelativePath**: nueva propiedad de interfaz para la ruta del directorio de culturas (CK3: `common/culture/cultures`).

### Cambiado

- **Versión actualizada a 1.5.2**: recurso `MainWindow_Title` actualizado en todos los ficheros de idioma, versión catalana unificada a 1.5.2.

### Corregido

- **Robustez del parser de cultura**: `ExtractAttribute` ahora salta correctamente valores con bloques posteriores (ej. `color = hsv { 0.72 0.6 0.76 }`) en lugar de romper el parseo.

---

## [1.5.1]

### Añadido

- **Vista de mapa Cultural (implementación completa)**: la nueva vista Cultural renderiza cada provincia con el nombre y color de su cultura, incluyendo prioridad de fuente Base/Mod/Ambos y búsqueda histórica por año.
- **Carga de datos de cultura**: nuevo `CultureLoader` que analiza definiciones de cultura (`common/culture/cultures/*.txt`), historial de provincias (`history/provinces/*.txt`), historial de titulares de títulos (`history/titles/*.txt`) y culturas de personajes (`history/characters/*.txt`).
- **Herencia de cultura**: las provincias sin `culture =` explícito en su historial heredan la cultura del titular del condado (titular resuelto desde el historial de títulos, cultura desde el historial de personajes).
- **Localización de culturas**: nombres de cultura mostrados cargados desde archivos de localización de CK3 (`cultures_l_*.yml`) para todos los idiomas soportados.
- **Formato de herencia**: las culturas heredadas se muestran como `"Anglosajona (Chelmsford)"` con el nombre de la provincia capital entre paréntesis.
- **Visibilidad de ShowNamesCheck**: `ShowNamesCheck` movido fuera de `TitleModePanel` en XAML para que permanezca visible en la vista Cultural.

### Cambiado

- **Versión actualizada a 1.5.1**: recurso `MainWindow_Title` actualizado en todos los ficheros de idioma.

### Corregido

- **Parseo de culture en bloques de fecha anidados**: el parser ahora usa profundidad + pila de fechas para capturar correctamente `culture =` en líneas separadas dentro de bloques de fecha en archivos de historial de provincias.
- **Parseo de definiciones de cultura a cualquier profundidad**: detecta bloques de cultura anidados dentro de grupos culturales usando `BlockRe` y un enfoque de pila única.
- **Parseo de colores flotantes**: se añadió `TryParseFloatColor()` para manejar valores RGB con decimales en definiciones de color de cultura.
- **Prevención de crash en `Math.Clamp`**: `maxFontSize` se calcula como `Math.Max(8f, boxW * 0.3f)` en lugar de `Math.Clamp` para evitar fallos cuando `boxW < 27`.
- **Visibilidad de etiquetas en el mapa**: se redujo el filtro de bounding-box de etiquetas de `30x20` a `20x12` para mostrar más etiquetas de cultura en el mapa.

---

## [1.5.0]

### Añadido

- **Selector de vista del mapa**: nuevo desplegable (ComboBox) en la pestaña Mapa para cambiar entre tres vistas: General (solo terreno, sin overlay Base/Mod), Titular (comportamiento actual del mapa de títulos con modos holder/condado/ducado/reino/imperio y botón de edición), y Cultural (placeholder para implementación futura).
- **Visibilidad de UI según la vista**: en vista General, los checks Base/Mod, el panel de modos de título y el botón de edición están ocultos. En vista Titular, todos los controles son visibles. En vista Cultural, Base/Mod son visibles pero los modos de título y el botón de edición están ocultos.
- **Fuente mínima obligatoria**: en vistas que no sean General, al menos uno de Base/Mod debe permanecer marcado — desmarcar la última fuente activa no tiene efecto.

### Cambiado

- **Versión actualizada a 1.5.0**: recurso `MainWindow_Title` actualizado en todos los ficheros de idioma.

---

## [1.4.18]

### Corregido

- **Colores de condado correctos para todos los condados (no solo los primeros 255)**: el LUT para todos los modos de superposición del mapa (Titular, Condado, Ducado, Reino, Imperio) se actualizó de `byte[]` (256 entradas máx., wrap-around en 255) a `ushort[]` (65535 entradas máx., sin wrap-around). Las paletas ahora tienen tamaño dinámico en lugar de fijo de 256 entradas. Esto corrige un error donde los condados con índice >255 sobrescribían las entradas de condados anteriores en `indexToCounty`, causando que los condados sobrescritos mostraran el color de otro condado.

### Cambiado

- **Todos los métodos Build*Lut ahora devuelven `ushort[]`**: `BuildHolderLut`, `BuildCombinedHolderLut`, `BuildCountyLut`, `BuildDuchyLut`, `BuildKingdomLut`, `BuildEmpireLut` usan valores LUT de 16 bits, eliminando la lógica de wrap-around `(idx-1)%255+1`.
- **Constructores de paleta con tamaño dinámico**: `BuildHolderPalette` y `BuildCountyPalette` crean bitmaps con el tamaño del índice máximo real en lugar de fijo 256×1.

---

## [1.4.17]

### Añadido

- **El modo Condado del mapa usa los colores reales de landed_titles**: el modo de superposición de Condados ahora lee el atributo `color = { r g b }` de los archivos `common/landed_titles/*.txt` y muestra esos colores en el mapa en lugar de los colores procedurales basados en índice (ángulo áureo HSL).

### Cambiado

- **Prioridad de carga de colores para títulos**: los colores se cargan con la siguiente prioridad: `<modRoot>/common/landed_titles/mod/` (máxima), luego `<modRoot>/common/landed_titles/` (raíz del mod), luego `<gameRoot>/common/landed_titles/` (juego base). Las líneas comentadas (`#color = { ... }`) se ignoran. Los condados sin un `color = { ... }` definido en ninguna fuente usan el color procedural HueSatLum como fallback.

---

## [1.4.16]

### Añadido

- **Claves de título y condado por defecto en ventana de división**: el campo "Título superior" ahora se rellena por defecto con el nombre de la primera baronía reemplazando `b_` por `d_`, y el campo "Condado" con `b_` reemplazado por `c_`.

### Cambiado

- **Comentarios `##MOD_DEL` simplificados**: el prefijo ya no incluye la clave del nuevo título. Cada línea comentada ahora comienza con `##MOD_DEL ` seguido solo del contenido original de la línea.
- **Nuevo archivo de título incluye referencias al padre**: el nuevo archivo de título ahora muestra el título superior original como comentario (`#`) junto al encabezado del nuevo título, y la clave del condado original como comentario junto al encabezado del nuevo condado.

### Corregido

- **Dividir condado ya no sobrescribe archivos existentes**: cuando el archivo de título destino (`d_xxx.txt` etc.) ya existe en el directorio del mod, el nuevo condado ahora se añade dentro del archivo existente en lugar de sobrescribirlo.
- **Detección de condado duplicado en división**: la aplicación ahora verifica si la clave del nuevo condado (`c_xxx`) ya existe en algún archivo `.txt` bajo `common/landed_titles/` del mod, ignorando líneas comentadas con `##MOD_DEL`. Si el bloque existente está activo, la operación se aborta. Si el bloque está muerto (todo `##MOD_DEL`), se permite y se marca con `##MOD_DEL`.
- **Detección de título duplicado en división**: la aplicación ahora verifica si el archivo de título (`d_xxx.txt` etc.) ya existe en `common/landed_titles/`. Si contiene contenido activo, se aborta. Si está vacío (todo `##MOD_DEL`), se permite y se marca.
- **Limpieza de condado original vacío**: tras una división, si el condado original se queda sin baronías activas, todo su bloque se marca con `##MOD_DEL`.
- **Permitir división cuando clave de condado/título coincide con origen**: `KeyExists` se omite cuando la clave del nuevo condado coincide con la original (`newCountyKey == _countyKey`) y `WouldBlockRemainActive` confirma que el original quedaría vacío. Igual para el título cuando coincide con el padre (`newTitleKey == _parentTitle`), añadiendo el nuevo condado al bloque del título padre en el archivo fuente en lugar de crear un archivo override en `mod/`.
- **`WouldBlockRemainActive` sin restricción de ruta**: la comprobación ahora se ejecuta independientemente de en qué archivo `FindBlockInLandedTitles` encontró el bloque.
- **Líneas `##MOD_DEL` filtradas en nuevos archivos de condado**: las líneas que empiezan con `##MOD_DEL` de los atributos del condado original ya no se copian al nuevo archivo de condado.
- **Condado original marcado con `##MOD_DEL` en división CopiedFromGame con mismo nombre**: al dividir un condado de origen del juego con la misma clave, el bloque del condado original ahora se marca correctamente como muerto en la copia del mod.

---

## [1.4.15]

### Corregido

- **CS8625 — null pasado a parámetro no-nullable en `BuildCountyLut`**: se cambió el parámetro `TitleHistoryLoader history` a nullable `TitleHistoryLoader?` para permitir el null intencionado.
- **CS0414 — campo `_lastHolderYear` sin usar en `MapRenderer`**: se eliminó el campo que se asignaba pero nunca se leía.
- **CS8602 — posible desreferencia null de `BaseSourceCheck`/`ModSourceCheck`**: se añadió operador null-forgiving (`!`) en referencias a controles WPF garantizados por XAML.
- **CS8604 — posible argumento null a `HashSet<string>.Contains`**: se añadió guarda explícita `prov.Type == null` antes de llamar a `Contains`.

### Cambiado

- **Compilación libre de warnings**: la solución ahora compila con 0 warnings (antes 4).
- **Selector de carpeta destino en ventana de división de condado**: se añadió un campo "Carpeta destino" con un botón Examinar que abre un selector de carpetas con raíz en `{ModRoot}/common/landed_titles/mod/`. El usuario puede elegir cualquier subdirectorio para escribir el nuevo archivo de títulos.

---

## [1.4.14]

### Añadido

- **Indicador de estado de modo**: una etiqueta centrada en la parte superior de la ventana principal muestra el modo actual (Vistas/Edición), el nivel de jerarquía activo (Condado, Ducado, etc.) y la fuente (Base/Mod). Se oculta cuando la pestaña Mapa no está activa o no hay fuente seleccionada.

### Cambiado

- **Botón "Modo Vistas" / "Modo Edición" renombrado para mostrar la acción**: el botón de alternancia ahora muestra "Ir a Modo Edición" / "Ir a Modo Vistas" en lugar del nombre del modo actual. Ancho aumentado a 140px. El Tooltip ahora muestra el nombre del modo actual.
- **División de condado preserva datos completos de baronías y condado**: los bloques de baronías ahora se analizan con seguimiento de profundidad de llaves. El nuevo archivo de título incluye los bloques completos de baronías originales (atributos como `color`, `cultural_names`, etc.) y los atributos del condado. Los atributos del condado original (excepto `capital`) se trasladan al nuevo condado.
- **Comentarios `##MOD_DEL` limpios**: sin indentación preservada antes de los marcadores `##MOD_DEL`. Las líneas vacías o de solo espacios dentro de bloques comentados se mantienen sin el prefijo.

### Corregido

- **El mapa se actualiza inmediatamente después de dividir**: se llama a `MapLoader.LoadModLandedTitles` tras una división exitosa para que los diccionarios de jerarquía reflejen los cambios. No es necesario reiniciar la aplicación.
- **La integración de la jerarquía del mapa se actualiza en tiempo real**: al cambiar de pestaña y volver a la pestaña Mapa se restaura la etiqueta de estado del modo.

---

## [1.4.13]

### Added

- **Ventana de división de condado muestra las provincias seleccionadas con jerarquía**: al hacer clic en el botón "Dividir condado" se abre una nueva ventana (`SplitCountyWindow`) que lista cada provincia seleccionada con su ID, Barony, County y título superior inmediato (ducado). Los datos se obtienen directamente de la jerarquía cargada de `MapLoader` (CountyToDuchy).
- **El título de la ventana principal ahora usa localización**: el título "Paradox Mod IDE v.1.4.13" se carga desde los diccionarios de idioma mediante `{DynamicResource MainWindow_Title}`.

---

## [1.4.5]

### Added

- **Versionado seguro de ficheros al procesar módulos**: cuando un fichero de destino ya existe en el directorio mod, ahora se renombra con sufijo `_v1`, `_v2`, etc. en lugar de sobrescribirse. El fichero nuevo conserva el nombre original. Si el contenido del fichero existente es idéntico al nuevo, no se produce ni renombrado ni escritura.

### Fixed

- **Mensaje "Process Complete" duplicado**: eliminado `MessageBox.Show` redundante en `DatesTab.xaml.cs` que causaba la aparición de dos ventanas de confirmación tras procesar módulos desde la pestaña Fechas.

---

## [1.4.6]

### Changed

- **Procesado de módulos no recursivo en la pestaña Fechas**: la pestaña Fechas ahora solo procesa los archivos directamente en la ruta del módulo sin recorrer subdirectorios. Añadido el parámetro `bool recurseSubdirectories` a través de `ProcessModulesAsync` / `ProcessModule` para controlar la recursividad.

### Removed

- **Auto-backup al procesar módulos**: eliminadas las copias automáticas de backup a la carpeta de backups del perfil durante el procesado de módulos. Los backups ahora son una operación manual.

---

## [1.4.4]

### Added

- **Selección independiente de módulos para la pestaña Fechas**: la selección de módulos ahora está dividida en dos listas independientes. La pestaña **Módulos** controla la selección global (usada por todas las pestañas excepto Fechas) con checkboxes. La pestaña **Fechas** tiene su propio selector de módulos independiente para el procesado. El selector de módulos anterior en la pestaña Perfil ha sido eliminado.
- **Texto informativo** en las pestañas Módulos y Fechas explicando el ámbito de cada selección de módulos.

### Changed

- **ProjectManager.ProcessModulesAsync** ahora usa `DatesModules` en lugar de `Modules` del perfil, por lo que el procesado solo actúa sobre los módulos seleccionados en la pestaña Fechas.
- **Auto-persistencia**: al marcar/desmarcar un checkbox de módulo en cualquiera de las dos pestañas ahora se guarda inmediatamente en `data/profiles.json`.

---

## [1.4.3]

### Changed

- **Localización del nombre de provincia en el panel de información del mapa**: el campo de nombre de provincia ahora usa `GetLocalizedTitleName()` para mostrar el nombre localizado de los archivos YML del juego en lugar de la clave raw de `definition.csv`. Aplica a todo tipo de provincia (tierra, mar, impasable, etc.); usa la clave raw como fallback si no existe entrada de localización.

---

## [1.4.2]

### Changed

- **Panel de título en la pestaña Mapa**: el panel de título (Barony, County, Holder, Liege) ahora solo se muestra cuando el tipo de provincia seleccionada es `"land"`. Para provincias no terrestres (sea, lake, river, impassable, unknown) el panel permanece oculto incluso si la fuente Base o Mod está activa.

---

## [1.4.1]

### Added

- **Localización de nombres de títulos en el panel de información del mapa**: los nombres de baronía y condado ahora muestran el nombre real localizado en lugar de la clave interna (ej. `b_*`). Los nombres se cargan del campo `name` en `common/landed_titles/*.txt` y de los archivos YML de localización (`localization/{lang}/*.yml`). El idioma sigue la configuración de la aplicación (Inglés/Español/Catalán) con fallback a inglés cuando el idioma no está disponible en el juego.
- **Localización de etiquetas del mapa**: las etiquetas superpuestas en el mapa también usan nombres localizados de las mismas fuentes.
- **Soporte para carpeta `localization/replace/` en mods**: los archivos de localización de reemplazo (`localization/replace/{lang}/*.yml`) sobrescriben la localización normal del mod con prioridad Mod > Base.

### Fixed

- **Parser de localización YML**: ahora maneja correctamente el formato YML de CK3 (`clave:0 "valor"`) que incluye un número de versión tras los dos puntos. Anteriormente el número de versión y las comillas se incluían en el nombre mostrado (ej. `0 "Tenerife"` en lugar de `Tenerife`).

---

## [1.4.0]

### Added

- **Etiquetas de nombres de títulos en el mapa de la pestaña Historia**: nueva casilla "Mostrar nombres" (por perfil, `ShowTitleNames`) que dibuja nombres de territorio (condado/ducado/reino/imperio/titular) directamente en el bitmap renderizado usando SkiaSharp por CPU. Características: tamaño de fuente dinámico proporcional al área del territorio × zoom (limitado 9–18px), rotación a lo largo del eje principal de la forma del territorio (límite ±45°), evitación de solapamiento (territorios más grandes primero, margen 4px), y fondo redondeado semitransparente. Escala el texto para llenar el bounding box cuando es más corto que el ancho de la caja. Los nombres de titulares usan `TitleHistoryLoader.GetHolderAtYear` directo (evitando el bug de wrap 255 del LUT).

### Fixed

- **Baronías con guión en el nombre no se detectaban en `landed_titles`**: el regex de parseo de títulos (`MapLoader.LoadLandedTitlesFrom`) solo admitía `[A-Za-z0-9_]+` en el identificador, por lo que nombres como `b_dvur-chvojno` no coincidían y la baronía (y su provincia asociada) quedaba fuera de `ProvinceToBarony`/`BaronyToCounty`, sin colorear en los modos Condado/Ducado/Reino/Imperio del mapa. Solución: se añadió el guión a la clase de caracteres del regex (`[A-Za-z0-9_-]+`).

---

## [1.3.4]

### Fixed

- **Overlay de titular/condado/ducado/etc roto en pestaña Mapa**: las provincias se mostraban grises en todos los modos de overlay. Causa: `SKShader.CreateImage` como child shader de `SKRuntimeEffect` devuelve 0 en `eval()` en SkiaSharp 3.116.1 (CPU raster). Solución: overlay por CPU en `RenderToBitmap` — lookup por píxel del color de provincia → holderIdx → color de paleta, preservando bordes y highlight. Ver `docs/skia-image-shader-bug-workaround.md`.
- **Crash al cargar el mapa**: `RenderToBitmap` devolvía un `SKBitmap` ya desechado por un `using var` accidental en la variable retornada.

### Changed

- **`RenderToBitmap`**: ahora renderiza terreno+ bordes vía shader (mode=0) y aplica overlay por CPU. Acceso a píxeles por filas con `GetPixels()` + `Marshal.Copy` para rendimiento.
- **`SetHolderMode`**: ya no crea `SKImage` del LUT de holder; almacena el `byte[]` para uso directo en CPU.
- **`BuildShaderCache`**: usa `SKShader.CreateColor(SKColors.Black)` dummy para `holderLut`/`palette` (no usados con mode=0).
- **`HistoryTab.xaml.cs`**: añadido `InvalidateRender()` para invalidación consistente de caché; reemplaza patrón manual `_cachedWidth = -1; QueueRender()`.

### Removed

- **`_holderLutImage` y `_holderLutBackingBitmap`**: ya no son necesarios al no usar shader para overlay.
- **Código diagnóstico**: eliminados `File.WriteAllText` y comparaciones bitmap/image usados durante la investigación del bug.

---

## [1.3.3]

### Changed

- **Formato de paneles en pestaña Mapa**: los headers de los GroupBox "PROVINCIA" y "TÍTULO" ahora se muestran en negrita con tamaño de fuente mayor para destacar sobre los subtítulos.
- **Panel Título reestructurado**: ahora sigue el mismo formato que el panel Provincia, con etiquetas en negrita (Baronía, Condado, Titular, Señor) y valores en una línea separada debajo. Usan `DynamicResource` para traducción correcta según el idioma activo.
- **Valores de Holder y Liege simplificados**: se ha eliminado el prefijo "in {año}" del valor mostrado; ahora se muestra solo el nombre del titular y la fuente ([Mod]/[Base]).
- **Traducciones coherentes**: nuevas claves `HistoryTab_BaronyLabel`, `HistoryTab_CountyLabel`, `HistoryTab_HolderLabel`, `HistoryTab_LiegeLabel` en ES/EN/CA. "Holder" se traduce como "Titular" en ES/CA; "Liege" como "Señor" (ES) y "Senyor" (CA).

---

## [1.3.2]

### Added

- **i18n para campos del panel de provincia**: nuevas claves `HistoryTab_IDLabel`, `HistoryTab_NameLabel`, `HistoryTab_ColorLabel`, `HistoryTab_TypeLabel` (solo etiqueta, sin placeholder) y `MapTerrain_Land`, `MapTerrain_Sea`, `MapTerrain_Lake`, `MapTerrain_River`, `MapTerrain_Impassable`, `MapTerrain_Unknown` para traducción de tipos de terreno en inglés, español y catalán.

### Changed

- **Diseño del panel de provincia**: los campos ID, Nombre, Color y Tipo ahora muestran la etiqueta en negrita con el valor en una línea separada debajo. Nombre usa `TextWrapping` para valores largos.
- **Orden de refresco de idioma**: `ApplyLanguage` y `ApplyTheme` en `MainWindow.xaml.cs` ahora ejecutan `RefreshMergedDictionaries()` antes de establecer la propiedad del ViewModel, asegurando que los manejadores de `PropertyChanged` lean los diccionarios de recursos ya actualizados.

### Fixed

- **Desfase de idioma en pestaña Mapa**: los valores de tipo de terreno (`MapTerrain_*`) y la información de provincia ahora se actualizan inmediatamente al cambiar de idioma, en lugar de mostrar la traducción del idioma anterior.

---

## [1.3.1]

### Added

- **Panel informativo en pestaña Mapa**: cuando no hay provincia seleccionada, la columna izquierda muestra ahora un panel con instrucciones sobre navegación del mapa (botones de zoom, rueda del ratón, arrastrar con clic derecho, ajustar ventana), selección de provincia (clic en cualquier provincia para ver detalles) y capas (activar casillas Base/Mod y modos de superposición). El panel se oculta al hacer clic en una provincia y reaparece al hacer clic en espacio vacío.
- **Nuevas claves i18n**: `HistoryTab_Navigation`, `HistoryTab_Navigation_Text`, `HistoryTab_Selection`, `HistoryTab_Selection_Text`, `HistoryTab_Layers`, `HistoryTab_Layers_Text` en inglés, español y catalán.

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

## [1.4.7]

### Added

- **Botón "Buscar módulos con fecha no configurados" en la pestaña Validación**: nuevo botón que escanea recursivamente la raíz del juego buscando carpetas no configuradas como módulos que contengan archivos con patrones de fecha. Los resultados se muestran en un diálogo informativo (no se modifica ninguna configuración). Usa `Parallel.ForEach` y lectura línea por línea con salida temprana para rendimiento óptimo, omitiendo archivos de más de 1 MB.

### Changed

- **La validación de módulos ya no recorre subdirectorios**: tanto la validación "Todos los módulos" como la de un solo módulo en la pestaña Validación ahora solo listan los archivos directamente en la ruta del módulo sin descender a subdirectorios (`SearchOption.TopDirectoryOnly`). Esto hace que la validación sea consistente con el procesado no recursivo introducido en 1.4.6 para la pestaña Fechas.

### Fixed

- **Guiones bajos (`_`) ocultos en nombres de módulos en pestaña Fechas**: WPF `CheckBox.Content` interpreta los guiones bajos como aceleradores de teclado, ocultándolos. Nombres como `common/landed_titles` se veían como `common/landedtitles`. Corregido usando un `TextBlock` dentro del `CheckBox` en lugar de usar `Content` directamente.
- **Lista de módulos en pestaña Fechas limitada a 6 columnas**: el cálculo dinámico de columnas en `RecalculateLayout()` no tenía límite superior, causando solapamiento de texto con 7 columnas. Limitado a 6 columnas.
- **Módulos recién añadidos no se procesaban hasta reiniciar la app**: `ModuleProcessor._moduleCache` nunca se invalidaba tras añadir, modificar o eliminar módulos, por lo que los nuevos módulos eran invisibles para el procesado. Añadido `_moduleProcessor.InvalidateCache()` tras cada operación CRUD.

---

## [1.4.11]

### Added

- **Selección múltiple de provincias en modo edición (mapa Historia)**: en modo Edición, hacer clic en provincias de tipo tierra las añade o elimina de una selección múltiple. El panel de información muestra valores combinados cuando todas las provincias seleccionadas coinciden, o "(Multiple)" cuando difieren. Hacer clic en una provincia no terrestre limpia la selección y selecciona solo esa. Hacer clic en espacio vacío deselecciona todo.

### Changed

- **El modo edición conserva la superposición de títulos y nombres**: la capa de títulos (titular/condado/ducado/reino/imperio) y las etiquetas "Mostrar nombres" permanecen activas en el mapa al entrar en modo Edición, usando el último modo seleccionado. El check "Mostrar nombres" siempre está visible; los checks de modo de título se ocultan en modo Edición.
- **El botón de alternancia de modo respeta el idioma seleccionado**: el texto y tooltip del botón "Modo Vistas" / "Modo Edición" usan recursos `DynamicResource` (claves `HistoryTab_ModeView/Edit` y tooltip) disponibles en EN, ES y CA.

### Fixed

- **El resaltado múltiple de provincias preserva los bordes**: el pase CPU de resaltado ahora salta los píxeles de borde, por lo que los bordes entre provincias seleccionadas siguen siendo visibles.
- **Provincia no terrestre ya no permanece resaltada al hacer clic en tierra**: cuando se selecciona una provincia no terrestre y luego se hace clic en una de tierra, la no terrestre se elimina del conjunto de selección.

---

## [1.4.12]

### Added

- **Botón "Dividir condado" en el modo edición del mapa**: en modo Edición con la vista de Condados, aparece un botón "Dividir condado" en la parte superior cuando una o más provincias de tierra del mismo condado están seleccionadas. El botón usa texto localizado (EN/ES/CA).

---

## [1.4.10]

## [1.4.9]

### Added

- **El panel de información ahora muestra la jerarquía completa de títulos y nombres de titulares con dinastía**: la pestaña Historia reemplaza las filas "Holder/Liege" con filas de nivel Ducado, Reino e Imperio. Cada nivel muestra el nombre del personaje resuelto desde `history/characters/*.txt` con el apellido de dinastía desde `common/dynasties/*.txt`, con fallback al ID si no se encuentra.
- **Cargadores de personajes y dinastías**: nuevos `CharacterHistoryLoader.cs` y `DynastyLoader.cs` que parsean nombres de personajes y nombres de dinastía (incluyendo archivos `.yml` localizados) desde el juego base y el directorio del mod.
- **Las etiquetas de titulares en el mapa ahora muestran nombres de personaje**: las etiquetas en modo titular renderizan el nombre del personaje (con dinastía) en lugar del nombre del título localizado.

### Changed

- **Título de ventana actualizado a "Paradox Mod IDE v.1.4.9"**: `MainWindow.xaml` refleja la nueva versión.
- **El panel de información se actualiza al cambiar el modo de superposición**: todos los métodos `Apply*Mode` ahora llaman a `UpdateProvinceInfo(_lastProvinceId)` para que el panel se actualice inmediatamente al cambiar de modo.

---

## [1.4.8]

### Changed

- **Las etiquetas del mapa ahora escalan con el tamaño de la provincia**: los nombres de provincia en el mapa de Historia ahora se renderizan con un tamaño de fuente proporcional al bounding box de la provincia (`boxW * 0.14`, clamp 8px–30% del ancho). El texto se reduce automáticamente si supera el 85% del ancho de la provincia.
- **Estilo y colores de las etiquetas del mapa mejorados**: el relleno del texto cambió de blanco sólido sobre rectángulo negro a gris oscuro (#666) dibujado 3 veces para dar grosor, con un borde blanco semitransparente (`SKColor(255,255,255,200)`) para un aspecto limpio estilo CK3, eliminando el rectángulo negro opaco de fondo.

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
