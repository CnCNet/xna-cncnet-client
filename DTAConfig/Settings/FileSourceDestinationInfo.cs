using ClientCore;
using ClientCore.Extensions;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

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
        /// <see cref="FileOperationOptions"/> is set to <see cref="FileOperationOptions.KeepChanges"/>.
        /// </summary>
        public string CachedPath => SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "SettingsCache", sourcePath);

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
                        SafePath.GetDirectory(Path.GetDirectoryName(CachedPath)).Create();

                        if (!File.Exists(CachedPath))
                            File.Copy(SourcePath, CachedPath, true);
                    }

                    CreateHardLinkFromSource(CachedPath, DestinationPath, fallback: true);

                    break;

                case FileOperationOptions.AlwaysOverwrite:
                    File.Copy(SourcePath, DestinationPath, true);
                    break;

                case FileOperationOptions.LinkAsReadOnly:
                    CreateHardLinkFromSource(sourcePath, destinationPath, fallback: true);
                    new FileInfo(DestinationPath).IsReadOnly = true;
                    new FileInfo(SourcePath).IsReadOnly = true;
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
                        string cacheHash = Utilities.CalculateSHA1ForFile(CachedPath);
                        string destinationHash = Utilities.CalculateSHA1ForFile(DestinationPath);
                        
                        if (cacheHash != destinationHash)
                            File.Copy(DestinationPath, CachedPath, true);

                        File.Delete(DestinationPath);
                    }
                    break;

                case FileOperationOptions.LinkAsReadOnly:
                case FileOperationOptions.OverwriteOnMismatch:
                case FileOperationOptions.DontOverwrite:
                case FileOperationOptions.AlwaysOverwrite:
                    if (File.Exists(DestinationPath))
                    {
                        FileInfo destinationFile = new(DestinationPath);
                        destinationFile.IsReadOnly = false;
                        destinationFile.Delete();
                    }

                    if (FileOperationOptions == FileOperationOptions.LinkAsReadOnly)
                    {
                        new FileInfo(SourcePath).IsReadOnly = false;
                    }

                    break;

                default:
                    throw new InvalidOperationException($"{nameof(FileSourceDestinationInfo)}: " +
                        $"Invalid {nameof(FileOperationOptions)} value of {FileOperationOptions}");
            }
        }

        /// <summary>
        /// Establishes a hard link between an existing file and a new file. This function is only supported on the NTFS file system, and only for files, not directories.
        /// <br/>
        /// https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-createhardlinkw
        /// </summary>
        /// <param name="lpFileName">The name of the new file.</param>
        /// <param name="lpExistingFileName">The name of the existing file.</param>
        /// <param name="lpSecurityAttributes">Reserved; must be NULL.</param>
        /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero (0).</returns>
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CreateHardLinkW")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [SupportedOSPlatform("windows")]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        /// <summary>
        /// The link function makes a new link to the existing file named by oldname, under the new name newname.
        /// <br/>
        /// https://www.gnu.org/software/libc/manual/html_node/Hard-Links.html
        /// <param name="oldname"></param>
        /// <param name="newname"></param>
        /// <returns>This function returns a value of 0 if it is successful and -1 on failure.</returns>
        [DllImport("libc", EntryPoint = "link", SetLastError = true)]
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("osx")]
        private static extern int link([MarshalAs(UnmanagedType.LPUTF8Str)] string oldname, [MarshalAs(UnmanagedType.LPUTF8Str)] string newname);

        private static void CreateHardLinkFromSource(string source, string destination, bool fallback = false)
        {
            if (fallback)
            {
                try
                {
                    CreateHardLinkFromSource(source, destination, fallback: false);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to create hard link at {destination}. Fallback to copy. {ex.Message}");
                    File.Copy(source, destination, true);
                }

                return;
            }

            if (File.Exists(destination))
            {
                FileInfo destinationFile = new(destination);
                destinationFile.IsReadOnly = false;
                destinationFile.Delete();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!CreateHardLink(destination, source, IntPtr.Zero))
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (link(source, destination) != 0)
                    throw new IOException(string.Format("Unable to create hard link at {0} with the following error code: {1}".L10N("Client:DTAConfig:CreateHardLinkFailed"), destination, Marshal.GetLastWin32Error()));
            }
            else
            {
                throw new PlatformNotSupportedException();
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
        KeepChanges,
        LinkAsReadOnly
    }
}
