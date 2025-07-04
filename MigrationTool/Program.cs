using System.IO;
using Rampastring.Tools;

namespace MigrationTool;

internal sealed class Program
{
    private enum Version
    {
        Begin,
        V_2_8,
        V_2_9,
        V_2_,
        V_2_12_1,
        End
    }

    private enum ClientGameType
    {
        TS,
        YR,
        Ares
    }

    private static Version currentConfigVersion = Version.V_2_8;
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

                try
                {
                    Migrate(arg);
                }
                catch (Exception ex)
                {
                    Log("Unable to migrate client configs due to internal error. Message: " + ex.Message, ConsoleColor.Red);
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

    private static void Migrate(string path)
    {
        DirectoryInfo clientDir = SafePath.GetDirectory(path);
        DirectoryInfo resouresDir = SafePath.GetDirectory(SafePath.CombineFilePath(path, "Resources"));

        for (int i = (int)Version.Begin; i != (int)Version.End; i++)
        {
            switch ((Version)i)
            {
                case (Version.V_2_12_1): // https://github.com/CnCNet/xna-cncnet-client/releases/tag/2.12.1
                    // Predict client type by guessing game engine files
                    var clientGameType = ClientGameType.TS;

                    if (!SafePath.GetFile(SafePath.CombineFilePath(clientDir.FullName, "Ares.dll")).Exists
                        && SafePath.GetFile(SafePath.CombineFilePath(clientDir.FullName, "gamemd-spawn.exe")).Exists)
                    {
                        clientGameType = ClientGameType.YR;
                    }
                    else if (SafePath.GetFile(SafePath.CombineFilePath(clientDir.FullName, "Ares.dll")).Exists)
                    {
                        clientGameType = ClientGameType.Ares;
                    }

                    var clientDefsIni = new IniFile(SafePath.CombineFilePath(path, "ClientDefinitions.ini"));
                    var value = clientDefsIni.GetStringValue("Settings", "ClientGameType", string.Empty);

                    value = !string.IsNullOrEmpty(value.Trim()) ? value : clientGameType switch
                    {
                        ClientGameType.Ares => "Ares",
                        ClientGameType.YR => "YR",
                        _ => "TS"
                    };

                    clientDefsIni.GetSection("Settings").AddKey("ClientGameType", value);
                    continue;
                default:
                    continue;
            }
        }
    }

}
