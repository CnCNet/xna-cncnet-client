using System;
using System.Collections.Generic;
using System.IO;
using Rampastring.Tools;

namespace ClientCore
{
    public static class SavedGameManager
    {
        private const string SAVED_GAMES_DIRECTORY = "Saved Games";
        private const int MAX_SAVED_GAMES = 1000;

        private static bool saveRenameInProgress = false;

        public static int GetSaveGameCount()
        {
            string saveGameDirectory = GetSaveGameDirectoryPath();

            if (!AreSavedGamesAvailable())
                return 0;

            for (int i = 0; i < MAX_SAVED_GAMES; i++)
            {
                if (!SafePath.GetFile(saveGameDirectory, $"SVGM_{i:D3}.NET").Exists)
                {
                    return i;
                }
            }

            return MAX_SAVED_GAMES;
        }

        public static List<string> GetSaveGameTimestamps()
        {
            int saveGameCount = GetSaveGameCount();

            List<string> timestamps = new List<string>();

            string saveGameDirectory = GetSaveGameDirectoryPath();

            for (int i = 0; i < saveGameCount; i++)
            {
                FileInfo sgFile = SafePath.GetFile(saveGameDirectory, $"SVGM_{i:D3}.NET");

                DateTime dt = sgFile.LastWriteTime;

                timestamps.Add(dt.ToString());
            }

            return timestamps;
        }

        public static bool AreSavedGamesAvailable()
        {
            return Directory.Exists(GetSaveGameDirectoryPath());
        }

        private static string GetSaveGameDirectoryPath()
        {
            return SafePath.CombineDirectoryPath(ProgramConstants.GamePath, SAVED_GAMES_DIRECTORY);
        }

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

            for (int i = 0; i < MAX_SAVED_GAMES; i++)
            {
                if (!SafePath.GetFile(saveGameDirectory, $"SVGM_{i:D3}.NET").Exists)
                {
                    saveGameId = i;
                    break;
                }
            }

            if (saveGameId == (MAX_SAVED_GAMES - 1))
            {
                if (SafePath.GetFile(saveGameDirectory, $"SVGM_{MAX_SAVED_GAMES - 1:D3}.NET").Exists)
                    Logger.Log($"{MAX_SAVED_GAMES} saved games exceeded! Overwriting previous MP save.");
            }

            string sgPath = SafePath.CombineFilePath(saveGameDirectory, $"SVGM_{saveGameId:D3}.NET");

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
                for (int i = 0; i < MAX_SAVED_GAMES; i++)
                {
                    SafePath.DeleteFileIfExists(GetSaveGameDirectoryPath(), $"SVGM_{i:D3}.NET");
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