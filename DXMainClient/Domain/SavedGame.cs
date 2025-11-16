using ClientCore;
using Rampastring.Tools;
using System;
using System.IO;
using OpenMcdf;
using System.Diagnostics;

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
        public int CustomMissionID { get; private set; }

        /// <summary>
        /// Reads and sets the saved game's name and last modified date, and returns true if succesful.
        /// </summary>
        /// <returns>True if parsing the info was succesful, otherwise false.</returns>
        public bool ParseInfo()
        {
            try
            {
                FileInfo savedGameFileInfo = SafePath.GetFile(ProgramConstants.GamePath, SAVED_GAME_PATH, FileName);

                using (Stream file = savedGameFileInfo.Open(FileMode.Open, FileAccess.Read))
                {
                    var cf = new CompoundFile(file);

                    GUIName = System.Text.Encoding.Unicode.GetString(cf.RootStorage.GetStream("Scenario Description").GetData()).TrimEnd(['\0']);
                    try
                    {
                        CustomMissionID = BitConverter.ToInt32(cf.RootStorage.GetStream("CustomMissionID").GetData(), 0);
                    }
                    catch (CFItemNotFound)
                    {
                        CustomMissionID = 0;
                    }
                }

                LastModified = savedGameFileInfo.LastWriteTime;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log("An error occured while parsing saved game " + FileName + ":" +
                    ex.ToString());
                return false;
            }
        }
    }
}
