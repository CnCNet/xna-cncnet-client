using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Rampastring.Tools;

namespace MigrationTool;

internal sealed class Program
{
    private enum Version
    {
        Begin,
        V_2_11_0,
        V_2_11_1,
        V_2_11_2,
        V_2_12_1,
        Latest,
        End
    }

    private enum ClientGameType
    {
        TS,
        YR,
        Ares
    }

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

    private static void AddKeyWithLog(IniFile src, string section, string key, string value)
    {
        if (src.KeyExists(section, key))
        {
            Log($"Update {src.FileName}.ini: Skip adding [{section}]->{key}, reason: already exist");
        }
        else
        {
            Log($"Update {src.FileName}.ini: Add [{section}]->{key}={value}");
            src.GetSection(section).AddKey(key, value);
        }
    }

    private static void Migrate(string path)
    {
        DirectoryInfo clientDir = SafePath.GetDirectory(path);
        DirectoryInfo resouresDir = SafePath.GetDirectory(SafePath.CombineFilePath(path, "Resources"));

        IniFile clientDefsIni = new IniFile(SafePath.CombineFilePath(resouresDir.FullName, "ClientDefinitions.ini"));
        IniFile gmLobbyBaseIni = null;

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

        for (int currentVersion = (int)Version.Begin; currentVersion != (int)Version.End; currentVersion++)
        {
            switch ((Version)currentVersion)
            {
                case (Version.V_2_11_0):

                    // Remove Rampastring.Tools from Resources directory (not recursive)
                    Log("Remove Resources\\Rampastring.Tools.* (* -- dll, pdb, xml)");
                    SafePath.DeleteFileIfExists(resouresDir.FullName, "Rampastring.Tools.dll");
                    SafePath.DeleteFileIfExists(resouresDir.FullName, "Rampastring.Tools.pdb");
                    SafePath.DeleteFileIfExists(resouresDir.FullName, "Rampastring.Tools.xml");

                    // Add GlobalThemeSettings.ini
                    IniFile globalThemeSettingsIni = new IniFile(SafePath.CombineFilePath(resouresDir.FullName, "GlobalThemeSettings.ini"));
                    Dictionary<string, int> gtsKeys = new()
                    {
                        { "DEFAULT_LBL_HEIGHT", 12 },
                        { "DEFAULT_CONTROL_HEIGHT", 21 },
                        { "DEFAULT_BUTTON_HEIGHT", 23 },
                        { "BUTTON_WIDTH_133", 133 },
                        { "OPEN_BUTTON_WIDTH", 18 },
                        { "OPEN_BUTTON_HEIGHT", 22 },
                        { "EMPTY_SPACE_TOP", 12 },
                        { "EMPTY_SPACE_BOTTOM", 12 },
                        { "EMPTY_SPACE_SIDES", 12 },
                        { "BUTTON_SPACING", 12 },
                        { "LABEL_SPACING", 6 },
                        { "CHECKBOX_SPACING", 24 },
                        { "LOBBY_EMPTY_SPACE_SIDES", 12 },
                        { "LOBBY_PANEL_SPACING", 10 },
                        { "GAME_OPTION_COLUMN_SPACING", 160 },
                        { "GAME_OPTION_ROW_SPACING", 6 },
                        { "GAME_OPTION_DD_WIDTH", 132 },
                        { "GAME_OPTION_DD_HEIGHT", 22 }
                    };

                    if (!globalThemeSettingsIni.SectionExists("ParserConstants"))
                        globalThemeSettingsIni.AddSection("ParserConstants");

                    foreach (var key in gtsKeys)
                        AddKeyWithLog(globalThemeSettingsIni, "ParserConstants", key.Key, key.Value.ToString());

                    globalThemeSettingsIni.WriteIniFile();

                    // Add PlayerExtraOptionsPanel.ini
                    IniFile playerExtraOptionsPanelIni = new IniFile(SafePath.CombineFilePath(resouresDir.FullName, "PlayerExtraOptionsPanel.ini"));
                    var peopSections = new List<string>() 
                    { 
                      "btnClose", "lblHeader", "chkBoxForceRandomSides", 
                      "chkBoxForceRandomColors", "chkBoxForceRandomTeams", 
                      "chkBoxForceRandomStarts", "chkBoxUseTeamStartMappings",
                      "btnHelp", "lblPreset", "ddTeamStartMappingPreset", 
                      "teamStartMappingsPanel" 
                    };

                    foreach (var section in peopSections)
                        if (!playerExtraOptionsPanelIni.SectionExists(section))
                            playerExtraOptionsPanelIni.AddSection(section);

                    AddKeyWithLog(playerExtraOptionsPanelIni, "btnClose",                   "Location", "220,0");
                    AddKeyWithLog(playerExtraOptionsPanelIni, "btnClose",                   "Size",     "18,18");
                    AddKeyWithLog(playerExtraOptionsPanelIni, "lblHeader",                  "Location", "12,6");
                    AddKeyWithLog(playerExtraOptionsPanelIni, "chkBoxForceRandomSides",     "Location", "12,28");
                    AddKeyWithLog(playerExtraOptionsPanelIni, "chkBoxForceRandomColors",    "Location", "12,50");
                    AddKeyWithLog(playerExtraOptionsPanelIni, "chkBoxForceRandomTeams",     "Location", "12,72");
                    AddKeyWithLog(playerExtraOptionsPanelIni, "chkBoxForceRandomStarts",    "Location", "12,94");
                    AddKeyWithLog(playerExtraOptionsPanelIni, "chkBoxUseTeamStartMappings", "Location", "12,130");
                    AddKeyWithLog(playerExtraOptionsPanelIni, "btnHelp",                    "Location", "160,130");
                    AddKeyWithLog(playerExtraOptionsPanelIni, "lblPreset",                  "Location", "12,156");
                    AddKeyWithLog(playerExtraOptionsPanelIni, "ddTeamStartMappingPreset",   "Location", "65,154");
                    AddKeyWithLog(playerExtraOptionsPanelIni, "ddTeamStartMappingPreset",   "Size",     "157,21");
                    AddKeyWithLog(playerExtraOptionsPanelIni, "teamStartMappingsPanel",     "Location", "12,189");

                    continue;

                case (Version.V_2_11_1): // https://github.com/CnCNet/xna-cncnet-client/releases/tag/2.11.1.0
                    // Add ClientDefinitions.ini->[Settings]->RecommendedResolutions
                    AddKeyWithLog(clientDefsIni, "Settings", "RecommendedResolutions", "1280x720");
                    AddKeyWithLog(clientDefsIni, "Settings", "MaximumRenderWidth", "1280");
                    AddKeyWithLog(clientDefsIni, "Settings", "MaximumRenderHeight", "720");
                    clientDefsIni.WriteIniFile();
                    continue;

                case (Version.V_2_11_2): // https://github.com/CnCNet/xna-cncnet-client/releases/tag/2.11.2.0
                    // Remove ClientUpdater.xml and SecondStageUpdater.xml
                    var listExtraXMLs = new List<string>(2) { "ClientUpdater.xml", "SecondStageUpdater.xml" };
                    Log("Remove ClientUpdater.xml and SecondStageUpdater.xml");

                    foreach (var extraXml in listExtraXMLs)
                    {
                        Directory.GetFiles(resouresDir.FullName, extraXml, SearchOption.AllDirectories)
                            .ToList()
                            .ForEach(elem => SafePath.DeleteFileIfExists(elem));
                    }

                    // Add ClientDefinitions.ini->[Settings]->ShowDevelopmentBuildWarnings
                    AddKeyWithLog(clientDefsIni, "Settings", "ShowDevelopmentBuildWarnings", "true");
                    clientDefsIni.WriteIniFile();
                    continue;

                case (Version.V_2_12_1): // https://github.com/CnCNet/xna-cncnet-client/releases/tag/2.12.1
                    // And add ClientDefinitions.ini->[Settings]->ClientGameType
                    string cgt = clientGameType switch
                    {
                        ClientGameType.Ares => "Ares",
                        ClientGameType.YR => "YR",
                        _ => "TS"
                    };

                    AddKeyWithLog(clientDefsIni, "Settings", "ClientGameType", cgt);
                    clientDefsIni.WriteIniFile();
                    continue;

                case (Version.Latest):
                    // Add GameLobbyBase.ini->[ddPlayerColorX]->ItemsDrawMode
                    gmLobbyBaseIni ??= new IniFile(SafePath.CombineFilePath(resouresDir.FullName, "GameLobbyBase.ini"));
                    string ddPlayerColor = nameof(ddPlayerColor);
                    foreach (var n in Enumerable.Range(0, 8))
                    {
                        if (!gmLobbyBaseIni.SectionExists(ddPlayerColor + n))
                            gmLobbyBaseIni.AddSection(ddPlayerColor + n);

                        AddKeyWithLog(gmLobbyBaseIni, ddPlayerColor + n, "ItemsDrawMode", "Text");
                        gmLobbyBaseIni.WriteIniFile();
                    }
                    continue;

                default:
                    continue;
            }
        }
    }
}
