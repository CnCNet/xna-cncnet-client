/*
 * StatsDumpParser.cs
 *
 * Credits:
 *   Iran (for stats.dmp parsing logic and guidance) https://github.com/mvdhout1992/ra303pStatsDumpParser
 * Used with permission.
 *
 * Integrated into CnCNet XNA Client by CO2
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ClientCore.Statistics.GameParsers
{
    public class StatsDumpParser
    {
        // Stream reading stuff
        public BigEndianReader Bin;
        public int Pos;

        public int QuitPlayerNumHelper = -1; // To help parse per player QUIT

        public int DumpSize = -1;
        public int ReportedSize = -1;
        public int[] PlayerMoneyHarvested;
        public int[] PlayerCredits;
        public int[] PlayerQuitStates;
        public int[] PlayerColors;
        public int[] PlayerAlliancesBitFields;
        public int[] PlayerSpectatorStates; // -1 = unparsed, 0 = not spectator, 1 = spectator
        public int[] PlayerDeadStates; // -1 = unparsed, 0 = not dead, 1 = dead
        public int[] PlayersSpawnLocation; // -1 = unparsed
        public int[] PlayersConnectionLost; // -1 = unparsed, 0 = not lost, 1 = lost
        public int[] PlayersResigned; // -1 = unparsed, 0 = didn't resign, 1 = did resign
        public string[] PlayerNames;
        public string[] PlayerSides;
        public CratesCollectedStruct[] PlayerCratesCollected;

        // Left on battlefield
        public VehiclesStruct[] PlayerVehiclesLeft;
        public InfantryStruct[] PlayerInfantryLeft;
        public PlanesStruct[] PlayerPlanesLeft;
        public BuildingsStruct[] PlayerBuildingsLeft;
        public VesselsStruct[] PlayerVesselsLeft;

        // Bought
        public VehiclesStruct[] PlayerVehiclesBought;
        public InfantryStruct[] PlayerInfantryBought;
        public PlanesStruct[] PlayerPlanesBought;
        public BuildingsStruct[] PlayerBuildingsBought;
        public VesselsStruct[] PlayerVesselsBought;

        // Killed
        public VehiclesStruct[] PlayerVehiclesKilled;
        public InfantryStruct[] PlayerInfantryKilled;
        public PlanesStruct[] PlayerPlanesKilled;
        public BuildingsStruct[] PlayerBuildingsKilled;
        public VesselsStruct[] PlayerVesselsKilled;

        // Buildings captured
        public BuildingsStruct[] PlayerBuildingsCaptured;


        public int SDFX = -1;
        public int GameNumber = -1;
        public int NumberOfPlayers = -1;
        public int NumberOfRemainingPlayers = -1;
        public int IsTournamentGame = -1;
        public int StartingCredits = -1;
        public int BasesEnabled = -1;
        public int OreRegenerates = -1;
        public int CratesEnabled = -1;
        public int NumberOfAIPlayers = -1;
        public int ShroudRegrows = -1;
        public int CTFEnabled = -1;
        public int StartingUnits = -1;
        public int TechLevel = -1;
        public string MapName = "UNPARSED";
        public string IPAddress1 = "UNPARSED";
        public string IPAddress2 = "UNPARSED";
        public string Ping = "UNPARSED";
        public int CompletionType = -1;
        public int GameDuration = -1;
        public int StartTime = -1;
        public int ProcessorType = -1;
        public int AverageFPS = -1;
        public uint SystemMemory = 0;
        public uint VideoMemory = 0;
        public int GameSpeed = -1;
        public string Version = "UNPARSED";
        public DateTime? GameEXELastWriteTimeUTC = null; // null is value if not parsed


        public StatsDumpParser(string fileName)
        {
            // Init
            PlayerMoneyHarvested = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
            PlayerCredits = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
            PlayerQuitStates = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
            PlayerColors = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
            PlayerAlliancesBitFields = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
            PlayerSpectatorStates = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
            PlayerDeadStates = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
            PlayersSpawnLocation = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
            PlayersConnectionLost = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
            PlayersResigned = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
            PlayerSides = new string[8];
            PlayerNames = new string[8];
            PlayerCratesCollected = new CratesCollectedStruct[8];

            // Left on battlefield
            PlayerVehiclesLeft = new VehiclesStruct[8];
            PlayerInfantryLeft = new InfantryStruct[8];
            PlayerPlanesLeft = new PlanesStruct[8];
            PlayerBuildingsLeft = new BuildingsStruct[8];
            PlayerVesselsLeft = new VesselsStruct[8];

            // Bought
            PlayerVehiclesBought = new VehiclesStruct[8];
            PlayerInfantryBought = new InfantryStruct[8];
            PlayerPlanesBought = new PlanesStruct[8];
            PlayerBuildingsBought = new BuildingsStruct[8];
            PlayerVesselsBought = new VesselsStruct[8];

            // Killed
            PlayerVehiclesKilled = new VehiclesStruct[8];
            PlayerInfantryKilled = new InfantryStruct[8];
            PlayerPlanesKilled = new PlanesStruct[8];
            PlayerBuildingsKilled = new BuildingsStruct[8];
            PlayerVesselsKilled = new VesselsStruct[8];

            // Buildings captured
            PlayerBuildingsCaptured = new BuildingsStruct[8];

            ParseStatsDumpFile(fileName);
        }

        public void ParseStatsDumpFile(string fileName)
        {
            using (BinaryReader b = new BinaryReader(File.Open(fileName, FileMode.Open)))
            {
                Bin = new BigEndianReader(b);
                Pos = 0;
                DumpSize = (int)Bin.BaseStream.Length;

                if (DumpSize < 4)
                {
                    throw new StatsDumpLengthException();
                }

                ReportedSize = Bin.ReadInt16();
                Bin.ReadInt16();
                Pos += 2;

                while (Pos < DumpSize)
                {
                    byte[] bytes = Bin.ReadBytes(4);
                    Pos += 4;
                    string id = Encoding.ASCII.GetString(bytes);

                    if (id.Contains("RSG"))
                    {
                        ParseResignedInfo(id);
                    }
                    else if (id.Contains("CON"))
                    {
                        ParseConnectionLostInfo(id);
                    }
                    else if (id.Contains("SPA"))
                    {
                        ParseSpawnLocationInfo(id);
                    }
                    else if (id.Contains("NAM"))
                    {
                        ParseNameInfo(id);
                    }
                    else if (id.Contains("COL"))
                    {
                        ParseColorInfo(id);
                    }
                    else if (id.Contains("ALY"))
                    {
                        ParseAlliancesInfo(id);
                    }
                    else if (id.Contains("SPC"))
                    {
                        ParseSpectatorStateInfo(id);
                    }
                    else if (id.Contains("DED"))
                    {
                        ParseDeadStateInfo(id);
                    }
                    else if (id.Contains("UNL"))
                    {
                        ParseVehiclesStuff(id, ref PlayerVehiclesLeft);
                    }
                    else if (id.Contains("UNB"))
                    {
                        ParseVehiclesStuff(id, ref PlayerVehiclesBought);
                    }
                    else if (id.Contains("UNK"))
                    {
                        ParseVehiclesStuff(id, ref PlayerVehiclesKilled);
                    }
                    else if (id.Contains("INL"))
                    {
                        ParseInfantryStuff(id, ref PlayerInfantryLeft);
                    }
                    else if (id.Contains("INB"))
                    {
                        ParseInfantryStuff(id, ref PlayerInfantryBought);
                    }
                    else if (id.Contains("INK"))
                    {
                        ParseInfantryStuff(id, ref PlayerInfantryKilled);
                    }
                    else if (id.Contains("PLL"))
                    {
                        ParsePlanesStuff(id, ref PlayerPlanesLeft);
                    }
                    else if (id.Contains("PLB"))
                    {
                        ParsePlanesStuff(id, ref PlayerPlanesBought);
                    }
                    else if (id.Contains("PLK"))
                    {
                        ParsePlanesStuff(id, ref PlayerPlanesKilled);
                    }
                    else if (id.Contains("VSL"))
                    {
                        ParseVesselsStuff(id, ref PlayerVesselsLeft);
                    }
                    else if (id.Contains("VSB"))
                    {
                        ParseVesselsStuff(id, ref PlayerVesselsBought);
                    }
                    else if (id.Contains("VSK"))
                    {
                        ParseVesselsStuff(id, ref PlayerVesselsKilled);
                    }
                    else if (id.Contains("BLL"))
                    {
                        ParseBuildingsStuff(id, ref PlayerBuildingsLeft);
                    }
                    else if (id.Contains("BLB"))
                    {
                        ParseBuildingsStuff(id, ref PlayerBuildingsBought);
                    }
                    else if (id.Contains("BLK"))
                    {
                        ParseBuildingsStuff(id, ref PlayerBuildingsKilled);
                    }
                    else if (id.Contains("BLC"))
                    {
                        ParseBuildingsStuff(id, ref PlayerBuildingsCaptured);
                    }
                    else if (id.Contains("SID"))
                    {
                        ParseSideInfo(id);
                    }
                    else if (id.Contains("HRV"))
                    {
                        Bin.ReadBytes(4);
                        int money = Bin.ReadInt32();
                        Pos += 8;
                        ParseMoneyHarvestedInfo(id, money);
                    }
                    else if (id.Contains("CRA") && id != "CRAT")
                    {
                        ParseCratesCollectedInfo(id);
                    }
                    else if (id.Contains("CRD"))
                    {
                        ParseCreditsInfo(id);
                    }
                    else if (id == "SDFX")
                    {
                        ReadGarbage();
                        SDFX = ReadByte();
                    }
                    else if (id == "IDNO")
                    {
                        ReadGarbage();
                        GameNumber = Read32Bits();
                    }
                    else if (id == "NUMP")
                    {
                        ReadGarbage();
                        NumberOfPlayers = Read32Bits();
                    }
                    else if (id == "REMN")
                    {
                        ReadGarbage();
                        NumberOfRemainingPlayers = Read32Bits();
                    }
                    else if (id == "TRNY")
                    {
                        ReadGarbage();
                        IsTournamentGame = Read32Bits();
                    }
                    else if (id == "CRED")
                    {
                        ReadGarbage();
                        StartingCredits = Read32Bits();
                    }
                    else if (id == "BASE")
                    {
                        BasesEnabled = ReadOnOrOff();
                    }
                    else if (id == "TIBR")
                    {
                        OreRegenerates = ReadOnOrOff();
                    }
                    else if (id == "CRAT")
                    {
                        CratesEnabled = ReadOnOrOff();
                    }
                    else if (id == "AIPL")
                    {
                        ReadGarbage();
                        NumberOfAIPlayers = Read32Bits();
                    }
                    else if (id == "SHAD")
                    {
                        ShroudRegrows = ReadOnOrOff();
                    }
                    else if (id == "FLAG")
                    {
                        CTFEnabled = ReadOnOrOff();
                    }
                    else if (id == "UNIT")
                    {
                        ReadGarbage();
                        StartingUnits = Read32Bits();
                    }
                    else if (id == "TECH")
                    {
                        ReadGarbage();
                        TechLevel = Read32Bits();
                    }
                    else if (id == "SCEN")
                    {
                        MapName = ParseString();
                    }
                    else if (id == "ADR1")
                    {
                        IPAddress1 = ParseString();
                    }
                    else if (id == "ADR2")
                    {
                        IPAddress2 = ParseString();
                    }
                    else if (id == "PING")
                    {
                        Ping = ParseString();
                    }
                    else if (id == "CMPL")
                    {
                        ReadGarbage();
                        CompletionType = ReadByte();
                    }
                    else if (id == "TIME")
                    {
                        ReadGarbage();
                        StartTime = Read32Bits();
                    }
                    else if (id == "DURA")
                    {
                        ReadGarbage();
                        GameDuration = Read32Bits();
                    }
                    else if (id == "AFPS")
                    {
                        ReadGarbage();
                        AverageFPS = Read32Bits();
                    }
                    else if (id == "PROC")
                    {
                        ReadGarbage();
                        ProcessorType = ReadByte();
                    }
                    else if (id == "MEMO")
                    {
                        ReadGarbage();
                        SystemMemory = ReadUnsigned32Bits();
                    }
                    else if (id == "VIDM")
                    {
                        ReadGarbage();
                        VideoMemory = ReadUnsigned32Bits();
                    }
                    else if (id == "SPED")
                    {
                        ReadGarbage();
                        GameSpeed = ReadByte();
                    }
                    else if (id == "VERS")
                    {
                        Version = ParseShortString();
                    }
                    else if (id == "QUIT")
                    {
                        ParseQuitState();
                    }
                    else if (id == "DATE")
                    {
                        ParseDateInfo();
                    }
                }
            }
        }

        public void PrintParsedData()
        {
            Console.WriteLine("Dead state for player 2 = {0}", PlayerDeadStates[1]);
            Console.WriteLine("Spectator state for player 2 = {0}", PlayerSpectatorStates[1]);
            Console.WriteLine("Alliances bitfield for player 3 = {0}, hex = {1:X}", GetAlliancesString(PlayerAlliancesBitFields[2]), PlayerAlliancesBitFields[2]);
            Console.WriteLine("\tPlayer allied with house Neutral: {0}", (PlayerAlliancesBitFields[2] & (1 << 10)) != 0 ? "True" : "False");
            Console.WriteLine("DumpSize = {0}", DumpSize);
            Console.WriteLine("ReportedSize = {0}", ReportedSize);
            Console.WriteLine("SDFX = {0}", SDFX);
            Console.WriteLine("GameNumber = {0}", GameNumber);
            Console.WriteLine("NumberOfPlayers = {0}", NumberOfPlayers);
            Console.WriteLine("NumberOfRemainingPlayers = {0}", NumberOfRemainingPlayers);
            Console.WriteLine("IsTournamentGame = {0}", IsTournamentGame);
            Console.WriteLine("StartingCredits = {0}", StartingCredits);
            Console.WriteLine("BasesEnabled = {0}", BasesEnabled);
            Console.WriteLine("OreRegenerates = {0}", OreRegenerates);
            Console.WriteLine("CratesEnabled = {0}", CratesEnabled);
            Console.WriteLine("NumberOfAIPlayers = {0}", NumberOfAIPlayers);
            Console.WriteLine("ShroudRegrows = {0}", ShroudRegrows);
            Console.WriteLine("CTFEnabled = {0}", CTFEnabled);
            Console.WriteLine("StartingUnits = {0}", StartingUnits);
            Console.WriteLine("TechLevel = {0}", TechLevel);
            Console.WriteLine("MapName = {0}", MapName);
            Console.WriteLine("IPAddress1 = {0}", IPAddress1);
            Console.WriteLine("IPAddress2 = {0}", IPAddress2);
            Console.WriteLine("Ping = {0}", Ping);
            Console.WriteLine("CompletionType = {0}", CompletionType);
            Console.WriteLine("GameDuration = {0}", GameDuration);
            Console.WriteLine("StartTime = {0}", StartTime);
            Console.WriteLine("AverageFPS = {0}", AverageFPS);
            Console.WriteLine("ProcessorType = {0}", ProcessorType);
            Console.WriteLine("SystemMemory = {0}", SystemMemory);
            Console.WriteLine("VideoMemory = {0}", VideoMemory);
            Console.WriteLine("GameSpeed = {0}", GameSpeed);
            Console.WriteLine("Version = {0}", Version);
            Console.WriteLine("GameEXELastWriteTimeUTC = {0}", GameEXELastWriteTimeUTC.ToString());

            PrintPlayerArray(PlayerMoneyHarvested, "Money harvested for player {0} = {1}");
            PrintPlayerArray(PlayerCredits, "Credits for player {0} = {1}");
            PrintPlayerArray(PlayerQuitStates, "Quit state for player {0} = {1}");
            PrintPlayerArray(PlayerColors, "Color for player {0} = {1}");
            PrintPlayerArray(PlayersResigned, "Resigned for player {0} = {1}");
            PrintPlayerArray(PlayersSpawnLocation, "SpawnLocation for player {0} = {1}");
            PrintPlayerArray(PlayersConnectionLost, "ConnectionLost for player {0} = {1}");
            PrintPlayerStringArray(PlayerSides, "Side for player {0} = {1}");
            PrintPlayerStringArray(PlayerNames, "Name for player {0} = {1}");

            for (int i = 0; i < 8; i++)
            {
                int playerNumber = i + 1;
                PrintStructForPlayer(PlayerVehiclesLeft[i], "Vehicles left", playerNumber);
                PrintStructForPlayer(PlayerVehiclesBought[i], "Vehicles bought", playerNumber);
                PrintStructForPlayer(PlayerVehiclesKilled[i], "Vehicles killed", playerNumber);

                PrintStructForPlayer(PlayerInfantryLeft[i], "Infantry left", playerNumber);
                PrintStructForPlayer(PlayerInfantryBought[i], "Infantry bought", playerNumber);
                PrintStructForPlayer(PlayerInfantryKilled[i], "Infantry killed", playerNumber);

                PrintStructForPlayer(PlayerPlanesLeft[i], "Planes left", playerNumber);
                PrintStructForPlayer(PlayerPlanesBought[i], "Planes bought", playerNumber);
                PrintStructForPlayer(PlayerPlanesKilled[i], "Planes killed", playerNumber);

                PrintStructForPlayer(PlayerVesselsLeft[i], "Vessels left", playerNumber);
                PrintStructForPlayer(PlayerVesselsBought[i], "Vessels bought", playerNumber);
                PrintStructForPlayer(PlayerVesselsKilled[i], "Vessels killed", playerNumber);

                PrintStructForPlayer(PlayerBuildingsLeft[i], "Buildings left", playerNumber);
                PrintStructForPlayer(PlayerBuildingsBought[i], "Buildings bought", playerNumber);
                PrintStructForPlayer(PlayerBuildingsKilled[i], "Buildings killed", playerNumber);
                PrintStructForPlayer(PlayerBuildingsCaptured[i], "Buildings captured", playerNumber);
            }
        }

        private void PrintStructForPlayer<T>(T struc, string label, int playerNumber)
        {
            if (struc == null)
                return;

            System.Reflection.FieldInfo[] fields = typeof(T).GetFields();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            foreach (var f in fields)
            {
                object valObj = f.GetValue(struc);
                if (valObj is int)
                {
                    int v = (int)valObj;
                    if (v != 0)
                    {
                        sb.AppendFormat("{0}={1}, ", f.Name, v);
                    }
                }
            }

            if (sb.Length > 0)
            {
                sb.Length -= 2;
                Console.WriteLine("{0} for player {1} = {2}", label, playerNumber, sb.ToString());
            }
        }

        public void ParseQuitState()
        {
            ReadGarbage();
            int playerNum = QuitPlayerNumHelper;

            PlayerQuitStates[playerNum - 1] = ReadByte();
        }

        public void ParseDateInfo()
        {
            ReadGarbage();
            FileTime fTime = new FileTime();

            fTime.DwLowDateTime = ReadUnsigned32Bits();
            fTime.DwHighDateTime = ReadUnsigned32Bits();

            long timeLong = FileTime.FileTimeToLong(fTime);
            GameEXELastWriteTimeUTC = DateTime.FromFileTimeUtc(timeLong);
        }

        public void ParseAlliancesInfo(string id)
        {
            int playerNum = GetPlayerNumberFromId(id);
            ReadGarbage();

            PlayerAlliancesBitFields[playerNum - 1] = Read32Bits();
        }

        public void ParseDeadStateInfo(string id)
        {
            int playerNum = GetPlayerNumberFromId(id);
            ReadGarbage();

            PlayerDeadStates[playerNum - 1] = Read32Bits();
        }

        public void ParseSpectatorStateInfo(string id)
        {
            int playerNum = GetPlayerNumberFromId(id);
            ReadGarbage();

            PlayerSpectatorStates[playerNum - 1] = Read32Bits();
        }

        public void ParseColorInfo(string id)
        {
            int playerNum = GetPlayerNumberFromId(id);
            ReadGarbage();

            PlayerColors[playerNum - 1] = ReadByte();
        }

        public void ParseCreditsInfo(string id)
        {
            int playerNum = GetPlayerNumberFromId(id);
            ReadGarbage();

            PlayerCredits[playerNum - 1] = Read32Bits();
        }

        public void ParseCratesCollectedInfo(string id)
        {
            int playerNum = GetPlayerNumberFromId(id);
            Read32Bits();

            CratesCollectedStruct crates = ParseCratesCollectedForPlayer();

            PlayerCratesCollected[playerNum - 1] = crates;
        }

        public void ParseVehiclesStuff(string id, ref VehiclesStruct[] vehArray)
        {
            int playerNum = GetPlayerNumberFromId(id);
            Read32Bits();

            VehiclesStruct vehicles = ParseVehicles();

            vehArray[playerNum - 1] = vehicles;
        }

        public void ParseVesselsStuff(string id, ref VesselsStruct[] vesArray)
        {
            int playerNum = GetPlayerNumberFromId(id);
            Read32Bits();

            VesselsStruct vessels = ParseVessels();

            vesArray[playerNum - 1] = vessels;
        }

        public void ParseInfantryStuff(string id, ref InfantryStruct[] infArray)
        {
            int playerNum = GetPlayerNumberFromId(id);
            Read32Bits();

            InfantryStruct infantry = ParseInfantry();

            infArray[playerNum - 1] = infantry;
        }

        public void ParseBuildingsStuff(string id, ref BuildingsStruct[] buildingsArray)
        {
            int playerNum = GetPlayerNumberFromId(id);
            Read32Bits();

            BuildingsStruct buildings = ParseBuildings();

            buildingsArray[playerNum - 1] = buildings;
        }

        public void ParsePlanesStuff(string id, ref PlanesStruct[] planesArray)
        {
            int playerNum = GetPlayerNumberFromId(id);
            Read32Bits();

            PlanesStruct planes = ParsePlanes();

            planesArray[playerNum - 1] = planes;
        }

        public string ParseString()
        {
            byte[] bytes = Bin.ReadBytes(4);
            int length = ((int)bytes[3]) - 1;

            byte[] stringBytes = Bin.ReadBytes(length);
            string retString = Encoding.ASCII.GetString(stringBytes);

            int alignRead = 4 - (length % 4);
            if (alignRead == 4)
                alignRead = 0;

            Bin.ReadBytes(alignRead);

            Pos += alignRead + 4 + length;
            return retString;
        }

        public string ParseShortString()
        {
            ReadGarbage();

            byte[] stringBytes = Bin.ReadBytes(3);
            Bin.ReadBytes(1);
            Pos += 4;
            string retString = Encoding.ASCII.GetString(stringBytes);
            return retString;
        }

        public int Read32Bits()
        {
            Pos += 4;
            return Bin.ReadInt32();
        }

        public uint ReadUnsigned32Bits()
        {
            Pos += 4;
            return Bin.ReadUInt32();
        }

        public int ReadByte()
        {
            Pos += 4;
            int byteRead = (int)Bin.ReadBigEndianBytes(1)[0];
            Bin.ReadBigEndianBytes(3);
            return byteRead;
        }

        public void ReadGarbage()
        {
            Read32Bits();
        }

        public int ReadOnOrOff()
        {
            ReadGarbage();

            int ret = -2;
            int bytes = Read32Bits();

            if (bytes == (int)0x4F4E0000)
                ret = 1;
            if (bytes == (int)0x4F464600)
                ret = 0;

            return ret;
        }

        public void ParseSideInfo(string id)
        {
            int playerNum = GetPlayerNumberFromId(id);

            PlayerSides[playerNum - 1] = ParseShortString();

            QuitPlayerNumHelper = playerNum;
        }

        public void ParseNameInfo(string id)
        {
            int playerNum = GetPlayerNumberFromId(id);

            PlayerNames[playerNum - 1] = ParseString();
        }

        public void ParseSpawnLocationInfo(string id)
        {
            int playerNum = GetPlayerNumberFromId(id);
            ReadGarbage();

            PlayersSpawnLocation[playerNum - 1] = Read32Bits();
        }

        public void ParseResignedInfo(string id)
        {
            int playerNum = GetPlayerNumberFromId(id);
            ReadGarbage();

            PlayersResigned[playerNum - 1] = Read32Bits();
        }

        public void ParseConnectionLostInfo(string id)
        {
            int playerNum = GetPlayerNumberFromId(id);
            ReadGarbage();

            PlayersConnectionLost[playerNum - 1] = Read32Bits();
        }

        public CratesCollectedStruct ParseCratesCollectedForPlayer()
        {
            CratesCollectedStruct crates = new CratesCollectedStruct();

            crates.MoneyCrates = Read32Bits();
            crates.UnitCrates = Read32Bits();
            crates.ParabombCrates = Read32Bits();
            crates.HealCrates = Read32Bits();
            crates.StealthCrates = Read32Bits();
            crates.ExplosionCrates = Read32Bits();
            crates.NapalmDeathCrates = Read32Bits();
            crates.SquadCrates = Read32Bits();
            crates.MapReshroud = Read32Bits();
            crates.MapRevealCrates = Read32Bits();
            crates.SonarPulseCrates = Read32Bits();
            crates.ArmorUpgradeCrates = Read32Bits();
            crates.SpeedUpgradeCrates = Read32Bits();
            crates.FirepowerUpgradeCrates = Read32Bits();
            crates.OneShotNukeCrates = Read32Bits();
            crates.TimeQuakeCrates = Read32Bits();
            crates.IronCurtainCrates = Read32Bits();
            crates.ChronoVortexCrates = Read32Bits();

            return crates;
        }

        public VesselsStruct ParseVessels()
        {
            VesselsStruct vessels = new VesselsStruct();

            vessels.Submarines = Read32Bits();
            vessels.Destroyers = Read32Bits();
            vessels.Cruisers = Read32Bits();
            vessels.Gunboats = Read32Bits();
            vessels.MissileSubs = Read32Bits();
            vessels.HeliCarriers = Read32Bits();

            return vessels;
        }

        public PlanesStruct ParsePlanes()
        {
            PlanesStruct planes = new PlanesStruct();

            planes.Chinooks = Read32Bits();
            planes.BadgeBombers = Read32Bits();
            planes.SpyPlanes = Read32Bits();
            planes.MIGs = Read32Bits();
            planes.YAKs = Read32Bits();
            planes.LongBows = Read32Bits();
            planes.Hinds = Read32Bits();

            return planes;
        }

        public BuildingsStruct ParseBuildings()
        {
            BuildingsStruct buildings = new BuildingsStruct();

            buildings.AlliedTechCenters = Read32Bits();
            buildings.IronCurtains = Read32Bits();
            buildings.WarFactories = Read32Bits();
            buildings.Chronospheres = Read32Bits();
            buildings.Pillboxes = Read32Bits();
            buildings.CameoPillboxes = Read32Bits();
            buildings.RadarDomes = Read32Bits();
            buildings.GapGenerators = Read32Bits();
            buildings.Turrets = Read32Bits();
            buildings.AAGuns = Read32Bits();
            buildings.FlameTowers = Read32Bits();
            buildings.ConstructionYards = Read32Bits();
            buildings.Refineries = Read32Bits();
            buildings.OreSilos = Read32Bits();
            buildings.Helipads = Read32Bits();
            buildings.SamSites = Read32Bits();
            buildings.Airfields = Read32Bits();
            buildings.PowerPlants = Read32Bits();
            buildings.AdvancedPowerPlants = Read32Bits();
            buildings.SovietTechCenters = Read32Bits();
            buildings.Hospitals = Read32Bits();
            buildings.SovietBarracks = Read32Bits();
            buildings.AlliesBarracks = Read32Bits();
            buildings.Kennels = Read32Bits();
            buildings.ServiceDepots = Read32Bits();
            buildings.BIOResearchFacilities = Read32Bits();
            buildings.TechnologyCenters = Read32Bits();
            buildings.Shipyards = Read32Bits();
            buildings.Subpens = Read32Bits();
            buildings.MissileSilos = Read32Bits();
            buildings.ForwardCommandPosts = Read32Bits();
            buildings.TeslaCoils = Read32Bits();
            buildings.FakeWarFactories = Read32Bits();
            buildings.FakeConstructionYards = Read32Bits();
            buildings.FakeShipyards = Read32Bits();
            buildings.FakeSubpens = Read32Bits();
            buildings.FakeRadarDomes = Read32Bits();
            buildings.Sandbags = Read32Bits();
            buildings.ChainLinkFences = Read32Bits();
            buildings.ConcreteWalls = Read32Bits();
            buildings.BarbwireFences = Read32Bits();
            buildings.WoodenFences = Read32Bits();
            buildings.WireFences = Read32Bits();
            buildings.AntiTankMines = Read32Bits();
            buildings.AntiPersonnelMines = Read32Bits();
            buildings.V1s = Read32Bits();
            buildings.V2s = Read32Bits();
            buildings.V3s = Read32Bits();
            buildings.V4s = Read32Bits();
            buildings.V5s = Read32Bits();
            buildings.V6s = Read32Bits();
            buildings.V7s = Read32Bits();
            buildings.V8s = Read32Bits();
            buildings.V9s = Read32Bits();
            buildings.V10s = Read32Bits();
            buildings.V11s = Read32Bits();
            buildings.V12s = Read32Bits();
            buildings.V13s = Read32Bits();
            buildings.V14s = Read32Bits();
            buildings.V15s = Read32Bits();
            buildings.V16s = Read32Bits();
            buildings.V17s = Read32Bits();
            buildings.V18s = Read32Bits();
            buildings.V19s = Read32Bits();
            buildings.V20s = Read32Bits();
            buildings.V21s = Read32Bits();
            buildings.V22s = Read32Bits();
            buildings.V23s = Read32Bits();
            buildings.V24s = Read32Bits();
            buildings.V25s = Read32Bits();
            buildings.V26s = Read32Bits();
            buildings.V27s = Read32Bits();
            buildings.V28s = Read32Bits();
            buildings.V29s = Read32Bits();
            buildings.V30s = Read32Bits();
            buildings.V31s = Read32Bits();
            buildings.V32s = Read32Bits();
            buildings.V33s = Read32Bits();
            buildings.V34s = Read32Bits();
            buildings.V35s = Read32Bits();
            buildings.V36s = Read32Bits();
            buildings.V37s = Read32Bits();
            buildings.Barrels = Read32Bits();
            buildings.BarrelsGroups = Read32Bits();
            buildings.AntQueens = Read32Bits();
            buildings.Larva1s = Read32Bits();
            buildings.Larva2s = Read32Bits();

            return buildings;
        }

        public VehiclesStruct ParseVehicles()
        {
            VehiclesStruct vehicles = new VehiclesStruct();

            vehicles.MammothTanks = Read32Bits();
            vehicles.HeavyTanks = Read32Bits();
            vehicles.MediumTanks = Read32Bits();
            vehicles.LightTanks = Read32Bits();
            vehicles.APCs = Read32Bits();
            vehicles.MineLayers = Read32Bits();
            vehicles.Rangers = Read32Bits();
            vehicles.OreTrucks = Read32Bits();
            vehicles.Artilleries = Read32Bits();
            vehicles.MobileRadarJammers = Read32Bits();
            vehicles.MobileGapGenerators = Read32Bits();
            vehicles.MCVs = Read32Bits();
            vehicles.V2RocketLaunchers = Read32Bits();
            vehicles.SupplyTrucks = Read32Bits();
            vehicles.ANT1s = Read32Bits();
            vehicles.ANT2s = Read32Bits();
            vehicles.ANT3s = Read32Bits();
            vehicles.ChronoTanks = Read32Bits();
            vehicles.TeslaTanks = Read32Bits();
            vehicles.MADTanks = Read32Bits();
            vehicles.DemoTrucks = Read32Bits();
            vehicles.PhaseTransports = Read32Bits();

            return vehicles;
        }

        public InfantryStruct ParseInfantry()
        {
            InfantryStruct infantry = new InfantryStruct();

            infantry.RifleInfantries = Read32Bits();
            infantry.Grenadiers = Read32Bits();
            infantry.RocketSoldiers = Read32Bits();
            infantry.Flamethrowers = Read32Bits();
            infantry.Engineers = Read32Bits();
            infantry.Tanyas = Read32Bits();
            infantry.Spies = Read32Bits();
            infantry.Thieves = Read32Bits();
            infantry.Medics = Read32Bits();
            infantry.GNRLs = Read32Bits();
            infantry.Dogs = Read32Bits();
            infantry.C1s = Read32Bits();
            infantry.C2s = Read32Bits();
            infantry.C3s = Read32Bits();
            infantry.C4s = Read32Bits();
            infantry.C5s = Read32Bits();
            infantry.C6s = Read32Bits();
            infantry.C7s = Read32Bits();
            infantry.C8s = Read32Bits();
            infantry.C9s = Read32Bits();
            infantry.C10s = Read32Bits();
            infantry.Einsteins = Read32Bits();
            infantry.Delphis = Read32Bits();
            infantry.Chans = Read32Bits();
            infantry.ShockTroopers = Read32Bits();
            infantry.Mechanics = Read32Bits();

            return infantry;
        }

        public void ParseMoneyHarvestedInfo(string id, int money)
        {
            int playerNumber = GetPlayerNumberFromId(id);

            PlayerMoneyHarvested[playerNumber - 1] = money;
        }

        public int GetPlayerNumberFromId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length < 4)
                throw new StatsDumpException("Invalid player ID string");

            string tmp = id.Substring(3, 1);

            if (!int.TryParse(tmp, out int playerNumber))
            {
                throw new StatsDumpException("Failed to parse player number from ID string");
            }

            if (playerNumber < 1 || playerNumber > 8)
            {
                throw new StatsDumpException("Player number was incorrectly parsed from ID string");
            }

            return playerNumber;
        }

        public void PrintPlayerArray(int[] array, string format)
        {
            int playerNumber = 1;
            foreach (int element in array)
            {
                if (element != -1)
                {
                    Console.WriteLine(format, playerNumber, element);
                }
                playerNumber++;
            }
        }

        public void PrintPlayerStringArray(string[] array, string format)
        {
            int playerNumber = 1;
            foreach (string element in array)
            {
                if (element != null)
                {
                    Console.WriteLine(format, playerNumber, element);
                }
                playerNumber++;
            }
        }

        private static string GetAlliancesString(int bitField)
        {
            if (bitField == -1)
                return "";

            string str = "";

            for (int i = 12; i < 20; i++)
            {
                if ((bitField & (1 << i)) != 0)
                {
                    str += string.Format("{0}|", i - 11);
                }
            }

            if (str.Length > 1)
            {
                str = str.Remove(str.Length - 1);
            }

            return str;
        }
    }

    public struct FileTime
    {
        public uint DwLowDateTime;
        public uint DwHighDateTime;

        public static long FileTimeToLong(FileTime ft)
        {
            long hFt2 = (((long)ft.DwHighDateTime) << 32) + ft.DwLowDateTime;
            return hFt2;
        }
    }

    [Serializable()]
    public class StatsDumpLengthException : StatsDumpException
    {
        public StatsDumpLengthException() : base() { }
        public StatsDumpLengthException(string message) : base(message) { }
        public StatsDumpLengthException(string message, System.Exception inner) : base(message, inner) { }

        protected StatsDumpLengthException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
        }
    }

    [Serializable()]
    public class StatsDumpException : System.Exception
    {
        public StatsDumpException() : base() { }
        public StatsDumpException(string message) : base(message) { }
        public StatsDumpException(string message, System.Exception inner) : base(message, inner) { }

        protected StatsDumpException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
        }
    }

    public class BigEndianReader
    {
        public BigEndianReader(BinaryReader baseReader)
        {
            BaseReader = baseReader;
        }

        public short ReadInt16()
        {
            byte[] b = ReadBigEndianBytes(2);
            return (short)((b[0] << 8) | b[1]);
        }

        public ushort ReadUInt16()
        {
            byte[] b = ReadBigEndianBytes(2);
            return (ushort)((b[0] << 8) | b[1]);
        }

        public uint ReadUInt32()
        {
            byte[] b = ReadBigEndianBytes(4);
            return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
        }

        public int ReadInt32()
        {
            byte[] b = ReadBigEndianBytes(4);
            return (int)(((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3]);
        }

        public byte[] ReadBigEndianBytes(int count)
        {
            byte[] bytes = new byte[count];
            for (int i = count - 1; i >= 0; i--)
                bytes[i] = BaseReader.ReadByte();

            return bytes;
        }

        public byte[] ReadBytes(int count)
        {
            return BaseReader.ReadBytes(count);
        }

        public void Close()
        {
            BaseReader.Close();
        }

        public Stream BaseStream
        {
            get { return BaseReader.BaseStream; }
        }

        private BinaryReader BaseReader;
    }

    public struct CratesCollectedStruct
    {
        public int MoneyCrates;
        public int UnitCrates;
        public int ParabombCrates;
        public int HealCrates;
        public int StealthCrates;
        public int ExplosionCrates;
        public int NapalmDeathCrates;
        public int SquadCrates;
        public int MapReshroud;
        public int MapRevealCrates;
        public int SonarPulseCrates;
        public int ArmorUpgradeCrates;
        public int SpeedUpgradeCrates;
        public int FirepowerUpgradeCrates;
        public int OneShotNukeCrates;
        public int TimeQuakeCrates;
        public int IronCurtainCrates;
        public int ChronoVortexCrates;
    }

    public struct PlanesStruct
    {
        public int Chinooks;
        public int BadgeBombers;
        public int SpyPlanes;
        public int MIGs;
        public int YAKs;
        public int LongBows;
        public int Hinds;
    }

    public struct VesselsStruct
    {
        public int Submarines;
        public int Destroyers;
        public int Cruisers;
        public int Gunboats;
        public int MissileSubs;
        public int HeliCarriers;
    }

    public struct VehiclesStruct
    {
        public int MammothTanks;
        public int HeavyTanks;
        public int MediumTanks;
        public int LightTanks;
        public int APCs;
        public int MineLayers;
        public int Rangers;
        public int OreTrucks;
        public int Artilleries;
        public int MobileRadarJammers;
        public int MobileGapGenerators;
        public int MCVs;
        public int V2RocketLaunchers;
        public int SupplyTrucks;
        public int ANT1s;
        public int ANT2s;
        public int ANT3s;
        public int ChronoTanks;
        public int TeslaTanks;
        public int MADTanks;
        public int DemoTrucks;
        public int PhaseTransports;
    }

    public struct InfantryStruct
    {
        public int RifleInfantries;
        public int Grenadiers;
        public int RocketSoldiers;
        public int Flamethrowers;
        public int Engineers;
        public int Tanyas;
        public int Spies;
        public int Thieves;
        public int Medics;
        public int GNRLs;
        public int Dogs;
        public int C1s;
        public int C2s;
        public int C3s;
        public int C4s;
        public int C5s;
        public int C6s;
        public int C7s;
        public int C8s;
        public int C9s;
        public int C10s;
        public int Einsteins;
        public int Delphis;
        public int Chans;
        public int ShockTroopers;
        public int Mechanics;
    }

    public struct BuildingsStruct
    {
        public int AlliedTechCenters;
        public int IronCurtains;
        public int WarFactories;
        public int Chronospheres;
        public int Pillboxes;
        public int CameoPillboxes;
        public int RadarDomes;
        public int GapGenerators;
        public int Turrets;
        public int AAGuns;
        public int FlameTowers;
        public int ConstructionYards;
        public int Refineries;
        public int OreSilos;
        public int Helipads;
        public int SamSites;
        public int Airfields;
        public int PowerPlants;
        public int AdvancedPowerPlants;
        public int SovietTechCenters;
        public int Hospitals;
        public int SovietBarracks;
        public int AlliesBarracks;
        public int Kennels;
        public int ServiceDepots;
        public int BIOResearchFacilities;
        public int TechnologyCenters;
        public int Shipyards;
        public int Subpens;
        public int MissileSilos;
        public int ForwardCommandPosts;
        public int TeslaCoils;
        public int FakeWarFactories;
        public int FakeConstructionYards;
        public int FakeShipyards;
        public int FakeSubpens;
        public int FakeRadarDomes;
        public int Sandbags;
        public int ChainLinkFences;
        public int ConcreteWalls;
        public int BarbwireFences;
        public int WoodenFences;
        public int WireFences;
        public int AntiTankMines;
        public int AntiPersonnelMines;
        public int V1s;
        public int V2s;
        public int V3s;
        public int V4s;
        public int V5s;
        public int V6s;
        public int V7s;
        public int V8s;
        public int V9s;
        public int V10s;
        public int V11s;
        public int V12s;
        public int V13s;
        public int V14s;
        public int V15s;
        public int V16s;
        public int V17s;
        public int V18s;
        public int V19s;
        public int V20s;
        public int V21s;
        public int V22s;
        public int V23s;
        public int V24s;
        public int V25s;
        public int V26s;
        public int V27s;
        public int V28s;
        public int V29s;
        public int V30s;
        public int V31s;
        public int V32s;
        public int V33s;
        public int V34s;
        public int V35s;
        public int V36s;
        public int V37s;
        public int Barrels;
        public int BarrelsGroups;
        public int AntQueens;
        public int Larva1s;
        public int Larva2s;
    }
}