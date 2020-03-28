using ClientCore;
using ClientGUI;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System.Collections.Generic;
using System.IO;

namespace DTAConfig.CustomSettings
{
    /// <summary>
    /// An implementation of a check-box that switches between two sets of files.
    /// </summary>
    public class CustomSettingFileCheckBox : XNAClientCheckBox, ICustomSetting
    {
        public CustomSettingFileCheckBox(WindowManager windowManager) : base(windowManager) { }

        private List<FileSourceDestinationInfo> enabledFiles = new List<FileSourceDestinationInfo>();
        private List<FileSourceDestinationInfo> disabledFiles = new List<FileSourceDestinationInfo>();

        private bool defaultValue;
        private bool originalState;
        private bool restartRequired;

        public override void GetAttributes(IniFile iniFile)
        {
            base.GetAttributes(iniFile);

            var section = iniFile.GetSection(Name);

            if (section == null)
                return;

            int i = 0;
            while (true)
            {
                string fileInfo = section.GetStringValue($"EnabledFile{i}", string.Empty);

                if (string.IsNullOrWhiteSpace(fileInfo))
                    break;

                string[] parts = fileInfo.Split(',');
                if (parts.Length != 2)
                {
                    Logger.Log($"Invalid CustomSettingFileCheckBox information in {Name}: {fileInfo}");
                    continue;
                }

                enabledFiles.Add(new FileSourceDestinationInfo(parts[0], parts[1]));

                i++;
            }

            i = 0;
            while (true)
            {
                string fileInfo = section.GetStringValue($"DisabledFile{i}", string.Empty);

                if (string.IsNullOrWhiteSpace(fileInfo))
                    break;

                string[] parts = fileInfo.Split(',');
                if (parts.Length != 2)
                {
                    Logger.Log($"Invalid CustomSettingFileCheckBox information in {Name}: {fileInfo}");
                    continue;
                }

                disabledFiles.Add(new FileSourceDestinationInfo(parts[0], parts[1]));

                i++;
            }
        }

        public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "DefaultValue":
                    defaultValue = Conversions.BooleanFromString(value, false);
                    return;
                case "RestartRequired":
                    restartRequired = Conversions.BooleanFromString(value, false);
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        public void Load()
        {
            Checked = UserINISettings.Instance.GetCustomSettingValue(Name, defaultValue);
            originalState = Checked;
        }

        public bool RefreshSetting()
            // TODO implement custom logic for refreshing the checkbox
            => false;

        public bool Save()
        {
            if (Checked)
            {
                disabledFiles.ForEach(f => File.Delete(ProgramConstants.GamePath + f.DestinationPath));
                enabledFiles.ForEach(f => File.Copy(ProgramConstants.GamePath + f.SourcePath,
                    ProgramConstants.GamePath + f.DestinationPath, true));
            }
            else
            {
                enabledFiles.ForEach(f => File.Delete(ProgramConstants.GamePath + f.DestinationPath));
                disabledFiles.ForEach(f => File.Copy(ProgramConstants.GamePath + f.SourcePath,
                    ProgramConstants.GamePath + f.DestinationPath, true));
            }

            UserINISettings.Instance.SetCustomSettingValue(Name, Checked);

            return restartRequired && (Checked != originalState);
        }
    }
}
