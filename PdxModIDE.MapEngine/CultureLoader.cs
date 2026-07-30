using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PdxModIDE.MapEngine
{
    public class CultureInfo
    {
        public string Name { get; set; } = "";
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
    }

    public class CultureLoader
    {
        private static readonly Regex BlockRe = new(@"^\s*(\w[\w.]*)\s*=\s*\{");
        private static readonly Regex NameRe = new(@"name\s*=\s*""([^""]*)""");
        private static readonly Regex ColorFloatRe = new(@"color\s*=\s*\{\s*([\d.]+)\s+([\d.]+)\s+([\d.]+)\s*\}");
        private static readonly Regex ProvinceBlockRe = new(@"^\s*(\d+)\s*=\s*\{");
        private static readonly Regex CultureRe = new(@"culture\s*=\s*(\w+)");
        private static readonly Regex DateRe = new(@"(\d+)\.(\d+)\.(\d+)\s*=\s*\{");

        public Dictionary<string, CultureInfo> AllCultures { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<int, List<(int Year, string Culture)>> ProvinceCultures { get; } = new();

        public int LoadCultures(string gameRoot, bool overwriteDuplicates = false)
        {
            string folder = Path.Combine(gameRoot, "common", "culture", "cultures");
            if (!Directory.Exists(folder))
                return 0;

            foreach (string fname in Directory.EnumerateFiles(folder, "*.txt", SearchOption.AllDirectories))
            {
                var fileData = ParseCultureFile(fname);
                foreach (var kvp in fileData)
                {
                    if (overwriteDuplicates || !AllCultures.ContainsKey(kvp.Key))
                        AllCultures[kvp.Key] = kvp.Value;
                }
            }

            return AllCultures.Count;
        }

        public int LoadProvinceHistory(string gameRoot, bool overwriteDuplicates = false)
        {
            string folder = Path.Combine(gameRoot, "history", "provinces");
            if (!Directory.Exists(folder))
                return 0;

            foreach (string fname in Directory.EnumerateFiles(folder, "*.txt", SearchOption.AllDirectories))
            {
                var fileData = ParseProvinceHistoryFile(fname);
                foreach (var kvp in fileData)
                {
                    if (overwriteDuplicates || !ProvinceCultures.ContainsKey(kvp.Key))
                        ProvinceCultures[kvp.Key] = kvp.Value;
                }
            }

            return ProvinceCultures.Count;
        }

        public static Dictionary<string, CultureInfo> ParseCultureFile(string path)
        {
            var data = new Dictionary<string, CultureInfo>(StringComparer.OrdinalIgnoreCase);
            var keyStack = new List<string>();

            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0)
                    continue;

                int hashIdx = line.IndexOf('#');
                if (hashIdx >= 0)
                {
                    line = line.Substring(0, hashIdx).TrimEnd();
                    if (line.Length == 0)
                        continue;
                }

                int opens = line.Count(c => c == '{');
                int closes = line.Count(c => c == '}');

                var m = BlockRe.Match(line);
                if (m.Success)
                {
                    string key = m.Groups[1].Value;
                    keyStack.Add(key);

                    if (!data.ContainsKey(key))
                    {
                        var info = new CultureInfo();
                        data[key] = info;

                        var nm = NameRe.Match(line);
                        if (nm.Success) info.Name = nm.Groups[1].Value;

                        var cm = ColorFloatRe.Match(line);
                        if (cm.Success)
                            TryParseFloatColor(cm, info);
                    }
                }
                else if (keyStack.Count > 0)
                {
                    var currentKey = keyStack[^1];
                    if (data.TryGetValue(currentKey, out var current))
                    {
                        var nm2 = NameRe.Match(line);
                        if (nm2.Success && string.IsNullOrEmpty(current.Name))
                            current.Name = nm2.Groups[1].Value;

                        var cm2 = ColorFloatRe.Match(line);
                        if (cm2.Success && current.R == 0 && current.G == 0 && current.B == 0)
                            TryParseFloatColor(cm2, current);
                    }
                }

                int netCloses = closes - opens;
                for (int i = 0; i < netCloses && keyStack.Count > 0; i++)
                    keyStack.RemoveAt(keyStack.Count - 1);
            }

            return data;
        }

        private static void TryParseFloatColor(Match cm, PdxModIDE.MapEngine.CultureInfo info)
        {
            if (float.TryParse(cm.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float fr) &&
                float.TryParse(cm.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float fg) &&
                float.TryParse(cm.Groups[3].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float fb))
            {
                if (fr <= 1.0f && fg <= 1.0f && fb <= 1.0f)
                {
                    info.R = (byte)(fr * 255);
                    info.G = (byte)(fg * 255);
                    info.B = (byte)(fb * 255);
                }
                else
                {
                    info.R = (byte)fr;
                    info.G = (byte)fg;
                    info.B = (byte)fb;
                }
            }
        }

        public static Dictionary<int, List<(int Year, string Culture)>> ParseProvinceHistoryFile(string path)
        {
            var data = new Dictionary<int, List<(int Year, string Culture)>>();
            int? currentProvince = null;
            var dateStack = new List<(int Year, int Depth)>();
            int depth = 0;

            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0)
                    continue;

                int hashIdx = line.IndexOf('#');
                if (hashIdx >= 0)
                {
                    line = line.Substring(0, hashIdx).TrimEnd();
                    if (line.Length == 0)
                        continue;
                }

                int opens = line.Count(c => c == '{');
                int closes = line.Count(c => c == '}');

                if (depth == 0)
                {
                    var pm = ProvinceBlockRe.Match(line);
                    if (pm.Success)
                    {
                        currentProvince = int.Parse(pm.Groups[1].Value);
                        depth += opens - closes;

                        var cm = CultureRe.Match(line);
                        if (cm.Success)
                        {
                            if (!data.ContainsKey(currentProvince.Value))
                                data[currentProvince.Value] = new List<(int, string)>();
                            data[currentProvince.Value].Add((0, cm.Groups[1].Value));
                        }

                        if (depth <= 0)
                            currentProvince = null;
                        continue;
                    }
                    depth += opens - closes;
                    continue;
                }

                var dm = DateRe.Match(line);
                if (dm.Success)
                {
                    int year = int.Parse(dm.Groups[1].Value);
                    int entryDepth = depth + opens;
                    dateStack.Add((year, entryDepth));

                    depth += opens - closes;

                    var cm = CultureRe.Match(line);
                    if (cm.Success && currentProvince.HasValue)
                    {
                        if (!data.ContainsKey(currentProvince.Value))
                            data[currentProvince.Value] = new List<(int, string)>();
                        data[currentProvince.Value].Add((year, cm.Groups[1].Value));
                    }

                    while (dateStack.Count > 0 && depth < dateStack[^1].Depth)
                        dateStack.RemoveAt(dateStack.Count - 1);
                    if (depth <= 0)
                        currentProvince = null;
                    continue;
                }

                var cultureMatch = CultureRe.Match(line);
                if (cultureMatch.Success && currentProvince.HasValue)
                {
                    string cultureName = cultureMatch.Groups[1].Value;
                    int year = dateStack.Count > 0 ? dateStack[^1].Year : 0;
                    if (!data.ContainsKey(currentProvince.Value))
                        data[currentProvince.Value] = new List<(int, string)>();
                    data[currentProvince.Value].Add((year, cultureName));
                }

                depth += opens - closes;

                while (dateStack.Count > 0 && depth < dateStack[^1].Depth)
                    dateStack.RemoveAt(dateStack.Count - 1);
                if (depth <= 0)
                    currentProvince = null;
            }

            foreach (var list in data.Values)
                list.Sort((a, b) => a.Year.CompareTo(b.Year));

            return data;
        }

        public static string? GetCultureAtYear(List<(int Year, string Culture)> cultures, int year)
        {
            string? last = null;
            foreach (var (y, c) in cultures)
            {
                if (y <= year)
                    last = c;
                else
                    break;
            }
            return last;
        }

        // =====================================================================
        // HERENCIA DE CULTURA DESDE EL TÍTULO DEL CONDADO (CK3)
        // Si una provincia no tiene culture = en history/provinces/,
        // hereda la cultura del titular del condado al que pertenece.
        // =====================================================================

        public Dictionary<int, string> ProvinceToCounty { get; } = new();

        public Dictionary<string, List<(int Year, int HolderId)>> CountyHolderHistory { get; }
            = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<int, string> CharacterCulture { get; } = new();

        private static readonly Regex _countyBlockRe = new(@"^(c_\w+)\s*=\s*\{");
        private static readonly Regex _dateBlockRe = new(@"(\d+)\.\d+\.\d+\s*=\s*\{");
        private static readonly Regex _holderRe = new(@"holder\s*=\s*(\d+)");
        private static readonly Regex _charBlockRe = new(@"^(\d+)\s*=\s*\{");
        private static readonly Regex _charCultureRe = new(@"culture\s*=\s*""?([\w_]+)""?");

        public Dictionary<string, int> CountyToCapitalProvince { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void BuildProvinceToCounty(MapLoader mapLoader)
        {
            ProvinceToCounty.Clear();
            CountyToCapitalProvince.Clear();
            foreach (var kvp in mapLoader.ProvinceToBarony)
            {
                int pid = kvp.Key;
                string barony = kvp.Value;
                if (mapLoader.BaronyToCounty.TryGetValue(barony, out string? county))
                {
                    ProvinceToCounty[pid] = county;
                    if (!CountyToCapitalProvince.ContainsKey(county))
                        CountyToCapitalProvince[county] = pid;
                }
            }
        }

        public static string[] Ck3LanguageFiles = { "cultures_l_spanish", "cultures_l_english", "cultures_l_french", "cultures_l_german", "cultures_l_russian" };

        public Dictionary<string, string> CultureLocalizedNames { get; } = new(StringComparer.OrdinalIgnoreCase);

        public int LoadCultureLocalizedNames(string gameRoot, string uiLanguage)
        {
            string ck3Lang = uiLanguage switch
            {
                "es" => "spanish",
                "ca" => "spanish",
                "fr" => "french",
                "de" => "german",
                "ru" => "russian",
                _ => "english"
            };

            string folder = Path.Combine(gameRoot, "localization", ck3Lang, "culture");
            if (!Directory.Exists(folder)) return 0;

            int count = 0;
            foreach (string fname in Directory.EnumerateFiles(folder, "cultures_l_*.yml", SearchOption.TopDirectoryOnly))
            {
                foreach (string raw in File.ReadAllLines(fname))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("l_") || !line.Contains(": ") || line.StartsWith("#"))
                        continue;

                    int colonIdx = line.IndexOf(':');
                    if (colonIdx < 0) continue;

                    string key = line.Substring(0, colonIdx).Trim();
                    if (key.Contains(' ')) continue;

                    string val = line.Substring(colonIdx + 1).Trim();
                    if (val.StartsWith("\"") && val.EndsWith("\""))
                        val = val.Substring(1, val.Length - 2);

                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(val) && !key.EndsWith("_prefix") && !key.EndsWith("_collective_noun"))
                    {
                        CultureLocalizedNames[key] = val;
                        count++;
                    }
                }
            }
            return count;
        }

        public HashSet<int> LoadTitleHistory(string gameRoot)
        {
            string folder = Path.Combine(gameRoot, "history", "titles");
            CountyHolderHistory.Clear();
            var collectedIds = new HashSet<int>();

            if (!Directory.Exists(folder))
                return collectedIds;

            foreach (string fname in Directory.EnumerateFiles(folder, "*.txt", SearchOption.AllDirectories))
                ParseTitleHistoryFile(fname, CountyHolderHistory, collectedIds);

            return collectedIds;
        }

        private static void ParseTitleHistoryFile(
            string path,
            Dictionary<string, List<(int Year, int HolderId)>> output,
            HashSet<int> collectedIds)
        {
            string? currentCounty = null;
            var dateStack = new List<(int Year, int Depth)>();
            int depth = 0;

            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;

                int hashIdx = line.IndexOf('#');
                if (hashIdx >= 0)
                {
                    line = line.Substring(0, hashIdx).TrimEnd();
                    if (line.Length == 0) continue;
                }

                int opens = line.Count(c => c == '{');
                int closes = line.Count(c => c == '}');

                if (depth == 0)
                {
                    var cm = _countyBlockRe.Match(line);
                    if (cm.Success)
                    {
                        currentCounty = cm.Groups[1].Value;
                        dateStack.Clear();
                        depth += opens - closes;

                        var hm = _holderRe.Match(line);
                        if (hm.Success)
                            AddHolderEvent(output, collectedIds, currentCounty, 0, int.Parse(hm.Groups[1].Value));

                        continue;
                    }
                    depth += opens - closes;
                    continue;
                }

                if (currentCounty != null)
                {
                    var dm = _dateBlockRe.Match(line);
                    if (dm.Success)
                    {
                        int year = int.Parse(dm.Groups[1].Value);
                        dateStack.Add((year, depth + opens));

                        var hm = _holderRe.Match(line);
                        if (hm.Success)
                            AddHolderEvent(output, collectedIds, currentCounty, year, int.Parse(hm.Groups[1].Value));

                        depth += opens - closes;
                        CleanDateStack(dateStack, depth);
                        if (depth <= 0) currentCounty = null;
                        continue;
                    }

                    var hm2 = _holderRe.Match(line);
                    if (hm2.Success)
                    {
                        int year = dateStack.Count > 0 ? dateStack[^1].Year : 0;
                        AddHolderEvent(output, collectedIds, currentCounty, year, int.Parse(hm2.Groups[1].Value));
                    }
                }

                depth += opens - closes;
                if (depth > 0)
                    CleanDateStack(dateStack, depth);
                else
                {
                    currentCounty = null;
                    dateStack.Clear();
                }
            }
        }

        private static void AddHolderEvent(
            Dictionary<string, List<(int Year, int HolderId)>> output,
            HashSet<int> collectedIds,
            string county, int year, int holderId)
        {
            if (!output.TryGetValue(county, out var list))
            {
                list = new List<(int, int)>();
                output[county] = list;
            }
            list.Add((year, holderId));
            collectedIds.Add(holderId);
        }

        private static void CleanDateStack(List<(int Year, int Depth)> stack, int depth)
        {
            while (stack.Count > 0 && depth < stack[^1].Depth)
                stack.RemoveAt(stack.Count - 1);
        }

        public int LoadCharacterCultures(string gameRoot, HashSet<int> relevantIds)
        {
            string folder = Path.Combine(gameRoot, "history", "characters");
            CharacterCulture.Clear();
            int count = 0;

            if (!Directory.Exists(folder))
                return 0;

            foreach (string fname in Directory.EnumerateFiles(folder, "*.txt", SearchOption.AllDirectories))
                count += ParseCharacterFile(fname, CharacterCulture, relevantIds);

            return count;
        }

        private static int ParseCharacterFile(
            string path,
            Dictionary<int, string> output,
            HashSet<int> relevantIds)
        {
            int? currentChar = null;
            int depth = 0;
            int count = 0;

            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;

                int hashIdx = line.IndexOf('#');
                if (hashIdx >= 0)
                {
                    line = line.Substring(0, hashIdx).TrimEnd();
                    if (line.Length == 0) continue;
                }

                int opens = line.Count(c => c == '{');
                int closes = line.Count(c => c == '}');

                if (depth == 0)
                {
                    var cm = _charBlockRe.Match(line);
                    if (cm.Success)
                    {
                        currentChar = int.Parse(cm.Groups[1].Value);
                        depth += opens - closes;
                        continue;
                    }
                    depth += opens - closes;
                    continue;
                }

                if (currentChar.HasValue && relevantIds.Contains(currentChar.Value))
                {
                    var cm = _charCultureRe.Match(line);
                    if (cm.Success && !output.ContainsKey(currentChar.Value))
                    {
                        output[currentChar.Value] = cm.Groups[1].Value;
                        count++;
                    }
                }

                depth += opens - closes;
                if (depth <= 0)
                    currentChar = null;
            }

            return count;
        }

        private static int? GetHolderAtYear(List<(int Year, int HolderId)> holders, int year)
        {
            int? last = null;
            foreach (var (y, h) in holders)
            {
                if (y <= year)
                    last = h;
                else
                    break;
            }
            return last;
        }

        public string? GetEffectiveCulture(int provinceId, int year)
        {
            if (ProvinceCultures.TryGetValue(provinceId, out var cultures))
            {
                var result = GetCultureAtYear(cultures, year);
                if (result != null) return result;
            }

            if (!ProvinceToCounty.TryGetValue(provinceId, out string? county))
                return null;

            if (!CountyHolderHistory.TryGetValue(county, out var holders))
                return null;

            int? holderId = GetHolderAtYear(holders, year);
            if (holderId == null) return null;

            if (CharacterCulture.TryGetValue(holderId.Value, out string? culture))
                return culture;

            return null;
        }
    }
}
