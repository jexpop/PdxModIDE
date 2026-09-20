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
        private Dictionary<string, BookmarkGroupInfo> _groupsMerged = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, BookmarkInfo> _bookmarksMerged = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, BookmarkGroupInfo> _groupsBase = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, BookmarkInfo> _bookmarksBase = new(StringComparer.OrdinalIgnoreCase);
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
            LoadBookmarks();
            if (_viewModel != null)
                _viewModel.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(MainViewModel.CurrentProfile))
                        Dispatcher.BeginInvoke(new Action(LoadBookmarks));
                };
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

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadBookmarks();

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
            GroupDefaultStartDateValue.Text = string.IsNullOrEmpty(g.DefaultStartDate) ? "-" : g.DefaultStartDate;
            GroupRawValue.Text = string.IsNullOrEmpty(g.RawBlock) ? "-" : g.RawBlock.Trim();
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
