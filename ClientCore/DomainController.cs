/// @author Rampastring
/// http://www.moddb.com/members/rampastring

using System;
using System.Text;
using System.IO;
using Rampastring.Tools;

namespace ClientCore
{
    public class DomainController
    {
        private static DomainController _instance;
        public IniFile gameOptions_ini;
        public IniFile DTACnCNetClient_ini;
        private IniFile clientDefinitionsIni;

        private const string CLIENT_SETTINGS = "DTACnCNetClient.ini";
        private const string GAME_OPTIONS = "GameOptions.ini";
        private const string CLIENT_DEFS = "ClientDefinitions.ini";

        /// <summary>
        ///     Default constructor.
        ///     Loads the settings and the language.
        /// </summary>
        protected DomainController()
        {
            // WARNING! This method can NOT contain any methods that use the Singleton pattern
            // to call Domaincontroller methods! If it does call methods that use it, make sure
            // such calls are ignored by checking with the hasInstance() method.

            clientDefinitionsIni = new IniFile(ProgramConstants.GetBaseResourcePath() + CLIENT_DEFS);

            DTACnCNetClient_ini = new IniFile(ProgramConstants.GetResourcePath() + CLIENT_SETTINGS);

            gameOptions_ini = new IniFile(ProgramConstants.GetBaseResourcePath() + GAME_OPTIONS);
        }

        /// <summary>
        ///     Singleton Pattern. Returns the object of this class.
        /// </summary>
        /// <returns>The object of the DomainController class.</returns>
        public static DomainController Instance()
        {
            if (_instance == null)
            {
                _instance = new DomainController();
            }
            return _instance;
        }

        public void RefreshSettings()
        {
            DTACnCNetClient_ini = new IniFile(ProgramConstants.GetResourcePath() + CLIENT_SETTINGS);
        }

        public string GetUILabelColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "UILabelColor", "0,0,0");
        }

        public string GetUIHintTextColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "HintTextColor", "128,128,128");
        }

        public string GetButtonDisabledColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "DisabledButtonColor", "108,108,108");
        }

        public string GetUIAltColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "AltUIColor", "255,255,255");
        }

        public string GetButtonHoverColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "ButtonHoverColor", "255,192,192");
        }

        public string GetMapPreviewNameBackgroundColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "MapPreviewNameBackgroundColor", "0,0,0,144");
        }

        public string GetMapPreviewNameBorderColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "MapPreviewNameBorderColor", "128,128,128,128");
        }

        public string GetMapPreviewStartingLocationHoverRemapColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "StartingLocationHoverColor", "255,255,255,128");
        }

        public string GetUIAltBackgroundColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "AltUIBackgroundColor", "196,196,196");
        }

        public string GetWindowBorderColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "WindowBorderColor", "128,128,128");
        }

        public string GetPanelBorderColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "PanelBorderColor", "255,255,255");
        }

        public string GetListBoxHeaderColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "ListBoxHeaderColor", "255,255,255");
        }

        public float GetDefaultAlphaRate()
        {
            return DTACnCNetClient_ini.GetSingleValue("General", "AlphaRate", 0.005f);
        }

        public float GetCheckBoxAlphaRate()
        {
            return DTACnCNetClient_ini.GetSingleValue("General", "CheckBoxAlphaRate", 0.05f);
        }

        public string GetDefaultChatColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "DefaultChatColor", "0,255,0");
        }

        public string GetDefaultGame()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "DefaultChannel", "DTA");
        }

        public string GetAdminNameColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "AdminNameColor", "255,0,0");
        }

        public string GetReceivedPMColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "PrivateMessageOtherUserColor", "196,196,196");
        }

        public string SentPMColor
        {
            get { return DTACnCNetClient_ini.GetStringValue("General", "PrivateMessageColor", "128,128,128"); }
        }

        public int GetDefaultPersonalChatColor()
        {
            return DTACnCNetClient_ini.GetIntValue("General", "DefaultPersonalChatColorIndex", 0);
        }

        public string GetListBoxFocusColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "ListBoxFocusColor", "64,64,168");
        }

        public string MainMenuMusicName
        {
            get { return DTACnCNetClient_ini.GetStringValue("General", "MainMenuTheme", "mainmenu"); }
        }

        public int GetLoadScreenCount()
        {
            return clientDefinitionsIni.GetIntValue("Settings", "LoadingScreenCount", 2);
        }

        public string GetSides()
        {
            return gameOptions_ini.GetStringValue("General", "Sides", "GDI,Nod,Allies,Soviet");
        }

        public string GetCommonFont()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "CommonFont", "Microsoft Sans Serif,Regular");
        }

        public int GetThemeCount()
        {
            return clientDefinitionsIni.GetSectionKeys("Themes").Count;
        }

        public int GetSendSleepInMs()
        {
            return clientDefinitionsIni.GetIntValue("Settings", "SendSleep", 2500);
        }

        public int MaxNameLength
        {
            get { return clientDefinitionsIni.GetIntValue("Settings", "MaxNameLength", 16); }
        }

        public string[] GetThemeInfoFromIndex(int themeIndex)
        {
            string[] values = clientDefinitionsIni.GetStringValue("Themes", themeIndex.ToString(), ",").Split(',');
            return values;
        }

        public string GetSettingsIniName()
        {
            return clientDefinitionsIni.GetStringValue("Settings", "SettingsFile", "Settings.ini");
        }

        public string GetExtraCommandLineParameters()
        {
            return clientDefinitionsIni.GetStringValue("Settings", "ExtraCommandLineParams", String.Empty);
        }

        public string GetMPMapsIniPath()
        {
            return clientDefinitionsIni.GetStringValue("Settings", "MPMapsPath", "INI\\MPMaps.ini");
        }

        public string GetModDBURL()
        {
            return clientDefinitionsIni.GetStringValue("Links", "ModDB", "http://www.moddb.com/mods/the-dawn-of-the-tiberium-age");
        }

        public string GetFacebookURL()
        {
            return clientDefinitionsIni.GetStringValue("Links", "Facebook", "https://www.facebook.com/DawnOfTheTiberiumAge");
        }

        public string GetYoutubeURL()
        {
            return clientDefinitionsIni.GetStringValue("Links", "Youtube", "https://www.youtube.com/user/BittahCommander");
        }

        public string GetTwitterURL()
        {
            return clientDefinitionsIni.GetStringValue("Links", "Twitter", "https://twitter.com/twistedins");
        }

        public string GetGooglePlusURL()
        {
            return clientDefinitionsIni.GetStringValue("Links", "GooglePlus", "https://plus.google.com/104355642453949180849/");
        }

        public string GetForumURL()
        {
            return clientDefinitionsIni.GetStringValue("Links", "Forum", "http://www.ppmforums.com");
        }

        public string GetHomepageURL()
        {
            return clientDefinitionsIni.GetStringValue("Links", "Homepage", "http://rampastring.cnc-comm.com");
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
