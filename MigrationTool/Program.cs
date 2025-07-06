using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Rampastring.Tools;

namespace MigrationTool;

public enum Version
{
    Begin,
    v2_11_0,
    v2_11_1,
    v2_11_2,
    v2_12_1,
    Latest,
    End
}

public enum ClientGameType
{
    TS,
    YR,
    Ares
}

internal sealed class Program
{
    private static ConsoleColor defaultColor = Console.ForegroundColor;

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
                    PrintHelp();
                    return;
                }

                if (!SafePath.GetDirectory(arg).Exists)
                {
                    PrintArgsError();
                    return;
                }

                if (!SafePath.GetFile(SafePath.CombineFilePath(arg, "Resources", "ClientDefinitions.ini")).Exists)
                {
                    Log("Unable to find Resources/ClientDefinitions.ini. Migration aborted.", ConsoleColor.Red);
                    return;
                }

                Patch patch = null;
                try
                {
                    patch = new Patch_v2_11_0(arg).Apply();
                    patch = new Patch_v2_11_1(arg).Apply();
                    patch = new Patch_v2_11_2(arg).Apply();
                    patch = new Patch_v2_12_1(arg).Apply();
                    patch = new Patch_Latest(arg).Apply();
                }
                catch (Exception ex)
                {
                    Log($"Unable to apply migration patch for client version {patch.ClientVersion.ToString().Replace('_', '.')} due to internal error. Message: " + ex.Message, ConsoleColor.Red);
                    Log("Migration to the latest client version has been failed", ConsoleColor.Red);
                }

                break;
            case 0:
            default:
                PrintArgsError();
                break;
        }
    }

    private static void Log(string text, ConsoleColor? color = null, bool echoToConsole = true)
    {
        Console.ForegroundColor = color ?? defaultColor;
        Logger.Log(text);

        if (echoToConsole)
            Console.WriteLine(text);
    }

    private static void PrintArgsError()
        => Log("Unknown arguments detected. Use -h argument to print help information", ConsoleColor.Red);

    private static void PrintHelp()
    {
        string text =
            """
            CnCNet Client Migration Tool.

            Execute this file with path to the unmigrated client directory as first argument.
            """;

        Console.WriteLine(text);
    }
}
