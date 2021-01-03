﻿using ClientCore;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;

namespace DTAClient.Domain
{
    /// <summary>
    /// A single-player saved game.
    /// </summary>
    public class SavedGame
    {
        const string SAVED_GAME_PATH = "Saved Games/";

        public SavedGame(string fileName)
        {
            FileName = fileName;
        }

        public string FileName { get; private set; }
        public string GUIName { get; private set; }
        public DateTime LastModified { get; private set; }

        /// <summary>
        /// Reads and sets the saved game's name and last modified date, and returns true if succesful.
        /// </summary>
        /// <returns>True if parsing the info was succesful, otherwise false.</returns>
        public bool ParseInfo()
        {
            try
            {
                using (var fs = File.Open(ProgramConstants.GamePath + SAVED_GAME_PATH + FileName, FileMode.Open, FileAccess.Read))
                    GUIName = SavedGameManager.GetSaveGameName(fs);

                LastModified = File.GetLastWriteTime(ProgramConstants.GamePath + SAVED_GAME_PATH + FileName);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log("An error occured while parsing saved game " + FileName + ":" +
                    ex.Message);
                return false;
            }
        }
    }
}
