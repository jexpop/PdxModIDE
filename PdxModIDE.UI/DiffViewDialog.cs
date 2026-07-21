using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using SWM = System.Windows.Media;

namespace PdxModIDE.UI
{
    public enum DiffLineStatus { Same, Added, Removed, Modified }

    public class DiffLinePair
    {
        public int? LeftLineNumber { get; set; }
        public int? RightLineNumber { get; set; }
        public string? LeftText { get; set; }
        public string? RightText { get; set; }
        public DiffLineStatus Status { get; set; }
    }

    public class DiffViewDialog : Window
    {
        private const double ColNumWidth = 52;
        private const double ColDividerWidth = 2;
        private const double RowHeight = 20;

        public DiffViewDialog(string title, System.Collections.Generic.List<string> diffLines)
        {
            Title = $"Diferencias — {title}";
            Width = 1200;
            Height = 600;
            MinWidth = 600;
            MinHeight = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            SetResourceReference(BackgroundProperty, "WindowBackground");

            if (diffLines == null || diffLines.Count == 0)
            {
                Content = new System.Windows.Controls.TextBlock
                {
                    Text = "No hay diferencias.",
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    FontSize = 16
                };
                return;
            }

            var app = System.Windows.Application.Current;
            var defaultFg = (SWM.Brush)(app.TryFindResource("WindowForeground")
                ?? new SolidColorBrush(SWM.Color.FromRgb(0x20, 0x20, 0x20)));
            var addedBg = (SWM.Brush)(app.TryFindResource("DiffAddedBackground")
                ?? new SolidColorBrush(SWM.Color.FromRgb(0xDD, 0xFF, 0xDD)));
            var removedBg = (SWM.Brush)(app.TryFindResource("DiffRemovedBackground")
                ?? new SolidColorBrush(SWM.Color.FromRgb(0xFF, 0xDD, 0xDD)));
            var lineNumBrush = new SolidColorBrush(SWM.Color.FromArgb(0x80, 0x80, 0x80, 0x80));
            var borderBrush = (SWM.Brush)(app.TryFindResource("ControlBorder")
                ?? new SolidColorBrush(SWM.Color.FromRgb(0xCC, 0xCC, 0xCC)));
            var headerBg = new SolidColorBrush(SWM.Color.FromArgb(0x18, 0x00, 0x00, 0x00));
            var font = new SWM.FontFamily("Consolas");

            var pairs = ParseDiffLines(diffLines);

            var scroll = new System.Windows.Controls.ScrollViewer
            {
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Visible,
                HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto
            };

            var table = new System.Windows.Controls.Grid();
            table.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(ColNumWidth) });
            table.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            table.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(ColDividerWidth) });
            table.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(ColNumWidth) });
            table.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });

            for (int i = 0; i < pairs.Count; i++)
            {
                table.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(RowHeight) });
                var p = pairs[i];

                var leftBg = p.Status == DiffLineStatus.Removed || p.Status == DiffLineStatus.Modified ? removedBg : SWM.Brushes.Transparent;
                var rightBg = p.Status == DiffLineStatus.Added || p.Status == DiffLineStatus.Modified ? addedBg : SWM.Brushes.Transparent;

                var leftNum = new System.Windows.Controls.TextBlock
                {
                    Text = p.LeftLineNumber?.ToString() ?? "",
                    Foreground = lineNumBrush,
                    FontFamily = font,
                    FontSize = 12,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Margin = new System.Windows.Thickness(0, 0, 4, 0)
                };
                System.Windows.Controls.Grid.SetRow(leftNum, i);
                System.Windows.Controls.Grid.SetColumn(leftNum, 0);
                table.Children.Add(leftNum);

                var leftBorder = new System.Windows.Controls.Border { Background = leftBg, Child = new System.Windows.Controls.TextBlock
                {
                    Text = p.LeftText ?? "",
                    Foreground = defaultFg,
                    FontFamily = font,
                    FontSize = 12,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center
                }};
                System.Windows.Controls.Grid.SetRow(leftBorder, i);
                System.Windows.Controls.Grid.SetColumn(leftBorder, 1);
                table.Children.Add(leftBorder);

                var rowDivider = new System.Windows.Shapes.Rectangle
                {
                    Fill = borderBrush,
                    Width = ColDividerWidth,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    IsHitTestVisible = false
                };
                System.Windows.Controls.Grid.SetRow(rowDivider, i);
                System.Windows.Controls.Grid.SetColumn(rowDivider, 2);
                table.Children.Add(rowDivider);

                var rightNum = new System.Windows.Controls.TextBlock
                {
                    Text = p.RightLineNumber?.ToString() ?? "",
                    Foreground = lineNumBrush,
                    FontFamily = font,
                    FontSize = 12,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Margin = new System.Windows.Thickness(0, 0, 4, 0)
                };
                System.Windows.Controls.Grid.SetRow(rightNum, i);
                System.Windows.Controls.Grid.SetColumn(rightNum, 3);
                table.Children.Add(rightNum);

                var rightBorder = new System.Windows.Controls.Border { Background = rightBg, Child = new System.Windows.Controls.TextBlock
                {
                    Text = p.RightText ?? "",
                    Foreground = defaultFg,
                    FontFamily = font,
                    FontSize = 12,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center
                }};
                System.Windows.Controls.Grid.SetRow(rightBorder, i);
                System.Windows.Controls.Grid.SetColumn(rightBorder, 4);
                table.Children.Add(rightBorder);
            }

            scroll.Content = table;

            var outerGrid = new System.Windows.Controls.Grid();
            outerGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
            outerGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            outerGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });

            var header = new System.Windows.Controls.Border
            {
                Background = headerBg,
                Padding = new System.Windows.Thickness(8, 4, 8, 4),
                BorderBrush = borderBrush,
                BorderThickness = new System.Windows.Thickness(0, 0, 0, 1),
                Child = new System.Windows.Controls.Grid
                {
                    ColumnDefinitions =
                    {
                        new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) },
                        new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(ColDividerWidth) },
                        new System.Windows.Controls.ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) }
                    },
                    Children =
                    {
                        new System.Windows.Controls.TextBlock
                        {
                            Text = "Original",
                            FontWeight = System.Windows.FontWeights.Bold,
                            FontSize = 13,
                            Foreground = defaultFg,
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                        },
                        new System.Windows.Shapes.Rectangle
                        {
                            Fill = borderBrush,
                            Width = ColDividerWidth,
                            VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                            IsHitTestVisible = false
                        },
                        new System.Windows.Controls.TextBlock
                        {
                            Text = "Modified",
                            FontWeight = System.Windows.FontWeights.Bold,
                            FontSize = 13,
                            Foreground = defaultFg,
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                        }
                    }
                }
            };
            foreach (var child in ((System.Windows.Controls.Grid)header.Child).Children)
            {
                if (child is System.Windows.Controls.TextBlock tb)
                {
                    // set column for header textblocks
                }
            }
            // Set columns for header children
            var headerGrid = (System.Windows.Controls.Grid)header.Child;
            System.Windows.Controls.Grid.SetColumn(headerGrid.Children[0], 0);
            System.Windows.Controls.Grid.SetColumn(headerGrid.Children[1], 1);
            System.Windows.Controls.Grid.SetColumn(headerGrid.Children[2], 2);

            System.Windows.Controls.Grid.SetRow(header, 0);
            System.Windows.Controls.Grid.SetRow(scroll, 1);
            outerGrid.Children.Add(header);
            outerGrid.Children.Add(scroll);

            Content = outerGrid;
        }

        private static System.Collections.Generic.List<DiffLinePair> ParseDiffLines(System.Collections.Generic.List<string> diffLines)
        {
            var pairs = new System.Collections.Generic.List<DiffLinePair>();
            int leftNum = 0;
            int rightNum = 0;

            for (int idx = 0; idx < diffLines.Count; idx++)
            {
                var line = diffLines[idx];

                if (line.StartsWith("---") || line.StartsWith("+++"))
                    continue;

                if (line.StartsWith(" "))
                {
                    leftNum++;
                    rightNum++;
                    pairs.Add(new DiffLinePair
                    {
                        LeftLineNumber = leftNum,
                        RightLineNumber = rightNum,
                        LeftText = line.Substring(1),
                        RightText = line.Substring(1),
                        Status = DiffLineStatus.Same
                    });
                }
                else if (line.StartsWith("-"))
                {
                    bool hasNextAddition = idx + 1 < diffLines.Count
                        && diffLines[idx + 1].StartsWith("+")
                        && !diffLines[idx + 1].StartsWith("+++");

                    if (hasNextAddition)
                    {
                        leftNum++;
                        rightNum++;
                        pairs.Add(new DiffLinePair
                        {
                            LeftLineNumber = leftNum,
                            RightLineNumber = rightNum,
                            LeftText = line.Substring(1),
                            RightText = diffLines[idx + 1].Substring(1),
                            Status = DiffLineStatus.Modified
                        });
                        idx++;
                    }
                    else
                    {
                        leftNum++;
                        pairs.Add(new DiffLinePair
                        {
                            LeftLineNumber = leftNum,
                            RightLineNumber = null,
                            LeftText = line.Substring(1),
                            RightText = null,
                            Status = DiffLineStatus.Removed
                        });
                    }
                }
                else if (line.StartsWith("+"))
                {
                    rightNum++;
                    pairs.Add(new DiffLinePair
                    {
                        LeftLineNumber = null,
                        RightLineNumber = rightNum,
                        LeftText = null,
                        RightText = line.Substring(1),
                        Status = DiffLineStatus.Added
                    });
                }
            }

            return pairs;
        }
    }
}
