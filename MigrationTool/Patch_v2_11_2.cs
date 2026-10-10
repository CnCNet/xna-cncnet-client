using System.Collections.Generic;
using System.IO;
using System.Linq;

using Rampastring.Tools;

namespace MigrationTool;

internal class Patch_v2_11_2 : Patch
{
    public Patch_v2_11_2(string clientPath) : base(clientPath)
    {
        ClientVersion = Version.v2_11_2;
    }

    public override void Apply()
    {
        base.Apply();

        // Remove ClientUpdater.xml and SecondStageUpdater.xml
        IniFile clientDefsIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "ClientDefinitions.ini"));
        var listExtraXMLs = new List<string>(2) { "ClientUpdater.xml", "SecondStageUpdater.xml" };
        Logger.Log("Remove ClientUpdater.xml and SecondStageUpdater.xml");

        foreach (var extraXml in listExtraXMLs)
        {
            Directory.GetFiles(ResouresDir.FullName, extraXml, SearchOption.AllDirectories)
                .ToList()
                .ForEach(elem => SafePath.DeleteFileIfExists(elem));
        }

        // Add ClientDefinitions.ini->[Settings]->ShowDevelopmentBuildWarnings
        AddKeyWithLog(clientDefsIni, "Settings", "ShowDevelopmentBuildWarnings", "true");
        clientDefsIni.WriteIniFile();

    }
}
