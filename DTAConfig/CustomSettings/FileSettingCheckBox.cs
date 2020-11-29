using ClientCore;
using ClientGUI;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
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
            files.Add(new FileSourceDestinationInfo(sourceFilePath, destinationFilePath, options));
            this.reversed = reversed;
        }

        private List<FileSourceDestinationInfo> files = new List<FileSourceDestinationInfo>();
        private bool reversed;
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
                string fileInfo = section.GetStringValue($"File{i}", string.Empty);

                if (string.IsNullOrWhiteSpace(fileInfo))
                    break;

                string[] parts = fileInfo.Split(',');
                if (parts.Length < 2)
                {
                    Logger.Log($"Invalid FileSettingCheckBox information in {Name}: {fileInfo}");
                    continue;
                }

                FileOperationOptions options = default;
                if (parts.Length >= 3)
                    Enum.TryParse(parts[2], out options);

                files.Add(new FileSourceDestinationInfo(parts[0], parts[1], options));

                i++;
            }
        }

        public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "Reversed":
                    reversed = Conversions.BooleanFromString(value, false);
                    return;
                case "RestartRequired":
                    restartRequired = Conversions.BooleanFromString(value, false);
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        public void Load()
        {
            Checked = reversed != File.Exists(ProgramConstants.GamePath + files[0].DestinationPath);
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

            return restartRequired && (Checked != originalState);
        }
    }
}
