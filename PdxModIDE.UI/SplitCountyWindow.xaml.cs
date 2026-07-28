using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using PdxModIDE.IO;
using PdxModIDE.MapEngine;

namespace PdxModIDE.UI
{
    public class CountyEntry
    {
        public string ProvinceId { get; set; } = "";
        public string BaronyKey { get; set; } = "";
        public string CountyKey { get; set; } = "";
        public string ParentTitle { get; set; } = "";
    }

    public enum SplitSearchCase { FoundInMod, FoundInLandedTitles, CopiedFromGame }

    public partial class SplitCountyWindow : Window
    {
        private static readonly Regex TitleRegex = new(@"^\s*([becdk]_[A-Za-z0-9_-]+)\s*=\s*\{");
        private readonly string _sourceFilePath = "";
        private readonly string _modRoot = "";
        private readonly string _gameRoot = "";
        private readonly SplitSearchCase _searchCase;
        private readonly MapLoader _mapLoader = null!;
        private string _targetDir = "";
        private readonly ObservableCollection<CountyEntry> _entries = null!;
        private readonly string _countyKey = "";
        private readonly string _parentTitle = "";
        private readonly HashSet<int> _selectedProvinceIds = null!;

        public SplitCountyWindow()
        {
            InitializeComponent();
        }

        public SplitCountyWindow(string title, ObservableCollection<CountyEntry> entries,
            string sourceFilePath, string modRoot, string gameRoot,
            SplitSearchCase searchCase, MapLoader mapLoader,
            string countyKey, string parentTitle, HashSet<int> selectedProvinceIds) : this()
        {
            _sourceFilePath = sourceFilePath;
            _modRoot = modRoot;
            _gameRoot = gameRoot;
            _searchCase = searchCase;
            _mapLoader = mapLoader;
            _entries = entries;
            _countyKey = countyKey;
            _parentTitle = parentTitle;
            _selectedProvinceIds = selectedProvinceIds;

            DataContext = new { SourceFile = sourceFilePath };

            Title = string.Format(
                System.Windows.Application.Current.TryFindResource("SplitCounty_Title") as string ?? "Split County — {0}",
                title);

            CountyList.ItemsSource = entries;

            CapitalCombo.ItemsSource = entries;
            CapitalCombo.DisplayMemberPath = "BaronyKey";
            if (entries.Count > 0)
                CapitalCombo.SelectedIndex = 0;

            switch (_searchCase)
            {
                case SplitSearchCase.FoundInMod:
                    _targetDir = Path.GetDirectoryName(_sourceFilePath) ?? "";
                    break;
                case SplitSearchCase.FoundInLandedTitles:
                    _targetDir = Path.Combine(_modRoot, "common", "landed_titles", "mod");
                    break;
                case SplitSearchCase.CopiedFromGame:
                    _targetDir = Path.Combine(_modRoot, "common", "landed_titles", "mod");
                    break;
            }

            string browseRoot = Path.Combine(_modRoot, "common", "landed_titles", "mod");
            if (_targetDir.StartsWith(browseRoot, StringComparison.OrdinalIgnoreCase))
            {
                string rel = _targetDir.Substring(browseRoot.Length).TrimStart('\\', '/');
                TargetFolderBox.Text = string.IsNullOrEmpty(rel) ? "." : rel;
            }
            else
            {
                TargetFolderBox.Text = _targetDir;
            }

            string firstBarony = entries.Count > 0 ? entries[0].BaronyKey : "";
            if (firstBarony.StartsWith("b_"))
            {
                string suffix = firstBarony.Substring(2);
                TitleKeyBox.Text = "d_" + suffix;
                CountyKeyBox.Text = "c_" + suffix;
            }
            else
            {
                TitleKeyBox.Text = "";
                CountyKeyBox.Text = "";
            }
        }

        private void BtnExecute_Click(object sender, RoutedEventArgs e)
        {
            string newTitleKey = TitleKeyBox.Text.Trim();
            string newCountyKey = CountyKeyBox.Text.Trim();

            if (string.IsNullOrEmpty(newTitleKey) || string.IsNullOrEmpty(newCountyKey))
            {
                ValidationMsg.Text = "Title key and County key are required.";
                return;
            }

            if (!newTitleKey.StartsWith("b_") && !newTitleKey.StartsWith("c_") &&
                !newTitleKey.StartsWith("d_") && !newTitleKey.StartsWith("k_") &&
                !newTitleKey.StartsWith("e_"))
            {
                ValidationMsg.Text = "Title key must start with b_, c_, d_, k_, or e_.";
                return;
            }

            if (!newCountyKey.StartsWith("c_"))
            {
                ValidationMsg.Text = "County key must start with c_.";
                return;
            }

            if (KeyExists(newTitleKey) || KeyExists(newCountyKey))
            {
                ValidationMsg.Text = "Key already exists in the game hierarchy.";
                return;
            }

            if (CapitalCombo.SelectedItem == null)
            {
                ValidationMsg.Text = "Please select a capital.";
                return;
            }

            try
            {
                ExecuteSplit(newTitleKey, newCountyKey);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ValidationMsg.Text = $"Error: {ex.Message}";
            }
        }

        private bool KeyExists(string key)
        {
            if (_mapLoader.BaronyToCounty.ContainsKey(key)) return true;
            if (_mapLoader.CountyToDuchy.ContainsKey(key)) return true;
            if (_mapLoader.DuchyToKingdom.ContainsKey(key)) return true;
            if (_mapLoader.KingdomToEmpire.ContainsKey(key)) return true;
            return false;
        }

        private void ExecuteSplit(string newTitleKey, string newCountyKey)
        {
            var baronyKeys = new HashSet<string>(_entries.Select(e => e.BaronyKey));

            var sourceLines = File.ReadAllLines(_sourceFilePath).ToList();
            int ci = FindBlockStart(sourceLines, _countyKey);
            List<string> countyAttrs;
            List<string[]> splitBlocks;

            if (ci >= 0)
            {
                int ce = FindBlockEnd(sourceLines, ci);
                var (attrs, allBlocks) = ParseBlockChildren(sourceLines, ci, ce);
                countyAttrs = attrs;
                splitBlocks = allBlocks.Where(b =>
                {
                    var m = TitleRegex.Match(b[0]);
                    return m.Success && baronyKeys.Contains(m.Groups[1].Value);
                }).Select(b => b.ToArray()).ToList();
            }
            else
            {
                countyAttrs = new List<string>();
                splitBlocks = new List<string[]>();
            }

            switch (_searchCase)
            {
                case SplitSearchCase.FoundInMod:
                    ProcessFile(_sourceFilePath, _countyKey, baronyKeys, newTitleKey, true);
                    break;

                case SplitSearchCase.FoundInLandedTitles:
                    ProcessFile(_sourceFilePath, _countyKey, baronyKeys, newTitleKey, false);
                    break;

                case SplitSearchCase.CopiedFromGame:
                    string copyDir = Path.Combine(_modRoot, "common", "landed_titles");
                    Directory.CreateDirectory(copyDir);
                    string copyPath = Path.Combine(copyDir, Path.GetFileName(_sourceFilePath));
                    FileOperations.CopyFile(_sourceFilePath, copyPath);
                    ProcessFile(copyPath, _countyKey, baronyKeys, newTitleKey, false);
                    break;

                default:
                    return;
            }

            Directory.CreateDirectory(_targetDir);
            string newFilePath = Path.Combine(_targetDir, $"{newTitleKey}.txt");
            string newContent = BuildNewTitleFile(newTitleKey, newCountyKey, countyAttrs, splitBlocks);
            FileOperations.WriteTextFile(newFilePath, newContent);

            if (_searchCase == SplitSearchCase.FoundInMod || _searchCase == SplitSearchCase.FoundInLandedTitles)
                TryDeleteFileIfEmpty(_sourceFilePath);
        }

        private void ProcessFile(string filePath, string countyKey, HashSet<string> baronyKeys,
            string newTitleKey, bool removeBlocks)
        {
            var lines = File.ReadAllLines(filePath).ToList();

            int ci = FindBlockStart(lines, countyKey);
            if (ci < 0) return;

            int ce = FindBlockEnd(lines, ci);
            var (attrs, blocks) = ParseBlockChildren(lines, ci, ce);

            var newCountyLines = new List<string>();
            newCountyLines.Add(lines[ci]);
            newCountyLines.AddRange(attrs);

            foreach (var block in blocks)
            {
                var m = TitleRegex.Match(block[0]);
                bool isSplit = m.Success && baronyKeys.Contains(m.Groups[1].Value);

                if (removeBlocks && isSplit)
                    continue;

                if (!removeBlocks && isSplit)
                {
                    foreach (var bl in block)
                    {
                        string trimmed = bl.TrimStart();
                        if (trimmed.Length > 0)
                            newCountyLines.Add($"##MOD_DEL {trimmed}");
                        else
                            newCountyLines.Add(bl);
                    }
                    continue;
                }

                newCountyLines.AddRange(block);
            }

            newCountyLines.Add(lines[ce]);

            var result = new List<string>();
            result.AddRange(lines.Take(ci));
            result.AddRange(newCountyLines);
            result.AddRange(lines.Skip(ce + 1));

            File.WriteAllText(filePath, string.Join(Environment.NewLine, result), Encoding.UTF8);
        }

        private static int FindBlockStart(List<string> lines, string key)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                var m = TitleRegex.Match(lines[i]);
                if (m.Success && m.Groups[1].Value == key)
                    return i;
            }
            return -1;
        }

        private static int FindBlockEnd(List<string> lines, int start)
        {
            int depth = 0;
            bool opened = false;
            for (int i = start; i < lines.Count; i++)
            {
                foreach (char c in lines[i])
                {
                    if (c == '{') { depth++; opened = true; }
                    else if (c == '}') depth--;
                }
                if (opened && depth <= 0) return i;
            }
            return lines.Count - 1;
        }

        private static (List<string> Attrs, List<List<string>> ChildBlocks) ParseBlockChildren(
            List<string> lines, int blockStart, int blockEnd)
        {
            var attrs = new List<string>();
            var childBlocks = new List<List<string>>();

            for (int i = blockStart + 1; i < blockEnd; i++)
            {
                var m = TitleRegex.Match(lines[i]);
                if (m.Success)
                {
                    int childEnd = FindBlockEnd(lines, i);
                    var block = new List<string>();
                    for (int j = i; j <= childEnd; j++)
                        block.Add(lines[j]);
                    childBlocks.Add(block);
                    i = childEnd;
                }
                else
                {
                    attrs.Add(lines[i]);
                }
            }

            return (attrs, childBlocks);
        }

        private string BuildNewTitleFile(string newTitleKey, string newCountyKey,
            List<string> countyAttrs, List<string[]> splitBlocks)
        {
            var capEntry = CapitalCombo.SelectedItem as CountyEntry;
            int capitalId = capEntry != null ? int.Parse(capEntry.ProvinceId) : int.Parse(_entries[0].ProvinceId);

            var sb = new StringBuilder();
            sb.AppendLine($"{newTitleKey} = {{ #{_parentTitle}");
            sb.AppendLine($"\t{newCountyKey} = {{ #{_countyKey}");
            sb.AppendLine($"\t\tcapital = {capitalId}");

            foreach (var attr in countyAttrs)
            {
                string t = attr.TrimStart();
                if ((t.StartsWith("capital") && !t.StartsWith("capital_")) || t.Length == 0)
                    continue;
                sb.AppendLine($"\t\t{t}");
            }

            foreach (var block in splitBlocks)
            {
                for (int i = 0; i < block.Length; i++)
                {
                    string t = block[i].TrimStart();
                    sb.AppendLine(i == 0 || i == block.Length - 1 ? $"\t\t{t}" : $"\t\t\t{t}");
                }
            }

            if (splitBlocks.Count == 0)
            {
                sb.AppendLine("\t\tplaceholder = 0");
            }

            sb.AppendLine("\t}");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private void BtnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            string browseRoot = Path.Combine(_modRoot, "common", "landed_titles", "mod");
            Directory.CreateDirectory(browseRoot);

            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = TryFindResource("SplitCounty_BrowseTitle") as string ?? "Select target folder";
            dialog.SelectedPath = browseRoot;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _targetDir = dialog.SelectedPath;
                string rel = _targetDir;
                if (rel.StartsWith(browseRoot, StringComparison.OrdinalIgnoreCase))
                {
                    rel = rel.Substring(browseRoot.Length).TrimStart('\\', '/');
                    if (string.IsNullOrEmpty(rel)) rel = ".";
                }
                TargetFolderBox.Text = rel;
            }
        }

        private string? TryFindResource(string key)
        {
            return System.Windows.Application.Current.TryFindResource(key) as string;
        }

        private void TryDeleteFileIfEmpty(string filePath)
        {
            try
            {
                string content = File.ReadAllText(filePath);
                if (!content.Contains("b_"))
                    File.Delete(filePath);
            }
            catch { }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
