using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rampastring.Tools;

namespace MigrationTool;

internal class Patch_v2_12_0 : Patch
{
    public Patch_v2_12_0(string clientPath) : base(clientPath)
    {
        ClientVersion = Version.v2_12_0;
    }
    public override void Apply()
    {
        base.Apply();

        // And add ClientDefinitions.ini->[Settings]->ClientGameType
        IniFile clientDefsIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "ClientDefinitions.ini"));
        AddKeyWithLog(clientDefsIni, "Settings", "ClientGameType", Game.ToString());
        clientDefsIni.WriteIniFile();

        MigrateScrollRateTrackbar();
    }

    private void MigrateScrollRateTrackbar()
    {
        foreach (FileInfo iniFileInfo in ResouresDir.GetFiles("OptionsWindow.ini", SearchOption.AllDirectories))
        {
            IniFile ini = new IniFile(iniFileInfo.FullName, applyBaseIni: false);

            // trbClientVolume still configures AudioOptionsPanel, so copy its settings to trbScrollRate.
            if (!ini.SectionExists("trbClientVolume") || ini.SectionExists("trbScrollRate"))
                continue;

            Logger.Log($"Update {ini.FilePath}: Copy [trbClientVolume] to [trbScrollRate]");
            CopySectionWithLog(ini, "trbClientVolume", ini, "trbScrollRate");
            ini.WriteIniFile();
        }
    }
}
