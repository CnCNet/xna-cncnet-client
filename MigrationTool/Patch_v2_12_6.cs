using System.Linq;

using Rampastring.Tools;

namespace MigrationTool;

internal class Patch_v2_12_6 : Patch
{
    public Patch_v2_12_6(string clientPath) : base(clientPath)
    {
        ClientVersion = Version.v2_12_6;
    }

    public override Patch Apply()
    {
        base.Apply();

        // Add GameLobbyBase.ini->[ddPlayerColorX]->ItemsDrawMode
        IniFile gmLobbyBaseIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "GameLobbyBase.ini"));
        string ddPlayerColor = nameof(ddPlayerColor);
        Enumerable.Range(0, 8).ToList().ForEach(i => AddKeyWithLog(gmLobbyBaseIni, ddPlayerColor + i, "ItemsDrawMode", "Text"));
        gmLobbyBaseIni.WriteIniFile();

        return this;
    }
}

