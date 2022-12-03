/*
Copyright 2022 CnCNet

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

namespace SecondStageUpdater;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Rampastring.Tools;

internal sealed class Program
{
    private static ConsoleColor defaultColor;

    private static async Task Main(string[] args)
    {
        defaultColor = Console.ForegroundColor;

        try
        {
            Write("CnCNet Client Second-Stage Updater", ConsoleColor.Green);
            Write(string.Empty);

            // e.g. clientogl.dll "C:\Game\"
            if (args.Length < 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]) || !SafePath.GetDirectory(args[1].Replace("\"", null)).Exists)
            {
                Write("Invalid arguments given!", ConsoleColor.Red);
                Write("Usage: <client_executable_name> <base_directory>");
                Write(string.Empty);
                Write("Press any key to exit.");
                Console.ReadKey();
                Environment.Exit(1);
            }
            else
            {
                FileInfo clientExecutable = SafePath.GetFile(args[0]);
                DirectoryInfo baseDirectory = SafePath.GetDirectory(args[1].Replace("\"", null));
                DirectoryInfo resourceDirectory = SafePath.GetDirectory(baseDirectory.FullName, "Resources");

                Write("Base directory: " + baseDirectory.FullName);
                Write("Waiting for the client (" + clientExecutable.Name + ") to exit..");

                string clientMutexId = FormattableString.Invariant($"Global{Guid.Parse("1CC9F8E7-9F69-4BBC-B045-E734204027A9")}");
                using var clientMutex = new Mutex(false, clientMutexId, out _);

                try
                {
                    clientMutex.WaitOne(-1, false);
                }
                catch (AbandonedMutexException)
                {
                }

                DirectoryInfo updaterDirectory = SafePath.GetDirectory(baseDirectory.FullName, "Updater");

                if (!updaterDirectory.Exists)
                {
                    Write($"{updaterDirectory.Name} directory does not exist!", ConsoleColor.Red);
                    Write("Press any key to exit.");
                    Console.ReadKey();
                    Environment.Exit(1);
                }

                Write("Updating files.", ConsoleColor.Green);

                IEnumerable<FileInfo> files = updaterDirectory.EnumerateFiles("*", SearchOption.AllDirectories);
                FileInfo executableFile = SafePath.GetFile(Assembly.GetExecutingAssembly().Location);
                FileInfo relativeExecutableFile = SafePath.GetFile(executableFile.FullName[baseDirectory.FullName.Length..]);
                const string versionFileName = "version";

                Write($"{nameof(SecondStageUpdater)}: {relativeExecutableFile}");

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
                        try
                        {
                            FileInfo copiedFile = SafePath.GetFile(baseDirectory.FullName, relativeFileInfo.ToString());

                            Write($"Updating {relativeFileInfo}");
                            fileInfo.CopyTo(copiedFile.FullName, true);
                        }
                        catch (Exception ex)
                        {
                            Write($"Updating file failed! Returned error message: {ex}", ConsoleColor.Yellow);
                            Write("Press any key to retry. If the problem persists, try to move the content of the \"Updater\" directory to the main directory manually or contact the staff for support.");
                            Console.ReadKey();
                            Environment.Exit(1);
                        }
                    }
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
                    Write("Checking ClientDefinitions.ini for launcher executable filename (LauncherExe).");

                    string[] lines = await File.ReadAllLinesAsync(SafePath.CombineFilePath(resourceDirectory.FullName, "ClientDefinitions.ini"));
                    string line = lines.Single(q => q.Trim().StartsWith("LauncherExe", StringComparison.OrdinalIgnoreCase) && q.Contains('='));
                    int commentStart = line.IndexOf(';');

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
                    Write("Press any key to exit.");
                    Console.ReadKey();
                    Environment.Exit(1);
                }
            }
        }
        catch (Exception ex)
        {
            Write("An error occured during the Launcher Updater's operation.", ConsoleColor.Red);
            Write($"Returned error was: {ex}");
            Write(string.Empty);
            Write("If you were updating a game, please try again. If the problem continues, contact the staff for support.");
            Write("Press any key to exit.");
            Console.ReadKey();
            Environment.Exit(1);
        }
    }

    private static void Write(string text)
    {
        Console.ForegroundColor = defaultColor;
        Console.WriteLine(text);
    }

    private static void Write(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = defaultColor;
    }
}