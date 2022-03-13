using ClientGUI;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System.Collections.Generic;
using System.IO;

namespace DTAConfig.CustomSettings
{
    /// <summary>
    /// A legacy implementation of a check-box fitting for a file presence toggle setting.
    /// </summary>
    public class FileSettingCheckBox : XNAClientCheckBox, ICustomSetting
    {
        public FileSettingCheckBox(WindowManager windowManager) : base(windowManager) { }

        public FileSettingCheckBox(WindowManager windowManager,
            string sourceFilePath, string destinationFilePath, FileOperationOptions options,
            bool reversed) : base(windowManager)
        {
            files = new List<FileSourceDestinationInfo>
            {
                new FileSourceDestinationInfo(sourceFilePath, destinationFilePath, options)
            };

            this.reversed = reversed;
        }

        private List<FileSourceDestinationInfo> files;
        private bool reversed;
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

            files = FileSourceDestinationInfo.ParseFSDInfoList(section, "File");
        }

        public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "Reversed":
                    reversed = Conversions.BooleanFromString(value, false);
                    return;
                case "RestartRequired":
                    RestartRequired = Conversions.BooleanFromString(value, false);
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        public void Load()
        {
            Checked = reversed != File.Exists(files[0].DestinationPath);
            originalState = Checked;
        }

        public bool RefreshSetting()
            // TODO implement custom logic for refreshing the checkbox
            => false;

        public bool Save()
        {
            if (reversed != Checked)
                files.ForEach(f => f.Apply());
            else
                files.ForEach(f => f.Revert());

            return RestartRequired && (Checked != originalState);
        }
    }
}
