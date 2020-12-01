using ClientCore;
using ClientGUI;
using Rampastring.Tools;
using Rampastring.XNAUI;
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

        public bool RestartRequired { get; private set; }
        public bool CheckAvailability { get; private set; }
        public bool ResetUnavailableValue { get; private set; }

        public override void GetAttributes(IniFile iniFile)
        {
            base.GetAttributes(iniFile);

            var section = iniFile.GetSection(Name);

            if (section == null)
                return;

            enabledFiles = FileSourceDestinationInfo.ParseFSDInfoList(section, "EnabledFile");
            disabledFiles = FileSourceDestinationInfo.ParseFSDInfoList(section, "DisabledFile");
        }

        public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "DefaultValue":
                    defaultValue = Conversions.BooleanFromString(value, false);
                    return;
                case "CheckAvailability":
                    CheckAvailability = Conversions.BooleanFromString(value, false);
                    return;
                case "ResetUnavailableValue":
                    ResetUnavailableValue = Conversions.BooleanFromString(value, false);
                    return;
                case "RestartRequired":
                    RestartRequired = Conversions.BooleanFromString(value, false);
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

            if (CheckAvailability)
            {
                Enabled = true;
                
                if (ResetUnavailableValue)
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
            UserINISettings.Instance.SetCustomSettingValue(Name, Checked);

            bool canBeChecked = !CheckAvailability || EnabledFilesComplete;
            bool canBeUnchecked = !CheckAvailability || DisabledFilesComplete;

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
            else // selected state is unavailable, don't do anything
            {
                Logger.Log($"{nameof(CustomSettingFileCheckBox)}: " +
                    $"The selected state ({Checked}) is unavailable in {Name}");
                return false;
            }

            return RestartRequired && (Checked != originalState);
        }
    }
}
