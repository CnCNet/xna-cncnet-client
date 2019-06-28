using System;
using Rampastring.Tools;
using System.IO;

namespace ClientCore
{
    public class ClientConfiguration
    {
        private const string GENERAL = "General";
        private const string SETTINGS = "Settings";
        private const string LINKS = "Links";

        private const string CLIENT_SETTINGS = "DTACnCNetClient.ini";
        private const string GAME_OPTIONS = "GameOptions.ini";
        private const string CLIENT_DEFS = "ClientDefinitions.ini";

        private static ClientConfiguration _instance;

        private IniFile gameOptions_ini;
        private IniFile DTACnCNetClient_ini;
        private IniFile clientDefinitionsIni;

        protected ClientConfiguration()
        {
            if (!File.Exists(ProgramConstants.GetBaseResourcePath() + CLIENT_DEFS))
                throw new FileNotFoundException("Couldn't find " + CLIENT_DEFS + ". Please verify that you're running the client from the correct directory.");

            clientDefinitionsIni = new IniFile(ProgramConstants.GetBaseResourcePath() + CLIENT_DEFS);

            DTACnCNetClient_ini = new IniFile(ProgramConstants.GetResourcePath() + CLIENT_SETTINGS);

            gameOptions_ini = new IniFile(ProgramConstants.GetBaseResourcePath() + GAME_OPTIONS);
        }

        /// <summary>
        /// Singleton Pattern. Returns the object of this class.
        /// </summary>
        /// <returns>The object of the ClientConfiguration class.</returns>
        public static ClientConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ClientConfiguration();
                }
                return _instance;
            }
        }

        public void RefreshSettings()
        {
            DTACnCNetClient_ini = new IniFile(ProgramConstants.GetResourcePath() + CLIENT_SETTINGS);
        }

        public string UILabelColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue(GENERAL, "UILabelColor", "0,0,0");
            }
        }

        public string UIHintTextColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue(GENERAL, "HintTextColor", "128,128,128");
            }
        }

        public string DisabledButtonColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue(GENERAL, "DisabledButtonColor", "108,108,108");
            }
        }

        public string AltUIColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue(GENERAL, "AltUIColor", "255,255,255");
            }
        }

        public string ButtonHoverColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue(GENERAL, "ButtonHoverColor", "255,192,192");
            }
        }

        public string MapPreviewNameBackgroundColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue(GENERAL, "MapPreviewNameBackgroundColor", "0,0,0,144");
            }
        }

        public string MapPreviewNameBorderColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue(GENERAL, "MapPreviewNameBorderColor", "128,128,128,128");
            }
        }

        public string MapPreviewStartingLocationHoverRemapColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue(GENERAL, "StartingLocationHoverColor", "255,255,255,128");
            }
        }

        public string AltUIBackgroundColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue(GENERAL, "AltUIBackgroundColor", "196,196,196");
            }
        }

        public string WindowBorderColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue(GENERAL, "WindowBorderColor", "128,128,128");
            }
        }

        public string PanelBorderColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue(GENERAL, "PanelBorderColor", "255,255,255");
            }
        }

        public string ListBoxHeaderColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue(GENERAL, "ListBoxHeaderColor", "255,255,255");
            }
        }

        public float DefaultAlphaRate
        {
            get
            {
                return DTACnCNetClient_ini.GetSingleValue(GENERAL, "AlphaRate", 0.005f);
            }
        }

        public float CheckBoxAlphaRate
        {
            get
            {
                return DTACnCNetClient_ini.GetSingleValue(GENERAL, "CheckBoxAlphaRate", 0.05f);
            }
        }

        public string DefaultChatColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue(GENERAL, "DefaultChatColor", "0,255,0");
            }
        }

        public string AdminNameColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue(GENERAL, "AdminNameColor", "255,0,0");
            }
        }

        public string ReceivedPMColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue(GENERAL, "PrivateMessageOtherUserColor", "196,196,196");
            }
        }

        public string SentPMColor
        {
            get { return DTACnCNetClient_ini.GetStringValue(GENERAL, "PrivateMessageColor", "128,128,128"); }
        }

        public int DefaultPersonalChatColorIndex
        {
            get
            {
                return DTACnCNetClient_ini.GetIntValue(GENERAL, "DefaultPersonalChatColorIndex", 0);
            }
        }

        public string ListBoxFocusColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue(GENERAL, "ListBoxFocusColor", "64,64,168");
            }
        }

        public string HoverOnGameColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue(GENERAL, "HoverOnGameColor", "32,32,84");
            }
        }

        public string MainMenuMusicName
        {
            get { return DTACnCNetClient_ini.GetStringValue(GENERAL, "MainMenuTheme", "mainmenu"); }
        }

        public int LoadingScreenCount
        {
            get
            {
                return clientDefinitionsIni.GetIntValue(SETTINGS, "LoadingScreenCount", 2);
            }
        }

        public string GetSides()
        {
            return gameOptions_ini.GetStringValue(GENERAL, "Sides", "GDI,Nod,Allies,Soviet");
        }

        public string GetInternalSideIds()
        {
            return gameOptions_ini.GetStringValue(GENERAL, "InternalSideIds", string.Empty);
        }

        public string GetSpectatorInternalSideId()
        {
            return gameOptions_ini.GetStringValue(GENERAL, "SpectatorInternalSideId", string.Empty);
        }

        public int ThemeCount
        {
            get
            {
                return clientDefinitionsIni.GetSectionKeys("Themes").Count;
            }
        }

        public string LocalGame
        {
            get
            {
                return clientDefinitionsIni.GetStringValue(SETTINGS, "LocalGame", "DTA");
            }
        }

        public int SendSleep
        {
            get
            {
                return clientDefinitionsIni.GetIntValue(SETTINGS, "SendSleep", 2500);
            }
        }

        public bool SidebarHack
        {
            get
            {
                return clientDefinitionsIni.GetBooleanValue(SETTINGS, "SidebarHack", false);
            }
        }

        public int MinimumRenderWidth
        {
            get
            {
                return clientDefinitionsIni.GetIntValue(SETTINGS, "MinimumRenderWidth", 1280);
            }
        }

        public int MinimumRenderHeight
        {
            get
            {
                return clientDefinitionsIni.GetIntValue(SETTINGS, "MinimumRenderHeight", 768);
            }
        }

        public int MaximumRenderWidth
        {
            get
            {
                return clientDefinitionsIni.GetIntValue(SETTINGS, "MaximumRenderWidth", 1280);
            }
        }

        public int MaximumRenderHeight
        {
            get
            {
                return clientDefinitionsIni.GetIntValue(SETTINGS, "MaximumRenderHeight", 800);
            }
        }

        public string WindowTitle
        {
            get
            {
                return clientDefinitionsIni.GetStringValue(SETTINGS, "WindowTitle", string.Empty);
            }
        }

        public string InstallationPathRegKey
        {
            get
            {
                return clientDefinitionsIni.GetStringValue(SETTINGS, "RegistryInstallPath", "TiberianSun");
            }
        }

        public string CnCNetLiveStatusIdentifier => clientDefinitionsIni.GetStringValue(SETTINGS, "CnCNetLiveStatusIdentifier", "cncnet5_ts");

        public string BattleFSFileName => clientDefinitionsIni.GetStringValue(SETTINGS, "BattleFSFileName", "BattleFS.ini");

        public string MapEditorExePath => clientDefinitionsIni.GetStringValue(SETTINGS, "MapEditorExePath", "FinalSun\\FinalSun.exe");

        public string UnixMapEditorExePath => clientDefinitionsIni.GetStringValue(SETTINGS, "UnixMapEditorExePath", Instance.MapEditorExePath);

        public bool ModMode => clientDefinitionsIni.GetBooleanValue(SETTINGS, "ModMode", false);

        public string LongGameName => clientDefinitionsIni.GetStringValue(SETTINGS, "LongGameName", "Tiberian Sun");

        public string LongSupportURL => clientDefinitionsIni.GetStringValue(SETTINGS, "LongSupportURL", "http://www.moddb.com/members/rampastring");

        public string ShortSupportURL => clientDefinitionsIni.GetStringValue(SETTINGS, "ShortSupportURL", "www.moddb.com/members/rampastring");

        public string ChangelogURL => clientDefinitionsIni.GetStringValue(SETTINGS, "ChangelogURL", "http://www.moddb.com/mods/the-dawn-of-the-tiberium-age/tutorials/change-log");

        public string CreditsURL => clientDefinitionsIni.GetStringValue(SETTINGS, "CreditsURL", "http://www.moddb.com/mods/the-dawn-of-the-tiberium-age/tutorials/credits#Rampastring");

        public string FinalSunIniPath => clientDefinitionsIni.GetStringValue(SETTINGS, "FSIniPath", "FinalSun\\FinalSun.ini");

        public int MaxNameLength => clientDefinitionsIni.GetIntValue(SETTINGS, "MaxNameLength", 16);

        public int MapCellSizeX => clientDefinitionsIni.GetIntValue(SETTINGS, "MapCellSizeX", 48); 

        public int MapCellSizeY => clientDefinitionsIni.GetIntValue(SETTINGS, "MapCellSizeY", 24);

        public bool UseBuiltStatistic => clientDefinitionsIni.GetBooleanValue(SETTINGS, "UseBuiltStatistic", false);

        public string StatisticsLogFileName => clientDefinitionsIni.GetStringValue(SETTINGS, "StatisticsLogFileName", "DTA.LOG");

        public string[] GetThemeInfoFromIndex(int themeIndex) => clientDefinitionsIni.GetStringValue("Themes", themeIndex.ToString(), ",").Split(',');

        /// <summary>
        /// Returns the directory path for a theme, or null if the specified
        /// theme name doesn't exist.
        /// </summary>
        /// <param name="themeName">The name of the theme.</param>
        /// <returns>The path to the theme's directory.</returns>
        public string GetThemePath(string themeName)
        {
            var themeSection = clientDefinitionsIni.GetSection("Themes");
            foreach (var key in themeSection.Keys)
            {
                string[] parts = key.Value.Split(',');
                if (parts[0] == themeName)
                    return parts[1];
            }

            return null;
        }

        public string SettingsIniName => clientDefinitionsIni.GetStringValue(SETTINGS, "SettingsFile", "Settings.ini");

        public string ExtraExeCommandLineParameters => clientDefinitionsIni.GetStringValue(SETTINGS, "ExtraCommandLineParams", string.Empty);

        public string MPMapsIniPath => clientDefinitionsIni.GetStringValue(SETTINGS, "MPMapsPath", "INI\\MPMaps.ini");

        public string MPModesIniPath => clientDefinitionsIni.GetStringValue(SETTINGS, "MPModesPath", "INI\\MPModes.ini");

        public string KeyboardINI => clientDefinitionsIni.GetStringValue(SETTINGS, "KeyboardINI", "Keyboard.ini");

        public int MinimumIngameWidth => clientDefinitionsIni.GetIntValue(SETTINGS, "MinimumIngameWidth", 640);

        public int MinimumIngameHeight => clientDefinitionsIni.GetIntValue(SETTINGS, "MinimumIngameHeight", 480);

        public int MaximumIngameWidth => clientDefinitionsIni.GetIntValue(SETTINGS, "MaximumIngameWidth", int.MaxValue);

        public int MaximumIngameHeight => clientDefinitionsIni.GetIntValue(SETTINGS, "MaximumIngameHeight", int.MaxValue);

        public bool CopyMissionsToSpawnmapINI => clientDefinitionsIni.GetBooleanValue(SETTINGS, "CopyMissionsToSpawnmapINI", true);

        public string GetAllowedGameModes => clientDefinitionsIni.GetStringValue(SETTINGS, "AllowedCustomGameModes", "Standard,Custom Map");

        public string GetGameExecutableName()
        {
            string[] exeNames = clientDefinitionsIni.GetStringValue(SETTINGS, "GameExecutableNames", "Game.exe").Split(',');

            return exeNames[0];
        }

        public string GetGameLauncherExecutableName => clientDefinitionsIni.GetStringValue(SETTINGS, "GameLauncherExecutableName", string.Empty);

        public bool SaveSkirmishGameOptions => clientDefinitionsIni.GetBooleanValue(SETTINGS, "SaveSkirmishGameOptions", false);

        public bool CreateSavedGamesDirectory => clientDefinitionsIni.GetBooleanValue(SETTINGS, "CreateSavedGamesDirectory", false);

        public bool DisableMultiplayerGameLoading => clientDefinitionsIni.GetBooleanValue(SETTINGS, "DisableMultiplayerGameLoading", false);

        public bool DisableUpdaterOptions => clientDefinitionsIni.GetBooleanValue(SETTINGS, "DisableUpdaterOptions", false);

        public bool DisableComponentOptions => clientDefinitionsIni.GetBooleanValue(SETTINGS, "DisableComponentOptions", false);

        public bool DisplayPlayerCountInTopBar => clientDefinitionsIni.GetBooleanValue(SETTINGS, "DisplayPlayerCountInTopBar", false);

        /// <summary>
        /// The name of the executable in the main game directory that selects 
        /// the correct main client executable.
        /// For example, DTA.exe in case of DTA.
        /// </summary>
        public string LauncherExe => clientDefinitionsIni.GetStringValue(SETTINGS, "LauncherExe", string.Empty);

        public string ClientDefaultResolutionText => clientDefinitionsIni.GetStringValue(SETTINGS, "ClientDefaultResolutionText", "(recommended)");

        public bool UseClientRandomStartLocations => clientDefinitionsIni.GetBooleanValue(SETTINGS, "UseClientRandomStartLocations", false);

        public bool ProcessScreenshots
        {
#if MO
            get { return clientDefinitionsIni.GetBooleanValue(SETTINGS, "ProcessScreenshots", true); }
#else
            get { return false; }
#endif
        }

        /// <summary>
        /// Returns the name of the game executable file that is used on
        /// Linux and macOS.
        /// </summary>
        public string GetUnixGameExecutableName() => clientDefinitionsIni.GetStringValue(SETTINGS, "UnixGameExecutableName", "wine-dta.sh");

        public OSVersion GetOperatingSystemVersion()
        {
            Version osVersion = Environment.OSVersion.Version;

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (osVersion.Major < 5)
                    return OSVersion.UNKNOWN;

                if (osVersion.Major == 5)
                    return OSVersion.WINXP;

                if (osVersion.Minor > 1)
                    return OSVersion.WIN810;
                else if (osVersion.Minor == 0)
                    return OSVersion.WINVISTA;

                return OSVersion.WIN7;
            }

            int p = (int)Environment.OSVersion.Platform;

            // http://mono.wikia.com/wiki/Detecting_the_execution_platform
            if (p == 4 || p == 6 || p == 128)
                return OSVersion.UNIX;

            return OSVersion.UNKNOWN;
        }
    }
}
