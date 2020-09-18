using ClientCore;
using ClientGUI;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System.Collections.Generic;
using System.IO;

namespace DTAConfig
{
    /// <summary>
    /// A check-box fitting for a file presence toggle setting.
    /// </summary>
    public class FileSettingCheckBox : XNAClientCheckBox
    {
        public FileSettingCheckBox(WindowManager windowManager) : base (windowManager) { }

        public FileSettingCheckBox(WindowManager windowManager,
            string sourceFilePath, string destinationFilePath,
            bool reversed) : base(windowManager)
        {
            files.Add(new FileSourceDestinationInfo(sourceFilePath, destinationFilePath));
        }

        List<FileSourceDestinationInfo> files = new List<FileSourceDestinationInfo>();
        private bool reversed;


        public override void GetAttributes(IniFile iniFile)
        {
            base.GetAttributes(iniFile);

            var section = iniFile.GetSection(Name);
            if (section == null)
                return;

            int i = 0;
            while (true)
            {
                string fileInfo = section.GetStringValue("File" + i.ToString(), string.Empty);
                if (fileInfo == string.Empty)
                    break;
                string[] parts = fileInfo.Split(',');
                if (parts.Length != 2)
                {
                    Logger.Log("Invalid MIXSettingCheckBox information in " + Name + ": " + fileInfo);
                    continue;
                }

                files.Add(new FileSourceDestinationInfo(parts[0], parts[1]));

                i++;
            }
        }

        public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            if (key == "Reversed")
            {
                reversed = Conversions.BooleanFromString(value, false);
                return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        public void Load()
        {
            Checked = reversed != File.Exists(ProgramConstants.GamePath + files[0].DestinationPath);
        }

        public void Save()
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
            {
                files.ForEach(f => File.Delete(ProgramConstants.GamePath + f.DestinationPath));
            }
        }

        sealed class FileSourceDestinationInfo
        {
            public FileSourceDestinationInfo(string source, string destination)
            {
                SourcePath = source;
                DestinationPath = destination;
            }

            public string SourcePath { get; private set; }
            public string DestinationPath { get; private set; }
        }
    }
}
