using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.domain.CnCNet
{
    /// <summary>
    /// A multiplayer map.
    /// </summary>
    public class Map
    {
        public Map(string path)
        {
            Path = path;
        }

        public Map(string name, int amountOfPlayers, int minPlayers, string[] gameModes, string author, string sha1, string path)
        {
            Name = name;
            AmountOfPlayers = amountOfPlayers;
            MinPlayers = minPlayers;
            GameModes = gameModes;
            Author = author;
            SHA1 = sha1;
            Path = path;
        }

        /// <summary>
        /// The name of the map.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The maximum amount of players supported by the map.
        /// </summary>
        public int AmountOfPlayers { get; set; }

        /// <summary>
        /// The minimum amount of players supported by the map.
        /// </summary>
        public int MinPlayers { get; set; }

        /// <summary>
        /// Whether to use AmountOfPlayers for limiting the player count of the map.
        /// If false (which is the default), AmountOfPlayers is only used for randomizing
        /// players to starting waypoints.
        /// </summary>
        public bool EnforceMaxPlayers { get; set; }

        /// <summary>
        /// Controls if the map is meant for a co-operation game mode
        /// (enables briefing logic and forcing options, among others).
        /// </summary>
        public bool IsCoop { get; set; }

        /// <summary>
        /// If true, the amount of pre-placed enemy AIs in a coop mission is forced
        /// to be equal to the amount of players.
        /// </summary>
        public bool CoopEvenPlayers { get; set; }

        /// <summary>
        /// The briefing of the map (if it's a coop mission).
        /// </summary>
        public string Briefing { get; set; }

        /// <summary>
        /// The author of the map.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// The calculated SHA1 of the map.
        /// </summary>
        public string SHA1 { get; set; }

        /// <summary>
        /// If true, the preview image of the map won't be scaled in the game lobby.
        /// </summary>
        public bool StaticPreviewSize { get; set; }

        /// <summary>
        /// The path to the map file.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The file name of the preview image.
        /// </summary>
        public string PreviewPath { get; set; }

        /// <summary>
        /// The game modes the map is listed for.
        /// </summary>
        public string[] GameModes;

        /// <summary>
        /// The X-coordinates of the map's player starting locations.
        /// </summary>
        public List<int> StartingLocationsX = new List<int>();

        /// <summary>
        /// The Y-coordinates of the map's player starting locations.
        /// </summary>
        public List<int> StartingLocationsY = new List<int>();

        private bool extractCustomPreview = true;

        /// <summary>
        /// If false, the preview shouldn't be extracted for this custom map.
        /// </summary>
        public bool ExtractCustomPreview
        {
            get { return extractCustomPreview; }
            set { extractCustomPreview = value; }
        }

        /// <summary>
        /// The local size of the map as it is expressed in Tiberian Sun map files.
        /// </summary>
        public string LocalSize { get; set; }

        /// <summary>
        /// The size of the map as it is expressed in Tiberian Sun map files.
        /// </summary>
        public string Size { get; set; }

        public void SetInfoFromINI(IniFile iniFile)
        {
            Name = iniFile.GetStringValue(Path, "Description", "Unnamed map");
            Author = iniFile.GetStringValue(Path, "Author", "Unknown author");
            GameModes = iniFile.GetStringValue(Path, "GameModes", "Default").Split(',');
            // TODO init
        }

        public string[] GameOptionsForcedOff;
        public string[] GameOptionsForcedOn;
        public List<KeyValuePair<string, int>> ForcedComboBoxValues = new List<KeyValuePair<string, int>>();

        public List<string> Waypoints = new List<string>();
    }
}
