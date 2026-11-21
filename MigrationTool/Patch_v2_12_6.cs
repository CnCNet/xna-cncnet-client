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

        // Remove GenericWindow.ini->{ GameCreationWindow; GameCreationWindow_Advanced }->Size
        IniFile genericWindowIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "GenericWindow.ini"));
        bool changed = RemoveKeyWithLog(genericWindowIni, "GameCreationWindow", "Size");
        changed |= RemoveKeyWithLog(genericWindowIni, "GameCreationWindow_Advanced", "Size");

        if (changed)
            genericWindowIni.WriteIniFile();
    }
}

