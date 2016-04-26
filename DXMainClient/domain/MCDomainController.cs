using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Principal;
using System.Windows.Forms;
using System.IO;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.domain
{
    class MCDomainController
    {
        private static MCDomainController _instance;
        private IniFile settings_ini;
        private IniFile mainClient_ini;

        private const String CLIENT_DEFINITIONS = "Resources\\MainClient.ini";

        /// <summary>
        ///     Default constructor.
        ///     Loads the settings and the language.
        /// </summary>
        protected MCDomainController()
        {
            // WARNING! This method can NOT contain any methods that use the Singleton pattern
            // to call Domaincontroller methods! If it does call methods that use it, make sure
            // such calls are ignored by checking with the hasInstance() method.
            settings_ini = null;
            String settingsPath = MainClientConstants.gamepath + MainClientConstants.GAME_SETTINGS;

            if (!File.Exists(settingsPath))
            {
                byte[] byteArray = Encoding.GetEncoding(1252).GetBytes(DTAClient.Properties.Resources.settings_ini);
                MemoryStream stream = new MemoryStream(byteArray);
                settings_ini = new IniFile(stream);
                settings_ini.SetFilePath(settingsPath);
            }
            else
                settings_ini = new IniFile(settingsPath);

            string clientDefinitionsPath = MainClientConstants.gamepath + CLIENT_DEFINITIONS;
            mainClient_ini = new IniFile(clientDefinitionsPath);
        }

        /// <summary>
        ///     Singleton Pattern. Returns the object of this class.
        /// </summary>
        /// <returns>The object of the DomainController class.</returns>
        public static MCDomainController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MCDomainController();
                }

                return _instance;
            }
        }

        public void ReloadSettings()
        {
            settings_ini = null;
            String settingsPath = MainClientConstants.gamepath + MainClientConstants.GAME_SETTINGS;

            if (!File.Exists(settingsPath))
            {
                byte[] byteArray = Encoding.GetEncoding(1252).GetBytes(DTAClient.Properties.Resources.settings_ini);
                MemoryStream stream = new MemoryStream(byteArray);
                settings_ini = new IniFile(stream);
                settings_ini.SetFilePath(settingsPath);
            }
            else
                settings_ini = new IniFile(settingsPath);
        }

        // functions used for detecting launcher settings

        public bool GetAutomaticUpdateStatus()
        {
            return settings_ini.GetBooleanValue("Options", "CheckforUpdates", true);
        }

        public int GetGameSpeed()
        {
            return settings_ini.GetIntValue("Options", "GameSpeed", 1);
        }

        public int GetDifficultyMode()
        {
            return settings_ini.GetIntValue("Options", "Difficulty", 1);
        }

        public int GetClientResolutionX()
        {
            return settings_ini.GetIntValue("Video", "ClientResolutionX", Screen.PrimaryScreen.Bounds.Width);
        }

        public int GetClientResolutionY()
        {
            return settings_ini.GetIntValue("Video", "ClientResolutionY", Screen.PrimaryScreen.Bounds.Height);
        }

        public bool GetMapPreviewPreloadStatus()
        {
            return settings_ini.GetBooleanValue("Video", "PreloadMapPreviews", false);
        }

        public bool GetBorderlessWindowedStatus()
        {
            return settings_ini.GetBooleanValue("Video", "BorderlessWindowedClient", true);
        }

        public string GetShortGameName()
        {
            return mainClient_ini.GetStringValue("General", "ShortGameName", "TS");
        }

        public string GetLongGameName()
        {
            return mainClient_ini.GetStringValue("General", "LongGameName", "Tiberian Sun");
        }

        public string GetLongSupportURL()
        {
            return mainClient_ini.GetStringValue("General", "LongSupportURL", "http://www.moddb.com/members/rampastring");
        }

        public string GetShortSupportURL()
        {
            return mainClient_ini.GetStringValue("General", "ShortSupportURL", "www.moddb.com/members/rampastring");
        }

        public string GetChangelogURL()
        {
            return mainClient_ini.GetStringValue("General", "ChangelogURL", "http://www.moddb.com/mods/the-dawn-of-the-tiberium-age/tutorials/change-log");
        }

        public string GetCreditsURL()
        {
            return mainClient_ini.GetStringValue("General", "CreditsURL", "http://www.moddb.com/mods/the-dawn-of-the-tiberium-age/tutorials/credits#Rampastring");
        }

        public string GetFinalSunIniPath()
        {
            return mainClient_ini.GetStringValue("General", "FSIniPath", "FinalSun\\FinalSun.ini");
        }

        public string GetInstallationPathRegKey()
        {
            return mainClient_ini.GetStringValue("General", "RegistryInstallPath", "TiberianSun");
        }

        public string GetCnCNetLiveStatusIdentifier()
        {
            return mainClient_ini.GetStringValue("General", "CnCNetLiveStatusIdentifier", "cncnet5_ts");
        }

        public string GetBattleFSFileName()
        {
            return mainClient_ini.GetStringValue("General", "BattleFSFileName", "BattleFS.ini");
        }

        public string GetMapEditorExePath()
        {
            return mainClient_ini.GetStringValue("General", "MapEditorExePath", "FinalSun\\FinalSun.exe");
        }

        public string GetCnCNetGameCountStatusText()
        {
            return mainClient_ini.GetStringValue("General", "CnCNetGameCountStatusText", "{0} games hosted at CnCNet:");
        }

        public bool GetModModeStatus()
        {
            return mainClient_ini.GetBooleanValue("General", "ModMode", false);
        }

        public int GetMinimumRenderWidth()
        {
            return mainClient_ini.GetIntValue("General", "MinimumRenderWidth", 800);
        }

        public int GetMinimumRenderHeight()
        {
            return mainClient_ini.GetIntValue("General", "MinimumRenderHeight", 600);
        }

        public bool GetSidebarHackStatus()
        {
            return mainClient_ini.GetBooleanValue("General", "SidebarHack", false);
        }

        public bool IsFirstRun()
        {
            return settings_ini.GetBooleanValue("Options", "IsFirstRun", true);
        }

        public bool GetInstallationPathWriteStatus()
        {
            return settings_ini.GetBooleanValue("Options", "WriteInstallationPathToRegistry", true);
        }

        public bool Win8CompatFixInstalled()
        {
            return settings_ini.GetBooleanValue("Compatibility", "Win8FixInstalled", false);
        }

        public void SetFirstRun()
        {
            settings_ini.SetBooleanValue("Options", "IsFirstRun", false);
            settings_ini.WriteIniFile();
        }

        public void SetWin8CompatFixInstalled(bool status)
        {
            settings_ini.SetBooleanValue("Compatibility", "Win8FixInstalled", status);
            settings_ini.WriteIniFile();
        }

        /// <summary>
        /// Saves settings used for campaign and new missions.
        /// </summary>
        /// <param name="difficultyMode"></param>
        public void SaveSingleplayerSettings(int difficultyMode)
        {
            settings_ini.SetIntValue("Options", "Difficulty", difficultyMode);
            settings_ini.SetBooleanValue("Options", "ForceLowestDetailLevel", false);
            settings_ini.WriteIniFile();
        }
    }
}
