using System;
using System.IO;

namespace DTAClient.Domain.Multiplayer;
public class MapFileEventArgs : EventArgs
{
    public string FilePath { get; set; }
    public string FileName { get; set; }
    public WatcherChangeTypes ChangeType { get; set; }
    public string OldFilePath { get; set; }

    public MapFileEventArgs(string filePath, WatcherChangeTypes changeType, string oldFilePath = null)
    {
        FilePath = filePath;
        FileName = Path.GetFileNameWithoutExtension(filePath);
        ChangeType = changeType;
        OldFilePath = oldFilePath;
    }
}