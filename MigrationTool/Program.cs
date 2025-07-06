using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Rampastring.Tools;

namespace MigrationTool;

public enum Version
{
    v2_11_0,
    v2_11_1,
    v2_11_2,
    v2_12_1,
    Latest,
}

public enum ClientGameType
{
    TS,
    YR,
    Ares,
}

internal sealed class Program
{
    private const string errMsg = "Unknown arguments detected. Use -h argument to print help information.";
    private const string helpMsg =
        """
        CnCNet Client Migration Tool.
        
        Execute this file with path to the unmigrated client directory as first argument.
        """;

    private static void Main(string[] args)
    {
        // Initialize logger
        DirectoryInfo baseDirectory = SafePath.GetDirectory(Directory.GetCurrentDirectory());
        FileInfo logFile = SafePath.GetFile(SafePath.CombineFilePath(baseDirectory.FullName, "MigrationTool.log"));
        Logger.Initialize(logFile.DirectoryName, logFile.Name);
        Logger.WriteLogFile = true;
        Logger.WriteToConsole = false;
        Logger.Log("CnCNet Client Migration Tool");
        Logger.Log("Version: " + GitVersionInformation.AssemblySemVer);
        Logger.WriteToConsole = true;

        // Check arguments
        switch (args.Length)
        {
            case 1:
                string arg = args[0].Trim();

                if (arg == "-h"
                    || arg == "--help"
                    || arg == "-?"
                    || arg == "/?"
                    || arg == "/h")
                {
                    Console.WriteLine(helpMsg);
                    return;
                }

                if (!SafePath.GetDirectory(arg).Exists)
                {
                    Console.WriteLine(errMsg);
                    return;
                }

                if (!SafePath.GetFile(SafePath.CombineFilePath(arg, "Resources", "ClientDefinitions.ini")).Exists)
                {
                    Logger.Log("Unable to find Resources/ClientDefinitions.ini. Migration aborted.");
                    return;
                }

                Patch patch = null;
                try
                {
                    patch = new Patch_v2_11_0(arg).Apply(); Console.WriteLine("");
                    patch = new Patch_v2_11_1(arg).Apply(); Console.WriteLine("");
                    patch = new Patch_v2_11_2(arg).Apply(); Console.WriteLine("");
                    patch = new Patch_v2_12_1(arg).Apply(); Console.WriteLine("");
                    patch = new Patch_Latest(arg).Apply();
                }
                catch (Exception ex)
                {
                    Logger.Log("");
                    Logger.Log($"Unable to apply migration patch for client version {patch.ClientVersion.ToString().Replace('_', '.')} due to internal error. Message: {ex.Message}");
                    Logger.Log("Migration to the latest client version has been failed");
                }

                break;
            case 0:
            default:
                Console.WriteLine(errMsg);
                break;
        }
    }
}
