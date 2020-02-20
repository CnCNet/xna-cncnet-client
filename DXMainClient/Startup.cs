using System;
using System.IO;
using System.Threading;
using System.Management;
using Microsoft.Win32;
using DTAClient.Domain;
using ClientCore;
using Updater;
using Rampastring.Tools;
using DTAClient.DXGUI;
using System.Security.Principal;
using System.DirectoryServices;
using System.Linq;
using DTAClient.Online;

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

            ProgramConstants.RESOURCES_DIR = "Resources\\" + themePath;

            if (!Directory.Exists(ProgramConstants.RESOURCES_DIR))
                throw new DirectoryNotFoundException("Theme directory not found!" + Environment.NewLine + ProgramConstants.RESOURCES_DIR);

            Logger.Log("Initializing updater.");

            File.Delete(ProgramConstants.GamePath + "version_u");

            CUpdater.Initialize(ClientConfiguration.Instance.LocalGame);

            Logger.Log("Operating system: " + Environment.OSVersion.VersionString);
            Logger.Log("Selected OS profile: " + MainClientConstants.OSId.ToString());

            // The query in CheckSystemSpecifications takes lots of time,
            // so we'll do it in a separate thread to make startup faster
            Thread thread = new Thread(CheckSystemSpecifications);
            thread.Start();

            Thread idThread = new Thread(GenerateOnlineId);
            idThread.Start();

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

            if (CUpdater.CustomComponents != null)
            {
                Logger.Log("Removing partial custom component downloads.");
                foreach (CustomComponent component in CUpdater.CustomComponents)
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

            GameClass gameClass = new GameClass();
            gameClass.Run();
        }

        /// <summary>
        /// Writes processor and graphics card info to the log file.
        /// </summary>
        private void CheckSystemSpecifications()
        {
            try
            {
                string cpu = string.Empty;
                string videoController = string.Empty;
                string memory = string.Empty;

                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("SELECT * FROM Win32_Processor");

                foreach (var proc in searcher.Get())
                {
                    cpu = cpu + proc["Name"] + " (" + proc["NumberOfCores"] + " cores) ";
                }

                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

                foreach (ManagementObject mo in searcher.Get())
                {
                    PropertyData currentBitsPerPixel = mo.Properties["CurrentBitsPerPixel"];
                    PropertyData description = mo.Properties["Description"];
                    if (currentBitsPerPixel != null && description != null)
                    {
                        if (currentBitsPerPixel.Value != null)
                            videoController = videoController + "Video controller: " + description.Value.ToString() + " ";
                    }
                }

                searcher = new ManagementObjectSearcher("Select * From Win32_PhysicalMemory");
                ulong total = 0;

                foreach (ManagementObject ram in searcher.Get())
                {
                    total += Convert.ToUInt64(ram.GetPropertyValue("Capacity"));
                }

                if (total != 0)
                    memory = "Total physical memory: " + (total >= 1073741824 ? total / 1073741824 + "GB" : total / 1048576 + "MB");

                Logger.Log(string.Format("Hardware info: {0} | {1} | {2}", cpu.Trim(), videoController.Trim(), memory));

            }
            catch (Exception ex)
            {
                Logger.Log("Checking system specifications failed. Message: " + ex.Message);
            }
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
                ManagementObjectCollection moc = mos.Get();
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
                string str;
                Object o = key.GetValue("Ident");
                if (o == null)
                {
                    str = rn.Next(Int32.MaxValue - 1).ToString();
                    key.SetValue("Ident", str);
                }
                else
                    str = o.ToString();

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
