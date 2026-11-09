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

        var argClientDir = new Argument<string>("client-directory")
        {
            Description = "Path to the client directory.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var optPatch = new Option<string?>("--patch", "-p")
        {
            Description = "Apply only the specified migration patch. "
                + "If neither --patch nor --patch-from-version is specified, all configured patches are applied in sequence. "
                + "Dots and underscores are accepted in version names. A leading 'v' is optional. "
                + "The specified patch must be included in this build.",
            HelpName = "version"
        };

        var optPatchFromVersion = new Option<string?>("--patch-from-version")
        {
            Description = "Apply the specified migration patch and all subsequent patches in sequence, including Latest. " +
                "Dots and underscores are accepted in version names. A leading 'v' is optional. " +
                "The specified patch must be included in this build.",
            HelpName = "version"
        };

        var optPatchList = new Option<bool>("--patch-list")
        {
            Description = "Show list of available client version patches to use."
        };

        var root = new RootCommand(
            """
            CnCNet Client Migration Tool
            Experimental utility for migrating CnCNet client configuration.

            Notes:
                - MigrationTool.log is written to the current working directory.
                - Before applying patches, the tool creates <client-directory>_backup.
                - If that backup directory already exists, it is kept without updating it.

            Examples:
                MigrationTool.exe "..\Games\Client"
                MigrationTool.exe "C:\Path\To\Client" -p 2.11.0
                MigrationTool.exe "C:\Path\To\Client" --patch-from-version 2.12.0
                ./MigrationTool "/home/user/games/clientConfig"
            """)
        {
            argClientDir,
            optPatch,
            optPatchFromVersion,
            optPatchList
        };

        root.Validators.Add(result =>
        {
            if (!result.GetValue(optPatchList) && result.GetValue(argClientDir) == null)
                result.AddError("A client directory is required unless --patch-list is specified.");

            var patchFromVersionResult = result.GetResult(optPatchFromVersion);

            if (result.GetResult(optPatch) != null && patchFromVersionResult != null)
                result.AddError("--patch and --patch-from-version cannot be used together.");

            foreach (var option in new[] { optPatch, optPatchFromVersion })
            {
                var optionResult = result.GetResult(option);
                if (optionResult?.Tokens.Count != 1)
                    continue;

                string versionName = NormalizeVersionName(optionResult.Tokens[0].Value);

                if (!Enum.GetNames(typeof(Version)).Any(name => NormalizeVersionName(name) == versionName))
                {
                    optionResult.AddError("Unknown patch version. Use --patch-list to see the available versions.");
                }
            }
        });

        root.SetAction(result =>
        {
            if (result.GetValue(optPatchList))
            {
                Console.WriteLine("List of available versions: " + string.Join(", ", Enum.GetNames(typeof(Version)).Select(name => name.Replace('_', '.'))));
                return;
            }

            Version[] patchVersions = GetPatchVersions(result.GetValue(optPatch), result.GetValue(optPatchFromVersion));

            MigrateClient(result.GetValue(argClientDir)!, patchVersions);
        });

        // Supply the executable token explicitly to preserve directories named MigrationTool.
        return root.Parse(new[] { root.Name }.Concat(args).ToArray()).Invoke();
    }

    private static string NormalizeVersionName(string name) => name.Trim().Replace('.', '_').TrimStart('v', 'V');

    private static Version[] GetPatchVersions(string? versionName, string? patchFromVersion)
    {
        var versions = Enum.GetValues(typeof(Version)).Cast<Version>().ToArray();
        string? firstVersionName = versionName ?? patchFromVersion;

        if (firstVersionName == null)
            return versions;

        firstVersionName = NormalizeVersionName(firstVersionName);
        Version firstVersion = versions.First(version => NormalizeVersionName(version.ToString()) == firstVersionName);
        Version lastVersion = versionName == null ? versions[versions.Length - 1] : firstVersion;

        return versions.Where(version => version >= firstVersion && version <= lastVersion).ToArray();
    }

    private static void MigrateClient(string clientDir, Version[] patchVersions)
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

        PatchConfigs(clientDir, patchVersions);
    }

    private static void PatchConfigs(string clientDir, Version[] patchVersions)
    {
        var assembly = Assembly.GetExecutingAssembly();
        Version? currentVersion = null;
        try
        {
            // https://stackoverflow.com/questions/16038819/how-to-find-all-direct-subclasses-of-a-class-with-net-reflection
            var patches = assembly.GetTypes().Where(type => type.BaseType == typeof(Patch)).ToList();

            foreach (Version patchVersion in patchVersions)
            {
                currentVersion = patchVersion;
                Type type = patches.Where(t => t.FullName == "MigrationTool.Patch_" + patchVersion).First();
                var patch = (Patch?)Activator.CreateInstance(type, clientDir);
                patch?.Apply();
                Console.WriteLine("");
            }

            Console.WriteLine("Patching has been done.");
        }
        catch (Exception ex)
        {
            Logger.Log("");
            Logger.Log(string.Format("Unable to apply migration patch for client version {0} due to an internal error. Message: {1}",
                currentVersion?.ToString().Replace('_', '.'),
                ex.ToString()));
            Logger.Log(string.Format("Migration to the {0} client version has been failed", patchVersions.Last().ToString().Replace('_', '.')));
        }
    }
}
