using System.Collections.Generic;

using Rampastring.Tools;

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

        int minWidth = clientDefsIni.GetIntValue(section, "MinimumRenderWidth", 1280);
        int minHeight = clientDefsIni.GetIntValue(section, "MinimumRenderHeight", 768);
        int maxWidth = clientDefsIni.GetIntValue(section, "MaximumRenderWidth", 1280);
        int maxHeight = clientDefsIni.GetIntValue(section, "MaximumRenderHeight", 800);

        string value = clientDefsIni.GetStringValue(section, key, string.Empty);
        List<string> keptResolutions = new();

        foreach (string entry in value.Split(','))
        {
            string trimmed = entry.Trim();

            if (trimmed.Length == 0)
                continue;

            string[] parts = trimmed.Split('x');

            if (parts.Length != 2
                || !int.TryParse(parts[0], out int width)
                || !int.TryParse(parts[1], out int height))
            {
                Logger.Log($"Update {clientDefsIni.FilePath}: Drop [{section}]->{key} entry \"{trimmed}\", reason: not a WxH resolution");
                continue;
            }

            if (width < minWidth || height < minHeight || width > maxWidth || height > maxHeight)
            {
                Logger.Log($"Update {clientDefsIni.FilePath}: Drop [{section}]->{key} entry \"{trimmed}\", "
                    + $"reason: outside the render range {minWidth}x{minHeight}..{maxWidth}x{maxHeight}");
                continue;
            }

            string resolution = $"{width}x{height}";

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
}
