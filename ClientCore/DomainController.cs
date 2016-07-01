/// @author Rampastring
/// http://www.moddb.com/members/rampastring

using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Principal;
using System.Windows.Forms;
using System.IO;
using ClientCore.CnCNet5.Games;
using Rampastring.Tools;

namespace ClientCore
{
    public class DomainController
    {
        private static DomainController _instance;
        private IniFile settings_ini;
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
            settings_ini = null;

            clientDefinitionsIni = new IniFile(ProgramConstants.GamePath + ProgramConstants.BASE_RESOURCE_PATH + CLIENT_DEFS);

            string clientSettingsPath = ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + CLIENT_SETTINGS;
            DTACnCNetClient_ini = new IniFile(clientSettingsPath);

            gameOptions_ini = new IniFile(ProgramConstants.GamePath + ProgramConstants.BASE_RESOURCE_PATH + GAME_OPTIONS);

            string settingsPath = ProgramConstants.GamePath + GetSettingsIniName();

            settings_ini = new IniFile(settingsPath);
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

        public void ReloadSettings()
        {
            settings_ini = null;
            String settingsPath = ProgramConstants.GamePath + GetSettingsIniName();

            if (!File.Exists(settingsPath))
            {
                byte[] byteArray = Encoding.GetEncoding(1252).GetBytes(ClientCore.Properties.Resources.Settings);
                MemoryStream stream = new MemoryStream(byteArray);
                settings_ini = new IniFile(stream);
                settings_ini.FileName = settingsPath;
            }
            else
                settings_ini = new IniFile(settingsPath);

            String clientSettingsPath = ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + CLIENT_SETTINGS;
            DTACnCNetClient_ini = new IniFile(clientSettingsPath);
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

        public string GetDropDownBorderColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "DropDownBorderColor", "164,164,164");
        }

        public string GetTrackBarBackColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "TrackBarBackgroundColor", "255,64,128");
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

        public string GetPlayerNameColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "PlayerNameColor", "0,255,0");
        }

        public string GetReceivedPMColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "PrivateMessageOtherUserColor", "196,196,196");
        }

        public string SentPMColor
        {
            get { return DTACnCNetClient_ini.GetStringValue("General", "PrivateMessageColor", "128,128,128"); }
        }

        public string GetComboBoxOutlineColor()
        {
            return DTACnCNetClient_ini.GetStringValue("GameLobby", "ComboBoxOutlineColor", "255,128,128");
        }

        public int GetDefaultPersonalChatColor()
        {
            return DTACnCNetClient_ini.GetIntValue("General", "DefaultPersonalChatColorIndex", 0);
        }

        public string GetBriefingForeColor()
        {
            return DTACnCNetClient_ini.GetStringValue("GameLobby", "CoopBriefingForeColor", "0,255,0");
        }

        public string GetLockedGameColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "LockedGameColor", "64,64,64");
        }

        public string GetListBoxFocusColor()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "ListBoxFocusColor", "64,64,168");
        }

        public string GetCnCNetLobbyBackgroundImageLayout()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "BackgroundImageLayout", "Tile");
        }

        public string GetGameLobbyBackgroundImageLayout()
        {
            return DTACnCNetClient_ini.GetStringValue("GameLobby", "BackgroundImageLayout", "Tile");
        }

        public string GetReadyBoxText()
        {
            return DTACnCNetClient_ini.GetStringValue("GameLobby", "ReadyBoxText", "Ready");
        }

        public int GetReadyBoxXCoordinate()
        {
            return DTACnCNetClient_ini.GetIntValue("GameLobby", "ReadyBoxX", 500);
        }

        public string GetGameOptionFont()
        {
            return DTACnCNetClient_ini.GetStringValue("GameLobby", "GameOptionFont", "Microsoft Sans Serif,Regular,8.25");
        }

        public string GetMapInfoStyle()
        {
            return DTACnCNetClient_ini.GetStringValue("GameLobby", "MapInfoStyle", "STANDARD");
        }

        public string GetButtonLabelStyle()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "ButtonInfoStyle", "STANDARD");
        }

        public string GetGameLobbyLogoStyle()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "LogoStyle", "Default");
        }

        public string GetMPColorOne()
        {
            return gameOptions_ini.GetStringValue("GameLobby", "MPColorOne", "255,255,255");
        }

        public string GetMPColorTwo()
        {
            return gameOptions_ini.GetStringValue("GameLobby", "MPColorTwo", "255,255,255");
        }

        public string GetMPColorThree()
        {
            return gameOptions_ini.GetStringValue("GameLobby", "MPColorThree", "255,255,255");
        }

        public string GetMPColorFour()
        {
            return gameOptions_ini.GetStringValue("GameLobby", "MPColorFour", "255,255,255");
        }

        public string GetMPColorFive()
        {
            return gameOptions_ini.GetStringValue("GameLobby", "MPColorFive", "255,255,255");
        }

        public string GetMPColorSix()
        {
            return gameOptions_ini.GetStringValue("GameLobby", "MPColorSix", "255,255,255");
        }

        public string GetMPColorSeven()
        {
            return gameOptions_ini.GetStringValue("GameLobby", "MPColorSeven", "255,255,255");
        }

        public string GetMPColorEight()
        {
            return gameOptions_ini.GetStringValue("GameLobby", "MPColorEight", "255,255,255");
        }

        public string GetMPColorNames()
        {
            return gameOptions_ini.GetStringValue("GameLobby", "MPColorNames", "Gold,Red,Teal,Green,Orange,Blue,Purple,Metalic");
        }

        public int GetLoadScreenCount()
        {
            return clientDefinitionsIni.GetIntValue("Settings", "LoadingScreenCount", 2);
        }

        public string GetSides()
        {
            return gameOptions_ini.GetStringValue("General", "Sides", "GDI,Nod,Allies,Soviet");
        }

        public string GetPanelBorderStyle()
        {
            return DTACnCNetClient_ini.GetStringValue("GameLobby", "PanelBorderStyle", "FixedSingle");
        }

        public string GetOptionsPanelBorderStyle()
        {
            return DTACnCNetClient_ini.GetStringValue("GameLobby", "OptionsPanelBorderStyle", "FixedSingle");
        }

        public string GetComboBoxNondefaultColor()
        {
            return DTACnCNetClient_ini.GetStringValue("GameLobby", "ComboBoxNondefaultColor", "0,0,0");
        }

        public string GetGameLobbyPersistentCheckBox()
        {
            return gameOptions_ini.GetStringValue("GameLobby", "GameLobbyPersistentCheckBox", "none");
        }

        public bool GetImageSharpeningCnCNetStatus()
        {
            return gameOptions_ini.GetBooleanValue("GameLobby", "SharpenImages", true);
        }

        public bool GetImageSharpeningSkirmishStatus()
        {
            return gameOptions_ini.GetBooleanValue("SkirmishLobby", "SharpenImages", true);
        }

        public string GetCommonFont()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "CommonFont", "Microsoft Sans Serif,Regular");
        }

        public string GetListBoxFont()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "ListBoxFont", "Microsoft Sans Serif,Regular,8.25");
        }

        public string GetChatTipText()
        {
            return DTACnCNetClient_ini.GetStringValue("General", "ChatTipText", String.Empty);
        }

        public bool GetNativeScrollbarStatus()
        {
            return DTACnCNetClient_ini.GetBooleanValue("General", "UseNativeScrollbar", false);
        }

        public string GetWindowSizeCnCNet()
        {
            return gameOptions_ini.GetStringValue("GameLobby", "DefaultWindowSize", "1011x679");
        }

        public string GetMinimumWindowSizeCnCNet()
        {
            return gameOptions_ini.GetStringValue("GameLobby", "MinimumWindowSize", "1011x517");
        }

        public string GetWindowSizeSkirmish()
        {
            return gameOptions_ini.GetStringValue("SkirmishLobby", "DefaultWindowSize", "898x660");
        }

        public string GetMinimumWindowSizeSkirmish()
        {
            return gameOptions_ini.GetStringValue("SkirmishLobby", "MinimumWindowSize", "898x560");
        }

        public int GetSideComboboxWidth()
        {
            return gameOptions_ini.GetIntValue("GameLobby", "SideComboboxWidth", 80);
        }

        public string GetWindowSizeMapSelection()
        {
            return gameOptions_ini.GetStringValue("GameLobby", "MapSelectionScreenSize", "840x480");
        }

        public bool GetGameEnabledStatus(string gameIdentifier)
        {
             return settings_ini.GetBooleanValue("Channels", gameIdentifier.ToUpper(), false);
        }

        // functions used for detecting client settings

        public int GetCnCNetChatColor()
        {
            return settings_ini.GetIntValue("MultiPlayer", "ChatColor", GetDefaultPersonalChatColor());
        }

        public bool GetCustomTunnelPingStatus()
        {
            return settings_ini.GetBooleanValue("MultiPlayer", "PingCustomTunnels", true);
        }

        public bool GetMainMenuMusicStatus()
        {
            return settings_ini.GetBooleanValue("Audio", "PlayCnCNetLobbyMusic", true);
        }

        public bool GetButtonHoverSoundStatus()
        {
            return settings_ini.GetBooleanValue("Audio", "EnableButtonHoverSound", true);
        }

        public bool GetMessageSoundStatus()
        {
            return settings_ini.GetBooleanValue("Options", "MessageSound", true);
        }

        public bool GetUserJoinLeaveNotificationStatus()
        {
            return settings_ini.GetBooleanValue("Options", "NotifyOnUserListChange", true);
        }

        public FormWindowState GetGameLobbyWindowState()
        {
            string windowState = settings_ini.GetStringValue("Options", "GameLobbyWindowState", "Normal");
            if (windowState == "Maximized")
                return FormWindowState.Maximized;

            return FormWindowState.Normal;
        }

        public bool GetWindowMinimizingStatus()
        {
            return settings_ini.GetBooleanValue("Options", "MinimizeWindowsOnGameStart", true);
        }

        public bool GetWindowedStatus()
        {
            return settings_ini.GetBooleanValue("Video", "Video.Windowed", false);
        }

        public int GetLobbyLocationY()
        {
            return settings_ini.GetIntValue("MultiPlayer", "LobbyLocationY", -1);
        }

        public int GetLobbyLocationX()
        {
            return settings_ini.GetIntValue("MultiPlayer", "LobbyLocationX", -1);
        }

        public string GetSkirmishMapMD5()
        {
            return settings_ini.GetStringValue("Skirmish", "Map", "default");
        }

        public string GetSkirmishGameMode()
        {
            return settings_ini.GetStringValue("Skirmish", "GameMode", "Default");
        }

        public string GetSkirmishDifficulties()
        {
            return settings_ini.GetStringValue("Skirmish", "Difficulties", String.Empty);
        }

        public string GetSkirmishSides()
        {
            return settings_ini.GetStringValue("Skirmish", "Sides", String.Empty);
        }

        public string GetSkirmishColors()
        {
            return settings_ini.GetStringValue("Skirmish", "Colors", String.Empty);
        }

        public string GetSkirmishStartingLocations()
        {
            return settings_ini.GetStringValue("Skirmish", "StartLocs", String.Empty);
        }

        public string GetSkirmishTeams()
        {
            return settings_ini.GetStringValue("Skirmish", "Teams", String.Empty);
        }

        public string GetSkirmishSettings()
        {
            return settings_ini.GetStringValue("Skirmish", "Settings", String.Empty);
        }

        public int GetThemeCount()
        {
            return clientDefinitionsIni.GetSectionKeys("Themes").Count;
        }

        public int GetSelectedThemeId()
        {
            return settings_ini.GetIntValue("MultiPlayer", "Theme", 0);
        }

        public int GetSendSleepInMs()
        {
            return settings_ini.GetIntValue("MultiPlayer", "SendSleep", 2500);
        }

        public bool GetGameCreatedBroadcastLocalStatus()
        {
            return settings_ini.GetBooleanValue("MultiPlayer", "BroadcastGameCreationLocally", false);
        }

        public bool GetGameCreatedBroadcastGlobalStatus()
        {
            return settings_ini.GetBooleanValue("MultiPlayer", "BroadcastGameCreationGlobally", false);
        }

        public bool EnablePrivateMessageSound
        {
            get { return settings_ini.GetBooleanValue("Audio", "PrivateMessageSound", true); }
        }

        public bool EnableMessageSound
        {
            get { return settings_ini.GetBooleanValue("Audio", "ChatMessageSound", true); }
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

        public string GetConfigToolWindowTitle()
        {
            return clientDefinitionsIni.GetStringValue("Settings", "ConfigWindowTitle", "ClientDefinitions.ini -> ConfigWindowTitle");
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

        public bool GetGameHostedSoundEnabledStatus()
        {
            return settings_ini.GetBooleanValue("MultiPlayer", "PlaySoundOnGameHosted", true);
        }

        public int GetLastUpdateDay()
        {
            return settings_ini.GetIntValue("Options", "LastUpdateDay", 1);
        }

        public void SetLastUpdateDay()
        {
            settings_ini.SetIntValue("Options", "LastUpdateDay", DateTime.Now.Day);
        }

        public void WriteSettingsIni()
        {
            settings_ini.WriteIniFile();
        }

        public string GetGameExecutableName(int id)
        {
            string[] exeNames = clientDefinitionsIni.GetStringValue("Settings", "GameExecutableNames", "Game.exe").Split(','); ;

            if (id < 0 || id >= exeNames.Length)
                return exeNames[0];

            return exeNames[id];
        }

        public String GetCurrentGameRes(bool isWindowed, out bool success)
        {
            int width = 800;
            int height = 600;
            width = settings_ini.GetIntValue("Video","ScreenWidth", 800, out success);
            if (success)
                height = settings_ini.GetIntValue("Video", "ScreenHeight", 600, out success);
            if (isWindowed)
            {
                if (width + 6 == Screen.PrimaryScreen.Bounds.Width)
                    width = width + 6;
                if (height + 65 == Screen.PrimaryScreen.Bounds.Height)
                    height = height + 65;

                return width + "x" + height;
            }

            return width + "x" + height;
        }

        public int GetCnCNetPort()
        {
            return settings_ini.GetIntValue("MultiPlayer", "CnCNet_Port_Index", 0);
        }

        public bool GetCnCNetAutologinStatus()
        {
            return settings_ini.GetBooleanValue("MultiPlayer", "AutomaticCnCNetLogin", false);
        }

        public bool GetCnCNetConnectDialogSkipStatus()
        {
            return settings_ini.GetBooleanValue("MultiPlayer", "SkipConnectDialog", false);
        }

        public bool GetCnCNetPersistentModeStatus()
        {
            return settings_ini.GetBooleanValue("MultiPlayer", "PersistentMode", false);
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

        public String GetMpHandle()
        {
            String name = settings_ini.GetStringValue("MultiPlayer", "Handle", String.Empty);
            name = SetMpHandle(name.Trim());
            return name;
        }

        private string SetMpHandle(String name)
        {
            if (name.Equals(String.Empty) || name.Equals("[NONAME]"))
            {
                name = WindowsIdentity.GetCurrent().Name;

                name = name.Substring(name.IndexOf("\\") + 1);
            }

            if (name.Length > 16) 
                name = name.Substring(0, 16);
            return name;
        }

        public void SaveCnCNetSettings(string name, bool skipConnectDialog, bool persistentMode, bool autoLogin)
        {
            Logger.Log("Saving CnCNet multiplayer settings.");

            settings_ini.SetStringValue("MultiPlayer", "Handle", name);

            settings_ini.SetBooleanValue("MultiPlayer", "SkipConnectDialog", skipConnectDialog);
            settings_ini.SetBooleanValue("MultiPlayer", "PersistentMode", persistentMode);
            settings_ini.SetBooleanValue("MultiPlayer", "AutomaticCnCNetLogin", autoLogin);
            settings_ini.SetBooleanValue("Options", "ForceLowestDetailLevel", false);

            settings_ini.WriteIniFile();
        }

        public void SaveMPHandle()
        {
            settings_ini.SetStringValue("MultiPlayer", "Handle", ProgramConstants.PLAYERNAME);
        }

        public void SaveLobbyPosition(int x, int y)
        {
            settings_ini.SetIntValue("MultiPlayer", "LobbyLocationX", x);
            settings_ini.SetIntValue("MultiPlayer", "LobbyLocationY", y);
        }

        public void SaveLobbyMusicSettings(bool enableMusic)
        {
            Logger.Log("Saving CnCNet lobby music setting.");

            settings_ini.SetBooleanValue("Audio", "PlayCnCNetLobbyMusic", enableMusic);

            settings_ini.WriteIniFile();
        }

        public void SaveCnCNetColorSetting(int colorId)
        {
            settings_ini.SetIntValue("MultiPlayer", "ChatColor", colorId);

            settings_ini.WriteIniFile();
        }

        public void SaveChannelSettings()
        {
            for (int i = 0; i < GameCollection.Instance.GetGameCount(); i++)
            {
                settings_ini.SetBooleanValue("Channels", GameCollection.Instance.GetGameIdentifierFromIndex(i).ToUpper(), false);
            }

            List<string> enabledChannels = GameCollection.Instance.GetInternalNamesOfFollowedGames();

            foreach (string channelName in enabledChannels)
            {
                settings_ini.SetBooleanValue("Channels", channelName.ToUpper(), true);
            }

            settings_ini.WriteIniFile();
        }

        /// <summary>
        /// Saves settings used for skirmish games.
        /// </summary>
        public void SaveSkirmishSettings(string mapmd5, string gameMode, string difficulties,
            string sides, string colors, string startLocs, string teams, string settings)
        {
            string name = SetMpHandle(ProgramConstants.PLAYERNAME);

            settings_ini.SetStringValue("MultiPlayer", "Handle", name);
            settings_ini.SetStringValue("Skirmish", "Map", mapmd5);
            settings_ini.SetStringValue("Skirmish", "GameMode", gameMode);
            settings_ini.SetStringValue("Skirmish", "Difficulties", difficulties);
            settings_ini.SetStringValue("Skirmish", "Sides", sides);
            settings_ini.SetStringValue("Skirmish", "Colors", colors);
            settings_ini.SetStringValue("Skirmish", "StartLocs", startLocs);
            settings_ini.SetStringValue("Skirmish", "Teams", teams);
            settings_ini.SetStringValue("Skirmish", "Settings", settings);
            settings_ini.SetBooleanValue("Options", "ForceLowestDetailLevel", false);
            settings_ini.WriteIniFile();
        }

        public void SaveGameLobbySettings(bool enableChatSounds, FormWindowState windowState)
        {
            settings_ini.SetBooleanValue("Options", "MessageSound", enableChatSounds);
            if (windowState == FormWindowState.Maximized)
                settings_ini.SetStringValue("Options", "GameLobbyWindowState", "Maximized");
            else
                settings_ini.SetStringValue("Options", "GameLobbyWindowState", "Normal");
        }
    }
}
