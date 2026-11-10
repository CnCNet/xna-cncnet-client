using System.IO;
using System.Linq;

using Rampastring.Tools;

namespace MigrationTool;

internal class Patch_v2_12_6 : Patch
{
    public Patch_v2_12_6(string clientPath) : base(clientPath)
    {
        ClientVersion = Version.v2_12_6;
    }

    public override void Apply()
    {
        base.Apply();

        // Add GameLobbyBase.ini->[ddPlayerColorX]->ItemsDrawMode
        string gameLobbyBasePath = SafePath.CombineFilePath(ResouresDir.FullName, "GameLobbyBase.ini");

        if (File.Exists(gameLobbyBasePath))
        {
            IniFile gmLobbyBaseIni = new IniFile(gameLobbyBasePath);
            string ddPlayerColor = nameof(ddPlayerColor);
            Enumerable.Range(0, 8).ToList().ForEach(i => AddKeyWithLog(gmLobbyBaseIni, ddPlayerColor + i, "ItemsDrawMode", "Text"));
            gmLobbyBaseIni.WriteIniFile();
        }
        else
        {
            Logger.Log("No GameLobbyBase.ini found, skipping the player color dropdown migration.");
        }

        // Remove GenericWindow.ini->{ GameCreationWindow; GameCreationWindow_Advanced }->Size
        IniFile genericWindowIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "GenericWindow.ini"));
        RemoveKeyWithLog(genericWindowIni, "GameCreationWindow", "Size");
        RemoveKeyWithLog(genericWindowIni, "GameCreationWindow_Advanced", "Size");
        genericWindowIni.WriteIniFile();
    }
}

