using ClientCore;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTAConfig.CustomSettings
{
    sealed class FileSourceDestinationInfo
    {
        public FileSourceDestinationInfo(string source, string destination, FileOperationOptions options)
        {
            SourcePath = source;
            DestinationPath = destination;
            FileOperationOptions = options;
        }

        public string SourcePath { get; }
        public string DestinationPath { get; }
        public string CachedPath => ProgramConstants.ClientUserFilesPath + "SettingsCache/" + SourcePath;
        public FileOperationOptions FileOperationOptions { get; }

        public void Apply()
        {
            switch (FileOperationOptions)
            {
                case FileOperationOptions.OverwriteOnMismatch:
                    string sourceHash = Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + SourcePath);
                    string destinationHash = Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + DestinationPath);

                    if (sourceHash != destinationHash)
                    {
                        File.Copy(ProgramConstants.GamePath + SourcePath,
                            ProgramConstants.GamePath + DestinationPath, true);
                    }

                    break;

                case FileOperationOptions.DontOverwrite:
                    if (File.Exists(ProgramConstants.GamePath + DestinationPath))
                        break;

                    File.Copy(ProgramConstants.GamePath + SourcePath,
                        ProgramConstants.GamePath + DestinationPath, false);
                    break;

                case FileOperationOptions.KeepChanges:
                    if (!File.Exists(ProgramConstants.GamePath + DestinationPath))
                    {
                        if (File.Exists(CachedPath))
                        {
                            File.Copy(CachedPath, ProgramConstants.GamePath + DestinationPath, false);
                        }
                        else
                        {
                            File.Copy(ProgramConstants.GamePath + SourcePath,
                                ProgramConstants.GamePath + DestinationPath, false);
                        }
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(CachedPath));
                    File.Copy(ProgramConstants.GamePath + DestinationPath, CachedPath, true);

                    break;

                case FileOperationOptions.MoveFile:
                    File.Move(ProgramConstants.GamePath + SourcePath,
                        ProgramConstants.GamePath + DestinationPath);
                    break;

                case FileOperationOptions.AlwaysOverwrite:
                default:
                    File.Copy(ProgramConstants.GamePath + SourcePath,
                        ProgramConstants.GamePath + DestinationPath, true);
                    break;
            }
        }

        public void Revert()
        {
            switch (FileOperationOptions)
            {
                case FileOperationOptions.MoveFile:
                    File.Move(ProgramConstants.GamePath + DestinationPath,
                        ProgramConstants.GamePath + SourcePath);
                    break;

                case FileOperationOptions.KeepChanges:
                    Directory.CreateDirectory(Path.GetDirectoryName(CachedPath));
                    File.Copy(ProgramConstants.GamePath + DestinationPath, CachedPath, true);
                    File.Delete(ProgramConstants.GamePath + DestinationPath);
                    break;

                case FileOperationOptions.OverwriteOnMismatch:
                case FileOperationOptions.DontOverwrite:
                case FileOperationOptions.AlwaysOverwrite:
                default:
                    File.Delete(ProgramConstants.GamePath + DestinationPath);
                    break;
            }
        }
    }

    public enum FileOperationOptions
    {
        AlwaysOverwrite,
        OverwriteOnMismatch,
        DontOverwrite,
        KeepChanges,
        MoveFile
    }
}
