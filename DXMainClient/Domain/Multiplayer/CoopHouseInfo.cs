namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// Holds information about enemy houses in a co-op map.
    /// </summary>
    public struct CoopHouseInfo
    {
        public CoopHouseInfo(int side, int color, int startingLocation)
        {
            Side = side;
            Color = color;
            StartingLocation = startingLocation;
        }

        /// <summary>
        /// The index of the enemy house's side.
        /// </summary>
        public int Side;

        /// <summary>
        /// The index of the enemy house's color.
        /// </summary>
        public int Color;

        /// <summary>
        /// The starting location waypoint of the enemy house.
        /// </summary>
        public int StartingLocation;
    }
}
