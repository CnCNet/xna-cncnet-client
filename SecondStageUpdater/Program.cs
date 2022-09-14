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
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace SecondStageUpdater
{
    internal class Program
    {
        private static ConsoleColor defaultColor = Console.ForegroundColor;

        private static void Main(string[] args)
        {
            Write("CnCNet Client Second-Stage Updater", ConsoleColor.Green);
            Write("");

            if (args.Length < 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]) || !Directory.Exists(args[1]))
            {
                Write("Invalid arguments given!", ConsoleColor.Red);
                Write("Usage: <client_executable_name> <base_directory>");
                Write("");
                Write("Press any key to exit.");
                Console.ReadKey();
            }
            else
            {
                string clientExecutable = args[0];
                string basePath = args[1].Replace("\\", "/");

                if (!basePath.EndsWith("/"))
                    basePath += "/";

                string resourcePath = basePath + "Resources/";
                string imagePath = resourcePath + "launcherupdater.png";

                MainForm mainForm = null;

                if (File.Exists(imagePath))
                {
                    mainForm = new MainForm(imagePath);
                    mainForm.Show();
                    Application.DoEvents();
                }

                try
                {
                    Write("Base directory: " + basePath);
                    Write("Waiting for the client (" + clientExecutable + ") to exit..");
                    while (Process.GetProcessesByName(clientExecutable.Remove(clientExecutable.Length - 4)).GetLength(0) != 0) ;

                    if (!Directory.Exists(basePath + "Updater"))
                    {
                        Write("\"Updater\" directory does not exist!", ConsoleColor.Red);
                        Write("Press any key to exit.");
                        Console.ReadKey();
                        return;
                    }

                    Write("Updating files.", ConsoleColor.Green);

                    string[] paths = Directory.GetFiles(basePath + "Updater", "*", SearchOption.AllDirectories);
                    string executablePath = new Uri(System.Reflection.Assembly.GetExecutingAssembly().Location).LocalPath;
                    executablePath = executablePath.Substring(basePath.Length).Replace("\\", "/");

                    for (int index = 0; index < paths.Length; ++index)
                    {
                        string filename = paths[index].Substring(basePath.Length + 8).Replace("\\", "/");

                        Write(executablePath);
                        Write(filename);

                        if (executablePath.Equals(filename) || filename.Equals("Resources/SecondStageUpdater.exe"))
                            Write($"Skipping { Path.GetFileName(filename) }");
                        else if (filename == "version")
                            Write("Skipping version");
                        else
                        {
                            try
                            {
                                Write("Updating " + filename);
                                Write(basePath + "Updater/" + filename + " -> " + basePath + filename);
                                File.Copy(basePath + "Updater/" + filename, basePath + filename, true);
                            }
                            catch (Exception ex)
                            {
                                Write("Updating file failed! Returned error message: " + ex.Message, ConsoleColor.Yellow);
                                Write("Press any key to retry. If the problem persists, try to move the content of the \"Updater\" directory to the main directory manually or contact the staff for support.");
                                --index;
                                Console.ReadKey();
                            }
                        }
                    }

                    if (File.Exists(basePath + "Updater/version"))
                    {
                        Write(basePath + "Updater/version -> " + basePath + "version");
                        File.Copy(basePath + "Updater/version", basePath + "version", true);
                    }

                    Write("Files succesfully updated. Starting launcher..", ConsoleColor.Green);
                    Process process = new Process();
                    process.StartInfo.UseShellExecute = false;
                    string launcherExe = "";

                    try
                    {
                        Write("Checking ClientDefinitions.ini for launcher executable filename (LauncherExe).");
                        string[] lines = File.ReadAllLines(resourcePath + "ClientDefinitions.ini");
                        foreach (string line in lines)
                        {
                            if (line.Trim().StartsWith("LauncherExe") && line.Contains("="))
                            {
                                string lineModified = line;
                                int commentStart = lineModified.IndexOf(';');

                                if (commentStart >= 0)
                                    lineModified = lineModified.Substring(0, commentStart);

                                launcherExe = lineModified.Split('=')[1].Trim();
                                break;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        Write("Failed to read ClientDefinitions.ini.", ConsoleColor.Yellow);
                    }

                    if (File.Exists(basePath + launcherExe))
                    {
                        Write("Launcher executable found: " + launcherExe, ConsoleColor.Green);
                        process.StartInfo.FileName = basePath + launcherExe;
                        process.Start();
                    }
                    else
                    {
                        Write("No suitable launcher executable found! Client will not automatically start after updater closes.", ConsoleColor.Yellow);
                        Write("Press any key to exit.");
                        Console.ReadKey();
                    }

                    if (mainForm != null)
                        mainForm.Hide();

                    Environment.Exit(0);
                }
                catch (Exception ex)
                {

                    if (mainForm != null)
                        mainForm.Hide();

                    MessageBox.Show("An error occured during the updater's operation. Returned error was: " + ex.Message + Environment.NewLine + Environment.NewLine + "If you were updating a game, please try again. If the problem continues, contact the staff for support. Press OK to exit.", "Update failed");
                    Write("An error occured during the Launcher Updater's operation.", ConsoleColor.Red);
                    Write("Returned error was: " + ex.Message);
                    Write("");
                    Write("If you were updating a game, please try again. If the problem continues, contact the staff for support.");
                }
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
