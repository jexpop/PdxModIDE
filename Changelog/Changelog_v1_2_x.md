# Changelog - PdxModIDE

All notable changes for the 1.2.x series of this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.2.2]
### Added
- **New language: Català (ca)**: Catalan added as the third available language. New file `Languages/ca.xaml` with full UI translation, `Languages/CK3.ca.xaml` as placeholder, radio selector in `GeneralSettingsWindow`, and support in `ApplyLanguage` / `GetSelectedLanguage`.
- **Complete UI internationalization (phase 2)**: ~140 new i18n keys extracted to `es.xaml` / `en.xaml` for all tabs and dialogs:
- **Complete UI internationalization (phase 2)**: ~140 new i18n keys extracted to `es.xaml` / `en.xaml` for all tabs and dialogs:
  - MainWindow (tooltips and tab headers)
  - ProfileTab (paths, CRUD buttons, modules group)
  - ModulesTab (editing, add/save/delete buttons)
  - DatesTab (offset, end_date, modules to process)
  - HistoryTab (province/title panel, zoom, modes, tooltips)
  - ValidationTab (modules, files, comparison, results)
  - LogsTab (viewer, filters, configuration)
  - InputDialog (Accept/Cancel buttons)
- **Separation of general vs game-specific texts**: general application texts reside in `es.xaml` / `en.xaml`. Game-specific texts go in `{GameKey}.{lang}.xaml` (e.g. `CK3.es.xaml`, `CK3.en.xaml`), loaded dynamically based on the active profile.
- **Improved `RefreshMergedDictionaries()`**: now loads three dictionaries (theme + general language + game-specific language) and refreshes when changing profile.
- **`GetGameLanguagePath()`**: new method that generates the path `Languages/{GameKey}.{language}.xaml` for the active game-specific dictionary.
- **Helper method `Res(string key)`** in code-behind classes (MainViewModel, HistoryTab, ValidationTab, DatesTab, LogsTab, App) to resolve i18n strings from C#.
- **Placeholder files**: `Languages/CK3.es.xaml` and `Languages/CK3.en.xaml` for future CK3-specific texts.
### Changed
- **Default language**: the `Language` field in `Settings` now defaults to `"en"` (English) instead of `"es"` (Spanish). The application starts in English if no previous `settings.json` exists.
- **Validation status codes**: internal status codes in `ProjectManager` changed from Spanish to English (`"Modified"`, `"Added"`, `"Deleted"`, `"SAME"`, `"CHANGED"`) for consistency with the default language.
- **`ValidationTab`**: module comparison now uses `SelectedIndex` instead of comparing translated ComboBox strings, avoiding dependency on the active language.
- **`MainWindow.xaml`**: initial reference to the language dictionary changed from `Languages/es.xaml` to `Languages/en.xaml`.
- **Status labels in HistoryTab**: map mode texts and province info labels are displayed in English by default.
### Fixed
- **Bug in `ApplyLanguage` (MainWindow.xaml.cs)**: the language dictionary path selection switch had no case for `"es"`, so selecting Spanish always loaded the English dictionary.
### Notes
- Validation status codes have been unified to English as part of the default language change. DiffDialog, DiffChoiceDialog, DiffViewDialog and ValidationTab use these codes for coloring and filtering.
- Game-specific texts (CK3) are structurally prepared but still empty; they will be populated in future versions.

---

## [1.2.0]
### Added
- **General Settings Window** (`GeneralSettingsWindow`): new modal window accessible via a wrench icon (⚙) in the top-right corner of `MainWindow`, with application settings not tied to a specific profile/mod (Visual Theme and Language).
- **Internationalization infrastructure (i18n)**: new language mechanism based on XAML `ResourceDictionary`, following the same pattern already used for Themes (`Themes/*.xaml` → dynamic dictionary swap with `DynamicResource`). Folder `PdxModIDE.UI/Languages/` with `es.xaml` (default) and `en.xaml`.
- **`Settings.Language`**: new field in `data/settings.json` (`"language"`, default `"es"`), persisted same as `Theme`. Propagated through `IProjectService.Language`, `ProjectManager.Language` and `MainViewModel.Language`.
- **`MainWindow.ApplyLanguage(string)`**: new public method that reloads the language dictionary without losing the active theme (and vice versa), via `RefreshMergedDictionaries()`, which recombines both dictionaries (theme + language) in the resources of `Application` and the window.
- Language selector (Español/English) in `GeneralSettingsWindow`, with hot application (no restart required).
### Changed
- **"Options" tab removed from `TabControl`**: the Theme configuration (previously in `SettingsTab`, inside the project tabs) has been moved to the new modal window `GeneralSettingsWindow`, since it is application configuration, not specific to a mod/profile. `SettingsTab.xaml`/`.xaml.cs` removed.
- `PdxModIDE.UI.csproj`: added `<Content Include="Languages\**">` (same as `Themes\**`) to copy language dictionaries to the output/publish directory.
### Notes
- Phase 1 of i18n: for now only the texts in `GeneralSettingsWindow` are translated (proof of concept of the hot language switch mechanism). The rest of the interface (Profile, Map, Dates, Modules, Validation, Logs) remains hardcoded in Spanish; its translation will be addressed in a later phase, reusing the same `ResourceDictionary` mechanism.