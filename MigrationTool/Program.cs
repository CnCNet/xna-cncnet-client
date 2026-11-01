using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.VisualBasic.FileIO;

using Rampastring.Tools;

namespace MigrationTool;

internal sealed class Program
{
    private const string ERROR_MESSAGE = "Unknown arguments detected. Use -h argument to print help information.";
    private const string HELP_MESSAGE =
        """
        CnCNet Client Migration Tool.
        
        Run this tool using the path to the unmigrated client directory as the first argument.

        Usage: 
           MigrationTool.exe <client-directory>
           MigrationTool.exe <client-directory> [--version <version>]

        --version. Runs only the patch for the specified XNA CnCNet client version.
                   Versions may have an optional v prefix (for example, v2.12.6).
                   This is the patch version, not the game or mod version.
                   Omit --version to apply all migrations in order.
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

                if (args.Length == 1 && arg is ("-h"
                    or "--help"
                    or "-?"
                    or "/?"
                    or "/h"))
                {
                    Console.WriteLine(HELP_MESSAGE);
                    Console.WriteLine("Migration tool support migration for these XNA CnCNet client versions: " + string.Join(", ", Enum.GetNames(typeof(Version)).Select(version => version.Replace('_', '.'))));
                    return;
                }

                Version? selectedVersion = null;
                if (args.Length == 3)
                {
                    if (args[1] != "--version")
                    {
                        Console.WriteLine(ERROR_MESSAGE);
                        Environment.ExitCode = 1;
                        return;
                    }

                    string versionText = args[2].Trim();
                    foreach (Version version in Enum.GetValues(typeof(Version)))
                    {
                        string patchVersion = version.ToString().Replace('_', '.');
                        if (string.Equals(versionText, patchVersion, StringComparison.OrdinalIgnoreCase)
                            || (version != Version.Latest && string.Equals("v" + versionText, patchVersion, StringComparison.OrdinalIgnoreCase)))
                        {
                            selectedVersion = version;
                            break;
                        }
                    }

                    if (selectedVersion == null)
                    {
                        Console.WriteLine($"Unsupported patch version \"{versionText}\". Use -h to see the supported versions.");
                        Environment.ExitCode = 1;
                        return;
                    }
                }

                if (!SafePath.GetDirectory(arg).Exists)
                {
                    Logger.Log($"Unable to find \"{arg}\" folder. Migration halted.");
                    return;
                }

                if (!SafePath.GetFile(SafePath.CombineFilePath(arg, "Resources", "ClientDefinitions.ini")).Exists)
                {
                    Logger.Log($"Unable to find \"{arg}\\Resources\\ClientDefinitions.ini\". Migration halted.");
                    return;
                }

                Logger.Log("Warning: Running the migration tool will back up your folder. However, if a backup already exists, a new one will not be created.");
                Logger.Log("Press any key to begin migration.");
                Console.ReadKey();

                if (!Directory.Exists(arg + "_backup"))
                {
                    Logger.Log($"Creating backup: {arg + "_backup"}");
                    FileSystem.CopyDirectory(arg, arg + "_backup");
                    Logger.Log("Backup created.");
                }

                var assembly = Assembly.GetExecutingAssembly();
                Patch? patch = null;
                try
                {
                    // https://stackoverflow.com/questions/16038819/how-to-find-all-direct-subclasses-of-a-class-with-net-reflection
                    var patches = assembly.GetTypes().Where(type => type.BaseType == typeof(Patch)).ToList();
                    var patchNames = Enum.GetValues(typeof(Version));
                    foreach (Version patchName in patchNames)
                    {
                        if (selectedVersion != null && patchName != selectedVersion)
                            continue;

                        Type type = patches.Where(t => t.FullName == "MigrationTool.Patch_" + patchName.ToString()).First();
                        patch = (Patch?)Activator.CreateInstance(type, arg);
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

                Console.WriteLine("Patching has been done.");

                break;
            case 0:
            default:
                Console.WriteLine(ERROR_MESSAGE);
                Environment.ExitCode = 1;
                break;
        }
    }
}