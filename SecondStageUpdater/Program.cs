// Copyright 2022-2025 CnCNet
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

#pragma warning disable IDE0057 // Use range operator

namespace SecondStageUpdater;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

using Rampastring.Tools;

internal sealed class Program
{
    private const int MutexTimeoutInSeconds = 30;

    private static ConsoleColor defaultColor;
    private static bool hasHandle;
    private static Mutex clientMutex;

    // e.g. args = ["clientogl.dll", "\"C:\\Game\\\""];
    private static void Main(string[] args)
    {
        defaultColor = Console.ForegroundColor;

        try
        {
            Write("CnCNet Client Second-Stage Updater", true, ConsoleColor.Green);
            Write(string.Empty);

            if (args.Length < 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]))
            {
                Write("Invalid arguments given!", true, ConsoleColor.Red);
                Write("Usage: <client_executable_name> <base_directory>");
                Write(string.Empty);
                Exit(false);
            }

            DirectoryInfo baseDirectory = SafePath.GetDirectory(args[1].Replace("\"", null));

            if (!baseDirectory.Exists)
            {
                Write("Base directory does not exist!", true, ConsoleColor.Red);
                Write(baseDirectory.FullName);
                Write(string.Empty);
                Exit(false);
            }
            else
            {
                string clientExecutable = args[0];
                DirectoryInfo resourceDirectory = SafePath.GetDirectory(baseDirectory.FullName, "Resources");
                FileInfo logFile = SafePath.GetFile(SafePath.CombineFilePath(baseDirectory.FullName, "Client", "SecondStageUpdater.log"));

                if (logFile.Exists)
                    logFile.Delete();

                Logger.Initialize(logFile.DirectoryName, logFile.Name);
                Logger.WriteLogFile = true;
                Logger.WriteToConsole = false;
                Logger.Log("CnCNet Client Second-Stage Updater");
                Logger.Log("Version: " + GitVersionInformation.AssemblySemVer);
                Write("Base directory: " + baseDirectory.FullName);
                Write($"Waiting for the client ({clientExecutable}) to exit..");

                // note: the GUID should be consistent with the one in xna-cncnet-client/DXMainClient/Program.cs
                string clientMutexId = FormattableString.Invariant($"Global{Guid.Parse("1CC9F8E7-9F69-4BBC-B045-E734204027A9")}");

                clientMutex = new(false, clientMutexId, out _);

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
                    Write($"Timeout while waiting for the client ({clientExecutable}) to exit!", true, ConsoleColor.Red);
                    Exit(false);
                }

                // This is occasionally necessary to prevent DLLs from being locked at the time that this update is attempting to overwrite them
                Thread.Sleep(1000);

                DirectoryInfo updaterDirectory = SafePath.GetDirectory(baseDirectory.FullName, "Updater");

                if (!updaterDirectory.Exists)
                {
                    Write($"{updaterDirectory.Name} directory does not exist!", true, ConsoleColor.Red);
                    Exit(false);
                }

                Write("Updating files.", true, ConsoleColor.Green);

                IEnumerable<FileInfo> files = updaterDirectory.EnumerateFiles("*", SearchOption.AllDirectories);
                FileInfo executableFile = SafePath.GetFile(Assembly.GetExecutingAssembly().Location);
                FileInfo relativeExecutableFile = SafePath.GetFile(executableFile.FullName.Substring(baseDirectory.FullName.Length));

                const string versionFileName = "version";

                Write($"{nameof(SecondStageUpdater)}: {relativeExecutableFile}");

                AssemblyName[] assemblies = Assembly.LoadFrom(executableFile.FullName).GetReferencedAssemblies();

                foreach (FileInfo fileInfo in files)
                {
                    string relativeFileName = fileInfo.FullName.Substring(updaterDirectory.FullName.Length);
                    string fileExtension = fileInfo.Extension;
                    string relativeFileNameWithoutExtension = relativeFileName.Substring(0, relativeFileName.Length - fileExtension.Length);

                    string relativeExecutableFileFullName = relativeExecutableFile.FullName;
                    string relativeExecutableFileExtension = relativeExecutableFile.Extension;
                    string relativeExecutableFileFullNameWithoutExtension = relativeExecutableFileFullName.Substring(0, relativeExecutableFileFullName.Length - relativeExecutableFileExtension.Length);

                    if (relativeFileNameWithoutExtension.Equals(relativeExecutableFileFullNameWithoutExtension, StringComparison.OrdinalIgnoreCase)
                        || relativeFileNameWithoutExtension.Equals(SafePath.CombineFilePath("Resources", Path.GetFileNameWithoutExtension(relativeExecutableFile.Name)), StringComparison.OrdinalIgnoreCase))
                    {
                        Write($"Skipping {nameof(SecondStageUpdater)} file {relativeFileName}");
                    }
                    else if (assemblies.Any(q => relativeFileNameWithoutExtension.Equals(q.Name, StringComparison.OrdinalIgnoreCase))
                        || assemblies.Any(q => relativeFileNameWithoutExtension.Equals(SafePath.CombineFilePath("Resources", q.Name), StringComparison.OrdinalIgnoreCase)))
                    {
                        Write($"Skipping {nameof(SecondStageUpdater)} dependency {relativeFileName}");
                    }
                    else if (relativeFileName.Equals(versionFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        Write($"Skipping {relativeFileName}");
                    }
                    else
                    {
                        try
                        {
                            FileInfo copiedFile = SafePath.GetFile(baseDirectory.FullName, relativeFileName);

                            Write($"Updating {relativeFileName}");

                            // If the file is read-only, we need to remove the read-only attribute before copying it
                            if (copiedFile.Exists && copiedFile.IsReadOnly)
                            {
                                copiedFile.IsReadOnly = false;
                                fileInfo.CopyTo(copiedFile.FullName, true);
                                copiedFile.IsReadOnly = true;
                            }
                            else
                            {
                                fileInfo.CopyTo(copiedFile.FullName, true);
                            }
                        }
                        catch (Exception ex)
                        {
                            Write($"Updating file failed! Returned error message: {ex}", true, ConsoleColor.Yellow);
                            Write("If the problem persists, try to move the content of the \"Updater\" directory to the main directory manually or contact the staff for support.");
                            Exit(false);
                        }
                    }
                }

                FileInfo versionFile = SafePath.GetFile(updaterDirectory.FullName, versionFileName);

                if (versionFile.Exists)
                {
                    FileInfo destinationFile = SafePath.GetFile(baseDirectory.FullName, versionFile.Name);
                    FileInfo relativeFileInfo = SafePath.GetFile(destinationFile.FullName.Substring(baseDirectory.FullName.Length));

                    Write($"Updating {relativeFileInfo}");
                    versionFile.CopyTo(destinationFile.FullName, true);
                }

                Write("Files successfully updated. Starting launcher..", true, ConsoleColor.Green);
                string launcherExe = string.Empty;

                try
                {
                    Write("Checking ClientDefinitions.ini for launcher executable filename.");

                    string[] lines = File.ReadAllLines(SafePath.CombineFilePath(resourceDirectory.FullName, "ClientDefinitions.ini"));
                    string launcherPropertyName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "LauncherExe" : "UnixLauncherExe";
                    string line = lines.Single(q => q.Trim().StartsWith(launcherPropertyName, StringComparison.OrdinalIgnoreCase) && q.Contains('='));
                    int commentStart = line.IndexOf(";", StringComparison.OrdinalIgnoreCase);

                    if (commentStart >= 0)
                        line = line.Substring(0, commentStart);

                    launcherExe = line.Split('=')[1].Trim();
                }
                catch (Exception ex)
                {
                    Write($"Failed to read ClientDefinitions.ini: {ex}", true, ConsoleColor.Yellow);
                }

                FileInfo architectureLauncherExeFile = SafePath.GetFile(resourceDirectory.FullName, "Launcher", FormattableString.Invariant($"{Path.GetFileNameWithoutExtension(launcherExe)}-{RuntimeInformation.OSArchitecture}{Path.GetExtension(launcherExe)}"));
                FileInfo launcherExeFile = SafePath.GetFile(baseDirectory.FullName, launcherExe);

                if (architectureLauncherExeFile.Exists)
                {
                    architectureLauncherExeFile.CopyTo(launcherExeFile.FullName, true);
                    launcherExeFile.Refresh();
                }

                if (launcherExeFile.Exists)
                {
                    Write("Launcher executable found: " + launcherExe, true, ConsoleColor.Green);

                    using var _ = Process.Start(new ProcessStartInfo
                    {
                        FileName = launcherExeFile.FullName
                    });
                }
                else
                {
                    Write("No suitable launcher executable found! Client will not automatically start after updater closes.", true, ConsoleColor.Yellow);
                    Exit(false);
                }
            }

            Exit(true);
        }
        catch (Exception ex)
        {
            Write("An error occurred during the Launcher Updater's operation.", true, ConsoleColor.Red);
            Write($"Returned error was: {ex}");
            Write(string.Empty);
            Write("If you were updating a game, please try again. If the problem continues, contact the staff for support.");
            Exit(false);
        }
    }

    private static void Exit(bool success)
    {
        if (hasHandle)
        {
            clientMutex.ReleaseMutex();
            clientMutex.Dispose();
        }

        if (!success)
        {
            Write("Press any key to exit.");
            Console.ReadKey();
            Environment.Exit(1);
        }
    }

    private static void Write(string text, bool logToFile = true, ConsoleColor? color = null)
    {
        Console.ForegroundColor = color ?? defaultColor;
        Console.WriteLine(text);

        if (logToFile)
            Logger.Log(text);
    }
}