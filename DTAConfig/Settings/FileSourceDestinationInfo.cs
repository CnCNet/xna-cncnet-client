using ClientCore;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;

namespace DTAConfig.Settings
{
    sealed class FileSourceDestinationInfo
    {
        private readonly string destinationPath;
        private readonly string sourcePath;

        public string SourcePath => SafePath.CombineFilePath(ProgramConstants.GamePath, sourcePath);

        public string DestinationPath => SafePath.CombineFilePath(ProgramConstants.GamePath, destinationPath);
        /// <summary>
        /// A path where the files edited by user are saved if
        /// <see cref="FileOperationOption"/> is set to <see cref="FileOperationOption.KeepChanges"/>.
        /// </summary>
        public string CachedPath => SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "SettingsCache", sourcePath);

        public FileOperationOption FileOperationOption { get; }

        public FileSourceDestinationInfo(string source, string destination, FileOperationOption option)
        {
            sourcePath = source;
            destinationPath = destination;
            FileOperationOption = option;
        }

        /// <summary>
        /// Constructs a new instance of <see cref="FileSourceDestinationInfo"/> from a given string.
        /// </summary>
        /// <param name="value">A string to be parsed.</param>
        public FileSourceDestinationInfo(string value)
        {
            string[] parts = value.Split(',');
            if (parts.Length < 2)
                throw new ArgumentException($"{nameof(FileSourceDestinationInfo)}: " +
                    $"Too few parameters specified in parsed value", nameof(value));

            FileOperationOption option = default(FileOperationOption);
            if (parts.Length >= 3)
                Enum.TryParse(parts[2], out option);

            sourcePath = parts[0];
            destinationPath = parts[1];
            FileOperationOption = option;
        }

        /// <summary>
        /// A method which parses certain key list values from an INI section
        /// into a list of <see cref="FileSourceDestinationInfo"/> objects.
        /// </summary>
        /// <param name="section">An INI section to parse key values from.</param>
        /// <param name="iniKeyPrefix">A string to append index to when
        /// parsing the values from key list.</param>
        /// <returns>A <see cref="List{FileSourceDestinationInfo}"/> of all correctly defined <see cref="FileSourceDestinationInfo"/>s.</returns>
        public static List<FileSourceDestinationInfo> ParseFSDInfoList(IniSection section, string iniKeyPrefix)
        {
            if (section == null)
                throw new ArgumentNullException(nameof(section));

            List<FileSourceDestinationInfo> result = new List<FileSourceDestinationInfo>();
            string fileInfo;

            for (int i = 0;
                !string.IsNullOrWhiteSpace(
                    fileInfo = section.GetStringValue($"{iniKeyPrefix}{i}", string.Empty));
                i++)
            {
                result.Add(new FileSourceDestinationInfo(fileInfo));
            }

            return result;
        }

        /// <summary>
        /// Performs file operations from <see cref="SourcePath"/> to
        /// <see cref="DestinationPath"/> according to <see cref="FileOperationOption"/>.
        /// </summary>
        public void Apply()
        {
            switch (FileOperationOption)
            {
                case FileOperationOption.OverwriteOnMismatch:
                    string sourceHash = Utilities.CalculateSHA1ForFile(SourcePath);
                    string destinationHash = Utilities.CalculateSHA1ForFile(DestinationPath);

                    if (sourceHash != destinationHash)
                        File.Copy(SourcePath, DestinationPath, true);

                    break;

                case FileOperationOption.DontOverwrite:
                    if (!File.Exists(DestinationPath))
                        File.Copy(SourcePath, DestinationPath, false);

                    break;

                case FileOperationOption.KeepChanges:
                    if (!File.Exists(DestinationPath))
                    {
                        if (File.Exists(CachedPath))
                            File.Move(CachedPath, DestinationPath);
                        else
                            File.Copy(SourcePath, DestinationPath, true);
                    }

                    break;

                case FileOperationOption.AlwaysOverwrite:
                    File.Copy(SourcePath, DestinationPath, true);
                    break;

                case FileOperationOption.AlwaysOverwrite_LinkAsReadOnly:
                    FileHelper.CreateHardLinkFromSource(sourcePath, destinationPath, fallback: true);
                    new FileInfo(DestinationPath).IsReadOnly = true;
                    new FileInfo(SourcePath).IsReadOnly = true;
                    break;

                default:
                    throw new InvalidOperationException($"{nameof(FileSourceDestinationInfo)}: " +
                        $"Invalid {nameof(FileOperationOption)} value of {FileOperationOption}");
            }
        }

        /// <summary>
        /// Performs file operations to undo changes made by <see cref="Apply"/>
        /// to <see cref="DestinationPath"/> according to <see cref="FileOperationOption"/>.
        /// </summary>
        public void Revert()
        {
            switch (FileOperationOption)
            {
                case FileOperationOption.KeepChanges:
                    if (File.Exists(DestinationPath))
                    {
                        if (!File.Exists(Path.GetDirectoryName(CachedPath)))
                            SafePath.GetDirectory(Path.GetDirectoryName(CachedPath)).Create();

                        File.Move(DestinationPath, CachedPath);
                    }
                    break;

                case FileOperationOption.AlwaysOverwrite_LinkAsReadOnly:
                case FileOperationOption.OverwriteOnMismatch:
                case FileOperationOption.DontOverwrite:
                case FileOperationOption.AlwaysOverwrite:
                    if (File.Exists(DestinationPath))
                    {
                        FileInfo destinationFile = new(DestinationPath);
                        destinationFile.IsReadOnly = false;
                        destinationFile.Delete();
                    }

                    if (FileOperationOption == FileOperationOption.AlwaysOverwrite_LinkAsReadOnly)
                        new FileInfo(SourcePath).IsReadOnly = false;

                    break;

                default:
                    throw new InvalidOperationException($"{nameof(FileSourceDestinationInfo)}: " +
                        $"Invalid {nameof(FileOperationOption)} value of {FileOperationOption}");
            }
        }
    }

    /// <summary>
    /// Defines the expected behavior of file operations performed with
    /// <see cref="FileSourceDestinationInfo"/>.
    /// </summary>
    public enum FileOperationOption
    {
        AlwaysOverwrite = 0,
        OverwriteOnMismatch,
        DontOverwrite,
        KeepChanges,
        AlwaysOverwrite_LinkAsReadOnly,
    }
}
