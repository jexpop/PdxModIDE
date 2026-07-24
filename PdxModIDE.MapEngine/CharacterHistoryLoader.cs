using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PdxModIDE.MapEngine
{
    public class CharacterInfo
    {
        public string Name { get; set; } = "";
        public string Dynasty { get; set; } = "";
    }

    public class CharacterHistoryLoader
    {
        private static readonly Regex CharBlockRe = new(@"^\s*(\d+)\s*=\s*\{");
        private static readonly Regex NameRe = new(@"name\s*=\s*""([^""]*)""");
        private static readonly Regex DynastyRe = new(@"dynasty\s*=\s*(\w+)");

        public Dictionary<string, CharacterInfo> AllCharacters { get; } = new();

        public int LoadAll(string gameRoot, bool overwriteDuplicates = false)
        {
            AllCharacters.Clear();

            string folder = Path.Combine(gameRoot, "history", "characters");
            if (!Directory.Exists(folder))
                return 0;

            foreach (string fname in Directory.EnumerateFiles(folder, "*.txt", SearchOption.AllDirectories))
            {
                var fileData = ParseCharacterHistoryFile(fname);
                foreach (var kvp in fileData)
                {
                    if (overwriteDuplicates || !AllCharacters.ContainsKey(kvp.Key))
                        AllCharacters[kvp.Key] = kvp.Value;
                }
            }

            return AllCharacters.Count;
        }

        public static Dictionary<string, CharacterInfo> ParseCharacterHistoryFile(string path)
        {
            var data = new Dictionary<string, CharacterInfo>();
            string? currentChar = null;
            int depth = 0;
            bool nameFound = false;

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

                if (depth == 0)
                {
                    var m = CharBlockRe.Match(line);
                    if (m.Success)
                    {
                        currentChar = m.Groups[1].Value;
                        if (!data.ContainsKey(currentChar))
                            data[currentChar] = new CharacterInfo();
                        nameFound = false;
                        depth = 0;

                        int opens = line.Count(c => c == '{');
                        int closes = line.Count(c => c == '}');
                        depth += opens - closes;

                        if (depth > 0)
                        {
                            var nm = NameRe.Match(line);
                            if (nm.Success)
                            {
                                data[currentChar].Name = nm.Groups[1].Value;
                                nameFound = true;
                            }
                            var dm = DynastyRe.Match(line);
                            if (dm.Success && string.IsNullOrEmpty(data[currentChar].Dynasty))
                                data[currentChar].Dynasty = dm.Groups[1].Value;
                        }

                        if (depth <= 0)
                        {
                            currentChar = null;
                            depth = 0;
                        }
                    }
                    continue;
                }

                if (currentChar == null)
                    continue;

                if (!nameFound)
                {
                    var nm = NameRe.Match(line);
                    if (nm.Success)
                    {
                        data[currentChar].Name = nm.Groups[1].Value;
                        nameFound = true;
                    }
                }

                if (string.IsNullOrEmpty(data[currentChar].Dynasty))
                {
                    var dm = DynastyRe.Match(line);
                    if (dm.Success)
                        data[currentChar].Dynasty = dm.Groups[1].Value;
                }

                int op = line.Count(c => c == '{');
                int cl = line.Count(c => c == '}');
                depth += op - cl;

                if (depth <= 0)
                {
                    currentChar = null;
                    depth = 0;
                    nameFound = false;
                }
            }

            return data;
        }

        public string? GetCharacterName(string characterId)
        {
            return AllCharacters.TryGetValue(characterId, out var info) ? info.Name : null;
        }

        public string? GetCharacterDynasty(string characterId)
        {
            return AllCharacters.TryGetValue(characterId, out var info) ? info.Dynasty : null;
        }
    }
}
