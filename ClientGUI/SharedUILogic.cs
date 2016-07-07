/// @author Rampastring
/// http://www.moddb.com/members/rampastring

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using ClientCore;
using ClientCore.CnCNet5;
using Rampastring.Tools;
using Utilities = ClientCore.Utilities;

namespace ClientGUI
{
    /// <summary>
    /// A static class holding UI-related functions useful for both the Skirmish and the CnCNet Game lobby.
    /// </summary>
    public static class SharedUILogic
    {
        public delegate void GameProcessStartedEventHandler();
        public static event GameProcessStartedEventHandler GameProcessStarted;

        public delegate void GameProcessExitedEventHandler();
        public static event GameProcessExitedEventHandler GameProcessExited; 

        public const int COOP_BRIEFING_WIDTH = 488;
        const int COOP_BRIEFING_HEIGHT = 200;

        public static Font CoopBriefingFont;

        /// <summary>
        /// Gathers the list of allowed sides.
        /// </summary>
        /// <param name="comboBoxes">ComboBoxes of the user interface.</param>
        /// <param name="sideComboboxPrerequisites">SideComboboxPrerequisites.</param>
        /// <returns>A list of allowed side indexes.</returns>
        public static List<int> GetAllowedSides(List<LimitedComboBox> comboBoxes,
            List<UserCheckBox> checkBoxes,
            List<SideComboboxPrerequisite> sideComboboxPrerequisites, 
            SideCheckboxPrerequisite[] sideCheckboxPrerequisites)
        {
            // Check which sides are pickable (prevent Allies and Soviet in Classic Mode)
            List<int> AllowedSidesToRandomizeTo = new List<int>();
            for (int rSideId = 0; rSideId < sideComboboxPrerequisites.Count; rSideId++)
            {
                SideComboboxPrerequisite prereq = sideComboboxPrerequisites[rSideId];
                if (prereq.Valid)
                {
                    int cmbId = prereq.ComboBoxId;
                    int indexId = prereq.RequiredIndexId;

                    if (comboBoxes[cmbId].SelectedIndex != indexId)
                        continue;
                }

                SideCheckboxPrerequisite cbPrereq = sideCheckboxPrerequisites[rSideId];
                if (cbPrereq.Valid)
                {
                    int chkId = cbPrereq.CheckBoxIndex;
                    bool requiredValue = cbPrereq.RequiredValue;

                    if (checkBoxes[chkId].Checked != requiredValue)
                        continue;
                }

                AllowedSidesToRandomizeTo.Add(rSideId);
            }

            return AllowedSidesToRandomizeTo;
        }

        /// <summary>
        /// Randomizes player options.
        /// </summary>
        /// <param name="players">List of human players in the game.</param>
        /// <param name="aiPlayers">List of AI players in the game.</param>
        /// <param name="map">The map.</param>
        /// <param name="seed">The seed number used for randomizing.</param>
        /// <param name="PlayerSides">List of player side indexes.</param>
        /// <param name="isPlayerSpectator">Determines whether a player at a specific index is a spectator.</param>
        /// <param name="PlayerColors">List of player color indexes.</param>
        /// <param name="PlayerStartingLocs">The list of player starting location indexes.</param>
        /// <param name="AllowedSidesToRandomizeTo">List of allowed sides.</param>
        /// <param name="sideCount">The amount of sides in the game.</param>
        public static void Randomize(List<PlayerInfo> players, List<PlayerInfo> aiPlayers, Map map, int seed,
            List<int> PlayerSides, List<bool> isPlayerSpectator, List<int> PlayerColors, List<int> PlayerStartingLocs,
            List<int> AllowedSidesToRandomizeTo, int sideCount)
        {
            Random random = new Random(seed);

            Logger.Log("Randomizing sides.");

            int previousSide = AllowedSidesToRandomizeTo[random.Next(0, AllowedSidesToRandomizeTo.Count)];

            foreach (PlayerInfo player in players)
            {
                if (player.SideId == 0)
                {
                    int side = AllowedSidesToRandomizeTo[random.Next(0, AllowedSidesToRandomizeTo.Count)];
                    PlayerSides.Add(side);
                    previousSide = side;
                    isPlayerSpectator.Add(false);
                }
                else if (player.SideId == sideCount + 1)
                {
                    PlayerSides.Add(previousSide);
                    isPlayerSpectator.Add(true);
                }
                else
                {
                    PlayerSides.Add(player.SideId - 1);
                    previousSide = player.SideId - 1;
                    isPlayerSpectator.Add(false);
                }
            }

            for (int pId = 0; pId < players.Count; pId++)
            {
                if (isPlayerSpectator[pId])
                {
                    PlayerSides[pId] = previousSide;
                }
            }

            Logger.Log("Randomizing AI sides.");

            foreach (PlayerInfo player in aiPlayers)
            {
                if (player.SideId == 0)
                {
                    PlayerSides.Add(AllowedSidesToRandomizeTo[random.Next(0, AllowedSidesToRandomizeTo.Count)]);
                    isPlayerSpectator.Add(false);
                }
                else if (player.SideId == sideCount + 1)
                {
                    PlayerSides.Add(0);
                    isPlayerSpectator.Add(true);
                }
                else
                {
                    PlayerSides.Add(player.SideId - 1);
                    isPlayerSpectator.Add(false);
                }
            }

            Logger.Log("Generated sides:");
            for (int pid = 0; pid < players.Count; pid++)
            {
                Logger.Log("PlayerID " + pid + ": sideId " + PlayerSides[pid]);
            }

            List<int> freeColors = new List<int>();
            for (int cId = 1; cId < 9; cId++)
                freeColors.Add(cId);

            for (int pId = 0; pId < players.Count; pId++)
                freeColors.Remove(players[pId].ColorId);

            for (int aiId = 0; aiId < aiPlayers.Count; aiId++)
                freeColors.Remove(aiPlayers[aiId].ColorId);

            // Randomize colors

            Logger.Log("Randomizing colors.");

            foreach (PlayerInfo player in players)
            {
                if (player.ColorId == 0 && player.ForcedColor == 0)
                {
                    int randomizedColorIndex = random.Next(0, freeColors.Count);

                    if (randomizedColorIndex > -1)
                    {
                        PlayerColors.Add(freeColors[randomizedColorIndex] - 1);
                        freeColors.RemoveAt(randomizedColorIndex);
                    }
                    else
                        throw new Exception("Unable to find valid color for player " + player.Name);
                }
                else if (player.ForcedColor > 0)
                {
                    PlayerColors.Add(player.ForcedColor);
                    Logger.Log("Forced Color for " + player.Name + ": " + player.ForcedColor);
                }
                else
                    PlayerColors.Add(player.ColorId - 1);
            }

            Logger.Log("Randomizing AI colors.");

            foreach (PlayerInfo player in aiPlayers)
            {
                if (player.ColorId == 0)
                {
                    int randomizedColorIndex = random.Next(0, freeColors.Count);

                    if (randomizedColorIndex > -1)
                    {
                        PlayerColors.Add(freeColors[randomizedColorIndex] - 1);
                        freeColors.RemoveAt(randomizedColorIndex);
                    }
                    else
                        PlayerColors.Add(0);
                }
                else
                    PlayerColors.Add(player.ColorId - 1);
            }

            List<int> freeStartingLocs = new List<int>();

            for (int sId = 1; sId <= map.AmountOfPlayers; sId++)
                freeStartingLocs.Add(sId);

            for (int pId = 0; pId < players.Count; pId++)
            {
                if (!isPlayerSpectator[pId])
                    freeStartingLocs.Remove(players[pId].StartingLocation);
            }

            for (int aiId = 0; aiId < aiPlayers.Count; aiId++)
                freeStartingLocs.Remove(aiPlayers[aiId].StartingLocation);

            // Randomize starting locations

            Logger.Log("Randomizing starting locations.");

            int sLocPId = 0;

            foreach (PlayerInfo player in players)
            {
                sLocPId++;

                if (isPlayerSpectator[sLocPId - 1])
                {
                    PlayerStartingLocs.Add(9);
                    continue;
                }

                if (player.StartingLocation == 0)
                {
                    if (freeStartingLocs.Count > 1)
                    {
                        int index = random.Next(0, freeStartingLocs.Count);
                        PlayerStartingLocs.Add(freeStartingLocs[index] - 1);
                        freeStartingLocs.RemoveAt(index);
                    }
                    else if (freeStartingLocs.Count == 1)
                    {
                        PlayerStartingLocs.Add(freeStartingLocs[0] - 1);
                        freeStartingLocs.RemoveAt(0);
                    }
                    else // if freeStartingLocs.Count == 0
                    {
                        PlayerStartingLocs.Add(random.Next(0, map.AmountOfPlayers));
                    }
                }
                else
                    PlayerStartingLocs.Add(player.StartingLocation - 1);
            }

            Logger.Log("Randomizing AI starting locations.");

            foreach (PlayerInfo player in aiPlayers)
            {
                if (player.StartingLocation == 0)
                {
                    if (freeStartingLocs.Count > 1)
                    {
                        int index = random.Next(0, freeStartingLocs.Count);
                        PlayerStartingLocs.Add(freeStartingLocs[index] - 1);
                        freeStartingLocs.RemoveAt(index);
                    }
                    else if (freeStartingLocs.Count == 1)
                    {
                        PlayerStartingLocs.Add(freeStartingLocs[0] - 1);
                        freeStartingLocs.RemoveAt(0);
                    }
                    else // if freeStartingLocs.Count == 0
                    {
                        PlayerStartingLocs.Add(random.Next(0, map.AmountOfPlayers));
                    }
                }
                else
                    PlayerStartingLocs.Add(player.StartingLocation - 1);
            }
        }

        /// <summary>
        /// Writes spawn.ini. See NGameLobby and SkirmishLobby for usage examples.
        /// </summary>
        public static void WriteSpawnIni(List<PlayerInfo> players, List<PlayerInfo> aiPlayers, Map map, string gameMode, int seed, 
            bool isHost, List<int> playerPorts, string tunnelAddress, int tunnelPort,
            List<LimitedComboBox> ComboBoxes, List<UserCheckBox> CheckBoxes, List<string> AssociatedCheckBoxSpawnIniOptions, 
            List<string> AssociatedComboBoxSpawnIniOptions, List<DataWriteMode> ComboBoxDataWriteModes, 
            List<int> PlayerSides, List<bool> isPlayerSpectator, List<int> PlayerColors, List<int> PlayerStartingLocs,
            out List<int> MultiCmbIndexes, int gameId)
        {
            File.Delete(ProgramConstants.GamePath + ProgramConstants.SPAWNMAP_INI);
            File.Delete(ProgramConstants.GamePath + ProgramConstants.SPAWNER_SETTINGS);

            string mapCodePath = map.Path.Substring(0, map.Path.Length - 3) + "ini";
            IniFile mapCodeIni = new IniFile(mapCodePath);

            if (map.IsCoop)
            {
                foreach (PlayerInfo pInfo in players)
                    pInfo.TeamId = 1;

                foreach (PlayerInfo pInfo in aiPlayers)
                    pInfo.TeamId = 1;
            }

            int iNumLoadingScreens = DomainController.Instance().GetLoadScreenCount();

            // Write spawn.ini
            StreamWriter sw = new StreamWriter(File.Create(ProgramConstants.GamePath + ProgramConstants.SPAWNER_SETTINGS));
            sw.WriteLine("[Settings]");
            sw.WriteLine("Name=" + ProgramConstants.PLAYERNAME);
            sw.WriteLine("Scenario=" + ProgramConstants.SPAWNMAP_INI);
            sw.WriteLine("UIGameMode=" + gameMode);
            sw.WriteLine("UIMapName=" + map.Name);
            sw.WriteLine("PlayerCount=" + players.Count);
            int myIndex = players.FindIndex(c => c.Name == ProgramConstants.PLAYERNAME);
            int sideId = PlayerSides[myIndex];
            sw.WriteLine("Side=" + sideId);
            sw.WriteLine("IsSpectator=" + isPlayerSpectator[myIndex]);
            sw.WriteLine("Color=" + PlayerColors[myIndex]);
            sw.WriteLine("CustomLoadScreen=" + LoadingScreenController.GetLoadScreenName(sideId));
            sw.WriteLine("AIPlayers=" + aiPlayers.Count);
            sw.WriteLine("Host=" + isHost);
            sw.WriteLine("Seed=" + seed);
            sw.WriteLine("GameID=" + gameId);

            for (int chkId = 0; chkId < CheckBoxes.Count; chkId++)
            {
                string option = AssociatedCheckBoxSpawnIniOptions[chkId];
                if (option != "none")
                {
                    Logger.Log("CheckBox " + CheckBoxes[chkId].Name + " associated spawn.ini option: " + option);
                    if (!CheckBoxes[chkId].Reversed)
                        sw.WriteLine(option + "=" + CheckBoxes[chkId].Checked);
                    else
                        sw.WriteLine(option + "=" + !CheckBoxes[chkId].Checked);
                }
            }
            for (int cmbId = 0; cmbId < ComboBoxes.Count; cmbId++)
            {
                DataWriteMode dwMode = ComboBoxDataWriteModes[cmbId];
                string option = AssociatedComboBoxSpawnIniOptions[cmbId];
                if (option != "none")
                {
                    Logger.Log("ComboBox " + ComboBoxes[cmbId].Name + " associated spawn.ini option: " + option);

                    if (dwMode == DataWriteMode.BOOLEAN)
                    {
                        if (ComboBoxes[cmbId].SelectedIndex > 0)
                            sw.WriteLine(option + "=Yes");
                        else
                            sw.WriteLine(option + "=No");
                    }
                    else if (dwMode == DataWriteMode.INDEX)
                    {
                        sw.WriteLine(option + "=" + ComboBoxes[cmbId].SelectedIndex);
                    }
                    else // if dwMode == DataWriteMode.String
                    {
                        sw.WriteLine(option + "=" + ComboBoxes[cmbId].SelectedItem.ToString());
                    }
                }
            }

            Logger.Log("Writing forced spawn.ini options from GameOptions.ini.");

            IniFile goIni = DomainController.Instance().gameOptions_ini;

            if (goIni.SectionExists("ForcedSpawnIniOptions"))
            {
                List<string> keys = goIni.GetSectionKeys("ForcedSpawnIniOptions");
                foreach (string key in keys)
                {
                    sw.WriteLine(key + "=" + goIni.GetStringValue("ForcedSpawnIniOptions", key, "give me a value, noob"));
                }
            }
            sw.Close();

            Logger.Log("Writing game mode forced spawn.ini options from INI\\" + gameMode + "_spawn.ini");

            IniFile spawnIni = new IniFile(ProgramConstants.GamePath + ProgramConstants.SPAWNER_SETTINGS);

            if (File.Exists(ProgramConstants.GamePath + "INI\\" + gameMode + "_spawn.ini"))
            {
                IniFile gameModeSpawnIni = new IniFile(ProgramConstants.GamePath + "INI\\" + gameMode + "_spawn.ini");
                if (gameModeSpawnIni.SectionExists("Settings"))
                {
                    List<string> keys = gameModeSpawnIni.GetSectionKeys("Settings");
                    foreach (string key in keys)
                    {
                        spawnIni.SetStringValue("Settings", key, gameModeSpawnIni.GetStringValue("Settings", key, "give me a value, noob"));
                    }
                }
                else
                    Logger.Log("WARNING: Game mode spawn.ini options file doesn't contain the Settings section. Ignoring.");
            }
            else
                Logger.Log("WARNING: Game mode spawn.ini options file doesn't exist.");

            Logger.Log("Writing forced spawn.ini options from the map settings INI file.");

            if (mapCodeIni.SectionExists("ForcedSpawnIniOptions"))
            {
                List<string> keys = mapCodeIni.GetSectionKeys("ForcedSpawnIniOptions");
                foreach (string key in keys)
                {
                    spawnIni.SetStringValue("Settings", key, mapCodeIni.GetStringValue("ForcedSpawnIniOptions", key, "No value specified!"));
                }
            }

            spawnIni.WriteIniFile();

            StreamWriter sw2 = new StreamWriter(File.OpenWrite(ProgramConstants.GamePath + ProgramConstants.SPAWNER_SETTINGS));
            sw2.BaseStream.Position = sw2.BaseStream.Length - 1;
            sw2.WriteLine();
            sw2.WriteLine();
            if (players.Count > 1)
            {
                sw2.WriteLine("Port=" + playerPorts[players.FindIndex(c => c.Name == ProgramConstants.PLAYERNAME)]);
                sw2.WriteLine();
                if (!String.IsNullOrEmpty(tunnelAddress))
                {
                    sw2.WriteLine("[Tunnel]");
                    sw2.WriteLine("Ip=" + tunnelAddress);
                    sw2.WriteLine("Port=" + tunnelPort);
                    sw2.WriteLine();
                }
            }

            int otherId = 1;

            for (int pId = 0; pId < players.Count; pId++)
            {
                if (players[pId].Name != ProgramConstants.PLAYERNAME)
                {
                    sw2.WriteLine("[Other" + otherId + "]");
                    sw2.WriteLine("Name=" + players[pId].Name);
                    sw2.WriteLine("Side=" + PlayerSides[pId]);
                    sw2.WriteLine("IsSpectator=" + isPlayerSpectator[pId]);
                    sw2.WriteLine("Color=" + PlayerColors[pId]);
                    if (String.IsNullOrEmpty(tunnelAddress))
                        sw2.WriteLine("Ip=" + players[pId].IPAddress);
                    else
                        sw2.WriteLine("Ip=0.0.0.0");
                    sw2.WriteLine("Port=" + playerPorts[pId]);
                    otherId++;
                    sw2.WriteLine();
                }
            }

            // Create the list of MultiX indexes according to color indexes
            MultiCmbIndexes = new List<int>();

            for (int cId = 0; cId < 8; cId++)
            {
                for (int pId = 0; pId < players.Count; pId++)
                {
                    if (PlayerColors[pId] == cId)
                        MultiCmbIndexes.Add(pId);
                }
            }

            // Secret color logic ;)
            for (int pId = 0; pId < players.Count; pId++)
            {
                if (PlayerColors[pId] > 7)
                    MultiCmbIndexes.Add(pId);
            }

            if (aiPlayers.Count > 0)
            {
                sw2.WriteLine("[HouseHandicaps]");
                int multiId = MultiCmbIndexes.Count + 1;

                for (int aiId = 0; aiId < aiPlayers.Count; aiId++)
                {
                    if (aiPlayers[aiId].Name.Contains("Easy"))
                        sw2.WriteLine(string.Format("Multi{0}=2", multiId));
                    else if (aiPlayers[aiId].Name.Contains("Medium"))
                        sw2.WriteLine(string.Format("Multi{0}=1", multiId));
                    else // if (aiPlayers[aiId].Name.Contains("Hard"))
                        sw2.WriteLine(string.Format("Multi{0}=0", multiId));

                    multiId++;
                }

                sw2.WriteLine();

                sw2.WriteLine("[HouseCountries]");
                multiId = MultiCmbIndexes.Count + 1;

                for (int aiId = 0; aiId < aiPlayers.Count; aiId++)
                {
                    sw2.WriteLine(string.Format("Multi{0}={1}", multiId, PlayerSides[players.Count + aiId]));
                    multiId++;
                }

                sw2.WriteLine();

                sw2.WriteLine("[HouseColors]");
                multiId = MultiCmbIndexes.Count + 1;

                for (int aiId = 0; aiId < aiPlayers.Count; aiId++)
                {
                    sw2.WriteLine(string.Format("Multi{0}={1}", multiId, PlayerColors[players.Count + aiId]));
                    multiId++;
                }
            }

            sw2.WriteLine();

            sw2.WriteLine("[IsSpectator]");
            for (int multiId = 0; multiId < MultiCmbIndexes.Count; multiId++)
            {
                int pIndex = MultiCmbIndexes[multiId];
                if (isPlayerSpectator[pIndex])
                    sw2.WriteLine(string.Format("Multi{0}=Yes", multiId + 1));
            }

            sw2.WriteLine();

            // Set alliances

            List<int> Team1MultiMemberIds = new List<int>();
            List<int> Team2MultiMemberIds = new List<int>();
            List<int> Team3MultiMemberIds = new List<int>();
            List<int> Team4MultiMemberIds = new List<int>();

            for (int pId = 0; pId < players.Count; pId++)
            {
                int teamId = players[pId].TeamId;

                if (teamId > 0)
                {
                    switch (teamId)
                    {
                        case 1:
                            Team1MultiMemberIds.Add(MultiCmbIndexes.FindIndex(c => c == pId) + 1);
                            break;
                        case 2:
                            Team2MultiMemberIds.Add(MultiCmbIndexes.FindIndex(c => c == pId) + 1);
                            break;
                        case 3:
                            Team3MultiMemberIds.Add(MultiCmbIndexes.FindIndex(c => c == pId) + 1);
                            break;
                        case 4:
                            Team4MultiMemberIds.Add(MultiCmbIndexes.FindIndex(c => c == pId) + 1);
                            break;
                    }
                }
            }

            int mId2 = MultiCmbIndexes.Count + 1;

            for (int aiId = 0; aiId < aiPlayers.Count; aiId++)
            {
                int teamId = aiPlayers[aiId].TeamId;

                if (teamId > 0)
                {
                    switch (teamId)
                    {
                        case 1:
                            Team1MultiMemberIds.Add(mId2);
                            break;
                        case 2:
                            Team2MultiMemberIds.Add(mId2);
                            break;
                        case 3:
                            Team3MultiMemberIds.Add(mId2);
                            break;
                        case 4:
                            Team4MultiMemberIds.Add(mId2);
                            break;
                    }
                }

                mId2++;
            }

            foreach (int houseId in Team1MultiMemberIds)
            {
                sw2.WriteLine("[Multi" + houseId + "_Alliances]");
                bool selfFound = false;

                for (int allyId = 0; allyId < Team1MultiMemberIds.Count; allyId++)
                {
                    int allyHouseId = Team1MultiMemberIds[allyId];

                    if (allyHouseId == houseId)
                        selfFound = true;
                    else
                    {
                        sw2.WriteLine(string.Format("HouseAlly{0}={1}", getHouseAllyIndexFromInt(allyId, selfFound), allyHouseId - 1));
                    }
                }

                sw2.WriteLine();
            }

            foreach (int houseId in Team2MultiMemberIds)
            {
                sw2.WriteLine("[Multi" + houseId + "_Alliances]");
                bool selfFound = false;

                for (int allyId = 0; allyId < Team2MultiMemberIds.Count; allyId++)
                {
                    int allyHouseId = Team2MultiMemberIds[allyId];

                    if (allyHouseId == houseId)
                        selfFound = true;
                    else
                    {
                        sw2.WriteLine(string.Format("HouseAlly{0}={1}", getHouseAllyIndexFromInt(allyId, selfFound), allyHouseId - 1));
                    }
                }

                sw2.WriteLine();
            }

            foreach (int houseId in Team3MultiMemberIds)
            {
                sw2.WriteLine("[Multi" + houseId + "_Alliances]");
                bool selfFound = false;

                for (int allyId = 0; allyId < Team3MultiMemberIds.Count; allyId++)
                {
                    int allyHouseId = Team3MultiMemberIds[allyId];

                    if (allyHouseId == houseId)
                        selfFound = true;
                    else
                    {
                        sw2.WriteLine(string.Format("HouseAlly{0}={1}", getHouseAllyIndexFromInt(allyId, selfFound), allyHouseId - 1));
                    }
                }

                sw2.WriteLine();
            }

            foreach (int houseId in Team4MultiMemberIds)
            {
                sw2.WriteLine("[Multi" + houseId + "_Alliances]");
                bool selfFound = false;

                for (int allyId = 0; allyId < Team4MultiMemberIds.Count; allyId++)
                {
                    int allyHouseId = Team4MultiMemberIds[allyId];

                    if (allyHouseId == houseId)
                        selfFound = true;
                    else
                    {
                        sw2.WriteLine(string.Format("HouseAlly{0}={1}", getHouseAllyIndexFromInt(allyId, selfFound), allyHouseId - 1));
                    }
                }

                sw2.WriteLine();
            }

            mId2 = 1;

            sw2.WriteLine("[SpawnLocations]");
            for (int pId = 0; pId < players.Count; pId++)
            {
                sw2.WriteLine(string.Format("Multi{0}={1}", mId2, PlayerStartingLocs[MultiCmbIndexes[pId]]));
                mId2++;
            }

            for (int aiId = 0; aiId < aiPlayers.Count; aiId++)
            {
                sw2.WriteLine(string.Format("Multi{0}={1}", mId2, PlayerStartingLocs[players.Count + aiId]));
                mId2++;
            }

            sw2.WriteLine();
            sw2.WriteLine();

            sw2.Close();
        }

        private static string getHouseAllyIndexFromInt(int allyId, bool selfFound)
        {
            if (selfFound)
                allyId = allyId - 1;

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
                case 6:
                    return "Seven";
            }

            return "None" + allyId;
        }

        /// <summary>
        /// Prepares and writes the actual map file (spawnmap.map) depending on various settings
        /// (game mode and selected CheckBox options).
        /// </summary>
        /// <param name="mapIni">The game map loaded into a IniFile class.</param>
        /// <param name="gameMode">The name of the selected game mode.</param>
        /// <param name="CheckBoxes">A list of check boxes.</param>
        /// <param name="AssociatedCheckBoxCustomInis">Custom INI files associated with a specific check box.</param>
        public static void WriteMap(string gameMode, List<UserCheckBox> CheckBoxes,
            List<string> AssociatedCheckBoxCustomInis, IniFile mapIni)
        {
            Logger.Log("Writing map.");

            IniFile globalCodeIni = new IniFile(ProgramConstants.GamePath + "INI\\GlobalCode.ini");

            IniFile gameModeIni = new IniFile(ProgramConstants.GamePath + "INI\\" + gameMode + ".ini");

            Logger.Log("Consolidating INI files.");

            IniFile.ConsolidateIniFiles(mapIni, gameModeIni);

            IniFile.ConsolidateIniFiles(mapIni, globalCodeIni);

            for (int chkId = 0; chkId < CheckBoxes.Count; chkId++)
            {
                UserCheckBox chkBox = CheckBoxes[chkId];

                if ((chkBox.Checked && !chkBox.Reversed) ||
                    (!chkBox.Checked && chkBox.Reversed))
                {
                    string associatedCustomIni = AssociatedCheckBoxCustomInis[chkId];
                    if (associatedCustomIni != "none")
                    {
                        Logger.Log("Consolidating map code INI and associated game option INI: " + associatedCustomIni);
                        IniFile associatedIni = new IniFile(ProgramConstants.GamePath + associatedCustomIni);
                        IniFile.ConsolidateIniFiles(mapIni, associatedIni);
                    }
                }
            }

            mapIni.MoveSectionToFirst("MultiplayerDialogSettings");

            Logger.Log("Writing final map INI file.");
            mapIni.WriteIniFile(ProgramConstants.GamePath + ProgramConstants.SPAWNMAP_INI);
        }

        /// <summary>
        /// Generic preview painter for the Skirmish and CnCNet Game Lobbies.
        /// See NGameLobby and SkirmishLobby for usage examples.
        /// </summary>
        public static void PaintPreview(Map map, Rectangle imageRectangle, PaintEventArgs e,
            Font playerNameFont, Color coopBriefingForeColor, bool displayCoopBriefing,
            double previewRatioY, double previewRatioX, List<string>[] playerNamesOnPlayerLocations,
            List<Color> mpColors, List<int>[] playerColorsOnPlayerLocations, Image[] startingLocationIndicators,
            Image enemyStartingLocationIndicator)
        {
            if (CoopBriefingFont == null)
                CoopBriefingFont = new System.Drawing.Font("Segoe UI", 11.25f, FontStyle.Regular);

            if (map == null)
                return;

            if (map.StartingLocationsX.Count > 0)
            {
                for (int pId = 0; pId < map.StartingLocationsX.Count; pId++)
                {
                    int xPos = map.StartingLocationsX[pId];
                    int yPos = map.StartingLocationsY[pId];

                    Image startingLocationIndicator;
                    if (pId < map.AmountOfPlayers)
                        startingLocationIndicator = startingLocationIndicators[pId];
                    else
                        startingLocationIndicator = enemyStartingLocationIndicator;

                    int halfWidth = startingLocationIndicator.Width / 2;
                    int halfHeight = startingLocationIndicator.Height / 2;

                    int x;
                    int y;
                    if (!map.StaticPreviewSize)
                    {
                        x = Convert.ToInt32(xPos * previewRatioX);
                        y = Convert.ToInt32(yPos * previewRatioY);
                    }
                    else
                    {
                        x = xPos;
                        y = yPos;
                    }

                    e.Graphics.DrawImage(startingLocationIndicator,
                        new Rectangle(imageRectangle.X + x - (halfWidth), imageRectangle.Y + y - (halfHeight),
                            startingLocationIndicator.Width, startingLocationIndicator.Height),
                        new Rectangle(0, 0, startingLocationIndicator.Width, startingLocationIndicator.Height),
                        GraphicsUnit.Pixel);

                    if (playerNamesOnPlayerLocations == null)
                        continue;

                    int id = 0;

                    foreach (string playerName in playerNamesOnPlayerLocations[pId])
                    {
                        float basePositionY = imageRectangle.Y + Convert.ToSingle(y) + (startingLocationIndicator.Height * id) - (halfHeight / 1.5f);

                        e.Graphics.DrawString(playerName, playerNameFont,
                            new SolidBrush(Color.Black),
                            new PointF(imageRectangle.X + Convert.ToSingle(x) + halfWidth + 4,
                            basePositionY + 1f));

                        e.Graphics.DrawString(playerName, playerNameFont,
                            new SolidBrush(mpColors[playerColorsOnPlayerLocations[pId][id]]),
                            new PointF(imageRectangle.X + Convert.ToSingle(x) + halfWidth + 3,
                                basePositionY));

                        id++;
                    }
                }
            }

            // Draw co-op mission briefing if we should do so
            if (!String.IsNullOrEmpty(map.Briefing) && displayCoopBriefing)
            {
                int height = (int)e.Graphics.MeasureString(map.Briefing, CoopBriefingFont).Height + 5;

                int briefingBoxY = (imageRectangle.Height - height) / 2;
                int briefingBoxX = (imageRectangle.Width - COOP_BRIEFING_WIDTH) / 2;

                e.Graphics.DrawRectangle(new Pen(new SolidBrush(coopBriefingForeColor)),
                    new Rectangle(imageRectangle.X + briefingBoxX, imageRectangle.Y + briefingBoxY,
                    COOP_BRIEFING_WIDTH, height));

                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(245, 0, 0, 0)),
                    new Rectangle(imageRectangle.X + briefingBoxX + 1, imageRectangle.Y + briefingBoxY + 1,
                    COOP_BRIEFING_WIDTH - 2, height - 2));

                e.Graphics.DrawString(map.Briefing, CoopBriefingFont,
                    new SolidBrush(Color.Black),
                    new PointF(imageRectangle.X + briefingBoxX + 4, imageRectangle.Y + briefingBoxY + 4));

                e.Graphics.DrawString(map.Briefing, CoopBriefingFont,
                    new SolidBrush(coopBriefingForeColor),
                    new PointF(imageRectangle.X + briefingBoxX + 3, imageRectangle.Y + briefingBoxY + 3));
            }
        }

        public static string FixBriefing(string briefing)
        {
            if (CoopBriefingFont == null)
                CoopBriefingFont = new Font("Segoe UI", 11.25f, FontStyle.Regular);

            Graphics g = new Control().CreateGraphics();

            string line = String.Empty;
            string returnValue = String.Empty;
            string[] wordArray = briefing.Split(' ');

            foreach (string word in wordArray)
            {
                if (g.MeasureString(line + word, CoopBriefingFont).Width > COOP_BRIEFING_WIDTH - 6)
                {
                    returnValue = returnValue + line + '\n';
                    line = String.Empty;
                }

                line = line + word + " ";
            }

            returnValue = returnValue + line;
            return returnValue;
        }

        /// <summary>
        /// Parses and applies forced game options from an INI file.
        /// </summary>
        /// <param name="lockedOptionsIni">The INI file to read and apply the forced game options from.</param>
        public static void ParseLockedOptionsFromIni(List<UserCheckBox> CheckBoxes,
            List<LimitedComboBox> ComboBoxes, IniFile lockedOptionsIni)
        {
            List<string> keys = lockedOptionsIni.GetSectionKeys("ForcedOptions");
            if (keys == null)
            {
                Logger.Log("Unable to parse ForcedOptions from forced game options ini.");
                return;
            }

            foreach (string key in keys)
            {
                UserCheckBox chkBox = CheckBoxes.Find(c => c.Name == key);

                if (chkBox != null)
                {
                    chkBox.Checked = lockedOptionsIni.GetBooleanValue("ForcedOptions", key, false);
                    chkBox.Enabled = false;
                }

                LimitedComboBox cmbBox = ComboBoxes.Find(c => c.Name == key);

                if (cmbBox != null)
                {
                    cmbBox.SelectedIndex = lockedOptionsIni.GetIntValue("ForcedOptions", key, 0);
                    cmbBox.Enabled = false;
                }
            }
        }

        /// <summary>
        /// Parses and applies various theme-related INI keys from DTACnCNetClient.ini.
        /// Enables editing attributes of individual controls in DTACnCNetClient.ini.
        /// </summary>
        public static void ParseClientThemeIni(Form form)
        {
            IniFile clientThemeIni = DomainController.Instance().DTACnCNetClient_ini;

            List<string> sections = clientThemeIni.GetSections();

            if (sections.Contains(form.Name))
            {
                List<string> keys = clientThemeIni.GetSectionKeys(form.Name);

                foreach (string key in keys)
                {
                    if (key == "Size")
                    {
                        string[] parts = clientThemeIni.GetStringValue(form.Name, key, "10,10").Split(',');

                        int w = Int32.Parse(parts[0]);
                        int h = Int32.Parse(parts[1]);

                        form.Size = new Size(w, h);
                    }
                }
            }

            foreach (string section in sections)
            {
                Control[] controls = form.Controls.Find(section, true);

                if (controls.Length == 0)
                    continue;

                Control control = controls[0];

                List<string> keys = clientThemeIni.GetSectionKeys(section);

                foreach (string key in keys)
                {
                    string keyValue = clientThemeIni.GetStringValue(section, key, String.Empty);

                    switch (key)
                    {
                        case "Font":
                            control.Font = SharedLogic.GetFont(keyValue);
                            break;
                        case "ForeColor":
                            control.ForeColor = GetColorFromString(keyValue);
                            break;
                        case "BackColor":
                            control.BackColor = GetColorFromString(keyValue);
                            break;
                        case "Size":
                            string[] sizeArray = keyValue.Split(',');
                            control.Size = new Size(Convert.ToInt32(sizeArray[0]), Convert.ToInt32(sizeArray[1]));
                            break;
                        case "Location":
                            string[] locationArray = keyValue.Split(',');
                            control.Location = new Point(Convert.ToInt32(locationArray[0]), Convert.ToInt32(locationArray[1]));
                            break;
                        case "Text":
                            control.Text = keyValue.Replace("@", Environment.NewLine);
                            break;
                        case "Visible":
                            control.Visible = clientThemeIni.GetBooleanValue(section, key, true);
                            break;
                        case "DefaultImage":
                            if (control is SwitchingImageButton)
                                ((SwitchingImageButton)control).DefaultImage = SharedUILogic.LoadImage(keyValue);
                            break;
                        case "HoveredImage":
                            if (control is SwitchingImageButton)
                                ((SwitchingImageButton)control).HoveredImage = SharedUILogic.LoadImage(keyValue);
                            break;
                        case "BorderStyle":
                            BorderStyle bs = BorderStyle.FixedSingle;
                            if (keyValue.ToUpper() == "NONE")
                                bs = BorderStyle.None;
                            else if (keyValue.ToUpper() == "FIXED3D")
                                bs = BorderStyle.Fixed3D;

                            if (control is Panel)
                                ((Panel)control).BorderStyle = bs;
                            else if (control is ScrollbarlessListBox)
                                ((ScrollbarlessListBox)control).BorderStyle = bs;
                            else if (control is TextBox)
                                ((TextBox)control).BorderStyle = bs;
                            else if (control is EnhancedPictureBox)
                                ((EnhancedPictureBox)control).BorderStyle = bs;

                            break;
                        case "Anchor":
                            if (keyValue == "Top,Left")
                                control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                            else if (keyValue == "Top,Right")
                                control.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                            else if (keyValue == "Bottom,Right")
                                control.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                            else if (keyValue == "Bottom,Left")
                                control.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                            else if (keyValue == "Top,Bottom,Left,Right")
                                control.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

                            break;
                        case "BackgroundImage":
                            control.BackgroundImage = LoadImage(keyValue);
                            control.Size = control.BackgroundImage.Size;
                            break;
                    }
                }
            }
        }

        public static void InitForm(MovableForm form, IniFile iniFile)
        {
            SetControlStyle(iniFile, form);
        }

        /// <summary>
        /// Sets the visual style of a control and (recursively) its child controls.
        /// </summary>
        /// <param name="iniFile">The INI file that contains information about the controls' styles.</param>
        /// <param name="control">The control that should be styled.</param>
        public static void SetControlStyle(IniFile iniFile, Control control)
        {
            List<string> sections = iniFile.GetSections();

            if (sections.Contains(control.Name))
            {
                List<string> keys = iniFile.GetSectionKeys(control.Name);

                foreach (string key in keys)
                {
                    string keyValue = iniFile.GetStringValue(control.Name, key, String.Empty);

                    if (keyValue == String.Empty)
                        continue;

                    switch (key)
                    {
                        case "Font":
                            control.Font = Utilities.GetFont(keyValue);
                            break;
                        case "ForeColor":
                            control.ForeColor = Utilities.GetColorFromString(keyValue);
                            break;
                        case "BackColor":
                            control.BackColor = Utilities.GetColorFromString(keyValue);
                            break;
                        case "Size":
                            string[] sizeArray = keyValue.Split(',');
                            control.Size = new Size(Convert.ToInt32(sizeArray[0]), Convert.ToInt32(sizeArray[1]));
                            break;
                        case "Location":
                            string[] locationArray = keyValue.Split(',');
                            control.Location = new Point(Convert.ToInt32(locationArray[0]), Convert.ToInt32(locationArray[1]));
                            break;
                        case "Text":
                            control.Text = keyValue.Replace("@", Environment.NewLine);
                            break;
                        case "BorderStyle":
                            BorderStyle bs = BorderStyle.None;

                            if (keyValue == "FixedSingle")
                                bs = BorderStyle.FixedSingle;
                            else if (keyValue == "Fixed3D")
                                bs = BorderStyle.Fixed3D;
                            else if (keyValue == "None")
                                bs = BorderStyle.None;

                            if (control is Panel)
                            {
                                ((Panel)control).BorderStyle = bs;
                            }
                            else if (control is PictureBox)
                            {
                                ((PictureBox)control).BorderStyle = bs;
                            }
                            else if (control is Label)
                            {
                                ((Label)control).BorderStyle = bs;
                                ((Label)control).AutoSize = true;
                            }
                            else
                                throw new InvalidDataException("Invalid BackgroundImage key for control " + control.Name + " (key valid for Panels, Labels and PictureBoxes only)");
                            break;
                        case "BackgroundImage":
                            string imagePath = keyValue;

                            Image image = SharedUILogic.LoadImage(imagePath);

                            if (control is PictureBox)
                            {
                                ((PictureBox)control).Image = image;

                                control.Size = image.Size;
                            }
                            else //if (control is Form)
                            {
                                control.BackgroundImage = image;
                            }
                            //else
                            //throw new InvalidDataException("Invalid BackgroundImage key for control " + control.Name + " (key valid for Panels and PictureBoxes only)");
                            break;
                        case "RepeatingImage":
                            if (iniFile.GetBooleanValue(control.Name, "RepeatingImage", true))
                            {
                                control.BackgroundImage = ((PictureBox)control).Image;
                                ((PictureBox)control).Image = null;
                            }
                            //    SetImageRepeating((PictureBox)control, ((PictureBox)control).Image);
                            break;
                        case "DefaultImage":
                            string imgPath = "MainMenu\\" + keyValue;

                            Image img = SharedUILogic.LoadImage(imgPath);

                            if (control is SwitchingImageButton)
                                ((SwitchingImageButton)control).DefaultImage = img;

                            break;
                        case "HoveredImage":
                            string hImgPath = "MainMenu\\" + keyValue;

                            Image hImg = SharedUILogic.LoadImage(hImgPath);

                            if (control is SwitchingImageButton)
                                ((SwitchingImageButton)control).HoveredImage = hImg;

                            break;
                        case "Visible":
                            control.Visible = iniFile.GetBooleanValue(control.Name, key, false);
                            break;
                        case "FormBorderStyle":
                            if (control is Form)
                            {
                                FormBorderStyle fbs = FormBorderStyle.Sizable;
                                switch (keyValue)
                                {
                                    case "None":
                                        fbs = FormBorderStyle.None;
                                        break;
                                    case "SizableToolWindow":
                                        fbs = FormBorderStyle.SizableToolWindow;
                                        break;
                                    case "Fixed3D":
                                        fbs = FormBorderStyle.Fixed3D;
                                        break;
                                    case "FixedSingle":
                                        fbs = FormBorderStyle.FixedSingle;
                                        break;
                                    case "FixedDialog":
                                        fbs = FormBorderStyle.FixedDialog;
                                        break;
                                    case "FixedToolWindow":
                                        fbs = FormBorderStyle.FixedToolWindow;
                                        break;
                                    case "Sizable":
                                    default:
                                        break;
                                }

                                ((Form)control).FormBorderStyle = fbs;
                            }
                            else
                                Logger.Log("SetControlStyle: Control " + control.Name + " isn't a form - FormBorderStyle doesn't apply!");
                            break;
                        case "DistanceFromRightBorder":
                            int distance = iniFile.GetIntValue(control.Name, key, 50);
                            control.Location = new Point(control.Parent.Size.Width - distance - control.Width, control.Location.Y);
                            break;
                        case "DistanceFromBottomBorder":
                            int bDistance = iniFile.GetIntValue(control.Name, key, 50);
                            control.Location = new Point(control.Location.X, control.Parent.Size.Height - bDistance - control.Height);
                            break;
                        case "FillHeight":
                            control.Size = new Size(control.Size.Width, control.Parent.Size.Height - iniFile.GetIntValue(control.Name, "FillHeight", 0));
                            break;
                        case "FillWidth":
                            control.Size = new Size(control.Parent.Size.Width - iniFile.GetIntValue(control.Name, "FillWidth", 0), control.Size.Height);
                            break;
                        case "Anchor":
                            switch (keyValue)
                            {
                                case "Top":
                                    control.Anchor = AnchorStyles.Top;
                                    break;
                                case "Bottom":
                                    control.Anchor = AnchorStyles.Bottom;
                                    break;
                                case "Top,Left":
                                    control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                                    break;
                                case "Top,Right":
                                    control.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                                    break;
                                case "Bottom,Left":
                                    control.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                                    break;
                                case "Bottom,Right":
                                    control.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                                    break;
                                case "Top,Left,Right":
                                case "Top,Right,Left":
                                    control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                                    break;
                                case "Bottom,Left,Right":
                                case "Bottom,Right,Left":
                                    control.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                                    break;
                                case "All":
                                case "Top,Bottom,Left,Right":
                                case "Top,Bottom,Right,Left":
                                case "Bottom,Top,Left,Right":
                                case "Bottom,Top,Right,Left":
                                    control.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                                    break;
                            }
                            break;
                    }
                }
            }

            foreach (Control c in control.Controls)
                SetControlStyle(iniFile, c);
        }

        /// <summary>
        /// Makes an image repeat in a picturebox.
        /// </summary>
        /// <param name="pb">The picturebox.</param>
        /// <param name="image">The image.</param>
        private static void SetImageRepeating(PictureBox pb, Image image)
        {
            Bitmap bm = new Bitmap(pb.Width, pb.Height);
            Graphics gp = Graphics.FromImage(bm);
            gp.DrawImage(image, new Point(0, 0));
            //if (pb.Image.Width < pb.Width)
            //{
            //    for (int x = image.Width; x <= bm.Width - image.Width; x += image.Width)
            //    {
            //        gp.DrawImage(image, new Point(x, 0));
            //    }
            //}
            //if (pb.Image.Height < pb.Height)
            //{
            //    for (int y = image.Height; y <= bm.Height - image.Height; y += image.Height)
            //    {
            //        gp.DrawImage(image, new Point(0, y));
            //    }
            //}
            pb.Image = bm;
        }

        /// <summary>
        /// Gets a color from a RGB color string (example: 255,255,255)
        /// </summary>
        /// <param name="colorString">The color string.</param>
        /// <returns>The color.</returns>
        public static Color GetColorFromString(string colorString)
        {
            string[] colorArray = colorString.Split(',');
            Color color = Color.FromArgb(Convert.ToByte(colorArray[0]), Convert.ToByte(colorArray[1]), Convert.ToByte(colorArray[2]));
            return color;
        }

        /// <summary>
        /// Starts the main game process.
        /// </summary>
        /// <param name="processId">The index of the game process to start (for RA2 support;
        /// GameOptions.ini -> GameExecutableNames= allows multiple names).</param>
        public static void StartGameProcess(int processId)
        {
            string gameExecutableName = DomainController.Instance().GetGameExecutableName(processId);

            string extraCommandLine = DomainController.Instance().GetExtraCommandLineParameters();

            File.Delete(ProgramConstants.GamePath + "DTA.LOG");

            if (DomainController.Instance().GetWindowedStatus())
            {
                Logger.Log("Windowed mode is enabled - using QRes.");
                Process QResProcess = new Process();
                QResProcess.StartInfo.FileName = ProgramConstants.QRES_EXECUTABLE;
                QResProcess.StartInfo.UseShellExecute = false;
                QResProcess.StartInfo.Arguments = "c=16 /R " + "\"" + ProgramConstants.GamePath + gameExecutableName + "\"" + " -SPAWN";
                if (!String.IsNullOrEmpty(extraCommandLine))
                    QResProcess.StartInfo.Arguments = QResProcess.StartInfo.Arguments + " " + extraCommandLine;
                QResProcess.EnableRaisingEvents = true;
                QResProcess.Exited += new EventHandler(Process_Exited);
                try
                {
                    QResProcess.Start();
                }
                catch (Exception ex)
                {
                    Logger.Log("Error launching QRes: " + ex.Message);
                    MessageBox.Show("Error launching " + ProgramConstants.QRES_EXECUTABLE + ". Please check that your anti-virus isn't blocking the CnCNet Client. " +
                        "You can also try running the client as an administrator." + Environment.NewLine + Environment.NewLine + "You are unable to participate in this match." +
                        Environment.NewLine + Environment.NewLine + "Returned error: " + ex.Message,
                        "Error launching game", MessageBoxButtons.OK);
                    Process_Exited(QResProcess, EventArgs.Empty);
                    return;
                }

                if (Environment.ProcessorCount > 1)
                    QResProcess.ProcessorAffinity = (IntPtr)2;
            }
            else
            {
                Process DtaProcess = new Process();
                DtaProcess.StartInfo.FileName = gameExecutableName;
                DtaProcess.StartInfo.UseShellExecute = false;
                DtaProcess.StartInfo.Arguments = "-SPAWN";
                if (!String.IsNullOrEmpty(extraCommandLine))
                    DtaProcess.StartInfo.Arguments = DtaProcess.StartInfo.Arguments + " " + extraCommandLine;
                DtaProcess.EnableRaisingEvents = true;
                DtaProcess.Exited += new EventHandler(Process_Exited);
                try
                {
                    DtaProcess.Start();
                }
                catch (Exception ex)
                {
                    Logger.Log("Error launching " + gameExecutableName + ": " + ex.Message);
                    MessageBox.Show("Error launching " + gameExecutableName + ". Please check that your anti-virus isn't blocking the CnCNet Client. " +
                        "You can also try running the client as an administrator." + Environment.NewLine + Environment.NewLine + "You are unable to participate in this match." + 
                        Environment.NewLine + Environment.NewLine + "Returned error: " + ex.Message,
                        "Error launching game", MessageBoxButtons.OK);
                    Process_Exited(DtaProcess, EventArgs.Empty);
                    return;
                }

                if (Environment.ProcessorCount > 1)
                    DtaProcess.ProcessorAffinity = (IntPtr)2;
            }

            GameProcessStarted?.Invoke();

            Logger.Log("Waiting for qres.dat or " + gameExecutableName + " to exit.");
        }

        static void Process_Exited(object sender, EventArgs e)
        {
            Process proc = (Process)sender;
            proc.Exited -= Process_Exited;
            proc.Dispose();
            if (GameProcessExited != null)
                GameProcessExited();
        }

        /// <summary>
        /// Loads icons used for displaying sides in the game lobby.
        /// </summary>
        /// <returns>An array of side images.</returns>
        public static Image[] LoadSideImages()
        {
            string[] sides = DomainController.Instance().GetSides().Split(',');
            Image[] returnValue = new Image[sides.Length + 2];

            returnValue[0] = Image.FromFile(ProgramConstants.GamePath + ProgramConstants.BASE_RESOURCE_PATH + "randomicon.png");

            for (int i = 1; i <= sides.Length; i++)
            {
                returnValue[i] = Image.FromFile(ProgramConstants.GamePath + ProgramConstants.BASE_RESOURCE_PATH + "" + sides[i - 1] + "icon.png");
            }

            returnValue[sides.Length + 1] = Image.FromFile(ProgramConstants.GamePath + ProgramConstants.BASE_RESOURCE_PATH + "spectatoricon.png");

            return returnValue;
        }

        /// <summary>
        /// Loads starting location indicator icons for the game lobby.
        /// </summary>
        /// <returns>An array of starting location indicator images.</returns>
        public static Image[] LoadStartingLocationIndicators()
        {
            Image[] startingLocationIndicators = new Image[8];
            startingLocationIndicators[0] = SharedUILogic.LoadImage("slocindicator1.png");
            startingLocationIndicators[1] = SharedUILogic.LoadImage("slocindicator2.png");
            startingLocationIndicators[2] = SharedUILogic.LoadImage("slocindicator3.png");
            startingLocationIndicators[3] = SharedUILogic.LoadImage("slocindicator4.png");
            startingLocationIndicators[4] = SharedUILogic.LoadImage("slocindicator5.png");
            startingLocationIndicators[5] = SharedUILogic.LoadImage("slocindicator6.png");
            startingLocationIndicators[6] = SharedUILogic.LoadImage("slocindicator7.png");
            startingLocationIndicators[7] = SharedUILogic.LoadImage("slocindicator8.png");

            return startingLocationIndicators;
        }

        /// <summary>
        /// Sets the background image layout of a form based on the client's settings.
        /// </summary>
        /// <param name="form">The form.</param>
        public static void SetBackgroundImageLayout(Form form)
        {
            string backgroundImageLayout = DomainController.Instance().GetGameLobbyBackgroundImageLayout();
            switch (backgroundImageLayout)
            {
                case "Center":
                    form.BackgroundImageLayout = ImageLayout.Center;
                    break;
                case "Stretch":
                    form.BackgroundImageLayout = ImageLayout.Stretch;
                    break;
                case "Zoom":
                    form.BackgroundImageLayout = ImageLayout.Zoom;
                    break;
                default:
                case "Tile":
                    form.BackgroundImageLayout = ImageLayout.Tile;
                    break;
            }
        }

        /// <summary>
        /// Sets the colors of a specific control and (recursively) all of its child controls.
        /// </summary>
        /// <param name="cLabelColor">The color of labels in the UI.</param>
        /// <param name="cBackColor">The background color of list boxes and combo boxes in the UI.</param>
        /// <param name="cAltUiColor">The foreground color of list boxes, buttons and combo boxes in the UI.</param>
        /// <param name="cListBoxFocusColor">The background color of highlighted list box and combo box items.</param>
        /// <param name="control">The control. Usually you'll want to have a form in this parameter.</param>
        public static void SetControlColor(Color cLabelColor, Color cBackColor, Color cAltUiColor,
            Color cListBoxFocusColor, Control control)
        {
            SetControlColors(cLabelColor, cBackColor, cAltUiColor, cListBoxFocusColor, control);

            foreach (Control child in control.Controls)
                SetControlColor(cLabelColor, cBackColor, cAltUiColor, cListBoxFocusColor, child);
        }

        /// <summary>
        /// Sets the colors of a single control.
        /// </summary>
        /// <param name="cLabelColor">The color of labels in the UI.</param>
        /// <param name="cBackColor">The background color of list boxes and combo boxes in the UI.</param>
        /// <param name="cAltUiColor">The foreground color of list boxes, buttons and combo boxes in the UI.</param>
        /// <param name="cListBoxFocusColor">The background color of highlighted list box and combo box items.</param>
        /// <param name="control">The control.</param>
        private static void SetControlColors(Color cLabelColor, Color cBackColor, Color cAltUiColor,
            Color cListBoxFocusColor, Control control)
        {
            if (control is LimitedComboBox)
            {
                control.ForeColor = cAltUiColor;
                control.BackColor = cBackColor;

                ((LimitedComboBox)control).FocusColor = cListBoxFocusColor;
            }
            else if (control is ScrollbarlessListBox || control is Button || control is TextBox)
            {
                control.ForeColor = cAltUiColor;
                control.BackColor = cBackColor;
            }
            else if (control is Label)
            {
                control.ForeColor = cLabelColor;
            }
        }

        /// <summary>
        /// Gathers and returns a list of usable multiplayer colors.
        /// </summary>
        /// <returns>A list of usable multiplayer colors.</returns>
        public static List<Color> GetMPColors()
        {
            List<Color> MPColors = new List<Color>();
            MPColors.Add(Color.White);
            MPColors.Add(GetColorFromString(DomainController.Instance().GetMPColorOne()));
            MPColors.Add(GetColorFromString(DomainController.Instance().GetMPColorTwo()));
            MPColors.Add(GetColorFromString(DomainController.Instance().GetMPColorThree()));
            MPColors.Add(GetColorFromString(DomainController.Instance().GetMPColorFour()));
            MPColors.Add(GetColorFromString(DomainController.Instance().GetMPColorFive()));
            MPColors.Add(GetColorFromString(DomainController.Instance().GetMPColorSix()));
            MPColors.Add(GetColorFromString(DomainController.Instance().GetMPColorSeven()));
            MPColors.Add(GetColorFromString(DomainController.Instance().GetMPColorEight()));

            return MPColors;
        }

        public static string[] GetTeamIdentifiers()
        {
            return new string[] { "[A] ", "[B] ", "[C] ", "[D] " }; 
        }

        public static LimitedComboBox SetUpComboBoxFromIni(string comboBoxName, IniFile gameOptionsIni, ToolTip toolTip,
            Font font, Color defaultOptionColor, Color nonDefaultOptionColor)
        {
            string[] items = gameOptionsIni.GetStringValue(comboBoxName, "Items", "give me items, noob!").Split(',');
            int defaultIndex = gameOptionsIni.GetIntValue(comboBoxName, "DefaultIndex", 0);
            string _dataWriteMode = gameOptionsIni.GetStringValue(comboBoxName, "DataWriteMode", "Boolean");
            DataWriteMode dwMode;
            if (_dataWriteMode == "Boolean")
                dwMode = DataWriteMode.BOOLEAN;
            else if (_dataWriteMode == "Index")
                dwMode = DataWriteMode.INDEX;
            else
                dwMode = DataWriteMode.STRING;
            string[] location = gameOptionsIni.GetStringValue(comboBoxName, "Location", "0,0").Split(',');
            Point pLocation = new Point(Convert.ToInt32(location[0]), Convert.ToInt32(location[1]));
            string[] size = gameOptionsIni.GetStringValue(comboBoxName, "Size", "83,21").Split(',');
            Size sSize = new Size(Convert.ToInt32(size[0]), Convert.ToInt32(size[1]));
            string sToolTip = gameOptionsIni.GetStringValue(comboBoxName, "ToolTip", String.Empty);

            LimitedComboBox cmbBox = new LimitedComboBox();
            cmbBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            cmbBox.FlatStyle = FlatStyle.Flat;
            cmbBox.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbBox.Font = font;
            cmbBox.Name = comboBoxName;
            for (int itemId = 0; itemId < items.Length; itemId++)
            {
                if (itemId == defaultIndex)
                    cmbBox.AddItem(items[itemId], defaultOptionColor);
                else
                    cmbBox.AddItem(items[itemId], nonDefaultOptionColor);
            }
            cmbBox.SelectedIndex = defaultIndex;
            cmbBox.Location = pLocation;
            cmbBox.Size = sSize;
            cmbBox.DrawMode = DrawMode.OwnerDrawVariable;
            cmbBox.MaxDropDownItems = cmbBox.Items.Count;
            cmbBox.Tag = dwMode;

            if (!String.IsNullOrEmpty(sToolTip))
            {
                toolTip.SetToolTip(cmbBox, sToolTip);
            }

            return cmbBox;
        }

        /// <summary>
        /// Returns the amount of spectators in a PlayerInfo list.
        /// </summary>
        /// <returns>The amount of spectators.</returns>
        public static int GetSpectatorCount(int sideCount, List<PlayerInfo> Players)
        {
            int spectatorSideId = sideCount + 1;

            int spectatorCount = 0;

            foreach (PlayerInfo player in Players)
            {
                if (player.SideId == spectatorSideId)
                    spectatorCount++;
            }

            return spectatorCount;
        }

        public static Image LoadImage(string resourceName)
        {
            if (File.Exists(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + resourceName))
                return Image.FromStream(new MemoryStream(File.ReadAllBytes(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + resourceName)));
            else if (File.Exists(ProgramConstants.GamePath + ProgramConstants.BASE_RESOURCE_PATH + "" + resourceName))
                return Image.FromStream(new MemoryStream(File.ReadAllBytes(ProgramConstants.GamePath + ProgramConstants.BASE_RESOURCE_PATH + "" + resourceName)));

            return Properties.Resources.hotbutton;
        }
    }
}
