# Changelog - PdxModIDE

All notable changes for the 1.3.x series of this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.3.4]
### Fixed
- **Holder/County/Duchy/etc overlay broken in Map tab**: provinces rendered gray in all overlay modes. Root cause: `SKShader.CreateImage` as child shader within `SKRuntimeEffect` returns 0 in `eval()` on SkiaSharp 3.116.1 (CPU raster backend). Workaround: CPU-based overlay in `RenderToBitmap` — per-pixel lookup of province color → holderIdx → palette color, preserving borders and highlight. See `docs/skia-image-shader-bug-workaround.md`.
- **Crash on map load**: `RenderToBitmap` returned a disposed `SKBitmap` due to an accidental `using var` on the returned bitmap.
### Changed
- **`RenderToBitmap`**: now renders terrain+borders via shader (mode=0) and applies overlay on CPU. Row-by-row pixel access via `GetPixels()` + `Marshal.Copy` for performance.
- **`SetHolderMode`**: no longer creates `SKImage` from the holder LUT; stores the `byte[]` for direct CPU use.
- **`BuildShaderCache`**: uses dummy `SKShader.CreateColor(SKColors.Black)` for `holderLut`/`palette` children (unused in mode=0).
- **`HistoryTab.xaml.cs`**: added `InvalidateRender()` for consistent cache invalidation; replaces manual `_cachedWidth = -1; QueueRender()` pattern.
### Removed
- **`_holderLutImage` and `_holderLutBackingBitmap`**: no longer needed since the shader is not used for overlay.
- **Diagnostic code**: removed `File.WriteAllText` and bitmap/image comparisons used during bug investigation.

---

## [1.3.3]
### Changed
- **Panel formatting in Map tab**: GroupBox headers "PROVINCE" and "TITLE" now render in bold with a larger font size to stand out from the subtitles.
- **Title panel restructured**: now follows the same format as the Province panel, with bold labels (Barony, County, Holder, Liege) and values on a separate line below. Uses `DynamicResource` for correct translation per active language.
- **Simplified Holder and Liege values**: removed the "in {year}" prefix from the displayed value; now shows only the holder name and source ([Mod]/[Base]).
- **Coherent translations**: new keys `HistoryTab_BaronyLabel`, `HistoryTab_CountyLabel`, `HistoryTab_HolderLabel`, `HistoryTab_LiegeLabel` in EN/ES/CA.

---

## [1.3.2]
### Added
- **i18n for province panel fields**: new resource keys `HistoryTab_IDLabel`, `HistoryTab_NameLabel`, `HistoryTab_ColorLabel`, `HistoryTab_TypeLabel` (label-only, no placeholder) and `MapTerrain_Land`, `MapTerrain_Sea`, `MapTerrain_Lake`, `MapTerrain_River`, `MapTerrain_Impassable`, `MapTerrain_Unknown` for terrain type translation in English, Spanish, and Catalan.
### Changed
- **Province info panel layout**: ID, Name, Color, and Type fields now display the label in bold with the value on a separate line below. Name uses `TextWrapping` for long values.
- **Language refresh order**: `ApplyLanguage` and `ApplyTheme` in `MainWindow.xaml.cs` now call `RefreshMergedDictionaries()` before setting the ViewModel property, ensuring `PropertyChanged` handlers read the already-updated resource dictionaries.
### Fixed
- **Off-by-one language refresh in Map tab**: terrain type values (`MapTerrain_*`) and province info values now update immediately when switching languages, instead of showing the previous language's translation.

---

## [1.3.1]
### Added
- **Informational placeholder panel in Map tab**: when no province is selected, the left column now shows a panel with instructions on map navigation (zoom buttons, mouse wheel, right-click drag, fit to window), province selection (click any province to view details), and layers (enable Base/Mod checkboxes and overlay modes). The panel is hidden when a province is clicked and reappears when clicking empty space.
- **New i18n keys**: `HistoryTab_Navigation`, `HistoryTab_Navigation_Text`, `HistoryTab_Selection`, `HistoryTab_Selection_Text`, `HistoryTab_Layers`, `HistoryTab_Layers_Text` in English, Spanish, and Catalan.

---

## [1.3.0]
### Added
- **Contextual info panel in Map tab**: the left province/title info panel is now hidden by default and only shown when clicking on a province. The "Title" block (Barony, County, Holder, Liege) is only visible when at least one of the "Base" or "Mod" checkboxes is active.
### Changed
- **Dynamic left panel visibility**: added `x:Name="InfoPanel"` to the left panel `StackPanel` in `HistoryTab.xaml`, with initial `Visibility="Collapsed"`. It is shown on province click (`UpdateProvinceInfo`) and hidden when clicking empty space.
- **Title conditional on Base/Mod**: the Title `GroupBox` (`TitleGroup`) is only shown if `HasActiveSource()` returns true (Base or Mod checked). It updates both on province click and when Base/Mod state changes while the panel is visible.