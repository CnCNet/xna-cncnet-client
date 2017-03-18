using System;
using Rampastring.Tools;

namespace ClientCore
{
    public class ClientConfiguration
    {
        private const string CLIENT_SETTINGS = "DTACnCNetClient.ini";
        private const string GAME_OPTIONS = "GameOptions.ini";
        private const string CLIENT_DEFS = "ClientDefinitions.ini";

        private static ClientConfiguration _instance;

        private IniFile gameOptions_ini;
        private IniFile DTACnCNetClient_ini;
        private IniFile clientDefinitionsIni;

        protected ClientConfiguration()
        {
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
                return DTACnCNetClient_ini.GetStringValue("General", "UILabelColor", "0,0,0");
            }
        }

        public string UIHintTextColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue("General", "HintTextColor", "128,128,128");
            }
        }

        public string DisabledButtonColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue("General", "DisabledButtonColor", "108,108,108");
            }
        }

        public string AltUIColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue("General", "AltUIColor", "255,255,255");
            }
        }

        public string ButtonHoverColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue("General", "ButtonHoverColor", "255,192,192");
            }
        }

        public string MapPreviewNameBackgroundColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue("General", "MapPreviewNameBackgroundColor", "0,0,0,144");
            }
        }

        public string MapPreviewNameBorderColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue("General", "MapPreviewNameBorderColor", "128,128,128,128");
            }
        }

        public string MapPreviewStartingLocationHoverRemapColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue("General", "StartingLocationHoverColor", "255,255,255,128");
            }
        }

        public string AltUIBackgroundColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue("General", "AltUIBackgroundColor", "196,196,196");
            }
        }

        public string WindowBorderColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue("General", "WindowBorderColor", "128,128,128");
            }
        }

        public string PanelBorderColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue("General", "PanelBorderColor", "255,255,255");
            }
        }

        public string ListBoxHeaderColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue("General", "ListBoxHeaderColor", "255,255,255");
            }
        }

        public float DefaultAlphaRate
        {
            get
            {
                return DTACnCNetClient_ini.GetSingleValue("General", "AlphaRate", 0.005f);
            }
        }

        public float CheckBoxAlphaRate
        {
            get
            {
                return DTACnCNetClient_ini.GetSingleValue("General", "CheckBoxAlphaRate", 0.05f);
            }
        }

        public string DefaultChatColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue("General", "DefaultChatColor", "0,255,0");
            }
        }

        public string AdminNameColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue("General", "AdminNameColor", "255,0,0");
            }
        }

        public string ReceivedPMColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue("General", "PrivateMessageOtherUserColor", "196,196,196");
            }
        }

        public string SentPMColor
        {
            get { return DTACnCNetClient_ini.GetStringValue("General", "PrivateMessageColor", "128,128,128"); }
        }

        public int DefaultPersonalChatColorIndex
        {
            get
            {
                return DTACnCNetClient_ini.GetIntValue("General", "DefaultPersonalChatColorIndex", 0);
            }
        }

        public string ListBoxFocusColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue("General", "ListBoxFocusColor", "64,64,168");
            }
        }

        public string HoverOnGameColor
        {
            get
            {
                return DTACnCNetClient_ini.GetStringValue("General", "HoverOnGameColor", "32,32,84");
            }
        }

        public string MainMenuMusicName
        {
            get { return DTACnCNetClient_ini.GetStringValue("General", "MainMenuTheme", "mainmenu"); }
        }

        public int LoadingScreenCount
        {
            get
            {
                return clientDefinitionsIni.GetIntValue("Settings", "LoadingScreenCount", 2);
            }
        }

        public string GetSides()
        {
            return gameOptions_ini.GetStringValue("General", "Sides", "GDI,Nod,Allies,Soviet");
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
                return clientDefinitionsIni.GetStringValue("Settings", "LocalGame", "DTA");
            }
        }

        public int SendSleep
        {
            get
            {
                return clientDefinitionsIni.GetIntValue("Settings", "SendSleep", 2500);
            }
        }

        public bool SidebarHack
        {
            get
            {
                return clientDefinitionsIni.GetBooleanValue("Settings", "SidebarHack", false);
            }
        }

        public int MinimumRenderWidth
        {
            get
            {
                return clientDefinitionsIni.GetIntValue("Settings", "MinimumRenderWidth", 1280);
            }
        }

        public int MinimumRenderHeight
        {
            get
            {
                return clientDefinitionsIni.GetIntValue("Settings", "MinimumRenderHeight", 768);
            }
        }

        public int MaximumRenderWidth
        {
            get
            {
                return clientDefinitionsIni.GetIntValue("Settings", "MaximumRenderWidth", 1280);
            }
        }

        public int MaximumRenderHeight
        {
            get
            {
                return clientDefinitionsIni.GetIntValue("Settings", "MaximumRenderHeight", 800);
            }
        }

        public string InstallationPathRegKey
        {
            get
            {
                return clientDefinitionsIni.GetStringValue("Settings", "RegistryInstallPath", "TiberianSun");
            }
        }

        public string CnCNetLiveStatusIdentifier
        {
            get
            {
                return clientDefinitionsIni.GetStringValue("Settings", "CnCNetLiveStatusIdentifier", "cncnet5_ts");
            }
        }

        public string BattleFSFileName
        {
            get
            {
                return clientDefinitionsIni.GetStringValue("Settings", "BattleFSFileName", "BattleFS.ini");
            }
        }

        public string MapEditorExePath
        {
            get
            {
                return clientDefinitionsIni.GetStringValue("Settings", "MapEditorExePath", "FinalSun\\FinalSun.exe");
            }
        }

        public bool ModMode
        {
            get
            {
                return clientDefinitionsIni.GetBooleanValue("Settings", "ModMode", false);
            }
        }

        public string LongGameName
        {
            get
            {
                return clientDefinitionsIni.GetStringValue("Settings", "LongGameName", "Tiberian Sun");
            }
        }

        public string LongSupportURL
        {
            get
            {
                return clientDefinitionsIni.GetStringValue("Settings", "LongSupportURL", "http://www.moddb.com/members/rampastring");
            }
        }

        public string ShortSupportURL
        {
            get
            {
                return clientDefinitionsIni.GetStringValue("Settings", "ShortSupportURL", "www.moddb.com/members/rampastring");
            }
        }

        public string ChangelogURL
        {
            get
            {
                return clientDefinitionsIni.GetStringValue("Settings", "ChangelogURL", "http://www.moddb.com/mods/the-dawn-of-the-tiberium-age/tutorials/change-log");
            }
        }

        public string CreditsURL
        {
            get
            {
                return clientDefinitionsIni.GetStringValue("Settings", "CreditsURL", "http://www.moddb.com/mods/the-dawn-of-the-tiberium-age/tutorials/credits#Rampastring");
            }
        }

        public string FinalSunIniPath
        {
            get
            {
                return clientDefinitionsIni.GetStringValue("Settings", "FSIniPath", "FinalSun\\FinalSun.ini");
            }
        }

        public int MaxNameLength
        {
            get { return clientDefinitionsIni.GetIntValue("Settings", "MaxNameLength", 16); }
        }

        public int MapCellSizeX
        {
            get { return clientDefinitionsIni.GetIntValue("Settings", "MapCellSizeX", 48); }
        }

        public int MapCellSizeY
        {
            get { return clientDefinitionsIni.GetIntValue("Settings", "MapCellSizeY", 24); }
        }

        public string[] GetThemeInfoFromIndex(int themeIndex)
        {
            string[] values = clientDefinitionsIni.GetStringValue("Themes", themeIndex.ToString(), ",").Split(',');
            return values;
        }

        public string SettingsIniName
        {
            get
            {
                return clientDefinitionsIni.GetStringValue("Settings", "SettingsFile", "Settings.ini");
            }
        }

        public string ExtraExeCommandLineParameters
        {
            get
            {
                return clientDefinitionsIni.GetStringValue("Settings", "ExtraCommandLineParams", String.Empty);
            }
        }

        public string MPMapsIniPath
        {
            get
            {
                return clientDefinitionsIni.GetStringValue("Settings", "MPMapsPath", "INI\\MPMaps.ini");
            }
        }

        public string ModDBURL
        {
            get
            {
                return clientDefinitionsIni.GetStringValue("Links", "ModDB", "http://www.moddb.com/mods/the-dawn-of-the-tiberium-age");
            }
        }

        public string FacebookURL
        {
            get
            {
                return clientDefinitionsIni.GetStringValue("Links", "Facebook", "https://www.facebook.com/DawnOfTheTiberiumAge");
            }
        }

        public string YoutubeURL
        {
            get
            {
                return clientDefinitionsIni.GetStringValue("Links", "Youtube", "https://www.youtube.com/user/BittahCommander");
            }
        }

        public string TwitterURL
        {
            get
            {
                return clientDefinitionsIni.GetStringValue("Links", "Twitter", "https://twitter.com/twistedins");
            }
        }

        public string GooglePlusURL
        {
            get
            {
                return clientDefinitionsIni.GetStringValue("Links", "GooglePlus", "https://plus.google.com/104355642453949180849/");
            }
        }

        public string ForumURL
        {
            get
            {
                return clientDefinitionsIni.GetStringValue("Links", "Forum", "http://www.ppmforums.com");
            }
        }

        public string HomepageURL
        {
            get
            {
                return clientDefinitionsIni.GetStringValue("Links", "Homepage", "http://rampastring.cnc-comm.com");
            }
        }

        public bool CopyMissionsToSpawnmapINI
        {
            get { return clientDefinitionsIni.GetBooleanValue("Settings", "CopyMissionsToSpawnmapINI", true); }
        }

        public string GetGameExecutableName(int id)
        {
            string[] exeNames = clientDefinitionsIni.GetStringValue("Settings", "GameExecutableNames", "Game.exe").Split(','); ;

            if (id < 0 || id >= exeNames.Length)
                return exeNames[0];

            return exeNames[id];
        }

        public OSVersion GetOperatingSystemVersion()
        {
            Version osVersion = Environment.OSVersion.Version;

            if (Environment.OSVersion.Platform == PlatformID.Win32Windows)
                return OSVersion.WIN9X;

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

            return OSVersion.UNKNOWN;
        }
    }
}
