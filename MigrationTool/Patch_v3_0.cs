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

        List<string> fallbackFontPaths = new List<string>();
        bool changed = false;
        int fontIndex = 0;

        while (fontsIni.SectionExists($"Font{fontIndex}"))
        {
            string fontSection = $"Font{fontIndex}";

            if (fontsIni.KeyExists(fontSection, "Fallback"))
            {
                int fallbackIndex = fontsIni.GetIntValue(fontSection, "Fallback", -1);

                if (fallbackIndex < 0 || !fontsIni.SectionExists($"Font{fallbackIndex}"))
                {
                    Logger.Log($"{fontsIni.FilePath}: [{fontSection}] has an invalid Fallback value, removing the key.");
                }
                else
                {
                    string fallbackSection = $"Font{fallbackIndex}";
                    string fallbackType = fontsIni.GetStringValue(fallbackSection, "Type", "SpriteFont");
                    string fallbackPath = fontsIni.GetStringValue(fallbackSection, "Path", string.Empty);

                    if (!string.Equals(fallbackType, "TrueType", StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.Log($"{fontsIni.FilePath}: [{fontSection}] falls back to [{fallbackSection}] which is not a TrueType font, removing the key.");
                    }
                    else if (string.IsNullOrEmpty(fallbackPath))
                    {
                        Logger.Log($"{fontsIni.FilePath}: [{fontSection}] falls back to [{fallbackSection}] which has no Path, removing the key.");
                    }
                    else if (!fallbackFontPaths.Contains(fallbackPath))
                    {
                        fallbackFontPaths.Add(fallbackPath);
                    }
                }

                RemoveKeyWithLog(fontsIni, fontSection, "Fallback");
                changed = true;
            }

            fontIndex++;
        }

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
}
