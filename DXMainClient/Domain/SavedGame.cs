using ClientCore;
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
                using (BinaryReader br = new BinaryReader(File.Open(ProgramConstants.GamePath + SAVED_GAME_PATH + FileName, FileMode.Open, FileAccess.Read)))
                {
                    br.BaseStream.Position = 2256; // 00000980

                    string saveGameName = String.Empty;
                    // Read a UTF-16 LE string, until a null character is met. A null character in UTF-16 LE usually consists of two 0x00 bytes, but it's not always the case.
                    var saveGameNameBuffer = new List<byte>();
                    while (true)
                    {
                        byte lowByte = br.ReadByte();
                        byte highByte = br.ReadByte();

                        if (lowByte == 0 && highByte == 0)
                        {
                            // TODO: if the character is a surrogate pair (i.e. two 16-bit code units) and one of the 16-bit code units happens to be 0x0000, the string will be truncated.
                            // However, very few characters are influenced and none of them are common characters. It's so rare that I can't take an example easily.
                            break;
                        }

                        saveGameNameBuffer.Add(lowByte);
                        saveGameNameBuffer.Add(highByte);
                    }

                    saveGameName = System.Text.Encoding.Unicode.GetString(saveGameNameBuffer.ToArray());
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
