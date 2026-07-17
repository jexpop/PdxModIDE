using System.Collections.Generic;
using System.Windows;

namespace PdxModIDE.UI
{
    public class DiffViewDialog : Window
    {
        public DiffViewDialog(string title, List<string> diffLines)
        {
            Title = $"Diferencias — {title}";
            Width = 1000;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            SetResourceReference(BackgroundProperty, "WindowBackground");

            var scroll = new System.Windows.Controls.ScrollViewer
            {
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto
            };

            var textBlock = new System.Windows.Controls.TextBlock
            {
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12
            };

            var app = System.Windows.Application.Current;
            var addedBrush = app.TryFindResource("StatusGreen") as System.Windows.Media.SolidColorBrush
                ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0x88, 0x00));
            var removedBrush = app.TryFindResource("StatusRed") as System.Windows.Media.SolidColorBrush
                ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xcc, 0x00, 0x00));
            var defaultBrush = app.TryFindResource("WindowForeground") as System.Windows.Media.SolidColorBrush
                ?? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x20, 0x20, 0x20));

            foreach (var line in diffLines)
            {
                var run = new System.Windows.Documents.Run(line + "\n");
                if (line.StartsWith("+") && !line.StartsWith("+++"))
                    run.Foreground = addedBrush;
                else if (line.StartsWith("-") && !line.StartsWith("---"))
                    run.Foreground = removedBrush;
                else
                    run.Foreground = defaultBrush;

                textBlock.Inlines.Add(run);
            }

            scroll.Content = textBlock;
            Content = scroll;
        }
    }
}
