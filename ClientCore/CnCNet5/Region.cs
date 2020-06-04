namespace ClientCore.CnCNet5
{
    /// <summary>
    /// A class for regions their irc channels and names.
    /// </summary>
    public class Region
    {
        public string UIName { get; set; }
        public string InternalName { get; set; }
        public string ChatChannel { get; set; }
        public string GameBroadcastChannel { get; set; }

        /// <summary>
        /// List of GMT Offsets (-12 to +14) that should join this Region
        /// </summary>
        public int[] TimeZones { get; set; }

        /// <summary>
        /// Automatically fill this channel with users until it hits the AutoFillAmount
        /// </summary>
        public int AutoFillAmount { get; set; }
        public int CurrentCount { get; set; } = 0;

        /// <summary>
        /// The Key to be used for this region in the cncnet live stats API
        /// </summary>
        public string CnCNetLiveStatsKey { get; set; }
    }
}
