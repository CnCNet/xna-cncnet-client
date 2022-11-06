using System;
using System.IO;
using ClientCore;
using Newtonsoft.Json;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models
{
    public class QmUserSettings
    {
        private static readonly string SettingsFile = $"{ProgramConstants.ClientUserFilesPath}QuickMatchSettings.ini";

        private const string BasicSectionKey = "Basic";
        private const string AuthSectionKey = "Auth";
        private const string AuthDataKey = "AuthData";
        private const string EmailKey = "Email";
        private const string LadderKey = "Ladder";
        private const string SideKey = "Side";

        public string Email { get; set; }

        public string Ladder { get; set; }

        public int? SideId { get; set; }

        public QmAuthData AuthData { get; set; }

        private QmUserSettings()
        {
        }

        public static QmUserSettings Load()
        {
            var settings = new QmUserSettings();
            if (!File.Exists(SettingsFile))
                return settings;

            var iniFile = new IniFile(SettingsFile);
            LoadAuthSettings(iniFile, settings);
            LoadBasicSettings(iniFile, settings);

            return settings;
        }

        public void ClearAuthData() => AuthData = null;

        public void Save()
        {
            var iniFile = new IniFile();
            var authSection = new IniSection(AuthSectionKey);
            authSection.AddKey(EmailKey, Email ?? string.Empty);
            authSection.AddKey(AuthDataKey, JsonConvert.SerializeObject(AuthData));

            var basicSection = new IniSection(BasicSectionKey);
            basicSection.AddKey(LadderKey, Ladder ?? string.Empty);
            basicSection.AddKey(SideKey, SideId?.ToString() ?? string.Empty);

            iniFile.AddSection(authSection);
            iniFile.AddSection(basicSection);
            iniFile.WriteIniFile(SettingsFile);
        }

        private static void LoadAuthSettings(IniFile iniFile, QmUserSettings settings)
        {
            IniSection authSection = iniFile.GetSection(AuthSectionKey);
            if (authSection == null)
                return;

            settings.AuthData = GetAuthData(authSection);
            settings.Email = authSection.GetStringValue(EmailKey, null);
        }

        private static void LoadBasicSettings(IniFile iniFile, QmUserSettings settings)
        {
            IniSection basicSection = iniFile.GetSection(BasicSectionKey);
            if (basicSection == null)
                return;

            settings.Ladder = basicSection.GetStringValue(LadderKey, null);
            int sideId = basicSection.GetIntValue(SideKey, -1);
            if (sideId != -1)
                settings.SideId = sideId;
        }

        private static QmAuthData GetAuthData(IniSection section)
        {
            if (!section.KeyExists(AuthDataKey))
                return null;

            string authDataValue = section.GetStringValue(AuthDataKey, null);
            if (string.IsNullOrEmpty(authDataValue))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<QmAuthData>(authDataValue);
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString());
                return null;
            }
        }
    }
}