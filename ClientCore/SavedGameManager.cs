using System;
using System.Collections.Generic;
using System.IO;
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

            for (int i = 0; i < 1000; i++)
            {
                if (!SafePath.GetFile(saveGameDirectory, string.Format("SVGM_{0}.NET", i.ToString("D3"))).Exists)
                {
                    return i;
                }
            }

            return 1000;
        }

        public static List<string> GetSaveGameTimestamps()
        {
            int saveGameCount = GetSaveGameCount();

            List<string> timestamps = new List<string>();

            string saveGameDirectory = GetSaveGameDirectoryPath();

            for (int i = 0; i < saveGameCount; i++)
            {
                FileInfo sgFile = SafePath.GetFile(saveGameDirectory, string.Format("SVGM_{0}.NET", i.ToString("D3")));

                DateTime dt = sgFile.LastWriteTime;

                timestamps.Add(dt.ToString());
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

            int saveGameId = 0;

            for (int i = 0; i < 1000; i++)
            {
                if (!SafePath.GetFile(saveGameDirectory, string.Format("SVGM_{0}.NET", i.ToString("D3"))).Exists)
                {
                    saveGameId = i;
                    break;
                }
            }

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
                for (int i = 0; i < 1000; i++)
                {
                    SafePath.DeleteFileIfExists(GetSaveGameDirectoryPath(), string.Format("SVGM_{0}.NET", i.ToString("D3")));
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
