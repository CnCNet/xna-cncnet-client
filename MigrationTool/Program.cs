using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.VisualBasic.FileIO;

using Rampastring.Tools;

namespace MigrationTool;

internal sealed class Program
{
    private const int SuccessExitCode = 0;
    private const int FailureExitCode = 1;

    private static int Main(string[] args)
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

        var argClientDir = new Argument<string>("client-directory")
        {
            Description = "Path to the client directory.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var optPatch = new Option<string?>("--patch", "-p")
        {
            Description = "Apply only the patch for the specified client version. "
                + "The version must be one of the versions listed by --patch-list.",
            HelpName = "version"
        };

        var optFrom = new Option<string?>("--from")
        {
            Description = "Apply the patches released after the specified client version. "
                + "Please pass the version the client folder currently runs. "
                + "The version does not have to be listed by --patch-list and can be any client version. "
                + "If --to is omitted, all later patches are applied, including the latest one.",
            HelpName = "version"
        };

        var optTo = new Option<string?>("--to")
        {
            Description = "Apply patches up to and including the specified client version. Requires --from. "
                + "The version does not have to be listed by --patch-list and can be any client version.",
            HelpName = "version"
        };

        var optPatchList = new Option<bool>("--patch-list")
        {
            Description = "Show list of available client version patches to use."
        };

        var optNonInteractive = new Option<bool>("--non-interactive")
        {
            Description = "Run without waiting for user input, for example from a script."
        };

        var root = new RootCommand(
            """
            CnCNet Client Migration Tool
            Experimental utility for migrating CnCNet client configuration.
            """)
        {
            argClientDir,
            optPatch,
            optFrom,
            optTo,
            optPatchList,
            optNonInteractive
        };

        root.Validators.Add(result =>
        {
            if (!result.GetValue(optPatchList) && result.GetValue(argClientDir) == null)
                result.AddError("A client directory is required.");

            var patchResult = result.GetResult(optPatch);
            var fromResult = result.GetResult(optFrom);
            var toResult = result.GetResult(optTo);

            if (patchResult != null && (fromResult != null || toResult != null))
                result.AddError("--patch cannot be combined with --from or --to.");

            if (toResult != null && fromResult == null)
                result.AddError("--to requires --from.");

            if (patchResult?.Tokens.Count == 1)
            {
                if (!ClientVersion.TryParse(patchResult.Tokens[0].Value, out ClientVersion patchVersion)
                    || !ClientVersion.All.Contains(patchVersion))
                {
                    patchResult.AddError($"There is no patch for client version \"{patchResult.Tokens[0].Value}\". "
                        + "Use --patch-list to see the available patch versions.");
                }
            }

            ClientVersion? fromVersion = null;
            ClientVersion? toVersion = null;

            if (fromResult?.Tokens.Count == 1)
            {
                if (!ClientVersion.TryParse(fromResult.Tokens[0].Value, out ClientVersion parsedFromVersion))
                {
                    fromResult.AddError($"Invalid client version \"{fromResult.Tokens[0].Value}\". "
                        + "Expected a version such as 2.12.0, 2_12_0 or v2.12.0.");
                }
                else if (parsedFromVersion == ClientVersion.Latest)
                {
                    fromResult.AddError("--from cannot be the latest version because there are no patches after it.");
                }
                else
                {
                    fromVersion = parsedFromVersion;
                }
            }

            if (toResult?.Tokens.Count == 1)
            {
                if (!ClientVersion.TryParse(toResult.Tokens[0].Value, out ClientVersion parsedToVersion))
                {
                    toResult.AddError($"Invalid client version \"{toResult.Tokens[0].Value}\". "
                        + "Expected a version such as 2.12.0, 2_12_0 or v2.12.0.");
                }
                else
                {
                    toVersion = parsedToVersion;
                }
            }

            if (fromVersion != null && toVersion != null)
            {
                if (toVersion.Value.CompareTo(fromVersion.Value) <= 0)
                {
                    result.AddError("--to must be later than --from.");
                }
                else if (SelectPatchVersions(null, fromVersion, toVersion).Length == 0)
                {
                    result.AddError($"The range from \"{fromVersion.Value}\" to \"{toVersion.Value}\" "
                        + "does not contain any patches. Use --patch-list to see the available patch versions.");
                }
            }
        });

        root.SetAction(result =>
        {
            if (result.GetValue(optPatchList))
            {
                Console.WriteLine("List of available versions: " + string.Join(", ", ClientVersion.All.Select(version => version.DisplayName)));
                return SuccessExitCode;
            }

            ClientVersion? patchVersion = GetOptionVersion(result, optPatch);
            ClientVersion? fromVersion = GetOptionVersion(result, optFrom);

            ClientVersion[] patchVersions = SelectPatchVersions(
                patchVersion,
                fromVersion,
                GetOptionVersion(result, optTo));

            bool appliesEveryPatch = patchVersion == null && fromVersion == null;

            bool migrated = MigrateClient(
                clientDir: result.GetValue(argClientDir)!,
                patchVersions: patchVersions,
                appliesEveryPatch: appliesEveryPatch,
                nonInteractive: result.GetValue(optNonInteractive));

            return migrated ? SuccessExitCode : FailureExitCode;
        });

        // Supply the executable token explicitly to preserve directories named MigrationTool.
        return root.Parse(new[] { root.Name }.Concat(args).ToArray()).Invoke();
    }

    private static ClientVersion? GetOptionVersion(ParseResult parseResult, Option<string?> option)
    {
        var optionResult = parseResult.GetResult(option);

        if (optionResult?.Tokens.Count != 1)
            return null;

        return ClientVersion.TryParse(optionResult.Tokens[0].Value, out ClientVersion version) ? version : null;
    }

    /// <summary>
    /// Selects the patches to apply. <paramref name="from"/> is exclusive, <paramref name="to"/> is inclusive.
    /// </summary>
    private static ClientVersion[] SelectPatchVersions(ClientVersion? exact, ClientVersion? from, ClientVersion? to)
    {
        IEnumerable<ClientVersion> selection = ClientVersion.All;

        if (exact != null)
            selection = selection.Where(version => version == exact.Value);

        if (from != null)
            selection = selection.Where(version => version > from.Value);

        if (to != null)
            selection = selection.Where(version => version <= to.Value);

        return selection.ToArray();
    }

    private static bool MigrateClient(string clientDir, ClientVersion[] patchVersions, bool appliesEveryPatch, bool nonInteractive)
    {
        if (!SafePath.GetDirectory(clientDir).Exists)
        {
            Logger.Log($"Unable to find \"{clientDir}\" folder. Migration halted.");
            return false;
        }

        if (!SafePath.GetFile(SafePath.CombineFilePath(clientDir, "Resources", "ClientDefinitions.ini")).Exists)
        {
            Logger.Log($"Unable to find \"{clientDir}\\Resources\\ClientDefinitions.ini\". Migration halted.");
            return false;
        }

        if (appliesEveryPatch)
        {
            Logger.Log("Warning: All migration patches will be applied. If the client already runs a released version, "
                + "pass --from <version> to apply only the patches released after it.");
        }

        Logger.Log("Warning: Running the migration tool will back up your folder. However, if a backup already exists, a new one will not be created.");

        Logger.Log("Warning: This is an experimental tool. Use it at your own risk.");

        if (!nonInteractive)
        {
            if (Console.IsInputRedirected)
            {
                Logger.Log("Cannot prompt for input because the console input is redirected. "
                    + "Pass --non-interactive to run unattended.");
                return false;
            }

            Logger.Log("Press any key to begin migration.");
            Console.ReadKey();
        }

        if (!Directory.Exists(clientDir + "_backup"))
        {
            Logger.Log($"Creating backup: {clientDir + "_backup"}");
            FileSystem.CopyDirectory(clientDir, clientDir + "_backup");
            Logger.Log("Backup created.");
        }

        return PatchConfigs(clientDir, patchVersions);
    }

    private static bool PatchConfigs(string clientDir, ClientVersion[] patchVersions)
    {
        var assembly = Assembly.GetExecutingAssembly();
        ClientVersion? currentVersion = null;
        try
        {
            // https://stackoverflow.com/questions/16038819/how-to-find-all-direct-subclasses-of-a-class-with-net-reflection
            var patches = assembly.GetTypes().Where(type => type.BaseType == typeof(Patch)).ToList();

            foreach (ClientVersion patchVersion in patchVersions)
            {
                currentVersion = patchVersion;
                Type type = patches.Where(t => t.FullName == "MigrationTool.Patch_" + patchVersion.Name).First();
                var patch = (Patch?)Activator.CreateInstance(type, clientDir);
                patch?.Apply();
                Console.WriteLine("");
            }

            Console.WriteLine("Patching has been done.");

            return true;
        }
        catch (Exception ex)
        {
            Logger.Log("");
            Logger.Log(string.Format("Unable to apply migration patch for client version {0} due to an internal error. Message: {1}",
                currentVersion?.ToString(),
                ex.ToString()));
            Logger.Log(string.Format("Migration to the {0} client version has been failed", patchVersions.Last()));

            return false;
        }
    }
}
