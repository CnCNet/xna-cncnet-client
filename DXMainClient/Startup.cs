using System;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Management;
using Microsoft.Win32;
using DTAClient.Domain;
using ClientCore;
using ClientGUI;
using Updater;
using Rampastring.Tools;
using DTAClient.DXGUI;

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
            int themeId = UserINISettings.Instance.ClientTheme;

            if (themeId >= DomainController.Instance().GetThemeCount() || themeId < 0)
            {
                themeId = 0;
                UserINISettings.Instance.ClientTheme.Value = 0;
            }

            ProgramConstants.RESOURCES_DIR = "Resources\\" + DomainController.Instance().GetThemeInfoFromIndex(themeId)[1];

            Logger.Log("Initializing updater.");

            File.Delete(ProgramConstants.GamePath + "version_u");

            CUpdater.Initialize(DomainController.Instance().GetDefaultGame());

            DetectOperatingSystem();

            Thread thread = new Thread(CheckSystemSpecifications);
            thread.Start();

            if (Directory.Exists(MainClientConstants.gamepath + "Updater"))
            {
                Logger.Log("Attempting to delete temporary updater directory.");
                try
                {
                    Directory.Delete(MainClientConstants.gamepath + "Updater", true);
                }
                catch
                {
                }
            }

            if (CUpdater.CustomComponents != null)
            {
                Logger.Log("Removing partial custom component downloads.");
                foreach (CustomComponent component in CUpdater.CustomComponents)
                {
                    try
                    {
                        File.Delete(MainClientConstants.gamepath + component.LocalPath + "_u");
                    }
                    catch
                    {

                    }
                }
            }

            FinalSunSettings.WriteFinalSunIni();

            WriteInstallPathToRegistry();

            DomainController.Instance().RefreshSettings();

            InstallCompatibilityFixes();

            GameClass gameClass = new GameClass();
            gameClass.Run();
        }

        /// <summary>
        /// Reports the operating system that the client is running on.
        /// </summary>
        private void DetectOperatingSystem()
        {
            Logger.Log("Operating system: " + Environment.OSVersion.VersionString);
            
            switch (MainClientConstants.OSId)
            {
                case OSVersion.UNKNOWN:
                    Logger.Log("Selected OS profile: Unknown OS");
                    break;
                case OSVersion.WIN9X:
                    Logger.Log("Selected OS profile: Windows 9x (??)");
                    break;
                case OSVersion.WINXP:
                    Logger.Log("Selected OS profile: Windows XP");
                    break;
                case OSVersion.WINVISTA:
                    Logger.Log("Selected OS profile: Windows Vista");
                    break;
                case OSVersion.WIN7:
                    Logger.Log("Selected OS profile: Windows 7");
                    break;
                case OSVersion.WIN810:
                    Logger.Log("Selected OS profile: Windows 8 / 10");
                    break;
            }
        }

        /// <summary>
        /// Writes processor and graphics card info to the log file.
        /// </summary>
        private void CheckSystemSpecifications()
        {
            try
            {
                string cpu = String.Empty;
                string videoController = String.Empty;

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

                Logger.Log("Hardware info: {0} {1}", cpu, videoController);
            }
            catch (Exception ex)
            {
                Logger.Log("Checking system specifications failed. Message: " + ex.Message);
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

            RegistryKey key;
            key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + MCDomainController.Instance.InstallationPathRegKey);
            key.SetValue("InstallPath", MainClientConstants.gamepath);
            key.Close();
        }

        /// <summary>
        /// Installs the necessary compatibility fixes for this game if they're included.
        /// </summary>
        private void InstallCompatibilityFixes()
        {
            InstallTSCompatibilityFix();
            InstallFSCompatibilityFix();
        }

        private void InstallTSCompatibilityFix()
        {
            if (MainClientConstants.OSId != OSVersion.WIN810 || !File.Exists(ProgramConstants.GamePath + "Resources\\compatfix.sdb"))
                return;

            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Tiberian Sun Client", true);

            if (regKey != null)
            {
                object value = regKey.GetValue("TSCompatFixInstalled", "No");

                string valueString = (string)value;

                if (valueString == "Yes")
                    return;

                value = regKey.GetValue("TSCompatFixDeclined", "No");

                valueString = (string)value;

                if (valueString == "Yes")
                    return;
            }

            DialogResult dr = MsgBoxForm.Show("A new performance-enhancing compatibility fix for Windows 8 and 10" + Environment.NewLine +
                "has been included in this version of " + MainClientConstants.GAME_NAME_SHORT + ". Enabling it requires" + Environment.NewLine +
                "administrative priveleges. Would you like to install the compatibility fix?", "New Compatibility Fix", MessageBoxButtons.YesNo);

            if (dr == DialogResult.OK)
            {
                if (MCDomainController.Instance.Win8CompatFixInstalled())
                {
                    MsgBoxForm.Show("An old compatibility fix has been detected." + Environment.NewLine +
                        "It will be uninstalled before installing the new compatibility fix.",
                        "Outdated Compatibility Fix Detected", MessageBoxButtons.OK);

                    try
                    {
                        Process sdbinst = Process.Start("sdbinst.exe", "-q -n \"TS Compatibility Fix\"");

                        sdbinst.WaitForExit();

                        MCDomainController.Instance.SetWin8CompatFixInstalled(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("Uninstalling compatibility fix failed. Message: " + ex.Message);
                        MsgBoxForm.Show("Uninstalling compatibility fix failed. Message: " + ex.Message,
                            "Compatibility fix failed", MessageBoxButtons.OK);
                        return;
                    }
                }

                try
                {
                    Logger.Log("Installing Windows 8/10 compatibility fix.");

                    Process sdbinst = Process.Start("sdbinst.exe", "-q \"" + ProgramConstants.GamePath + "Resources\\compatfix.sdb\"");

                    sdbinst.WaitForExit();

                    regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                    regKey = regKey.CreateSubKey("Tiberian Sun Client");
                    regKey.SetValue("TSCompatFixInstalled", "Yes");
                    MsgBoxForm.Show("Compatibility fix succesfully installed.", "Success", MessageBoxButtons.OK);
                }
                catch (Exception ex)
                {
                    Logger.Log("Installing compatibility fix failed. Message: " + ex.Message);
                    MsgBoxForm.Show("Installing compatibility fix failed. Message: " + ex.Message,
                        "Compatibility fix failed", MessageBoxButtons.OK);
                }

                return;
            }

            try
            {
                regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                regKey = regKey.CreateSubKey("Tiberian Sun Client");
                regKey.SetValue("TSCompatFixDeclined", "Yes");
            }
            catch (Exception ex)
            {
                Logger.Log("Setting TSCompatFixDeclined failed! Returned error: " + ex.Message);
            }
        }

        private void InstallFSCompatibilityFix()
        {
            if (MainClientConstants.OSId == OSVersion.WIN9X ||
                MainClientConstants.OSId == OSVersion.WINXP || 
                MainClientConstants.OSId == OSVersion.UNKNOWN)
                return;

            if (!File.Exists(ProgramConstants.GamePath + "Resources\\FSCompatFix.sdb"))
                return;

            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Tiberian Sun Client", true);

            if (regKey == null)
            {
                ProceedInstallFSCompatibilityFix();
                return;
            }

            object val = regKey.GetValue("FSCompatFixDeclined", "No");
            string stringValue = (string)val;

            if (stringValue == "Yes")
                return;

            val = regKey.GetValue("FSCompatFixInstalled", "No");
            stringValue = (string)val;

            if (stringValue == "No")
                ProceedInstallFSCompatibilityFix();
        }

        private void ProceedInstallFSCompatibilityFix()
        {
            DialogResult dr = MsgBoxForm.Show("A new performance-enhancing compatibility fix for the FinalSun map editor" + Environment.NewLine +
                "has been included in this version of " + MainClientConstants.GAME_NAME_SHORT + ". Enabling it requires" + Environment.NewLine +
                "administrative priveleges. Would you like to install the compatibility fix?", "FinalSun Compatibility Fix", MessageBoxButtons.YesNo);

            if (dr == DialogResult.OK)
            {
                try
                {
                    Logger.Log("Installing FinalSun compatibility fix.");

                    Process sdbinst = Process.Start("sdbinst.exe", "-q \"" + ProgramConstants.GamePath + "Resources\\FSCompatFix.sdb\"");

                    sdbinst.WaitForExit();

                    RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                    regKey = regKey.CreateSubKey("Tiberian Sun Client");
                    regKey.SetValue("FSCompatFixInstalled", "Yes");
                    MsgBoxForm.Show("FinalSun Compatibility fix succesfully installed.", "Success", MessageBoxButtons.OK);
                }
                catch (Exception ex)
                {
                    Logger.Log("Installing compatibility fix failed. Message: " + ex.Message);
                    MsgBoxForm.Show("Installing compatibility fix failed. Message: " + ex.Message,
                        "Compatibility fix failed", MessageBoxButtons.OK);
                }

                return;
            }

            try
            {
                RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                regKey = regKey.CreateSubKey("Tiberian Sun Client");
                regKey.SetValue("FSCompatFixDeclined", "Yes");
            }
            catch (Exception ex)
            {
                Logger.Log("Setting FSCompatFixDeclined failed! Returned error: " + ex.Message);
            }
        }
    }
}
