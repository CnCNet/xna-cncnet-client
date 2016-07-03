/// @author Rami "Rampastring" Pasanen
/// http://www.moddb.com/members/rampastring

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;
using ClientCore.CnCNet5;
using Rampastring.Tools;

namespace ClientCore
{
    /// <summary>
    /// Includes static methods useful for both Skirmish and CnCNet lobbies.
    /// </summary>
    public static class SharedLogic
    {
        /// <summary>
        /// Consolidates the contents of a map INI file, game mode INI file and a list of
        /// game option INI files. Writes the content into spawnmap.map.
        /// </summary>
        /// <param name="mapIni">An IniFile that represents the map.</param>
        /// <param name="gameModeIni">An IniFile that contains game mode specific code.</param>
        /// <param name="gameOptionInis">A list of game option associated INI files.</param>
        public static void WriteMap(IniFile mapIni, IniFile gameModeIni, List<IniFile> gameOptionInis)
        {
            Logger.Log("Consolidating INI files.");

            IniFile.ConsolidateIniFiles(mapIni, gameModeIni);

            for (int iniId = 0; iniId < gameOptionInis.Count; iniId++)
            {
                IniFile.ConsolidateIniFiles(mapIni, gameOptionInis[iniId]);
            }

            Logger.Log("Writing final map INI file.");
            mapIni.WriteIniFile(ProgramConstants.GamePath + ProgramConstants.SPAWNMAP_INI);
        }


        /// <summary>
        /// Writes co-op data to spawn.ini in case the map is co-op map.
        /// </summary>
        /// <param name="map">The selected map.</param>
        /// <param name="players">The list of human players in the game.</param>
        /// <param name="aiPlayers">The list of AI players in the game.</param>
        /// <param name="MultiCmbIndexes">A list containing info on players' MultiX indexes.</param>
        /// <param name="coopDifficultyLevel">The selected difficulty level. 0 = hard, 1 = medium, 2 = boringly easy.</param>
        /// <param name="amountOfSides">The amount of sides in the game.</param>
        /// <param name="mapCodePath">The code to the map's co-op descriptor INI (usually (map path).ini).</param>
        public static int WriteCoopDataToSpawnIni(Map map, List<PlayerInfo> players, List<PlayerInfo> aiPlayers,
            List<int> MultiCmbIndexes, int coopDifficultyLevel, int amountOfSides, string mapCodePath, int seed)
        {
            if (!map.IsCoop)
            {
                Logger.Log("The current map isn't for co-op - skipping writing co-op data.");
                return -1;
            }

            Logger.Log("Writing co-op info.");

            Random random = new Random(seed);

            IniFile coopInfoIni = new IniFile(mapCodePath);

            List<PlayerInfo> enemyPlayers = new List<PlayerInfo>();
            List<int> enemyHandicaps = new List<int>();

            int[] pickableRandomColors = new int[20] { 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31, 33, 35,
                37, 39, 41, 43, 45, 47, 49};

            int enemyCount = coopInfoIni.GetIntValue("CoopInfo", "EnemyPlayerCount", 0);

            if (map.CoopEvenPlayers)
                enemyCount = players.Count + aiPlayers.Count;

            string[] enemyStartingLocations = coopInfoIni.GetStringValue("CoopInfo", "StartingLocations", "-1").Split(',');
            string eSides = coopInfoIni.GetStringValue("CoopInfo", "Sides", "-1");
            string[] enemySides = null;
            if (eSides != "-1")
                enemySides = eSides.Split(',');
            string eColors = coopInfoIni.GetStringValue("CoopInfo", "Colors", "-1");
            string[] enemyColors = null;
            if (eColors != "-1")
                enemyColors = eColors.Split(',');

            int friendlyAICount = coopInfoIni.GetIntValue("CoopInfo", "FriendlyAICount", 0);

            int neutralColor = coopInfoIni.GetIntValue("CoopInfo", "NeutralColor", -1);
            int specialColor = coopInfoIni.GetIntValue("CoopInfo", "SpecialColor", -1);

            for (int eId = 0; eId < enemyCount; eId++)
            {
                PlayerInfo enemyAI = new PlayerInfo();

                // Hard
                if (coopDifficultyLevel == 0)
                {
                    enemyAI.Name = "Hard Coop AI Player";
                    enemyHandicaps.Add(0);
                }
                // Medium
                else if (coopDifficultyLevel == 1)
                {
                    enemyAI.Name = "Medium Coop AI Player";
                    enemyHandicaps.Add(1);
                }
                // Way too easy
                else
                {
                    enemyAI.Name = "Easy Coop AI Player";
                    enemyHandicaps.Add(2);
                }

                enemyAI.StartingLocation = Convert.ToInt32(enemyStartingLocations[eId]);
                if (enemySides != null)
                    enemyAI.SideId = Convert.ToInt32(enemySides[eId]);
                else
                    enemyAI.SideId = random.Next(0, amountOfSides);

                if (enemyColors == null || enemyColors.Length < eId)
                    enemyAI.ColorId = pickableRandomColors[random.Next(0, pickableRandomColors.Length)];
                else
                    enemyAI.ColorId = Convert.ToInt32(enemyColors[eId]);

                enemyAI.TeamId = 0;
                enemyAI.IsAI = true;

                enemyPlayers.Add(enemyAI);
            }

            // Write data into spawn.ini
            IniFile spawnIni = new IniFile(ProgramConstants.GamePath + ProgramConstants.SPAWNER_SETTINGS);
            spawnIni.SetIntValue("Settings", "AIPlayers", aiPlayers.Count + enemyCount + friendlyAICount);
            int multiId = players.Count + aiPlayers.Count + 1;

            for (int aId = 0; aId < friendlyAICount; aId++)
            {


                multiId++;
            }

            for (int eId = 0; eId < enemyCount; eId++)
            {
                PlayerInfo enemyAI = enemyPlayers[eId];

                spawnIni.SetIntValue("HouseHandicaps", "Multi" + multiId, enemyHandicaps[eId]);
                spawnIni.SetIntValue("HouseCountries", "Multi" + multiId, enemyAI.SideId);
                spawnIni.SetIntValue("HouseColors", "Multi" + multiId, enemyAI.ColorId);
                spawnIni.SetIntValue("SpawnLocations", "Multi" + multiId, enemyAI.StartingLocation);

                int allyId = 0;
                int allyMultiId = players.Count + aiPlayers.Count + friendlyAICount;
                for (int e2Id = 0; e2Id < enemyCount; e2Id++)
                {
                    allyMultiId++;

                    if (eId == e2Id)
                        continue;

                    spawnIni.SetIntValue("Multi" + multiId + "_Alliances", "HouseAlly" + getHouseIdFromInt(allyId), allyMultiId - 1);
                    allyId++;
                }

                multiId++;
            }

            if (neutralColor > -1)
                spawnIni.SetIntValue("HouseColors", "Multi" + multiId, neutralColor);

            if (specialColor > -1)
                spawnIni.SetIntValue("HouseColors", "Multi" + (multiId + 1), specialColor);

            // Check if the mission has forced sides for players
            string[] pSides = coopInfoIni.GetStringValue("CoopInfo", "ForcedPlayerSide", "-1").Split(',');
            
            if (pSides.Length == 1)
            {
                if (pSides[0] != "-1")
                {
                    int playerSide = Convert.ToInt32(pSides[0]);
                    if (playerSide > -1)
                    {
                        spawnIni.SetIntValue("Settings", "Side", playerSide);
                        for (int pId = 1; pId < players.Count; pId++)
                        {
                            spawnIni.SetIntValue("Other" + pId, "Side", playerSide);
                        }

                        for (int aiId = 0; aiId < aiPlayers.Count; aiId++)
                        {
                            spawnIni.SetIntValue("HouseCountries", "Multi" + (players.Count + aiId + 1), playerSide);
                        }

                        spawnIni.SetStringValue("Settings", "CustomLoadScreen", LoadingScreenController.GetLoadScreenName(playerSide));
                    }

                    spawnIni.WriteIniFile();
                    return playerSide;
                }
            }
            else
            {
                Logger.Log("Multiple co-op sides have been specified.");

                List<int> playerSides = new List<int>();
                foreach (string pSide in pSides)
                    playerSides.Add(Convert.ToInt32(pSide));

                int myIndex = players.FindIndex(p => p.Name == ProgramConstants.PLAYERNAME);
                int mySide = -1;

                for (int fSideId = 0; fSideId < playerSides.Count; fSideId++)
                {
                    if (fSideId == myIndex)
                    {
                        mySide = playerSides[fSideId];
                        spawnIni.SetIntValue("Settings", "Side", mySide);
                        Logger.Log("Personal side: " + fSideId + " | " + mySide);
                    }
                    else
                    {
                        if (fSideId < players.Count)
                        {
                            int index = MultiCmbIndexes[fSideId];

                            Logger.Log("Player index " + index + " side: " + fSideId + " | " + playerSides[fSideId]);

                            spawnIni.SetIntValue("Other" + index + 1, "Side", playerSides[fSideId]);
                        }
                        else
                        {
                            Logger.Log("AI multi-index: " + (fSideId + 1) + " side: " + fSideId + " | " + playerSides[fSideId]);
                            spawnIni.SetIntValue("HouseCountries", "Multi" + fSideId + 1, playerSides[fSideId]);
                        }
                    }
                }
            }

            spawnIni.WriteIniFile();

            return -1;
        }

        /// <summary>
        /// Returns a house's ID as a string. For example 0 returns "One" and 1 returns "Two".
        /// Used when writing [MultiX_Alliances] to spawn.ini.
        /// </summary>
        /// <param name="allyId">The multi-ID of the house.</param>
        private static string getHouseIdFromInt(int allyId)
        {
            switch (allyId)
            {
                case 0:
                    return "One";
                case 1:
                    return "Two";
                case 2:
                    return "Three";
                case 3:
                    return "Four";
                case 4:
                    return "Five";
                case 5:
                    return "Six";
                default:
                    return "One";
            }
        }

        /// <summary>
        /// Loads the map preview for a specific map.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <returns>The preview image to use.</returns>
        public static Image LoadPreview(Map map, out PictureBoxSizeMode sizeMode, out bool success)
        {
            if (map.StaticPreviewSize)
                sizeMode = PictureBoxSizeMode.CenterImage;
            else
                sizeMode = PictureBoxSizeMode.Zoom;

            string previewPath = String.Empty;

            if (String.IsNullOrEmpty(map.PreviewPath))
                previewPath = map.Path.Substring(0, map.Path.Length - 3) + "png";
            else
                previewPath = Path.GetDirectoryName(map.Path) + "\\" + map.PreviewPath;

            Logger.Log("Loading preview from " + previewPath);

            Image previewImg;

            if (!File.Exists(ProgramConstants.GamePath + previewPath))
            {
                Logger.Log("Preview file " + previewPath + " doesn't exist!");

                if (!map.ExtractCustomPreview)
                {
                    Logger.Log("Displaying missing preview image because the preview shouldn't be extracted for this map.");
                    success = false;
                    return Image.FromFile(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + "nopreview.png");
                }

                try
                {
                    Logger.Log("Attempting to extract map preview from the map file.");
                    PreviewExtractor.MapThumbnailExtractor mte = new PreviewExtractor.MapThumbnailExtractor(map.Path, 1);
                    previewImg = mte.Get_Bitmap();
                }
                catch
                {
                    Logger.Log("Extracting map preview failed.");
                    success = false;
                    return Image.FromFile(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + "nopreview.png");
                }
            }
            else
                previewImg = Image.FromFile(ProgramConstants.GamePath + previewPath);

            success = true;
            return previewImg;
        }

        /// <summary>
        /// Resizes an image.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="maxWidth">The maximum width of the new image.</param>
        /// <param name="maxHeight">The maximum height of the new image.</param>
        /// <returns>The resized image.</returns>
        public static Image ResizeImage(Image image, int maxWidth, int maxHeight)
        {
            // Get the image's original width and height
            int originalWidth = image.Width;
            int originalHeight = image.Height;

            // To preserve the aspect ratio
            float ratioX = (float)maxWidth / (float)originalWidth;
            float ratioY = (float)maxHeight / (float)originalHeight;
            float ratio = Math.Min(ratioX, ratioY);

            // New width and height based on aspect ratio
            int newWidth = (int)(originalWidth * ratio);
            int newHeight = (int)(originalHeight * ratio);

            // Convert other formats (including CMYK) to RGB.
            Bitmap newImage = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb);

            // Draws the image in the specified size with quality mode set to HighQuality
            using (Graphics graphics = Graphics.FromImage(newImage))
            {
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
            }

            return newImage;
        }

        /// <summary>
        /// Sharpens the specified image.
        /// http://stackoverflow.com/questions/903632/sharpen-on-a-bitmap-using-c-sharp
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="strength">The strength.</param>
        /// <returns></returns>
        public static Bitmap Sharpen(Image image, double strength)
        {
            using (var bitmap = image as Bitmap)
            {
                if (bitmap != null)
                {
                    var sharpenImage = bitmap.Clone() as Bitmap;

                    int width = image.Width;
                    int height = image.Height;

                    // Create sharpening filter.
                    const int filterSize = 3;

                    var filter = new double[,]
                {
                    {-1, -1, -1, -1, -1},
                    {-1,  2,  2,  2, -1},
                    {-1,  2, 16,  2, -1},
                    {-1,  2,  2,  2, -1},
                    {-1, -1, -1, -1, -1}
                };

                    double bias = 1.0 - strength;
                    double factor = strength / 16.0;

                    const int s = filterSize / 2;

                    var result = new Color[image.Width, image.Height];

                    // Lock image bits for read/write.
                    if (sharpenImage != null)
                    {
                        BitmapData pbits = sharpenImage.LockBits(new Rectangle(0, 0, width, height),
                                                                    ImageLockMode.ReadWrite,
                                                                    PixelFormat.Format24bppRgb);

                        // Declare an array to hold the bytes of the bitmap.
                        int bytes = pbits.Stride * height;
                        var rgbValues = new byte[bytes];

                        // Copy the RGB values into the array.
                        Marshal.Copy(pbits.Scan0, rgbValues, 0, bytes);

                        int rgb;
                        // Fill the color array with the new sharpened color values.
                        for (int x = s; x < width - s; x++)
                        {
                            for (int y = s; y < height - s; y++)
                            {
                                double red = 0.0, green = 0.0, blue = 0.0;

                                for (int filterX = 0; filterX < filterSize; filterX++)
                                {
                                    for (int filterY = 0; filterY < filterSize; filterY++)
                                    {
                                        int imageX = (x - s + filterX + width) % width;
                                        int imageY = (y - s + filterY + height) % height;

                                        rgb = imageY * pbits.Stride + 3 * imageX;

                                        red += rgbValues[rgb + 2] * filter[filterX, filterY];
                                        green += rgbValues[rgb + 1] * filter[filterX, filterY];
                                        blue += rgbValues[rgb + 0] * filter[filterX, filterY];
                                    }

                                    rgb = y * pbits.Stride + 3 * x;

                                    int r = Math.Min(Math.Max((int)(factor * red + (bias * rgbValues[rgb + 2])), 0), 255);
                                    int g = Math.Min(Math.Max((int)(factor * green + (bias * rgbValues[rgb + 1])), 0), 255);
                                    int b = Math.Min(Math.Max((int)(factor * blue + (bias * rgbValues[rgb + 0])), 0), 255);

                                    result[x, y] = Color.FromArgb(r, g, b);
                                }
                            }
                        }

                        // Update the image with the sharpened pixels.
                        for (int x = s; x < width - s; x++)
                        {
                            for (int y = s; y < height - s; y++)
                            {
                                rgb = y * pbits.Stride + 3 * x;

                                rgbValues[rgb + 2] = result[x, y].R;
                                rgbValues[rgb + 1] = result[x, y].G;
                                rgbValues[rgb + 0] = result[x, y].B;
                            }
                        }

                        // Copy the RGB values back to the bitmap.
                        Marshal.Copy(rgbValues, 0, pbits.Scan0, bytes);
                        // Release image bits.
                        sharpenImage.UnlockBits(pbits);
                    }

                    return sharpenImage;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the font used by the client based on font information given in DTACnCNetClient.ini.
        /// </summary>
        /// <returns>The font.</returns>
        public static Font GetCommonFont()
        {
            return GetFont(DomainController.Instance().GetCommonFont());
        }

        /// <summary>
        /// Gets the font used by the client's list boxes based on font information given in DTACnCNetClient.ini.
        /// </summary>
        /// <returns>The font.</returns>
        public static Font GetListBoxFont()
        {
            return GetFont(DomainController.Instance().GetListBoxFont());
        }

        /// <summary>
        /// Parses a font based on the font information given in a string.
        /// </summary>
        /// <param name="fontString">The string that contains info about the font. Example: "Microsoft Sans Serif,Regular,8.25"</param>
        /// <returns>The font.</returns>
        public static Font GetFont(string fontString)
        {
            string[] font = fontString.Split(',');

            string fontName = font[0];
            string style = font[1];
            float emSize = 8.25f;
            if (font.Length > 2)
                emSize = Convert.ToSingle(font[2], CultureInfo.GetCultureInfo("en-US"));

            FontStyle fontStyle = FontStyle.Regular;

            switch (style)
            {
                case "Bold":
                    fontStyle = FontStyle.Bold;
                    break;
                case "Italic":
                    fontStyle = FontStyle.Italic;
                    break;
                case "Strikeout":
                    fontStyle = FontStyle.Strikeout;
                    break;
                case "Underline":
                    fontStyle = FontStyle.Underline;
                    break;
                case "BoldAndItalic":
                    fontStyle = FontStyle.Bold | FontStyle.Italic;
                    break;
                default:
                    fontStyle = FontStyle.Regular;
                    break;
            }

            return new System.Drawing.Font(fontName, emSize, fontStyle);
        }

        /// <summary>
        /// Parses a map and adds it to the map list. Returns true if succesful, otherwise false.
        /// </summary>
        /// <param name="mapPath">The path to the map file, NOT including the path to the game directory.</param>
        /// <returns>True if the map was succesfully parsed and added to the maplist, otherwise false.</returns>
        public static bool AddMapToMaplist(string mapPath)
        {
            Logger.Log("Loading custom map " + mapPath);

            IniFile customMap;
            try
            {
                customMap = new IniFile(ProgramConstants.GamePath + mapPath);
            }
            catch
            {
                Logger.Log("Map " + mapPath + " has bad INI code in it - parsing failed.");
                return false;
            }

            if (!customMap.SectionExists("Basic"))
            {
                Logger.Log("[Basic] section is missing - unable to load custom map.");
                return false;
            }

            string customMapGameMode = "Custom Maps";

            string name = customMap.GetStringValue("Basic", "Name", "Unnamed map");
            string author = customMap.GetStringValue("Basic", "Author", "Unknown author");
            //string briefing = customMap.GetStringValue("Basic", "Briefing", String.Empty);
            //bool isCoopMission = customMap.GetBooleanValue("Basic", "IsCoopMission", false);
            int minplayers = customMap.GetIntValue("Basic", "MinPlayer", 2);
            int maxplayers = customMap.GetIntValue("Basic", "MaxPlayer", 8);
            string sha1 = Utilities.CalculateSHA1ForFile(ProgramConstants.GamePath + mapPath);

            string[] gameModes = customMap.GetStringValue("Basic", "GameModes", customMapGameMode).Split(',');

            if (gameModes[0] == customMapGameMode)
            {
                gameModes = customMap.GetStringValue("Basic", "GameMode", customMapGameMode).Split(',');

                for (int gmId = 0; gmId < gameModes.Length; gmId++)
                {
                    if (gameModes[gmId][0] == ' ')
                        gameModes[gmId] = gameModes[gmId].Substring(1);

                    char initialChar = gameModes[gmId][0];

                    if (Char.IsLower(initialChar))
                        gameModes[gmId] = Char.ToUpper(initialChar) + gameModes[gmId].Substring(1);

                    if (gameModes[gmId] == "Official Ladder")
                        gameModes[gmId] = "Unofficial Ladder";
                }
            }

            Map map = new Map(name, maxplayers, minplayers, gameModes, author, sha1, mapPath);
            //map.Briefing = briefing.Replace("@", Environment.NewLine);
            //map.IsCoop = isCoopMission;
            //map.GameOptionsForcedOn = customMap.GetStringValue("Basic", "GameOptionsForcedOn", String.Empty).Split(',');
            //map.GameOptionsForcedOff = customMap.GetStringValue("Basic", "GameOptionsForcedOff", String.Empty).Split(',');
            map.EnforceMaxPlayers = true;

            foreach (string gameMode in gameModes)
            {
                if (!CnCNetData.GameTypes.Contains(gameMode))
                    CnCNetData.GameTypes.Add(gameMode);
            }

            string[] actualSizeValues = customMap.GetStringValue("Map", "Size", "0,0,100,100").Split(',');
            string[] localSizeValues = customMap.GetStringValue("Map", "Size", "0,0,100,100").Split(',');

            if (actualSizeValues.Length != 4 || localSizeValues.Length != 4)
            {
                Logger.Log("Invalid size values for map, skipping it.");
                return false;
            }

            map.LocalSize = customMap.GetStringValue("Map", "LocalSize", "0,0,100,100");
            map.Size = customMap.GetStringValue("Map", "Size", "0,0,100,100");

            int previewSizeY = 0;
            int previewSizeX = 0;

            string previewPath = ProgramConstants.GamePath + mapPath.Substring(0, mapPath.Length - 3) + "png";

            if (File.Exists(previewPath))
            {
                Image image = Image.FromFile(previewPath);

                previewSizeX = image.Width;
                previewSizeY = image.Height;
            }
            else
            {
                try
                {
                    Logger.Log("Attempting to extract thumbnail of " + mapPath);

                    if (customMap.GetStringValue("PreviewPack", "1", "") ==
                        "yAsAIAXQ5PDQ5PDQ6JQATAEE6PDQ4PDI4JgBTAFEAkgAJyAATAG0AydEAEABpAJIA0wBVA")
                    {
                        Logger.Log("Hidden preview detected - skipping extraction because Iran's thumbnail extraction code fails with hidden previews.");
                        Logger.Log("Adding the map without preview information.");
                        map.ExtractCustomPreview = false;
                        CnCNetData.MapList.Add(map);
                        return true;
                    }

                    PreviewExtractor.MapThumbnailExtractor mte = new PreviewExtractor.MapThumbnailExtractor(ProgramConstants.GamePath + mapPath, 1);
                    Bitmap bmp = mte.Get_Bitmap();

                    previewSizeX = bmp.Width;
                    previewSizeY = bmp.Height;
                }
                catch
                {
                    Logger.Log("An error occured while extracting the thumbnail. Adding the map to the list of custom maps without preview information.");
                    map.ExtractCustomPreview = false;
                    CnCNetData.MapList.Add(map);
                    return true;
                }
            }

            for (int pId = 0; pId < 8; pId++)
            {
                string waypoint = customMap.GetStringValue("Waypoints", Convert.ToString(pId), "-1");

                if (waypoint == "-1")
                    break;

                int[] coords = getWaypointXYCoords(waypoint, actualSizeValues, localSizeValues, previewSizeX, previewSizeY);

                map.StartingLocationsX.Add(coords[0]);
                map.StartingLocationsY.Add(coords[1]);
            }

            CnCNetData.MapList.Add(map);

            Logger.Log("Custom map " + mapPath + " loaded succesfully.");
            return true;
        }


        /// <summary>
        /// Updates a map's starting location indicator coordinates.
        /// </summary>
        /// <param name="map">The map.</param>
        public static void UpdateWaypointCoords(Map map)
        {
            Logger.Log("Updating waypoint coordinates for " + map.Path);

            map.StartingLocationsX.Clear();
            map.StartingLocationsY.Clear();

            IniFile mapIni = new IniFile(ProgramConstants.GamePath + map.Path);

            string[] actualSizeValues = mapIni.GetStringValue("Map", "Size", "0,0,100,100").Split(',');
            string[] localSizeValues = mapIni.GetStringValue("Map", "Size", "0,0,100,100").Split(',');

            int previewSizeY = 0;
            int previewSizeX = 0;

            string previewPath = ProgramConstants.GamePath + map.Path.Substring(0, map.Path.Length - 3) + "png";

            if (File.Exists(previewPath))
            {
                Image image = Image.FromFile(previewPath);

                previewSizeX = image.Width;
                previewSizeY = image.Height;
            }
            else
            {
                try
                {
                    Logger.Log("Attempting to extract thumbnail of " + map.Path);

                    if (mapIni.GetStringValue("PreviewPack", "1", "") ==
                        "yAsAIAXQ5PDQ5PDQ6JQATAEE6PDQ4PDI4JgBTAFEAkgAJyAATAG0AydEAEABpAJIA0wBVA")
                    {
                        Logger.Log("Hidden preview detected - skipping extraction because Iran's thumbnail extraction code fails with hidden previews.");
                        return;
                    }

                    PreviewExtractor.MapThumbnailExtractor mte = new PreviewExtractor.MapThumbnailExtractor(ProgramConstants.GamePath + map.Path, 1);
                    Bitmap bmp = mte.Get_Bitmap();

                    previewSizeX = bmp.Width;
                    previewSizeY = bmp.Height;
                }
                catch
                {
                    Logger.Log("An error occured while extracting the thumbnail.");
                    return;
                }
            }

            for (int pId = 0; pId < 8; pId++)
            {
                string waypoint = mapIni.GetStringValue("Waypoints", Convert.ToString(pId), "-1");

                if (waypoint == "-1")
                    break;

                int[] coords = getWaypointXYCoords(waypoint, actualSizeValues, localSizeValues, previewSizeX, previewSizeY);

                map.StartingLocationsX.Add(coords[0]);
                map.StartingLocationsY.Add(coords[1]);
            }
        }


        /// <summary>
        /// Converts a waypoint's coordinate string into pixel coordinates on the preview image.
        /// </summary>
        /// <returns>The waypoint's location on the map preview as an int[] array {x, y}.</returns>
        private static int[] getWaypointXYCoords(string waypoint, string[] actualSizeValues, string[] localSizeValues,
            int previewSizeX, int previewSizeY)
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

            int x = Convert.ToInt32(ratioX * previewSizeX);
            int y = Convert.ToInt32(ratioY * previewSizeY);

            return new int[] { x, y };
        }

        public static void DumpMapInfo(string path, int firstMapIndex)
        {
            Logger.Log("Dumping map data to " + path);

            StreamWriter sw = new StreamWriter(File.OpenWrite(path));

            sw.WriteLine("; Generated by the CnCNet Client, version " + Application.ProductVersion);
            sw.WriteLine("[MultiMaps]");
            int mapCount = 0;
            for (int i = firstMapIndex; i < CnCNetData.MapList.Count; i++)
            {
                Map map = CnCNetData.MapList[i];
                sw.WriteLine(mapCount + "=" + map.Path.Substring(0, map.Path.Length - 4));
                mapCount++;
            }

            sw.WriteLine();
            sw.WriteLine();
            for (int i = firstMapIndex; i < CnCNetData.MapList.Count; i++)
            {
                Map map = CnCNetData.MapList[i];

                Logger.Log("Writing map data: " + map.Path);

                sw.WriteLine("[" + map.Path.Substring(0, map.Path.Length - 4) + "]");
                sw.WriteLine("MinPlayers=" + map.MinPlayers);
                sw.WriteLine("MaxPlayers=" + map.AmountOfPlayers);
                sw.WriteLine("Description=" + map.Name);
                sw.WriteLine("Author=" + map.Author);
                sw.WriteLine("EnforceMaxPlayers=" + map.EnforceMaxPlayers);
                sw.WriteLine("ID=" + map.SHA1);

                if (String.IsNullOrEmpty(map.Size))
                {
                    IniFile mapIni = new IniFile(ProgramConstants.GamePath + map.Path);
                    sw.WriteLine("Size=" + mapIni.GetStringValue("Map", "Size", "0,0,0,0"));
                    sw.WriteLine("LocalSize=" + mapIni.GetStringValue("Map", "LocalSize", "0,0,0,0"));
                }
                else
                {
                    sw.WriteLine("Size=" + map.Size);
                    sw.WriteLine("LocalSize=" + map.LocalSize);
                }

                PictureBoxSizeMode pbsz;
                bool success;
                Image previewImage = SharedLogic.LoadPreview(map, out pbsz, out success);
                sw.WriteLine("PreviewSize=" + previewImage.Width + "," + previewImage.Height);
                string gameModes = String.Empty;
                foreach (string gameMode in map.GameModes)
                    gameModes = gameModes + "," + gameMode;
                gameModes = gameModes.Remove(0, 1);
                sw.WriteLine("GameModes=" + gameModes);
                int wpId = 0;
                foreach (string waypoint in map.Waypoints)
                {
                    sw.WriteLine("Waypoint" + wpId + "=" + waypoint);
                    wpId++;
                }

                sw.WriteLine();
            }

            sw.Close();
        }
    }
}
