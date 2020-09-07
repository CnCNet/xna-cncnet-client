﻿using ClientCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PreviewExtractor;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utilities = Rampastring.Tools.Utilities;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// A multiplayer map.
    /// </summary>
    public class Map
    {
        private const int MAX_PLAYERS = 8;

        public Map(string path, bool official)
        {
            BaseFilePath = path;
            Official = official;
        }

        /// <summary>
        /// The name of the map.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The maximum amount of players supported by the map.
        /// </summary>
        public int MaxPlayers { get; private set; }

        /// <summary>
        /// The minimum amount of players supported by the map.
        /// </summary>
        public int MinPlayers { get; private set; }

        /// <summary>
        /// Whether to use AmountOfPlayers for limiting the player count of the map.
        /// If false (which is the default), AmountOfPlayers is only used for randomizing
        /// players to starting waypoints.
        /// </summary>
        public bool EnforceMaxPlayers { get; private set; }

        /// <summary>
        /// Controls if the map is meant for a co-operation game mode
        /// (enables briefing logic and forcing options, among others).
        /// </summary>
        public bool IsCoop { get; private set; }

        /// <summary>
        /// If set, this map won't be automatically transferred over CnCNet when
        /// a player doesn't have it.
        /// </summary>
        public bool Official { get; private set; }

        /// <summary>
        /// Contains co-op information.
        /// </summary>
        public CoopMapInfo CoopInfo { get; private set; }

        /// <summary>
        /// The briefing of the map.
        /// </summary>
        public string Briefing { get; private set; }

        /// <summary>
        /// The author of the map.
        /// </summary>
        public string Author { get; private set; }

        /// <summary>
        /// The calculated SHA1 of the map.
        /// </summary>
        public string SHA1 { get; private set; }

        /// <summary>
        /// The path to the map file.
        /// </summary>
        public string BaseFilePath { get; private set; }

        /// <summary>
        /// Returns the complete path to the map file.
        /// Includes the game directory in the path.
        /// </summary>
        public string CompleteFilePath => ProgramConstants.GamePath + BaseFilePath + ".map";

        /// <summary>
        /// The file name of the preview image.
        /// </summary>
        public string PreviewPath { get; private set; }

        /// <summary>
        /// If set, this map cannot be played on Skirmish.
        /// </summary>
        public bool MultiplayerOnly { get; private set; }

        /// <summary>
        /// If set, this map cannot be played with AI players.
        /// </summary>
        public bool HumanPlayersOnly { get; private set; }

        /// <summary>
        /// If set, players are forced to random starting locations on this map.
        /// </summary>
        public bool ForceRandomStartLocations { get; private set; }

        /// <summary>
        /// If set, players are forced to different teams on this map.
        /// </summary>
        public bool ForceNoTeams { get; private set; }

        /// <summary>
        /// The name of an extra INI file in INI\Map Code\ that should be
        /// embedded into this map's INI code when a game is started.
        /// </summary>
        public string ExtraININame { get; private set; }

        /// <summary>
        /// The game modes that the map is listed for.
        /// </summary>
        public string[] GameModes;

        /// <summary>
        /// The forced UnitCount for the map. -1 means none.
        /// </summary>
        int UnitCount = -1;

        /// <summary>
        /// The forced starting credits for the map. -1 means none.
        /// </summary>
        int Credits = -1;

        int NeutralHouseColor = -1;

        int SpecialHouseColor = -1;

        int Bases = -1;

        string[] localSize;

        string[] actualSize;

        IniFile mapIni;

        List<string> waypoints = new List<string>();

        /// <summary>
        /// The pixel coordinates of the map's player starting locations.
        /// </summary>
        List<Point> startingLocations;

        public Texture2D PreviewTexture { get; set; }

        private bool extractCustomPreview = true;

        /// <summary>
        /// If false, the preview shouldn't be extracted for this (custom) map.
        /// </summary>
        public bool ExtractCustomPreview
        {
            get { return extractCustomPreview; }
            set { extractCustomPreview = value; }
        }

        public List<KeyValuePair<string, bool>> ForcedCheckBoxValues = new List<KeyValuePair<string, bool>>();
        public List<KeyValuePair<string, int>> ForcedDropDownValues = new List<KeyValuePair<string, int>>();

        List<KeyValuePair<string, string>> ForcedSpawnIniOptions = new List<KeyValuePair<string, string>>();

        public bool SetInfoFromINI(IniFile iniFile)
        {
            try
            {
                string baseSectionName = iniFile.GetStringValue(BaseFilePath, "BaseSection", string.Empty);

                if (!string.IsNullOrEmpty(baseSectionName))
                    iniFile.CombineSections(baseSectionName, BaseFilePath);

                var section = iniFile.GetSection(BaseFilePath);

                Name = section.GetStringValue("Description", "Unnamed map");
                Author = section.GetStringValue("Author", "Unknown author");
                GameModes = section.GetStringValue("GameModes", "Default").Split(',');

                MinPlayers = section.GetIntValue("MinPlayers", 0);
                MaxPlayers = section.GetIntValue("MaxPlayers", 0);
                EnforceMaxPlayers = section.GetBooleanValue("EnforceMaxPlayers", false);
                PreviewPath = Path.GetDirectoryName(BaseFilePath) + "\\" +
                    section.GetStringValue("PreviewImage", Path.GetFileNameWithoutExtension(BaseFilePath) + ".png");
                Briefing = section.GetStringValue("Briefing", string.Empty).Replace("@", Environment.NewLine);
                SHA1 = Utilities.CalculateSHA1ForFile(CompleteFilePath);
                IsCoop = section.GetBooleanValue("IsCoopMission", false);
                Credits = section.GetIntValue("Credits", -1);
                UnitCount = section.GetIntValue("UnitCount", -1);
                NeutralHouseColor = section.GetIntValue("NeutralColor", -1);
                SpecialHouseColor = section.GetIntValue("SpecialColor", -1);
                MultiplayerOnly = section.GetBooleanValue("MultiplayerOnly", false);
                HumanPlayersOnly = section.GetBooleanValue("HumanPlayersOnly", false);
                ForceRandomStartLocations = section.GetBooleanValue("ForceRandomStartLocations", false);
                ForceNoTeams = section.GetBooleanValue("ForceNoTeams", false);
                ExtraININame = section.GetStringValue("ExtraININame", string.Empty);
                string bases = section.GetStringValue("Bases", string.Empty);
                if (!string.IsNullOrEmpty(bases))
                {
                    Bases = Convert.ToInt32(Conversions.BooleanFromString(bases, false));
                }

                if (IsCoop)
                {
                    CoopInfo = new CoopMapInfo();
                    string[] disallowedSides = section.GetStringValue("DisallowedPlayerSides", string.Empty).Split(
                        new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string sideIndex in disallowedSides)
                        CoopInfo.DisallowedPlayerSides.Add(int.Parse(sideIndex));

                    string[] disallowedColors = section.GetStringValue("DisallowedPlayerColors", string.Empty).Split(
                        new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string colorIndex in disallowedColors)
                        CoopInfo.DisallowedPlayerColors.Add(int.Parse(colorIndex));

                    CoopInfo.SetHouseInfos(section);
                }

                localSize = section.GetStringValue("LocalSize", "0,0,0,0").Split(',');
                actualSize = section.GetStringValue("Size", "0,0,0,0").Split(',');

                for (int i = 0; i < MAX_PLAYERS; i++)
                {
                    string waypoint = section.GetStringValue("Waypoint" + i, string.Empty);

                    if (String.IsNullOrEmpty(waypoint))
                        break;

                    waypoints.Add(waypoint);
                }

#if !WINDOWSGL
                if (UserINISettings.Instance.PreloadMapPreviews)
                    PreviewTexture = LoadPreviewTexture();
#endif
                // Parse forced options

                string forcedOptionsSections = iniFile.GetStringValue(BaseFilePath, "ForcedOptions", string.Empty);

                if (!string.IsNullOrEmpty(forcedOptionsSections))
                {
                    string[] sections = forcedOptionsSections.Split(',');
                    foreach (string foSection in sections)
                        ParseForcedOptions(iniFile, foSection);
                }

                string forcedSpawnIniOptionsSections = iniFile.GetStringValue(BaseFilePath, "ForcedSpawnIniOptions", string.Empty);

                if (!string.IsNullOrEmpty(forcedSpawnIniOptionsSections))
                {
                    string[] sections = forcedSpawnIniOptionsSections.Split(',');
                    foreach (string fsioSection in sections)
                        ParseSpawnIniOptions(iniFile, fsioSection);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log("Setting info for " + BaseFilePath + " failed! Reason: " + ex.Message);
                return false;
            }
        }

        public List<Point> GetStartingLocationPreviewCoords(Point previewSize)
        {
            if (startingLocations == null)
            {
                startingLocations = new List<Point>();

                foreach (string waypoint in waypoints)
                {
                    startingLocations.Add(GetWaypointCoords(waypoint, actualSize, localSize, previewSize));
                }
            }

            return startingLocations;
        }

        public void WriteInfoToIniSection(IniSection iniSection)
        {
            // TODO necessary for custom map caching
            throw new NotImplementedException();
        }

        /// <summary>
        /// Loads map information from a TS/RA2 map INI file.
        /// Returns true if succesful, otherwise false.
        /// </summary>
        /// <param name="path">The full path to the map INI file.</param>
        public bool SetInfoFromMap(string path)
        {
            if (!File.Exists(path))
                return false;

            try
            {
                IniFile iniFile = new IniFile();
                iniFile.FileName = path;
                iniFile.AddSection("Basic");
                iniFile.AddSection("Map");
                iniFile.AddSection("Waypoints");
                iniFile.AddSection("Preview");
                iniFile.AddSection("PreviewPack");
                iniFile.AddSection("ForcedOptions");
                iniFile.AddSection("ForcedSpawnIniOptions");
                iniFile.AllowNewSections = false;

                iniFile.Parse();

                mapIni = iniFile;

                var basicSection = iniFile.GetSection("Basic");

                Name = basicSection.GetStringValue("Name", "Unnamed map");
                Author = basicSection.GetStringValue("Author", "Unknown author");

                string gameModesString = basicSection.GetStringValue("GameModes", string.Empty);
                if (string.IsNullOrEmpty(gameModesString))
                {
                    gameModesString = basicSection.GetStringValue("GameMode", "Default");
                }

                GameModes = gameModesString.Split(',');

                if (GameModes.Length == 0)
                {
                    Logger.Log("Custom map " + path + " has no game modes!");
                    return false;
                }
                
                for (int i = 0; i < GameModes.Length; i++)
                {
                    string gameMode = GameModes[i].Trim();
                    GameModes[i] = gameMode.Substring(0, 1).ToUpperInvariant() + gameMode.Substring(1);
                }

                MinPlayers = 0;
                if (basicSection.KeyExists("ClientMaxPlayer"))
                    MaxPlayers = basicSection.GetIntValue("ClientMaxPlayer", 0);
                else
                    MaxPlayers = basicSection.GetIntValue("MaxPlayer", 0);
                EnforceMaxPlayers = basicSection.GetBooleanValue("EnforceMaxPlayers", true);
                //PreviewPath = Path.GetDirectoryName(BaseFilePath) + "\\" +
                //    iniFile.GetStringValue(BaseFilePath, "PreviewImage", Path.GetFileNameWithoutExtension(BaseFilePath) + ".png");
                Briefing = basicSection.GetStringValue("Briefing", string.Empty).Replace("@", Environment.NewLine);
                SHA1 = Utilities.CalculateSHA1ForFile(path);
                IsCoop = basicSection.GetBooleanValue("IsCoopMission", false);
                Credits = basicSection.GetIntValue("Credits", -1);
                UnitCount = basicSection.GetIntValue("UnitCount", -1);
                NeutralHouseColor = basicSection.GetIntValue("NeutralColor", -1);
                SpecialHouseColor = basicSection.GetIntValue("SpecialColor", -1);
                HumanPlayersOnly = basicSection.GetBooleanValue("HumanPlayersOnly", false);
                ForceRandomStartLocations = basicSection.GetBooleanValue("ForceRandomStartLocations", false);
                ForceNoTeams = basicSection.GetBooleanValue("ForceNoTeams", false);
                PreviewPath = Path.ChangeExtension(path.Substring(ProgramConstants.GamePath.Length), ".png");
                MultiplayerOnly = basicSection.GetBooleanValue("ClientMultiplayerOnly", false);

                string bases = basicSection.GetStringValue("Bases", string.Empty);
                if (!string.IsNullOrEmpty(bases))
                {
                    Bases = Convert.ToInt32(Conversions.BooleanFromString(bases, false));
                }

                if (IsCoop)
                {
                    CoopInfo = new CoopMapInfo();
                    string[] disallowedSides = iniFile.GetStringValue("Basic", "DisallowedPlayerSides", string.Empty).Split(
                        new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string sideIndex in disallowedSides)
                        CoopInfo.DisallowedPlayerSides.Add(int.Parse(sideIndex));

                    string[] disallowedColors = iniFile.GetStringValue("Basic", "DisallowedPlayerColors", string.Empty).Split(
                        new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string colorIndex in disallowedColors)
                        CoopInfo.DisallowedPlayerColors.Add(int.Parse(colorIndex));

                    CoopInfo.SetHouseInfos(basicSection);
                }

                localSize = iniFile.GetStringValue("Map", "LocalSize", "0,0,0,0").Split(',');
                actualSize = iniFile.GetStringValue("Map", "Size", "0,0,0,0").Split(',');

                for (int i = 0; i < MAX_PLAYERS; i++)
                {
                    string waypoint = mapIni.GetStringValue("Waypoints", i.ToString(), string.Empty);

                    if (string.IsNullOrEmpty(waypoint))
                        break;

                    waypoints.Add(waypoint);
                }

                ParseForcedOptions(iniFile, "ForcedOptions");
                ParseSpawnIniOptions(iniFile, "ForcedSpawnIniOptions");

                return true;
            }
            catch
            {
                Logger.Log("Loading custom map " + path + " failed!");
                return false;
            }
        }

        private void ParseForcedOptions(IniFile iniFile, string forcedOptionsSection)
        {
            List<string> keys = iniFile.GetSectionKeys(forcedOptionsSection);

            if (keys == null)
            {
                Logger.Log("Invalid ForcedOptions section \"" + forcedOptionsSection + "\" in map " + BaseFilePath);
                return;
            }

            foreach (string key in keys)
            {
                string value = iniFile.GetStringValue(forcedOptionsSection, key, String.Empty);

                int intValue = 0;
                if (Int32.TryParse(value, out intValue))
                {
                    ForcedDropDownValues.Add(new KeyValuePair<string, int>(key, intValue));
                }
                else
                {
                    ForcedCheckBoxValues.Add(new KeyValuePair<string, bool>(key, Conversions.BooleanFromString(value, false)));
                }
            }
        }

        private void ParseSpawnIniOptions(IniFile forcedOptionsIni, string spawnIniOptionsSection)
        {
            List<string> spawnIniKeys = forcedOptionsIni.GetSectionKeys(spawnIniOptionsSection);

            foreach (string key in spawnIniKeys)
            {
                ForcedSpawnIniOptions.Add(new KeyValuePair<string, string>(key, 
                    forcedOptionsIni.GetStringValue(spawnIniOptionsSection, key, String.Empty)));
            }
        }

        /// <summary>
        /// Loads and returns the map preview texture.
        /// </summary>
        public Texture2D LoadPreviewTexture()
        {
            if (File.Exists(ProgramConstants.GamePath + PreviewPath))
                return AssetLoader.LoadTextureUncached(PreviewPath);

            if (!Official)
            {
                // Extract preview from the map itself

                if (mapIni.GetSectionKeys("PreviewPack").Count == 0)
                {
                    Logger.Log(mapIni.FileName + " - no [PreviewPack] exists");
                    return AssetLoader.CreateTexture(Color.Black, 10, 10);
                }
                    
                if (mapIni.GetStringValue("PreviewPack", "1", string.Empty) ==
                    "yAsAIAXQ5PDQ5PDQ6JQATAEE6PDQ4PDI4JgBTAFEAkgAJyAATAG0AydEAEABpAJIA0wBVA")
                {
                    Logger.Log(mapIni.FileName + " - Hidden preview detected - not extracting.");
                    return AssetLoader.CreateTexture(Color.Black, 10, 10);
                }

                try
                {
                    var extractor = new MapThumbnailExtractor(mapIni, 1);
                    var bitmap = extractor.Get_Bitmap();

                    var texture = AssetLoader.TextureFromImage(bitmap);
                    if (texture != null)
                        return texture;
                }
                catch (Exception ex)
                {
                    Logger.Log(mapIni.FileName + " - Failed to extract preview from map: " + ex.Message);
                }
            }

            return AssetLoader.CreateTexture(Color.Black, 10, 10);
        }

        public IniFile GetMapIni()
        {
            var mapIni = new IniFile(CompleteFilePath);

            if (!string.IsNullOrEmpty(ExtraININame))
            {
                var extraIni = new IniFile(ProgramConstants.GamePath + "INI\\Map Code\\" + ExtraININame);
                IniFile.ConsolidateIniFiles(mapIni, extraIni);
            }

            return mapIni;
        }

        public void ApplySpawnIniCode(IniFile spawnIni, int totalPlayerCount, 
            int aiPlayerCount, int coopDifficultyLevel)
        {
            foreach (KeyValuePair<string, string> key in ForcedSpawnIniOptions)
                spawnIni.SetStringValue("Settings", key.Key, key.Value);

            if (Credits != -1)
                spawnIni.SetIntValue("Settings", "Credits", Credits);

            if (UnitCount != -1)
                spawnIni.SetIntValue("Settings", "UnitCount", UnitCount);

            int neutralHouseIndex = totalPlayerCount + 1;
            int specialHouseIndex = totalPlayerCount + 2;

            if (IsCoop)
            {
                var allyHouses = CoopInfo.AllyHouses;
                var enemyHouses = CoopInfo.EnemyHouses;

                int multiId = totalPlayerCount + 1;
                foreach (var houseInfo in allyHouses.Concat(enemyHouses))
                {
                    spawnIni.SetIntValue("HouseHandicaps", "Multi" + multiId, coopDifficultyLevel);
                    spawnIni.SetIntValue("HouseCountries", "Multi" + multiId, houseInfo.Side);
                    spawnIni.SetIntValue("HouseColors", "Multi" + multiId, houseInfo.Color);
                    spawnIni.SetIntValue("SpawnLocations", "Multi" + multiId, houseInfo.StartingLocation);

                    multiId++;
                }

                for (int i = 0; i < allyHouses.Count; i++)
                {
                    int aMultiId = totalPlayerCount + i + 1;

                    int allyIndex = 0;

                    // Write alliances
                    for (int pIndex = 0; pIndex < totalPlayerCount + allyHouses.Count; pIndex++)
                    {
                        int allyMultiIndex = pIndex;

                        if (pIndex == aMultiId - 1)
                            continue;

                        spawnIni.SetIntValue("Multi" + aMultiId + "_Alliances",
                            "HouseAlly" + HouseAllyIndexToString(allyIndex), allyMultiIndex);
                        spawnIni.SetIntValue("Multi" + (allyMultiIndex + 1) + "_Alliances",
                            "HouseAlly" + HouseAllyIndexToString(totalPlayerCount + i - 1), aMultiId - 1);
                        allyIndex++;
                    }
                }

                for (int i = 0; i < enemyHouses.Count; i++)
                {
                    int eMultiId = totalPlayerCount + allyHouses.Count + i + 1;

                    int allyIndex = 0;

                    // Write alliances
                    for (int enemyIndex = 0; enemyIndex < enemyHouses.Count; enemyIndex++)
                    {
                        int allyMultiIndex = totalPlayerCount + allyHouses.Count + enemyIndex;

                        if (enemyIndex == i)
                            continue;

                        spawnIni.SetIntValue("Multi" + eMultiId + "_Alliances",
                            "HouseAlly" + HouseAllyIndexToString(allyIndex), allyMultiIndex);
                        allyIndex++;
                    }
                }

                spawnIni.SetIntValue("Settings", "AIPlayers", aiPlayerCount +
                    allyHouses.Count + enemyHouses.Count);

                neutralHouseIndex += allyHouses.Count + enemyHouses.Count;
                specialHouseIndex += allyHouses.Count + enemyHouses.Count;
            }

            if (NeutralHouseColor > -1)
                spawnIni.SetIntValue("HouseColors", "Multi" + neutralHouseIndex, NeutralHouseColor);

            if (SpecialHouseColor > -1)
                spawnIni.SetIntValue("HouseColors", "Multi" + specialHouseIndex, SpecialHouseColor);

            if (Bases > -1)
                spawnIni.SetBooleanValue("Settings", "Bases", Convert.ToBoolean(Bases));
        }

        private static string HouseAllyIndexToString(int index)
        {
            string[] houseAllyIndexStrings = new string[]
            {
                "One",
                "Two",
                "Three",
                "Four",
                "Five",
                "Six",
                "Seven"
            };

            return houseAllyIndexStrings[index];
        }

        /// <summary>
        /// Converts a waypoint's coordinate string into pixel coordinates on the preview image.
        /// </summary>
        /// <returns>The waypoint's location on the map preview as a point.</returns>
        private static Point GetWaypointCoords(string waypoint, string[] actualSizeValues, string[] localSizeValues,
            Point previewSizePoint)
        {
            int xCoordIndex = waypoint.Length - 3;

            int ry = Convert.ToInt32(waypoint.Substring(0, xCoordIndex));
            int rx = Convert.ToInt32(waypoint.Substring(xCoordIndex));

            int isoTileX = rx - ry + Convert.ToInt32(actualSizeValues[2]) - 1;
            int isoTileY = rx + ry - Convert.ToInt32(actualSizeValues[2]) - 1;

            int pixelPosX = isoTileX * MainClientConstants.MAP_CELL_SIZE_X / 2;
            int pixelPosY = isoTileY * MainClientConstants.MAP_CELL_SIZE_Y / 2;

            pixelPosX = pixelPosX - (Convert.ToInt32(localSizeValues[0]) * MainClientConstants.MAP_CELL_SIZE_X);
            pixelPosY = pixelPosY - (Convert.ToInt32(localSizeValues[1]) * MainClientConstants.MAP_CELL_SIZE_Y);

            // Calculate map size
            int mapSizeX = Convert.ToInt32(localSizeValues[2]) * MainClientConstants.MAP_CELL_SIZE_X;
            int mapSizeY = Convert.ToInt32(localSizeValues[3]) * MainClientConstants.MAP_CELL_SIZE_Y;

            double ratioX = Convert.ToDouble(pixelPosX) / mapSizeX;
            double ratioY = Convert.ToDouble(pixelPosY) / mapSizeY;

            int x = Convert.ToInt32(ratioX * previewSizePoint.X);
            int y = Convert.ToInt32(ratioY * previewSizePoint.Y);

            return new Point(x, y);
        }

        public string GetSizeString()
        {
            if (actualSize == null || actualSize.Length < 4)
                return "Not available";
            return actualSize[2] + "x" + actualSize[3];
        }
    }
}
