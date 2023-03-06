// Copyright 2023 CnCNet
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.

namespace SecondStageUpdater;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Rampastring.Tools;

internal sealed class Program
{
    private const int MutexTimeoutInSeconds = 30;
    private const int MaxCopyAttempts = 5;
    private const int CopyRetryWaitMilliseconds = 500;

    private static readonly object consoleMessageLock = new();

    private static async Task Main(string[] args)
    {
        try
        {
            Write("CnCNet Client Second-Stage Updater", ConsoleColor.Green);
            Write(string.Empty);

            // e.g. clientogl.dll "C:\Game\"
            if (args.Length < 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]) || !SafePath.GetDirectory(args[1].Replace("\"", null, StringComparison.OrdinalIgnoreCase)).Exists)
            {
                Write("Invalid arguments given!", ConsoleColor.Red);
                Write("Usage: <client_executable_name> <base_directory>");
                Write(string.Empty);
                Exit(false);
            }
            else
            {
                FileInfo clientExecutable = SafePath.GetFile(args[0]);
                DirectoryInfo baseDirectory = SafePath.GetDirectory(args[1].Replace("\"", null, StringComparison.OrdinalIgnoreCase));
                DirectoryInfo resourceDirectory = SafePath.GetDirectory(baseDirectory.FullName, "Resources");

                Write("Base directory: " + baseDirectory.FullName);
                Write($"Waiting for the client ({clientExecutable.Name}) to exit..");

                string clientMutexId = FormattableString.Invariant($"Global{Guid.Parse("1CC9F8E7-9F69-4BBC-B045-E734204027A9")}");

                Mutex clientMutex = new(false, clientMutexId, out _);
                bool hasHandle;

                try
                {
                    hasHandle = clientMutex.WaitOne(TimeSpan.FromSeconds(MutexTimeoutInSeconds), false);
                }
                catch (AbandonedMutexException)
                {
                    hasHandle = true;
                }

                if (!hasHandle)
                {
                    Write($"Timeout while waiting for the client ({clientExecutable.Name}) to exit!", ConsoleColor.Red);
                    Exit(false);
                }

                clientMutex.ReleaseMutex();
                clientMutex.Dispose();

                // This is occasionally necessary to prevent DLLs from being locked at the time that this update is attempting to overwrite them
                await Task.Delay(1000).ConfigureAwait(false);

                DirectoryInfo updaterDirectory = SafePath.GetDirectory(baseDirectory.FullName, "Updater");

                if (!updaterDirectory.Exists)
                {
                    Write($"{updaterDirectory.Name} directory does not exist!", ConsoleColor.Red);
                    Exit(false);
                }

                Write("Updating files.", ConsoleColor.Green);

                IEnumerable<FileInfo> files = updaterDirectory.EnumerateFiles("*", SearchOption.AllDirectories);
                FileInfo executableFile = SafePath.GetFile(Assembly.GetExecutingAssembly().Location);
                FileInfo relativeExecutableFile = SafePath.GetFile(executableFile.FullName[baseDirectory.FullName.Length..]);
                const string versionFileName = "version";

                Write($"{nameof(SecondStageUpdater)}: {relativeExecutableFile}");

                var copyTasks = new List<Task>();
                var failedFiles = new List<FileInfo>();

                foreach (FileInfo fileInfo in files)
                {
                    FileInfo relativeFileInfo = SafePath.GetFile(fileInfo.FullName[updaterDirectory.FullName.Length..]);
                    AssemblyName[] assemblies = Assembly.LoadFrom(executableFile.FullName).GetReferencedAssemblies();

                    if (relativeFileInfo.ToString()[..^relativeFileInfo.Extension.Length].Equals(relativeExecutableFile.ToString()[..^relativeExecutableFile.Extension.Length], StringComparison.OrdinalIgnoreCase)
                        || relativeFileInfo.ToString()[..^relativeFileInfo.Extension.Length].Equals(SafePath.CombineFilePath("Resources", Path.GetFileNameWithoutExtension(relativeExecutableFile.Name)), StringComparison.OrdinalIgnoreCase))
                    {
                        Write($"Skipping {nameof(SecondStageUpdater)} file {relativeFileInfo}");
                    }
                    else if (assemblies.Any(q => relativeFileInfo.ToString()[..^relativeFileInfo.Extension.Length].Equals(q.Name, StringComparison.OrdinalIgnoreCase))
                        || assemblies.Any(q => relativeFileInfo.ToString()[..^relativeFileInfo.Extension.Length].Equals(SafePath.CombineFilePath("Resources", q.Name), StringComparison.OrdinalIgnoreCase)))
                    {
                        Write($"Skipping {nameof(SecondStageUpdater)} dependency {relativeFileInfo}");
                    }
                    else if (relativeFileInfo.ToString().Equals(versionFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        Write($"Skipping {relativeFileInfo}");
                    }
                    else
                    {
                        copyTasks.Add(CopyFileTaskAsync(baseDirectory, fileInfo, relativeFileInfo, failedFiles));
                    }
                }

                await Task.WhenAll(copyTasks.ToArray()).ConfigureAwait(false);

                if (failedFiles.Any())
                {
                    Write("Updating file(s) failed!", ConsoleColor.Yellow);
                    Write("If the problem persists, try to move the content of the \"Updater\" directory to the main directory manually or contact the staff for support.");
                    Exit(false);
                }

                FileInfo versionFile = SafePath.GetFile(updaterDirectory.FullName, versionFileName);

                if (versionFile.Exists)
                {
                    FileInfo destinationFile = SafePath.GetFile(baseDirectory.FullName, versionFile.Name);
                    FileInfo relativeFileInfo = SafePath.GetFile(destinationFile.FullName[baseDirectory.FullName.Length..]);

                    Write($"Updating {relativeFileInfo}");
                    versionFile.CopyTo(destinationFile.FullName, true);
                }

                Write("Files successfully updated. Starting launcher..", ConsoleColor.Green);
                string launcherExe = string.Empty;

                try
                {
                    Write("Checking ClientDefinitions.ini for launcher executable filename.");

                    string[] lines = await File.ReadAllLinesAsync(SafePath.CombineFilePath(resourceDirectory.FullName, "ClientDefinitions.ini")).ConfigureAwait(false);
                    string launcherPropertyName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "LauncherExe" : "UnixLauncherExe";
                    string line = lines.Single(q => q.Trim().StartsWith(launcherPropertyName, StringComparison.OrdinalIgnoreCase) && q.Contains('=', StringComparison.OrdinalIgnoreCase));
                    int commentStart = line.IndexOf(';', StringComparison.OrdinalIgnoreCase);

                    if (commentStart >= 0)
                        line = line[..commentStart];

                    launcherExe = line.Split('=')[1].Trim();
                }
                catch (Exception ex)
                {
                    Write($"Failed to read ClientDefinitions.ini: {ex}", ConsoleColor.Yellow);
                }

                FileInfo launcherExeFile = SafePath.GetFile(baseDirectory.FullName, launcherExe);

                if (launcherExeFile.Exists)
                {
                    Write("Launcher executable found: " + launcherExe, ConsoleColor.Green);

#pragma warning disable SA1312 // Variable names should begin with lower-case letter
                    using var _ = Process.Start(new ProcessStartInfo
                    {
                        FileName = launcherExeFile.FullName
                    });
#pragma warning restore SA1312 // Variable names should begin with lower-case letter
                }
                else
                {
                    Write("No suitable launcher executable found! Client will not automatically start after updater closes.", ConsoleColor.Yellow);
                    Exit(false);
                }
            }

            Exit(true);
        }
        catch (Exception ex)
        {
            Write("An error occurred during the Launcher Updater's operation.", ConsoleColor.Red);
            Write($"Returned error was: {ex}");
            Write(string.Empty);
            Write("If you were updating a game, please try again. If the problem continues, contact the staff for support.");
            Exit(false);
        }
    }

    /// <summary>
    /// This attempts to copy a file for the update with the ability to retry up to <see cref="MaxCopyAttempts"/> times.
    /// There are instances where DLLs or other files may be locked and are unable to be overwritten by the update.
    ///
    /// TODO:
    /// Make a backup of all files that are attempted. When we check for any failed files outside this function, restore all backups
    /// if any failures occurred. This will prevent the user from being in a partially updated state.
    ///
    /// </summary>
    /// <param name="baseDirectory">The absolute path of the game installation.</param>
    /// <param name="sourceFileInfo">The file to be copied.</param>
    /// <param name="relativeFileInfo">The relative file info for the destination of the file to be copied.</param>
    /// <param name="failedFiles">If the copy fails too many times, the file should be added to this list.</param>
    /// <returns>A Task.</returns>
    private static async Task CopyFileTaskAsync(DirectoryInfo baseDirectory, FileInfo sourceFileInfo, FileInfo relativeFileInfo, List<FileInfo> failedFiles)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                FileInfo destinationFile = SafePath.GetFile(baseDirectory.FullName, relativeFileInfo.ToString());
                FileStream sourceFileStream = sourceFileInfo.Open(new FileStreamOptions
                {
                    Access = FileAccess.Read,
                    Mode = FileMode.Open,
                    Options = FileOptions.Asynchronous,
                    Share = FileShare.None
                });
                await using (sourceFileStream.ConfigureAwait(false))
                {
                    FileStream destinationFileStream = destinationFile.Open(new FileStreamOptions
                    {
                        Access = FileAccess.Write,
                        Mode = FileMode.Create,
                        Options = FileOptions.Asynchronous,
                        Share = FileShare.None
                    });
                    await using (destinationFileStream.ConfigureAwait(false))
                    {
                        await sourceFileStream.CopyToAsync(destinationFileStream).ConfigureAwait(false);
                    }
                }

                Write($"Updated {relativeFileInfo}");

                // File was succesfully copied. Return from the function.
                return;
            }
            catch (IOException ex)
            {
                if (attempt >= MaxCopyAttempts)
                {
                    // We tried too many times and need to bail.
                    failedFiles.Add(sourceFileInfo);
                    Write($"Updating file failed too many times! Returned error message: {ex}", ConsoleColor.Yellow);
                    return;
                }

                // We failed to copy the file, but can try again.
                Write($"Updating file attempt {attempt} failed! Returned error message: {ex.Message}", ConsoleColor.Yellow);
                await Task.Delay(CopyRetryWaitMilliseconds).ConfigureAwait(false);
            }
        }
    }

    private static void Exit(bool success)
    {
        if (success)
            return;

        Write("Press any key to exit.");
        Console.ReadKey();
        Environment.Exit(1);
    }

    private static void Write(string text, ConsoleColor? color = null)
    {
        // This is necessary, because console is written to from the copy file task
        lock (consoleMessageLock)
        {
            Console.ForegroundColor = color ?? Console.ForegroundColor;
            Console.WriteLine(text);
            Console.ResetColor();
        }
    }
}