using ClientCore;
using ClientGUI;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        private bool EnabledFilesComplete => enabledFiles.All(f => File.Exists(f.SourcePath));
        private bool DisabledFilesComplete => disabledFiles.All(f => File.Exists(f.SourcePath));

        private bool defaultValue;
        private bool originalState;
        private bool restartRequired;
        private bool checkFilePresence;
        private bool resetUnselectableItem;

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

                FileOperationOptions options = default;
                if (parts.Length >= 3)
                    Enum.TryParse(parts[2], out options);

                enabledFiles.Add(new FileSourceDestinationInfo(parts[0], parts[1], options));

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

                FileOperationOptions options = default;
                if (parts.Length >= 3)
                    Enum.TryParse(parts[2], out options);

                disabledFiles.Add(new FileSourceDestinationInfo(parts[0], parts[1], options));

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
                case "CheckFilePresence":
                    checkFilePresence = Conversions.BooleanFromString(value, false);
                    return;
                case "ResetUnselectableItem":
                    resetUnselectableItem = Conversions.BooleanFromString(value, false);
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
        {
            bool currentValue = Checked;

            if (checkFilePresence)
            {
                Enabled = true;
                
                if (resetUnselectableItem)
                {
                    if (DisabledFilesComplete != EnabledFilesComplete)
                        Checked = EnabledFilesComplete;
                    else if (!DisabledFilesComplete && !EnabledFilesComplete)
                        Checked = defaultValue;
                }
            }

            return Checked != currentValue;
        }

        public bool Save()
        {
            bool canBeChecked = !checkFilePresence || EnabledFilesComplete;
            bool canBeUnchecked = !checkFilePresence || DisabledFilesComplete;

            if (Checked && canBeChecked)
            {
                disabledFiles.ForEach(f => f.Revert());
                enabledFiles.ForEach(f => f.Apply());
            }
            else if (!Checked && canBeUnchecked)
            {
                enabledFiles.ForEach(f => f.Revert());
                disabledFiles.ForEach(f => f.Apply());
            }
            else // undefined state, the checkbox is bork, don't continue
            {
                return false;
            }

            UserINISettings.Instance.SetCustomSettingValue(Name, Checked);

            return restartRequired && (Checked != originalState);
        }
    }
}
