using Rampastring.Tools;

namespace MigrationTool;

internal class Patch_v2_11_1 : Patch
{
    public Patch_v2_11_1 (string clientPath) : base (clientPath)
    {
        ClientVersion = Version.v2_11_1;
    }

    public override Patch Apply()
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

        return this;
    }
}
