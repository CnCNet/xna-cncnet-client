using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.VisualBasic.FileIO;

using Rampastring.Tools;

namespace MigrationTool;

internal sealed class Program
{
    private const string ERROR_MESSAGE = "Unknown arguments detected. Use -h argument to print help information.";
    private const string HELP_MESSAGE =
        """
        CnCNet Client Migration Tool
        Experimental utility for migrating CnCNet client configuration files.

        Usage:
            MigrationTool <client-directory>
            MigrationTool <client-directory> [-p <version>]
            MigrationTool <client-directory> [--patch <version>]
            MigrationTool --patch-list
            MigrationTool -h

        Arguments:
            <client-directory>     Path to the client directory.

        Options:
            -p, --patch <version>  Apply only the specified migration patch.
                                   If omitted, all configured patches are applied
                                   in sequence.

                                   Dots and underscores are accepted in version names.
                                   A leading 'v' is optional.
                                   The specified patch must be included in this build.

            --patch-list           Show list of available client versions to use.

            -h, --help, -?, /?, /h
                                   Display this help.

        Notes:
            - MigrationTool.log is written to the current working directory.
            - Before applying patches, the tool creates <client-directory>_backup.
            - If that backup directory already exists, it is kept without updating it.

        Examples:
            MigrationTool.exe "..\Games\Client"
            MigrationTool.exe "C:\Path\To\Client" -p 2.11.0
            ./MigrationTool "/home/user/games/clientConfig"
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
        Logger.Log("Warning: Migration Tool is an experimental application.");
        Logger.Log("Version: " + GitVersionInformation.AssemblySemVer);
        Logger.WriteToConsole = true;

        // Check arguments
        switch (args.Length)
        {
            case 1:
            case 3:
                string arg = args[0].Trim();

                if (arg is "-h"
                    or "--help"
                    or "-?"
                    or "/?"
                    or "/h")
                {
                    Console.WriteLine(HELP_MESSAGE);
                    return;
                }

                if (arg is "--patch-list")
                {
                    StringBuilder sb = new();
                    var patchNames = Enum.GetValues(typeof(Version));
                    for (int i = 0; i < patchNames.Length; i++)
                    {
                        sb.Append(patchNames.GetValue(i).ToString().Replace('_', '.') + (i == (patchNames.Length - 1) ? " " : ", "));
                    }

                    Console.WriteLine("List of available versions: " + sb.ToString());

                    return;
                }

                var clientDir = args[0];

                if (!SafePath.GetDirectory(clientDir).Exists)
                {
                    Logger.Log($"Unable to find \"{clientDir}\" folder. Migration halted.");
                    return;
                }

                if (!SafePath.GetFile(SafePath.CombineFilePath(clientDir, "Resources", "ClientDefinitions.ini")).Exists)
                {
                    Logger.Log($"Unable to find \"{clientDir}\\Resources\\ClientDefinitions.ini\". Migration halted.");
                    return;
                }

                string? versionName = null;
                if (args.Length == 3 && (args[1] == "--patch" || args[1] == "-p"))
                {
                    versionName = args[2].Trim().ToLower();
                }
                else if (args.Length > 1)
                {
                    Console.WriteLine(HELP_MESSAGE);
                    return;
                }

                Logger.Log("Warning: Running the migration tool will back up your folder. However, if a backup already exists, a new one will not be created.");
                Logger.Log("Press any key to begin migration.");
                Console.ReadKey();

                if (!Directory.Exists(clientDir + "_backup"))
                {
                    Logger.Log($"Creating backup: {clientDir + "_backup"}");
                    FileSystem.CopyDirectory(clientDir, clientDir + "_backup");
                    Logger.Log("Backup created.");
                }

                PatchConfigs(clientDir, versionName);

                break;
            case 0:
            default:
                Console.WriteLine(ERROR_MESSAGE);
                break;
        }
    }

    private static void PatchConfigs(string clientDir, string? versionName = null)
    {
        var assembly = Assembly.GetExecutingAssembly();
        Patch? patch = null;
        try
        {
            // https://stackoverflow.com/questions/16038819/how-to-find-all-direct-subclasses-of-a-class-with-net-reflection
            var patches = assembly.GetTypes().Where(type => type.BaseType == typeof(Patch)).ToList();
            var patchNames = Enum.GetValues(typeof(Version));

            if (!string.IsNullOrWhiteSpace(versionName))
            {
                versionName = versionName?.Replace('.', '_');
                versionName = versionName.StartsWith("v") ? versionName : "v" + versionName;
                Type type = patches.Where(t => t.FullName == "MigrationTool.Patch_" + versionName).First();
                patch = (Patch?)Activator.CreateInstance(type, clientDir);
                patch?.Apply();
                Console.WriteLine("");
            }
            else
            {
                foreach (var patchName in patchNames)
                {
                    Type type = patches.Where(t => t.FullName == "MigrationTool.Patch_" + patchName.ToString()).First();
                    patch = (Patch?)Activator.CreateInstance(type, clientDir);
                    patch?.Apply();
                    Console.WriteLine("");
                }
            }

            Console.WriteLine("Patching has been done.");
        }
        catch (Exception ex)
        {
            Logger.Log("");
            Logger.Log(string.Format("Unable to apply migration patch for client version {0} due to an internal error. Message: {1}",
                patch == null ? versionName : patch?.ClientVersion.ToString().Replace('_', '.'),
                ex.ToString()));
            Logger.Log(string.Format("Migration to the {0} client version has been failed", string.IsNullOrWhiteSpace(versionName) ? "latest" : versionName));
        }
    }
}
