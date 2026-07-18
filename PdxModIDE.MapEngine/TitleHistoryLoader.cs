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

            foreach (string fname in Directory.EnumerateFiles(folder, "*.txt", SearchOption.AllDirectories))
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
                if (line.Length == 0)
                    continue;

                // Quitar comentarios en línea para evitar falsos positivos en holder=/liege=
                int hashIdx = line.IndexOf('#');
                if (hashIdx >= 0)
                {
                    line = line.Substring(0, hashIdx).TrimEnd();
                    if (line.Length == 0)
                        continue;
                }

                var m = TitleRe.Match(line);
                if (m.Success)
                {
                    currentTitle = m.Groups[1].Value;
                    if (!data.ContainsKey(currentTitle))
                        data[currentTitle] = new TitleHistory();
                    stack = 1;
                    currentYear = null;
                    continue;
                }

                if (currentTitle == null)
                    continue;

                // Extraer fecha y datos de holder/liege ANTES de contabilizar llaves,
                // porque bloques de fecha "todo en una línea" (muy habituales en baronías
                // y condados) contienen tanto '{' como '}' en la misma línea.
                var dm = DateRe.Match(line);
                if (dm.Success)
                    currentYear = int.Parse(dm.Groups[1].Value);

                if (currentYear.HasValue)
                {
                    var hm = HolderRe.Match(line);
                    if (hm.Success)
                        data[currentTitle].Holders.Add((currentYear.Value, hm.Groups[1].Value));

                    var lm = LiegeRe.Match(line);
                    if (lm.Success)
                        data[currentTitle].Lieges.Add((currentYear.Value, lm.Groups[1].Value));
                }

                int opens = line.Count(c => c == '{');
                int closes = line.Count(c => c == '}');
                if (opens != closes)
                {
                    stack += opens - closes;
                    if (stack <= 0)
                    {
                        currentTitle = null;
                        currentYear = null;
                    }
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
