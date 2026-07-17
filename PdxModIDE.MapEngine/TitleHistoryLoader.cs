using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PdxModIDE.MapEngine
{
    public class TitleHistory
    {
        public List<(int Year, string Holder)> Holders { get; set; } = new();
        public List<(int Year, string Liege)> Lieges { get; set; } = new();
    }

    public class TitleHistoryLoader
    {
        private static readonly Regex DateRe = new(@"(\d+)\.(\d+)\.(\d+)");
        private static readonly Regex TitleRe = new(@"^\s*([becdk]_[A-Za-z0-9_]+)\s*=\s*\{");
        private static readonly Regex HolderRe = new(@"holder\s*=\s*(\w+)");
        private static readonly Regex LiegeRe = new(@"liege\s*=\s*(\w+)");

        public Dictionary<string, TitleHistory> AllTitles { get; } = new();

        public int LoadAll(string gameRoot)
        {
            AllTitles.Clear();

            string folder = Path.Combine(gameRoot, "history", "titles");
            if (!Directory.Exists(folder))
                return 0;

            foreach (string fname in Directory.EnumerateFiles(folder, "*.txt"))
            {
                var fileData = ParseTitleHistoryFile(fname);
                foreach (var kvp in fileData)
                {
                    if (!AllTitles.ContainsKey(kvp.Key))
                        AllTitles[kvp.Key] = kvp.Value;
                }
            }

            foreach (var info in AllTitles.Values)
            {
                info.Holders.Sort((a, b) => a.Year.CompareTo(b.Year));
                info.Lieges.Sort((a, b) => a.Year.CompareTo(b.Year));
            }

            return AllTitles.Count;
        }

        public static Dictionary<string, TitleHistory> ParseTitleHistoryFile(string path)
        {
            var data = new Dictionary<string, TitleHistory>();
            string? currentTitle = null;
            int? currentYear = null;
            int stack = 0;

            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();

                var m = TitleRe.Match(line);
                if (m.Success)
                {
                    currentTitle = m.Groups[1].Value;
                    data[currentTitle] = new TitleHistory();
                    stack = 1;
                    currentYear = null;
                    continue;
                }

                if (line.Contains("{"))
                    stack += line.Count(c => c == '{');
                if (line.Contains("}"))
                {
                    stack -= line.Count(c => c == '}');
                    if (stack <= 0)
                    {
                        currentTitle = null;
                        currentYear = null;
                    }
                    continue;
                }

                if (currentTitle == null)
                    continue;

                var dm = DateRe.Match(line);
                if (dm.Success)
                {
                    currentYear = int.Parse(dm.Groups[1].Value);
                    continue;
                }

                var hm = HolderRe.Match(line);
                if (hm.Success && currentYear.HasValue)
                {
                    data[currentTitle].Holders.Add((currentYear.Value, hm.Groups[1].Value));
                    continue;
                }

                var lm = LiegeRe.Match(line);
                if (lm.Success && currentYear.HasValue)
                {
                    data[currentTitle].Lieges.Add((currentYear.Value, lm.Groups[1].Value));
                    continue;
                }
            }

            return data;
        }

        public static string? GetHolderAtYear(TitleHistory history, int year)
        {
            string? last = null;
            foreach (var (y, h) in history.Holders)
            {
                if (y <= year)
                    last = h;
                else
                    break;
            }
            return last;
        }

        public static string? GetLiegeAtYear(TitleHistory history, int year)
        {
            string? last = null;
            foreach (var (y, l) in history.Lieges)
            {
                if (y <= year)
                    last = l;
                else
                    break;
            }
            return last;
        }
    }
}
