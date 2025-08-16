#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClientCore;
using ClientCore.Extensions;

using Rampastring.Tools;

namespace DTAClient.Domain;
internal static class CustomMissionHelper
{
    public static List<(string extension, string filename)>? CustomMissionSupplementDefinition { get; private set; }

    private static bool IsValidExtension(string extension) => extension == extension.ToWin32FileName() && extension.IndexOfAny(new char[] { '.', ' ' }) == -1;

    private static bool IsValidFileName(string filename) => filename == filename.ToWin32FileName();

    public static void Initialize()
    {
        CustomMissionSupplementDefinition = GetCustomMissionSupplementDefinition();
    }

    public static List<(string extension, string filename)> GetCustomMissionSupplementDefinition()
    {
        string rawDefinition = ClientConfiguration.Instance.CustomMissionSupplementDefinition;
        string[] definitionItems = rawDefinition.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        int fileCount = definitionItems.Length / 2;

        HashSet<string> extensions = [];

        List<(string extension, string filename)> ret = [];

        for (int i = 0; i < fileCount; i++)
        {
            string extension = definitionItems[2 * i];
            string filename = definitionItems[2 * i + 1];

            if (!IsValidExtension(extension))
            {
                throw new Exception(string.Format("Invalid extension {0}", extension));
            }

            if (!IsValidFileName(filename))
            {
                throw new Exception(string.Format("Invalid file name {0}", filename));
            }

            if (extensions.Contains(extension))
            {
                throw new Exception(string.Format("Extension {0} already exists", extension));
            }

            extensions.Add(extension);

            ret.Add((extension, filename));
        }

        return ret;
    }

    public static void DeleteSupplementalMissionFiles()
    {
        Debug.Assert(CustomMissionSupplementDefinition != null, "CustomMissionHelper must be initialized.");

        IEnumerable<string> filenames = CustomMissionSupplementDefinition.Select(def => def.filename);
        DirectoryInfo gameDirectory = SafePath.GetDirectory(ProgramConstants.GamePath);
        foreach (string filename in filenames)
        {
            FileInfo? fileInfo = gameDirectory.EnumerateFiles(filename).SingleOrDefault();
            if (fileInfo?.Exists ?? false)
            {
                fileInfo.IsReadOnly = false;
                fileInfo.Delete();
            }
        }
    }

    public static void CopySupplementalMissionFiles(Mission mission)
    {
        Debug.Assert(CustomMissionSupplementDefinition != null, "CustomMissionHelper must be initialized.");

        DeleteSupplementalMissionFiles();

        if (mission.IsCustomMission)
        {
            string mapExtension = "." + ClientConfiguration.Instance.MapFileExtension; // e.g., ".map"

            string missionFileName = mission.Scenario;
            Debug.Assert(missionFileName.EndsWith(mapExtension, StringComparison.InvariantCultureIgnoreCase), string.Format("Mission file should have the extension \"{0}\".", mapExtension));

            // copy the CSF file if exists
            foreach ((string ext, string filename) in CustomMissionSupplementDefinition!)
            {
                string sourceFileName = missionFileName[..^mapExtension.Length] + "." + ext;
                string sourceFilePath = SafePath.CombineFilePath(ProgramConstants.GamePath, sourceFileName);
                if (SafePath.GetFile(sourceFilePath).Exists)
                {
                    string targetFilePath = SafePath.CombineFilePath(ProgramConstants.GamePath, filename);

                    FileHelper.CreateHardLinkFromSource(sourceFilePath, targetFilePath);
                    new FileInfo(targetFilePath).IsReadOnly = true;
                }
            }
        }
    }
}