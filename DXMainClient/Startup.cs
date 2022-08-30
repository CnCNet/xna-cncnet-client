using System;
using System.IO;
using System.Threading;
using System.Management;
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

namespace DTAClient
{
    /// <summary>
    /// A class that handles initialization of the Client.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// The main method for startup and initialization.
        /// </summary>
        public void Execute()
        {
            string themePath = ClientConfiguration.Instance.GetThemePath(UserINISettings.Instance.ClientTheme);

            if (themePath == null)
            {
                themePath = ClientConfiguration.Instance.GetThemeInfoFromIndex(0)[1];
            }

            ProgramConstants.RESOURCES_DIR = "Resources/" + themePath;

            if (!Directory.Exists(ProgramConstants.RESOURCES_DIR))
                throw new DirectoryNotFoundException("Theme directory not found!" + Environment.NewLine + ProgramConstants.RESOURCES_DIR);

            Logger.Log("Initializing updater.");

            File.Delete(ProgramConstants.GamePath + "version_u");

            Updater.Initialize(ProgramConstants.GamePath, ProgramConstants.GetBaseResourcePath(), ClientConfiguration.Instance.SettingsIniName, ClientConfiguration.Instance.LocalGame);

            Logger.Log("Operating system: " + Environment.OSVersion.VersionString);
            Logger.Log("Selected OS profile: " + MainClientConstants.OSId.ToString());
            Logger.Log("Current culture: " + CultureInfo.CurrentCulture?.ToString());

            // The query in CheckSystemSpecifications takes lots of time,
            // so we'll do it in a separate thread to make startup faster
            Thread thread = new Thread(CheckSystemSpecifications);
            thread.Start();

            Thread idThread = new Thread(GenerateOnlineId);
            idThread.Start();

#if ARES
            Task.Factory.StartNew(() => PruneFiles(ProgramConstants.GamePath + "debug", DateTime.Now.AddDays(-7)));
#endif
            Task.Factory.StartNew(MigrateOldLogFiles);

            if (Directory.Exists(ProgramConstants.GamePath + "Updater"))
            {
                Logger.Log("Attempting to delete temporary updater directory.");
                try
                {
                    Directory.Delete(ProgramConstants.GamePath + "Updater", true);
                }
                catch
                {
                }
            }

            if (ClientConfiguration.Instance.CreateSavedGamesDirectory)
            {
                if (!Directory.Exists(ProgramConstants.GamePath + "Saved Games"))
                {
                    Logger.Log("Saved Games directory does not exist - attempting to create one.");
                    try
                    {
                        Directory.CreateDirectory(ProgramConstants.GamePath + "Saved Games");
                    }
                    catch
                    {
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
                        File.Delete(ProgramConstants.GamePath + component.LocalPath + "_u");
                    }
                    catch
                    {

                    }
                }
            }

            FinalSunSettings.WriteFinalSunIni();

            WriteInstallPathToRegistry();

            ClientConfiguration.Instance.RefreshSettings();

            // Start INI file preprocessor
            PreprocessorBackgroundTask.Instance.Run();

            GameClass gameClass = new GameClass();
            gameClass.Run();
        }

#if ARES
        /// <summary>
        /// Recursively deletes all files from the specified directory that were created at <paramref name="pruneThresholdTime"/> or before.
        /// If directory is empty after deleting files, the directory itself will also be deleted.
        /// </summary>
        /// <param name="directoryPath">Directory to prune files from.</param>
        /// <param name="pruneThresholdTime">Time at or before which files must have been created for them to be pruned.</param>
        private void PruneFiles(string directoryPath, DateTime pruneThresholdTime)
        {
            if (!Directory.Exists(directoryPath))
                return;

            try
            {
                foreach (string fsEntry in Directory.EnumerateFileSystemEntries(directoryPath))
                {
                    var attr = File.GetAttributes(fsEntry);
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                        PruneFiles(fsEntry, pruneThresholdTime);
                    else
                    {
                        try
                        {
                            FileInfo fileInfo = new FileInfo(fsEntry);
                            if (fileInfo.CreationTime <= pruneThresholdTime)
                                fileInfo.Delete();
                        }
                        catch (Exception e)
                        {
                            Logger.Log("PruneFiles: Could not delete file " + fsEntry.Replace(ProgramConstants.GamePath, "") +
                                ". Error message: " + e.Message);
                            continue;
                        }
                    }
                }

                if (!Directory.EnumerateFileSystemEntries(directoryPath).Any())
                    Directory.Delete(directoryPath);
            }
            catch (Exception ex)
            {
                Logger.Log("PruneFiles: An error occured while pruning files from " +
                    directoryPath.Replace(ProgramConstants.GamePath, "") + ". Message: " + ex.Message);
            }
        }
#endif

        /// <summary>
        /// Move log files from obsolete directories to currently used ones and adjust filenames to match currently used timestamp scheme.
        /// </summary>
        private void MigrateOldLogFiles()
        {
            MigrateLogFiles(ProgramConstants.ClientUserFilesPath + "ErrorLogs", ProgramConstants.ClientUserFilesPath + "ClientCrashLogs", "ClientCrashLog*.txt");
            MigrateLogFiles(ProgramConstants.ClientUserFilesPath + "ErrorLogs", ProgramConstants.ClientUserFilesPath + "GameCrashLogs", "EXCEPT*.txt");
            MigrateLogFiles(ProgramConstants.ClientUserFilesPath + "ErrorLogs", ProgramConstants.ClientUserFilesPath + "SyncErrorLogs", "SYNC*.txt");
        }

        /// <summary>
        /// Move log files matching given search pattern from specified directory to another one and adjust filename timestamps.
        /// </summary>
        /// <param name="currentDirectory">Current log files directory.</param>
        /// <param name="newDirectory">New log files directory.</param>
        /// <param name="searchPattern">Search string the log file names must match against to be copied. Can contain wildcard characters (* and ?) but doesn't support regular expressions.</param>
        private static void MigrateLogFiles(string currentDirectory, string newDirectory, string searchPattern)
        {
            try
            {
                if (!Directory.Exists(currentDirectory))
                    return;

                if (!Directory.Exists(newDirectory))
                    Directory.CreateDirectory(newDirectory);

                foreach (string filename in Directory.EnumerateFiles(currentDirectory, searchPattern))
                {
                    string filenameTS = Path.GetFileNameWithoutExtension(filename.Replace(currentDirectory, ""));
                    string[] ts = filenameTS.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries);

                    string timestamp = string.Empty;
                    string baseFilename = Path.GetFileNameWithoutExtension(ts[0]);

                    if (ts.Length >= 6)
                    {
                        timestamp = string.Format("_{0}_{1}_{2}_{3}_{4}",
                            ts[3], ts[2].PadLeft(2, '0'), ts[1].PadLeft(2, '0'), ts[4].PadLeft(2, '0'), ts[5].PadLeft(2, '0'));
                    }

                    string newFilename = newDirectory + "/" + baseFilename + timestamp + Path.GetExtension(filename);
                    File.Move(filename, newFilename);
                }

                if (!Directory.EnumerateFiles(currentDirectory).Any())
                    Directory.Delete(currentDirectory);
            }
            catch (Exception ex)
            {
                Logger.Log("MigrateLogFiles: An error occured while moving log files from " +
                    currentDirectory.Replace(ProgramConstants.GamePath, "") + " to " +
                    newDirectory.Replace(ProgramConstants.GamePath, "") + ". Message: " + ex.Message);
            }
        }

        /// <summary>
        /// Writes processor, graphics card and memory info to the log file.
        /// </summary>
        private void CheckSystemSpecifications()
        {
            string cpu = string.Empty;
            string videoController = string.Empty;
            string memory = string.Empty;

            ManagementObjectSearcher searcher;

            try
            {
                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");

                foreach (var proc in searcher.Get())
                {
                    cpu = cpu + proc["Name"].ToString().Trim() + " (" + proc["NumberOfCores"] + " cores) ";
                }

            }
            catch
            {
                cpu = "CPU info not found";
            }

            try
            {
                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

                foreach (ManagementObject mo in searcher.Get())
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
            catch
            {
                cpu = "Video controller info not found";
            }

            try
            {
                searcher = new ManagementObjectSearcher("Select * From Win32_PhysicalMemory");
                ulong total = 0;

                foreach (ManagementObject ram in searcher.Get())
                {
                    total += Convert.ToUInt64(ram.GetPropertyValue("Capacity"));
                }

                if (total != 0)
                    memory = "Total physical memory: " + (total >= 1073741824 ? total / 1073741824 + "GB" : total / 1048576 + "MB");
            }
            catch
            {
                cpu = "Memory info not found";
            }

            Logger.Log(string.Format("Hardware info: {0} | {1} | {2}", cpu.Trim(), videoController.Trim(), memory));
        }


        /// <summary>
        /// Generate an ID for online play.
        /// </summary>
        private static void GenerateOnlineId()
        {
            try
            {
                ManagementObjectCollection mbsList = null;
                ManagementObjectSearcher mbs = new ManagementObjectSearcher("Select * From Win32_processor");
                mbsList = mbs.Get();
                string cpuid = "";
                foreach (ManagementObject mo in mbsList)
                {
                    cpuid = mo["ProcessorID"].ToString();
                }

                ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
                var moc = mos.Get();
                string mbid = "";
                foreach (ManagementObject mo in moc)
                {
                    mbid = (string)mo["SerialNumber"];
                }

                string sid = new SecurityIdentifier((byte[])new DirectoryEntry(string.Format("WinNT://{0},Computer", Environment.MachineName)).Children.Cast<DirectoryEntry>().First().InvokeGet("objectSID"), 0).AccountDomainSid.Value;

                Connection.SetId(cpuid + mbid + sid);
                Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + ClientConfiguration.Instance.InstallationPathRegKey).SetValue("Ident", cpuid + mbid + sid);
            }
            catch (Exception)
            {
                Random rn = new Random();

                RegistryKey key;
                key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + ClientConfiguration.Instance.InstallationPathRegKey);
                string str = rn.Next(Int32.MaxValue - 1).ToString();

                try
                {
                    Object o = key.GetValue("Ident");
                    if (o == null)
                    {
                        key.SetValue("Ident", str);
                    }
                    else
                        str = o.ToString();
                }
                catch { }

                key.Close();
                Connection.SetId(str);
            }
        }

        /// <summary>
        /// Writes the game installation path to the Windows registry.
        /// </summary>
        private void WriteInstallPathToRegistry()
        {
            if (!UserINISettings.Instance.WritePathToRegistry)
            {
                Logger.Log("Skipping writing installation path to the Windows Registry because of INI setting.");
                return;
            }

            Logger.Log("Writing installation path to the Windows registry.");

            try
            {
                RegistryKey key;
                key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + ClientConfiguration.Instance.InstallationPathRegKey);
                key.SetValue("InstallPath", ProgramConstants.GamePath);
                key.Close();
            }
            catch
            {
                Logger.Log("Failed to write installation path to the Windows registry");
            }
        }
    }
}
