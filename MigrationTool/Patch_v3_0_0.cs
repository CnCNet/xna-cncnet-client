using System;
using System.Collections.Generic;
using System.IO;

using Rampastring.Tools;

namespace MigrationTool;

internal class Patch_v3_0_0 : Patch
{
    private const int OLD_OPTIONS_WINDOW_WIDTH = 576;
    private const int OPTIONS_WINDOW_WIDTH_DELTA = 92;
    private const int NEW_OPTIONS_WINDOW_WIDTH = OLD_OPTIONS_WINDOW_WIDTH + OPTIONS_WINDOW_WIDTH_DELTA;
    private const int OPTIONS_WINDOW_SECOND_COLUMN_X = 276;

    public Patch_v3_0_0(string clientPath) : base(clientPath)
    {
        ClientVersion = Version.v3_0_0;
    }

    public override void Apply()
    {
        base.Apply();

        MigrateFontsIniFiles();
        MigrateOptionsWindowIni();
    }

    private void MigrateOptionsWindowIni()
    {
        string optionsWindowPath = SafePath.CombineFilePath(ResouresDir.FullName, "OptionsWindow.ini");

        if (!File.Exists(optionsWindowPath))
        {
            Logger.Log("No OptionsWindow.ini found, skipping the OptionsWindow.ini migration.");
            return;
        }

        // Only migrate sections that are actually defined in this file. The INI
        // system inherits from GenericWindow.ini, and writing inherited sections
        // back would duplicate the whole base configuration into OptionsWindow.ini.
        IniFile optionsWindowIni = new IniFile(optionsWindowPath, applyBaseIni: false);

        bool hasExplicitWidth = TryGetOptionsWindowWidth(optionsWindowIni, out int optionsWindowWidth);
        bool layoutWasBasedOnOldWidth = hasExplicitWidth && optionsWindowWidth == OLD_OPTIONS_WINDOW_WIDTH;
        bool layoutIsAlreadyMigrated = hasExplicitWidth && optionsWindowWidth == NEW_OPTIONS_WINDOW_WIDTH;

        bool changed = UpdateOptionsWindowWidth(optionsWindowIni);

        // When the INI specifies the old window width, it also describes a layout
        // that was designed for that width, so shift its second column as well.
        // Otherwise only shift controls that clearly ended at the right edge.
        changed |= ShiftControls(optionsWindowIni, layoutWasBasedOnOldWidth, layoutIsAlreadyMigrated);

        if (changed)
            optionsWindowIni.WriteIniFile();

        Logger.Log("OptionsWindow.ini: Check labels, buttons, list boxes and drop-downs for clipping or misalignment after upgrading.");
    }

    private static bool TryGetOptionsWindowWidth(IniFile optionsWindowIni, out int width)
    {
        if (optionsWindowIni.KeyExists("OptionsWindow", "Width"))
            return int.TryParse(optionsWindowIni.GetStringValue("OptionsWindow", "Width", string.Empty), out width);

        if (!optionsWindowIni.KeyExists("OptionsWindow", "Size"))
        {
            width = 0;
            return false;
        }

        string[] size = optionsWindowIni.GetStringValue("OptionsWindow", "Size", string.Empty).Split(',');

        if (size.Length < 1)
        {
            width = 0;
            return false;
        }

        return int.TryParse(size[0].Trim(), out width);
    }

    private bool UpdateOptionsWindowWidth(IniFile optionsWindowIni)
    {
        bool changed = false;

        if (optionsWindowIni.KeyExists("OptionsWindow", "Width"))
        {
            int width = optionsWindowIni.GetIntValue("OptionsWindow", "Width", 0);

            if (width == OLD_OPTIONS_WINDOW_WIDTH)
            {
                optionsWindowIni.SetIntValue("OptionsWindow", "Width", NEW_OPTIONS_WINDOW_WIDTH);
                Logger.Log($"Update {optionsWindowIni.FilePath}: [OptionsWindow]->Width={OLD_OPTIONS_WINDOW_WIDTH} to {NEW_OPTIONS_WINDOW_WIDTH}");
                changed = true;
            }
            else if (width != NEW_OPTIONS_WINDOW_WIDTH)
            {
                Logger.Log($"Update {optionsWindowIni.FilePath}: Skip changing [OptionsWindow]->Width, reason: custom value {width}");
            }
        }

        if (optionsWindowIni.KeyExists("OptionsWindow", "Size"))
        {
            string[] size = optionsWindowIni.GetStringValue("OptionsWindow", "Size", string.Empty).Split(',');

            if (size.Length >= 1 && int.TryParse(size[0].Trim(), out int width))
            {
                if (width == OLD_OPTIONS_WINDOW_WIDTH)
                {
                    size[0] = NEW_OPTIONS_WINDOW_WIDTH.ToString();
                    optionsWindowIni.SetStringValue("OptionsWindow", "Size", string.Join(",", size));
                    Logger.Log($"Update {optionsWindowIni.FilePath}: [OptionsWindow]->Size width from {OLD_OPTIONS_WINDOW_WIDTH} to {NEW_OPTIONS_WINDOW_WIDTH}");
                    changed = true;
                }
                else if (width != NEW_OPTIONS_WINDOW_WIDTH)
                {
                    Logger.Log($"Update {optionsWindowIni.FilePath}: Skip changing [OptionsWindow]->Size, reason: custom width {width}");
                }
            }
        }

        return changed;
    }

    private bool ShiftControls(IniFile optionsWindowIni, bool shiftSecondColumn, bool layoutIsAlreadyMigrated)
    {
        if (layoutIsAlreadyMigrated)
            return false;

        int shiftedControls = 0;

        foreach (string section in optionsWindowIni.GetSections())
        {
            if (section == "INISystem" || section == "OptionsWindow")
                continue;

            if (!TryGetControlX(optionsWindowIni, section, out int x))
                continue;

            bool hasWidth = TryGetControlWidth(optionsWindowIni, section, out int width);
            int right = hasWidth ? x + width : x;

            bool isAtRightEdge = right >= OLD_OPTIONS_WINDOW_WIDTH - OPTIONS_WINDOW_WIDTH_DELTA &&
                right < OLD_OPTIONS_WINDOW_WIDTH;
            bool isInSecondColumn = shiftSecondColumn &&
                x >= OPTIONS_WINDOW_SECOND_COLUMN_X &&
                x < OLD_OPTIONS_WINDOW_WIDTH;

            if (!isAtRightEdge && !isInSecondColumn)
                continue;

            int newX = x + OPTIONS_WINDOW_WIDTH_DELTA;
            SetControlX(optionsWindowIni, section, newX);
            Logger.Log($"Update {optionsWindowIni.FilePath}: Shift control [{section}] from X={x} to X={newX}");
            shiftedControls++;
        }

        if (shiftedControls > 0)
            Logger.Log($"Update {optionsWindowIni.FilePath}: Shifted {shiftedControls} control(s) for the new Options window width.");

        return shiftedControls > 0;
    }

    private static bool TryGetControlX(IniFile iniFile, string section, out int x)
    {
        if (iniFile.KeyExists(section, "X"))
            return int.TryParse(iniFile.GetStringValue(section, "X", string.Empty), out x);

        if (!iniFile.KeyExists(section, "Location"))
        {
            x = 0;
            return false;
        }

        string[] location = iniFile.GetStringValue(section, "Location", string.Empty).Split(',');

        if (location.Length < 1)
        {
            x = 0;
            return false;
        }

        return int.TryParse(location[0].Trim(), out x);
    }

    private static bool TryGetControlWidth(IniFile iniFile, string section, out int width)
    {
        if (iniFile.KeyExists(section, "Width"))
            return int.TryParse(iniFile.GetStringValue(section, "Width", string.Empty), out width);

        if (!iniFile.KeyExists(section, "Size"))
        {
            width = 0;
            return false;
        }

        string[] size = iniFile.GetStringValue(section, "Size", string.Empty).Split(',');

        if (size.Length < 1)
        {
            width = 0;
            return false;
        }

        return int.TryParse(size[0].Trim(), out width);
    }

    private static void SetControlX(IniFile iniFile, string section, int x)
    {
        if (iniFile.KeyExists(section, "X"))
        {
            iniFile.SetIntValue(section, "X", x);
            return;
        }

        string[] location = iniFile.GetStringValue(section, "Location", string.Empty).Split(',');
        location[0] = x.ToString();
        iniFile.SetStringValue(section, "Location", string.Join(",", location));
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

        bool changed = RemoveObsoleteFontsCount(fontsIni);
        bool hasFallbackFontsSection = fontsIni.SectionExists("FallbackFonts");

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

            if (hasFallbackFontsSection)
                continue;

            int fallbackIndex = fontsIni.GetIntValue(fontSection, "Fallback", -1);
            List<string> chain = BuildFallbackChain(fontsIni, fontSection, fallbackIndex);

            if (chain.Count > 0)
                fallbackChains.Add(chain);
        }

        foreach (string fontSection in sectionsWithFallback)
        {
            RemoveKeyWithLog(fontsIni, fontSection, "Fallback");
            changed = true;
        }

        if (hasFallbackFontsSection)
        {
            changed |= UpdateFallbackFontsCount(fontsIni);

            if (changed)
            {
                fontsIni.WriteIniFile();
                Logger.Log($"{fontsIni.FilePath}: Fonts.ini migration completed.");
            }
            else
            {
                Logger.Log($"{fontsIni.FilePath}: [FallbackFonts] already exists, nothing to migrate.");
            }

            return;
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

    private static bool UpdateFallbackFontsCount(IniFile fontsIni)
    {
        int maxFallbackIndex = -1;

        foreach (string key in fontsIni.GetSectionKeys("FallbackFonts"))
        {
            if (!key.StartsWith("Fallback", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!int.TryParse(key.Substring("Fallback".Length), out int index) || index < 0)
                continue;

            maxFallbackIndex = Math.Max(maxFallbackIndex, index);
        }

        int expectedCount = maxFallbackIndex + 1;
        int currentCount = fontsIni.GetIntValue("FallbackFonts", "Count", -1);

        if (currentCount == expectedCount)
            return false;

        fontsIni.SetIntValue("FallbackFonts", "Count", expectedCount);
        Logger.Log($"Update {fontsIni.FilePath}: [FallbackFonts]->Count={currentCount} to {expectedCount}");
        return true;
    }

    private bool RemoveObsoleteFontsCount(IniFile fontsIni)
    {
        if (!fontsIni.KeyExists("Fonts", "Count"))
            return false;

        RemoveKeyWithLog(fontsIni, "Fonts", "Count");

        if (fontsIni.GetSectionKeys("Fonts").Count == 0)
        {
            fontsIni.RemoveSection("Fonts");
            Logger.Log($"Update {fontsIni.FilePath}: Remove empty [Fonts] section");
        }

        return true;
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
            Logger.Log($"{filePath}: Conflicting font fallback precedence. Cannot represent all chains globally. Keeping first-seen order for: {string.Join(", ", remaining)}");
            ordered.AddRange(remaining);
        }

        return ordered;
    }
}
