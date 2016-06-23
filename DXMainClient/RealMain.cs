using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using DTAClient.gui;
using DTAClient.domain;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient
{
    static class RealMain
    {
        public static void ProxyMain(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(HandleExcept);

            Environment.CurrentDirectory = MainClientConstants.gamepath;

            CheckPermissions();

            Logger.Initialize(MainClientConstants.gamepath + "Client\\", "client.log");
            Logger.WriteLogFile = true;

            File.Delete(MainClientConstants.gamepath + "Client\\client.log");

            if (!Directory.Exists(MainClientConstants.gamepath + "Client"))
                Directory.CreateDirectory(MainClientConstants.gamepath + "Client");

            MainClientConstants.Initialize();

            Logger.Log("***Logfile for " + MainClientConstants.GAME_NAME_LONG + " client***");
            Logger.Log("Client version: " + Application.ProductVersion);

            File.Delete(MainClientConstants.gamepath + "mainclient.log");
            File.Delete(MainClientConstants.gamepath + "launchupdt.dat");
            try
            {
                File.Delete(MainClientConstants.gamepath + "wsock32.dll");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Deleting wsock32.dll failed! Please close any applications that could be using the file, and then start the client again." + Environment.NewLine + Environment.NewLine +
                    "Message: " + ex.Message,
                    "CnCNet Client");
                Environment.Exit(0);
            }

            Application.EnableVisualStyles();

            int argsLength = args.GetLength(0);

            for (int arg = 0; arg < argsLength; arg++)
            {
                string argument = args[arg].ToUpper();

                if (argument == "-AUTOUPDATE")
                {
                    MainClientConstants.IsAutomaticInstallation = true;
                    Logger.Log("Startup parameter: Automatic installation");
                }
                else if (argument == "-RUNCLIENT")
                {
                    MainClientConstants.AutoRunCnCNetClient = true;
                    Logger.Log("Startup parameter: Automatically run CnCNet Client");
                }
                else if (argument == "-SHUTUP")
                {
                    MainClientConstants.IgnoreVersionMismatch = true;
                    Logger.Log("Startup parameter: Do not show version mismatch popup");
                }
                else
                {
                    Logger.Log("Unknown startup parameter: " + argument);
                }
            }

            //try
            //{
            //    if (ParentProcessUtilities.GetParentProcess().ProcessName == "FinalTI")
            //    {
            //        Logger.Log("Launched through FinalSun.");
            //    }
            //}
            //catch
            //{
            //}

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

            MessageBox.Show(string.Format("{0} has crashed. Error message:" + Environment.NewLine + Environment.NewLine +
                ex.Message + Environment.NewLine + Environment.NewLine +
                "If the issue is repeatable, contact the {1} staff at {2}.",
                MainClientConstants.GAME_NAME_LONG,
                MainClientConstants.GAME_NAME_SHORT,
                MainClientConstants.SUPPORT_URL_SHORT),
                "KABOOOOOOOM", MessageBoxButtons.OK);
        }

        static void CheckPermissions()
        {
            try
            {
                File.Delete(Environment.CurrentDirectory + "\\tmpfile");
                FileStream fs = File.Create(Environment.CurrentDirectory + "\\tmpfile");
                fs.Close();
                File.Delete(Environment.CurrentDirectory + "\\tmpfile");
            }
            catch (UnauthorizedAccessException)
            {
                DialogResult dr = MessageBox.Show(String.Format("You seem to be running {0} from a write-protected directory." + Environment.NewLine + Environment.NewLine +
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
        }
    }
}
