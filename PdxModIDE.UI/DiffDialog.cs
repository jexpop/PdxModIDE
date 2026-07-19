using System.Collections.Generic;
using System.Linq;
using System.Windows;
using PdxModIDE.Validation;

namespace PdxModIDE.UI
{
    public class DiffDialog : Window
    {
        public DiffDialog(string moduleName, List<FileComparisonResult> modVsBackup, List<FileComparisonResult> gameVsBackup)
        {
            Title = $"Detalles — {moduleName}";
            Width = 1000;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var tabControl = new System.Windows.Controls.TabControl();
            tabControl.Items.Add(CreateFileTab("MOD ↔ Backup", modVsBackup));
            tabControl.Items.Add(CreateFileTab("Juego ↔ Backup", gameVsBackup));
            Content = tabControl;
        }

        private static System.Windows.Controls.TabItem CreateFileTab(string header, List<FileComparisonResult> results)
        {
            var tree = new System.Windows.Controls.TreeView();
            foreach (var r in results.Where(r => r.Status is "Modified" or "Added" or "Deleted"))
            {
                var item = new System.Windows.Controls.TreeViewItem
                {
                    Header = $"[{r.Status}] {r.RelativePath}",
                    Tag = r.DiffLines
                };
                var app = System.Windows.Application.Current;
                item.Foreground = r.Status switch
                {
                    "Modified" => (System.Windows.Media.SolidColorBrush)(app.TryFindResource("StatusBlue")
                        ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xd1, 0x7b, 0x00))),
                    "Added" => (System.Windows.Media.SolidColorBrush)(app.TryFindResource("StatusGreen")
                        ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green)),
                    "Deleted" => (System.Windows.Media.SolidColorBrush)(app.TryFindResource("StatusRed")
                        ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red)),
                    _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray)
                };
                item.MouseDoubleClick += (s, args) =>
                {
                    if (s is System.Windows.Controls.TreeViewItem tvi && tvi.Tag is List<string> diff && diff.Count > 0)
                    {
                        var dlg = new DiffViewDialog(header, diff);
                        dlg.Owner = System.Windows.Application.Current.MainWindow;
                        dlg.ShowDialog();
                    }
                };
                tree.Items.Add(item);
            }

            var tab = new System.Windows.Controls.TabItem { Header = header };
            tab.Content = new System.Windows.Controls.ScrollViewer
            {
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                Content = tree
            };
            return tab;
        }
    }
}
