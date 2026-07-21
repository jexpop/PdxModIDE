# Changelog - PdxModIDE

Tots els canvis notables d'aquest projecte es documentaran en aquest fitxer.

El format es basa en [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
i aquest projecte s'adhereix a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.4.5]

### Added

- **Versionat segur de fitxers en processar mòduls**: quan un fitxer de destinació ja existeix al directori mod, ara es renombra amb sufix `_v1`, `_v2`, etc. en lloc de sobreescriure's. El fitxer nou conserva el nom original. Si el contingut del fitxer existent és idèntic al nou, no es produeix ni renombrament ni escriptura.

### Fixed

- **Missatge "Process Complete" duplicat**: eliminat `MessageBox.Show` redundant a `DatesTab.xaml.cs` que causava l'aparició de dues finestres de confirmació després de processar mòduls des de la pestanya Dates.

---

## [1.4.6]

### Changed

- **Processament de mòduls no recursiu a la pestanya Dates**: la pestanya Dates ara només processa els fitxers directament a la ruta del mòdul sense recórrer subdirectoris. Afegit el paràmetre `bool recurseSubdirectories` a través de `ProcessModulesAsync` / `ProcessModule` per controlar la recursivitat.

### Removed

- **Auto-backup en processar mòduls**: eliminades les còpies automàtiques de backup a la carpeta de backups del perfil durant el processament de mòduls. Els backups ara són una operació manual.

---

## [1.4.4]

### Added

- **Selecció independent de mòduls per a la pestanya Dates**: la selecció de mòduls ara està dividida en dues llistes independents. La pestanya **Mòduls** controla la selecció global (utilitzada per totes les pestanyes excepte Dates) amb checkboxes. La pestanya **Dates** té el seu propi selector de mòduls independent per al processament. El selector de mòduls anterior a la pestanya Perfil ha estat eliminat.
- **Text informatiu** a les pestanyes Mòduls i Dates explicant l'abast de cada selecció de mòduls.

### Changed

- **ProjectManager.ProcessModulesAsync** ara utilitza `DatesModules` en lloc de `Modules` del perfil, de manera que el processament només actua sobre els mòduls seleccionats a la pestanya Dates.
- **Auto-persistència**: en marcar/desmarcar un checkbox de mòdul a qualsevol de les dues pestanyes ara es guarda immediatament a `data/profiles.json`.

---

## [1.4.3]

### Changed

- **Localització del nom de província al panell d'informació del mapa**: el camp de nom de província ara utilitza `GetLocalizedTitleName()` per mostrar el nom localitzat dels fitxers YML del joc en lloc de la clau raw de `definition.csv`. Aplica a tot tipus de província (terra, mar, impassable, etc.); utilitza la clau raw com a fallback si no existeix entrada de localització.

---

## [1.4.2]

### Changed

- **Panell de títol a la pestanya Mapa**: el panell de títol (Barony, County, Holder, Liege) ara només es mostra quan el tipus de província seleccionada és `"land"`. Per a províncies no terrestres (sea, lake, river, impassable, unknown) el panell roman ocult fins i tot si la font Base o Mod està activa.

---

## [1.4.1]

### Added

- **Localització de noms de títols al panell d'informació del mapa**: els noms de baronia i comtat ara mostren el nom real localitzat en lloc de la clau interna (p. ex. `b_*`). Els noms es carreguen del camp `name` a `common/landed_titles/*.txt` i dels fitxers YML de localització (`localization/{lang}/*.yml`). L'idioma segueix la configuració de l'aplicació (Anglès/Castellà/Català) amb fallback a anglès quan l'idioma no està disponible al joc.
- **Localització d'etiquetes del mapa**: les etiquetes superposades al mapa també utilitzen noms localitzats de les mateixes fonts.
- **Suport per a la carpeta `localization/replace/` en mods**: els fitxers de localització de reemplaçament (`localization/replace/{lang}/*.yml`) sobreescriuen la localització normal del mod amb prioritat Mod > Base.

### Fixed

- **Parser de localització YML**: ara gestiona correctament el format YML de CK3 (`clau:0 "valor"`) que inclou un número de versió després dels dos punts. Anteriorment el número de versió i les cometes s'incloïen al nom mostrat (p. ex. `0 "Tenerife"` en lloc de `Tenerife`).

---

## [1.4.0]

### Added

- **Etiquetes de noms de títols al mapa de la pestanya Història**: nova casella "Mostrar noms" (per perfil, `ShowTitleNames`) que dibuixa noms de territori (comtat/ducat/regne/imperi/titular) directament al bitmap renderitzat usant SkiaSharp per CPU. Característiques: mida de font dinàmica proporcional a l'àrea del territori × zoom (limitada 9–18px), rotació al llarg de l'eix principal de la forma del territori (límit ±45°), evitació de solapament (territoris més grans primer, marge 4px), i fons arrodonit semitransparent. Escala el text per omplir el bounding box quan és més curt que l'amplada de la caixa. Els noms de titulars usen `TitleHistoryLoader.GetHolderAtYear` directe (evitant el bug de wrap 255 del LUT).

### Fixed

- **Baronies amb guionet al nom no es detectaven a `landed_titles`**: el regex de parseig de títols (`MapLoader.LoadLandedTitlesFrom`) només admetia `[A-Za-z0-9_]+` a l'identificador, per la qual cosa noms com `b_dvur-chvojno` no coincidien i la baronia (i la seva província associada) quedava fora de `ProvinceToBarony`/`BaronyToCounty`, sense acolorir-se en els modes Comtat/Ducat/Regne/Imperi del mapa. Solució: s'ha afegit el guionet a la classe de caràcters del regex (`[A-Za-z0-9_-]+`).

---

## [1.3.4]

### Fixed

- **Overlay de titular/comtat/ducat/etc trencat a la pestanya Mapa**: les províncies es mostraven grises en tots els modes d'overlay. Causa: `SKShader.CreateImage` com a child shader de `SKRuntimeEffect` retorna 0 en `eval()` a SkiaSharp 3.116.1 (CPU raster). Solució: overlay per CPU a `RenderToBitmap` — lookup per píxel del color de província → holderIdx → color de paleta, preservant vores i highlight. Veure `docs/skia-image-shader-bug-workaround.md`.
- **Crash en carregar el mapa**: `RenderToBitmap` retornava un `SKBitmap` ja disposat per un `using var` accidental a la variable retornada.

### Changed

- **`RenderToBitmap`**: ara renderitza terreny+vores via shader (mode=0) i aplica overlay per CPU. Accés a píxels per files amb `GetPixels()` + `Marshal.Copy` per rendiment.
- **`SetHolderMode`**: ja no crea `SKImage` del LUT d'holder; emmagatzema el `byte[]` per a ús directe a CPU.
- **`BuildShaderCache`**: utiliza `SKShader.CreateColor(SKColors.Black)` dummy per a `holderLut`/`palette` (no usats amb mode=0).
- **`HistoryTab.xaml.cs`**: afegit `InvalidateRender()` per invalidació consistent de cache; reemplaça patró manual `_cachedWidth = -1; QueueRender()`.

### Removed

- **`_holderLutImage` i `_holderLutBackingBitmap`**: ja no són necessaris al no utilitzar shader per overlay.
- **Codi diagnòstic**: eliminats `File.WriteAllText` i comparacions bitmap/image usats durant la investigació del bug.

---

## [1.3.3]

### Changed

- **Format de panells a la pestanya Mapa**: els headers dels GroupBox "PROVÍNCIA" i "TÍTOL" ara es mostren en negreta amb mida de font més gran per destacar sobre els subtítols.
- **Panell Títol reestructurat**: ara segueix el mateix format que el panell Província, amb etiquetes en negreta (Baronia, Comtat, Titular, Senyor) i valors en una línia separada a sota. Utilitzen `DynamicResource` per a la traducció correcta segons l'idioma actiu.
- **Valors de Holder i Liege simplificats**: s'ha eliminat el prefix "in {any}" del valor mostrat; ara es mostra només el nom del titular i la font ([Mod]/[Base]).
- **Traduccions coherents**: noves claus `HistoryTab_BaronyLabel`, `HistoryTab_CountyLabel`, `HistoryTab_HolderLabel`, `HistoryTab_LiegeLabel` en CA/ES/EN. "Holder" es tradueix com "Titular" a CA/ES; "Liege" com "Senyor" (CA) i "Señor" (ES).

---

## [1.3.2]

### Added

- **i18n per als camps del panell de província**: noves claus `HistoryTab_IDLabel`, `HistoryTab_NameLabel`, `HistoryTab_ColorLabel`, `HistoryTab_TypeLabel` (només etiqueta, sense placeholder) i `MapTerrain_Land`, `MapTerrain_Sea`, `MapTerrain_Lake`, `MapTerrain_River`, `MapTerrain_Impassable`, `MapTerrain_Unknown` per a la traducció de tipus de terreny en anglès, espanyol i català.

### Changed

- **Disseny del panell de província**: els camps ID, Nom, Color i Tipus ara mostren l'etiqueta en negreta amb el valor en una línia separada a sota. Nom usa `TextWrapping` per a valors llargs.
- **Ordre de refresc d'idioma**: `ApplyLanguage` i `ApplyTheme` a `MainWindow.xaml.cs` ara executen `RefreshMergedDictionaries()` abans d'establir la propietat del ViewModel, assegurant que els gestors de `PropertyChanged` llegeixin els diccionaris de recursos ja actualitzats.

### Fixed

- **Desfasament d'idioma a la pestanya Mapa**: els valors de tipus de terreny (`MapTerrain_*`) i la informació de província ara s'actualitzen immediatament en canviar d'idioma, en lloc de mostrar la traducció de l'idioma anterior.

---

## [1.3.1]

### Added

- **Panell informatiu a la pestanya Mapa**: quan no hi ha cap província seleccionada, la columna esquerra ara mostra un panell amb instruccions sobre navegació del mapa (botons de zoom, roda del ratolí, arrossegar amb clic dret, ajustar finestra), selecció de província (clic a qualsevol província per veure'n detalls) i capes (activar caselles Base/Mod i modes de superposició). El panell s'oculta en fer clic a una província i reapareix en fer clic a espai buit.
- **Noves claus i18n**: `HistoryTab_Navigation`, `HistoryTab_Navigation_Text`, `HistoryTab_Selection`, `HistoryTab_Selection_Text`, `HistoryTab_Layers`, `HistoryTab_Layers_Text` en anglès, espanyol i català.

---

## [1.3.0]

### Added

- **Panell d'informació contextual a la pestanya Mapa**: el panell esquerre d'informació de província/títol ara està ocult per defecte i només es mostra en fer clic a una província. El bloc "Títol" (Barony, County, Holder, Liege) només és visible quan almenys un dels checks "Base" o "Mod" està actiu.

### Changed

- **Visibilitat dinàmica del panell esquerre**: s'ha afegit `x:Name="InfoPanel"` al `StackPanel` del panell esquerre a `HistoryTab.xaml`, amb `Visibility="Collapsed"` inicial. Es mostra en fer clic a província (`UpdateProvinceInfo`) i s'oculta en fer clic a zona sense província.
- **Títol condicional a Base/Mod**: el `GroupBox` de Títol (`TitleGroup`) només es mostra si `HasActiveSource()` retorna true (Base o Mod marcat). S'actualitza tant en fer clic a província com en canviar l'estat de Base/Mod mentre el panell és visible.

---

## [1.2.2]

### Added

- **Nou idioma: Català (ca)**: s'afegeix el Català com a tercer idioma disponible. Nou fitxer `Languages/ca.xaml` amb traducció completa de tota la interfície, `Languages/CK3.ca.xaml` com a placeholder, selector ràdio a `GeneralSettingsWindow`, i suport a `ApplyLanguage` / `GetSelectedLanguage`.
- **Internacionalització completa de la UI (fase 2)**: ~140 noves claus i18n extretes a `es.xaml` / `en.xaml` per a totes les pestanyes i quadres de diàleg:

- **Internacionalització completa de la UI (fase 2)**: ~140 noves claus i18n extretes a `es.xaml` / `en.xaml` per a totes les pestanyes i quadres de diàleg:
  - MainWindow (tooltips i headers de tabs)
  - ProfileTab (rutes, botons CRUD, grup mòduls)
  - ModulesTab (edició, botons add/save/delete)
  - DatesTab (offset, end_date, mòduls a processar)
  - HistoryTab (panell província/títol, zoom, modes, tooltips)
  - ValidationTab (mòduls, fitxers, comparació, resultats)
  - LogsTab (visor, filtres, configuració)
  - InputDialog (botons Acceptar/Cancel·lar)
- **Separació de textos generals vs específics de joc**: els textos generals de l'aplicació resideixen a `es.xaml` / `en.xaml`. Els textos específics de cada joc van a `{GameKey}.{lang}.xaml` (ex. `CK3.es.xaml`, `CK3.en.xaml`), carregats dinàmicament segons el perfil actiu.
- **`RefreshMergedDictionaries()` millorat**: ara carrega tres diccionaris (tema + idioma general + idioma específic del joc) i es refresca en canviar de perfil.
- **`GetGameLanguagePath()`**: nou mètode que genera la ruta `Languages/{GameKey}.{language}.xaml` per al diccionari específic del joc actiu.
- **Mètode helper `Res(string key)`** en classes code-behind (MainViewModel, HistoryTab, ValidationTab, DatesTab, LogsTab, App) per a resoldre strings i18n des de C#.
- **Fitxers placeholder**: `Languages/CK3.es.xaml` i `Languages/CK3.en.xaml` per a futurs textos específics de CK3.

### Changed

- **Idioma per defecte**: el camp `Language` a `Settings` ara per defecte és `"en"` (anglès) en lloc de `"es"` (espanyol). L'aplicació arrenca en anglès si no hi ha cap `settings.json` previ.
- **Status codes de validació**: els codis interns d'estat del `ProjectManager` canvien d'espanyol a anglès (`"Modified"`, `"Added"`, `"Deleted"`, `"SAME"`, `"CHANGED"`) per a consistència amb l'idioma per defecte.
- **`ValidationTab`**: la comparació de mòduls ara usa `SelectedIndex` en lloc de comparar strings traduïts del ComboBox, evitant dependència de l'idioma actiu.
- **`MainWindow.xaml`**: la referència inicial al diccionari d'idioma passa de `Languages/es.xaml` a `Languages/en.xaml`.
- **Status labels a HistoryTab**: els textos de mode de mapa i etiquetes d'informació de província es mostren en anglès per defecte.

### Fixed

- **Bug a `ApplyLanguage` (MainWindow.xaml.cs)**: el switch de selecció de ruta del diccionari d'idioma no tenia cas per a `"es"`, per la qual cosa en seleccionar Espanyol sempre carregava el diccionari d'anglès.

### Notes

- Els codis d'estat de validació s'han unificat a anglès com a part del canvi d'idioma per defecte. Els diàlegs DiffDialog, DiffChoiceDialog, DiffViewDialog i ValidationTab usen aquests codis per a coloració i filtratge.
- Els textos específics de joc (CK3) estan preparats estructuralment però encara buits; es poblaran en versions futures.

---

## [1.2.0]

### Added

- **Finestra d'Ajustos Generals** (`GeneralSettingsWindow`): nova finestra modal accessible mitjançant una icona d'engranatge (⚙) a la cantonada superior dreta de `MainWindow`, amb la configuració de l'aplicació que no depèn d'un perfil/mod concret (Tema visual i Idioma).
- **Infraestructura d'internacionalització (i18n)**: nou mecanisme d'idiomes basat en `ResourceDictionary` XAML, seguint el mateix patró ja usat per als Temes (`Themes/*.xaml` → swap dinàmic de diccionari amb `DynamicResource`). Carpeta `PdxModIDE.UI/Languages/` amb `es.xaml` (per defecte) i `en.xaml`.
- **`Settings.Language`**: nou camp a `data/settings.json` (`"language"`, per defecte `"es"`), persistit igual que `Theme`. Propagat a través de `IProjectService.Language`, `ProjectManager.Language` i `MainViewModel.Language`.
- **`MainWindow.ApplyLanguage(string)`**: nou mètode públic que recarrega el diccionari d'idioma sense perdre el tema actiu (i viceversa), mitjançant `RefreshMergedDictionaries()`, que recombina ambdós diccionaris (tema + idioma) als recursos de `Application` i de la finestra.
- Selector d'idioma (Espanyol/English) a `GeneralSettingsWindow`, amb aplicació en calent (sense reiniciar l'aplicació).

### Changed

- **Pestanya "Opcions" eliminada del `TabControl`**: la configuració de Tema (abans a `SettingsTab`, dins de les pestanyes del projecte) s'ha traslladat a la nova finestra modal `GeneralSettingsWindow`, ja que és configuració d'aplicació, no d'un mod/perfil concret. `SettingsTab.xaml`/`.xaml.cs` eliminats.
- `PdxModIDE.UI.csproj`: afegit `<Content Include="Languages\**">` (igual que `Themes\**`) per a copiar els diccionaris d'idioma al directori de sortida/publicació.

### Notes

- Fase 1 d'i18n: de moment només es tradueixen els textos de `GeneralSettingsWindow` (prova de concepte del mecanisme de canvi d'idioma en calent). La resta de la interfície (Perfil, Mapa, Dates, Mòduls, Validació, Logs) roman en espanyol hardcoded; la seva traducció s'abordarà en una fase posterior, reutilitzant el mateix mecanisme de `ResourceDictionary`.

---

## [1.1.10]

### Changed
- **Noms complets en checkboxes de mode de títol**: Els modes "Tit.", "Cond.", "Duc.", "Rey.", "Imp." ara es mostren com "Titular", "Comtat", "Ducat", "Regne", "Imperi" respectivament.
- **Visibilitat condicional de modes de títol**: Els checkboxes de mode (Titular/Comtat/Ducat/Regne/Imperi) només es mostren quan almenys un dels checks "Base" o "Mod" està actiu. Si es desactiven tots dos, els modes de títol s'oculten.
- **Selecció per defecte**: En activar "Base" o "Mod" sense cap mode de títol actiu, se selecciona automàticament "Titular".

### Fixed
- **Sempre un mode actiu**: Ara no es pot desmarcar l'últim mode de títol mentre "Base" o "Mod" estigui actiu. Si l'usuari intenta desmarcar-lo, es re-marca "Titular" automàticament.
- **Mode no aplicat després de càrrega de mapa**: Si l'usuari activava "Base" o "Mod" abans que el mapa acabés de carregar-se (càrrega asíncrona), `SourceModeChanged` retornava d'hora per `_mapLoaded == false` i mai s'aplicava el mode de títol. En finalitzar `DoLoad` ara es crida a `ReapplyActiveMode()` si hi ha una font activa.
- **Dades del mod sobreescrites per còpies base en mod**: Quan el mod contenia còpies de fitxers base de `history/titles` més un fitxer personalitzat, `TitleHistoryLoader.LoadAll` ignorava els títols duplicats (`if (!AllTitles.ContainsKey)`) i el primer en ordre alfabètic guanyava — normalment la còpia base, no la dada personalitzada. Afegit paràmetre `overwriteDuplicates` perquè el mod sempre tingui prioritat.
- **Estructura de landed_titles no s'actualitzava en canviar font**: `MapLoader` només carregava l'estructura de landed_titles del joc base. En activar "Mod", l'estructura de baronies/comtats/ducats etc. del mod no s'aplicava. Afegit `SaveBaseSnapshot()`, `LoadModLandedTitles(modRoot)` i `ResetToBase()` per a canviar l'estructura segons la font activa (Base → base, Mod → mod, Ambdós → mod).

---

## [1.1.9]

### Fixed
- **Parser de `common/landed_titles` perdia títols amb blocs intermedis no-títol**: blocs com `cultural_names = { ... }`, `color = { ... }` o `definite_form = { ... }` dins d'un títol feien que el seu `}` solitari fes pop prematur del títol pare del stack. Això impedia que les baronies següents es vinculessin al seu comtat (`BaronyToCounty` quedava buit), per la qual cosa `BuildCountyLut`/`BuildHolderLut` mai trobaven el comtat d'aquestes províncies. Afegit comptador `nonTitleDepth` que rastreja claus de blocs no-títol per a ignorar els seus tancaments sense afectar el stack de títols.

---

## [1.1.8]

### Fixed
- **Parser de `history/titles` ignorava blocs de data "en una sola línia"**: format molt habitual en baronies i bastants comtats de CK3, p. ex. `900.1.1={ holder=140000 liege=k_england }`. El comptador de claus tallava el processament de la línia (`continue`) en veure un `}`, sense comprovar si aquest tancament corresponia al bloc de data (niuat) o al títol complet, així que aquestes línies mai arribaven a llegir-se — afectava igual a Base i a Mod. Reescrit el parser per a calcular el balanç net de claus de la línia i extreure sempre `holder=`/`liege=` abans de decidir si el títol es tanca.
- De pas, s'ignoren ara els comentaris en línia (`# ...`) per a evitar falsos positius en buscar `holder=`/`liege=`.

---

## [1.1.7]

### Fixed
- **Cerca recursiva a `history/titles` i `common/landed_titles`**: `TitleHistoryLoader.LoadAll` i `MapLoader.LoadLandedTitles` només escanejaven el nivell superior de la carpeta. El motor de Paradox processa recursivament qualsevol subcarpeta dins d'aquestes rutes (amb qualsevol nom, no només carpetes literals "mod"), així que un mod que organitza els seus fitxers d'història/títols en subcarpetes pròpies no s'estava llegint. Ara ambdós usen `SearchOption.AllDirectories`, de forma genèrica tant per a Base com per a Mod.

---

## [1.1.6]

### Added
- **Lògica funcional dels checks "Base"/"Mod"**: Ara determinen d'on surt la informació de titulars mostrada al mapa (pestanya Mapa):
  - **Només Base**: usa `history/titles` del joc base, amb l'any tal qual està al `TextBox` de data.
  - **Només Mod**: usa `history/titles` del mod, aplicant l'offset del perfil (any + `YearOffset`) perquè la data cercada coincideixi amb les dates ja desplaçades als fitxers del mod.
  - **Ambdós actius**: prioritat a la dada del Mod (amb offset); si no hi ha holder per a aquella data al mod, s'usa la del joc base (sense offset).
  - **Cap actiu**: es mostra el mapa general de terra/mar per defecte, igual que abans d'aquesta funció, independentment de si Titular/Comtat/Ducat/Regne/Imperi està marcat.
  - **Colors de "sense dades" en mode LUT**: quan un mode de títol està actiu però una província no té dada (titular/comtat/etc.), ara es pinta terra en gris i mar en blau (abans tot sortia en un gris pla uniforme, sense distingir mar). Canvi al shader de `MapRenderer`.
- **`MapLoader.BuildCombinedHolderLut`**: nou mètode que combina el holder de Base i de Mod per província amb la prioritat Mod > Base descrita anteriorment.
- **Panell d'informació de província**: en fer clic a una província, el "Holder"/"Liege" mostrats ara respecten els checks Base/Mod actius (amb offset per a Mod) i indiquen entre claudàtors de quina font procedeixen (`[Mod]` / `[Base]`).

---

## [1.1.5]

### Added
- **Checks "Base" i "Mod" a pestanya Mapa**: Nous checkboxes `BaseSourceCheck` i `ModSourceCheck`, no excloents entre si, situats entre la data (amb la seva "Data Mod" calculada) i els checks de Titular/Comtat/Ducat/Regne/Imperi. De moment només refresquen el mapa en canviar (`SourceModeChanged`); la lògica de quines dades mostrar segons Base/Mod s'implementa a la versió 1.1.6.

---

## [1.1.4]

### Added
- **Data Mod calculada a pestanya Mapa**: Nova etiqueta `OffsetLabel` al costat de l'any (abans dels checks de titular/comtat/etc.) que mostra la data resultant al mod (`any + YearOffset` del perfil actiu), mostrant tots dos valors (any base i data mod) al mateix temps. Només informativa, no editable; s'actualitza en carregar la pestanya, en canviar de perfil, en modificar l'offset i en canviar l'any.

---

## [1.1.3]

### Changed
- **Unificació de pestanyes Mapa**: Les dues pestanyes "Història (Base)" i "Història (Mod)" s'han fusionat en una única pestanya anomenada "Mapa" (`local:HistoryTab` sense `Mode` fix a `MainWindow.xaml`).

---

## [1.1.2]

### Changed
- **Text informatiu pestanya Història**: Eliminat el prefix "Vista: Mod/Joc Base" del text mostrat després de carregar el mapa; ara només es mostra el recompte de províncies i títols (`X prov, Y títols`).

---

## [1.1.1]

### Added
- **Modes Ducats / Regnes / Imperis** a pestanya Història: Checkboxes "Duc.", "Rey.", "Imp." per a acolorir mapa per límits de ducat (`d_xxx`), regne (`k_xxx`) i imperi (`e_xxx`).
- **Jerarquia completa de títols**: `MapLoader.LoadLandedTitles()` ara construeix `CountyToDuchy`, `DuchyToKingdom`, `KingdomToEmpire` des de la pila de títols niats.
- **Nous LUTs**: `BuildDuchyLut()`, `BuildKingdomLut()`, `BuildEmpireLut()` amb paletes i wrap-around de colors.
- **Mútua exclusió estesa**: Els 5 modes (Titular, Comtats, Ducats, Regnes, Imperis) es desmarquen entre si.
- **Labels compactes**: Checkboxes usen abreviatures (Tit., Cond., Duc., Rey., Imp.) amb tooltips per a estalviar espai a la barra.

### Changed
- **Etiquetes al panell info**: Panell "Títol" ara mostra Baronia, Comtat, Ducat, Regne, Imperi, Holder, Liege segons mode actiu.

---

## [1.1.0]

### Added
- **Mode Comtats a pestanya Història**: Nou checkbox "Comtats" junt amb "Titular" que acoloreix el mapa per límits de comtat (`c_xxx`) en lloc de per holder (personatge). Usa `MapLoader.BuildCountyLut()` → mapeja província → baronia → comtat.
- **Cicle de colors per a >255 ítems**: A `BuildHolderLut` i `BuildCountyLut`, els índexs >255 ara fan wrap-around (mòdul 255) en lloc de clavar-se a 255, evitant que centenars de comtats/holders comparteixin el mateix color verd.
- **Mútua exclusió**: Checkboxes "Titular" i "Comtats" es desmarquen mútuament.

### Fixed
- **Comtats verds**: En haver >255 comtats a CK3, tots a partir del 256 usaven índex 255 (mateix color). Ara ciclen 1-255.
- **Holders verds**: Mateix fix aplicat a `BuildHolderLut` per a >255 holders únics.

---

## [1.0.0]

### Added
- **Arquitectura modular multi-projecte**: 9 projectes .NET 8 (Core, Domain, Data, IO, MapEngine, Project, Rendering, UI, Validation).
- **Sistema de perfils**: Perfils per mod amb GameRoot, ModRoot, BackupRoot, YearOffset, mòduls i fitxers seleccionats.
- **Processador de mòduls paral·lel**: `ModuleProcessor.ProcessModulesAsync` copia fitxers joc→mod aplicant offset de dates (regex per joc) amb `Parallel.ForEach` i logging per mòdul.
- **Plugin system per a jocs**: `IGamePlugin` + `GameRegistry` amb detecció automàtica (`DetectGame`) i fallback a diàleg de selecció. Implementat `CK3GamePlugin`.
- **Processament de defines**: `DefinesProcessor` llegeix/escriu `end_date` a `defines.txt` (game + mod) amb backup automàtic.
- **Map Engine complet**:
  - `MapLoader`: carrega `definition.csv`, `default.map`, `landed_titles/*.txt`, `provinces.png/bmp`.
  - LUT cache (16M entrades) persistit a `%LocalAppData%/PdxModIDE/lut_cache` amb hash MD5 de fonts.
  - `TitleHistoryLoader`: parseja `history/titles/*.txt` → `TitleHistory { Holders: SortedList<int, string> }`.
  - `BuildHolderLut`: genera LUT de titulars per any per a renderitzat.
  - **Mode Comtats**: `BuildCountyLut` acoloreix mapa per límits de comtat (`c_xxx`) des de `landed_titles`.
- **Renderitzat de mapa**: `MapRenderer` (SkiaSharp) amb viewport, zoom/pan, color picker, tooltips província/titular.
- **Validació de mòduls**: `ModuleValidator` compara recursivament game/mod/backup; diff línia a línia; resum per estat (Igual/Modificat/Afegit/Eliminat).
- **Persistència JSON**: `DataLoader` genèric per a profiles, modules, files, settings, logfilters a `data/*.json`.
- **UI WPF (MVVM lleuger)**:
  - `MainWindow` + `MainViewModel`: tabs Perfil, Mòduls, Fitxers, Dates, Validació, Historial, Logs, Ajustos.
  - Temes dinàmics: Light, Dark, CK3, Sepia, Contrast, VSCode Dark/Light (ResourceDictionary swap).
  - Gestió de perfils (CRUD, renombrar, detecció joc), selecció mòduls/fitxers amb checkboxes.
  - Processat asíncron amb progrés, validació paral·lela, diff viewer en tabs.
- **Gestió d'errors global**: `App.OnStartup` registra `UnhandledException` + `DispatcherUnhandledException` → `logs/crash.log` + MessageBox.

### Changed
- **Target Framework**: .NET 8.0, `Nullable=enable`, `ImplicitUsings=enable`.
- **Estructura de dades**: `Domain` entitats pures; `Data` configs JSON; mapatge bidireccional a `ProjectManager.SyncDomainProfiles`.
- **Injecció de dependències manual**: `ProjectManager` instància `ModuleProcessor(ModuleRepository())`; repositoris usen `DataLoader` estàtic.

### Deprecated
- (Cap - versió inicial)

### Removed
- (Cap - versió inicial)

### Fixed
- (Cap - versió inicial)

### Security
- No s'emmagatzemen secrets; paths de joc/mod/backup configurats per l'usuari al perfil.

---

## [Unreleased]

### Changed

- **Algorisme de diff millorat a `ModuleValidator.CompareFileContents`**: s'ha reemplaçat l'heurística bàsica de mirar 3 línies endavant per una cerca bidireccional (fins a 20 línies) que produeix addicions i eliminacions correctament entrellaçades en lloc de treure primer totes les addicions i després totes les eliminacions.
- **Visor diff costat a costat (`DiffViewDialog`)**: s'ha substituït el visor de text en format unificat per una vista costat a costat similar al plugin Compare del Notepad++. Mostra panells "Original" i "Modified" amb números de línia a ambdós costats, fons de color (verd per a addicions, vermell per a eliminacions) i files de modificació aparellades quan una eliminació va seguida immediatament d'una addició.

### Planned
- **Suport EU4 / Imperator / HOI4 / Victoria 3**: nous `IGamePlugin` amb regex dates, defines paths, extensions processables.
- **Migració a DI container** (Microsoft.Extensions.DependencyInjection) per a `ProjectManager`, repositoris, processadors.
- **ViewModels base amb `INotifyPropertyChanged`** centralitzat (actualment implementació manual a `MainViewModel`).
- **Tests unitaris**: xUnit + Moq per a `ModuleProcessor.ApplyOffset`, `DefinesProcessor`, `MapLoader.LoadDefinition`, `ModuleValidator.CompareFileContents`.
- **Paginació / virtualització** en llistes de mòduls/fitxers (actualment `ObservableCollection` completa).
- **Perfil de rendiment**: benchmark `ProcessModulesAsync` amb `BenchmarkDotNet`; optimitzar I/O paral·lel (actualment `Parallel.ForEach` síncron sobre I/O).
- **LUT cache incremental**: invalidar només províncies canviades en lloc de rebuild complet.
- **Notificacions toast** a UI (actualment MessageBox per a errors).
- **Settings persistents per usuari** (theme, últim perfil, paths recents) → ja a `Settings.json` però estendre.
- **Validació incremental**: watcher `FileSystemWatcher` a ModRoot per a actualitzar estat validació en temps real.
- **Exportació de diff**: HTML/Markdown report de validació.
- **Internacionalització (i18n) - traducció completa de la UI**: la infraestructura base (`ResourceDictionary` XAML EN/ES) ja existeix des de 1.2.0, però només cobreix `GeneralSettingsWindow`. Falta extreure i traduir els strings hardcoded en espanyol de la resta de tabs (`ProfileTab`, `HistoryTab`, `DatesTab`, `ModulesTab`, `ValidationTab`, `LogsTab`) i de `MainViewModel`.

---

## Template for Future Entries

## [X.Y.Z]

### Added
- Descripcions de funcionalitats

### Changed
- Canvis a funcionalitats existents

### Deprecated
- Funcionalitats properes a ser eliminades

### Removed
- Funcionalitats eliminades

### Fixed
- Correccions d'errors

### Security
- Pedaços de vulnerabilitat
