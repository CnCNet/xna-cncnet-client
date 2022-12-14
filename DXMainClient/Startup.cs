using System;
using System.IO;
using Microsoft.Win32;
using DTAClient.Domain;
using ClientCore;
using Rampastring.Tools;
using DTAClient.DXGUI;
using ClientUpdater;
using System.Security.Principal;
using System.DirectoryServices;
using System.Linq;
using DTAClient.Online;
using ClientCore.INIProcessing;
using System.Threading.Tasks;
using System.Globalization;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ClientCore.Extensions;
using ClientCore.Settings;
using Microsoft.Xna.Framework.Graphics;

namespace DTAClient
{
    /// <summary>
    /// A class that handles initialization of the Client.
    /// </summary>
    internal sealed class Startup
    {
        /// <summary>
        /// The main method for startup and initialization.
        /// </summary>
        public void Execute()
        {
            ProgramConstants.RESOURCES_DIR = SafePath.CombineDirectoryPath(ProgramConstants.BASE_RESOURCE_PATH, UserINISettings.Instance.ThemeFolderPath);

            DirectoryInfo resourcesDirectory = SafePath.GetDirectory(ProgramConstants.GetResourcePath());

            if (!resourcesDirectory.Exists)
                throw new DirectoryNotFoundException("Theme directory not found!" + Environment.NewLine + ProgramConstants.RESOURCES_DIR);

            Logger.Log("Initializing updater.");
            SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "version_u");
            Updater.Initialize(ProgramConstants.GamePath, ProgramConstants.GetBaseResourcePath(), ClientConfiguration.Instance.SettingsIniName, ClientConfiguration.Instance.LocalGame, SafePath.GetFile(ProgramConstants.StartupExecutable).Name);
            Logger.Log("OSDescription: " + RuntimeInformation.OSDescription);
            Logger.Log("OSArchitecture: " + RuntimeInformation.OSArchitecture);
            Logger.Log("ProcessArchitecture: " + RuntimeInformation.ProcessArchitecture);
            Logger.Log("FrameworkDescription: " + RuntimeInformation.FrameworkDescription);
            Logger.Log("RuntimeIdentifier: " + RuntimeInformation.RuntimeIdentifier);
            Logger.Log("Selected OS profile: " + ProgramConstants.OSId);
            Logger.Log("Current culture: " + CultureInfo.CurrentCulture);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // The query in CheckSystemSpecifications takes lots of time,
                // so we'll do it in a separate thread to make startup faster
                Task.Run(CheckSystemSpecifications).HandleTask();
            }

            GenerateOnlineIdAsync().HandleTask();

#if ARES
            Task.Run(() => PruneFiles(SafePath.GetDirectory(ProgramConstants.GamePath, "debug"), DateTime.Now.AddDays(-7))).HandleTask();
#endif
            Task.Run(MigrateOldLogFiles).HandleTask();

            DirectoryInfo updaterFolder = SafePath.GetDirectory(ProgramConstants.GamePath, "Updater");

            if (updaterFolder.Exists)
            {
                Logger.Log("Attempting to delete temporary updater directory.");
                try
                {
                    updaterFolder.Delete(true);
                }
                catch (Exception ex)
                {
                    ProgramConstants.LogException(ex);
                }
            }

            if (ClientConfiguration.Instance.CreateSavedGamesDirectory)
            {
                DirectoryInfo savedGamesFolder = SafePath.GetDirectory(ProgramConstants.GamePath, ProgramConstants.SAVED_GAMES_DIRECTORY);

                if (!savedGamesFolder.Exists)
                {
                    Logger.Log("Saved Games directory does not exist - attempting to create one.");
                    try
                    {
                        savedGamesFolder.Create();
                    }
                    catch (Exception ex)
                    {
                        ProgramConstants.LogException(ex);
                    }
                }
            }

            if (Updater.CustomComponents != null)
            {
                Logger.Log("Removing partial custom component downloads.");
                foreach (var component in Updater.CustomComponents)
                {
                    try
                    {
                        SafePath.DeleteFileIfExists(ProgramConstants.GamePath, FormattableString.Invariant($"{component.LocalPath}_u"));
                    }
                    catch (Exception ex)
                    {
                        ProgramConstants.LogException(ex);
                    }
                }
            }

            FinalSunSettings.WriteFinalSunIni();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                WriteInstallPathToRegistry();

            ClientConfiguration.Instance.RefreshSettings();

            // Start INI file preprocessor
            PreprocessorBackgroundTask.Instance.Run();

            GameClass gameClass = new GameClass();

            int currentWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            int currentHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

            UserINISettings.Instance.ClientResolutionX = new IntSetting(UserINISettings.Instance.SettingsIni, UserINISettings.VIDEO, "ClientResolutionX", currentWidth);
            UserINISettings.Instance.ClientResolutionY = new IntSetting(UserINISettings.Instance.SettingsIni, UserINISettings.VIDEO, "ClientResolutionY", currentHeight);

            gameClass.Run();
        }

#if ARES
        /// <summary>
        /// Recursively deletes all files from the specified directory that were created at <paramref name="pruneThresholdTime"/> or before.
        /// If directory is empty after deleting files, the directory itself will also be deleted.
        /// </summary>
        /// <param name="directory">Directory to prune files from.</param>
        /// <param name="pruneThresholdTime">Time at or before which files must have been created for them to be pruned.</param>
        private void PruneFiles(DirectoryInfo directory, DateTime pruneThresholdTime)
        {
            if (!directory.Exists)
                return;

            try
            {
                foreach (FileSystemInfo fsEntry in directory.EnumerateFileSystemInfos())
                {
                    if ((fsEntry.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                        PruneFiles(new DirectoryInfo(fsEntry.FullName), pruneThresholdTime);
                    else
                    {
                        try
                        {
                            FileInfo fileInfo = new FileInfo(fsEntry.FullName);
                            if (fileInfo.CreationTime <= pruneThresholdTime)
                                fileInfo.Delete();
                        }
                        catch (Exception ex)
                        {
                            ProgramConstants.LogException(ex, "PruneFiles: Could not delete file " + fsEntry.Name + ".");
                        }
                    }
                }

                if (!directory.EnumerateFileSystemInfos().Any())
                    directory.Delete();
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, "PruneFiles: An error occurred while pruning files from " + directory.Name + ".");
            }
        }
#endif

        /// <summary>
        /// Move log files from obsolete directories to currently used ones and adjust filenames to match currently used timestamp scheme.
        /// </summary>
        private void MigrateOldLogFiles()
        {
            MigrateLogFiles(SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath, "ClientCrashLogs"), "ClientCrashLog*.txt");
            MigrateLogFiles(SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath, "GameCrashLogs"), "EXCEPT*.txt");
            MigrateLogFiles(SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath, "SyncErrorLogs"), "SYNC*.txt");
        }

        /// <summary>
        /// Move log files matching given search pattern from specified directory to another one and adjust filename timestamps.
        /// </summary>
        /// <param name="newDirectory">New log files directory.</param>
        /// <param name="searchPattern">Search string the log file names must match against to be copied. Can contain wildcard characters (* and ?) but doesn't support regular expressions.</param>
        private static void MigrateLogFiles(DirectoryInfo newDirectory, string searchPattern)
        {
            DirectoryInfo currentDirectory = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath, "ErrorLogs");
            try
            {
                if (!currentDirectory.Exists)
                    return;

                if (!newDirectory.Exists)
                    newDirectory.Create();

                foreach (FileInfo file in currentDirectory.EnumerateFiles(searchPattern))
                {
                    string filenameTS = Path.GetFileNameWithoutExtension(file.Name);
                    string[] ts = filenameTS.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);

                    string timestamp = string.Empty;
                    string baseFilename = Path.GetFileNameWithoutExtension(ts[0]);

                    if (ts.Length >= 6)
                    {
                        timestamp = string.Format("_{0}_{1}_{2}_{3}_{4}",
                            ts[3], ts[2].PadLeft(2, '0'), ts[1].PadLeft(2, '0'), ts[4].PadLeft(2, '0'), ts[5].PadLeft(2, '0'));
                    }

                    string newFilename = SafePath.CombineFilePath(newDirectory.FullName, baseFilename, timestamp, file.Extension);
                    file.MoveTo(newFilename);
                }

                if (!currentDirectory.EnumerateFiles().Any())
                    currentDirectory.Delete();
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, "MigrateLogFiles: An error occurred while moving log files from " +
                    currentDirectory.Name + " to " +
                    newDirectory.Name + ".");
            }
        }

        /// <summary>
        /// Writes processor, graphics card and memory info to the log file.
        /// </summary>
        [SupportedOSPlatform("windows")]
        private static void CheckSystemSpecifications()
        {
            string cpu = string.Empty;
            string videoController = string.Empty;
            string memory = string.Empty;

            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");

                foreach (var proc in searcher.Get())
                {
                    cpu = cpu + proc["Name"].ToString().Trim() + " (" + proc["NumberOfCores"] + " cores) ";
                }
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex);

                cpu = "CPU info not found";
            }

            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

                foreach (ManagementObject mo in searcher.Get().Cast<ManagementObject>())
                {
                    var currentBitsPerPixel = mo.Properties["CurrentBitsPerPixel"];
                    var description = mo.Properties["Description"];
                    if (currentBitsPerPixel != null && description != null)
                    {
                        if (currentBitsPerPixel.Value != null)
                            videoController = videoController + "Video controller: " + description.Value.ToString().Trim() + " ";
                    }
                }
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex);

                cpu = "Video controller info not found";
            }

            try
            {
                using var searcher = new ManagementObjectSearcher("Select * From Win32_PhysicalMemory");
                ulong total = 0;

                foreach (ManagementObject ram in searcher.Get().Cast<ManagementObject>())
                {
                    total += Convert.ToUInt64(ram.GetPropertyValue("Capacity"));
                }

                if (total != 0)
                    memory = "Total physical memory: " + (total >= 1073741824 ? total / 1073741824 + "GB" : total / 1048576 + "MB");
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex);

                cpu = "Memory info not found";
            }

            Logger.Log(string.Format("Hardware info: {0} | {1} | {2}", cpu.Trim(), videoController.Trim(), memory));
        }

        /// <summary>
        /// Generate an ID for online play.
        /// </summary>
        private static async ValueTask GenerateOnlineIdAsync()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    ManagementObjectCollection mbsList = null;
                    using var mbs = new ManagementObjectSearcher("Select * From Win32_processor");
                    mbsList = mbs.Get();
                    string cpuid = "";

                    foreach (ManagementObject mo in mbsList.Cast<ManagementObject>())
                        cpuid = mo["ProcessorID"].ToString();

                    using var mos = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
                    var moc = mos.Get();
                    string mbid = "";

                    foreach (ManagementObject mo in moc.Cast<ManagementObject>())
                        mbid = (string)mo["SerialNumber"];

                    using var computer = new DirectoryEntry(FormattableString.Invariant($"WinNT://{Environment.MachineName},Computer"));
                    string sid = new SecurityIdentifier((byte[])computer.Children.Cast<DirectoryEntry>().First().InvokeGet("objectSID"), 0).AccountDomainSid.Value;

                    Connection.SetId(cpuid + mbid + sid);
                    using RegistryKey key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + ClientConfiguration.Instance.InstallationPathRegKey);
                    key.SetValue("Ident", cpuid + mbid + sid);
                }
                catch (Exception ex)
                {
                    ProgramConstants.LogException(ex);
                    var rn = new Random();
                    using RegistryKey key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + ClientConfiguration.Instance.InstallationPathRegKey);
                    string str = rn.Next(int.MaxValue - 1).ToString(CultureInfo.InvariantCulture);

                    try
                    {
                        object o = key.GetValue("Ident");

                        if (o == null)
                            key.SetValue("Ident", str);
                        else
                            str = o.ToString();
                    }
                    catch (Exception ex1)
                    {
                        ProgramConstants.LogException(ex1);
                    }

                    Connection.SetId(str);
                }
            }
            else
            {
                try
                {
                    string machineId = await File.ReadAllTextAsync("/var/lib/dbus/machine-id").ConfigureAwait(false);

                    Connection.SetId(machineId);
                }
                catch (Exception ex)
                {
                    ProgramConstants.LogException(ex);
                    Connection.SetId(new Random().Next(int.MaxValue - 1).ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        /// <summary>
        /// Writes the game installation path to the Windows registry.
        /// </summary>
        [SupportedOSPlatform("windows")]
        private static void WriteInstallPathToRegistry()
        {
            if (!UserINISettings.Instance.WritePathToRegistry)
            {
                Logger.Log("Skipping writing installation path to the Windows Registry because of INI setting.");
                return;
            }

            Logger.Log("Writing installation path to the Windows registry.");

            try
            {
                using RegistryKey key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + ClientConfiguration.Instance.InstallationPathRegKey);
                key.SetValue("InstallPath", ProgramConstants.GamePath);
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, "Failed to write installation path to the Windows registry");
            }
        }
    }
}