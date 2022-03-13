using System;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// An instance of a Map in a given GameMode
    /// </summary>
    public class GameModeMap
    {
        public GameMode GameMode { get; }
        public Map Map { get; }
        public bool IsFavorite { get; set; }

        public GameModeMap(GameMode gameMode, Map map, bool isFavorite)
        {
            GameMode = gameMode;
            Map = map;
            IsFavorite = isFavorite;
        }

        protected bool Equals(GameModeMap other) => Equals(GameMode, other.GameMode) && Equals(Map, other.Map);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (GameMode != null ? GameMode.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Map != null ? Map.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IsFavorite.GetHashCode();
                return hashCode;
            }
        }
    }
}
