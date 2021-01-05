using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Rampastring.Tools;

namespace ClientCore
{
    /// <summary>
    /// A class for handling saved multiplayer games.
    /// </summary>
    public static class SavedGameManager
    {
        private const string SAVED_GAMES_DIRECTORY = "Saved Games";
        private static readonly Mutex saveRenameProcess = new Mutex();

        /// <summary>
        /// Get Archive Name
        /// </summary>
        /// <param name="stream"> FileStream </param>
        /// <returns> Archive Name </returns>
        public static string GetSaveGameName(Stream stream)
        {
            var buffer = new byte[4];
            stream.Seek(0x08CC, SeekOrigin.Begin);
            stream.Read(buffer, 0, 4);// Get Name Length.

            if (!BitConverter.IsLittleEndian)
                Array.Reverse(buffer);
            buffer = new byte[(BitConverter.ToInt32(buffer, 0) - 1) << 1];

            stream.Read(buffer, 0, buffer.Length);// Get Name.
            var result = System.Text.Encoding.Unicode.GetString(buffer);
            Logger.Log($"[SavedGameManager]\tFound Archive: {result}");
            return result;
        }

        public static int GetSaveGameCount() => AreSavedGamesAvailable()
            ? new DirectoryInfo(SaveGameDirectoryPath).GetFiles("SVGM_???.NET").Length : 0;

        public static List<string> GetSaveGameTimestamps()
            => new DirectoryInfo(SaveGameDirectoryPath)
                .GetFiles("SVGM_???.NET")
                .Select(i => i.LastWriteTime.ToString())
                .ToList();

        public static bool AreSavedGamesAvailable() => Directory.Exists(SaveGameDirectoryPath);

        private static string SaveGameDirectoryPath // Use System.IO.Path Member and NOT use character '/' or '\\'
            => Path.Combine(ProgramConstants.GamePath, SAVED_GAMES_DIRECTORY);

        /// <summary>
        /// Initializes saved MP games for a match.
        /// </summary>
        public static bool InitSavedGames()
        {
            if (!EraseSavedGames())
                return false;

            try
            {
                Logger.Log("Writing spawn.ini for saved game.");
                File.Delete(Path.Combine(SaveGameDirectoryPath, "spawnSG.ini"));
                File.Copy(Path.Combine(ProgramConstants.GamePath, "spawn.ini"), Path.Combine(SaveGameDirectoryPath, "spawnSG.ini"));
            }
            catch (Exception ex)
            {
                Logger.Log("Writing spawn.ini for saved game failed! Exception message: " + ex.Message);
                return false;
            }

            return true;
        }

        public static void RenameSavedGame()
        {
            Logger.Log("Renaming saved game.");

            if (saveRenameProcess.WaitOne(1))
            {
                var file = new FileInfo(Path.Combine(SaveGameDirectoryPath, "SAVEGAME.NET"));

                if (!file.Exists)
                {
                    Logger.Log("SAVEGAME.NET doesn't exist!");
                    return;
                }

                try
                {
                    int saveGameId;
                    for (saveGameId = 0; saveGameId < 1000; saveGameId++)
                    {
                        if (!File.Exists(Path.Combine(SaveGameDirectoryPath, $"SVGM_{saveGameId:D3}.NET")))
                            break;
                    }

                    if (saveGameId == 999
                        && File.Exists(Path.Combine(SaveGameDirectoryPath, "SVGM_999.NET")))
                    {
                        Logger.Log("1000 saved games exceeded! Overwriting previous MP save.");
                    }

                    var sgPath = Path.Combine(SaveGameDirectoryPath, $"SVGM_{saveGameId:D3}.NET");

                    int tryCount = 0;

                    while (true)
                    {
                        try
                        {
                            file.MoveTo(sgPath);
                            break;
                        }
                        catch (Exception ex)
                        {
                            Logger.Log("Renaming saved game failed! Exception message: " + ex.Message);
                        }

                        tryCount++;

                        if (tryCount > 40)
                        {
                            Logger.Log("Renaming saved game failed 40 times! Aborting.");
                            return;
                        }

                        Thread.Sleep(250);
                    }

                    Logger.Log("Saved game SAVEGAME.NET succesfully renamed to " + Path.GetFileName(sgPath));
                }
                finally
                {
                    saveRenameProcess.ReleaseMutex();
                }
            }
            else
            {
                Logger.Log("Save renaming in progress!");
                return;
            }
        }

        public static bool EraseSavedGames()
        {
            Logger.Log("Erasing previous MP saved games.");

            try
            {
                Parallel.ForEach(new DirectoryInfo(SaveGameDirectoryPath).GetFiles("SVGM_???.NET").Select(i => i.FullName), File.Delete);
            }
            catch (Exception ex)
            {
                Logger.Log("Erasing previous MP saved games failed! Exception message: " + ex.Message);
                return false;
            }

            Logger.Log("MP saved games succesfully erased.");
            return true;
        }
    }
}
