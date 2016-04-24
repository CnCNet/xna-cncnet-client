using System;
using System.Collections.Generic;
using System.Text;

namespace DTAClient.domain
{
    public class SavedGame
    {
        public SavedGame() { }

        public SavedGame(string fileName, string guiName, bool enhanced, DateTime lastModified)
        {
            FileName = fileName;
            GUIName = guiName;
            LastModified = lastModified;
        }

        public string FileName { get; set; }
        public string GUIName { get; set; }
        public DateTime LastModified { get; set; }
    }
}
