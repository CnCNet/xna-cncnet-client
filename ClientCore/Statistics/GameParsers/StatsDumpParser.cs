using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ra303pStatsDumpParser;

namespace ClientCore.Statistics.GameParsers
{
    class StatsDumpParser
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


        public StatsDumpParser(string FileName)
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

            // Parse the file
            this.Parse_Stats_Dump_File(FileName);
        }

        public void Parse_Stats_Dump_File(string FileName)
        {
            using (BinaryReader b = new BinaryReader(File.Open(FileName, FileMode.Open)))
            {
                Bin = new BigEndianReader(b);
                // 2.
                // Position and length variables.
                this.Pos = 0; // USE the class field, not a new local variable
                this.DumpSize = (int)Bin.BaseStream.Length;

                // Size needs to be at least 4 bytes for the size header in the dump file
                if (this.DumpSize < 4)
                {
                    throw new StatsDumpLengthException();
                }
                this.ReportedSize = Bin.ReadInt16();
                Bin.ReadInt16(); // advance position by 2 bytes
                this.Pos += 2;

                int length = (int)Bin.BaseStream.Length;
                while (this.Pos < this.DumpSize)
                {
                    Byte[] Bytes = Bin.ReadBytes(4); this.Pos += 4;
                    string ID = System.Text.Encoding.Default.GetString(Bytes);

                    if (ID.Contains("RSG"))
                    {
                        Parse_Resigned_Info(ID);
                    }

                    if (ID.Contains("CON"))
                    {
                        Parse_Connection_Lost_Info(ID);
                    }

                    if (ID.Contains("SPA"))
                    {
                        Parse_Spawn_Location_Info(ID);
                    }

                    if (ID.Contains("NAM"))
                    {
                        Parse_Name_Info(ID);
                    }

                    else if (ID.Contains("COL"))
                    {
                        Parse_Color_Info(ID);
                    }

                    else if (ID.Contains("ALY"))
                    {
                        Parse_Alliances_Info(ID);
                    }

                    else if (ID.Contains("SPC"))
                    {
                        Parse_Spectator_State_Info(ID);
                    }

                    else if (ID.Contains("DED"))
                    {
                        Parse_Dead_State_Info(ID);
                    }

                    else if (ID.Contains("UNL"))
                    {
                        Parse_Vehicles_Stuff(ID, ref this.PlayerVehiclesLeft);
                    }

                    else if (ID.Contains("UNB"))
                    {
                        Parse_Vehicles_Stuff(ID, ref this.PlayerVehiclesBought);
                    }

                    else if (ID.Contains("UNK"))
                    {
                        Parse_Vehicles_Stuff(ID, ref this.PlayerVehiclesKilled);
                    }

                    else if (ID.Contains("INL"))
                    {
                        Parse_Infantry_Stuff(ID, ref this.PlayerInfantryLeft);
                    }

                    else if (ID.Contains("INB"))
                    {
                        Parse_Infantry_Stuff(ID, ref this.PlayerInfantryBought);
                    }

                    else if (ID.Contains("INK"))
                    {
                        Parse_Infantry_Stuff(ID, ref this.PlayerInfantryKilled);
                    }

                    else if (ID.Contains("PLL"))
                    {
                        Parse_Planes_Stuff(ID, ref this.PlayerPlanesLeft);
                    }

                    else if (ID.Contains("PLB"))
                    {
                        Parse_Planes_Stuff(ID, ref this.PlayerPlanesBought);
                    }

                    else if (ID.Contains("PLK"))
                    {
                        Parse_Planes_Stuff(ID, ref this.PlayerPlanesKilled);
                    }

                    else if (ID.Contains("VSL"))
                    {
                        Parse_Vessels_Stuff(ID, ref this.PlayerVesselsLeft);
                    }

                    else if (ID.Contains("VSB"))
                    {
                        Parse_Vessels_Stuff(ID, ref this.PlayerVesselsBought);
                    }

                    else if (ID.Contains("VSK"))
                    {
                        Parse_Vessels_Stuff(ID, ref this.PlayerVesselsKilled);
                    }

                    else if (ID.Contains("BLL"))
                    {
                        Parse_Buildings_Stuff(ID, ref this.PlayerBuildingsLeft);
                    }

                    else if (ID.Contains("BLB"))
                    {
                        Parse_Buildings_Stuff(ID, ref this.PlayerBuildingsBought);
                    }

                    else if (ID.Contains("BLK"))
                    {
                        Parse_Buildings_Stuff(ID, ref this.PlayerBuildingsKilled);
                    }

                    else if (ID.Contains("BLC"))
                    {
                        Parse_Buildings_Stuff(ID, ref this.PlayerBuildingsCaptured);
                    }

                    else if (ID.Contains("SID"))
                    {
                        Parse_Side_Info(ID);
                    }

                    else if (ID.Contains("HRV"))
                    {
                        Bin.ReadBytes(4);
                        int Money = Bin.ReadInt32();
                        this.Pos += 8;
                        this.Parse_Money_Harvested_Info(ID, Money);
                    }

                    else if (ID.Contains("CRA") && ID != "CRAT")
                    {
                        this.Parse_Crates_Collected_Info(ID);
                    }

                    else if (ID.Contains("CRD"))
                    {
                        this.Parse_Credits_Info(ID);
                    }

                    else if (ID == "SDFX")
                    {
                        Read_Garbage();
                        this.SDFX = this.Read_Byte();
                    }

                    else if (ID == "IDNO")
                    {
                        Read_Garbage();
                        this.GameNumber = this.Read_32Bits();
                    }

                    else if (ID == "NUMP")
                    {
                        Read_Garbage();
                        this.NumberOfPlayers = this.Read_32Bits();
                    }
                    else if (ID == "REMN")
                    {
                        Read_Garbage();
                        this.NumberOfRemainingPlayers = this.Read_32Bits();
                    }
                    else if (ID == "TRNY")
                    {
                        Read_Garbage();
                        this.IsTournamentGame = this.Read_32Bits();
                    }
                    else if (ID == "CRED")
                    {
                        Read_Garbage();
                        this.StartingCredits = this.Read_32Bits();
                    }
                    else if (ID == "BASE")
                    {
                        this.BasesEnabled = this.Read_ON_Or_OFF();
                    }
                    else if (ID == "TIBR")
                    {
                        this.OreRegenerates = this.Read_ON_Or_OFF();
                    }
                    else if (ID == "CRAT")
                    {
                        this.CratesEnabled = this.Read_ON_Or_OFF();
                    }
                    else if (ID == "AIPL")
                    {
                        Read_Garbage();
                        this.NumberOfAIPlayers = this.Read_32Bits();
                    }
                    else if (ID == "SHAD")
                    {
                        this.ShroudRegrows = this.Read_ON_Or_OFF();
                    }
                    else if (ID == "FLAG")
                    {
                        this.CTFEnabled = this.Read_ON_Or_OFF();
                    }
                    else if (ID == "UNIT")
                    {
                        Read_Garbage();
                        this.StartingUnits = this.Read_32Bits();
                    }
                    else if (ID == "TECH")
                    {
                        Read_Garbage();
                        this.TechLevel = this.Read_32Bits();
                    }
                    else if (ID == "SCEN")
                    {
                        this.MapName = this.Parse_String();
                    }
                    else if (ID == "ADR1")
                    {
                        this.IPAddress1 = this.Parse_String();
                    }
                    else if (ID == "ADR2")
                    {
                        this.IPAddress2 = this.Parse_String();
                    }
                    else if (ID == "PING")
                    {
                        this.Ping = this.Parse_String();
                    }
                    else if (ID == "CMPL")
                    {
                        Read_Garbage();
                        this.CompletionType = this.Read_Byte();
                    }
                    else if (ID == "TIME")
                    {
                        Read_Garbage();
                        this.StartTime = this.Read_32Bits();
                    }
                    else if (ID == "DURA")
                    {
                        Read_Garbage();
                        this.GameDuration = this.Read_32Bits();
                    }
                    else if (ID == "AFPS")
                    {
                        Read_Garbage();
                        this.AverageFPS = this.Read_32Bits();
                    }
                    else if (ID == "PROC")
                    {
                        Read_Garbage();
                        this.ProcessorType = this.Read_Byte();
                    }
                    else if (ID == "MEMO")
                    {
                        Read_Garbage();
                        this.SystemMemory = this.Read_Unsigned_32Bits();
                    }
                    else if (ID == "VIDM")
                    {
                        Read_Garbage();
                        this.VideoMemory = this.Read_Unsigned_32Bits();
                    }
                    else if (ID == "SPED")
                    {
                        Read_Garbage();
                        this.GameSpeed = this.Read_Byte();
                    }
                    else if (ID == "VERS")
                    {
                        this.Version = this.Parse_Short_String();
                    }
                    else if (ID == "QUIT")
                    {
                        this.Parse_Quit_State();
                    }
                    else if (ID == "DATE")
                    {
                        this.Parse_Date_Info();
                    }
                }
            }
        }

        public void Print_Parsed_Data()
        {
            Console.WriteLine("Dead state for player 2 = {0}", PlayerDeadStates[1]);
            Console.WriteLine("Spectator state for player 2 = {0}", PlayerSpectatorStates[1]);
            Console.WriteLine("Alliances bitfield for player 3 = {0}, hex = {1:X}", Get_Alliances_String(PlayerAlliancesBitFields[2]), PlayerAlliancesBitFields[2] );
            Console.WriteLine("\tPlayer allied with house Neutral: {0}", (PlayerAlliancesBitFields[2] & (1 << 10)) != 0 ? "True" : "False");
            Console.WriteLine("DumpSize = {0}", this.DumpSize);
            Console.WriteLine("ReportedSize = {0}", this.ReportedSize);
            Console.WriteLine("SDFX = {0}", this.SDFX);
            Console.WriteLine("GameNumber = {0}", this.GameNumber);
            Console.WriteLine("NumberOfPlayers = {0}", this.NumberOfPlayers);
            Console.WriteLine("NumberOfRemainingPlayers = {0}", this.NumberOfRemainingPlayers);
            Console.WriteLine("IsTournamentGame = {0}", this.IsTournamentGame);
            Console.WriteLine("StartingCredits = {0}", this.StartingCredits);
            Console.WriteLine("BasesEnabled = {0}", this.BasesEnabled);
            Console.WriteLine("OreRegenerates = {0}", this.OreRegenerates);
            Console.WriteLine("CratesEnabled = {0}", this.CratesEnabled);
            Console.WriteLine("NumberOfAIPlayers = {0}", this.NumberOfAIPlayers);
            Console.WriteLine("ShroudRegrows = {0}", this.ShroudRegrows);
            Console.WriteLine("CTFEnabled = {0}", this.CTFEnabled);
            Console.WriteLine("StartingUnits = {0}", this.StartingUnits);
            Console.WriteLine("TechLevel = {0}", this.TechLevel);
            Console.WriteLine("MapName = {0}", this.MapName);
            Console.WriteLine("IPAddress1 = {0}", this.IPAddress1);
            Console.WriteLine("IPAddress2 = {0}", this.IPAddress2);
            Console.WriteLine("Ping = {0}", this.Ping);
            Console.WriteLine("CompletionType = {0}", this.CompletionType);
            Console.WriteLine("GameDuration = {0}", this.GameDuration);
            Console.WriteLine("StartTime = {0}", this.StartTime);
            Console.WriteLine("AverageFPS = {0}", this.AverageFPS);
            Console.WriteLine("ProcessorType = {0}", this.ProcessorType);
            Console.WriteLine("SystemMemory = {0}", this.SystemMemory);
            Console.WriteLine("VideoMemory = {0}", this.VideoMemory);
            Console.WriteLine("GameSpeed = {0}", this.GameSpeed);
            Console.WriteLine("Version = {0}", this.Version);
            Console.WriteLine("GameEXELastWriteTimeUTC = {0}", this.GameEXELastWriteTimeUTC.ToString());

            this.Print_Player_Array(this.PlayerMoneyHarvested, "Money harvested for player {0} = {1}");
            this.Print_Player_Array(this.PlayerCredits, "Credits for player {0} = {1}");
            this.Print_Player_Array(this.PlayerQuitStates, "Quit state for player {0} = {1}");
            this.Print_Player_Array(this.PlayerColors, "Color for player {0} = {1}");
            this.Print_Player_Array(this.PlayersResigned, "Resigned for player {0} = {1}");
            this.Print_Player_Array(this.PlayersSpawnLocation, "SpawnLocation for player {0} = {1}");
            this.Print_Player_Array(this.PlayersConnectionLost, "ConnectionLost for player {0} = {1}");
            this.Print_Player_String_Array(this.PlayerSides, "Side for player {0} = {1}");
            this.Print_Player_String_Array(this.PlayerNames, "Name for player {0} = {1}");

            // Print unit/building structs for each player (only non-zero fields are shown)
            for (int i = 0; i < 8; i++)
            {
                int playerNumber = i + 1;
                Print_Struct_For_Player(this.PlayerVehiclesLeft[i], "Vehicles left", playerNumber);
                Print_Struct_For_Player(this.PlayerVehiclesBought[i], "Vehicles bought", playerNumber);
                Print_Struct_For_Player(this.PlayerVehiclesKilled[i], "Vehicles killed", playerNumber);

                Print_Struct_For_Player(this.PlayerInfantryLeft[i], "Infantry left", playerNumber);
                Print_Struct_For_Player(this.PlayerInfantryBought[i], "Infantry bought", playerNumber);
                Print_Struct_For_Player(this.PlayerInfantryKilled[i], "Infantry killed", playerNumber);

                Print_Struct_For_Player(this.PlayerPlanesLeft[i], "Planes left", playerNumber);
                Print_Struct_For_Player(this.PlayerPlanesBought[i], "Planes bought", playerNumber);
                Print_Struct_For_Player(this.PlayerPlanesKilled[i], "Planes killed", playerNumber);

                Print_Struct_For_Player(this.PlayerVesselsLeft[i], "Vessels left", playerNumber);
                Print_Struct_For_Player(this.PlayerVesselsBought[i], "Vessels bought", playerNumber);
                Print_Struct_For_Player(this.PlayerVesselsKilled[i], "Vessels killed", playerNumber);

                Print_Struct_For_Player(this.PlayerBuildingsLeft[i], "Buildings left", playerNumber);
                Print_Struct_For_Player(this.PlayerBuildingsBought[i], "Buildings bought", playerNumber);
                Print_Struct_For_Player(this.PlayerBuildingsKilled[i], "Buildings killed", playerNumber);
                Print_Struct_For_Player(this.PlayerBuildingsCaptured[i], "Buildings captured", playerNumber);
            }
        }

        // Generic helper that prints only non-zero fields of any struct used (Vehicles/Infantry/Buildings/etc.)
        private void Print_Struct_For_Player<T>(T struc, string label, int playerNumber)
        {
            if (struc == null) return; // just in case (structs can't be null, but arrays might be)
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
                sb.Length -= 2; // trim trailing ", "
                Console.WriteLine("{0} for player {1} = {2}", label, playerNumber, sb.ToString());
            }
        }

        public void Parse_Quit_State()
        {
            Read_Garbage();
            int PlayerNum = this.QuitPlayerNumHelper;

            this.PlayerQuitStates[PlayerNum - 1] = this.Read_Byte();
        }

        public void Parse_Date_Info()
        {
            Read_Garbage();
            FileTime FTime = new FileTime();

            FTime.dwLowDateTime = this.Read_Unsigned_32Bits();
            FTime.dwHighDateTime = this.Read_Unsigned_32Bits();

            long TimeLong = FileTime.FileTime_To_Long(FTime);
            this.GameEXELastWriteTimeUTC = DateTime.FromFileTimeUtc(TimeLong);

        }

        public void Parse_Alliances_Info(string ID)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);
            this.Read_Garbage();

            this.PlayerAlliancesBitFields[PlayerNum - 1] = this.Read_32Bits();
        }

        public void Parse_Dead_State_Info(string ID)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);
            this.Read_Garbage();

            this.PlayerDeadStates[PlayerNum - 1] = this.Read_32Bits();
        }

        public void Parse_Spectator_State_Info(string ID)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);
            this.Read_Garbage();

            this.PlayerSpectatorStates[PlayerNum - 1] = this.Read_32Bits();
        }

        public void Parse_Color_Info(string ID)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);
            this.Read_Garbage();

            this.PlayerColors[PlayerNum - 1] = this.Read_Byte();
        }

        public void Parse_Credits_Info(string ID)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);
            this.Read_Garbage();

            this.PlayerCredits[PlayerNum - 1] = this.Read_32Bits();
        }

        public void Parse_Crates_Collected_Info(string ID)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);
            this.Read_32Bits(); // Read garbage

            CratesCollectedStruct Crates = Parse_Crates_Collected_For_Player();

            this.PlayerCratesCollected[PlayerNum - 1] = Crates;
        }

        public void Parse_Vehicles_Stuff(string ID, ref VehiclesStruct[] VehArray)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);
            this.Read_32Bits(); // Read garbage

            VehiclesStruct Vehicles = Parse_Vehicles();

            VehArray[PlayerNum - 1] = Vehicles;
        }

        public void Parse_Vessels_Stuff(string ID, ref VesselsStruct[] VesArray)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);
            this.Read_32Bits(); // Read garbage

            VesselsStruct Vessels = Parse_Vessels();

            VesArray[PlayerNum - 1] = Vessels;
        }

        public void Parse_Infantry_Stuff(string ID, ref InfantryStruct[] InfArray)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);
            this.Read_32Bits(); // Read garbage

            InfantryStruct Infantry = Parse_Infantry();

            InfArray[PlayerNum - 1] = Infantry;
        }

        public void Parse_Buildings_Stuff(string ID, ref BuildingsStruct[] BuildingsArray)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);
            this.Read_32Bits(); // Read garbage

            BuildingsStruct Buildings = Parse_Buildings();

            BuildingsArray[PlayerNum - 1] = Buildings;
        }

        public void Parse_Planes_Stuff(string ID, ref PlanesStruct[] PlanesArray)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);
            this.Read_32Bits(); // Read garbage

            PlanesStruct Planes = Parse_Planes();

            PlanesArray[PlayerNum - 1] = Planes;
        }

        public string Parse_String()
        {
            byte[] Bytes = Bin.ReadBytes(4);
            int Length = ((int)Bytes[3]) - 1;

            byte[] StringBytes = Bin.ReadBytes(Length);
            string RetString = System.Text.Encoding.Default.GetString(StringBytes);

            int AlignRead = 4 - (Length % 4);
            if (AlignRead == 4) { AlignRead = 0; }
            Bin.ReadBytes(AlignRead); // Read for 4 byte alignment

//            Console.WriteLine("Length = {0}, AlignRead = {1}", Length, AlignRead);
            Pos += AlignRead + 4 + Length;
            return RetString;
        }

        public string Parse_Short_String()
        {
            Read_Garbage();


            byte[] StringBytes = Bin.ReadBytes(3);
            Bin.ReadBytes(1);
            Pos += 4;
            string RetString = System.Text.Encoding.Default.GetString(StringBytes);
            return RetString;
        }

        public int Read_32Bits()
        {
            Pos += 4;
            return Bin.ReadInt32();
        }

        public uint Read_Unsigned_32Bits()
        {
            Pos += 4;
            return Bin.ReadUInt32();
        }

        public int Read_Byte()
        {
            Pos += 4;
            int ByteRead = (int)Bin.ReadBigEndianBytes(1)[0];
            Bin.ReadBigEndianBytes(3); // throw away
            return ByteRead;
        }

        public void Read_Garbage()
        {
            this.Read_32Bits();
        }

        // Return 1 if ASCII string "ON" is read or 0 if "OFF" is read,
        // return -2 if something else was read
        public int Read_ON_Or_OFF()
        {
            Read_Garbage();

            int Ret = -2;
            int Bytes = this.Read_32Bits();

            if (Bytes == (int)0x4F4E0000) { Ret = 1; }
            if (Bytes == (int)0x4F464600) { Ret = 0; }

            return Ret;
        }

        public void Parse_Side_Info(String ID)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);

            this.PlayerSides[PlayerNum - 1] = Parse_Short_String();

            this.QuitPlayerNumHelper = PlayerNum; // To help parsing QUIT per player
        }

        public void Parse_Name_Info(String ID)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);

            this.PlayerNames[PlayerNum - 1] = Parse_String();
        }

        public void Parse_Spawn_Location_Info(String ID)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);
            this.Read_Garbage();

            this.PlayersSpawnLocation[PlayerNum - 1] = this.Read_32Bits();
        }

        public void Parse_Resigned_Info(String ID)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);
            this.Read_Garbage();

            this.PlayersResigned[PlayerNum - 1] = this.Read_32Bits();
        }

        public void Parse_Connection_Lost_Info(String ID)
        {
            int PlayerNum = Get_Player_Number_From_ID(ID);
            this.Read_Garbage();

            this.PlayersConnectionLost[PlayerNum - 1] = this.Read_32Bits();
        }

        public CratesCollectedStruct Parse_Crates_Collected_For_Player()
        {
            CratesCollectedStruct Crates = new CratesCollectedStruct();

            Crates.MoneyCrates = this.Read_32Bits();
            Crates.UnitCrates = this.Read_32Bits();
            Crates.ParabombCrates = this.Read_32Bits();
            Crates.HealCrates = this.Read_32Bits();
            Crates.StealthCrates = this.Read_32Bits();
            Crates.ExplosionCrates = this.Read_32Bits();
            Crates.NapalmDeathCrates = this.Read_32Bits();
            Crates.SquadCrates = this.Read_32Bits();
            Crates.MapReshroud = this.Read_32Bits();
            Crates.MapRevealCrates = this.Read_32Bits();
            Crates.SonarPulseCrates = this.Read_32Bits();
            Crates.ArmorUpgradeCrates = this.Read_32Bits();
            Crates.SpeedUpgradeCrates = this.Read_32Bits();
            Crates.FirepowerUpgradeCrates = this.Read_32Bits();
            Crates.OneShotNukeCrates = this.Read_32Bits();
            Crates.TimeQuakeCrates = this.Read_32Bits();
            Crates.IronCurtainCrates = this.Read_32Bits();
            Crates.ChronoVortexCrates = this.Read_32Bits();

            return Crates;
        }

        public VesselsStruct Parse_Vessels()
        {
            VesselsStruct Vessels = new VesselsStruct();

            Vessels.Submarines = this.Read_32Bits();
            Vessels.Destroyers = this.Read_32Bits();
            Vessels.Cruisers = this.Read_32Bits();
            Vessels.Gunboats = this.Read_32Bits();
            Vessels.MissileSubs = this.Read_32Bits();
            Vessels.HeliCarriers = this.Read_32Bits(); // Aftermath hidden unit

            return Vessels;
        }

        public PlanesStruct Parse_Planes()
        {
            PlanesStruct Planes = new PlanesStruct();

            Planes.Chinooks = Read_32Bits();
            Planes.BadgeBombers = Read_32Bits();
            Planes.SpyPlanes = Read_32Bits();
            Planes.MIGs = Read_32Bits();
            Planes.YAKs = Read_32Bits();
            Planes.LongBows = Read_32Bits();
            Planes.Hinds = Read_32Bits();

            return Planes;
        }

        public BuildingsStruct Parse_Buildings()
        {
            BuildingsStruct Buildings = new BuildingsStruct();

            Buildings.AlliedTechCenters = Read_32Bits();
            Buildings.IronCurtains = Read_32Bits();
            Buildings.WarFactories = Read_32Bits();
            Buildings.Chronospheres = Read_32Bits();
            Buildings.Pillboxes = Read_32Bits();
            Buildings.CameoPillboxes = Read_32Bits();
            Buildings.RadarDomes = Read_32Bits();
            Buildings.GapGenerators = Read_32Bits();
            Buildings.Turrets = Read_32Bits();
            Buildings.AAGuns = Read_32Bits();
            Buildings.FlameTowers = Read_32Bits();
            Buildings.ConstructionYards = Read_32Bits();
            Buildings.Refineries = Read_32Bits();
            Buildings.OreSilos = Read_32Bits();
            Buildings.Helipads = Read_32Bits();
            Buildings.SamSites = Read_32Bits();
            Buildings.Airfields = Read_32Bits();
            Buildings.PowerPlants = Read_32Bits();
            Buildings.AdvancedPowerPlants = Read_32Bits();
            Buildings.SovietTechCenters = Read_32Bits();
            Buildings.Hospitals = Read_32Bits();
            Buildings.SovietBarracks = Read_32Bits();
            Buildings.AlliesBarracks = Read_32Bits();
            Buildings.Kennels = Read_32Bits();
            Buildings.ServiceDepots = Read_32Bits();
            Buildings.BIOResearchFacilities = Read_32Bits();
            Buildings.TechnologyCenters = Read_32Bits();
            Buildings.Shipyards = Read_32Bits();
            Buildings.Subpens = Read_32Bits();
            Buildings.MissileSilos = Read_32Bits();
            Buildings.ForwardCommandPosts = Read_32Bits();
            Buildings.TeslaCoils = Read_32Bits();
            Buildings.FakeWarFactories = Read_32Bits();
            Buildings.FakeConstructionYards = Read_32Bits();
            Buildings.FakeShipyards = Read_32Bits();
            Buildings.FakeSubpens = Read_32Bits();
            Buildings.FakeRadarDomes = Read_32Bits();
            Buildings.Sandbags = Read_32Bits();
            Buildings.ChainLinkFences = Read_32Bits();
            Buildings.ConcreteWalls = Read_32Bits();
            Buildings.BarbwireFences = Read_32Bits();
            Buildings.WoodenFences = Read_32Bits();
            Buildings.WireFences = Read_32Bits();
            Buildings.AntiTankMines = Read_32Bits();
            Buildings.AntiPersonnelMines = Read_32Bits();
            Buildings.V1s = Read_32Bits();
            Buildings.V2s = Read_32Bits();
            Buildings.V3s = Read_32Bits();
            Buildings.V4s = Read_32Bits();
            Buildings.V5s = Read_32Bits();
            Buildings.V6s = Read_32Bits();
            Buildings.V7s = Read_32Bits();
            Buildings.V8s = Read_32Bits();
            Buildings.V9s = Read_32Bits();
            Buildings.V10s = Read_32Bits();
            Buildings.V11s = Read_32Bits();
            Buildings.V12s = Read_32Bits();
            Buildings.V13s = Read_32Bits();
            Buildings.V14s = Read_32Bits();
            Buildings.V15s = Read_32Bits();
            Buildings.V16s = Read_32Bits();
            Buildings.V17s = Read_32Bits();
            Buildings.V18s = Read_32Bits();
            Buildings.V19s = Read_32Bits();
            Buildings.V20s = Read_32Bits();
            Buildings.V21s = Read_32Bits();
            Buildings.V22s = Read_32Bits();
            Buildings.V23s = Read_32Bits();
            Buildings.V24s = Read_32Bits();
            Buildings.V25s = Read_32Bits();
            Buildings.V26s = Read_32Bits();
            Buildings.V27s = Read_32Bits();
            Buildings.V28s = Read_32Bits();
            Buildings.V29s = Read_32Bits();
            Buildings.V30s = Read_32Bits();
            Buildings.V31s = Read_32Bits();
            Buildings.V32s = Read_32Bits();
            Buildings.V33s = Read_32Bits();
            Buildings.V34s = Read_32Bits();
            Buildings.V35s = Read_32Bits();
            Buildings.V36s = Read_32Bits();
            Buildings.V37s = Read_32Bits();
            Buildings.Barrels = Read_32Bits();
            Buildings.BarrelsGroups = Read_32Bits();
            Buildings.AntQueens = Read_32Bits();
            Buildings.Larva1s = Read_32Bits();
            Buildings.Larva2s = Read_32Bits();

            return Buildings;
        }

        public VehiclesStruct Parse_Vehicles()
        {
            VehiclesStruct Vehicles = new VehiclesStruct();

            Vehicles.MammothTanks = Read_32Bits();
            Vehicles.HeavyTanks = Read_32Bits();
            Vehicles.MediumTanks = Read_32Bits();
            Vehicles.LightTanks = Read_32Bits();
            Vehicles.APCs = Read_32Bits();
            Vehicles.MineLayers = Read_32Bits(); // Both Anti-Tank AND Anti-Personnel MineLayers
            Vehicles.Rangers = Read_32Bits();
            Vehicles.OreTrucks = Read_32Bits();
            Vehicles.Artilleries = Read_32Bits();
            Vehicles.MobileRadarJammers = Read_32Bits();
            Vehicles.MobileGapGenerators = Read_32Bits();
            Vehicles.MCVs = Read_32Bits();
            Vehicles.V2RocketLaunchers = Read_32Bits();
            Vehicles.SupplyTrucks = Read_32Bits();
            Vehicles.ANT1s = Read_32Bits();
            Vehicles.ANT2s = Read_32Bits();
            Vehicles.ANT3s = Read_32Bits();
            Vehicles.ChronoTanks = Read_32Bits();
            Vehicles.TeslaTanks = Read_32Bits();
            Vehicles.MADTanks = Read_32Bits();
            Vehicles.DemoTrucks = Read_32Bits();
            Vehicles.PhaseTransports = Read_32Bits();

            return Vehicles;
        }

        public InfantryStruct Parse_Infantry()
        {
            InfantryStruct Infantry = new InfantryStruct();

            Infantry.RifleInfantries = Read_32Bits();
            Infantry.Grenadiers = Read_32Bits();
            Infantry.RocketSoldiers = Read_32Bits();
            Infantry.Flamethrowers = Read_32Bits();
            Infantry.Engineers = Read_32Bits();
            Infantry.Tanyas = Read_32Bits();
            Infantry.Spies = Read_32Bits();
            Infantry.Thieves = Read_32Bits();
            Infantry.Medics = Read_32Bits();
            Infantry.GNRLs = Read_32Bits();
            Infantry.Dogs = Read_32Bits();
            Infantry.C1s = Read_32Bits();
            Infantry.C2s = Read_32Bits();
            Infantry.C3s = Read_32Bits();
            Infantry.C4s = Read_32Bits();
            Infantry.C5s = Read_32Bits();
            Infantry.C6s = Read_32Bits();
            Infantry.C7s = Read_32Bits();
            Infantry.C8s = Read_32Bits();
            Infantry.C9s = Read_32Bits();
            Infantry.C10s = Read_32Bits();
            Infantry.Einsteins = Read_32Bits();
            Infantry.Delphis = Read_32Bits();
            Infantry.Chans = Read_32Bits();
            Infantry.ShockTroopers = Read_32Bits();
            Infantry.Mechanics = Read_32Bits();

            return Infantry;
        }


        public void Parse_Money_Harvested_Info(string ID, int Money)
        {
            int PlayerNumber = Get_Player_Number_From_ID(ID);

            this.PlayerMoneyHarvested[PlayerNumber - 1] = Money;
        }

        public int Get_Player_Number_From_ID(string ID)
        {
            int PlayerNumber = -1;

            string tmp = new string(ID[3], 1);
            Int32.TryParse(tmp, out PlayerNumber);


            if (PlayerNumber < 1 || PlayerNumber > 8)
            {
                throw new StatsDumpException("Player number was incorrectly parsed from ID string");
            }

            return PlayerNumber;
        }

        public void Print_Player_Array(int[] Array, string Format)
        {
            int PlayerNumber = 1;
            foreach (int Element in Array)
            {
                if (Element != -1)
                {
                    Console.WriteLine(Format, PlayerNumber, Element);
                }
                PlayerNumber++;
            }
        }
        public void Print_Player_String_Array(string[] Array, string Format)
        {
            int PlayerNumber = 1;
            foreach (string Element in Array)
            {
                if (Element != null)
                {
                    Console.WriteLine(Format, PlayerNumber, Element);
                }
                PlayerNumber++;
            }
        }

        static string Get_Alliances_String(int BitField)
        {
            if (BitField == -1) { return ""; }

            string str = "";

            for (int i = 12; i < 20; i++)
            {
                if ((BitField & (1 << i)) != 0)
                {
                    str += String.Format("{0}|", i - 11);
                }
            }

            if (str.Length > 1)
            {
               str =  str.Remove(str.Length - 1);
            }

            return str;
        }
    }

    public struct FileTime
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;

        public static long FileTime_To_Long(FileTime ft)
        {
            long hFT2 = (((long)ft.dwHighDateTime) << 32) + ft.dwLowDateTime;
            return hFT2;
        }
    }

    [Serializable()]
    public class StatsDumpLengthException : StatsDumpException
    {
        public StatsDumpLengthException() : base() { }
        public StatsDumpLengthException(string message) : base(message) { }
        public StatsDumpLengthException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an 
        // exception propagates from a remoting server to the client.  
        protected StatsDumpLengthException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) { }
    }

    [Serializable()]
    public class StatsDumpException : System.Exception
    {
        public StatsDumpException() : base() { }
        public StatsDumpException(string message) : base(message) { }
        public StatsDumpException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an 
        // exception propagates from a remoting server to the client.  
        protected StatsDumpException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) { }
    }

    public class BigEndianReader
    {
        public BigEndianReader(BinaryReader baseReader)
        {
            mBaseReader = baseReader;
        }

        public short ReadInt16()
        {
            return BitConverter.ToInt16(ReadBigEndianBytes(2), 0);
        }

        public ushort ReadUInt16()
        {
            return BitConverter.ToUInt16(ReadBigEndianBytes(2), 0);
        }

        public uint ReadUInt32()
        {
            return BitConverter.ToUInt32(ReadBigEndianBytes(4), 0);
        }

        public int ReadInt32()
        {
            return BitConverter.ToInt32(ReadBigEndianBytes(4), 0);
        }

        public byte[] ReadBigEndianBytes(int count)
        {
            byte[] bytes = new byte[count];
            for (int i = count - 1; i >= 0; i--)
                bytes[i] = mBaseReader.ReadByte();

            return bytes;
        }

        public byte[] ReadBytes(int count)
        {
            return mBaseReader.ReadBytes(count);
        }

        public void Close()
        {
            mBaseReader.Close();
        }

        public Stream BaseStream
        {
            get { return mBaseReader.BaseStream; }
        }

        private BinaryReader mBaseReader;
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
    };

    public struct PlanesStruct
    {
        public int Chinooks;
        public int BadgeBombers;
        public int SpyPlanes;
        public int MIGs;
        public int YAKs;
        public int LongBows;
        public int Hinds;
    };

    public struct VesselsStruct
    {
        public int Submarines;
        public int Destroyers;
        public int Cruisers;
        public int Gunboats;
        public int MissileSubs;
        public int HeliCarriers; // Aftermath hidden unit
    };

    public struct VehiclesStruct
    {
        public int MammothTanks;
        public int HeavyTanks;
        public int MediumTanks;
        public int LightTanks;
        public int APCs;
        public int MineLayers; // Both Anti-Tank AND Anti-Personnel MineLayers
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
    };

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
    };

    public struct BuildingsStruct
    {
        public int AlliedTechCenters; //1
        public int IronCurtains; //2
        public int WarFactories; //3
        public int Chronospheres; //4
        public int Pillboxes; //5
        public int CameoPillboxes; //6
        public int RadarDomes; //7
        public int GapGenerators; //8
        public int Turrets; //9
        public int AAGuns; //10
        public int FlameTowers; //11
        public int ConstructionYards; //12
        public int Refineries; //13
        public int OreSilos; //14
        public int Helipads; //15
        public int SamSites; //16
        public int Airfields; //17
        public int PowerPlants; //18
        public int AdvancedPowerPlants; //19
        public int SovietTechCenters; // 20
        public int Hospitals; //21
        public int SovietBarracks; //22
        public int AlliesBarracks; //23
        public int Kennels; //24
        public int ServiceDepots; //25
        public int BIOResearchFacilities; // 26
        public int TechnologyCenters; // 27
        public int Shipyards; // Allies
        public int Subpens; // Soviets
        public int MissileSilos; // 30
        public int ForwardCommandPosts; // 31
        public int TeslaCoils; // 32
        public int FakeWarFactories; // 33
        public int FakeConstructionYards; // 34
        public int FakeShipyards; // 35
        public int FakeSubpens; // Hidden Red alert unit
        public int FakeRadarDomes; // 37
        public int Sandbags; // 38
        public int ChainLinkFences; // 39
        public int ConcreteWalls; // 40
        public int BarbwireFences; // 41
        public int WoodenFences; // 42
        public int WireFences; // 43
        public int AntiTankMines; // 44
        public int AntiPersonnelMines; // 45
        public int V1s; // 46
        public int V2s; // 47
        public int V3s; // 48
        public int V4s; // 49
        public int V5s; // 50
        public int V6s; // 51
        public int V7s; // 52
        public int V8s; // 53
        public int V9s; // 54
        public int V10s; // 55
        public int V11s; // 56
        public int V12s; // 57
        public int V13s; // 58
        public int V14s; // 59
        public int V15s; // 60
        public int V16s; // 61
        public int V17s; // 62
        public int V18s; // 63
        public int V19s; // 64
        public int V20s; // 65
        public int V21s; // 66
        public int V22s; // 67
        public int V23s; // 68
        public int V24s; // 69
        public int V25s; // 70
        public int V26s; // 71
        public int V27s; // 72
        public int V28s; // 73
        public int V29s; // 74
        public int V30s; // 75
        public int V31s; // 76
        public int V32s; // 77
        public int V33s; // 78
        public int V34s; // 79
        public int V35s; // 80
        public int V36s; // 81
        public int V37s; // 82
        public int Barrels; // 83
        public int BarrelsGroups; // 84
        public int AntQueens; // 85
        public int Larva1s; // 86
        public int Larva2s; // 87
    }
}

