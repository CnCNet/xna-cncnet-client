using ClientCore;
using ClientGUI;
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
        public FileSettingCheckBox(WindowManager windowManager,
            string sourceFilePath, string destinationFilePath,
            bool reversed) : base(windowManager)
        {
            files.Add(new FileSourceDestinationInfo(sourceFilePath, destinationFilePath));
            this.reversed = reversed;
        }

        List<FileSourceDestinationInfo> files = new List<FileSourceDestinationInfo>();
        bool reversed;

        public void AddFile(string sourceFile, string destinationFile)
        {
            files.Add(new FileSourceDestinationInfo(sourceFile, destinationFile));
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
