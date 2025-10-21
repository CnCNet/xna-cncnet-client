using System;
using System.IO;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer;
public class MapFileWatcher
{
    private readonly string mapsDirectory;
    private readonly string mapFileExtension;
    private FileSystemWatcher fileSystemWatcher;

    public event EventHandler<MapFileEventArgs> MapFileChanged;

    public MapFileWatcher(string mapsPath, string fileExtension)
    {
        mapsDirectory = mapsPath;
        mapFileExtension = fileExtension;
    }

    public void StartWatching()
    {
        if (fileSystemWatcher != null)
            return;

        DirectoryInfo directoryInfo = SafePath.GetDirectory(mapsDirectory);
        if (!directoryInfo.Exists)
            return;

        try
        {
            fileSystemWatcher = new FileSystemWatcher(mapsDirectory, $"*.{mapFileExtension}")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                IncludeSubdirectories = true
            };

            fileSystemWatcher.Created += OnFileSystemEvent;
            fileSystemWatcher.Changed += OnFileSystemEvent;
            fileSystemWatcher.Deleted += OnFileSystemEvent;
            fileSystemWatcher.Renamed += OnFileRenamed;

            fileSystemWatcher.EnableRaisingEvents = true;

            Logger.Log($"MapFileWatcher: Started watching {mapsDirectory} for *.{mapFileExtension} files");
        }
        catch (Exception ex)
        {
            Logger.Log($"MapFileWatcher: Failed to start watching directory {mapsDirectory}: {ex.Message}");
            fileSystemWatcher?.Dispose();
            fileSystemWatcher = null;
        }
    }

    private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        ProcessFileEvent(e.FullPath, e.ChangeType);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        // delete + create
        ProcessFileEvent(e.OldFullPath, WatcherChangeTypes.Deleted);
        ProcessFileEvent(e.FullPath, WatcherChangeTypes.Created);
    }

    private void ProcessFileEvent(string filePath, WatcherChangeTypes changeType)
    {
        try
        {
            var eventArgs = new MapFileEventArgs(filePath, changeType);
            MapFileChanged?.Invoke(this, eventArgs);
        }
        catch (Exception ex)
        {
            Logger.Log($"MapFileWatcher: Error processing file event for {filePath}: {ex.Message}");
        }
    }
}