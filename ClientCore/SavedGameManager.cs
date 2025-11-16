using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rampastring.Tools;

namespace ClientCore
{
    /// <summary>
    /// A class for handling saved multiplayer games.
    /// </summary>
    public static class SavedGameManager
    {
        private const string SAVED_GAMES_DIRECTORY = "Saved Games";

        private static bool saveRenameInProgress = false;

        public static int GetSaveGameCount()
        {
            string saveGameDirectory = GetSaveGameDirectoryPath();

            if (!AreSavedGamesAvailable())
                return 0;

            try
            {
                var files = Directory.EnumerateFiles(saveGameDirectory, "SVGM_*.NET");
                int maxIndex = -1;

                foreach (var file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    if (fileName.Length >= 5 && fileName.StartsWith("SVGM_"))
                    {
                        string indexStr = fileName.Substring(5);
                        if (int.TryParse(indexStr, out int index))
                        {
                            if (index > maxIndex)
                                maxIndex = index;
                        }
                    }
                }

                return maxIndex + 1;
            }
            catch
            {
                return 0;
            }
        }

        public static List<string> GetSaveGameTimestamps()
        {
            string saveGameDirectory = GetSaveGameDirectoryPath();

            if (!AreSavedGamesAvailable())
                return new List<string>();

            var timestamps = new List<string>();

            try
            {
                var files = Directory.EnumerateFiles(saveGameDirectory, "SVGM_*.NET")
                    .OrderBy(f => f);

                foreach (var file in files)
                {
                    FileInfo sgFile = new FileInfo(file);
                    timestamps.Add(sgFile.LastWriteTime.ToString());
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error getting save game timestamps: {ex.Message}");
            }

            return timestamps;
        }

        public static bool AreSavedGamesAvailable()
        {
            if (Directory.Exists(GetSaveGameDirectoryPath()))
                return true;

            return false;
        }

        private static string GetSaveGameDirectoryPath()
        {
            return SafePath.CombineDirectoryPath(ProgramConstants.GamePath, SAVED_GAMES_DIRECTORY);
        }

        /// <summary>
        /// Initializes saved MP games for a match.
        /// </summary>
        public static bool InitSavedGames()
        {
            bool success = EraseSavedGames();

            if (!success)
                return false;

            try
            {
                Logger.Log("Writing spawn.ini for saved game.");
                SafePath.DeleteFileIfExists(ProgramConstants.GamePath, SAVED_GAMES_DIRECTORY, "spawnSG.ini");
                File.Copy(SafePath.CombineFilePath(ProgramConstants.GamePath, "spawn.ini"), SafePath.CombineFilePath(ProgramConstants.GamePath, SAVED_GAMES_DIRECTORY, "spawnSG.ini"));
            }
            catch (Exception ex)
            {
                Logger.Log("Writing spawn.ini for saved game failed! Exception message: " + ex.ToString());
                return false;
            }

            return true;
        }

        public static void RenameSavedGame()
        {
            Logger.Log("Renaming saved game.");

            if (saveRenameInProgress)
            {
                Logger.Log("Save renaming in progress!");
                return;
            }

            string saveGameDirectory = GetSaveGameDirectoryPath();

            if (!SafePath.GetFile(saveGameDirectory, "SAVEGAME.NET").Exists)
            {
                Logger.Log("SAVEGAME.NET doesn't exist!");
                return;
            }

            saveRenameInProgress = true;

            int saveGameId = GetSaveGameCount();

            if (saveGameId >= 1000)
                saveGameId = 999;

            if (saveGameId == 999)
            {
                if (SafePath.GetFile(saveGameDirectory, "SVGM_999.NET").Exists)
                    Logger.Log("1000 saved games exceeded! Overwriting previous MP save.");
            }

            string sgPath = SafePath.CombineFilePath(saveGameDirectory, string.Format("SVGM_{0}.NET", saveGameId.ToString("D3")));

            int tryCount = 0;

            while (true)
            {
                try
                {
                    File.Move(SafePath.CombineFilePath(saveGameDirectory, "SAVEGAME.NET"), sgPath);
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Log("Renaming saved game failed! Exception message: " + ex.ToString());
                }

                tryCount++;

                if (tryCount > 40)
                {
                    Logger.Log("Renaming saved game failed 40 times! Aborting.");
                    return;
                }

                System.Threading.Thread.Sleep(250);
            }

            saveRenameInProgress = false;

            Logger.Log("Saved game SAVEGAME.NET succesfully renamed to " + Path.GetFileName(sgPath));
        }

        public static bool EraseSavedGames()
        {
            Logger.Log("Erasing previous MP saved games.");

            try
            {
                string saveGameDirectory = GetSaveGameDirectoryPath();
                
                if (!Directory.Exists(saveGameDirectory))
                    return true;

                var files = Directory.EnumerateFiles(saveGameDirectory, "SVGM_*.NET");
                
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Failed to delete {Path.GetFileName(file)}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Erasing previous MP saved games failed! Exception message: " + ex.ToString());
                return false;
            }

            Logger.Log("MP saved games succesfully erased.");
            return true;
        }
    }
}
