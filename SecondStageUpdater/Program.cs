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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
#if WINDOWS || NETFRAMEWORK
using System.Windows.Forms;
#endif
using Rampastring.Tools;

namespace SecondStageUpdater
{
    internal sealed class Program
    {
        private static ConsoleColor defaultColor = Console.ForegroundColor;

        private static async Task Main(string[] args)
        {
            try
            {
                Write("CnCNet Client Second-Stage Updater", ConsoleColor.Green);
                Write("");

                if (args.Length < 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]) || !SafePath.GetDirectory(args[1].Replace("\"", null)).Exists)
                {
                    Write("Invalid arguments given!", ConsoleColor.Red);
                    Write("Usage: <client_executable_name> <base_directory>");
                    Write("");
                    Write("Press any key to exit.");
                    Console.ReadKey();
                    Environment.Exit(1);
                }
                else
                {
                    FileInfo clientExecutable = SafePath.GetFile(args[0]);
                    DirectoryInfo baseDirectory = SafePath.GetDirectory(args[1].Replace("\"", null));
                    DirectoryInfo resourceDirectory = SafePath.GetDirectory(baseDirectory.FullName, "Resources");
#if WINDOWS || NETFRAMEWORK

#if NETFRAMEWORK
                    Application.EnableVisualStyles();
#else
                    ApplicationConfiguration.Initialize();
#endif

                    FileInfo imageFile = SafePath.GetFile(resourceDirectory.FullName, "launcherupdater.png");
                    MainForm mainForm = null;

                    if (imageFile.Exists)
                    {
                        mainForm = new MainForm(imageFile.FullName);
                        mainForm.Show();
                        Application.DoEvents();
                    }
#endif

                    try
                    {
                        Write("Base directory: " + baseDirectory.FullName);
                        Write("Waiting for the client (" + clientExecutable.Name + ") to exit..");

                        while (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(clientExecutable.Name)).Any())
                            await Task.Delay(10);

                        DirectoryInfo updaterDirectory = SafePath.GetDirectory(baseDirectory.FullName, "Updater");

                        if (!updaterDirectory.Exists)
                        {
                            Write($"{updaterDirectory.FullName} directory does not exist!", ConsoleColor.Red);
                            Write("Press any key to exit.");
                            Console.ReadKey();
                        }

                        Write("Updating files.", ConsoleColor.Green);

                        List<FileInfo> files = updaterDirectory.EnumerateFiles("*", SearchOption.AllDirectories).ToList();
                        FileInfo executableFile = SafePath.GetFile(Assembly.GetExecutingAssembly().Location);

                        foreach (FileInfo fileInfo in files)
                        {
                            Write(executableFile.FullName);
                            Write(fileInfo.FullName);

                            if (Path.GetFileNameWithoutExtension(fileInfo.Name).Equals(Path.GetFileNameWithoutExtension(executableFile.Name)))
                                Write($"Skipping {fileInfo.FullName}");
                            else if (fileInfo.Name == "version")
                                Write("Skipping version");
                            else
                            {
                                try
                                {
                                    FileInfo copiedFile = SafePath.GetFile(baseDirectory.FullName, fileInfo.Name);

                                    Write("Updating " + fileInfo.FullName);
                                    Write(fileInfo.FullName + " -> " + copiedFile.FullName);
                                    fileInfo.CopyTo(copiedFile.FullName, true);
                                }
                                catch (Exception ex)
                                {
                                    Write("Updating file failed! Returned error message: " + ex.Message, ConsoleColor.Yellow);
                                    Write("Press any key to retry. If the problem persists, try to move the content of the \"Updater\" directory to the main directory manually or contact the staff for support.");
                                    Console.ReadKey();
                                    Environment.Exit(1);
                                }
                            }
                        }

                        FileInfo versionFile = SafePath.GetFile(updaterDirectory.FullName, "version");

                        if (versionFile.Exists)
                        {
                            Write(versionFile.FullName);
                            versionFile.CopyTo(SafePath.CombineFilePath(baseDirectory.FullName, versionFile.Name), true);
                        }

                        Write("Files succesfully updated. Starting launcher..", ConsoleColor.Green);
                        string launcherExe = "";

                        try
                        {
                            Write("Checking ClientDefinitions.ini for launcher executable filename (LauncherExe).");
#if NETFRAMEWORK
                            string[] lines = File.ReadAllLines(SafePath.CombineFilePath(resourceDirectory.FullName, "ClientDefinitions.ini"));
#else
                            string[] lines = await File.ReadAllLinesAsync(SafePath.CombineFilePath(resourceDirectory.FullName, "ClientDefinitions.ini"));
#endif
                            string line = lines.Single(q => q.Trim().StartsWith("LauncherExe") && q.Contains("="));
                            int commentStart = line.IndexOf(';');

                            if (commentStart >= 0)
                                line = line.Substring(0, commentStart);

                            launcherExe = line.Split('=')[1].Trim();
                        }
                        catch (Exception ex)
                        {
                            Write($"Failed to read ClientDefinitions.ini: {ex.Message}", ConsoleColor.Yellow);
                        }

                        FileInfo launcherExeFile = SafePath.GetFile(baseDirectory.FullName, launcherExe);

                        if (launcherExeFile.Exists)
                        {
                            Write("Launcher executable found: " + launcherExe, ConsoleColor.Green);

                            using var _ = Process.Start(new ProcessStartInfo
                            {
                                UseShellExecute = false,
                                FileName = launcherExeFile.FullName
                            });
                        }
                        else
                        {
                            Write("No suitable launcher executable found! Client will not automatically start after updater closes.", ConsoleColor.Yellow);
                            Write("Press any key to exit.");
                            Console.ReadKey();
                            Environment.Exit(1);
                        }

                        Environment.Exit(0);
                    }
                    catch (Exception ex)
                    {
                        Write("An error occured during the Launcher Updater's operation.", ConsoleColor.Red);
                        Write("Returned error was: " + ex.Message);
                        Write("");
                        Write("If you were updating a game, please try again. If the problem continues, contact the staff for support.");
#if WINDOWS || NETFRAMEWORK
                        mainForm?.Hide();
                        MessageBox.Show("An error occured during the updater's operation. Returned error was: " + ex.Message + Environment.NewLine + Environment.NewLine + "If you were updating a game, please try again. If the problem continues, contact the staff for support. Press OK to exit.", "Update failed");
#else
                        Write("Press any key to exit.");
                        Console.ReadKey();
#endif
                    }
                }
            }
            catch (Exception ex)
            {
                Write("Error: " + ex);
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
}