using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
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
        const int MAX_PLAYERS = 8;
        const int MAP_SIZE_X = 48;
        const int MAP_SIZE_Y = 24;

        public Map(string path)
        {
            Path = path;
        }

        public Map(string name, int amountOfPlayers, int minPlayers, string[] gameModes, string author, string sha1, string path)
        {
            Name = name;
            MaxPlayers = amountOfPlayers;
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
        public int MaxPlayers { get; set; }

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
        /// The briefing of the map.
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
        /// The path to the map file.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// The file name of the preview image.
        /// </summary>
        public string PreviewPath { get; set; }

        /// <summary>
        /// The game modes that the map is listed for.
        /// </summary>
        public string[] GameModes;

        /// <summary>
        /// The pixel coordinates of the map's player starting locations.
        /// </summary>
        public List<Point> StartingLocations = new List<Point>();

        public Texture2D PreviewTexture { get; set; }

        private bool extractCustomPreview = true;

        /// <summary>
        /// If false, the preview shouldn't be extracted for this custom map.
        /// </summary>
        public bool ExtractCustomPreview
        {
            get { return extractCustomPreview; }
            set { extractCustomPreview = value; }
        }

        public void SetInfoFromINI(IniFile iniFile)
        {
            Name = iniFile.GetStringValue(Path, "Description", "Unnamed map");
            Author = iniFile.GetStringValue(Path, "Author", "Unknown author");
            GameModes = iniFile.GetStringValue(Path, "GameModes", "Default").Split(',');
            MinPlayers = iniFile.GetIntValue(Path, "MinPlayers", 0);
            MaxPlayers = iniFile.GetIntValue(Path, "MaxPlayers", 0);
            EnforceMaxPlayers = iniFile.GetBooleanValue(Path, "EnforceMaxPlayers", false);
            PreviewPath = iniFile.GetStringValue(Path, "PreviewPath", System.IO.Path.GetFileNameWithoutExtension(Path) + ".png");
            Briefing = iniFile.GetStringValue(Path, "Briefing", String.Empty).Replace("@", Environment.NewLine);

            string[] localSize = iniFile.GetStringValue(Path, "LocalSize", "0,0,0,0").Split(',');
            string[] size = iniFile.GetStringValue(Path, "Size", "0,0,0,0").Split(',');

            string[] previewSize = iniFile.GetStringValue(Path, "PreviewSize", "0,0").Split(',');
            Point previewSizePoint = new Point(Int32.Parse(previewSize[0]), Int32.Parse(previewSize[1]));

            for (int i = 0; i < MAX_PLAYERS; i++)
            {
                string waypoint = iniFile.GetStringValue(Path, "Waypoint" + i, String.Empty);

                if (!String.IsNullOrEmpty(waypoint))
                    StartingLocations.Add(GetWaypointCoords(waypoint, size, localSize, previewSizePoint));
                else
                    break;
            }

            if (MCDomainController.Instance.GetMapPreviewPreloadStatus())
                PreviewTexture = AssetLoader.LoadTexture(Path + ".png");

            string forcedOptionsSection = iniFile.GetStringValue(Path, "ForcedOptions", String.Empty);

            if (String.IsNullOrEmpty(forcedOptionsSection))
                return;

            List<string> keys = iniFile.GetSectionKeys(forcedOptionsSection);

            foreach (string key in keys)
            {
                string value = iniFile.GetStringValue(forcedOptionsSection, key, String.Empty);

                int intValue = 0;
                if (Int32.TryParse(value, out intValue))
                {

                }
            }
        }

        /// <summary>
        /// Converts a waypoint's coordinate string into pixel coordinates on the preview image.
        /// </summary>
        /// <returns>The waypoint's location on the map preview as a point.</returns>
        private static Point GetWaypointCoords(string waypoint, string[] actualSizeValues, string[] localSizeValues,
            Point previewSizePoint)
        {
            int rx = 0;
            int ry = 0;

            if (waypoint.Length == 5)
            {
                ry = Convert.ToInt32(waypoint.Substring(0, 2));
                rx = Convert.ToInt32(waypoint.Substring(2));
            }
            else // if location.Length == 6
            {
                ry = Convert.ToInt32(waypoint.Substring(0, 3));
                rx = Convert.ToInt32(waypoint.Substring(3));
            }

            int isoTileX = rx - ry + Convert.ToInt32(actualSizeValues[2]) - 1;
            int isoTileY = rx + ry - Convert.ToInt32(actualSizeValues[2]) - 1;

            int pixelPosX = isoTileX * 24;
            int pixelPosY = isoTileY * 12;

            pixelPosX = pixelPosX - (Convert.ToInt32(localSizeValues[0]) * 48);
            pixelPosY = pixelPosY - (Convert.ToInt32(localSizeValues[1]) * 24);

            // Calculate map size
            int mapSizeX = Convert.ToInt32(localSizeValues[2]) * 48;
            int mapSizeY = Convert.ToInt32(localSizeValues[3]) * 24;

            double ratioX = Convert.ToDouble(pixelPosX) / mapSizeX;
            double ratioY = Convert.ToDouble(pixelPosY) / mapSizeY;

            int x = Convert.ToInt32(ratioX * previewSizePoint.X);
            int y = Convert.ToInt32(ratioY * previewSizePoint.Y);

            return new Point(x, y);
        }

        public string[] GameOptionsForcedOff;
        public string[] GameOptionsForcedOn;
        public List<KeyValuePair<string, int>> ForcedComboBoxValues = new List<KeyValuePair<string, int>>();
    }
}
