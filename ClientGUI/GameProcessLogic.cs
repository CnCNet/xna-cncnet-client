using System;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using ClientCore;
using Rampastring.Tools;

namespace ClientGUI
{
    /// <summary>
    /// A static class used for controlling the launching and exiting of the game executable.
    /// </summary>
    public static class GameProcessLogic
    {
        public static event Action GameProcessStarted;

        public static event Action GameProcessStarting;

        public static event Action GameProcessExited;

        /// <summary>
        /// Starts the main game process.
        /// </summary>
        public static void StartGameProcess()
        {
            Logger.Log("About to launch main game executable.");

            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();

            string gameExecutableName = osVersion == OSVersion.UNIX ? 
                ClientConfiguration.Instance.GetUnixGameExecutableName() : 
                ClientConfiguration.Instance.GetGameExecutableName();

            if (osVersion == OSVersion.UNIX)
                gameExecutableName = ClientConfiguration.Instance.GetUnixGameExecutableName();
            else
                gameExecutableName = ClientConfiguration.Instance.GetGameExecutableName();

            string extraCommandLine = ClientConfiguration.Instance.ExtraExeCommandLineParameters;

            File.Delete(ProgramConstants.GamePath + "DTA.LOG");
            File.Delete(ProgramConstants.GamePath + "TI.LOG");
            File.Delete(ProgramConstants.GamePath + "TS.LOG");

            GameProcessStarting?.Invoke();

            if (UserINISettings.Instance.WindowedMode)
            {
                Logger.Log("Windowed mode is enabled - using QRes.");
                Process QResProcess = new Process();
                QResProcess.StartInfo.FileName = ProgramConstants.QRES_EXECUTABLE;
                QResProcess.StartInfo.UseShellExecute = false;
                if (!string.IsNullOrEmpty(extraCommandLine))
                    QResProcess.StartInfo.Arguments = "c=16 /R " + "\"" + ProgramConstants.GamePath + gameExecutableName + "\" " + extraCommandLine + " -SPAWN";
                else
                    QResProcess.StartInfo.Arguments = "c=16 /R " + "\"" + ProgramConstants.GamePath + gameExecutableName + "\" " + "-SPAWN";
                QResProcess.EnableRaisingEvents = true;
                QResProcess.Exited += new EventHandler(Process_Exited);
                try
                {
                    QResProcess.Start();
                }
                catch (Exception ex)
                {
                    Logger.Log("Error launching QRes: " + ex.Message);
                    MessageBox.Show("Error launching " + ProgramConstants.QRES_EXECUTABLE + ". Please check that your anti-virus isn't blocking the CnCNet Client. " +
                        "You can also try running the client as an administrator." + Environment.NewLine + Environment.NewLine + "You are unable to participate in this match." +
                        Environment.NewLine + Environment.NewLine + "Returned error: " + ex.Message,
                        "Error launching game", MessageBoxButtons.OK);
                    Process_Exited(QResProcess, EventArgs.Empty);
                    return;
                }

                if (Environment.ProcessorCount > 1)
                    QResProcess.ProcessorAffinity = (IntPtr)2;
            }
            else
            {
                Process DtaProcess = new Process();
                DtaProcess.StartInfo.FileName = gameExecutableName;
                DtaProcess.StartInfo.UseShellExecute = false;
                if (!string.IsNullOrEmpty(extraCommandLine))
                    DtaProcess.StartInfo.Arguments = " " + extraCommandLine + " -SPAWN";
                else
                    DtaProcess.StartInfo.Arguments = "-SPAWN";
                DtaProcess.EnableRaisingEvents = true;
                DtaProcess.Exited += new EventHandler(Process_Exited);
                try
                {
                    DtaProcess.Start();
                }
                catch (Exception ex)
                {
                    Logger.Log("Error launching " + gameExecutableName + ": " + ex.Message);
                    MessageBox.Show("Error launching " + gameExecutableName + ". Please check that your anti-virus isn't blocking the CnCNet Client. " +
                        "You can also try running the client as an administrator." + Environment.NewLine + Environment.NewLine + "You are unable to participate in this match." + 
                        Environment.NewLine + Environment.NewLine + "Returned error: " + ex.Message,
                        "Error launching game", MessageBoxButtons.OK);
                    Process_Exited(DtaProcess, EventArgs.Empty);
                    return;
                }

                if (Environment.ProcessorCount > 1)
                    DtaProcess.ProcessorAffinity = (IntPtr)2;
            }

            GameProcessStarted?.Invoke();

            Logger.Log("Waiting for qres.dat or " + gameExecutableName + " to exit.");
        }

        static void Process_Exited(object sender, EventArgs e)
        {
            Process proc = (Process)sender;
            proc.Exited -= Process_Exited;
            proc.Dispose();
            GameProcessExited?.Invoke();
        }
    }
}
