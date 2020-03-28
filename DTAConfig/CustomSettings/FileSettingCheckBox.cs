using ClientCore;
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
            string sourceFilePath, string destinationFilePath,
            bool reversed) : base(windowManager)
        {
            files.Add(new FileSourceDestinationInfo(sourceFilePath, destinationFilePath));
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
                if (parts.Length != 2)
                {
                    Logger.Log($"Invalid FileSettingCheckBox information in {Name}: {fileInfo}");
                    continue;
                }

                files.Add(new FileSourceDestinationInfo(parts[0], parts[1]));

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
            {
                foreach (var info in files)
                {
                    File.Copy(ProgramConstants.GamePath + info.SourcePath,
                            ProgramConstants.GamePath + info.DestinationPath, true);
                }
            }
            else
                files.ForEach(f => File.Delete(ProgramConstants.GamePath + f.DestinationPath));

            if (restartRequired && (Checked != originalState))
                return true;

            return false;
        }
    }
}
