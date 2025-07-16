using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Rampastring.Tools;
using ClientCore.Enums;
using ClientCore.Extensions;

namespace MigrationTool;

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

                if (arg is "-h"
                    or "--help"
                    or "-?"
                    or "/?"
                    or "/h")
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

                var assembly = Assembly.GetExecutingAssembly();
                Patch? patch = null;
                try
                {
                    // https://stackoverflow.com/questions/16038819/how-to-find-all-direct-subclasses-of-a-class-with-net-reflection
                    var patches = assembly.GetTypes().Where(type => type.BaseType == typeof(Patch)).ToList();
                    var patchNames = Enum.GetValues(typeof(Version));
                    foreach (var patchName in patchNames)
                    {
                        Type type = patches.Where(t => t.FullName == "MigrationTool.Patch_" + patchName.ToString()).First();
                        patch = (Patch)Activator.CreateInstance(type, arg);
                        patch?.Apply();
                        Console.WriteLine("");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("");
                    Logger.Log($"Unable to apply migration patch for client version {patch?.ClientVersion.ToString().Replace('_', '.')} due to an internal error. Message: {ex.ToString()}");
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
