using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using PdxModIDE.UI.ViewModels;

namespace PdxModIDE.UI
{
    public partial class LogsTab : System.Windows.Controls.UserControl
    {
        private MainViewModel? _viewModel;
        private List<string> _filters = new();

        private static readonly string FilterFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "log_filters.json");

        public LogsTab()
        {
            InitializeComponent();
            Loaded += OnLoaded;

            BtnReload.Click += BtnReload_Click;
            BtnApply.Click += BtnApply_Click;
            BtnAddFilter.Click += BtnAddFilter_Click;
            BtnDeleteFilter.Click += BtnDeleteFilter_Click;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as MainViewModel;
            _filters = LoadFilters();
            RefreshFilterList();
        }

        private List<string> LoadFilters()
        {
            try
            {
                if (File.Exists(FilterFile))
                {
                    var json = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(File.ReadAllText(FilterFile));
                    return json?.GetValueOrDefault("filters", new List<string>()) ?? new List<string>();
                }
            }
            catch { }
            return new List<string>();
        }

        private void SaveFilters()
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(FilterFile);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var json = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object> { ["filters"] = _filters });
                File.WriteAllText(FilterFile, json);
            }
            catch { }
        }

        private void RefreshFilterList()
        {
            FilterList.Items.Clear();
            foreach (var f in _filters)
                FilterList.Items.Add(f);
        }

        private string? GetErrorLogPath()
        {
            var profile = _viewModel?.CurrentProfile;
            if (profile == null || string.IsNullOrEmpty(profile.ModRoot)) return null;

            var baseDir = System.IO.Path.GetDirectoryName(profile.ModRoot);
            if (baseDir == null) return null;
            baseDir = System.IO.Path.GetDirectoryName(baseDir);
            if (baseDir == null) return null;

            return System.IO.Path.Combine(baseDir, "logs", "error.log");
        }

        private void LoadLog(string? extraFilter = null)
        {
            LogText.Document = new FlowDocument();
            var logPath = GetErrorLogPath();

            if (logPath == null || !File.Exists(logPath))
            {
                LogText.AppendText(Res("LogsTab_NoLog"));
                return;
            }

            var paragraph = new Paragraph();

            foreach (var line in File.ReadLines(logPath))
            {
                if (_filters.Any(f => line.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0))
                    continue;

                if (!string.IsNullOrEmpty(extraFilter) && line.IndexOf(extraFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var run = new Run(line + "\n");
                var upper = line.ToUpperInvariant();
                if (upper.Contains("ERROR"))
                    run.Foreground = new SolidColorBrush(Colors.Red);
                else if (upper.Contains("WARN"))
                    run.Foreground = new SolidColorBrush(Colors.Orange);
                else if (upper.Contains("INFO"))
                    run.Foreground = new SolidColorBrush(Colors.Gray);
                else
                    run.Foreground = new SolidColorBrush(Colors.Black);

                paragraph.Inlines.Add(run);
            }

            LogText.Document.Blocks.Add(paragraph);
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            LoadLog();
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            var filter = FilterEntry.Text?.Trim();
            LoadLog(string.IsNullOrEmpty(filter) ? null : filter);
        }

        private void BtnAddFilter_Click(object sender, RoutedEventArgs e)
        {
            var text = NewFilterEntry.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            _filters.Add(text);
            SaveFilters();
            RefreshFilterList();
            NewFilterEntry.Clear();
        }

        private void BtnDeleteFilter_Click(object sender, RoutedEventArgs e)
        {
            if (FilterList.SelectedItem is string item)
            {
                _filters.Remove(item);
                SaveFilters();
                RefreshFilterList();
            }
        }

        private static string Res(string key)
        {
            return System.Windows.Application.Current.TryFindResource(key) as string ?? key;
        }
    }
}
