using System;
using System.Collections.Generic;
using System.IO;

using Rampastring.Tools;

namespace MigrationTool;

internal class Patch_v3_0 : Patch
{
    public Patch_v3_0(string clientPath) : base(clientPath)
    {
        ClientVersion = Version.v3_0;
    }

    public override void Apply()
    {
        base.Apply();

        MigrateFontsIniFiles();

        // TODO: migate other v3.0 changes here
    }

    private void MigrateFontsIniFiles()
    {
        FileInfo[] fontsIniFiles = ClientDir.GetFiles("Fonts.ini", SearchOption.AllDirectories);

        if (fontsIniFiles.Length == 0)
        {
            Logger.Log("No Fonts.ini found, skipping the Fonts.ini migration.");
            return;
        }

        foreach (FileInfo fontsIniFile in fontsIniFiles)
            MigrateFontsIni(fontsIniFile);
    }

    private void MigrateFontsIni(FileInfo fontsIniFile)
    {
        IniFile fontsIni = new IniFile(fontsIniFile.FullName);

        if (fontsIni.SectionExists("FallbackFonts"))
        {
            Logger.Log($"{fontsIni.FilePath}: [FallbackFonts] already exists, skipping the Fonts.ini migration.");
            return;
        }

        List<string> fontSections = GetFontSections(fontsIni);
        List<string> sectionsWithFallback = new List<string>();
        List<List<string>> fallbackChains = new List<List<string>>();

        // Collect every chain before removing any Fallback keys. A chain can
        // point at another font that also has a Fallback key.
        foreach (string fontSection in fontSections)
        {
            if (!fontsIni.KeyExists(fontSection, "Fallback"))
                continue;

            sectionsWithFallback.Add(fontSection);
            int fallbackIndex = fontsIni.GetIntValue(fontSection, "Fallback", -1);
            List<string> chain = BuildFallbackChain(fontsIni, fontSection, fallbackIndex);

            if (chain.Count > 0)
                fallbackChains.Add(chain);
        }

        bool changed = false;

        foreach (string fontSection in sectionsWithFallback)
        {
            RemoveKeyWithLog(fontsIni, fontSection, "Fallback");
            changed = true;
        }

        List<string> fallbackFontPaths = MergeFallbackChains(fallbackChains, fontsIni.FilePath);

        if (fallbackFontPaths.Count > 0)
        {
            AddKeyWithLog(fontsIni, "FallbackFonts", "Count", fallbackFontPaths.Count.ToString());

            for (int i = 0; i < fallbackFontPaths.Count; i++)
                AddKeyWithLog(fontsIni, "FallbackFonts", $"Fallback{i}", fallbackFontPaths[i]);

            changed = true;
        }

        if (changed)
        {
            fontsIni.WriteIniFile();
            Logger.Log($"{fontsIni.FilePath}: Fonts.ini migration completed.");
        }
        else
        {
            Logger.Log($"{fontsIni.FilePath}: No per-font Fallback keys found, nothing to migrate.");
        }
    }

    private static List<string> GetFontSections(IniFile fontsIni)
    {
        List<string> fontSections = new List<string>();

        foreach (string section in fontsIni.GetSections())
        {
            if (section.Length <= 4 || !section.StartsWith("Font", StringComparison.Ordinal))
                continue;

            if (int.TryParse(section.Substring(4), out _))
                fontSections.Add(section);
        }

        fontSections.Sort((a, b) => GetFontIndex(a).CompareTo(GetFontIndex(b)));
        return fontSections;
    }

    private static int GetFontIndex(string fontSection)
    {
        if (!int.TryParse(fontSection.Substring(4), out int index))
            return -1;

        return index;
    }

    private static List<string> BuildFallbackChain(IniFile fontsIni, string sourceSection, int fallbackIndex)
    {
        List<string> paths = new List<string>();
        HashSet<int> visited = new HashSet<int>();
        int sourceIndex = GetFontIndex(sourceSection);
        int currentIndex = fallbackIndex;

        if (currentIndex < 0)
        {
            Logger.Log($"{fontsIni.FilePath}: [{sourceSection}] has an invalid Fallback value, removing the key.");
            return paths;
        }

        while (currentIndex >= 0)
        {
            string targetSection = $"Font{currentIndex}";

            if (currentIndex == sourceIndex)
            {
                Logger.Log($"{fontsIni.FilePath}: [{sourceSection}] fallback chain points back to itself, removing the key.");
                break;
            }

            if (!fontsIni.SectionExists(targetSection))
            {
                Logger.Log($"{fontsIni.FilePath}: [{sourceSection}] has an invalid Fallback value, removing the key.");
                break;
            }

            if (!visited.Add(currentIndex))
            {
                Logger.Log($"{fontsIni.FilePath}: [{sourceSection}] fallback chain contains a cycle at [{targetSection}], removing the key.");
                break;
            }

            string targetType = fontsIni.GetStringValue(targetSection, "Type", "SpriteFont");
            string targetPath = fontsIni.GetStringValue(targetSection, "Path", string.Empty);

            if (!string.Equals(targetType, "TrueType", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log($"{fontsIni.FilePath}: [{sourceSection}] falls back to [{targetSection}] which is not a TrueType font, removing the key.");
                break;
            }

            if (string.IsNullOrEmpty(targetPath))
            {
                Logger.Log($"{fontsIni.FilePath}: [{sourceSection}] falls back to [{targetSection}] which has no Path, removing the key.");
                break;
            }

            if (!paths.Contains(targetPath))
                paths.Add(targetPath);

            if (!fontsIni.KeyExists(targetSection, "Fallback"))
                break;

            currentIndex = fontsIni.GetIntValue(targetSection, "Fallback", -1);

            if (currentIndex < 0)
            {
                Logger.Log($"{fontsIni.FilePath}: [{targetSection}] has an invalid Fallback value, stopping the chain for [{sourceSection}].");
                break;
            }
        }

        return paths;
    }

    private static List<string> MergeFallbackChains(List<List<string>> chains, string filePath)
    {
        List<string> nodes = new List<string>();
        Dictionary<string, int> nodeIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
        Dictionary<string, HashSet<string>> edges = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (List<string> chain in chains)
        {
            foreach (string path in chain)
            {
                if (!nodeIndexes.ContainsKey(path))
                {
                    nodeIndexes.Add(path, nodes.Count);
                    nodes.Add(path);
                    edges.Add(path, new HashSet<string>(StringComparer.Ordinal));
                }
            }

            for (int i = 0; i < chain.Count - 1; i++)
            {
                string from = chain[i];
                string to = chain[i + 1];

                if (!string.Equals(from, to, StringComparison.Ordinal))
                    edges[from].Add(to);
            }
        }

        Dictionary<string, int> inDegree = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (string node in nodes)
            inDegree[node] = 0;

        foreach (HashSet<string> targets in edges.Values)
        {
            foreach (string target in targets)
                inDegree[target]++;
        }

        SortedSet<int> available = new SortedSet<int>();

        for (int i = 0; i < nodes.Count; i++)
        {
            if (inDegree[nodes[i]] == 0)
                available.Add(i);
        }

        List<string> ordered = new List<string>();

        while (available.Count > 0)
        {
            int index = available.Min;
            available.Remove(index);
            string node = nodes[index];
            ordered.Add(node);

            foreach (string target in edges[node])
            {
                inDegree[target]--;

                if (inDegree[target] == 0)
                    available.Add(nodeIndexes[target]);
            }
        }

        if (ordered.Count != nodes.Count)
        {
            List<string> remaining = new List<string>();

            foreach (string node in nodes)
            {
                if (!ordered.Contains(node))
                    remaining.Add(node);
            }

            // A global fallback list cannot satisfy contradictory per-font
            // chains. Keep the first-seen order and report the conflict.
            Logger.Log($"{filePath}: Conflicting font fallback precedence; cannot represent all chains globally. Keeping first-seen order for: {string.Join(", ", remaining)}");
            ordered.AddRange(remaining);
        }

        return ordered;
    }
}
