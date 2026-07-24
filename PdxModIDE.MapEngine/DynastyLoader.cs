using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PdxModIDE.MapEngine
{
    public class DynastyLoader
    {
        private static readonly Regex DynastyBlockRe = new(@"^\s*((?:dynn_\w+|\d+))\s*=\s*\{");
        private static readonly Regex NameRe = new(@"name\s*=\s*""([^""]*)""");

        public Dictionary<string, string> AllDynasties { get; } = new();

        public int LoadAll(string gameRoot, bool overwriteDuplicates = false)
        {
            AllDynasties.Clear();

            string folder = Path.Combine(gameRoot, "common", "dynasties");
            if (!Directory.Exists(folder))
                return 0;

            foreach (string fname in Directory.EnumerateFiles(folder, "*.txt", SearchOption.AllDirectories))
            {
                var fileData = ParseDynastyFile(fname);
                foreach (var kvp in fileData)
                {
                    if (overwriteDuplicates || !AllDynasties.ContainsKey(kvp.Key))
                        AllDynasties[kvp.Key] = kvp.Value;
                }
            }

            return AllDynasties.Count;
        }

        public static Dictionary<string, string> ParseDynastyFile(string path)
        {
            var data = new Dictionary<string, string>();
            string? currentDynasty = null;
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
                    var m = DynastyBlockRe.Match(line);
                    if (m.Success)
                    {
                        currentDynasty = m.Groups[1].Value;
                        nameFound = false;
                        depth = 0;

                        int opens = line.Count(c => c == '{');
                        int closes = line.Count(c => c == '}');
                        depth += opens - closes;

                        if (depth > 0 && !nameFound)
                        {
                            var nm = NameRe.Match(line);
                            if (nm.Success)
                            {
                                data[currentDynasty] = nm.Groups[1].Value;
                                nameFound = true;
                            }
                        }

                        if (depth <= 0)
                        {
                            currentDynasty = null;
                            depth = 0;
                        }
                    }
                    continue;
                }

                if (currentDynasty == null)
                    continue;

                if (!nameFound)
                {
                    var nm = NameRe.Match(line);
                    if (nm.Success)
                    {
                        data[currentDynasty] = nm.Groups[1].Value;
                        nameFound = true;
                    }
                }

                int op = line.Count(c => c == '{');
                int cl = line.Count(c => c == '}');
                depth += op - cl;

                if (depth <= 0)
                {
                    currentDynasty = null;
                    depth = 0;
                    nameFound = false;
                }
            }

            return data;
        }

        public string? GetDynastyName(string dynastyId)
        {
            return AllDynasties.TryGetValue(dynastyId, out var name) ? name : null;
        }
    }
}
