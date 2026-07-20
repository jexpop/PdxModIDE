using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace PdxModIDE.MapEngine
{
    public class ProvinceInfo
    {
        public int Id { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public int ColorPacked => (R << 16) | (G << 8) | B;
        public string Name { get; set; } = "";
        public string? Type { get; set; }
        public string? Barony { get; set; }
        public string? County { get; set; }
    }

    public class MapLoader
    {
        public Dictionary<int, ProvinceInfo> ProvincesById { get; } = new();
        public Dictionary<int, ProvinceInfo> ProvincesByColor { get; } = new();
        public HashSet<int> Sea { get; } = new();
        public HashSet<int> Lakes { get; } = new();
        public HashSet<int> Rivers { get; } = new();
        public HashSet<int> Impassable { get; } = new();
        public HashSet<int> ImpassableSeas { get; } = new();
        public Dictionary<int, string> ProvinceToBarony { get; } = new();
        public Dictionary<string, string> BaronyToCounty { get; } = new();
        public Dictionary<string, string> CountyToDuchy { get; } = new();
        public Dictionary<string, string> DuchyToKingdom { get; } = new();
        public Dictionary<string, string> KingdomToEmpire { get; } = new();
        public Dictionary<string, string> TitleDisplayNames { get; } = new();
        public Dictionary<string, Dictionary<string, string>> LocalizedNames { get; } = new();

        private Dictionary<int, string> _baseProvinceToBarony = new();
        private Dictionary<string, string> _baseBaronyToCounty = new();
        private Dictionary<string, string> _baseCountyToDuchy = new();
        private Dictionary<string, string> _baseDuchyToKingdom = new();
        private Dictionary<string, string> _baseKingdomToEmpire = new();
        private Dictionary<string, string> _baseTitleDisplayNames = new();
        private Dictionary<string, Dictionary<string, string>> _baseLocalizedNames = new();
        public Dictionary<int, int> ProvinceIdToPacked { get; } = new();
        public byte[]? Lut { get; private set; }
        public int[]? ProvinceIdMap { get; private set; }
        public int MapWidth { get; private set; }
        public int MapHeight { get; private set; }

        public string? DefinitionPath { get; private set; }
        public string? DefaultMapPath { get; private set; }
        public string? ProvincesPngPath { get; private set; }
        public string? LandedTitlesPath { get; private set; }

        private readonly string _gameRoot;

        public MapLoader(string gameRoot)
        {
            _gameRoot = gameRoot;
        }

        public void LoadAll()
        {
            string mapDataDir = Path.Combine(_gameRoot, "map_data");
            string mapDir = Path.Combine(_gameRoot, "map");

            DefinitionPath = FindFile(mapDataDir, "definition.csv") ?? FindFile(mapDir, "definition.csv");
            DefaultMapPath = FindFile(mapDataDir, "default.map") ?? FindFile(mapDir, "default.map");
            ProvincesPngPath = FindFile(mapDataDir, "provinces.png") ?? FindFile(mapDir, "provinces.png")
                ?? FindFile(mapDataDir, "provinces.bmp") ?? FindFile(mapDir, "provinces.bmp");

            if (DefinitionPath == null)
                throw new FileNotFoundException("definition.csv not found in map_data/");

            LoadDefinition();
            LoadDefaultMap();
            LoadLandedTitles();
            LoadLocalization();
            SaveBaseSnapshot();
            MarkTerrainTypes();
            Lut = BuildOrLoadLut();
            BuildPixelData();
        }

        private static string? FindFile(string dir, string fileName)
        {
            if (!Directory.Exists(dir)) return null;
            string path = Path.Combine(dir, fileName);
            return File.Exists(path) ? path : null;
        }

        private void LoadDefinition()
        {
            ProvincesById.Clear();
            ProvincesByColor.Clear();

            foreach (var line in File.ReadAllLines(DefinitionPath!))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                var parts = trimmed.Split(';');
                if (parts.Length < 5) continue;

                if (!int.TryParse(parts[0], out int pid)) continue;
                if (!byte.TryParse(parts[1], out byte r)) continue;
                if (!byte.TryParse(parts[2], out byte g)) continue;
                if (!byte.TryParse(parts[3], out byte b)) continue;

                if (pid == 0) continue;

                string name = parts[4].Trim();
                var info = new ProvinceInfo
                {
                    Id = pid,
                    R = r, G = g, B = b,
                    Name = name
                };

                ProvincesById[pid] = info;
                ProvincesByColor[info.ColorPacked] = info;
                ProvinceIdToPacked[pid] = info.ColorPacked;

                if (name.StartsWith("b_"))
                    ProvinceToBarony[pid] = name;
            }
        }

        private void LoadDefaultMap()
        {
            if (DefaultMapPath == null) return;

            string text = File.ReadAllText(DefaultMapPath);
            string pattern = @"(\w+)\s*=\s*(LIST|RANGE)?\s*\{([^}]+)\}";

            foreach (Match match in Regex.Matches(text, pattern, RegexOptions.Multiline))
            {
                string name = match.Groups[1].Value;
                string blockType = match.Groups[2].Value;
                string content = match.Groups[3].Value;

                var ids = new HashSet<int>();
                foreach (var line in content.Split('\n'))
                {
                    var clean = line.Split('#')[0].Trim();
                    if (string.IsNullOrEmpty(clean)) continue;

                    var nums = Regex.Matches(clean, @"\d+")
                        .Select(m => int.Parse(m.Value)).ToList();
                    if (nums.Count == 0) continue;

                    if (blockType == "RANGE" && nums.Count == 2)
                    {
                        for (int x = nums[0]; x <= nums[1]; x++)
                            ids.Add(x);
                        continue;
                    }

                    foreach (var n in nums) ids.Add(n);
                }

                switch (name)
                {
                    case "sea_zones": Sea.UnionWith(ids); break;
                    case "lakes": Lakes.UnionWith(ids); break;
                    case "river_provinces": Rivers.UnionWith(ids); break;
                    case "impassable_mountains": Impassable.UnionWith(ids); break;
                    case "impassable_seas": ImpassableSeas.UnionWith(ids); break;
                }
            }
        }

        private void SaveBaseSnapshot()
        {
            _baseProvinceToBarony = new Dictionary<int, string>(ProvinceToBarony);
            _baseBaronyToCounty = new Dictionary<string, string>(BaronyToCounty);
            _baseCountyToDuchy = new Dictionary<string, string>(CountyToDuchy);
            _baseDuchyToKingdom = new Dictionary<string, string>(DuchyToKingdom);
            _baseKingdomToEmpire = new Dictionary<string, string>(KingdomToEmpire);
            _baseTitleDisplayNames = new Dictionary<string, string>(TitleDisplayNames);
            _baseLocalizedNames = new Dictionary<string, Dictionary<string, string>>();
            foreach (var kvp in LocalizedNames)
                _baseLocalizedNames[kvp.Key] = new Dictionary<string, string>(kvp.Value);
        }

        public void LoadModLandedTitles(string modRoot)
        {
            string baseDir = Path.Combine(modRoot, "common", "landed_titles");
            if (!Directory.Exists(baseDir)) return;
            LoadLandedTitlesFrom(baseDir);
        }

        public void LoadModLocalization(string modRoot)
        {
            string locDir = Path.Combine(modRoot, "localization");
            if (!Directory.Exists(locDir)) return;
            LoadLocalizationFromDir(locDir);

            string replaceDir = Path.Combine(locDir, "replace");
            if (Directory.Exists(replaceDir))
                LoadLocalizationFromDir(replaceDir);
        }

        public void ResetToBase()
        {
            ProvinceToBarony.Clear();
            BaronyToCounty.Clear();
            CountyToDuchy.Clear();
            DuchyToKingdom.Clear();
            KingdomToEmpire.Clear();
            foreach (var kvp in _baseProvinceToBarony) ProvinceToBarony[kvp.Key] = kvp.Value;
            foreach (var kvp in _baseBaronyToCounty) BaronyToCounty[kvp.Key] = kvp.Value;
            foreach (var kvp in _baseCountyToDuchy) CountyToDuchy[kvp.Key] = kvp.Value;
            foreach (var kvp in _baseDuchyToKingdom) DuchyToKingdom[kvp.Key] = kvp.Value;
            foreach (var kvp in _baseKingdomToEmpire) KingdomToEmpire[kvp.Key] = kvp.Value;
            TitleDisplayNames.Clear();
            foreach (var kvp in _baseTitleDisplayNames)
                TitleDisplayNames[kvp.Key] = kvp.Value;
            LocalizedNames.Clear();
            foreach (var kvp in _baseLocalizedNames)
                LocalizedNames[kvp.Key] = new Dictionary<string, string>(kvp.Value);
        }

        private void LoadLandedTitles()
        {
            string baseDir = Path.Combine(_gameRoot, "common", "landed_titles");
            if (Directory.Exists(baseDir))
                LoadLandedTitlesFrom(baseDir);
        }

        private void LoadLandedTitlesFrom(string dir)
        {
            if (!Directory.Exists(dir)) return;

            var titleRegex = new Regex(@"^\s*([becdk]_[A-Za-z0-9_-]+)\s*=\s*\{");
            var provinceRegex = new Regex(@"province\s*=\s*(\d+)");
            var nameRegex = new Regex(@"^\s*name\s*=\s*""([^""]*)""");

            foreach (var file in Directory.EnumerateFiles(dir, "*.txt", SearchOption.AllDirectories))
            {
                var stack = new List<string>();
                string? currentTitle = null;
                int nonTitleDepth = 0;

                foreach (var rawLine in File.ReadAllLines(file))
                {
                    string line = rawLine.Trim();

                    var m = titleRegex.Match(line);
                    if (m.Success)
                    {
                        currentTitle = m.Groups[1].Value;
                        stack.Add(currentTitle);
                        continue;
                    }

                    var m2 = provinceRegex.Match(line);
                    if (m2.Success && currentTitle != null)
                    {
                        int pid = int.Parse(m2.Groups[1].Value);
                        if (currentTitle.StartsWith("b_"))
                        {
                            ProvinceToBarony[pid] = currentTitle;
                            var county = stack.LastOrDefault(t => t.StartsWith("c_"));
                            if (county != null)
                                BaronyToCounty[currentTitle] = county;
                        }
                    }

                    var nameMatch = nameRegex.Match(line);
                    if (nameMatch.Success && currentTitle != null)
                    {
                        TitleDisplayNames[currentTitle] = nameMatch.Groups[1].Value;
                    }

                    // Build hierarchy mappings from stack
                    if (currentTitle != null)
                    {
                        var county = stack.LastOrDefault(t => t.StartsWith("c_"));
                        var duchy = stack.LastOrDefault(t => t.StartsWith("d_"));
                        var kingdom = stack.LastOrDefault(t => t.StartsWith("k_"));
                        var empire = stack.LastOrDefault(t => t.StartsWith("e_"));

                        if (county != null && duchy != null)
                            CountyToDuchy[county] = duchy;
                        if (duchy != null && kingdom != null)
                            DuchyToKingdom[duchy] = kingdom;
                        if (kingdom != null && empire != null)
                            KingdomToEmpire[kingdom] = empire;
                    }

                    int opens = line.Count(c => c == '{');
                    int closes = line.Count(c => c == '}');

                    nonTitleDepth += opens;

                    if (closes > 0)
                    {
                        if (nonTitleDepth >= closes)
                        {
                            nonTitleDepth -= closes;
                        }
                        else
                        {
                            int titleCloses = closes - nonTitleDepth;
                            nonTitleDepth = 0;
                            for (int i = 0; i < titleCloses; i++)
                            {
                                if (stack.Count > 0)
                                {
                                    stack.RemoveAt(stack.Count - 1);
                                    currentTitle = stack.Count > 0 ? stack[^1] : null;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void MarkTerrainTypes()
        {
            var inDefault = new HashSet<int>(Sea);
            inDefault.UnionWith(Lakes);
            inDefault.UnionWith(Rivers);
            inDefault.UnionWith(Impassable);
            inDefault.UnionWith(ImpassableSeas);

            foreach (var info in ProvincesById.Values)
            {
                if (Sea.Contains(info.Id)) info.Type = "sea";
                else if (Lakes.Contains(info.Id)) info.Type = "lake";
                else if (Rivers.Contains(info.Id)) info.Type = "river";
                else if (Impassable.Contains(info.Id) || ImpassableSeas.Contains(info.Id)) info.Type = "impassable";
                else if (!inDefault.Contains(info.Id)) info.Type = "land";
                else info.Type = "unknown";
            }
        }

        private static string CacheDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PdxModIDE", "lut_cache");

        private byte[] BuildOrLoadLut()
        {
            var defHash = FileHash(DefinitionPath);
            var mapHash = FileHash(DefaultMapPath);
            var metaPath = Path.Combine(CacheDir, "lut_meta.json");
            var binPath = Path.Combine(CacheDir, "lut_types.bin");

            if (File.Exists(metaPath) && File.Exists(binPath))
            {
                try
                {
                    var meta = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(metaPath));
                    if (meta != null &&
                        meta.TryGetValue("definition_hash", out var dh) && dh == defHash &&
                        meta.TryGetValue("default_map_hash", out var mh) && mh == mapHash)
                    {
                        var cached = File.ReadAllBytes(binPath);
                        if (cached.Length == 16_777_216)
                            return cached;
                    }
                }
                catch { }
            }

            var lut = BuildLutInMemory();

            try
            {
                Directory.CreateDirectory(CacheDir);
                File.WriteAllBytes(binPath, lut);
                File.WriteAllText(metaPath, JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["definition_hash"] = defHash ?? "",
                    ["default_map_hash"] = mapHash ?? ""
                }));
            }
            catch { }

            return lut;
        }

        private static string? FileHash(string? path)
        {
            if (path == null || !File.Exists(path)) return null;
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(path);
            var hash = md5.ComputeHash(stream);
            return Convert.ToHexString(hash).ToLower();
        }

        private byte[] BuildLutInMemory()
        {
            var lut = new byte[16_777_216];

            foreach (var info in ProvincesByColor.Values)
            {
                int idx = info.ColorPacked;
                byte val = info.Type switch
                {
                    "sea" => 1,
                    "lake" => 2,
                    "river" => 3,
                    "impassable" => 4,
                    "land" => 0,
                    _ => 5
                };
                lut[idx] = val;
            }

            return lut;
        }

        private void BuildPixelData()
        {
            if (ProvincesPngPath == null || Lut == null) return;
            using var bmp = SKBitmap.Decode(ProvincesPngPath);
            int w = bmp.Width;
            int h = bmp.Height;
            MapWidth = w;
            MapHeight = h;

            var pixels = ReadPixels(bmp);
            ProvinceIdMap = new int[w * h];

            for (int y = 0; y < h; y++)
            {
                int rowOff = y * w * 4;
                for (int x = 0; x < w; x++)
                {
                    int off = rowOff + x * 4;
                    byte pb = pixels[off];
                    byte pg = pixels[off + 1];
                    byte pr = pixels[off + 2];
                    int packed = (pr << 16) | (pg << 8) | pb;

                    if (!ProvincesByColor.ContainsKey(packed))
                    {
                        ProvincesByColor[packed] = new ProvinceInfo
                        {
                            Id = -1, R = pr, G = pg, B = pb,
                            Name = "UNKNOWN", Type = "unknown"
                        };
                    }

                    ProvinceIdMap[y * w + x] = ProvincesByColor[packed].Id;
                }
            }
        }

        public ProvinceInfo? GetProvinceFromColor(byte r, byte g, byte b)
        {
            int packed = (r << 16) | (g << 8) | b;
            return ProvincesByColor.TryGetValue(packed, out var info) ? info : null;
        }

        public ProvinceInfo? GetProvinceFromId(int id)
        {
            return ProvincesById.TryGetValue(id, out var info) ? info : null;
        }

        public string? GetTitleFromProvinceId(int provinceId)
        {
            return ProvinceToBarony.TryGetValue(provinceId, out var b) ? b : null;
        }

        public string? GetCountyFromBarony(string barony)
        {
            return BaronyToCounty.TryGetValue(barony, out var c) ? c : null;
        }

        public string? GetCountyFromProvinceId(int provinceId)
        {
            var barony = GetTitleFromProvinceId(provinceId);
            return barony != null ? GetCountyFromBarony(barony) : null;
        }

        public byte GetTerrainType(int packedColor)
        {
            if (Lut == null || packedColor < 0 || packedColor >= Lut.Length) return 5;
            return Lut[packedColor];
        }

        public static byte[] ReadPixels(SKBitmap bmp)
        {
            int size = bmp.ByteCount;
            byte[] result = new byte[size];
            IntPtr ptr = bmp.GetPixels();
            Marshal.Copy(ptr, result, 0, size);
            return result;
        }

        public byte[] BuildHolderLut(int year, TitleHistoryLoader history, out Dictionary<int, string> indexToHolder)
        {
            indexToHolder = new Dictionary<int, string>();
            var holderToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int nextIndex = 1;
            var lut = new byte[16_777_216];

            foreach (var info in ProvincesByColor.Values)
            {
                int idx = info.ColorPacked;
                int pid = info.Id;

                if (pid <= 0)
                {
                    lut[idx] = 0;
                    continue;
                }

                string? holder = null;

                if (ProvinceToBarony.TryGetValue(pid, out var barony) &&
                    BaronyToCounty.TryGetValue(barony, out var county) &&
                    history.AllTitles.TryGetValue(county, out var titleHist))
                {
                    holder = GetHolderAtYear(titleHist, year);
                }

                if (holder == null)
                {
                    lut[idx] = 0;
                    continue;
                }

                if (!holderToIndex.TryGetValue(holder, out var hIdx))
                {
                    hIdx = nextIndex++;
                    if (hIdx > 255) hIdx = (hIdx - 1) % 255 + 1; // wrap around 1-255
                    holderToIndex[holder] = hIdx;
                    indexToHolder[hIdx] = holder;
                }

                lut[idx] = (byte)hIdx;
            }

            return lut;
        }

        public byte[] BuildCombinedHolderLut(
            int? baseYear, TitleHistoryLoader? baseHistory,
            int? modYear, TitleHistoryLoader? modHistory,
            out Dictionary<int, string> indexToHolder)
        {
            indexToHolder = new Dictionary<int, string>();
            var holderToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int nextIndex = 1;
            var lut = new byte[16_777_216];

            foreach (var info in ProvincesByColor.Values)
            {
                int idx = info.ColorPacked;
                int pid = info.Id;

                if (pid <= 0)
                {
                    lut[idx] = 0;
                    continue;
                }

                string? holder = null;

                if (ProvinceToBarony.TryGetValue(pid, out var barony) &&
                    BaronyToCounty.TryGetValue(barony, out var county))
                {
                    // Prioridad: Mod primero (con offset ya aplicado en modYear); si no hay datos, Base.
                    if (modYear.HasValue && modHistory != null &&
                        modHistory.AllTitles.TryGetValue(county, out var modHist))
                    {
                        holder = TitleHistoryLoader.GetHolderAtYear(modHist, modYear.Value);
                    }

                    if (holder == null && baseYear.HasValue && baseHistory != null &&
                        baseHistory.AllTitles.TryGetValue(county, out var baseHist))
                    {
                        holder = TitleHistoryLoader.GetHolderAtYear(baseHist, baseYear.Value);
                    }
                }

                if (holder == null)
                {
                    lut[idx] = 0;
                    continue;
                }

                if (!holderToIndex.TryGetValue(holder, out var hIdx))
                {
                    hIdx = nextIndex++;
                    if (hIdx > 255) hIdx = (hIdx - 1) % 255 + 1; // wrap around 1-255
                    holderToIndex[holder] = hIdx;
                    indexToHolder[hIdx] = holder;
                }

                lut[idx] = (byte)hIdx;
            }

            return lut;
        }

        public byte[] BuildCountyLut(out Dictionary<int, string> indexToCounty)
        {
            // Overload without year/history - county boundaries don't change by year
            return BuildCountyLut(0, null, out indexToCounty);
        }

        public byte[] BuildCountyLut(int year, TitleHistoryLoader history, out Dictionary<int, string> indexToCounty)
        {
            indexToCounty = new Dictionary<int, string>();
            var countyToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int nextIndex = 1;
            var lut = new byte[16_777_216];

            foreach (var info in ProvincesByColor.Values)
            {
                int idx = info.ColorPacked;
                int pid = info.Id;

                if (pid <= 0)
                {
                    lut[idx] = 0;
                    continue;
                }

                string? county = null;

                if (ProvinceToBarony.TryGetValue(pid, out var barony) &&
                    BaronyToCounty.TryGetValue(barony, out county))
                {
                    // county found, use it directly
                }

                if (county == null)
                {
                    lut[idx] = 0;
                    continue;
                }

if (!countyToIndex.TryGetValue(county, out var cIdx))
                {
                    cIdx = nextIndex++;
                    if (cIdx > 255) cIdx = (cIdx - 1) % 255 + 1; // wrap around 1-255
                    countyToIndex[county] = cIdx;
                    indexToCounty[cIdx] = county;
                }

                lut[idx] = (byte)cIdx;
            }

            return lut;
        }

        public byte[] BuildDuchyLut(out Dictionary<int, string> indexToDuchy)
        {
            indexToDuchy = new Dictionary<int, string>();
            var duchyToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int nextIndex = 1;
            var lut = new byte[16_777_216];

            foreach (var info in ProvincesByColor.Values)
            {
                int idx = info.ColorPacked;
                int pid = info.Id;

                if (pid <= 0)
                {
                    lut[idx] = 0;
                    continue;
                }

                string? duchy = null;

                if (ProvinceToBarony.TryGetValue(pid, out var barony) &&
                    BaronyToCounty.TryGetValue(barony, out var county) &&
                    CountyToDuchy.TryGetValue(county, out duchy))
                {
                }

                if (duchy == null)
                {
                    lut[idx] = 0;
                    continue;
                }

                if (!duchyToIndex.TryGetValue(duchy, out var dIdx))
                {
                    dIdx = nextIndex++;
                    if (dIdx > 255) dIdx = (dIdx - 1) % 255 + 1;
                    duchyToIndex[duchy] = dIdx;
                    indexToDuchy[dIdx] = duchy;
                }

                lut[idx] = (byte)dIdx;
            }

            return lut;
        }

        public byte[] BuildKingdomLut(out Dictionary<int, string> indexToKingdom)
        {
            indexToKingdom = new Dictionary<int, string>();
            var kingdomToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int nextIndex = 1;
            var lut = new byte[16_777_216];

            foreach (var info in ProvincesByColor.Values)
            {
                int idx = info.ColorPacked;
                int pid = info.Id;

                if (pid <= 0)
                {
                    lut[idx] = 0;
                    continue;
                }

                string? kingdom = null;

                if (ProvinceToBarony.TryGetValue(pid, out var barony) &&
                    BaronyToCounty.TryGetValue(barony, out var county) &&
                    CountyToDuchy.TryGetValue(county, out var duchy) &&
                    DuchyToKingdom.TryGetValue(duchy, out kingdom))
                {
                }

                if (kingdom == null)
                {
                    lut[idx] = 0;
                    continue;
                }

                if (!kingdomToIndex.TryGetValue(kingdom, out var kIdx))
                {
                    kIdx = nextIndex++;
                    if (kIdx > 255) kIdx = (kIdx - 1) % 255 + 1;
                    kingdomToIndex[kingdom] = kIdx;
                    indexToKingdom[kIdx] = kingdom;
                }

                lut[idx] = (byte)kIdx;
            }

            return lut;
        }

        public byte[] BuildEmpireLut(out Dictionary<int, string> indexToEmpire)
        {
            indexToEmpire = new Dictionary<int, string>();
            var empireToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int nextIndex = 1;
            var lut = new byte[16_777_216];

            foreach (var info in ProvincesByColor.Values)
            {
                int idx = info.ColorPacked;
                int pid = info.Id;

                if (pid <= 0)
                {
                    lut[idx] = 0;
                    continue;
                }

                string? empire = null;

                if (ProvinceToBarony.TryGetValue(pid, out var barony) &&
                    BaronyToCounty.TryGetValue(barony, out var county) &&
                    CountyToDuchy.TryGetValue(county, out var duchy) &&
                    DuchyToKingdom.TryGetValue(duchy, out var kingdom) &&
                    KingdomToEmpire.TryGetValue(kingdom, out empire))
                {
                }

                if (empire == null)
                {
                    lut[idx] = 0;
                    continue;
                }

                if (!empireToIndex.TryGetValue(empire, out var eIdx))
                {
                    eIdx = nextIndex++;
                    if (eIdx > 255) eIdx = (eIdx - 1) % 255 + 1;
                    empireToIndex[empire] = eIdx;
                    indexToEmpire[eIdx] = empire;
                }

                lut[idx] = (byte)eIdx;
            }

            return lut;
        }

        private static string? GetHolderAtYear(TitleHistory history, int year)
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

        public static SKImage BuildHolderPalette(Dictionary<int, string> indexToHolder)
        {
            var bmp = new SKBitmap(256, 1, SKColorType.Rgba8888, SKAlphaType.Opaque);
            var pixels = new byte[256 * 4];

            for (int i = 0; i < 256; i++)
            {
                int off = i * 4;
                if (i == 0 || !indexToHolder.ContainsKey(i))
                {
                    pixels[off] = 40;
                    pixels[off + 1] = 40;
                    pixels[off + 2] = 40;
                    pixels[off + 3] = 255;
                }
                else
                {
                    var (h, s, l) = HueSatLum(i);
                    var (r, g, b) = HslToRgb(h, s, l);
                    pixels[off] = r;
                    pixels[off + 1] = g;
                    pixels[off + 2] = b;
                    pixels[off + 3] = 255;
                }
            }

            Marshal.Copy(pixels, 0, bmp.GetPixels(), pixels.Length);
            return SKImage.FromBitmap(bmp);
        }

        public static SKImage BuildCountyPalette(Dictionary<int, string> indexToCounty)
        {
            return BuildHolderPalette(indexToCounty);
        }

        public static SKImage BuildDuchyPalette(Dictionary<int, string> indexToDuchy)
        {
            return BuildHolderPalette(indexToDuchy);
        }

        public static SKImage BuildKingdomPalette(Dictionary<int, string> indexToKingdom)
        {
            return BuildHolderPalette(indexToKingdom);
        }

        public static SKImage BuildEmpirePalette(Dictionary<int, string> indexToEmpire)
        {
            return BuildHolderPalette(indexToEmpire);
        }

        private static (float h, float s, float l) HueSatLum(int index)
        {
            float hue = (index * 137.508f) % 360f;
            float sat = 0.7f + (index % 3) * 0.1f;
            float lum = 0.5f + (index % 5) * 0.05f;
            return (hue, Math.Min(sat, 0.9f), Math.Min(lum, 0.75f));
        }

        private static (byte r, byte g, byte b) HslToRgb(float h, float s, float l)
        {
            float c = (1 - Math.Abs(2 * l - 1)) * s;
            float x = c * (1 - Math.Abs((h / 60f) % 2 - 1));
            float m = l - c / 2;
            float r, g, bl;

            if (h < 60) { r = c; g = x; bl = 0; }
            else if (h < 120) { r = x; g = c; bl = 0; }
            else if (h < 180) { r = 0; g = c; bl = x; }
            else if (h < 240) { r = 0; g = x; bl = c; }
            else if (h < 300) { r = x; g = 0; bl = c; }
            else { r = c; g = 0; bl = x; }

            return ((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((bl + m) * 255));
        }

        public void LoadLocalization()
        {
            string locDir = Path.Combine(_gameRoot, "localization");
            if (!Directory.Exists(locDir)) return;
            LocalizedNames.Clear();
            LoadLocalizationFromDir(locDir);
        }

        private void LoadLocalizationFromDir(string dir)
        {
            foreach (var langSubDir in Directory.EnumerateDirectories(dir))
            {
                string dirName = Path.GetFileName(langSubDir).ToLower();
                string? appLang = Ck3DirToAppLang(dirName);
                if (appLang == null) continue;

                if (!LocalizedNames.ContainsKey(appLang))
                    LocalizedNames[appLang] = new Dictionary<string, string>();

                foreach (var file in Directory.EnumerateFiles(langSubDir, "*.yml", SearchOption.AllDirectories))
                    ParseLocalizationYml(file, appLang);
            }
        }

        private void ParseLocalizationYml(string filePath, string appLang)
        {
            var dict = LocalizedNames[appLang];
            string? currentLang = null;

            foreach (var rawLine in File.ReadAllLines(filePath))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;

                if (line.StartsWith("l_") && line.EndsWith(":"))
                {
                    currentLang = line.Substring(0, line.Length - 1);
                    continue;
                }

                if (currentLang == null) continue;

                int colonIdx = line.IndexOf(':');
                if (colonIdx <= 0) continue;

                string key = line.Substring(0, colonIdx).Trim();
                if (string.IsNullOrEmpty(key)) continue;

                string valueStr = line.Substring(colonIdx + 1).Trim();
                if (string.IsNullOrEmpty(valueStr)) continue;

                int quoteStart = valueStr.IndexOf('"');
                string value;
                if (quoteStart >= 0)
                {
                    int quoteEnd = FindClosingQuote(valueStr, quoteStart);
                    if (quoteEnd > quoteStart)
                        value = valueStr.Substring(quoteStart + 1, quoteEnd - quoteStart - 1).Replace("\"\"", "\"");
                    else
                        continue;
                }
                else
                {
                    int commentIdx = valueStr.IndexOf('#');
                    value = commentIdx >= 0 ? valueStr.Substring(0, commentIdx).Trim() : valueStr;
                }

                if (!string.IsNullOrEmpty(value))
                    dict[key] = value;
            }
        }

        private static int FindClosingQuote(string text, int start)
        {
            for (int i = start + 1; i < text.Length; i++)
            {
                if (text[i] == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                        i++;
                    else
                        return i;
                }
            }
            return -1;
        }

        private static string? Ck3DirToAppLang(string dirName) => dirName switch
        {
            "english" => "en",
            "spanish" => "es",
            _ => null
        };

        public string GetDisplayName(string titleKey, string language)
        {
            if (LocalizedNames.TryGetValue(language, out var langDict) &&
                langDict.TryGetValue(titleKey, out var localized))
                return localized;

            if (language != "en" && LocalizedNames.TryGetValue("en", out var enDict) &&
                enDict.TryGetValue(titleKey, out var english))
                return english;

            if (TitleDisplayNames.TryGetValue(titleKey, out var name))
                return name;

            return titleKey;
        }
    }
}
