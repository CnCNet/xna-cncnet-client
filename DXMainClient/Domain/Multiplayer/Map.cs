using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;

using ClientCore;
using ClientCore.Extensions;

using DTAClient.DXGUI.Multiplayer.GameLobby;

using Rampastring.Tools;

using SixLabors.ImageSharp;

using Point = Microsoft.Xna.Framework.Point;

namespace DTAClient.Domain.Multiplayer
{
    public struct ExtraMapPreviewTexture
    {
        public string TextureName;
        public Point Point;
        public int Level;
        public bool Toggleable;

        public ExtraMapPreviewTexture(string textureName, Point point, int level, bool toggleable)
        {
            TextureName = textureName;
            Point = point;
            Level = level;
            Toggleable = toggleable;
        }
    }

    /// <summary>
    /// A multiplayer map.
    /// </summary>
    public class Map : GameModeMapBase
    {
        [JsonConstructor]
        public Map(string baseFilePath)
            : this(baseFilePath, true)
        {
        }

        public Map(string baseFilePath, bool isCustomMap)
        {
            if (string.IsNullOrWhiteSpace(baseFilePath))
                throw new ArgumentNullException(nameof(baseFilePath));

            Debug.Assert(!baseFilePath.EndsWith($".{ClientConfiguration.Instance.MapFileExtension}", StringComparison.InvariantCultureIgnoreCase), $"Unexpected map path {baseFilePath}. It should not end with the map extension.");

            BaseFilePath = baseFilePath;
            customMapFilePath = isCustomMap
                ? SafePath.CombineFilePath(ProgramConstants.GamePath, FormattableString.Invariant($"{baseFilePath}.{ClientConfiguration.Instance.MapFileExtension}"))
                : null;
            Official = string.IsNullOrWhiteSpace(customMapFilePath);
        }

        /// <summary>
        /// The name of the map.
        /// </summary>
        [JsonIgnore]
        public string Name => !Official || string.IsNullOrEmpty(UntranslatedName) || string.IsNullOrEmpty(BaseFilePath)
            ? UntranslatedName
            : UntranslatedName.L10N($"INI:Maps:{BaseFilePath}:Description");

        /// <summary>
        /// The original untranslated name of the map.
        /// </summary>
        [JsonInclude]
        public string UntranslatedName
        {
            get => field;
            private set
            {
                field = value;
                // Force triggering localization of the name now
                _ = Name;
            }
        }

        /// <summary>
        /// If set, this map won't be automatically transferred over CnCNet when
        /// a player doesn't have it.
        /// </summary>
        [JsonIgnore]
        public bool Official { get; private set; }

        /// <summary>
        /// The briefing of the map.
        /// </summary>
        [JsonInclude]
        public string Briefing { get; private set; }

        /// <summary>
        /// The author of the map.
        /// </summary>
        [JsonInclude]
        public string Author { get; private set; }

        /// <summary>
        /// The calculated SHA1 hash of the map.
        /// </summary>
        [JsonInclude]
        public string SHA1 { get; private set; } = null;

        /// <summary>
        /// The path to the map file.
        /// </summary>
        [JsonInclude]
        public string BaseFilePath { get; private set; }

        /// <summary>
        /// Returns the complete path to the map file.
        /// Includes the game directory in the path.
        /// </summary>
        [JsonIgnore]
        public string CompleteFilePath => SafePath.CombineFilePath(ProgramConstants.GamePath, FormattableString.Invariant($"{BaseFilePath}.{ClientConfiguration.Instance.MapFileExtension}"));

        /// <summary>
        /// The file name of the preview image.
        /// </summary>
        [JsonInclude]
        public string PreviewPath { get; private set; }

        /// <summary>
        /// The game modes that the map is listed for.
        /// </summary>
        [JsonInclude]
        public string[] GameModes;

        /// <summary>
        /// The forced UnitCount for the map. -1 means none.
        /// </summary>
        [JsonInclude]
        public int UnitCount = -1;

        /// <summary>
        /// The forced starting credits for the map. -1 means none.
        /// </summary>
        [JsonInclude]
        public int Credits = -1;

        [JsonInclude]
        public int NeutralHouseColor = -1;

        [JsonInclude]
        public int SpecialHouseColor = -1;

        [JsonInclude]
        public int Bases = -1;

        [JsonInclude]
        public string[] localSize;

        [JsonInclude]
        public string[] actualSize;

        [JsonInclude]
        public int x;

        [JsonInclude]
        public int y;

        [JsonInclude]
        public int width;

        [JsonInclude]
        public int height;

        /// <summary>
        /// The full path of custom map INI file. It gets re-initialized in JsonConstructor, so it won't be serialized / deserialized directly.
        /// </summary>
        [JsonIgnore]
        private readonly string customMapFilePath;

        [JsonInclude]
        public List<string> waypoints = new List<string>();

        /// <summary>
        /// The pixel coordinates of the map's player starting locations.
        /// </summary>
        [JsonInclude]
        public List<Point> startingLocations;

        [JsonInclude]
        public List<TeamStartMappingPreset> TeamStartMappingPresets = new List<TeamStartMappingPreset>();

        [JsonIgnore]
        public List<TeamStartMapping> TeamStartMappings => TeamStartMappingPresets?.FirstOrDefault()?.TeamStartMappings;

        public void CalculateSHA()
        {
            SHA1 = Utilities.CalculateSHA1ForFile(CompleteFilePath);
        }

        [JsonInclude]
        public List<KeyValuePair<string, bool>> ForcedCheckBoxValues = new List<KeyValuePair<string, bool>>(0);

        [JsonInclude]
        public List<KeyValuePair<string, int>> ForcedDropDownValues = new List<KeyValuePair<string, int>>(0);

        [JsonIgnore]
        private List<ExtraMapPreviewTexture> extraTextures = new List<ExtraMapPreviewTexture>(0);

        public List<ExtraMapPreviewTexture> GetExtraMapPreviewTextures() => extraTextures;

        [JsonIgnore]
        private List<KeyValuePair<string, string>> ForcedSpawnIniOptions = new List<KeyValuePair<string, string>>(0);

        /// <summary>
        /// The name of an extra INI file in INI\Map Code\ that should be
        /// embedded into this map's INI code when a game is started.
        /// </summary>
        [JsonInclude]
        public string ExtraININame { get; private set; }

        /// <summary>
        /// This is used to load a map from the MPMaps.ini (default name) file.
        /// </summary>
        /// <param name="iniFile">The configuration file for the multiplayer maps.</param>
        /// <returns>True if loading the map succeeded, otherwise false.</returns>
        public bool InitializeFromMpMapsINI(IniFile iniFile)
        {
            try
            {
                string baseSectionName = iniFile.GetStringValue(BaseFilePath, "BaseSection", string.Empty);

                if (!string.IsNullOrEmpty(baseSectionName))
                    iniFile.CombineSections(baseSectionName, BaseFilePath);

                var section = iniFile.GetSection(BaseFilePath);

                UntranslatedName = section.GetStringValue("Description", "Unnamed map");

                Author = section.GetStringValue("Author", "Unknown author");
                GameModes = section.GetStringValue("GameModes", "Default").Split(',');

                // Initialize PreviewPath
                {
                    FileInfo mapFile = SafePath.GetFile(BaseFilePath);
                    string previewPath = SafePath.CombineFilePath(SafePath.GetDirectory(mapFile.FullName).Parent.FullName[ProgramConstants.GamePath.Length..], FormattableString.Invariant($"{section.GetStringValue("PreviewImage", mapFile.Name)}.png"));
                    if (!SafePath.GetFile(ProgramConstants.GamePath, previewPath).Exists)
                        previewPath = null;

                    PreviewPath = previewPath;
                }

                Briefing = section.GetStringValue("Briefing", string.Empty)
                    .FromIniString()
                    .L10N($"INI:Maps:{BaseFilePath}:Briefing");

                CalculateSHA();

                InitializeBaseSettingsFromIniSection(section, isCustomMap: false);

                Credits = section.GetIntValue("Credits", -1);
                UnitCount = section.GetIntValue("UnitCount", -1);
                NeutralHouseColor = section.GetIntValue("NeutralColor", -1);
                SpecialHouseColor = section.GetIntValue("SpecialColor", -1);

                string bases = section.GetStringValue("Bases", string.Empty);
                if (!string.IsNullOrEmpty(bases))
                {
                    Bases = Convert.ToInt32(Conversions.BooleanFromString(bases, false));
                }

                int i = 0;
                while (true)
                {
                    // Format example:
                    // ExtraTexture0=oilderrick.png,200,150,1,false
                    // Third value is optional map cell level, defaults to 0 if unspecified.
                    // Fourth value is optional boolean value that determines if the texture can be toggled on / off.
                    string value = section.GetStringValue("ExtraTexture" + i, null);

                    if (string.IsNullOrWhiteSpace(value))
                        break;

                    string[] parts = value.Split(',');

                    if (parts.Length is < 3 or > 5)
                    {
                        Logger.Log($"Invalid format for ExtraTexture{i} in map " + BaseFilePath);
                        continue;
                    }

                    bool success = int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x);
                    success &= int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y);

                    int level = 0;
                    bool toggleable = false;

                    if (parts.Length > 3)
                        int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out level);

                    if (parts.Length > 4)
                        toggleable = Conversions.BooleanFromString(parts[4], false);

                    extraTextures.Add(new ExtraMapPreviewTexture(parts[0], new Point(x, y), level, toggleable));

                    i++;
                }

                if (MainClientConstants.USE_ISOMETRIC_CELLS)
                {
                    localSize = section.GetStringValue("LocalSize", "0,0,0,0").Split(',');
                    actualSize = section.GetStringValue("Size", "0,0,0,0").Split(',');
                }
                else
                {
                    x = section.GetIntValue("X", 0);
                    y = section.GetIntValue("Y", 0);
                    width = section.GetIntValue("Width", 0);
                    height = section.GetIntValue("Height", 0);
                }

                for (i = 0; i < MAX_PLAYERS; i++)
                {
                    string waypoint = section.GetStringValue("Waypoint" + i, string.Empty);

                    if (string.IsNullOrEmpty(waypoint))
                        break;

                    Debug.Assert(int.TryParse(waypoint.Split(',')[0], out _), $"waypoint should be a number, got {waypoint}");
                    waypoints.Add(waypoint);
                }

                GetTeamStartMappingPresets(section);

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

                ExtraININame = section.GetStringValueOrNull("ExtraININame");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log("Setting info for " + BaseFilePath + " failed! Reason: " + ex.ToString());
                PreStartup.LogException(ex);
                return false;
            }
        }

        private void GetTeamStartMappingPresets(IniSection section)
        {
            TeamStartMappingPresets = new List<TeamStartMappingPreset>();
            for (int i = 0; ; i++)
            {
                try
                {
                    var teamStartMappingPreset = section.GetStringValue($"TeamStartMapping{i}", string.Empty);
                    if (string.IsNullOrEmpty(teamStartMappingPreset))
                        return; // mapping not found

                    var teamStartMappingPresetName = section.GetStringValue($"TeamStartMapping{i}Name", string.Empty);
                    if (string.IsNullOrEmpty(teamStartMappingPresetName))
                        continue; // mapping found, but no name specified

                    TeamStartMappingPresets.Add(new TeamStartMappingPreset()
                    {
                        Name = teamStartMappingPresetName,
                        TeamStartMappings = TeamStartMapping.FromListString(teamStartMappingPreset)
                    });
                }
                catch (Exception ex)
                {
                    Logger.Log($"Unable to parse team start mappings. Map: \"{Name}\", Error: {ex.Message}");
                    TeamStartMappingPresets = new List<TeamStartMappingPreset>();
                }
            }
        }

        public List<Point> GetStartingLocationPreviewCoords(Point previewSize)
        {
            if (startingLocations == null)
            {
                startingLocations = new List<Point>();

                foreach (string waypoint in waypoints)
                {
                    if (MainClientConstants.USE_ISOMETRIC_CELLS)
                        startingLocations.Add(GetIsometricWaypointCoords(waypoint, actualSize, localSize, previewSize));
                    else
                        startingLocations.Add(GetTDRAWaypointCoords(waypoint, x, y, width, height, previewSize));
                }
            }

            return startingLocations;
        }

        public Point MapPointToMapPreviewPoint(Point mapPoint, Point previewSize, int level)
        {
            if (MainClientConstants.USE_ISOMETRIC_CELLS)
                return GetIsoTilePixelCoord(mapPoint.X, mapPoint.Y, actualSize, localSize, previewSize, level);

            return GetTDRACellPixelCoord(mapPoint.X, mapPoint.Y, x, y, width, height, previewSize);
        }

        /// <summary>Returns the loaded INI file of a custom map.</summary>
        private IniFile GetCustomMapIniFile(bool loadPreviewTextureSection = true)
        {
            var customMapIni = new IniFile { FileName = SafePath.CombineFilePath(customMapFilePath) };
            customMapIni.AddSection("Basic");
            customMapIni.AddSection("Map");
            customMapIni.AddSection("Waypoints");
            customMapIni.AddSection("ForcedOptions");
            customMapIni.AddSection("ForcedSpawnIniOptions");

            // Optionally load preview sections, to accelerate building custom map caches without reading preview.
            if (loadPreviewTextureSection)
            {
                customMapIni.AddSection("Preview");
                customMapIni.AddSection("PreviewPack");
            }
            customMapIni.AllowNewSections = false;
            customMapIni.Parse();

            return customMapIni;
        }

        /// <summary>
        /// Loads map information from a TS/RA2 map INI file.
        /// Returns true if successful, otherwise false.
        /// </summary>
        public bool InitializeFromCustomMap()
        {
            if (!File.Exists(customMapFilePath))
                return false;

            try
            {
                IniFile iniFile = GetCustomMapIniFile(loadPreviewTextureSection: false);

                IniSection basicSection = iniFile.GetSection("Basic");

                UntranslatedName = basicSection.GetStringValue("Name", "Unnamed map");
                Author = basicSection.GetStringValue("Author", "Unknown author");

                string gameModesString = basicSection.GetStringValue("GameModes", string.Empty);
                if (string.IsNullOrEmpty(gameModesString))
                {
                    gameModesString = basicSection.GetStringValue("GameMode", "Default");
                }

                GameModes = gameModesString.Split(',');

                if (GameModes.Length == 0)
                {
                    Logger.Log("Custom map " + customMapFilePath + " has no game modes!");
                    return false;
                }

                for (int i = 0; i < GameModes.Length; i++)
                {
                    string gameMode = GameModes[i].Trim();
                    GameModes[i] = gameMode.Substring(0, 1).ToUpperInvariant() + gameMode.Substring(1);
                }

                Briefing = basicSection.GetStringValue("Briefing", string.Empty)
                    .FromIniString();

                CalculateSHA();

                InitializeBaseSettingsFromIniSection(basicSection, isCustomMap: true);

                Credits = basicSection.GetIntValue("Credits", -1);
                UnitCount = basicSection.GetIntValue("UnitCount", -1);
                NeutralHouseColor = basicSection.GetIntValue("NeutralColor", -1);
                SpecialHouseColor = basicSection.GetIntValue("SpecialColor", -1);

                // Initialize PreviewPath
                {
                    string previewPath = Path.ChangeExtension(customMapFilePath[ProgramConstants.GamePath.Length..], ".png");
                    if (!SafePath.GetFile(ProgramConstants.GamePath, previewPath).Exists)
                        previewPath = null;

                    PreviewPath = previewPath;
                }

                string bases = basicSection.GetStringValue("Bases", string.Empty);
                if (!string.IsNullOrEmpty(bases))
                {
                    Bases = Convert.ToInt32(Conversions.BooleanFromString(bases, false));
                }

                localSize = iniFile.GetStringValue("Map", "LocalSize", "0,0,0,0").Split(',');
                actualSize = iniFile.GetStringValue("Map", "Size", "0,0,0,0").Split(',');

                if (MainClientConstants.USE_ISOMETRIC_CELLS)
                {
                    localSize = iniFile.GetStringValue("Map", "LocalSize", "0,0,0,0").Split(',');
                    actualSize = iniFile.GetStringValue("Map", "Size", "0,0,0,0").Split(',');
                }
                else
                {
                    x = iniFile.GetIntValue("Map", "X", 0);
                    y = iniFile.GetIntValue("Map", "Y", 0);
                    width = iniFile.GetIntValue("Map", "Width", 0);
                    height = iniFile.GetIntValue("Map", "Height", 0);
                }

                for (int i = 0; i < MAX_PLAYERS; i++)
                {
                    string waypoint = iniFile.GetStringValue("Waypoints", i.ToString(CultureInfo.InvariantCulture), string.Empty);

                    if (string.IsNullOrEmpty(waypoint))
                        break;

                    waypoints.Add(waypoint);
                }

                GetTeamStartMappingPresets(basicSection);

                ParseForcedOptions(iniFile, "ForcedOptions");
                ParseSpawnIniOptions(iniFile, "ForcedSpawnIniOptions");

                ExtraININame = basicSection.GetStringValueOrNull("ExtraININame");

                return true;
            }
            catch
            {
                Logger.Log("Loading custom map " + customMapFilePath + " failed!");
                return false;
            }
        }

        // Ran after the map has been loaded from cache if it is a custom map.
        public void AfterDeserialize(bool recalculateSHA = true)
        {
            if (recalculateSHA)
            {
                // Instead of doing so, we should just remove the Map object from cache when the map file changes.
                // Otherwise, the metadata can be out of date.
                Debug.Assert(false, "The map SHA1 should not be recalculated after deserialization. Remove the Map object from cache when the map file changes instead.");
                CalculateSHA();
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
                string value = iniFile.GetStringValue(forcedOptionsSection, key, string.Empty);

                if (int.TryParse(value, out int intValue))
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
                    forcedOptionsIni.GetStringValue(spawnIniOptionsSection, key, string.Empty)));
            }
        }

        public bool IsImmediatePreviewImageAvailable() => !string.IsNullOrWhiteSpace(PreviewPath) && SafePath.GetFile(ProgramConstants.GamePath, PreviewPath).Exists;

        public Image GetImmediatePreviewImage() => IsImmediatePreviewImageAvailable()
            ? Image.Load(SafePath.GetFile(ProgramConstants.GamePath, PreviewPath).FullName)
            : throw new FileNotFoundException("Immediate preview texture not found for map " + BaseFilePath);

        public bool IsNonImmediatePreviewImageAvailable() => !string.IsNullOrWhiteSpace(customMapFilePath) && File.Exists(customMapFilePath);

        public Image GetNonImmediatePreviewImage()
        {
            if (!IsNonImmediatePreviewImageAvailable())
                throw new FileNotFoundException("Custom map file not found for map " + BaseFilePath);

            // Debug.WriteLine("Loading map preview from custom map INI for map " + BaseFilePath);

            return MapPreviewExtractor.ExtractMapPreview(GetCustomMapIniFile(loadPreviewTextureSection: true));
        }

        public IniFile GetMapIni()
        {
            Encoding mapIniEncoding = MapCodeHelper.GetMapEncoding(CompleteFilePath);

            var mapIni = new IniFile(CompleteFilePath, mapIniEncoding);

            if (!string.IsNullOrEmpty(ExtraININame))
            {
                string extraIniPath = SafePath.CombineFilePath(ProgramConstants.GamePath, "INI", "Map Code", ExtraININame);
                Encoding extraIniEncoding = MapCodeHelper.GetMapEncoding(extraIniPath);
                var extraIni = new IniFile(extraIniPath, extraIniEncoding);
                IniFile.ConsolidateIniFiles(mapIni, extraIni);
            }

            return mapIni;
        }

        public void ApplySpawnIniCode(IniFile spawnIni, int totalPlayerCount,
            int aiPlayerCount, bool isCoop, CoopMapInfo coopInfo, int coopDifficultyLevel, Random pseudoRandom, int sideCount)
        {
            foreach (KeyValuePair<string, string> key in ForcedSpawnIniOptions)
                spawnIni.SetStringValue("Settings", key.Key, key.Value);

            if (Credits != -1)
                spawnIni.SetIntValue("Settings", "Credits", Credits);

            if (UnitCount != -1)
                spawnIni.SetIntValue("Settings", "UnitCount", UnitCount);

            int neutralHouseIndex = totalPlayerCount + 1;
            int specialHouseIndex = totalPlayerCount + 2;

            if (isCoop)
            {
                int NextRandomSide() => pseudoRandom.Next(0, sideCount);

                var allyHouses = coopInfo.AllyHouses;
                var enemyHouses = coopInfo.EnemyHouses;

                int multiId = totalPlayerCount + 1;
                foreach (var houseInfo in allyHouses.Concat(enemyHouses))
                {
                    spawnIni.SetIntValue("HouseHandicaps", "Multi" + multiId, coopDifficultyLevel);
                    spawnIni.SetIntValue("HouseCountries", "Multi" + multiId, houseInfo.Side == -1 ? NextRandomSide() : houseInfo.Side);
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

                spawnIni.SetIntValue("Settings", "AIPlayers",
                    aiPlayerCount + allyHouses.Count + enemyHouses.Count);

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

        public string GetSizeString()
        {
            if (MainClientConstants.USE_ISOMETRIC_CELLS)
            {
                if (actualSize == null || actualSize.Length < 4)
                    return "Not available";

                return actualSize[2] + "x" + actualSize[3];
            }
            else
            {
                return width + "x" + height;
            }
        }

        private static Point GetTDRAWaypointCoords(string waypoint, int x, int y, int width, int height, Point previewSizePoint)
        {
            int waypointCoordsInt = Conversions.IntFromString(waypoint, -1);

            if (waypointCoordsInt < 0)
                return new Point(0, 0);

            // https://modenc.renegadeprojects.com/Waypoints
            int waypointX = waypointCoordsInt % MainClientConstants.TDRA_WAYPOINT_COEFFICIENT;
            int waypointY = waypointCoordsInt / MainClientConstants.TDRA_WAYPOINT_COEFFICIENT;

            return GetTDRACellPixelCoord(waypointX, waypointY, x, y, width, height, previewSizePoint);
        }

        private static Point GetTDRACellPixelCoord(int cellX, int cellY, int x, int y, int width, int height, Point previewSizePoint)
        {
            int rx = cellX - x;
            int ry = cellY - y;

            double ratioX = rx / (double)width;
            double ratioY = ry / (double)height;

            int pixelX = (int)(ratioX * previewSizePoint.X);
            int pixelY = (int)(ratioY * previewSizePoint.Y);

            return new Point(pixelX, pixelY);
        }

        /// <summary>
        /// Converts a waypoint's coordinate string into pixel coordinates on the preview image.
        /// </summary>
        /// <returns>The waypoint's location on the map preview as a point.</returns>
        private static Point GetIsometricWaypointCoords(string waypoint, string[] actualSizeValues, string[] localSizeValues,
            Point previewSizePoint)
        {
            string[] parts = waypoint.Split(',');

            int xCoordIndex = parts[0].Length - 3;

            int isoTileY = Convert.ToInt32(parts[0].Substring(0, xCoordIndex), CultureInfo.InvariantCulture);
            int isoTileX = Convert.ToInt32(parts[0].Substring(xCoordIndex), CultureInfo.InvariantCulture);

            int level = 0;

            if (parts.Length > 1)
                level = Conversions.IntFromString(parts[1], 0);

            return GetIsoTilePixelCoord(isoTileX, isoTileY, actualSizeValues, localSizeValues, previewSizePoint, level);
        }

        private static Point GetIsoTilePixelCoord(int isoTileX, int isoTileY, string[] actualSizeValues, string[] localSizeValues, Point previewSizePoint, int level)
        {
            int rx = isoTileX - isoTileY + Convert.ToInt32(actualSizeValues[2], CultureInfo.InvariantCulture) - 1;
            int ry = isoTileX + isoTileY - Convert.ToInt32(actualSizeValues[2], CultureInfo.InvariantCulture) - 1;

            int pixelPosX = rx * MainClientConstants.MAP_CELL_SIZE_X / 2;
            int pixelPosY = ry * MainClientConstants.MAP_CELL_SIZE_Y / 2 - level * MainClientConstants.MAP_CELL_SIZE_Y / 2;

            pixelPosX = pixelPosX - (Convert.ToInt32(localSizeValues[0], CultureInfo.InvariantCulture) * MainClientConstants.MAP_CELL_SIZE_X);
            pixelPosY = pixelPosY - (Convert.ToInt32(localSizeValues[1], CultureInfo.InvariantCulture) * MainClientConstants.MAP_CELL_SIZE_Y);

            // Calculate map size
            int mapSizeX = Convert.ToInt32(localSizeValues[2], CultureInfo.InvariantCulture) * MainClientConstants.MAP_CELL_SIZE_X;
            int mapSizeY = Convert.ToInt32(localSizeValues[3], CultureInfo.InvariantCulture) * MainClientConstants.MAP_CELL_SIZE_Y;

            double ratioX = Convert.ToDouble(pixelPosX) / mapSizeX;
            double ratioY = Convert.ToDouble(pixelPosY) / mapSizeY;

            int pixelX = Convert.ToInt32(ratioX * previewSizePoint.X);
            int pixelY = Convert.ToInt32(ratioY * previewSizePoint.Y);

            return new Point(pixelX, pixelY);
        }

        /// <summary>
        /// Opens the folder containing this map in the system file manager and selects the map file.
        /// </summary>
        public void OpenContainingFolder()
        {
            FileInfo mapFileInfo = SafePath.GetFile(CompleteFilePath);
            if (!mapFileInfo.Exists)
                return;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // https://stackoverflow.com/questions/13680415/how-to-open-explorer-with-a-specific-file-selected
                ProcessLauncher.StartShellProcess("explorer.exe", $"/select,\"{mapFileInfo.FullName}\"");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // https://stackoverflow.com/questions/39214539/opening-finder-from-terminal-with-file-selected
                ProcessLauncher.StartShellProcess("open", $"-R \"{mapFileInfo.FullName}\"");
            }
            else
            {
                // Linux: no standard way to select a file, just open the folder
                ProcessLauncher.StartShellProcess(mapFileInfo.Directory?.FullName);
            }
        }

        public override bool Equals(object other)
        {
            if (other is Map otherMap)
            {
                Debug.Assert(otherMap?.SHA1 != null || SHA1 != null);
                return string.Equals(SHA1, otherMap?.SHA1, StringComparison.InvariantCultureIgnoreCase);
            }

            return false;
        }

        public override int GetHashCode() => SHA1 != null ? StringComparer.InvariantCultureIgnoreCase.GetHashCode(SHA1) : 0;

        public static bool operator ==(Map left, Map right) => left is null ? right is null : left.Equals(right);

        public static bool operator !=(Map left, Map right) => !(left == right);
    }
}
