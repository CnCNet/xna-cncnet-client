﻿using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using DTAClient.Domain;
using Rampastring.Tools;
using ClientCore;
using Rampastring.XNAUI;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Collections.Generic;

namespace DTAClient
{
    /// <summary>
    /// Contains client startup parameters.
    /// </summary>
    struct StartupParams
    {
        public StartupParams(bool noAudio, bool multipleInstanceMode,
            List<string> unknownParams)
        {
            NoAudio = noAudio;
            MultipleInstanceMode = multipleInstanceMode;
            UnknownStartupParams = unknownParams;
        }

        public bool NoAudio { get; }
        public bool MultipleInstanceMode { get; }
        public List<string> UnknownStartupParams { get; }
    }

    static class PreStartup
    {
        /// <summary>
        /// Initializes various basic systems like the client's logger, 
        /// constants, and the general exception handler.
        /// Reads the user's settings from an INI file, 
        /// checks for necessary permissions and starts the client if
        /// everything goes as it should.
        /// </summary>
        /// <param name="parameters">The client's startup parameters.</param>
        public static void Initialize(StartupParams parameters)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(HandleExcept);

            Environment.CurrentDirectory = MainClientConstants.gamepath;

            CheckPermissions();

            Logger.Initialize(MainClientConstants.gamepath + "Client\\", "client.log");
            Logger.WriteLogFile = true;

            if (!Directory.Exists(MainClientConstants.gamepath + "Client"))
                Directory.CreateDirectory(MainClientConstants.gamepath + "Client");

            File.Delete(MainClientConstants.gamepath + "Client\\client.log");

            MainClientConstants.Initialize();

            Logger.Log("***Logfile for " + MainClientConstants.GAME_NAME_LONG + " client***");
            Logger.Log("Client version: " + Application.ProductVersion);

            // Log information about given startup params
            if (parameters.NoAudio)
                Logger.Log("Startup parameter: No audio");

            if (parameters.MultipleInstanceMode)
                Logger.Log("Startup parameter: Allow multiple client instances");

            parameters.UnknownStartupParams.ForEach(p => Logger.Log("Unknown startup parameter: " + p));

            Logger.Log("Loading settings.");

            UserINISettings.Initialize(ClientConfiguration.Instance.SettingsIniName);

            // Delete obsolete files from old target project versions

            File.Delete(MainClientConstants.gamepath + "mainclient.log");
            File.Delete(MainClientConstants.gamepath + "launchupdt.dat");
            try
            {
                File.Delete(MainClientConstants.gamepath + "wsock32.dll");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Deleting wsock32.dll failed! Please close any " +
                    "applications that could be using the file, and then start the client again."
                    + Environment.NewLine + Environment.NewLine +
                    "Message: " + ex.Message,
                    "CnCNet Client");
                Environment.Exit(0);
            }

            Application.EnableVisualStyles();

            new Startup().Execute();
        }

        static void HandleExcept(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;

            Logger.Log("KABOOOOOOM!!! Info:");
            Logger.Log("Message: " + ex.Message);
            Logger.Log("Source: " + ex.Source);
            Logger.Log("TargetSite.Name: " + ex.TargetSite.Name);
            Logger.Log("Stacktrace: " + ex.StackTrace);
            if (ex.InnerException != null)
            {
                Logger.Log("InnerException info:");
                Logger.Log("Message: " + ex.InnerException.Message);
                Logger.Log("Stacktrace: " + ex.InnerException.StackTrace);
            }

            try
            {
                if (Directory.Exists(Environment.CurrentDirectory + "\\Client\\ErrorLogs"))
                {
                    DateTime dtn = DateTime.Now;

                    File.Copy(Environment.CurrentDirectory + "\\Client\\client.log",
                        Environment.CurrentDirectory + string.Format("\\Client\\ErrorLogs\\ClientCrashLog_{0}_{1}_{2}_{3}_{4}.txt",
                        dtn.Day, dtn.Month, dtn.Year, dtn.Hour, dtn.Minute), true);
                }
            }
            catch { }

            MessageBox.Show(string.Format("{0} has crashed. Error message:" + Environment.NewLine + Environment.NewLine +
                ex.Message + Environment.NewLine + Environment.NewLine +
                "If the issue is repeatable, contact the {1} staff at {2}.",
                MainClientConstants.GAME_NAME_LONG,
                MainClientConstants.GAME_NAME_SHORT,
                MainClientConstants.SUPPORT_URL_SHORT),
                "KABOOOOOOOM", MessageBoxButtons.OK);
        }

        private static void CheckPermissions()
        {
            if (UserHasDirectoryAccessRights(Environment.CurrentDirectory, FileSystemRights.Modify))
                return;

            DialogResult dr = MessageBox.Show(string.Format("You seem to be running {0} from a write-protected directory." + Environment.NewLine + Environment.NewLine +
                "For {1} to function properly when run from a write-protected directory, it needs administrative priveleges." + Environment.NewLine + Environment.NewLine +
                "Would you like to restart the client with administrative rights?" + Environment.NewLine + Environment.NewLine +
                "Please also make sure that your security software isn't blocking {1}.", MainClientConstants.GAME_NAME_LONG, MainClientConstants.GAME_NAME_SHORT),
                "Administrative priveleges required", MessageBoxButtons.YesNo);

            if (dr == DialogResult.No)
                Environment.Exit(0);

            ProcessStartInfo psInfo = new ProcessStartInfo();
            psInfo.FileName = Application.ExecutablePath;
            psInfo.Verb = "runas";
            Process.Start(psInfo);
            Environment.Exit(0);
        }

        /// <summary>
        /// Checks whether the client has specific file system rights to a directory.
        /// See ssds's answer at https://stackoverflow.com/questions/1410127/c-sharp-test-if-user-has-write-access-to-a-folder
        /// </summary>
        /// <param name="path">The path to the directory.</param>
        /// <param name="accessRights">The file system rights.</param>
        private static bool UserHasDirectoryAccessRights(string path, FileSystemRights accessRights)
        {
#if WINDOWSGL
            // Mono doesn't implement everything necessary for the below to work,
            // so we'll just return to make the client able to run on non-Windows
            // platforms
            // On Windows you rarely have a reason for using the OpenGL build anyway
            return true;
#endif

#pragma warning disable 0162
            var currentUser = WindowsIdentity.GetCurrent();
#pragma warning restore 0162
            var principal = new WindowsPrincipal(currentUser);

            // If the user is not running the client with administrator privileges in Program Files, they need to be prompted to do so.
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                string progfiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string progfilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                if (Environment.CurrentDirectory.Contains(progfiles) || Environment.CurrentDirectory.Contains(progfilesx86))
                    return false;
            }

            var isInRoleWithAccess = false;

            try
            {
                var di = new DirectoryInfo(path);
                var acl = di.GetAccessControl();
                var rules = acl.GetAccessRules(true, true, typeof(NTAccount));

                foreach (AuthorizationRule rule in rules)
                {
                    var fsAccessRule = rule as FileSystemAccessRule;
                    if (fsAccessRule == null)
                        continue;

                    if ((fsAccessRule.FileSystemRights & accessRights) > 0)
                    {
                        var ntAccount = rule.IdentityReference as NTAccount;
                        if (ntAccount == null)
                            continue;

                        if (principal.IsInRole(ntAccount.Value))
                        {
                            if (fsAccessRule.AccessControlType == AccessControlType.Deny)
                                return false;
                            isInRoleWithAccess = true;
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            return isInRoleWithAccess;
        }
    }
}
