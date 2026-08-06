# Changelog - PdxModIDE

Tots els canvis notables d'aquest projecte es documentaran en aquest fitxer.

El format es basa en [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
i aquest projecte s'adhereix a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.6.1]

### Afegit

- **Decodificació completa BC7/DX10 al grid d'edificis (pestanya Cultures)**: `DdsDecoder` ara decodifica les textures DX10 (DXGI) dels edificis de CK3, incloent-hi els 8 modes de BC7 (formats 98/99), a més de BC4 (74) i BC5 (76). BC6H (95/96) es detecta i llança `NotSupportedException`. Abans aquestes textures es decodificaven com a gris pla. Això fa que les textures `_unique.dds` dels edificis (tgp/asiàtics) mostrin el seu color real.

### Canviat

- **Grid d'edificis texturitzat per malla (pestanya Cultures)**: el grid ara renderitza cada submalla amb el seu difús + UV + la `_unique` opcional resolta des del `.asset` (`texture = { file = "..._gridunique.dds" index = 5 }`), mostrant l'única amb UV1 i provant l'atlas difús quan no hi ha segon set de UV. Les submalles de col·llisió s'ometen.

- **Corregit el bug de canals que feia veure blavosos els edificis**: `DdsDecoder` guarda els rec en ordre RGBA, però el `BitmapSource` de WPF usa `PixelFormats.Bgra32`. `LoadTexture` ara converteix RGBA→BGRA abans de crear el bitmap, de manera que les textures `_UNIC` vermelles (teulades, etc.) es renderitzen en vermell i no en blau.

- **Blend atlas + matís de color (opció 2)**: cada submalla d'edifici ara fa servir el difús de l'atlas (UV1) com a text gru base de detall i aplica el color mitjà de la seva textura `_UNIC` a través del `DiffuseMaterial`, replicant el shader `standard_atlas` del joc (`Diffuse.rgb *= Unique`), amb un factor de barreja del 70% per mantenir visible el detall.

---

## [1.6.0]

### Canviat

- **Previsualització d'escut d'armes a les cultures (pestanya Cultures)**: el bloc de Coat-of-Arms ja no renderitza cap previsualització de l'escut (ni viewport 3D ni imatge); ara mostra únicament el text GFX (`coa_gfx = ...`). Les previsualitzacions 3D de `building_gfx`, `clothing_gfx` i `unit_gfx` romanen sense canvis.

---

## [1.5.9]

### Afegit

- **Llista de noms als detalls de cultura (pestanya Cultures)**: el panell de detalls ara mostra la llista de noms de la cultura (de `name_list = name_list_xxx` a la definició de cultura), amb el seu nom localitzat i una secció desplegable amb tots els seus paràmetres agrupats per categoria: **Opcions** (flags booleanes com `dynasty_name_first`, `founder_named_dynasties`, `house_based_map_names`, `suggest_family_names`, `suggest_ancestor_names`, `always_use_patronym`), **Llistes de noms** (`male_names`, `female_names`, `dynasty_names`, `cadet_dynasty_names`, `mercenary_names`), **Probabilitats** (`pat_grf_name_chance`, `mat_grf_name_chance`, `father_name_chance`, `pat_grm_name_chance`, `mat_grm_name_chance`, `mother_name_chance`), **Prefixos i sufixos** (`patronym_prefix_*`, `patronym_suffix_*`, `dynasty_of_location_prefix`, `bastard_dynasty_prefix`), i **Altres** (`grammar_transform`). Les definicions s'analitzen des de `common/culture/name_lists/*.txt`, amb prioritat dels fitxers del mod sobre el joc. Cada paràmetre inclou una explicació localitzada (claus `NameListParam_*_Desc` a `en/es/ca.xaml`).

### Canviat

- **Ordre del panell de detalls de cultura**: el bloc Llista de noms es situa ara després de Tradicions (al final del panell de detalls).

### Afegit

- **Tradicions als detalls de cultura (pestanya Cultures)**: el panell de detalls ara també mostra les tradicions de la cultura (de `traditions = { ... }` en la definició de cultura), cadascuna amb el seu nom localitzat (`tradition_<name>_name`), la seva descripció localitzada (`tradition_<name>_desc`) i una secció desplegable amb els seus paràmetres (`category`, `layers`, `can_pick`, `can_pick_for_hybridization`, `parameters`, `character_modifier`, `province_modifier`, `county_modifier`, `culture_modifier`, `effects`, `cost`, `ai_will_do`, `desc`, etc.), replicant la funcionalitat de l'ethos/herència/idioma/tradició marcial/designació del líder. Les definicions s'analitzen des de `common/culture/traditions/*.txt`, amb prioritat dels fitxers del mod sobre el joc. Cada paràmetre inclou una explicació localitzada (claus `TraditionParam_*_Desc` a `CK3.*.xaml`).
- **`IGamePlugin.TraditionsRelativePath`**: nova propietat d'interfície per a la ruta del directori de tradicions (CK3: `common/culture/traditions`).

### Canviat

- **Disposició del panell de detalls de cultura**: el panell de detalls ara està dins d'un `ScrollViewer` vertical perquè ja no surti de la finestra quan s'expandeixen moltes seccions. El panell d'estadístiques s'ha mogut a la part superior de la columna dreta i només es mostra quan no hi ha cultura seleccionada. S'ha augmentat l'espai entre l'arbre de cultures i el panell de detalls.

---

## [1.5.7]

### Afegit

- **Tradició marcial als detalls de cultura (pestanya Cultures)**: el panell de detalls ara també mostra la tradició marcial de la cultura (de `martial_custom = martial_custom_xxx`, localitzada via les claus `martial_custom_<name>_name` a `cultural_traditions_l_*.yml`), amb una secció desplegable amb els seus paràmetres (`parameters`, `can_pick`, `ai_will_do`, etc.), replicant la funcionalitat de l'ethos/herència/idioma. Les definicions s'analitzen des de `common/culture/pillars/*martial_custom.txt`, amb prioritat dels fitxers del mod sobre el joc. Cada paràmetre inclou una explicació localitzada (claus `MartialCustomParam_*_Desc` a `CK3.*.xaml`).
- **Designació del líder als detalls de cultura (pestanya Cultures)**: el panell de detalls ara també mostra la designació del líder de la cultura (de `head_determination = head_determination_xxx`, localitzada via `head_determination_l_*.yml`), amb una secció desplegable amb els seus paràmetres (`head_determination_type`, etc.). Les definicions s'analitzen des de `common/culture/pillars/*head_determination.txt`, amb prioritat dels fitxers del mod sobre el joc. Cada paràmetre inclou una explicació localitzada (claus `HeadDeterminationParam_*_Desc` a `CK3.*.xaml`). Els dos camps nous es col·loquen després del camp de l'idioma, seguint l'ordre del fitxer de definició de cultura.

---

## [1.5.6]

### Afegit

- **Idioma als detalls de cultura (pestanya Cultures)**: el panell de detalls ara també mostra els detalls de l'idioma de la cultura (de `language = language_xxx`, localitzat via `cultural_languages_l_*.yml`), amb una secció desplegable amb els seus paràmetres (`is_shown`, `ai_will_do`, `color`, etc.), replicant la funcionalitat de l'ethos/herència. Les definicions d'idioma s'analitzen des de `common/culture/pillars/*language.txt`, amb prioritat dels fitxers del mod sobre el joc. Cada paràmetre inclou una explicació localitzada (claus `LanguageParam_*_Desc` a `CK3.*.xaml`). El camp de l'idioma es col·loca al final del panell de detalls.

---

## [1.5.5]

### Afegit

- **Herència als detalls de cultura (pestanya Cultures)**: el panell de detalls ara també mostra els detalls de l'herència de la cultura (de `heritage = heritage_xxx`), amb una secció desplegable amb els seus paràmetres (`is_shown`, `audio_parameter`, etc.), replicant la funcionalitat de l'ethos. Les definicions d'herència s'analitzen des de `common/culture/pillars/*_heritage.txt`, amb prioritat dels fitxers del mod sobre el joc. Cada paràmetre inclou una explicació localitzada (claus `HeritageParam_*_Desc` a `CK3.*.xaml`).

### Canviat

- **Ordre del panell de detalls de cultura**: els camps ara s'ordenen Origen, Nom, Color, Ethos, Herència (de dalt a baix).

---

## [1.5.4]

### Afegit

- **Ethos als detalls de cultura (pestanya Cultures)**: el panell de detalls ara mostra l'ethos de la cultura (de `ethos = ethos_xxx`, localitzat via `cultural_traditions_l_*.yml`) i una secció desplegable amb els seus paràmetres (`character_modifier`, `province_modifier`, `county_modifier`, `culture_modifier`, `parameters`, `ai_will_do`, `desc`, etc.). Les definicions d'ethos s'analitzen des de `common/culture/pillars/*_ethos.txt`, amb prioritat dels fitxers del mod sobre el joc. Cada paràmetre inclou una explicació localitzada (claus `EthosParam_*_Desc` a `CK3.*.xaml`).

---

## [1.5.3]

### Afegit

- **Visualització del color de cultura (pestanya Cultures)**: el panell de detalls ara mostra el color de la cultura com a valor RGB numèric amb un swatch visual. Suporta modes `hsv`, `hsv360` i `rgb`, i resol referències `color = <nom>` contra `common/named_colors/*.txt` (els colors referenciats mostren el seu nom d'origen).

### Canviat

- **`CultureLoader` reescrit com a parser basat en blocs**: `ParseCultureFile` ja no crea entrades espúries per a blocs `color = { ... }`; el color s'assigna a la cultura contenidora. Gestiona `hsv`/`hsv360`/`rgb` i valors RGB enters/flotants.
- **Resolució de colors amb nom al mapa**: `LoadCultures` resol referències `color = <nom>` contra `common/named_colors/*.txt` tant de l'arrel del joc com del mod, de manera que totes les cultures base i del mod obtenen color.
- **Herència de cultura amb IDs de personatge tipus string**: el contingut oriental (Xina/Japó/Corea, ex. `holder = tuyuhun0006`, `japanese_yamato_1 = { ... }`) usa IDs de personatge amb nom en lloc de numèrics. L'historial de titulars i les cultures de personatges ara accepten tant IDs numèrics com string, de manera que les províncies orientals sense `culture =` directe hereten correctament la cultura del titular del comtat al mapa.

### Corregit

- **Arxius de cultura amb línies malformades (sense `=`) aturaven l'anàlisi**: línies com `khitan { ... }` interrompien el parser línia a línia, perdent totes les cultures posteriors. El parser per blocs ja no s'atura.

---

## [1.5.2]

### Afegit

- **Pestanya Cultures**: nova pestanya a la dreta de Mapa que mostra cultures agrupades per herència en un arbre, prioritzant el mod sobre el joc base.
- **Anàlisi d'arxius de cultura**: parser Clausewitz que llegeix `common/culture/cultures/*.txt` recursivament, suportant blocs niats, comentaris i tipus de valor complexos (hsv, cadenes).
- **Localització de cultures**: noms mostrats carregats d'arxius de localització de CK3 (`cultures_l_*.yml` i `cultural_heritages_l_*.yml`) per a anglès i espanyol; català usa anglès com a fallback.
- **Panell de detalls**: en seleccionar una cultura es mostra el seu nom localitzat, herència i origen (Base/Mod).
- **Panell d'estadístiques**: mostra grups d'herència totals, grups amb canvis del mod, cultures del mod i del joc base.
- **IGamePlugin.CulturesRelativePath**: nova propietat d'interfície per a la ruta del directori de cultures (CK3: `common/culture/cultures`).

### Canviat

- **Versió actualitzada a 1.5.2**: recurs `MainWindow_Title` actualitzat a tots els fitxers d'idioma, versió catalana unificada a 1.5.2.

### Corregit

- **Robustesa del parser de cultura**: `ExtractAttribute` ara salta correctament valors amb blocs posteriors (ex. `color = hsv { 0.72 0.6 0.76 }`) en lloc de trencar l'anàlisi.

---

## [1.5.1]

### Afegit

- **Vista de mapa Cultural (implementació completa)**: la nova vista Cultural renderitza cada província amb el nom i color de la seva cultura, incloent prioritat de font Base/Mod/Ambos i cerca històrica per any.
- **Càrrega de dades de cultura**: nou `CultureLoader` que analitza definicions de cultura (`common/culture/cultures/*.txt`), historial de províncies (`history/provinces/*.txt`), historial de titulars de títols (`history/titles/*.txt`) i cultures de personatges (`history/characters/*.txt`).
- **Herència de cultura**: les províncies sense `culture =` explícit al seu historial hereten la cultura del titular del comtat (titular resolt des de l'historial de títols, cultura des de l'historial de personatges).
- **Localització de cultures**: noms de cultura mostrats carregats des d'arxius de localització de CK3 (`cultures_l_*.yml`) per a tots els idiomes suportats.
- **Format d'herència**: les cultures heretades es mostren com `"Anglosaxona (Chelmsford)"` amb el nom de la província capital entre parèntesis.
- **Visibilitat de ShowNamesCheck**: `ShowNamesCheck` mogut fora de `TitleModePanel` a XAML perquè romangui visible a la vista Cultural.

### Canviat

- **Versió actualitzada a 1.5.1**: recurs `MainWindow_Title` actualitzat a tots els fitxers d'idioma.

### Corregit

- **Parseig de culture en blocs de data niats**: el parser ara usa profunditat + pila de dates per capturar correctament `culture =` en línies separades dins de blocs de data en arxius d'historial de províncies.
- **Parseig de definicions de cultura a qualsevol profunditat**: detecta blocs de cultura niats dins de grups culturals usant `BlockRe` i un enfocament de pila única.
- **Parseig de colors flotants**: s'ha afegit `TryParseFloatColor()` per gestionar valors RGB amb decimals en definicions de color de cultura.
- **Prevenció de crash a `Math.Clamp`**: `maxFontSize` es calcula com `Math.Max(8f, boxW * 0.3f)` en lloc de `Math.Clamp` per evitar errors quan `boxW < 27`.
- **Visibilitat d'etiquetes al mapa**: s'ha reduït el filtre de bounding-box d'etiquetes de `30x20` a `20x12` per mostrar més etiquetes de cultura al mapa.

---

## [1.5.0]

### Afegit

- **Selector de vista del mapa**: nou desplegable (ComboBox) a la pestanya Mapa per canviar entre tres vistes: General (només terreny, sense overlay Base/Mod), Titular (comportament actual del mapa de títols amb modes holder/comtat/ducat/regne/imperi i botó d'edició), i Cultural (placeholder per a implementació futura).
- **Visibilitat de la interfície segons la vista**: en vista General, els checks Base/Mod, el panell de modes de títol i el botó d'edició estan ocults. En vista Titular, tots els controls són visibles. En vista Cultural, Base/Mod són visibles però els modes de títol i el botó d'edició estan ocults.
- **Font mínima obligatòria**: en vistes que no siguin General, almenys un de Base/Mod ha de romandre marcat — desmarcar l'última font activa no té efecte.

### Canviat

- **Versió actualitzada a 1.5.0**: recurs `MainWindow_Title` actualitzat a tots els fitxers d'idioma.

---

## [1.4.18]

### Corregit

- **Colors de comtat correctes per a tots els comtats (no només els primers 255)**: el LUT per a tots els modes de superposició del mapa (Titular, Comtat, Ducat, Regne, Imperi) es va actualitzar de `byte[]` (256 entrades màx., wrap-around a 255) a `ushort[]` (65535 entrades màx., sense wrap-around). Les paletes ara tenen mida dinàmica en lloc de fixa de 256 entrades. Això corregeix un error on els comtats amb índex >255 sobreescrivien les entrades de comtats anteriors a `indexToCounty`, causant que els comtats sobreescrits mostressin el color d'un altre comtat.

### Canviat

- **Tots els mètodes Build*Lut ara retornen `ushort[]`**: `BuildHolderLut`, `BuildCombinedHolderLut`, `BuildCountyLut`, `BuildDuchyLut`, `BuildKingdomLut`, `BuildEmpireLut` utilitzen valors LUT de 16 bits, eliminant la lògica de wrap-around `(idx-1)%255+1`.
- **Constructors de paleta amb mida dinàmica**: `BuildHolderPalette` i `BuildCountyPalette` creen bitmaps amb la mida de l'índex màxim real en lloc de fix 256×1.

---

## [1.4.17]

### Afegit

- **El mode Comtat del mapa utilitza els colors reals de landed_titles**: el mode de superposició de Comtats ara llegeix l'atribut `color = { r g b }` dels fitxers `common/landed_titles/*.txt` i mostra aquests colors al mapa en lloc dels colors procedimentals basats en índex (angle auri HSL).

### Canviat

- **Prioritat de càrrega de colors per a títols**: els colors es carreguen amb la següent prioritat: `<modRoot>/common/landed_titles/mod/` (màxima), després `<modRoot>/common/landed_titles/` (arrel del mod), després `<gameRoot>/common/landed_titles/` (joc base). Les línies comentades (`#color = { ... }`) s'ignoren. Els comtats sense un `color = { ... }` definit en cap font utilitzen el color procedimental HueSatLum com a fallback.

---

## [1.4.16]

### Afegit

- **Claus de títol i comtat per defecte a la finestra de divisió**: el camp "Títol superior" ara es reomple per defecte amb el nom de la primera baronia reemplaçant `b_` per `d_`, i el camp "Comtat" amb `b_` reemplaçat per `c_`.

### Canviat

- **Comentaris `##MOD_DEL` simplificats**: el prefix ja no inclou la clau del nou títol. Cada línia comentada ara comença amb `##MOD_DEL ` seguit només del contingut original de la línia.
- **Nou fitxer de títol inclou referències al pare**: el nou fitxer de títol ara mostra el títol superior original com a comentari (`#`) al costat de la capçalera del nou títol, i la clau del comtat original com a comentari al costat de la capçalera del nou comtat.

### Corregit

- **Dividir comtat ja no sobreescriu fitxers existents**: quan el fitxer de títol destí (`d_xxx.txt` etc.) ja existeix al directori del mod, el nou comtat ara s'afegeix dins del fitxer existent en lloc de sobreescriure'l.
- **Detecció de comtat duplicat en divisió**: l'aplicació ara verifica si la clau del nou comtat (`c_xxx`) ja existeix en algun fitxer `.txt` sota `common/landed_titles/` del mod, ignorant línies comentades amb `##MOD_DEL`. Si el bloc existent està actiu, l'operació s'avorta. Si el bloc està mort (tot `##MOD_DEL`), es permet i es marca amb `##MOD_DEL`.
- **Detecció de títol duplicat en divisió**: l'aplicació ara verifica si el fitxer de títol (`d_xxx.txt` etc.) ja existeix a `common/landed_titles/`. Si conté contingut actiu, s'avorta. Si està buit (tot `##MOD_DEL`), es permet i es marca.
- **Neteja de comtat original buit**: després d'una divisió, si el comtat original es queda sense baronies actives, tot el seu bloc es marca amb `##MOD_DEL`.
- **Permetre divisió quan clau de comtat/títol coincideix amb l'origen**: `KeyExists` s'omet quan la clau del nou comtat coincideix amb l'original (`newCountyKey == _countyKey`) i `WouldBlockRemainActive` confirma que l'original quedaria buit. Igual per al títol quan coincideix amb el pare (`newTitleKey == _parentTitle`), afegint el nou comtat al bloc del títol pare al fitxer font en lloc de crear un fitxer override a `mod/`.
- **`WouldBlockRemainActive` sense restricció de ruta**: la comprovació ara s'executa independentment de en quin fitxer `FindBlockInLandedTitles` va trobar el bloc.
- **Línies `##MOD_DEL` filtrades en nous fitxers de comtat**: les línies que comencen amb `##MOD_DEL` dels atributs del comtat original ja no es copien al nou fitxer de comtat.
- **Comtat original marcat amb `##MOD_DEL` en divisió CopiedFromGame amb el mateix nom**: en dividir un comtat d'origen del joc amb la mateixa clau, el bloc del comtat original ara es marca correctament com a mort a la còpia del mod.

---

## [1.4.15]

### Corregit

- **CS8625 — null passat a paràmetre no-nullable a `BuildCountyLut`**: es va canviar el paràmetre `TitleHistoryLoader history` a nullable `TitleHistoryLoader?` per permetre el null intencionat.
- **CS0414 — camp `_lastHolderYear` sense usar a `MapRenderer`**: es va eliminar el camp que s'assignava però mai es llegia.
- **CS8602 — possible desreferència null de `BaseSourceCheck`/`ModSourceCheck`**: es va afegir operador null-forgiving (`!`) en referències a controls WPF garantits per XAML.
- **CS8604 — possible argument null a `HashSet<string>.Contains`**: es va afegir guarda explícita `prov.Type == null` abans de cridar a `Contains`.

### Canviat

- **Compilació lliure de warnings**: la solució ara compila amb 0 warnings (abans 4).
- **Selector de carpeta destí a la finestra de divisió de comtat**: s'ha afegit un camp "Carpeta destí" amb un botó d'Explorar que obre un selector de carpetes amb arrel a `{ModRoot}/common/landed_titles/mod/`. L'usuari pot triar qualsevol subdirectori per escriure el nou fitxer de títols.

---

## [1.4.14]

### Afegit

- **Indicador d'estat de mode**: una etiqueta centrada a la part superior de la finestra principal mostra el mode actual (Vistes/Edició), el nivell de jerarquia actiu (Comtat, Ducat, etc.) i la font (Base/Mod). S'amaga quan la pestanya Mapa no està activa o no hi ha font seleccionada.

### Canviat

- **Botó "Mode Vistes" / "Mode Edició" renombrat per mostrar l'acció**: el botó de commutació ara mostra "Anar a Mode Edició" / "Anar a Mode Vistes" en lloc del nom del mode actual. Ampliat a 140px. El Tooltip ara mostra el nom del mode actual.
- **Divisió de comtat preserva dades completes de baronies i comtat**: els blocs de baronies ara s'analitzen amb seguiment de profunditat de claus. El nou fitxer de títol inclou els blocs complets de baronies originals (atributs com `color`, `cultural_names`, etc.) i els atributs del comtat. Els atributs del comtat original (excepte `capital`) es traslladen al nou comtat.
- **Comentaris `##MOD_DEL` nets**: sense indentació preservada abans dels marcadors `##MOD_DEL`. Les línies buides o de només espais dins de blocs comentats es mantenen sense el prefix.

### Corregit

- **El mapa s'actualitza immediatament després de dividir**: es crida a `MapLoader.LoadModLandedTitles` després d'una divisió exitosa perquè els diccionaris de jerarquia reflecteixin els canvis. No cal reiniciar l'aplicació.
- **La integració de la jerarquia del mapa s'actualitza en temps real**: en canviar de pestanya i tornar a la pestanya Mapa es restaura l'etiqueta d'estat del mode.

---

## [1.4.13]

### Added

- **Finestra de divisió de comtat mostra les províncies seleccionades amb jerarquia**: en fer clic al botó "Dividir comtat" s'obre una nova finestra (`SplitCountyWindow`) que llista cada província seleccionada amb el seu ID, Barony, County i títol superior immediat (ducat). Les dades s'obtenen directament de la jerarquia carregada de `MapLoader` (CountyToDuchy).
- **El títol de la finestra principal ara usa localització**: el títol "Paradox Mod IDE v.1.4.13" es carrega des dels diccionaris d'idioma mitjançant `{DynamicResource MainWindow_Title}`.

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

## [1.4.7]

### Added

- **Botó "Cercar mòduls amb data no configurats" a la pestanya Validació**: nou botó que escaneja recursivament l'arrel del joc cercant carpetes no configurades com a mòduls que continguin fitxers amb patrons de data. Els resultats es mostren en un diàleg informatiu (no es modifica cap configuració). Usa `Parallel.ForEach` i lectura línia per línia amb sortida primerenca per a rendiment òptim, ometent fitxers de més d'1 MB.

### Changed

- **La validació de mòduls ja no recorre subdirectoris**: tant la validació "Tots els mòduls" com la d'un sol mòdul a la pestanya Validació ara només llisten els fitxers directament a la ruta del mòdul sense descendir a subdirectoris (`SearchOption.TopDirectoryOnly`). Això fa que la validació sigui consistent amb el processament no recursiu introduït a 1.4.6 per a la pestanya Dates.

### Fixed

- **Guions baixos (`_`) ocultats en noms de mòduls a la pestanya Dates**: WPF `CheckBox.Content` interpreta els guions baixos com a acceleradors de teclat, ocultant-los. Noms com `common/landed_titles` apareixien com `common/landedtitles`. Corregit usant un `TextBlock` dins del `CheckBox` en lloc d'usar `Content` directament.
- **Llista de mòduls a la pestanya Dates limitada a 6 columnes**: el càlcul dinàmic de columnes a `RecalculateLayout()` no tenia límit superior, causant solapament de text amb 7 columnes. Limitat a 6 columnes.
- **Mòduls acabats d'afegir no es processaven fins a reiniciar l'app**: `ModuleProcessor._moduleCache` mai s'invalidava després d'afegir, modificar o eliminar mòduls, de manera que els nous mòduls eren invisibles per al processament. Afegit `_moduleProcessor.InvalidateCache()` després de cada operació CRUD.

---

## [1.4.11]

### Added

- **Selecció múltiple de províncies en mode edició (mapa Història)**: en mode Edició, fer clic en províncies de tipus terra les afegeix o elimina d'una selecció múltiple. El panell d'informació mostra valors combinats quan totes les províncies seleccionades coincideixen, o "(Multiple)" quan difereixen. Fer clic en una província no terrestre neteja la selecció i selecciona només essa. Fer clic en espai buit deselecciona tot.

### Changed

- **El mode edició conserva la superposició de títols i noms**: la capa de títols (titular/comtat/ducat/regne/imperi) i les etiquetes "Mostrar noms" romanen actives al mapa en entrar en mode Edició, utilitzant l'últim mode seleccionat. El check "Mostrar noms" sempre és visible; els checks de mode de títol s'oculten en mode Edició.
- **El botó d'alternança de mode respecta l'idioma seleccionat**: el text i el tooltip del botó "Mode Vista" / "Mode Edició" utilitzen recursos `DynamicResource` (claus `HistoryTab_ModeView/Edit` i tooltip) disponibles en EN, ES i CA.

### Fixed

- **El ressaltat múltiple de províncies preserva les vores**: el pas CPU de ressaltat ara salta els píxels de vora, de manera que les vores entre províncies seleccionades continuen sent visibles.
- **Província no terrestre ja no roman ressaltada en fer clic a terra**: quan es selecciona una província no terrestre i després es fa clic en una de terra, la no terrestre s'elimina del conjunt de selecció.

---

## [1.4.12]

### Added

- **Botó "Dividir comtat" al mode edició del mapa**: en mode Edició amb la vista de Comtats, apareix un botó "Dividir comtat" a la part superior quan una o més províncies de terra del mateix comtat estan seleccionades. El botó usa text localitzat (EN/ES/CA).

---

## [1.4.10]

## [1.4.9]

### Added

- **El panell d'informació ara mostra la jerarquia completa de títols i noms de titulars amb dinastia**: la pestanya Història reemplaça les files "Holder/Liege" amb files de nivell Ducat, Regne i Imperi. Cada nivell mostra el nom del personatge resolt des de `history/characters/*.txt` amb el cognom de dinastia des de `common/dynasties/*.txt`, amb fallback a l'ID si no es troba.
- **Carregadors de personatges i dinasties**: nous `CharacterHistoryLoader.cs` i `DynastyLoader.cs` que analitzen noms de personatges i noms de dinastia (incloent fitxers `.yml` localitzats) des del joc base i el directori del mod.
- **Les etiquetes de titulars al mapa ara mostren noms de personatge**: les etiquetes en mode titular renderitzen el nom del personatge (amb dinastia) en lloc del nom del títol localitzat.

### Changed

- **Títol de finestra actualitzat a "Paradox Mod IDE v.1.4.9"**: `MainWindow.xaml` reflecteix la nova versió.
- **El panell d'informació s'actualitza en canviar el mode de superposició**: tots els mètodes `Apply*Mode` ara criden `UpdateProvinceInfo(_lastProvinceId)` perquè el panell s'actualitzi immediatament en canviar de mode.

---

## [1.4.8]

### Changed

- **Les etiquetes del mapa ara escalen amb la mida de la província**: els noms de província al mapa d'Història ara es renderitzen amb una mida de font proporcional al bounding box de la província (`boxW * 0.14`, clamp 8px–30% de l'amplada). El text es redueix automàticament si supera el 85% de l'amplada de la província.
- **Estil i colors de les etiquetes del mapa millorats**: l'ompliment del text va canviar de blanc sòlid sobre rectangle negre a gris fosc (#666) dibuixat 3 vegades per donar gruix, amb un vora blanca semitransparent (`SKColor(255,255,255,200)`) per a un aspecte net estil CK3, eliminant el rectangle negre opac de fons.

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
