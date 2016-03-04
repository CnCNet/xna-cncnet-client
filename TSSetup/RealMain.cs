using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using dtasetup.gui;
using dtasetup.domain;
using ClientCore;

namespace dtasetup
{
    static class RealMain
    {
        public static void ProxyMain(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(HandleExcept);
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

            Logger.LogFileName = "client.log";
            Logger.Log("Initializing constants.");
            MainClientConstants.Initialize();

            CheckPermissions();

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

            Logger.Log("***Logfile for " + MainClientConstants.GAME_NAME_LONG + " client***");
            Logger.Log("Client version: " + Application.ProductVersion);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Logger.Log("Initializing settings.");
            MCDomainController.Instance();
            DomainController.Instance();

            int argsLength = args.GetLength(0);
            if (argsLength > 0)
            {
                for (int arg = 0; arg < argsLength; arg++)
                {
                    string argument = args[arg].ToUpper(System.Globalization.CultureInfo.GetCultureInfo("fi-FI"));

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
            }

            try
            {
                if (ParentProcessUtilities.GetParentProcess().ProcessName == "FinalTI")
                {
                    Logger.Log("Launched through FinalSun.");

                    Environment.CurrentDirectory = MainClientConstants.gamepath;
                }
            }
            catch
            {
            }


            new Startup().Execute();
        }

        static void HandleExcept(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;

            Logger.Log("An unhandled exception has occured. Info:");
            Logger.Log("Message: " + ex.Message);
            Logger.Log("Source: " + ex.Source);
            Logger.Log("TargetSite.Name: " + ex.TargetSite.Name);
            Logger.Log("Stacktrace: " + ex.StackTrace);

            MessageBox.Show(string.Format("{0} has crashed. If you were in the middle of doing something, the operation has been canceled." + Environment.NewLine + Environment.NewLine +
                ex.Message + Environment.NewLine + "Please see client.log for more info." + Environment.NewLine +
                "If the issue is repeatable, contact the {1} staff at {2}.",
                MainClientConstants.GAME_NAME_LONG,
                MainClientConstants.GAME_NAME_SHORT,
                MainClientConstants.SUPPORT_URL_SHORT),
                "KABOOOOOOOM", MessageBoxButtons.OK);
        }

        static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains("AudioOptions"))
            {
                // Load DLL from Resources\Binaries
                byte[] data = File.ReadAllBytes(MainClientConstants.gamepath + ProgramConstants.BASE_RESOURCE_PATH + "Binaries\\AudioOptions.dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.Contains("DisplayOptions"))
            {
                byte[] data = File.ReadAllBytes(MainClientConstants.gamepath + ProgramConstants.BASE_RESOURCE_PATH + "Binaries\\DisplayOptions.dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.Contains("DTAUpdater"))
            {
                byte[] data = File.ReadAllBytes(MainClientConstants.gamepath + ProgramConstants.BASE_RESOURCE_PATH + "Binaries\\DTAUpdater.dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.Contains("DTAConfig"))
            {
                byte[] data = File.ReadAllBytes(MainClientConstants.gamepath + ProgramConstants.BASE_RESOURCE_PATH + "Binaries\\DTAConfig.dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.Contains("GenericOptions"))
            {
                byte[] data = File.ReadAllBytes(MainClientConstants.gamepath + ProgramConstants.BASE_RESOURCE_PATH + "Binaries\\GenericOptions.dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.Contains("GameOptions"))
            {
                byte[] data = File.ReadAllBytes(MainClientConstants.gamepath + ProgramConstants.BASE_RESOURCE_PATH + "Binaries\\GameOptions.dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.Contains("UpdaterOptions"))
            {
                byte[] data = File.ReadAllBytes(MainClientConstants.gamepath + ProgramConstants.BASE_RESOURCE_PATH + "Binaries\\UpdaterOptions.dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.Contains("CnCNetOptions"))
            {
                byte[] data = File.ReadAllBytes(MainClientConstants.gamepath + ProgramConstants.BASE_RESOURCE_PATH + "Binaries\\CnCNetOptions.dll");
                return System.Reflection.Assembly.Load(data);
            }

            if (args.Name.Contains("ComponentOptions"))
            {
                byte[] data = File.ReadAllBytes(MainClientConstants.gamepath + ProgramConstants.BASE_RESOURCE_PATH + "Binaries\\ComponentOptions.dll");
                return System.Reflection.Assembly.Load(data);
            }

            return null;
        }

        static void CheckPermissions()
        {
            try
            {
                System.IO.File.Delete(Environment.CurrentDirectory + "\\client.log");
                FileStream fs = System.IO.File.Create(Environment.CurrentDirectory + "\\client.log");
                fs.Close();
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
