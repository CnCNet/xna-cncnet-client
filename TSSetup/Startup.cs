using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;
using System.Management;
using Microsoft.Win32;
using dtasetup.domain;
using dtasetup.domain.cncnet5;
using dtasetup.gui;
using ClientCore;
using Updater;
using DTAConfig;

namespace dtasetup
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
            int themeId = DomainController.Instance().GetSelectedThemeId();

            if (themeId > DomainController.Instance().GetThemeCount())
                themeId = 0;

            Logger.Log("Initializing updater.");

            File.Delete(ProgramConstants.gamepath + "version_u");

            CUpdater.Initialize();
            ProgramConstants.RESOURCES_DIR = "Resources\\" + DomainController.Instance().GetThemeInfoFromIndex(themeId)[1];
            DomainController.Instance().ReloadSettings();

            SplashScreen ss = new SplashScreen();
            ss.Show();

            CheckIfAlreadyRunning();
            DetectOperatingSystem();
            CheckSystemSpecifications();
            CheckIfFirstRun();
            InstallCompatibilityFixes();

            // Delete updater temporary directory
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

            // Remove partial custom component downloads
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

            // Check if FinalSun.ini exists
            FinalSunSettings.WriteFinalSunIni();

            if (MainClientConstants.IsAutomaticInstallation)
            {
                Logger.Log("Performing automatic installation.");

                CUpdater.DoVersionCheck();
                Thread thread = new Thread(new ThreadStart(CUpdater.PerformUpdate));
                thread.Start();

                new UpdateForm().ShowDialog();
            }

            SerialHandler.CheckForSerial();
            InitializeVersioning();

            // Check if we should enter CnCNet
            if (MainClientConstants.AutoRunCnCNetClient)
            {
                ss.Hide();

                ProcessStartInfo startInfo = new ProcessStartInfo(MainClientConstants.gamepath + "cncnetclient.dat");
                startInfo.Arguments = "-VER" + CUpdater.GameVersion;
                startInfo.UseShellExecute = false;
                Process process = Process.Start(startInfo);

                process.WaitForExit();

                if (process.ExitCode == 1337)
                {
                    Logger.Log("The CnCNet client was switched - exiting.");
                    Environment.Exit(0);
                }
            }

            ss.Close();

            WriteInstallPathToRegistry();

            Application.Run(new gui.MainMenu());
        }

        /// <summary>
        /// Checks if the client or the game is already running.
        /// If so, asks the user to terminate them and exits.
        /// </summary>
        private void CheckIfAlreadyRunning()
        {
            // Check if the launcher is already running
            Process[] launcherProcesses = Process.GetProcessesByName(
                Path.GetFileNameWithoutExtension(CUpdater.CURRENT_LAUNCHER_NAME));

            if (launcherProcesses.GetLength(0) > 1)
            {
                int processCount = 0;
                for (int processId = 0; processId < launcherProcesses.GetLength(0); processId++)
                {
                    try
                    {
                        if (launcherProcesses[processId].MainModule.FileName != Application.ExecutablePath)
                        {
                            continue;
                        }
                    }
                    catch
                    {
                        Logger.Log("Failed to get main module from " + launcherProcesses[processId].ProcessName + "!");
                    }

                    processCount++;

                    if (processCount < 2)
                    {
                        continue;
                    }

                    Logger.Log("The client is already running.");

                    try
                    {
                        if (launcherProcesses[0].Responding == false)
                            launcherProcesses[0].Kill();
                        else
                        {

                            MsgBoxForm msForm = new MsgBoxForm("An instance of the client is already running.", "Client already running",
                                MessageBoxButtons.OK);
                            msForm.ShowDialog();
                            Environment.Exit(0);
                        }
                    }
                    catch
                    {
                        Logger.Log("The client is already running, but the instance is frozen and cannot be terminated.");
                        MsgBoxForm msForm = new MsgBoxForm(string.Format(
                            "A frozen previous instance of the client is already running, but it cannot be terminated." + Environment.NewLine +
                            "Please terminate the previous instance ({0}) and run the client again",
                            CUpdater.CURRENT_LAUNCHER_NAME), "Launcher error", MessageBoxButtons.OK);
                        msForm.ShowDialog();
                        Environment.Exit(0);
                    }
                }
            }

            // Check if DTA is already running
            string mainProcess = ClientCore.DomainController.Instance().GetGameExecutableName(0);
            Process[] dtaProcesses = Process.GetProcessesByName(mainProcess);

            if (dtaProcesses.GetLength(0) > 0)
            {
                for (int processId = 0; processId < dtaProcesses.GetLength(0); processId++)
                {
                    string filePath = Application.StartupPath.ToLower() + "\\" + mainProcess;
                    if (dtaProcesses[processId].MainModule.FileName.ToLower() == filePath.ToLower())
                    {
                        // ^ An instance of DTA is running
                        MsgBoxForm msForm = new MsgBoxForm(string.Format("{0} ({1}) is already running. " + Environment.NewLine +
                            "To use the Client, please quit {0}" + Environment.NewLine +
                            "or terminate the process {1} if the game is not responding.",
                            MainClientConstants.GAME_NAME_LONG, mainProcess),
                            string.Format("{0} already running", MainClientConstants.GAME_NAME_SHORT), MessageBoxButtons.OK);
                        msForm.ShowDialog();

                        Environment.Exit(0);
                    }
                }
            }
        }

        /// <summary>
        /// Detects the operating system that the client is running on and
        /// writes the value to MainClientConstants.OsId.
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
                case OSVersion.WIN7:
                    Logger.Log("Selected OS profile: Windows Vista / 7");
                    break;
                case OSVersion.WIN8:
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
                ManagementObjectSearcher searcher = 
                    new ManagementObjectSearcher("SELECT * FROM Win32_Processor");

                foreach (var proc in searcher.Get())
                {
                    Logger.Log("Processor name: " + proc["Name"] + " (" + proc["NumberOfCores"] + " cores)");
                }

                searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

                foreach (ManagementObject mo in searcher.Get())
                {
                    PropertyData currentBitsPerPixel = mo.Properties["CurrentBitsPerPixel"];
                    PropertyData description = mo.Properties["Description"];
                    if (currentBitsPerPixel != null && description != null)
                    {
                        if (currentBitsPerPixel.Value != null)
                            Logger.Log("Video controller: " + description.Value.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Checking system specifications failed. Message: " + ex.Message);
            }
        }

        /// <summary>
        /// Checks the integrity of local files and starts an update if automatic updates are enabled.
        /// </summary>
        private void InitializeVersioning()
        {
            bool isYR = DomainController.Instance().GetDefaultGame().ToUpper() == "YR";

            if (MCDomainController.Instance().GetModModeStatus() && !isYR)
                return;

            if (CUpdater.DTAVersionState == VersionState.UPDATEINPROGRESS)
                return;

            if (File.Exists(MainClientConstants.gamepath + MainClientConstants.NEW_VERSION) && !CUpdater.LocalFileVersionsChecked)
            {
                CUpdater.CheckLocalFileVersions();
            }
            else if (isYR)
            {
                CUpdater.IsVersionMismatch = true;
                return;
            }

            //if (!CUpdater.IsVersionMismatch && !MainClientConstants.AutoRunCnCNetClient)
            //{
            //    if (isYR)
            //    {
            //        Thread thread = new Thread(new ThreadStart(CUpdater.DoVersionCheck));
            //        thread.Start();
            //        return;
            //    }

            //    if (MCDomainController.Instance().getAutomaticUpdateStatus())
            //    {
            //        Thread thread = new Thread(new ThreadStart(CUpdater.DoVersionCheck));
            //        thread.Start();
            //    }
            //}
        }

        /// <summary>
        /// Writes the game installation path to the Windows registry.
        /// </summary>
        private void WriteInstallPathToRegistry()
        {
            if (!MCDomainController.Instance().GetInstallationPathWriteStatus())
            {
                Logger.Log("Skipping writing installation path to the Windows Registry because of INI setting.");
                return;
            }

            Logger.Log("Writing installation path to the Windows registry.");

            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + MCDomainController.Instance().GetInstallationPathRegKey());
            key.SetValue("InstallPath", MainClientConstants.gamepath);
            key.Close();
        }

        /// <summary>
        /// Checks if the game is started for the first time. If so, asks the user if they wish to configure settings.
        /// </summary>
        private void CheckIfFirstRun()
        {
            bool firstRun = MCDomainController.Instance().IsFirstRun();

            if (MCDomainController.Instance().GetShortGameName() == "YR")
                return;

            if (firstRun)
            {
                MCDomainController.Instance().SetFirstRun();

                DialogResult dr = new MsgBoxForm(string.Format("You have just installed {0}. " + Environment.NewLine +
                    "It's highly recommended that you configure your settings before playing." + Environment.NewLine +
                    "Do you want to configure them now?", MainClientConstants.GAME_NAME_SHORT), "Initial installation", MessageBoxButtons.YesNo).ShowDialog();

                if (dr == DialogResult.OK)
                {
                    new OptionsForm().ShowDialog();
                    MCDomainController.Instance().ReloadSettings();
                    DomainController.Instance().ReloadSettings();
                }
            }
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
            if (MainClientConstants.OSId != OSVersion.WIN8 || !File.Exists(ProgramConstants.gamepath + "Resources\\compatfix.sdb"))
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
                if (MCDomainController.Instance().Win8CompatFixInstalled())
                {
                    MsgBoxForm.Show("An old compatibility fix has been detected." + Environment.NewLine +
                        "It will be uninstalled before installing the new compatibility fix.",
                        "Outdated Compatibility Fix Detected", MessageBoxButtons.OK);

                    try
                    {
                        Process sdbinst = Process.Start("sdbinst.exe", "-q -n \"TS Compatibility Fix\"");

                        sdbinst.WaitForExit();

                        MCDomainController.Instance().SetWin8CompatFixInstalled(false);
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

                    Process sdbinst = Process.Start("sdbinst.exe", "-q \"" + ProgramConstants.gamepath + "Resources\\compatfix.sdb\"");

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

            if (!File.Exists(ProgramConstants.gamepath + "Resources\\FSCompatFix.sdb"))
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

                    Process sdbinst = Process.Start("sdbinst.exe", "-q \"" + ProgramConstants.gamepath + "Resources\\FSCompatFix.sdb\"");

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
