#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using Rampastring.Tools;

namespace ClientCore.Launch;

/// <summary>Starts the game executable and watches for it to exit.</summary>
public static class GameProcessHost
{
    /// <summary>Whether to start the game through QRes when it runs in windowed mode.</summary>
    public static bool UseQres { get; set; }

    /// <summary>Whether to run the game on a single CPU core.</summary>
    public static bool SingleCoreAffinity { get; set; }

    /// <summary>
    /// Starts the game executable with the -SPAWN argument. The spawn files must already be written.
    /// </summary>
    public static LaunchResult Start(LaunchRequest request)
    {
        OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();

        string gameExecutableName;
        string additionalExecutableName = string.Empty;

        if (osVersion == OSVersion.UNIX)
            gameExecutableName = ClientConfiguration.Instance.UnixGameExecutableName;
        else
        {
            string launcherExecutableName = ClientConfiguration.Instance.GameLauncherExecutableName;
            if (string.IsNullOrEmpty(launcherExecutableName))
                gameExecutableName = ClientConfiguration.Instance.GetGameExecutableName();
            else
            {
                gameExecutableName = launcherExecutableName;
                additionalExecutableName = "\"" + ClientConfiguration.Instance.GetGameExecutableName() + "\" ";
            }
        }

        string extraCommandLine = ClientConfiguration.Instance.ExtraExeCommandLineParameters;

        SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "DTA.LOG");
        SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "TI.LOG");
        SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "TS.LOG");

        GameSessionManager.OnGameStarting();

        var session = new GameSession(request);
        Process process;
        string startedExecutableName;

        if (UserINISettings.Instance.WindowedMode && UseQres && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Logger.Log("Windowed mode is enabled - using QRes.");
            process = new Process();
            process.StartInfo.FileName = ProgramConstants.QRES_EXECUTABLE;
            process.StartInfo.UseShellExecute = false;

            if (!string.IsNullOrEmpty(extraCommandLine))
                process.StartInfo.Arguments = "c=16 /R " + "\"" + SafePath.CombineFilePath(ProgramConstants.GamePath, gameExecutableName) + "\" " + additionalExecutableName + "-SPAWN " + extraCommandLine;
            else
                process.StartInfo.Arguments = "c=16 /R " + "\"" + SafePath.CombineFilePath(ProgramConstants.GamePath, gameExecutableName) + "\" " + additionalExecutableName + "-SPAWN";

            startedExecutableName = ProgramConstants.QRES_EXECUTABLE;
        }
        else
        {
            string arguments;

            if (!string.IsNullOrWhiteSpace(extraCommandLine))
                arguments = " " + additionalExecutableName + "-SPAWN " + extraCommandLine;
            else
                arguments = additionalExecutableName + "-SPAWN";

            FileInfo gameFileInfo = SafePath.GetFile(ProgramConstants.GamePath, gameExecutableName);

            process = new Process();
            process.StartInfo.FileName = gameFileInfo.FullName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;

            startedExecutableName = gameFileInfo.Name;
        }

        process.EnableRaisingEvents = true;
        process.Exited += (sender, _) => OnProcessExited((Process)sender!, session);

        Logger.Log("Launch executable: " + process.StartInfo.FileName);
        Logger.Log("Launch arguments: " + process.StartInfo.Arguments);
        GameSessionManager.OnProcessStarting();

        try
        {
            process.Start();
            Logger.Log("GameProcessHost: Process started.");
        }
        catch (Exception ex)
        {
            Logger.Log("Error launching " + startedExecutableName + ": " + ex.ToString());
            OnProcessExited(process, session);

            return LaunchResult.Failed(LaunchFailureReason.ProcessStartFailed, startedExecutableName, ex.Message);
        }

        if (Environment.ProcessorCount > 1 && SingleCoreAffinity
            && (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux)))
        {
            process.ProcessorAffinity = (IntPtr)2;
        }

        GameSessionManager.OnGameStarted(session);

        Logger.Log("Waiting for qres.dat or " + gameExecutableName + " to exit.");

        return LaunchResult.Started(session);
    }

    private static void OnProcessExited(Process process, GameSession session)
    {
        Logger.Log("GameProcessHost: Process exited.");

        int? exitCode = null;

        try
        {
            exitCode = process.ExitCode;
        }
        catch (InvalidOperationException)
        {
            // The process never started.
        }

        process.Dispose();
        GameSessionManager.OnGameExited();
        session.End(exitCode);
    }
}
