# Changelog - PdxModIDE

All notable changes for the 1.7.x series of this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.7.0] - 2026-08-24

### Added
- New **Terrain** map view mode in History tab to visualize CK3 provinces colored by terrain type (plains, hills, mountains, forest, desert, wetlands, taiga, farmlands, floodplains, oasis, desert mountains, coastal sea, sea)
- Terrain data loading from CK3 game files:
  - `common/terrain_types/00_terrains.txt` — terrain HSV color definitions (converted to RGB)
  - `common/province_terrain/00_province_terrain.txt` — province-to-terrain mappings
- HSV to RGB color conversion with locale-invariant decimal parsing
- Localized terrain type names in English, Spanish, and Catalan for info panel display
- Trace logging to `logs/debug.log` for terrain loading diagnostics (works in Release builds)

### Changed
- Updated application title to version 1.7.0 in all language files (en, es, ca)
- View selector combo box now properly handles "Terrain" tag

### Fixed
- Fixed ComboBox SelectionChanged handler missing "Terrain" case (was defaulting to General view)
- Fixed MapRenderer.SetHolderMode not updating GPU shader holderLut texture (caused single-color terrain display)
- Fixed HSV color regex to handle 3 or 4 values (optional alpha channel)
- Fixed province terrain parser to only read `00_province_terrain.txt` (ignoring `01_province_properties.txt` which has different format)
- Removed HasActiveSource() requirement for Terrain view (terrain is base game data, not mod-dependent)
- UI now hides Base/Mod checkboxes when Terrain view is active
- Fixed duplicate localization key `HistoryTab_NoTerrainData`

### Technical
- MapLoader: Added `ProvinceToTerrain` dictionary, `TerrainColors` dictionary, `LoadTerrain()`, `BuildTerrainLut()`, `BuildTerrainPalette()`, `HsvToRgb()`
- MapRenderer: Added `_holderLutImage` field and GPU texture update in `SetHolderMode()`
- HistoryTab: Added `MapViewType.Terrain` enum, `ApplyTerrainMode()`, UI state management for terrain view
- App.xaml.cs: Added file-based trace logging for debug output in Release builds