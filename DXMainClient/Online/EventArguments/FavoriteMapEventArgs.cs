using System;
using DTAClient.Domain.Multiplayer;

namespace DTAClient.Online.EventArguments
{
    public class FavoriteMapEventArgs : EventArgs
    {
        public readonly Map Map;

        public FavoriteMapEventArgs(Map map)
        {
            Map = map;
        }
    }
}
