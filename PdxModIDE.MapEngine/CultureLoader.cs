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
        public bool HasColor { get; set; }
        public string ColorReference { get; set; } = "";
    }

    public class NamedColor
    {
        public string Name { get; set; } = "";
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public bool HasColor { get; set; }
    }

    public class CultureLoader
    {
        private static readonly Regex ProvinceBlockRe = new(@"^\s*(\d+)\s*=\s*\{");
        private static readonly Regex CultureRe = new(@"culture\s*=\s*(\w+)");
        private static readonly Regex DateRe = new(@"(\d+)\.(\d+)\.(\d+)\s*=\s*\{");

        public Dictionary<string, CultureInfo> AllCultures { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<int, List<(int Year, string Culture)>> ProvinceCultures { get; } = new();

        public int LoadCultures(string gameRoot, bool overwriteDuplicates = false, string? namedColorsRoot = null)
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

            ResolveNamedColorReferences(gameRoot, namedColorsRoot);
            return AllCultures.Count;
        }

        private void ResolveNamedColorReferences(string gameRoot, string? namedColorsRoot)
        {
            var namedColors = LoadNamedColors(gameRoot, namedColorsRoot);
            foreach (var kvp in AllCultures)
            {
                var ci = kvp.Value;
                if (ci.HasColor || string.IsNullOrEmpty(ci.ColorReference)) continue;

                if (namedColors.TryGetValue(ci.ColorReference, out var named) && named.HasColor)
                {
                    ci.HasColor = true;
                    ci.R = named.R;
                    ci.G = named.G;
                    ci.B = named.B;
                }
            }
        }

        private static Dictionary<string, NamedColor> LoadNamedColors(string gameRoot, string? namedColorsRoot)
        {
            var result = new Dictionary<string, NamedColor>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in new[] { gameRoot, namedColorsRoot })
            {
                if (string.IsNullOrEmpty(root)) continue;
                var dir = Path.Combine(root, "common", "named_colors");
                if (!Directory.Exists(dir)) continue;
                foreach (var file in Directory.GetFiles(dir, "*.txt"))
                    ParseNamedColorsFile(file, result);
            }
            return result;
        }

        private static void ParseNamedColorsFile(string path, Dictionary<string, NamedColor> output)
        {
            var text = File.ReadAllText(path);
            int pos = 0;
            while (pos < text.Length)
            {
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length) break;

                string key = ReadKey(text, ref pos);
                if (string.IsNullOrEmpty(key)) break;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '=')
                {
                    SkipValueAndFollowingBlock(text, ref pos);
                    continue;
                }
                pos++;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '{') continue;
                pos++;

                string block = ReadBlock(text, ref pos);
                if (key == "colors")
                    ParseNamedColorBlock(block, output);
            }
        }

        private static void ParseNamedColorBlock(string block, Dictionary<string, NamedColor> output)
        {
            int pos = 0;
            while (pos < block.Length)
            {
                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length) break;

                string name = ReadKey(block, ref pos);
                if (string.IsNullOrEmpty(name)) break;

                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length || block[pos] != '=')
                {
                    SkipValueAndFollowingBlock(block, ref pos);
                    continue;
                }
                pos++;

                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length) break;

                if (TryParseColorValue(block, ref pos, out byte r, out byte g, out byte b, out _))
                {
                    output[name] = new NamedColor
                    {
                        Name = name,
                        R = r,
                        G = g,
                        B = b,
                        HasColor = true
                    };
                }
            }
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
            string text = File.ReadAllText(path);
            int pos = 0;

            while (pos < text.Length)
            {
                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length) break;

                string key = ReadKey(text, ref pos);
                if (string.IsNullOrEmpty(key)) break;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '=')
                {
                    SkipValueAndFollowingBlock(text, ref pos);
                    continue;
                }
                pos++;

                SkipWhitespaceAndComments(text, ref pos);
                if (pos >= text.Length || text[pos] != '{')
                {
                    SkipValueAndFollowingBlock(text, ref pos);
                    continue;
                }
                pos++;

                string block = ReadBlock(text, ref pos);
                if (!data.ContainsKey(key))
                {
                    var info = new CultureInfo();

                    string? nameAttr = ExtractAttribute(block, "name");
                    if (nameAttr != null) info.Name = nameAttr;

                    ExtractColor(block, info);
                    data[key] = info;
                }
            }

            return data;
        }

        private static string? ExtractAttribute(string block, string attributeName)
        {
            int pos = 0;
            while (pos < block.Length)
            {
                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length) break;

                string key = ReadKey(block, ref pos);
                if (string.IsNullOrEmpty(key)) break;

                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length || block[pos] != '=') continue;
                pos++;

                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length) break;

                if (key == attributeName)
                {
                    if (block[pos] == '"')
                    {
                        pos++;
                        int start = pos;
                        while (pos < block.Length && block[pos] != '"') pos++;
                        if (pos < block.Length)
                            return block.Substring(start, pos - start);
                    }
                    else
                    {
                        int start = pos;
                        while (pos < block.Length && !char.IsWhiteSpace(block[pos]) && block[pos] != '}' && block[pos] != '#')
                        {
                            if (block[pos] == '-' && pos + 1 < block.Length && block[pos + 1] == '-')
                                break;
                            pos++;
                        }
                        return block.Substring(start, pos - start);
                    }
                }

                SkipValueAndFollowingBlock(block, ref pos);
            }

            return null;
        }

        private static void ExtractColor(string block, CultureInfo culture)
        {
            int pos = 0;
            while (pos < block.Length)
            {
                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length) break;

                string key = ReadKey(block, ref pos);
                if (string.IsNullOrEmpty(key)) break;

                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length || block[pos] != '=')
                {
                    SkipValueAndFollowingBlock(block, ref pos);
                    continue;
                }
                pos++;

                SkipWhitespaceAndComments(block, ref pos);
                if (pos >= block.Length) break;

                if (key == "color")
                {
                    if (TryParseColorValue(block, ref pos, out byte r, out byte g, out byte b, out _))
                    {
                        culture.HasColor = true;
                        culture.R = r;
                        culture.G = g;
                        culture.B = b;
                    }
                    else
                    {
                        culture.ColorReference = ReadKey(block, ref pos);
                    }
                    return;
                }

                SkipValueAndFollowingBlock(block, ref pos);
            }
        }

        private static bool TryParseColorValue(string block, ref int pos, out byte r, out byte g, out byte b, out string display)
        {
            r = g = b = 0;
            display = "";

            string? mode = null;
            if (block[pos] != '{')
            {
                int save = pos;
                string kw = ReadKey(block, ref pos);
                if (!string.IsNullOrEmpty(kw))
                {
                    SkipWhitespaceAndComments(block, ref pos);
                    if (pos < block.Length && block[pos] == '{')
                        mode = kw;
                    else
                        pos = save;
                }
            }

            if (pos < block.Length && block[pos] == '{')
            {
                string content = ReadBraceContent(block, ref pos);
                return TryParseColorValues(content, mode, out r, out g, out b, out display);
            }

            return false;
        }

        private static bool TryParseColorValues(string content, string? mode, out byte r, out byte g, out byte b, out string display)
        {
            r = g = b = 0;
            display = "";

            var numbers = new List<float>();
            int pos = 0;
            while (pos < content.Length)
            {
                SkipWhitespaceAndComments(content, ref pos);
                if (pos >= content.Length) break;

                int start = pos;
                while (pos < content.Length && (char.IsDigit(content[pos]) || content[pos] == '.' || content[pos] == '-'))
                    pos++;
                if (pos > start &&
                    float.TryParse(content.Substring(start, pos - start),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float v))
                    numbers.Add(v);
            }

            if (numbers.Count < 3) return false;

            display = mode != null
                ? $"{mode} {{ {FormatColorNumber(numbers[0])} {FormatColorNumber(numbers[1])} {FormatColorNumber(numbers[2])} }}"
                : $"{FormatColorNumber(numbers[0])} {FormatColorNumber(numbers[1])} {FormatColorNumber(numbers[2])}";

            float fr, fg, fb;
            switch (mode)
            {
                case "hsv":
                    (fr, fg, fb) = HsvToRgb(numbers[0], numbers[1], numbers[2]);
                    break;
                case "hsv360":
                    (fr, fg, fb) = HsvToRgb(numbers[0] / 360f, numbers[1] / 100f, numbers[2] / 100f);
                    break;
                case "rgb":
                    fr = numbers[0]; fg = numbers[1]; fb = numbers[2];
                    break;
                default:
                    fr = numbers[0]; fg = numbers[1]; fb = numbers[2];
                    if (fr <= 1f && fg <= 1f && fb <= 1f)
                    {
                        fr *= 255f; fg *= 255f; fb *= 255f;
                    }
                    break;
            }

            r = (byte)Math.Clamp(Math.Round(fr), 0, 255);
            g = (byte)Math.Clamp(Math.Round(fg), 0, 255);
            b = (byte)Math.Clamp(Math.Round(fb), 0, 255);
            return true;
        }

        private static string FormatColorNumber(float value)
        {
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static (float R, float G, float B) HsvToRgb(float h, float s, float v)
        {
            float c = v * s;
            float x = c * (1f - Math.Abs((h * 6f) % 2f - 1f));
            float m = v - c;
            float r = 0f, g = 0f, b = 0f;
            switch ((int)Math.Floor(h * 6f) % 6)
            {
                case 0: r = c; g = x; break;
                case 1: r = x; g = c; break;
                case 2: g = c; b = x; break;
                case 3: g = x; b = c; break;
                case 4: r = x; b = c; break;
                default: r = c; b = x; break;
            }
            return (r + m, g + m, b + m);
        }

        private static string ReadBraceContent(string text, ref int pos)
        {
            int depth = 1;
            int start = pos + 1;
            pos++;
            while (pos < text.Length && depth > 0)
            {
                if (text[pos] == '{') depth++;
                else if (text[pos] == '}') depth--;
                if (depth > 0) pos++;
            }
            string result = text.Substring(start, pos - start);
            if (pos < text.Length) pos++;
            return result;
        }

        private static string ReadBlock(string text, ref int pos)
        {
            int depth = 1;
            int start = pos;
            while (pos < text.Length && depth > 0)
            {
                if (text[pos] == '{') depth++;
                else if (text[pos] == '}') depth--;
                if (depth > 0) pos++;
            }
            string result = text.Substring(start, pos - start);
            if (pos < text.Length) pos++;
            return result;
        }

        private static string ReadKey(string text, ref int pos)
        {
            int start = pos;
            while (pos < text.Length && (char.IsLetterOrDigit(text[pos]) || text[pos] == '_' || text[pos] == '@'))
                pos++;
            return pos > start ? text.Substring(start, pos - start) : "";
        }

        private static void SkipWhitespaceAndComments(string text, ref int pos)
        {
            while (pos < text.Length)
            {
                if (char.IsWhiteSpace(text[pos]))
                {
                    pos++;
                }
                else if (text[pos] == '#' || (text[pos] == '-' && pos + 1 < text.Length && text[pos + 1] == '-'))
                {
                    while (pos < text.Length && text[pos] != '\n') pos++;
                }
                else
                {
                    break;
                }
            }
        }

        private static void SkipValueAndFollowingBlock(string block, ref int pos)
        {
            if (pos >= block.Length) return;

            if (block[pos] == '{')
            {
                pos++;
                ReadBlock(block, ref pos);
            }
            else if (block[pos] == '"')
            {
                pos++;
                while (pos < block.Length && block[pos] != '"') pos++;
                if (pos < block.Length) pos++;
            }
            else
            {
                while (pos < block.Length && !char.IsWhiteSpace(block[pos]) && block[pos] != '}' && block[pos] != '#')
                {
                    if (block[pos] == '-' && pos + 1 < block.Length && block[pos + 1] == '-')
                        break;
                    pos++;
                }
                SkipWhitespaceAndComments(block, ref pos);
                if (pos < block.Length && block[pos] == '{')
                {
                    pos++;
                    ReadBlock(block, ref pos);
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

        public Dictionary<string, List<(int Year, string HolderId)>> CountyHolderHistory { get; }
            = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> CharacterCulture { get; } = new();

        private static readonly Regex _countyBlockRe = new(@"^(c_\w+)\s*=\s*\{");
        private static readonly Regex _dateBlockRe = new(@"(\d+)\.\d+\.\d+\s*=\s*\{");
        private static readonly Regex _holderRe = new(@"holder\s*=\s*""?([\w.]+)""?");
        private static readonly Regex _charBlockRe = new(@"^([\w.]+)\s*=\s*\{");
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

        public HashSet<string> LoadTitleHistory(string gameRoot)
        {
            string folder = Path.Combine(gameRoot, "history", "titles");
            CountyHolderHistory.Clear();
            var collectedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!Directory.Exists(folder))
                return collectedIds;

            foreach (string fname in Directory.EnumerateFiles(folder, "*.txt", SearchOption.AllDirectories))
                ParseTitleHistoryFile(fname, CountyHolderHistory, collectedIds);

            return collectedIds;
        }

        private static void ParseTitleHistoryFile(
            string path,
            Dictionary<string, List<(int Year, string HolderId)>> output,
            HashSet<string> collectedIds)
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
                            AddHolderEvent(output, collectedIds, currentCounty, 0, hm.Groups[1].Value);

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
                            AddHolderEvent(output, collectedIds, currentCounty, year, hm.Groups[1].Value);

                        depth += opens - closes;
                        CleanDateStack(dateStack, depth);
                        if (depth <= 0) currentCounty = null;
                        continue;
                    }

                    var hm2 = _holderRe.Match(line);
                    if (hm2.Success)
                    {
                        int year = dateStack.Count > 0 ? dateStack[^1].Year : 0;
                        AddHolderEvent(output, collectedIds, currentCounty, year, hm2.Groups[1].Value);
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
            Dictionary<string, List<(int Year, string HolderId)>> output,
            HashSet<string> collectedIds,
            string county, int year, string holderId)
        {
            if (!output.TryGetValue(county, out var list))
            {
                list = new List<(int, string)>();
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

        public int LoadCharacterCultures(string gameRoot, HashSet<string> relevantIds)
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
            Dictionary<string, string> output,
            HashSet<string> relevantIds)
        {
            string? currentChar = null;
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
                        currentChar = cm.Groups[1].Value;
                        depth += opens - closes;
                        continue;
                    }
                    depth += opens - closes;
                    continue;
                }

                if (currentChar != null && relevantIds.Contains(currentChar))
                {
                    var cm = _charCultureRe.Match(line);
                    if (cm.Success && !output.ContainsKey(currentChar))
                    {
                        output[currentChar] = cm.Groups[1].Value;
                        count++;
                    }
                }

                depth += opens - closes;
                if (depth <= 0)
                    currentChar = null;
            }

            return count;
        }

        private static string? GetHolderAtYear(List<(int Year, string HolderId)> holders, int year)
        {
            string? last = null;
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

            string? holderId = GetHolderAtYear(holders, year);
            if (holderId == null) return null;

            if (CharacterCulture.TryGetValue(holderId, out string? culture))
                return culture;

            return null;
        }
    }
}
