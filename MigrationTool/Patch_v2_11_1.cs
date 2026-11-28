using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Rampastring.Tools;
using ClientCore.Display;

namespace MigrationTool;

internal class Patch_v2_11_1 : Patch
{
    public Patch_v2_11_1(string clientPath) : base(clientPath)
    {
    }

    public override ClientVersion PatchVersion => ClientVersion.V2_11_1;

    public override void Apply()
    {
        base.Apply();

        // 2.11.1 changed the meaning of RecommendedResolutions: the listed resolutions
        // now take part in integer scaling and must stay within the render size range.
        // Old INI files can contain resolutions that no longer fit, so filter the
        // existing list instead of replacing it. An empty list means no recommendation.
        IniFile clientDefsIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "ClientDefinitions.ini"));
        bool clientDefsChanged = MigrateRecommendedResolutions(clientDefsIni);

        if (clientDefsChanged)
            clientDefsIni.WriteIniFile();

        // Rename GameLobbyBase.ini->[SkirmishLobby]->BtnSaveLoadGameOptions to btnSaveLoadGameOptions
        Logger.Log("Update name from BtnSaveLoadGameOptions to btnSaveLoadGameOptions in GameLobbyBase.ini->[SkirmishLobby]");
        IniFile glb = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "GameLobbyBase.ini"));
        var presets = glb.GetSection("BtnSaveLoadGameOptions");

        if (presets == null)
            return;

        presets.SectionName = "btnSaveLoadGameOptions";

        foreach (var pair in glb.GetSection("SkirmishLobby").Keys)
        {
            if (pair.Value.Contains("BtnSaveLoadGameOptions"))
                glb.SetStringValue("SkirmishLobby", pair.Key, pair.Value.Replace("BtnSaveLoadGameOptions", "btnSaveLoadGameOptions"));
        }

        glb.WriteIniFile();

    }

    /// <summary>
    /// Removes the entries of <c>RecommendedResolutions</c> that are not valid for the
    /// client's render size range. The key is left untouched when it does not exist.
    /// </summary>
    /// <returns><see langword="true"/> if the file was changed.</returns>
    private static bool MigrateRecommendedResolutions(IniFile clientDefsIni)
    {
        const string section = "Settings";
        const string key = "RecommendedResolutions";

        if (!clientDefsIni.KeyExists(section, key))
            return false;

        ScreenResolution minimumRenderResolution = (
            clientDefsIni.GetIntValue(section, "MinimumRenderWidth", 1280),
            clientDefsIni.GetIntValue(section, "MinimumRenderHeight", 768));
        ScreenResolution maximumRenderResolution = (
            clientDefsIni.GetIntValue(section, "MaximumRenderWidth", 1280),
            clientDefsIni.GetIntValue(section, "MaximumRenderHeight", 800));

        string value = clientDefsIni.GetStringValue(section, key, string.Empty);
        List<ScreenResolution> keptResolutions = new();

        foreach (string entry in value.Split(','))
        {
            string trimmedEntry = entry.Trim();

            if (trimmedEntry.Length == 0)
                continue;

            if (!TryParseResolution(trimmedEntry, out ScreenResolution? resolution))
            {
                Logger.Log($"Update {clientDefsIni.FilePath}: Drop [{section}]->{key} entry \"{trimmedEntry}\", reason: not a WxH resolution");
                continue;
            }

            // Fits() checks that the resolution is not smaller than its argument.
            bool isWithinRenderRange = resolution.Fits(minimumRenderResolution)
                && maximumRenderResolution.Fits(resolution);

            if (!isWithinRenderRange)
            {
                Logger.Log($"Update {clientDefsIni.FilePath}: Drop [{section}]->{key} entry \"{trimmedEntry}\", "
                    + $"reason: outside the render range {minimumRenderResolution}..{maximumRenderResolution}");
                continue;
            }

            if (!keptResolutions.Contains(resolution))
                keptResolutions.Add(resolution);
        }

        string filteredValue = string.Join(",", keptResolutions);

        if (filteredValue == value)
            return false;

        Logger.Log($"Update {clientDefsIni.FilePath}: Set [{section}]->{key}={filteredValue}");

        clientDefsIni.SetStringValue(section, key, filteredValue);

        return true;
    }

    private static bool TryParseResolution(string value, [NotNullWhen(true)] out ScreenResolution? resolution)
    {
        resolution = null;

        string[] parts = value.Split('x');

        if (parts.Length != 2
            || !int.TryParse(parts[0], out int width)
            || !int.TryParse(parts[1], out int height))
        {
            return false;
        }

        resolution = (width, height);

        return true;
    }
}
