namespace ClientCore.Statistics
{
    public class PlayerStatistics
    {
        public PlayerStatistics() { }

        public PlayerStatistics(string name, bool isLocal, bool isAi, bool isSpectator, int side, int team, int aiLevel)
        {
            Name = name;
            IsLocalPlayer = isLocal;
            IsAI = isAi;
            WasSpectator = isSpectator;
            Side = side;
            Team = team;
            AILevel = aiLevel;
        }

        public string Name { get; set; }
        public int Kills { get; set; }
        public int Losses {get; set;}
        public int Economy { get; set; }
        public int Score { get; set; }
        public int Side { get; set; }
        public int Team { get; set; }
        public int AILevel { get; set; }
        public bool SawEnd { get; set; }
        public bool WasSpectator { get; set; }
        public bool Won { get; set; }
        public bool IsLocalPlayer { get; set; }
        public bool IsAI { get; set; }
    }
}
