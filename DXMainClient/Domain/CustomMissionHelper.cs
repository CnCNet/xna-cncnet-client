using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Domain;
internal static class CustomMissionHelper
{

    public static void DeleteSupplementalMissionFiles()
    {
        DirectoryInfo gameDirectory = SafePath.GetDirectory(ProgramConstants.GamePath);
        foreach (string filename in new string[]
        {
                ClientConfiguration.Instance.CustomMissionCsfName,
                ClientConfiguration.Instance.CustomMissionPalName,
                ClientConfiguration.Instance.CustomMissionShpName,
        })
        {
            gameDirectory.EnumerateFiles(filename).SingleOrDefault()?.Delete();
        }
    }

    public static void CopySupplementalMissionFiles(Mission mission)
    {
        DeleteSupplementalMissionFiles();

        if (mission.IsCustomMission)
        {
            string missionFileName = mission.Scenario;
            Debug.Assert(missionFileName.EndsWith(".map", StringComparison.InvariantCultureIgnoreCase), "Mission file should have the extension \".map\".");

            // copy the CSF file if exists
            foreach ((string ext, string filename) in new (string, string)[]
            {
                    ("csf", ClientConfiguration.Instance.CustomMissionCsfName),
                    ("pal", ClientConfiguration.Instance.CustomMissionPalName),
                    ("shp", ClientConfiguration.Instance.CustomMissionShpName),
            })
            {
                string sourceFileName = missionFileName[..^".map".Length] + "." + ext;
                if (SafePath.GetFile(SafePath.CombineFilePath(ProgramConstants.GamePath, sourceFileName)).Exists)
                    File.Copy(SafePath.CombineFilePath(ProgramConstants.GamePath, sourceFileName), SafePath.CombineFilePath(ProgramConstants.GamePath, filename));
            }
        }
    }
}