using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rampastring.Tools;

namespace MigrationTool;

internal class Patch_v2_12_1 : Patch
{
    public Patch_v2_12_1(string clientPath) : base(clientPath)
    {
        ClientVersion = Version.v2_12_1;
    }
    public override Patch Apply()
    {
        base.Apply();

        // And add ClientDefinitions.ini->[Settings]->ClientGameType
        IniFile clientDefsIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "ClientDefinitions.ini"));
        AddKeyWithLog(clientDefsIni, "Settings", "ClientGameType", Game.ToString());
        clientDefsIni.WriteIniFile();

        return this;
    }
}
