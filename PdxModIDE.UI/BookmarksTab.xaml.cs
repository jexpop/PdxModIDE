using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PdxModIDE.MapEngine;
using PdxModIDE.UI.ViewModels;

namespace PdxModIDE.UI
{
    public class BookmarkGroupVm
    {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string RawKey { get; set; } = "";
        public ObservableCollection<BookmarkVm> Bookmarks { get; set; } = new();
        public string CountText => $"({Bookmarks.Count})";
        public string DefaultStartDate { get; set; } = "";
    }

    public class BookmarkVm
    {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Source { get; set; } = "Base";
        public string DisplayKey => Name;
        public System.Windows.Media.Brush SourceBrush => Source == "Mod"
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212))
            : System.Windows.Media.Brushes.Black;
        public BookmarkInfo? Model { get; set; }
    }

    public class CharacterVm
    {
        public string TitleLine { get; set; } = "";
        public string MetaLine { get; set; } = "";
        public ObservableCollection<CharacterVm> SubCharacters { get; set; } = new();
        public bool HasSubCharacters => SubCharacters.Count > 0;
        public string SubLine { get; set; } = "";
    }

    public partial class BookmarksTab : System.Windows.Controls.UserControl
    {
        private MainViewModel? _viewModel;
        private static System.Net.Http.HttpClient _translationHttp = new() { Timeout = TimeSpan.FromSeconds(30) };
        private static readonly (string Folder, string Code)[] GameSupportedLanguages =
        {
            ("english", "en"), ("french", "fr"), ("german", "de"), ("japanese", "ja"),
            ("korean", "ko"), ("polish", "pl"), ("russian", "ru"), ("simp_chinese", "zh-CN"), ("spanish", "es")
        };
        private string _editorSavedGroupName = "";
        private Dictionary<string, BookmarkGroupInfo> _groupsMerged = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, BookmarkInfo> _bookmarksMerged = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, BookmarkGroupInfo> _groupsBase = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, BookmarkInfo> _bookmarksBase = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _baseGroupKeys = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _baseBookmarkKeys = new(StringComparer.OrdinalIgnoreCase);
        private BookmarkGroupInfo? _editorGroup;
        private BookmarkInfo? _editorBookmark;
        private bool _editorIsNew;
        private string _editorType = "Group"; // Group or Bookmark
        private static readonly System.Text.RegularExpressions.Regex _dateRegex = new(@"^-?\d+\.\d+\.\d+$", System.Text.RegularExpressions.RegexOptions.Compiled);
        private static readonly System.Text.RegularExpressions.Regex _idRegex = new(@"^[a-zA-Z0-9_]+$", System.Text.RegularExpressions.RegexOptions.Compiled);
        private Dictionary<string, string> _loc = new(StringComparer.OrdinalIgnoreCase);

        public BookmarksTab()
        {
            InitializeComponent();
            Loaded += BookmarksTab_Loaded;
        }

        private void BookmarksTab_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as MainViewModel;
            if (_viewModel == null)
                _viewModel = TryFindViewModel();
            if (GroupEditorTabHeaderText != null && string.IsNullOrEmpty(GroupEditorTabHeaderText.Text))
            {
                GroupEditorTabHeaderText.Text = Res("BookmarksTab_SubTabNewGroup");
                GroupEditorModeText.Text = Res("BookmarksTab_GroupEditorNewTitle");
                GroupEditorHintText.Text = Res("BookmarksTab_GroupEditorHint");
            }
            if (BookmarkEditorTabHeaderText != null && string.IsNullOrEmpty(BookmarkEditorTabHeaderText.Text))
            {
                BookmarkEditorTabHeaderText.Text = Res("BookmarksTab_SubTabBookmark");
                BookmarkEditorModeText.Text = Res("BookmarksTab_BookmarkEditorNewTitle");
                BookmarkEditorHintText.Text = Res("BookmarksTab_BookmarkEditorHint");
            }
            // Standalone "Nuevo grupo" tab is for creation by default
            _editorType = "Group";
            _editorGroup = null;
            _editorBookmark = null;
            _editorIsNew = true;
            _editorSavedGroupName = "";
            UpdateEditorModeUi();
            LoadBookmarks();
            if (_viewModel != null)
                _viewModel.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(MainViewModel.CurrentProfile))
                        Dispatcher.BeginInvoke(new Action(LoadBookmarks));
                };
        }

        private void UpdateEditorModeUi()
        {
            bool isNew = _editorIsNew;
            if (EditorGroupId != null) { EditorGroupId.IsReadOnly = !isNew; EditorGroupId.IsEnabled = true; }
            if (EditorBookmarkId != null) { EditorBookmarkId.IsReadOnly = !isNew; EditorBookmarkId.IsEnabled = true; }
        }

        private MainViewModel? TryFindViewModel()
        {
            DependencyObject? cur = this;
            while (cur != null)
            {
                if (cur is FrameworkElement fe && fe.DataContext is MainViewModel vm) return vm;
                cur = LogicalTreeHelper.GetParent(cur) ?? VisualTreeHelper.GetParent(cur);
            }
            return System.Windows.Application.Current?.MainWindow?.DataContext as MainViewModel;
        }

        private void LoadBookmarks()
        {
            if (_viewModel?.CurrentProfile == null)
            {
                StatsText.Text = Res("BookmarksTab_NoProfile");
                BookmarkTreeMod.ItemsSource = null;
                BookmarkTreeBase.ItemsSource = null;
                UpdateEmpty(true);
                return;
            }

            var gameRoot = _viewModel.CurrentProfile.GameRoot;
            var modRoot = _viewModel.CurrentProfile.ModRoot;
            var lang = _viewModel.Language ?? "en";

            if (string.IsNullOrEmpty(gameRoot))
            {
                StatsText.Text = Res("BookmarksTab_NoGameRoot");
                BookmarkTreeMod.ItemsSource = null;
                BookmarkTreeBase.ItemsSource = null;
                UpdateEmpty(true);
                return;
            }

            try
            {
                var baseGroupsPure = BookmarkLoader.LoadGroups(gameRoot, "Base");
                var baseBmPure = BookmarkLoader.LoadBookmarks(gameRoot, "Base");
                var merged = BookmarkLoader.LoadMergedBookmarks(gameRoot, modRoot, out var groupsMerged);
                _groupsBase = baseGroupsPure;
                _bookmarksBase = baseBmPure;
                _groupsMerged = groupsMerged;
                _bookmarksMerged = merged;
                _loc = BookmarkLoader.LoadBookmarkLocalization(gameRoot, modRoot, lang);

                // Resolve display names for all
                foreach (var g in _groupsMerged.Values)
                {
                    if (_loc.TryGetValue(g.Name, out var loc)) g.DisplayName = loc;
                    else g.DisplayName = g.Name;
                }
                foreach (var g in _groupsBase.Values)
                {
                    if (_loc.TryGetValue(g.Name, out var loc)) g.DisplayName = loc;
                    else g.DisplayName = g.Name;
                }
                foreach (var b in _bookmarksMerged.Values)
                {
                    if (_loc.TryGetValue(b.Name, out var loc)) b.DisplayName = loc;
                    else b.DisplayName = b.Name;
                    if (!string.IsNullOrEmpty(b.Group) && _groupsMerged.TryGetValue(b.Group, out var gg))
                        b.GroupDisplayName = gg.DisplayName;
                    else if (!string.IsNullOrEmpty(b.Group) && _loc.TryGetValue(b.Group, out var gloc))
                        b.GroupDisplayName = gloc;
                    else
                        b.GroupDisplayName = b.Group;
                    foreach (var ch in b.Characters)
                    {
                        if (!string.IsNullOrEmpty(ch.NameKey) && _loc.TryGetValue(ch.NameKey, out var cloc))
                            ch.DisplayName = cloc;
                        else
                            ch.DisplayName = ch.NameKey;
                        foreach (var sub in ch.SubCharacters)
                        {
                            if (!string.IsNullOrEmpty(sub.NameKey) && _loc.TryGetValue(sub.NameKey, out var sloc))
                                sub.DisplayName = sloc;
                            else
                                sub.DisplayName = sub.NameKey;
                        }
                    }
                }
                foreach (var b in _bookmarksBase.Values)
                {
                    if (_loc.TryGetValue(b.Name, out var loc)) b.DisplayName = loc;
                    else b.DisplayName = b.Name;
                    if (!string.IsNullOrEmpty(b.Group) && _groupsBase.TryGetValue(b.Group, out var gg))
                        b.GroupDisplayName = gg.DisplayName;
                    else if (!string.IsNullOrEmpty(b.Group) && _loc.TryGetValue(b.Group, out var gloc))
                        b.GroupDisplayName = gloc;
                    else
                        b.GroupDisplayName = b.Group;
                    foreach (var ch in b.Characters)
                    {
                        if (!string.IsNullOrEmpty(ch.NameKey) && _loc.TryGetValue(ch.NameKey, out var cloc))
                            ch.DisplayName = cloc;
                        else
                            ch.DisplayName = ch.NameKey;
                        foreach (var sub in ch.SubCharacters)
                        {
                            if (!string.IsNullOrEmpty(sub.NameKey) && _loc.TryGetValue(sub.NameKey, out var sloc))
                                sub.DisplayName = sloc;
                            else
                                sub.DisplayName = sub.NameKey;
                        }
                    }
                }

                BuildTrees();
                UpdateStats();
                UpdateEmpty(false);
            }
            catch (Exception ex)
            {
                StatsText.Text = $"{Res("BookmarksTab_LoadError")}: {ex.Message}";
            }
        }

        private static (int y, int m, int d)? TryParseDate(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var parts = s.Trim().Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 1) return null;
            if (!int.TryParse(parts[0], out int y)) return null;
            int m = 1, d = 1;
            if (parts.Length >= 2) int.TryParse(parts[1], out m);
            if (parts.Length >= 3) int.TryParse(parts[2], out d);
            return (y, m, d);
        }

        private static int CompareDates(string a, string b)
        {
            var pa = TryParseDate(a);
            var pb = TryParseDate(b);
            if (pa == null && pb == null) return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            if (pa == null) return 1;
            if (pb == null) return -1;
            int c = pa.Value.y.CompareTo(pb.Value.y);
            if (c != 0) return c;
            c = pa.Value.m.CompareTo(pb.Value.m);
            if (c != 0) return c;
            c = pa.Value.d.CompareTo(pb.Value.d);
            if (c != 0) return c;
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }

        private void BuildTrees()
        {
            // MOD section: show ALL merged groups even if empty, plus bookmarks
            var modBookmarksByGroup = new Dictionary<string, List<BookmarkInfo>>(StringComparer.OrdinalIgnoreCase);
            foreach (var bm in _bookmarksMerged.Values.Where(b => b.Source == "Mod"))
            {
                string grp = string.IsNullOrEmpty(bm.Group) ? "_ungrouped" : bm.Group;
                if (!modBookmarksByGroup.TryGetValue(grp, out var list)) modBookmarksByGroup[grp] = list = new List<BookmarkInfo>();
                list.Add(bm);
            }

            var modGroups = new List<BookmarkGroupVm>();
            bool hasUngroupedMod = modBookmarksByGroup.ContainsKey("_ungrouped");
            foreach (var g in _groupsMerged.Values
                .Where(g => g.Source == "Mod" || modBookmarksByGroup.ContainsKey(g.Name))
                .OrderBy(g => TryParseDate(g.DefaultStartDate)?.y ?? int.MaxValue).ThenBy(g => g.DisplayName, StringComparer.CurrentCultureIgnoreCase))
            {
                var gvm = new BookmarkGroupVm
                {
                    Name = g.Name,
                    RawKey = g.RawKey,
                    DisplayName = g.DisplayName,
                    DefaultStartDate = g.DefaultStartDate
                };
                if (modBookmarksByGroup.TryGetValue(g.Name, out var bms))
                {
                    foreach (var bm in bms.OrderBy(b => b.StartDate != null ? TryParseDate(b.StartDate)?.y ?? int.MaxValue : int.MaxValue).ThenBy(b => b.DisplayName, StringComparer.CurrentCultureIgnoreCase))
                        gvm.Bookmarks.Add(new BookmarkVm { Name = bm.Name, DisplayName = bm.DisplayName, Source = bm.Source, Model = bm });
                }
                modGroups.Add(gvm);
            }
            // groups referenced by mod bookmarks but not in groups file (orphans)
            foreach (var kv in modBookmarksByGroup.Where(kv => kv.Key != "_ungrouped" && !_groupsMerged.ContainsKey(kv.Key)))
            {
                var gvm = new BookmarkGroupVm { Name = kv.Key, RawKey = kv.Key, DisplayName = _loc.TryGetValue(kv.Key, out var gloc) ? gloc : kv.Key };
                foreach (var bm in kv.Value.OrderBy(b => TryParseDate(b.StartDate)?.y ?? int.MaxValue).ThenBy(b => b.DisplayName, StringComparer.CurrentCultureIgnoreCase))
                    gvm.Bookmarks.Add(new BookmarkVm { Name = bm.Name, DisplayName = bm.DisplayName, Source = bm.Source, Model = bm });
                modGroups.Add(gvm);
            }
            if (hasUngroupedMod)
            {
                var ug = new BookmarkGroupVm { Name = "_ungrouped", DisplayName = Res("BookmarksTab_Ungrouped"), RawKey = "_ungrouped", DefaultStartDate = "" };
                foreach (var bm in modBookmarksByGroup["_ungrouped"].OrderBy(b => b.DisplayName, StringComparer.CurrentCultureIgnoreCase))
                    ug.Bookmarks.Add(new BookmarkVm { Name = bm.Name, DisplayName = bm.DisplayName, Source = bm.Source, Model = bm });
                modGroups.Add(ug);
            }

            // already sorted by date above (groups iteration), but ensure final sort including orphans and ungrouped
            modGroups.Sort((a, b) =>
            {
                if (a.Name == "_ungrouped" && b.Name == "_ungrouped") return 0;
                if (a.Name == "_ungrouped") return 1;
                if (b.Name == "_ungrouped") return -1;
                int c = CompareDates(a.DefaultStartDate ?? "", b.DefaultStartDate ?? "");
                if (c != 0) return c;
                return string.Compare(a.DisplayName, b.DisplayName, StringComparison.CurrentCultureIgnoreCase);
            });

            BookmarkTreeMod.ItemsSource = modGroups;
            ModEmptyText.Visibility = modGroups.All(g => g.Bookmarks.Count == 0) && !hasUngroupedMod ? Visibility.Visible : Visibility.Collapsed;

            // BASE section: all base groups + base bookmarks (informative)
            var baseGroupsMap = new Dictionary<string, BookmarkGroupVm>(StringComparer.OrdinalIgnoreCase);
            foreach (var g in _groupsBase.Values)
            {
                baseGroupsMap[g.Name] = new BookmarkGroupVm { Name = g.Name, RawKey = g.RawKey, DisplayName = g.DisplayName, DefaultStartDate = g.DefaultStartDate };
            }
            // ensure groups referenced by base bookmarks but not in groups file still appear
            foreach (var bm in _bookmarksBase.Values)
            {
                string grp = string.IsNullOrEmpty(bm.Group) ? "_ungrouped" : bm.Group;
                if (!baseGroupsMap.ContainsKey(grp) && grp != "_ungrouped")
                    baseGroupsMap[grp] = new BookmarkGroupVm { Name = grp, RawKey = grp, DisplayName = _loc.TryGetValue(grp, out var gloc) ? gloc : grp };
            }
            bool hasUngroupedBase = _bookmarksBase.Values.Any(b => string.IsNullOrEmpty(b.Group));
            if (hasUngroupedBase && !baseGroupsMap.ContainsKey("_ungrouped"))
                baseGroupsMap["_ungrouped"] = new BookmarkGroupVm { Name = "_ungrouped", DisplayName = Res("BookmarksTab_Ungrouped") };
            if (!hasUngroupedBase && baseGroupsMap.ContainsKey("_ungrouped"))
                baseGroupsMap.Remove("_ungrouped");

            foreach (var bm in _bookmarksBase.Values)
            {
                var vm = new BookmarkVm { Name = bm.Name, DisplayName = bm.DisplayName, Source = "Base", Model = bm };
                string grp = string.IsNullOrEmpty(bm.Group) ? "_ungrouped" : bm.Group;
                if (baseGroupsMap.TryGetValue(grp, out var gv))
                    gv.Bookmarks.Add(vm);
                else
                    baseGroupsMap["_ungrouped"].Bookmarks.Add(vm);
            }

            // sort bookmarks inside each base group by date
            foreach (var gv in baseGroupsMap.Values)
            {
                var sorted = gv.Bookmarks.OrderBy(b => b.Model?.StartDate != null ? TryParseDate(b.Model.StartDate)?.y ?? int.MaxValue : int.MaxValue).ThenBy(b => b.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
                gv.Bookmarks.Clear();
                foreach (var b in sorted) gv.Bookmarks.Add(b);
            }

            var baseGroupsList = baseGroupsMap.Values.ToList();
            // keep only groups with bookmarks? For base informative we keep all groups even empty? Requirement: show base groups info even if replaced. Keep all.
            baseGroupsList.Sort((a, b) =>
            {
                if (a.Name == "_ungrouped" && b.Name == "_ungrouped") return 0;
                if (a.Name == "_ungrouped") return 1;
                if (b.Name == "_ungrouped") return -1;
                int c = CompareDates(a.DefaultStartDate ?? "", b.DefaultStartDate ?? "");
                if (c != 0) return c;
                return string.Compare(a.DisplayName, b.DisplayName, StringComparison.CurrentCultureIgnoreCase);
            });

            BookmarkTreeBase.ItemsSource = baseGroupsList;
            BaseEmptyText.Visibility = baseGroupsList.All(g => g.Bookmarks.Count == 0) ? Visibility.Visible : Visibility.Collapsed;

            StatsText.Text = $"{_bookmarksMerged.Count} {Res("BookmarksTab_BookmarksCount")} (mod {_bookmarksMerged.Values.Count(b => b.Source == "Mod")} / base {_bookmarksBase.Count})";
        }

        private void UpdateStats()
        {
            int totalGroups = _groupsMerged.Count;
            int total = _bookmarksMerged.Count;
            int mod = _bookmarksMerged.Values.Count(b => b.Source == "Mod");
            int bas = _bookmarksBase.Count;
            StatsGroupsValue.Text = $"{Res("BookmarksTab_Groups")}: {totalGroups} / base { _groupsBase.Count}";
            StatsBookmarksValue.Text = $"{Res("BookmarksTab_Bookmarks")}: {total}";
            StatsModBookmarksValue.Text = $"{Res("BookmarksTab_ModBookmarks")}: {mod}";
            StatsBaseBookmarksValue.Text = $"{Res("BookmarksTab_BaseBookmarks")}: {bas}";
        }

        private void BookmarkTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Determine which TreeView fired and get its selected item
            BookmarkVm? bmVm = null;
            BookmarkGroupVm? gVm = null;
            if (sender is System.Windows.Controls.TreeView tv)
            {
                if (tv.SelectedItem is BookmarkVm bv) bmVm = bv;
                else if (tv.SelectedItem is BookmarkGroupVm gv) gVm = gv;
            }
            else
            {
                // fallback
                if (BookmarkTreeMod.SelectedItem is BookmarkVm b1) bmVm = b1;
                else if (BookmarkTreeBase.SelectedItem is BookmarkVm b2) bmVm = b2;
                else if (BookmarkTreeMod.SelectedItem is BookmarkGroupVm g1) gVm = g1;
                else if (BookmarkTreeBase.SelectedItem is BookmarkGroupVm g2) gVm = g2;
            }

            if (bmVm != null)
            {
                // clear selection of the other tree
                if (sender != BookmarkTreeMod) BookmarkTreeMod.SelectedItemChanged -= BookmarkTree_SelectedItemChanged;
                if (sender != BookmarkTreeBase) BookmarkTreeBase.SelectedItemChanged -= BookmarkTree_SelectedItemChanged;
                try
                {
                    if (sender != BookmarkTreeMod && BookmarkTreeMod.SelectedItem != null) UnselectTree(BookmarkTreeMod);
                    if (sender != BookmarkTreeBase && BookmarkTreeBase.SelectedItem != null) UnselectTree(BookmarkTreeBase);
                }
                finally
                {
                    if (sender != BookmarkTreeMod) BookmarkTreeMod.SelectedItemChanged += BookmarkTree_SelectedItemChanged;
                    if (sender != BookmarkTreeBase) BookmarkTreeBase.SelectedItemChanged += BookmarkTree_SelectedItemChanged;
                }
                ShowBookmark(bmVm.Model);
                return;
            }
            if (gVm != null)
            {
                if (sender != BookmarkTreeMod) BookmarkTreeMod.SelectedItemChanged -= BookmarkTree_SelectedItemChanged;
                if (sender != BookmarkTreeBase) BookmarkTreeBase.SelectedItemChanged -= BookmarkTree_SelectedItemChanged;
                try
                {
                    if (sender != BookmarkTreeMod && BookmarkTreeMod.SelectedItem != null) UnselectTree(BookmarkTreeMod);
                    if (sender != BookmarkTreeBase && BookmarkTreeBase.SelectedItem != null) UnselectTree(BookmarkTreeBase);
                }
                finally
                {
                    if (sender != BookmarkTreeMod) BookmarkTreeMod.SelectedItemChanged += BookmarkTree_SelectedItemChanged;
                    if (sender != BookmarkTreeBase) BookmarkTreeBase.SelectedItemChanged += BookmarkTree_SelectedItemChanged;
                }
                ShowGroup(gVm);
                return;
            }
            DetailGroup.Visibility = Visibility.Collapsed;
            GroupDetailGroup.Visibility = Visibility.Collapsed;
            DetailEmptyText.Visibility = Visibility.Visible;
        }

        private static void UnselectTree(System.Windows.Controls.TreeView tv)
        {
            // workaround to clear selection: set to null via reflection of TreeViewItem
            var item = tv.SelectedItem;
            if (item == null) return;
            // Find TreeViewItem and set IsSelected false
            var container = tv.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
            if (container != null) container.IsSelected = false;
        }

        private void ShowGroup(BookmarkGroupVm? gv)
        {
            DetailGroup.Visibility = Visibility.Collapsed;
            if (gv == null || gv.Name == "_ungrouped")
            {
                GroupDetailGroup.Visibility = Visibility.Collapsed;
                DetailEmptyText.Visibility = gv?.Name == "_ungrouped" ? Visibility.Collapsed : Visibility.Visible;
                if (gv?.Name == "_ungrouped")
                {
                    GroupDetailGroup.Visibility = Visibility.Visible;
                    GroupNameValue.Text = Res("BookmarksTab_Ungrouped");
                    GroupSourceValue.Text = "-";
                    GroupSourceFileValue.Text = "";
                    GroupDefaultStartDateValue.Text = "-";
                    GroupRawValue.Text = Res("BookmarksTab_UngroupedHint");
                }
                return;
            }
            // try merged first then base
            BookmarkGroupInfo? g = null;
            if (!_groupsMerged.TryGetValue(gv.Name, out g))
                _groupsBase.TryGetValue(gv.Name, out g);
            if (g == null)
            {
                GroupDetailGroup.Visibility = Visibility.Collapsed;
                return;
            }
            GroupDetailGroup.Visibility = Visibility.Visible;
            DetailEmptyText.Visibility = Visibility.Collapsed;
            GroupNameValue.Text = g.DisplayName;
            GroupSourceValue.Text = g.Source == "Mod" ? $"{Res("BookmarksTab_SourceMod")} {(g.IsModNew ? Res("BookmarksTab_SourceModNew") : "")}" : Res("BookmarksTab_SourceBase");
            GroupSourceFileValue.Text = g.SourceFile ?? "";
            GroupDefaultStartDateValue.Text = FormatGroupDateWithOffset(g.DefaultStartDate);
            GroupRawValue.Text = string.IsNullOrEmpty(g.RawBlock) ? "-" : g.RawBlock.Trim();
        }

        private string FormatGroupDateWithOffset(string? fileDate)
        {
            if (string.IsNullOrWhiteSpace(fileDate)) return "-";
            int offset = _viewModel?.CurrentProfile?.YearOffset ?? 0;
            string real = BookmarkLoader.ShiftDate(fileDate, -offset) ?? fileDate.Trim();
            var parts = real.Split('.', System.StringSplitOptions.TrimEntries);
            if (parts.Length < 1 || !int.TryParse(parts[0], out int y)) return real;
            int m = 1, d = 1;
            if (parts.Length >= 2) int.TryParse(parts[1], out m);
            if (parts.Length >= 3) int.TryParse(parts[2], out d);
            string lang = _viewModel?.Language ?? "en";
            string culture = lang switch { "es" => "es-ES", "ca" => "ca-ES", _ => "en-GB" };
            string month;
            try
            {
                var ci = new System.Globalization.CultureInfo(culture);
                month = (m >= 1 && m <= 12) ? ci.DateTimeFormat.GetMonthName(m) : m.ToString();
            }
            catch { month = m.ToString(); }
            if (y < 0) return lang == "en" ? $"{d} {month} {-y} BCE" : $"{d} {month} {-y} AEC";
            return $"{d} {month} {y}";
        }

        private void ShowBookmark(BookmarkInfo? bm)
        {
            GroupDetailGroup.Visibility = Visibility.Collapsed;
            if (bm == null)
            {
                DetailGroup.Visibility = Visibility.Collapsed;
                DetailEmptyText.Visibility = Visibility.Visible;
                return;
            }
            DetailGroup.Visibility = Visibility.Visible;
            DetailEmptyText.Visibility = Visibility.Collapsed;
            DetailNameValue.Text = bm.DisplayName;
            DetailSourceValue.Text = bm.Source == "Mod" ? $"{Res("BookmarksTab_SourceMod")} {(bm.IsModNew ? Res("BookmarksTab_SourceModNew") : "")}" : Res("BookmarksTab_SourceBase");
            DetailSourceFileValue.Text = bm.SourceFile ?? "";
            DetailStartDateValue.Text = string.IsNullOrEmpty(bm.StartDate) ? "-" : bm.StartDate;
            DetailGroupValue.Text = string.IsNullOrEmpty(bm.GroupDisplayName) ? "-" : bm.GroupDisplayName;
            DetailIsPlayableValue.Text = string.IsNullOrEmpty(bm.IsPlayable) ? "-" : bm.IsPlayable;
            DetailRecommendedValue.Text = string.IsNullOrEmpty(bm.Recommended) ? "-" : bm.Recommended;
            DetailRequiresDlcValue.Text = string.IsNullOrEmpty(bm.RequiresDlcFlag) ? "-" : bm.RequiresDlcFlag;
            DetailRawValue.Text = string.IsNullOrEmpty(bm.RawBlock) ? "-" : bm.RawBlock.Trim();

            var list = new ObservableCollection<CharacterVm>();
            foreach (var ch in bm.Characters)
            {
                var vm = new CharacterVm
                {
                    TitleLine = BuildTitleLine(ch),
                    MetaLine = BuildMetaLine(ch)
                };
                foreach (var sub in ch.SubCharacters)
                {
                    vm.SubCharacters.Add(new CharacterVm
                    {
                        SubLine = BuildSubLine(sub)
                    });
                }
                list.Add(vm);
            }
            CharactersList.ItemsSource = list;
        }

        private string BuildTitleLine(BookmarkCharacter ch)
        {
            string name = string.IsNullOrEmpty(ch.DisplayName) ? ch.NameKey : ch.DisplayName;
            if (string.IsNullOrEmpty(name)) name = ch.HistoryId ?? "-";
            string title = string.IsNullOrEmpty(ch.Title) ? "" : $" — {ch.Title}";
            return $"{name}{title}";
        }

        private string BuildMetaLine(BookmarkCharacter ch)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(ch.Dynasty)) parts.Add($"dynasty {ch.Dynasty}");
            if (!string.IsNullOrEmpty(ch.DynastyHouse)) parts.Add(ch.DynastyHouse);
            if (!string.IsNullOrEmpty(ch.Culture)) parts.Add(ch.Culture);
            if (!string.IsNullOrEmpty(ch.Religion)) parts.Add(ch.Religion);
            if (!string.IsNullOrEmpty(ch.Government)) parts.Add(ch.Government);
            if (!string.IsNullOrEmpty(ch.Birth)) parts.Add($"b.{ch.Birth}");
            if (!string.IsNullOrEmpty(ch.Difficulty)) parts.Add(ch.Difficulty);
            if (!string.IsNullOrEmpty(ch.HistoryId)) parts.Add($"id {ch.HistoryId}");
            if (!string.IsNullOrEmpty(ch.Relation)) parts.Add(ch.Relation);
            if (!string.IsNullOrEmpty(ch.BookmarkType)) parts.Add(ch.BookmarkType);
            return parts.Count == 0 ? "-" : string.Join(" · ", parts);
        }

        private string BuildSubLine(BookmarkCharacter ch)
        {
            string name = string.IsNullOrEmpty(ch.DisplayName) ? ch.NameKey : ch.DisplayName;
            if (string.IsNullOrEmpty(name)) name = ch.HistoryId ?? "-";
            var meta = BuildMetaLine(ch);
            return $"{name} — {meta}";
        }

        private void UpdateEmpty(bool noData)
        {
            if (noData)
            {
                DetailGroup.Visibility = Visibility.Collapsed;
                GroupDetailGroup.Visibility = Visibility.Collapsed;
            }
            DetailEmptyText.Visibility = DetailGroup.Visibility == Visibility.Visible || GroupDetailGroup.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BookmarkTree_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var item = FindAncestor<System.Windows.Controls.TreeViewItem>(e.OriginalSource as DependencyObject);
            if (item != null) { item.IsSelected = true; item.Focus(); }
        }

        private void BookmarkTree_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var sel = BookmarkTreeMod.SelectedItem;
            // New group / new bookmark visibility: group -> both, marker -> only new marker
            bool showNewGroup = true;
            if (sel is BookmarkVm) showNewGroup = false;
            if (CtxNewGroupMenuItem != null) CtxNewGroupMenuItem.Visibility = showNewGroup ? Visibility.Visible : Visibility.Collapsed;
            if (CtxNewBookmarkMenuItem != null) CtxNewBookmarkMenuItem.Visibility = Visibility.Visible;
            // if base tree has selection, prefer it? handled separately
            if (sel is BookmarkVm vm)
            {
                bool canEdit = vm.Model != null && vm.Model.Source == "Mod";
                if (CtxEditMenuItem != null) CtxEditMenuItem.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
                if (CtxDeleteMenuItem != null) CtxDeleteMenuItem.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (sel is BookmarkGroupVm gv)
            {
                // groups: check if mod group
                bool isMod = false;
                if (_groupsMerged.TryGetValue(gv.Name, out var g)) isMod = g.Source == "Mod";
                if (CtxEditMenuItem != null) CtxEditMenuItem.Visibility = isMod ? Visibility.Visible : Visibility.Collapsed;
                if (CtxDeleteMenuItem != null) CtxDeleteMenuItem.Visibility = isMod ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                if (CtxEditMenuItem != null) CtxEditMenuItem.Visibility = Visibility.Collapsed;
                if (CtxDeleteMenuItem != null) CtxDeleteMenuItem.Visibility = Visibility.Collapsed;
            }
        }

        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null && current is not T) current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            return current as T;
        }

        private void CtxNewGroup_Click(object sender, RoutedEventArgs e) => OpenEditorForGroup(null, true);
        private void CtxNewBookmark_Click(object sender, RoutedEventArgs e) => OpenEditorForBookmark(null, true);

        private void CtxCopy_Click(object sender, RoutedEventArgs e)
        {
            if (BookmarkTreeMod.SelectedItem is BookmarkVm bm && bm.Model != null) OpenEditorForBookmark(bm.Model, true);
            else if (BookmarkTreeMod.SelectedItem is BookmarkGroupVm gv && _groupsMerged.TryGetValue(gv.Name, out var g)) OpenEditorForGroup(g, true);
        }

        private void CtxEdit_Click(object sender, RoutedEventArgs e)
        {
            if (BookmarkTreeMod.SelectedItem is BookmarkVm bm && bm.Model != null) OpenEditorForBookmark(bm.Model, false);
            else if (BookmarkTreeMod.SelectedItem is BookmarkGroupVm gv && _groupsMerged.TryGetValue(gv.Name, out var g)) OpenEditorForGroup(g, false);
        }

        private void CtxDelete_Click(object sender, RoutedEventArgs e)
        {
            if (BookmarkTreeMod.SelectedItem is BookmarkVm bm && bm.Model != null) DeleteBookmark(bm.Model);
            else if (BookmarkTreeMod.SelectedItem is BookmarkGroupVm gv && _groupsMerged.TryGetValue(gv.Name, out var g)) DeleteGroup(g);
        }

        private void OpenEditorForGroup(BookmarkGroupInfo? group, bool asNew)
        {
            _editorType = "Group";
            _editorGroup = group;
            _editorBookmark = null;
            _editorIsNew = asNew || group == null;
            int off = _viewModel?.CurrentProfile?.YearOffset ?? 0;
            string ToReal(string fileDate) => BookmarkLoader.ShiftDate(fileDate, -off) ?? fileDate;
            if (group != null && !asNew)
            {
                GroupEditorTabHeaderText.Text = $"{Res("BookmarksTab_GroupEditorEditTitle")}: {group.DisplayName}";
                GroupEditorModeText.Text = $"{Res("BookmarksTab_GroupEditorEditTitle")}: {group.DisplayName}";
                EditorGroupId.Text = group.Name;
                EditorGroupName.Text = LookupBookmarkLoc(group.Name) ?? group.DisplayName;
                EditorGroupDate.Text = ToReal(group.DefaultStartDate ?? "");
                _editorSavedGroupName = EditorGroupName.Text?.Trim() ?? "";
            }
            else if (group != null && asNew)
            {
                GroupEditorTabHeaderText.Text = Res("BookmarksTab_GroupEditorNewTitle");
                GroupEditorModeText.Text = $"{Res("BookmarksTab_GroupEditorNewTitle")} ({group.DisplayName})";
                EditorGroupId.Text = group.Name + "_copy";
                EditorGroupName.Text = (LookupBookmarkLoc(group.Name) ?? group.DisplayName);
                EditorGroupDate.Text = ToReal(group.DefaultStartDate ?? "");
                _editorSavedGroupName = "";
            }
            else
            {
                GroupEditorTabHeaderText.Text = Res("BookmarksTab_GroupEditorNewTitle");
                GroupEditorModeText.Text = Res("BookmarksTab_GroupEditorNewTitle");
                EditorGroupId.Text = "";
                EditorGroupName.Text = "";
                EditorGroupDate.Text = "";
                _editorSavedGroupName = "";
            }
            GroupEditorHintText.Text = Res("BookmarksTab_GroupEditorHint");
            EditorGroupId.IsReadOnly = !_editorIsNew;
            EditorGroupId.IsEnabled = true;
            UpdateEditorModeUi();
            GroupEditorTabItem.Visibility = Visibility.Visible;
            BookmarkEditorTabItem.Visibility = Visibility.Collapsed;
            BookmarksSubTabs.SelectedItem = GroupEditorTabItem;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                EditorGroupId.IsReadOnly = !_editorIsNew;
                EditorGroupId.IsEnabled = true;
                if (_editorIsNew) { EditorGroupId.Focus(); EditorGroupId.SelectAll(); }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void OpenEditorForBookmark(BookmarkInfo? bm, bool asNew)
        {
            _editorType = "Bookmark";
            _editorBookmark = bm;
            _editorGroup = null;
            _editorIsNew = asNew || bm == null;
            RefreshBookmarkGroupCombo();
            if (bm != null && !asNew)
            {
                BookmarkEditorTabHeaderText.Text = $"{Res("BookmarksTab_BookmarkEditorEditTitle")}: {bm.DisplayName}";
                BookmarkEditorModeText.Text = $"{Res("BookmarksTab_BookmarkEditorEditTitle")}: {bm.DisplayName}";
                EditorBookmarkId.Text = bm.Name;
                EditorBookmarkStartDate.Text = bm.StartDate ?? "";
                EditorBookmarkGroup.Text = bm.Group ?? "";
                EditorIsPlayable.IsChecked = string.Equals(bm.IsPlayable, "yes", StringComparison.OrdinalIgnoreCase);
                EditorRecommended.IsChecked = string.Equals(bm.Recommended, "yes", StringComparison.OrdinalIgnoreCase);
                EditorRequiresDlc.Text = bm.RequiresDlcFlag ?? "";
                var ch = bm.Characters.FirstOrDefault();
                if (ch != null)
                {
                    EditorCharName.Text = ch.NameKey ?? "";
                    EditorCharHistoryId.Text = ch.HistoryId ?? "";
                    EditorCharTitle.Text = ch.Title ?? "";
                    EditorCharCulture.Text = ch.Culture ?? "";
                    EditorCharReligion.Text = ch.Religion ?? "";
                }
                else
                {
                    EditorCharName.Text = ""; EditorCharHistoryId.Text = ""; EditorCharTitle.Text = ""; EditorCharCulture.Text = ""; EditorCharReligion.Text = "";
                }
            }
            else if (bm != null && asNew)
            {
                BookmarkEditorTabHeaderText.Text = Res("BookmarksTab_BookmarkEditorNewTitle");
                BookmarkEditorModeText.Text = $"{Res("BookmarksTab_BookmarkEditorNewTitle")} ({bm.DisplayName})";
                EditorBookmarkId.Text = bm.Name + "_copy";
                EditorBookmarkStartDate.Text = bm.StartDate ?? "";
                EditorBookmarkGroup.Text = bm.Group ?? "";
                EditorIsPlayable.IsChecked = string.Equals(bm.IsPlayable, "yes", StringComparison.OrdinalIgnoreCase);
                EditorRecommended.IsChecked = string.Equals(bm.Recommended, "yes", StringComparison.OrdinalIgnoreCase);
                EditorRequiresDlc.Text = bm.RequiresDlcFlag ?? "";
                var ch = bm.Characters.FirstOrDefault();
                if (ch != null)
                {
                    EditorCharName.Text = ch.NameKey ?? "";
                    EditorCharHistoryId.Text = ch.HistoryId ?? "";
                    EditorCharTitle.Text = ch.Title ?? "";
                    EditorCharCulture.Text = ch.Culture ?? "";
                    EditorCharReligion.Text = ch.Religion ?? "";
                }
                else
                {
                    EditorCharName.Text = ""; EditorCharHistoryId.Text = ""; EditorCharTitle.Text = ""; EditorCharCulture.Text = ""; EditorCharReligion.Text = "";
                }
            }
            else
            {
                BookmarkEditorTabHeaderText.Text = Res("BookmarksTab_BookmarkEditorNewTitle");
                BookmarkEditorModeText.Text = Res("BookmarksTab_BookmarkEditorNewTitle");
                EditorBookmarkId.Text = "";
                EditorBookmarkStartDate.Text = "";
                EditorBookmarkGroup.Text = "";
                EditorIsPlayable.IsChecked = true;
                EditorRecommended.IsChecked = false;
                EditorRequiresDlc.Text = "";
                EditorCharName.Text = ""; EditorCharHistoryId.Text = ""; EditorCharTitle.Text = ""; EditorCharCulture.Text = ""; EditorCharReligion.Text = "";
            }
            BookmarkEditorHintText.Text = Res("BookmarksTab_BookmarkEditorHint");
            UpdateEditorModeUi();
            BookmarkEditorTabItem.Visibility = Visibility.Visible;
            GroupEditorTabItem.Visibility = Visibility.Collapsed;
            BookmarksSubTabs.SelectedItem = BookmarkEditorTabItem;
        }

        private void RefreshBookmarkGroupCombo()
        {
            if (EditorBookmarkGroup == null) return;
            var groups = _groupsMerged.Values.OrderBy(g => g.DisplayName, StringComparer.CurrentCultureIgnoreCase).Select(g => g.Name).ToList();
            EditorBookmarkGroup.ItemsSource = groups;
        }

        private void DeleteBookmark(BookmarkInfo bm)
        {
            if (bm.Source != "Mod" || string.IsNullOrEmpty(bm.SourceFile)) { BookmarkEditorStatusText.Text = Res("BookmarksTab_DeleteNotAllowed"); return; }
            var confirm = System.Windows.MessageBox.Show(string.Format(Res("BookmarksTab_DeleteConfirm"), bm.DisplayName), Res("BookmarksTab_DeleteConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
            try
            {
                string file = bm.SourceFile;
                if (!BookmarkLoader.DeleteBookmarkBlockFromFile(file, bm.RawKey)) { BookmarkEditorStatusText.Text = Res("BookmarksTab_DeleteBlockNotFound"); return; }
                if (BookmarkLoader.CountBookmarkBlocks(file) == 0) System.IO.File.Delete(file);
                LoadBookmarks();
                BookmarkEditorStatusText.Text = string.Format(Res("BookmarksTab_DeleteSuccess"), bm.Name);
                if (_editorBookmark != null && string.Equals(_editorBookmark.RawKey, bm.RawKey, StringComparison.OrdinalIgnoreCase))
                {
                    EditorClear_Click(null!, null!);
                }
            }
            catch (Exception ex) { BookmarkEditorStatusText.Text = $"{Res("BookmarksTab_DeleteError")}: {ex.Message}"; }
        }

        private void DeleteGroup(BookmarkGroupInfo g)
        {
            if (g.Source != "Mod" || string.IsNullOrEmpty(g.SourceFile)) { GroupEditorStatusText.Text = Res("BookmarksTab_DeleteNotAllowed"); return; }
            // check if group has bookmarks in mod
            bool hasModBookmarks = _bookmarksMerged.Values.Any(b => b.Source == "Mod" && string.Equals(b.Group, g.Name, StringComparison.OrdinalIgnoreCase));
            if (hasModBookmarks) { GroupEditorStatusText.Text = Res("BookmarksTab_DeleteGroupHasBookmarks"); return; }
            var confirm = System.Windows.MessageBox.Show(string.Format(Res("BookmarksTab_DeleteConfirm"), g.DisplayName), Res("BookmarksTab_DeleteConfirmTitle"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
            try
            {
                string file = g.SourceFile;
                if (!BookmarkLoader.DeleteGroupBlockFromFile(file, g.RawKey)) { GroupEditorStatusText.Text = Res("BookmarksTab_DeleteBlockNotFound"); return; }
                if (BookmarkLoader.CountGroupBlocks(file) == 0) System.IO.File.Delete(file);
                var profile = _viewModel?.CurrentProfile;
                if (profile != null && !string.IsNullOrEmpty(profile.ModRoot))
                    DeleteGroupLocalization(profile.ModRoot, g.RawKey);
                LoadBookmarks();
                GroupEditorStatusText.Text = string.Format(Res("BookmarksTab_DeleteSuccess"), g.Name);
                if (_editorGroup != null && string.Equals(_editorGroup.RawKey, g.RawKey, StringComparison.OrdinalIgnoreCase))
                    EditorClear_Click(null!, null!);
            }
            catch (Exception ex) { GroupEditorStatusText.Text = $"{Res("BookmarksTab_DeleteError")}: {ex.Message}"; }
        }

        private static void DeleteGroupLocalization(string modRoot, string groupId)
        {
            // groups are stored in replace/ (vanilla override) but also clean legacy non-replace locations
            var roots = new[]
            {
                System.IO.Path.Combine(modRoot, "localization", "replace"),
                System.IO.Path.Combine(modRoot, "localization")
            };
            foreach (var root in roots)
            {
                if (!System.IO.Directory.Exists(root)) continue;
                foreach (var file in System.IO.Directory.GetFiles(root, "bookmarks_l_*.yml", System.IO.SearchOption.AllDirectories))
                    RemoveBookmarkLocKeys(file, new[] { groupId });
            }
        }

        private static void RemoveBookmarkLocKeys(string filePath, string[] keys)
        {
            var lines = new List<string>(System.IO.File.ReadAllLines(filePath));
            bool changed = false;
            lines.RemoveAll(line =>
            {
                string t = line.TrimStart();
                foreach (var key in keys)
                {
                    if (t.StartsWith(key + ":", StringComparison.OrdinalIgnoreCase)) { changed = true; return true; }
                }
                return false;
            });
            if (changed)
            {
                if (!HasBookmarkLocEntries(lines)) System.IO.File.Delete(filePath);
                else System.IO.File.WriteAllLines(filePath, lines, new System.Text.UTF8Encoding(true));
            }
        }

        private static bool HasBookmarkLocEntries(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                string t = line.Trim();
                if (string.IsNullOrEmpty(t) || t.StartsWith("#")) continue;
                if (t.IndexOf(':') >= 0 && t.IndexOf('"') >= 0) return true;
            }
            return false;
        }

        private void EditorClear_Click(object sender, RoutedEventArgs e)
        {
            if (_editorType == "Group")
            {
                if (_editorIsNew || _editorGroup == null)
                {
                    // new mode: blank fields, keep "Nuevo grupo" title
                    EditorGroupId.Text = ""; EditorGroupName.Text = ""; EditorGroupDate.Text = "";
                    _editorGroup = null; _editorIsNew = true;
                }
                else
                {
                    // edit mode: restore saved values, keep "Editar grupo: ..." title
                    var g = _editorGroup;
                    EditorGroupId.Text = g.Name;
                    EditorGroupName.Text = LookupBookmarkLoc(g.Name) ?? g.DisplayName;
                    int off = _viewModel?.CurrentProfile?.YearOffset ?? 0;
                    EditorGroupDate.Text = BookmarkLoader.ShiftDate(g.DefaultStartDate ?? "", -off) ?? g.DefaultStartDate ?? "";
                }
                GroupEditorStatusText.Text = Res("BookmarksTab_GroupEditorHint");
            }
            else
            {
                if (_editorIsNew || _editorBookmark == null)
                {
                    EditorBookmarkId.Text = ""; EditorBookmarkStartDate.Text = ""; EditorBookmarkGroup.Text = "";
                    EditorIsPlayable.IsChecked = true; EditorRecommended.IsChecked = false; EditorRequiresDlc.Text = "";
                    EditorCharName.Text = ""; EditorCharHistoryId.Text = ""; EditorCharTitle.Text = ""; EditorCharCulture.Text = ""; EditorCharReligion.Text = "";
                    _editorBookmark = null; _editorIsNew = true;
                }
                else
                {
                    var bm = _editorBookmark;
                    EditorBookmarkId.Text = bm.Name;
                    EditorBookmarkStartDate.Text = bm.StartDate ?? "";
                    EditorBookmarkGroup.Text = bm.Group ?? "";
                    EditorIsPlayable.IsChecked = string.Equals(bm.IsPlayable, "yes", StringComparison.OrdinalIgnoreCase);
                    EditorRecommended.IsChecked = string.Equals(bm.Recommended, "yes", StringComparison.OrdinalIgnoreCase);
                    EditorRequiresDlc.Text = bm.RequiresDlcFlag ?? "";
                    var ch = bm.Characters.FirstOrDefault();
                    if (ch != null)
                    {
                        EditorCharName.Text = ch.NameKey ?? ""; EditorCharHistoryId.Text = ch.HistoryId ?? "";
                        EditorCharTitle.Text = ch.Title ?? ""; EditorCharCulture.Text = ch.Culture ?? ""; EditorCharReligion.Text = ch.Religion ?? "";
                    }
                    else
                    {
                        EditorCharName.Text = ""; EditorCharHistoryId.Text = ""; EditorCharTitle.Text = ""; EditorCharCulture.Text = ""; EditorCharReligion.Text = "";
                    }
                }
                BookmarkEditorStatusText.Text = Res("BookmarksTab_BookmarkEditorHint");
            }
            UpdateEditorModeUi();
        }

        private void GroupEditorClear_Click(object sender, RoutedEventArgs e) { _editorType = "Group"; EditorClear_Click(sender, e); }
        private void BookmarkEditorClear_Click(object sender, RoutedEventArgs e) { _editorType = "Bookmark"; EditorClear_Click(sender, e); }
        private async void GroupEditorSave_Click(object sender, RoutedEventArgs e)
        {
            _editorType = "Group";
            GroupEditorStatusText.Text = "Guardando grupo…";
            LogBookmark($"CLICK Save group: rawId='{EditorGroupId.Text}' name='{EditorGroupName.Text}' date='{EditorGroupDate.Text}' isNew={_editorIsNew}");
            string id = NormalizeFileId(EditorGroupId.Text?.Trim() ?? "");
            bool ok = EditorSave_Click(sender, e);
            LogBookmark($"CLICK Save group result: ok={ok} id='{id}' status='{GroupEditorStatusText.Text}'");
            if (!ok) return;
            // resolve saved id for localization (editor may have set _editorGroup)
            string savedId = _editorGroup?.RawKey ?? _editorGroup?.Name ?? id;
            if (!string.IsNullOrEmpty(savedId))
                await SaveGroupLocalizationIfChanged(savedId);
        }
        private void BookmarkEditorSave_Click(object sender, RoutedEventArgs e) { _editorType = "Bookmark"; EditorSave_Click(sender, e); }

        private static string? _logFilePath;
        private static void LogBookmark(string msg)
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\r\n";
            // primary: exe folder
            try
            {
                string dir = System.IO.Path.Combine(AppContext.BaseDirectory, "logs");
                System.IO.Directory.CreateDirectory(dir);
                _logFilePath = System.IO.Path.Combine(dir, "bookmarks_debug.log");
                System.IO.File.AppendAllText(_logFilePath, line);
            }
            catch { }
            // fallback: MyDocuments (publish/single-file safe)
            try
            {
                string dir2 = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PdxModIDE_logs");
                System.IO.Directory.CreateDirectory(dir2);
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir2, "bookmarks_debug.log"), line);
            }
            catch { }
        }

        private static string NormalizeFileId(string input)
        {
            // id for common/: english, spaces -> _, no quotes
            string s = (input ?? "").Trim().ToLowerInvariant().Replace(' ', '_');
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s) sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            string r = sb.ToString();
            while (r.Contains("__")) r = r.Replace("__", "_");
            return r.Trim('_');
        }

        private string? LookupBookmarkLoc(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return _loc.TryGetValue(key, out var v) ? v : null;
        }

        private PdxModIDE.UI.Translation.ITranslationProvider[] BuildBookmarkProviders()
        {
            var enabled = _viewModel?.EnabledTranslationProviders ?? new List<string> { "mymemory" };
            var urls = _viewModel?.TranslationProviderUrls ?? new Dictionary<string, string>();
            var key = _viewModel?.DeeplApiKey;
            var list = new List<PdxModIDE.UI.Translation.ITranslationProvider>();
            foreach (var id in enabled)
            {
                switch (id)
                {
                    case "mymemory": list.Add(new PdxModIDE.UI.Translation.MyMemoryProvider(_translationHttp)); break;
                    case "libretranslate":
                        var lu = urls.TryGetValue("libretranslate", out var l) && !string.IsNullOrWhiteSpace(l) ? l! : PdxModIDE.UI.Translation.TranslationProviderConstants.DefaultLibreTranslateUrl;
                        list.Add(new PdxModIDE.UI.Translation.LibreTranslateProvider(_translationHttp, lu)); break;
                    case "lingva":
                        var lv = urls.TryGetValue("lingva", out var v) && !string.IsNullOrWhiteSpace(v) ? v! : PdxModIDE.UI.Translation.TranslationProviderConstants.DefaultLingvaUrl;
                        list.Add(new PdxModIDE.UI.Translation.LingvaProvider(_translationHttp, lv)); break;
                    case "deepl":
                        if (!string.IsNullOrWhiteSpace(key)) list.Add(new PdxModIDE.UI.Translation.DeepLProvider(_translationHttp, key)); break;
                }
            }
            if (list.Count == 0) list.Add(new PdxModIDE.UI.Translation.MyMemoryProvider(_translationHttp));
            var rng = new Random();
            for (int i = list.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (list[i], list[j]) = (list[j], list[i]); }
            return list.ToArray();
        }

        private async Task<(string? Text, bool Ok)> TranslateBookmarkAsync(string text, string src, string dst, PdxModIDE.UI.Translation.ITranslationProvider[] providers)
        {
            if (string.IsNullOrWhiteSpace(text)) return (text, true);
            foreach (var p in providers)
            {
                var (r, ok) = await p.TranslateAsync(text, src, dst);
                if (ok && !string.IsNullOrWhiteSpace(r)) return (r, true);
            }
            return (text, false);
        }

        private async Task SaveGroupLocalizationIfChanged(string? forceId = null)
        {
            var profile = _viewModel?.CurrentProfile;
            if (profile == null) return;
            string modRoot = profile.ModRoot ?? "";
            if (string.IsNullOrEmpty(modRoot)) return;
            string? gid = forceId ?? _editorGroup?.RawKey ?? _editorGroup?.Name;
            string editId = NormalizeFileId(EditorGroupId.Text?.Trim() ?? "");
            string id = !string.IsNullOrEmpty(editId) ? editId : (gid ?? "");
            if (string.IsNullOrEmpty(id)) return;
            string name = EditorGroupName.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(name))
            {
                name = id;
                EditorGroupName.Text = name;
                LogBookmark($"Group loc: empty name for '{id}', using id as fallback");
            }
            if (name == _editorSavedGroupName && _groupsMerged.TryGetValue(id, out _)) { LogBookmark($"Group loc: unchanged '{id}'"); return; }
            LogBookmark($"Group loc: start id='{id}' name='{name}'");
            GroupEditorStatusText.Text = Res("BookmarksTab_EditorLocTranslating");
            try
            {
                string appLang = _viewModel?.Language ?? "en";
                bool autoTranslate = _viewModel?.AutoTranslate ?? true;
                string srcCode = appLang.ToLowerInvariant() switch { "es" => "es", "en" => "en", _ => "ca" };
                var providers = autoTranslate ? BuildBookmarkProviders() : Array.Empty<PdxModIDE.UI.Translation.ITranslationProvider>();
                string? directFolder = appLang switch { "es" => "spanish", "en" => "english", _ => null };
                List<(string Folder, string Code)> targets = autoTranslate
                    ? GameSupportedLanguages.Select(f => (f.Folder, f.Code)).ToList()
                    : (directFolder != null ? new List<(string, string)> { (directFolder, srcCode) } : new List<(string, string)>());
                int saved = 0;
                foreach (var (folder, code) in targets)
                {
                    string loc = name;
                    if (autoTranslate && folder != directFolder)
                    {
                        var (tr, ok) = await TranslateBookmarkAsync(name, srcCode, code, providers);
                        if (!string.IsNullOrEmpty(tr)) loc = tr;
                    }
                    // bookmark groups live in `localization/replace/<lang>/` in the mod (vanilla keys overridden)
                    string dir = System.IO.Path.Combine(modRoot, "localization", "replace", folder);
                    try
                    {
                        System.IO.Directory.CreateDirectory(dir);
                        string file = System.IO.Path.Combine(dir, $"bookmarks_l_{folder}.yml");
                        UpsertSimpleLoc(file, $"l_{folder}:", id, loc);
                        saved++;
                        LogBookmark($"Group loc: wrote '{file}' key='{id}'");
                    }
                    catch (Exception ex) { LogBookmark($"Group loc EX '{folder}': {ex}"); }
                }
                GroupEditorStatusText.Text = $"{string.Format(Res("BookmarksTab_EditorSaved"), id)} ({saved})";
                _editorSavedGroupName = name;
                LogBookmark($"Group loc: done id='{id}' saved={saved}");
                LoadBookmarks();
            }
            catch (Exception ex) { GroupEditorStatusText.Text = $"{Res("BookmarksTab_EditorLocError")}: {ex.Message}"; }
        }

        private static void UpsertSimpleLoc(string filePath, string header, string key, string value)
        {
            var sb = new System.Text.StringBuilder();
            if (!System.IO.File.Exists(filePath))
            {
                sb.AppendLine(header); sb.AppendLine();
                sb.AppendLine($"{key}:0 \"{value.Replace("\"", "\\\"")}\"");
                System.IO.File.WriteAllText(filePath, sb.ToString(), new System.Text.UTF8Encoding(true));
                return;
            }
            var lines = new List<string>(System.IO.File.ReadAllLines(filePath));
            bool found = false;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].TrimStart().StartsWith(key + ":", StringComparison.Ordinal))
                {
                    lines[i] = $"{key}:0 \"{value.Replace("\"", "\\\"")}\"";
                    found = true; break;
                }
            }
            if (!found)
            {
                int at = 1;
                if (at < lines.Count && string.IsNullOrWhiteSpace(lines[at])) at++;
                lines.Insert(at, $"{key}:0 \"{value.Replace("\"", "\\\"")}\"");
            }
            System.IO.File.WriteAllLines(filePath, lines, new System.Text.UTF8Encoding(true));
        }

        private bool EditorSave_Click(object sender, RoutedEventArgs e)
        {
            var status = _editorType == "Group" ? GroupEditorStatusText : BookmarkEditorStatusText;
            if (_viewModel?.CurrentProfile == null) { status.Text = Res("BookmarksTab_EditorNoModRoot"); LogBookmark("EditorSave: no profile"); return false; }
            var modRoot = _viewModel.CurrentProfile.ModRoot;
            if (string.IsNullOrEmpty(modRoot)) { status.Text = Res("BookmarksTab_EditorNoModRoot"); LogBookmark("EditorSave: empty modRoot"); return false; }

            if (_editorType == "Group")
            {
                string idRaw = EditorGroupId.Text?.Trim() ?? "";
                string id = NormalizeFileId(idRaw);
                if (string.IsNullOrEmpty(id)) { status.Text = string.Format(Res("BookmarksTab_EditorFieldRequired"), Res("BookmarksTab_EditorGroupId")); LogBookmark($"Group save: empty id (raw='{idRaw}')"); return false; }
                if (!_idRegex.IsMatch(id)) { status.Text = Res("BookmarksTab_EditorIdInvalid"); LogBookmark($"Group save: invalid id '{id}'"); return false; }
                EditorGroupId.Text = id;
                string groupName = EditorGroupName.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(groupName)) { status.Text = string.Format(Res("BookmarksTab_EditorFieldRequired"), Res("BookmarksTab_EditorGroupName")); LogBookmark($"Group save: empty name for '{id}'"); return false; }
                string date = EditorGroupDate.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(date)) { status.Text = string.Format(Res("BookmarksTab_EditorFieldRequired"), Res("BookmarksTab_EditorGroupDefaultDate")); LogBookmark($"Group save: empty date for '{id}'"); return false; }
                if (!_dateRegex.IsMatch(date)) { status.Text = Res("BookmarksTab_EditorDateInvalid"); LogBookmark($"Group save: invalid date '{date}'"); return false; }
                int offset = _viewModel?.CurrentProfile?.YearOffset ?? 0;
                string fileDate = string.IsNullOrEmpty(date) ? date : (BookmarkLoader.ShiftDate(date, offset) ?? date);
                string block = string.IsNullOrEmpty(date) ? BookmarkLoader.BuildGroupBlock(id, date) : BookmarkLoader.BuildGroupBlockWithOffset(id, date, offset);
                string fileName = (_viewModel?.BookmarkGroupFileName ?? "00_bookmark_groups.txt").Trim();
                if (string.IsNullOrEmpty(fileName)) fileName = "00_bookmark_groups.txt";
                string folder = System.IO.Path.Combine(modRoot, "common", "bookmarks", "groups");
                try { System.IO.Directory.CreateDirectory(folder); }
                catch (Exception ex) { status.Text = $"{Res("BookmarksTab_EditorSaveError")}: {ex.Message}"; LogBookmark($"Group save: mkdir fail '{folder}': {ex}"); return false; }
                string targetFile = System.IO.Path.Combine(folder, fileName);
                LogBookmark($"Group save: id='{id}' date='{date}' fileDate='{fileDate}' offset={offset} isNew={_editorIsNew} hasEditor={_editorGroup != null} folder='{folder}' target='{targetFile}'");
                try
                {
                    if (_editorIsNew)
                    {
                        if (_bookmarksMerged.ContainsKey(id) || _groupsMerged.ContainsKey(id)) { status.Text = string.Format(Res("BookmarksTab_EditorExists"), id); LogBookmark($"Group save: duplicate '{id}'"); return false; }
                        if (!string.IsNullOrEmpty(fileDate) && System.IO.File.Exists(targetFile)) BookmarkLoader.InsertGroupChronologically(targetFile, id, block, fileDate);
                        else System.IO.File.WriteAllText(targetFile, block, new System.Text.UTF8Encoding(true));
                        status.Text = string.Format(Res("BookmarksTab_EditorSaved"), targetFile);
                        LogBookmark($"Group save: wrote new '{targetFile}'");
                        LoadBookmarks();
                        _editorIsNew = false;
                        if (_groupsMerged.TryGetValue(id, out var ngNew)) { _editorGroup = ngNew; }
                        UpdateEditorModeUi();
                        LogBookmark($"Group save: done ok new '{targetFile}'");
                        return true;
                    }
                    else
                    {
                        // No editor target (e.g. direct "Nuevo grupo" tab use) -> treat as new insert
                        if (_editorGroup == null)
                        {
                            if (_bookmarksMerged.ContainsKey(id) || _groupsMerged.ContainsKey(id)) { status.Text = string.Format(Res("BookmarksTab_EditorExists"), id); LogBookmark($"Group save: duplicate '{id}'"); return false; }
                            if (!string.IsNullOrEmpty(fileDate) && System.IO.File.Exists(targetFile)) BookmarkLoader.InsertGroupChronologically(targetFile, id, block, fileDate);
                            else System.IO.File.WriteAllText(targetFile, block, new System.Text.UTF8Encoding(true));
                            status.Text = string.Format(Res("BookmarksTab_EditorSaved"), targetFile);
                            LogBookmark($"Group save: wrote (null editor) '{targetFile}'");
                            LoadBookmarks();
                            _editorIsNew = false;
                            if (_groupsMerged.TryGetValue(id, out var ngNull)) { _editorGroup = ngNull; }
                            UpdateEditorModeUi();
                            LogBookmark($"Group save: done ok null-editor '{targetFile}'");
                            return true;
                        }
                        else
                        {
                        string file = _editorGroup?.SourceFile ?? targetFile;
                        if (!System.IO.File.Exists(file)) file = targetFile;
                        // if date changed, reorder chronologically
                        string oldDate = _editorGroup?.DefaultStartDate ?? "";
                        if (!string.Equals(oldDate, fileDate, StringComparison.OrdinalIgnoreCase) && !string.Equals(_editorGroup!.RawKey, id, StringComparison.OrdinalIgnoreCase))
                        {
                            // id changed -> treat as delete+insert
                            BookmarkLoader.DeleteGroupBlockFromFile(file, _editorGroup!.RawKey);
                            if (BookmarkLoader.CountGroupBlocks(file) == 0 && System.IO.File.Exists(file)) System.IO.File.Delete(file);
                            if (System.IO.File.Exists(targetFile)) BookmarkLoader.InsertGroupChronologically(targetFile, id, block, fileDate);
                            else System.IO.File.WriteAllText(targetFile, block, new System.Text.UTF8Encoding(true));
                        }
                        else if (!string.Equals(oldDate, fileDate, StringComparison.OrdinalIgnoreCase))
                        {
                            BookmarkLoader.DeleteGroupBlockFromFile(file, _editorGroup!.RawKey);
                            string dest = System.IO.File.Exists(targetFile) ? targetFile : file;
                            if (System.IO.File.Exists(dest)) BookmarkLoader.InsertGroupChronologically(dest, id, block, fileDate);
                            else System.IO.File.WriteAllText(dest, block, new System.Text.UTF8Encoding(true));
                            if (BookmarkLoader.CountGroupBlocks(file) == 0 && System.IO.File.Exists(file) && !string.Equals(file, dest, StringComparison.OrdinalIgnoreCase)) System.IO.File.Delete(file);
                        }
                        else
                        {
                            BookmarkLoader.ReplaceBlockInFile(file, _editorGroup!.RawKey, block);
                        }
                        status.Text = string.Format(Res("BookmarksTab_EditorSaved"), file);
                    }
                    LoadBookmarks();
                    _editorIsNew = false;
                    if (_groupsMerged.TryGetValue(id, out var ng)) { _editorGroup = ng; }
                    UpdateEditorModeUi();
                    LogBookmark($"Group save: done ok '{targetFile}'");
                    return true;
                    }
                }
                catch (Exception ex) { status.Text = $"{Res("BookmarksTab_EditorSaveError")}: {ex.Message}"; LogBookmark($"Group save EX: {ex}"); return false; }
            }
            else
            {
                string id = EditorBookmarkId.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(id)) { status.Text = string.Format(Res("BookmarksTab_EditorFieldRequired"), Res("BookmarksTab_EditorBookmarkId")); return false; }
                if (!_idRegex.IsMatch(id)) { status.Text = Res("BookmarksTab_EditorIdInvalid"); return false; }
                string startDate = EditorBookmarkStartDate.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(startDate)) { status.Text = string.Format(Res("BookmarksTab_EditorFieldRequired"), Res("BookmarksTab_EditorBookmarkStartDate")); return false; }
                if (!_dateRegex.IsMatch(startDate)) { status.Text = Res("BookmarksTab_EditorDateInvalid"); return false; }
                string group = EditorBookmarkGroup.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(group)) { status.Text = string.Format(Res("BookmarksTab_EditorFieldRequired"), Res("BookmarksTab_EditorBookmarkGroup")); return false; }
                string isPlayable = EditorIsPlayable.IsChecked == true ? "yes" : "no";
                string recommended = EditorRecommended.IsChecked == true ? "yes" : "no";
                string dlc = EditorRequiresDlc.Text?.Trim() ?? "";
                string charName = EditorCharName.Text?.Trim() ?? "";
                string charHistoryId = EditorCharHistoryId.Text?.Trim() ?? "";
                string charTitle = EditorCharTitle.Text?.Trim() ?? "";
                string charCulture = EditorCharCulture.Text?.Trim() ?? "";
                string charReligion = EditorCharReligion.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(charName)) { status.Text = string.Format(Res("BookmarksTab_EditorFieldRequired"), Res("BookmarksTab_EditorCharName")); return false; }
                if (string.IsNullOrEmpty(charHistoryId)) { status.Text = string.Format(Res("BookmarksTab_EditorFieldRequired"), Res("BookmarksTab_EditorCharHistoryId")); return false; }
                if (string.IsNullOrEmpty(charTitle)) { status.Text = string.Format(Res("BookmarksTab_EditorFieldRequired"), Res("BookmarksTab_EditorCharTitle")); return false; }
                if (string.IsNullOrEmpty(charCulture)) { status.Text = string.Format(Res("BookmarksTab_EditorFieldRequired"), Res("BookmarksTab_EditorCharCulture")); return false; }
                if (string.IsNullOrEmpty(charReligion)) { status.Text = string.Format(Res("BookmarksTab_EditorFieldRequired"), Res("BookmarksTab_EditorCharReligion")); return false; }
                var ch = new BookmarkCharacter
                {
                    NameKey = charName,
                    HistoryId = charHistoryId,
                    Title = charTitle,
                    Culture = charCulture,
                    Religion = charReligion,
                };
                var chars = new List<BookmarkCharacter> { ch };
                string block = BookmarkLoader.BuildBookmarkBlock(id, startDate, group, isPlayable, recommended, dlc, "", chars);
                string fileName = _viewModel?.BookmarkFileName ?? "00_bookmarks.txt";
                string folder = System.IO.Path.Combine(modRoot, "common", "bookmarks", "bookmarks");
                System.IO.Directory.CreateDirectory(folder);
                string targetFile = System.IO.Path.Combine(folder, fileName);
                try
                {
                    if (_editorIsNew)
                    {
                        if (_bookmarksMerged.ContainsKey(id) || _groupsMerged.ContainsKey(id)) { status.Text = string.Format(Res("BookmarksTab_EditorExists"), id); return false; }
                        if (!string.IsNullOrEmpty(group) && !_groupsMerged.ContainsKey(group)) { status.Text = string.Format(Res("BookmarksTab_EditorGroupNotFound"), group); return false; }
                        if (System.IO.File.Exists(targetFile)) BookmarkLoader.InsertBlockAlphabetically(targetFile, id, block);
                        else System.IO.File.WriteAllText(targetFile, block, new System.Text.UTF8Encoding(true));
                        status.Text = string.Format(Res("BookmarksTab_EditorSaved"), id);
                    }
                    else
                    {
                        string file = _editorBookmark?.SourceFile ?? targetFile;
                        if (!System.IO.File.Exists(file)) file = targetFile;
                        BookmarkLoader.ReplaceBlockInFile(file, _editorBookmark!.RawKey, block);
                        status.Text = string.Format(Res("BookmarksTab_EditorSaved"), id);
                    }
                    LoadBookmarks();
                    _editorIsNew = false;
                    if (_bookmarksMerged.TryGetValue(id, out var nb)) _editorBookmark = nb;
                    UpdateEditorModeUi();
                    return true;
                }
                catch (Exception ex) { status.Text = $"{Res("BookmarksTab_EditorSaveError")}: {ex.Message}"; LogBookmark($"Bookmark save EX: {ex}"); return false; }
            }
        }

        private string Res(string key)
        {
            try
            {
                if (System.Windows.Application.Current.TryFindResource(key) is string s) return s;
            }
            catch { }
            return key;
        }
    }
}
