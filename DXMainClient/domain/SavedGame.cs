using ClientCore;
using Rampastring.Tools;
using System;
using System.IO;

namespace DTAClient.Domain
{
    /// <summary>
    /// A single-player saved game.
    /// </summary>
    public class SavedGame
    {
        const string SAVED_GAME_PATH = "Saved Games\\";

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
                using (BinaryReader br = new BinaryReader(File.Open(ProgramConstants.GamePath + SAVED_GAME_PATH + FileName, FileMode.Open, FileAccess.Read)))
                {
                    br.BaseStream.Position = 2256; // 00000980

                    string saveGameName = String.Empty;
                    // Read name until we encounter two zero-bytes
                    // TODO remake, it's probably an Unicode string
                    bool wasLastByteZero = false;
                    while (true)
                    {
                        byte characterByte = br.ReadByte();
                        if (characterByte == 0)
                        {
                            if (wasLastByteZero)
                                break;
                            wasLastByteZero = true;
                        }
                        else
                        {
                            wasLastByteZero = false;
                            char character = Convert.ToChar(characterByte);
                            saveGameName = saveGameName + character;
                        }
                    }

                    GUIName = saveGameName;
                }

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
