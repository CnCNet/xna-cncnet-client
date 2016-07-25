using System;
using System.Text;
using System.Windows.Forms;
using System.IO;
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
                settings_ini.FileName = settingsPath;
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
                settings_ini.FileName = settingsPath;
            }
            else
                settings_ini = new IniFile(settingsPath);
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

        public bool GetModModeStatus()
        {
            return mainClient_ini.GetBooleanValue("General", "ModMode", false);
        }

        public int GetMinimumRenderWidth()
        {
            return mainClient_ini.GetIntValue("General", "MinimumRenderWidth", 1280);
        }

        public int GetMinimumRenderHeight()
        {
            return mainClient_ini.GetIntValue("General", "MinimumRenderHeight", 768);
        }

        public int GetMaximumRenderWidth()
        {
            return mainClient_ini.GetIntValue("General", "MaximumRenderWidth", 1280);
        }

        public int GetMaximumRenderHeight()
        {
            return mainClient_ini.GetIntValue("General", "MaximumRenderHeight", 800);
        }

        public bool GetSidebarHackStatus()
        {
            return mainClient_ini.GetBooleanValue("General", "SidebarHack", false);
        }

        public bool Win8CompatFixInstalled()
        {
            return settings_ini.GetBooleanValue("Compatibility", "Win8FixInstalled", false);
        }

        public void SetWin8CompatFixInstalled(bool status)
        {
            settings_ini.SetBooleanValue("Compatibility", "Win8FixInstalled", status);
            settings_ini.WriteIniFile();
        }
    }
}
