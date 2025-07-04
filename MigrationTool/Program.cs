using System.IO;
using System.Linq;
using Rampastring.Tools;

namespace MigrationTool;

internal sealed class Program
{
    private enum Version
    {
        Begin,
        V_2_8_x_x,
        V_2_11_0,
        V_2_11_1,
        V_2_11_2,
        V_2_12_0,
        V_2_12_1,
        V_2_12_5,
        End
    }

    private enum ClientGameType
    {
        TS,
        YR,
        Ares
    }

    private static Version currentConfigVersion = Version.V_2_8_x_x;
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

        IniFile clientDefsIni = new IniFile(SafePath.CombineFilePath(resouresDir.FullName, "ClientDefinitions.ini"));

        for (int i = (int)Version.Begin; i != (int)Version.End; i++)
        {
            switch ((Version)i)
            {
                case (Version.V_2_11_0):
                    continue;

                case (Version.V_2_11_1): // https://github.com/CnCNet/xna-cncnet-client/releases/tag/2.11.1.0
                    // Add ClientDefinitions.ini->[Settings]->RecommendedResolutions
                    if (clientDefsIni.KeyExists("Settings", "RecommendedResolutions"))
                    {
                        Log($"Update ClientDefinitions.ini: Skip add [Settings]->RecommendedResolutions, reason: already exist");
                        continue;
                    }

                    var rr = "1280x720";
                    Log($"Update ClientDefinitions.ini: Add [Settings]->RecommendedResolutions={rr}");
                    clientDefsIni.GetSection("Settings").AddKey("RecommendedResolutions", rr);
                    clientDefsIni.WriteIniFile();
                    continue;

                case (Version.V_2_11_2): // https://github.com/CnCNet/xna-cncnet-client/releases/tag/2.11.2.0
                    // Remove ClientUpdater.xml and SecondStageUpdater.xml
                    var listExtraXMLs = new List<string>(2) { "ClientUpdater.xml", "SecondStageUpdater.xml" };
                    Log("Remove ClientUpdater.xml and SecondStageUpdater.xml");

                    foreach (var item in listExtraXMLs)
                    {
                        Directory.GetFiles(resouresDir.FullName, item, SearchOption.AllDirectories)
                            .ToList()
                            .ForEach(elem => SafePath.DeleteFileIfExists(elem));
                    }

                    // Add ClientDefinitions.ini->[Settings]->ShowDevelopmentBuildWarnings
                    if (clientDefsIni.KeyExists("Settings", "ShowDevelopmentBuildWarnings"))
                    {
                        Log($"Update ClientDefinitions.ini: Skip add [Settings]->ShowDevelopmentBuildWarnings, reason: already exist");
                        continue;
                    }

                    var sdbw = true;
                    Log($"Update ClientDefinitions.ini: Add [Settings]->ShowDevelopmentBuildWarnings={sdbw.ToString()}");
                    clientDefsIni.GetSection("Settings").AddKey("ShowDevelopmentBuildWarnings", sdbw.ToString());
                    clientDefsIni.WriteIniFile();
                    continue;

                case (Version.V_2_12_0): // https://github.com/CnCNet/xna-cncnet-client/releases/tag/2.12.0
                    // Remove Rampastring.Tools from Resources directory (not recursive)
                    Log("Remove Resources/Rampastring.Tools.* (* -- dll, pdb, xml)");
                    SafePath.DeleteFileIfExists(resouresDir.FullName, "Rampastring.Tools.dll");
                    SafePath.DeleteFileIfExists(resouresDir.FullName, "Rampastring.Tools.pdb");
                    SafePath.DeleteFileIfExists(resouresDir.FullName, "Rampastring.Tools.xml");
                    break;

                case (Version.V_2_12_1): // https://github.com/CnCNet/xna-cncnet-client/releases/tag/2.12.1
                    // Predict client type by guessing game engine files
                    // And add ClientDefinitions.ini->[Settings]->ClientGameType
                    if (clientDefsIni.KeyExists("Settings", "ClientGameType"))
                    {
                        Log($"Update ClientDefinitions.ini: Skip add [Settings]->ClientGameType, reason: already exist");
                        continue;
                    }

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

                    clientDefsIni = new IniFile(SafePath.CombineFilePath(resouresDir.FullName, "ClientDefinitions.ini"));
                    string cgt = clientGameType switch
                    {
                        ClientGameType.Ares => "Ares",
                        ClientGameType.YR => "YR",
                        _ => "TS"
                    };

                    Log($"Update ClientDefinitions.ini: Add [Settings]->ClientGameType={cgt}");
                    clientDefsIni.GetSection("Settings").AddKey("ClientGameType", cgt);
                    clientDefsIni.WriteIniFile();
                    continue;

                case (Version.V_2_12_5): // https://github.com/CnCNet/xna-cncnet-client/releases/tag/2.12.5
                    // Add ClientDefinitions.ini->[Settings]->TrustedDomains
                    if (clientDefsIni.KeyExists("Settings", "TrustedDomains"))
                    {
                        Log($"Update ClientDefinitions.ini: Skip add [Settings]->TrustedDomains, reason: already exist");
                        continue;
                    }

                    var td = "moddb.com";
                    Log($"Update ClientDefinitions.ini: Add [Settings]->TrustedDomains={td}");
                    clientDefsIni.GetSection("Settings").AddKey("TrustedDomains", td);
                    clientDefsIni.WriteIniFile();
                    continue;

                default:
                    continue;
            }
        }
    }
}
