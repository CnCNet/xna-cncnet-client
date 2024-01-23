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

        public string SourcePath => ProgramConstants.GamePath + sourcePath;

        public string DestinationPath => ProgramConstants.GamePath + destinationPath;

        /// <summary>
        /// A path where the files edited by user are saved if
        /// <see cref="FileOperationOptions"/> is set to <see cref="FileOperationOptions.KeepChanges"/>.
        /// </summary>
        public string CachedPath => ProgramConstants.ClientUserFilesPath + "SettingsCache/" + sourcePath;

        public FileOperationOptions FileOperationOptions { get; }

        public FileSourceDestinationInfo(string source, string destination, FileOperationOptions options)
        {
            sourcePath = source;
            destinationPath = destination;
            FileOperationOptions = options;
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

            FileOperationOptions options = default(FileOperationOptions);
            if (parts.Length >= 3)
                Enum.TryParse(parts[2], out options);

            sourcePath = parts[0];
            destinationPath = parts[1];
            FileOperationOptions = options;
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
        /// <see cref="DestinationPath"/> according to <see cref="FileOperationOptions"/>.
        /// </summary>
        public void Apply()
        {
            switch (FileOperationOptions)
            {
                case FileOperationOptions.OverwriteOnMismatch:
                    string sourceHash = Utilities.CalculateSHA1ForFile(SourcePath);
                    string destinationHash = Utilities.CalculateSHA1ForFile(DestinationPath);

                    if (sourceHash != destinationHash)
                        File.Copy(SourcePath, DestinationPath, true);

                    break;

                case FileOperationOptions.DontOverwrite:
                    if (!File.Exists(DestinationPath))
                        File.Copy(SourcePath, DestinationPath, false);

                    break;

                case FileOperationOptions.KeepChanges:
                    if (!File.Exists(DestinationPath))
                    {
                        if (File.Exists(CachedPath))
                            File.Copy(CachedPath, DestinationPath, false);
                        else
                            File.Copy(SourcePath, DestinationPath, false);
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(CachedPath));
                    File.Copy(DestinationPath, CachedPath, true);

                    break;

                case FileOperationOptions.AlwaysOverwrite:
                    File.Copy(SourcePath, DestinationPath, true);
                    break;

                default:
                    throw new InvalidOperationException($"{nameof(FileSourceDestinationInfo)}: " +
                        $"Invalid {nameof(FileOperationOptions)} value of {FileOperationOptions}");
            }
        }

        /// <summary>
        /// Performs file operations to undo changes made by <see cref="Apply"/>
        /// to <see cref="DestinationPath"/> according to <see cref="FileOperationOptions"/>.
        /// </summary>
        public void Revert()
        {
            switch (FileOperationOptions)
            {
                case FileOperationOptions.KeepChanges:
                    if (File.Exists(DestinationPath))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(CachedPath));
                        File.Copy(DestinationPath, CachedPath, true);
                        File.Delete(DestinationPath);
                    }
                    break;

                case FileOperationOptions.OverwriteOnMismatch:
                case FileOperationOptions.DontOverwrite:
                case FileOperationOptions.AlwaysOverwrite:
                    File.Delete(DestinationPath);
                    break;

                default:
                    throw new InvalidOperationException($"{nameof(FileSourceDestinationInfo)}: " +
                        $"Invalid {nameof(FileOperationOptions)} value of {FileOperationOptions}");
            }
        }
    }

    /// <summary>
    /// Defines the expected behavior of file operations performed with
    /// <see cref="FileSourceDestinationInfo"/>.
    /// </summary>
    public enum FileOperationOptions
    {
        AlwaysOverwrite,
        OverwriteOnMismatch,
        DontOverwrite,
        KeepChanges
    }
}
