using Rampastring.Tools;

namespace MigrationTool;

internal class Patch_v2_11_1 : Patch
{
    public Patch_v2_11_1(string clientPath) : base(clientPath)
    {
        ClientVersion = Version.v2_11_1;
    }

    public override void Apply()
    {
        base.Apply();

        // Add ClientDefinitions.ini->[Settings]->RecommendedResolutions, MaximumRenderWidth, MaximumRenderHeight
        IniFile clientDefsIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "ClientDefinitions.ini"));
        AddKeyWithLog(clientDefsIni, "Settings", "MaximumRenderWidth", "1280");
        AddKeyWithLog(clientDefsIni, "Settings", "MaximumRenderHeight", "720");
        var width = clientDefsIni.GetStringValue("Settings", "MaximumRenderWidth", "1280");
        var height = clientDefsIni.GetStringValue("Settings", "MaximumRenderHeight", "720");
        AddKeyWithLog(clientDefsIni, "Settings", "RecommendedResolutions", $"{width}x{height}");
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
}
