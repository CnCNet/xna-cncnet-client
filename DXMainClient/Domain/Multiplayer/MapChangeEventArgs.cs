using System;

namespace DTAClient.Domain.Multiplayer;
public class MapChangedEventArgs : EventArgs
{
    public Map Map { get; set; }
    public MapChangeType ChangeType { get; set; }
    public string PreviousMapSHA1 { get; set; }

    public MapChangedEventArgs(Map map, MapChangeType changeType, string previousMapSHA1 = null)
    {
        Map = map;
        ChangeType = changeType;
        PreviousMapSHA1 = previousMapSHA1;
    }
}