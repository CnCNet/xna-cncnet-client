using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.VisualBasic.FileIO;

using Rampastring.Tools;

namespace MigrationTool;

internal sealed class Program
{
    private static int Main(string[] args)
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

        var clientDirectoryArgument = new Argument<string>("client-directory")
        {
            Description = "Path to the client directory. Required unless --patch-list is specified.",
            Arity = ArgumentArity.ZeroOrOne
        };
        var patchOption = new Option<string?>("--patch", "-p")
        {
            Description = "Apply only the specified migration patch. If omitted, all configured patches are applied in sequence. " +
                "Dots and underscores are accepted in version names. A leading 'v' is optional. " +
                "The specified patch must be included in this build.",
            HelpName = "version"
        };
        var patchListOption = new Option<bool>("--patch-list")
        {
            Description = "Show list of available client versions to use."
        };
        var rootCommand = new RootCommand(
            """
            CnCNet Client Migration Tool
            Experimental utility for migrating CnCNet client configuration files.

            Notes:
                - MigrationTool.log is written to the current working directory.
                - Before applying patches, the tool creates <client-directory>_backup.
                - If that backup directory already exists, it is kept without updating it.

            Examples:
                MigrationTool.exe "..\Games\Client"
                MigrationTool.exe "C:\Path\To\Client" -p 2.11.0
                ./MigrationTool "/home/user/games/clientConfig"
            """)
        {
            clientDirectoryArgument,
            patchOption,
            patchListOption
        };

        rootCommand.Validators.Add(result =>
        {
            if (!result.GetValue(patchListOption) && result.GetValue(clientDirectoryArgument) == null)
                result.AddError("A client directory is required unless --patch-list is specified.");
        });
        rootCommand.SetAction(result =>
        {
            if (result.GetValue(patchListOption))
            {
                Console.WriteLine("List of available versions: " + string.Join(", ", Enum.GetNames(typeof(Version)).Select(name => name.Replace('_', '.'))));

                return;
            }

            MigrateClient(result.GetValue(clientDirectoryArgument)!, result.GetValue(patchOption)?.Trim().ToLowerInvariant());
        });

        // Supply the executable token explicitly to preserve directories named MigrationTool.
        return rootCommand.Parse(new[] { rootCommand.Name }.Concat(args).ToArray()).Invoke();
    }

    private static void MigrateClient(string clientDir, string? versionName)
    {
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
