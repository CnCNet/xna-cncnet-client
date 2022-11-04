using System;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// Events args for MapLoader.GameModeMapsUpdated events.
    /// </summary>
    public class MapLoaderEventArgs : EventArgs
    {
        public MapLoaderEventArgs(Map map)
        {
            Map = map;
        }

        public Map Map { get; private set; }
    }
}
