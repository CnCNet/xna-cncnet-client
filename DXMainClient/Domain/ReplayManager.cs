#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using ClientCore;
using ClientCore.Extensions;

using ClientUpdater;

using Rampastring.Tools;

namespace DTAClient.Domain;

/// <summary>
/// Replay paths, naming, listing and pruning.
/// </summary>
public static class ReplayManager
{
    /// <summary>Maximum timestamp and map name length.</summary>
    private const int MaxRecordingBaseFileNameLength = 180;

    public static bool IsSupported => ClientConfiguration.Instance.ReplaySupport;

    public static string DirectoryName => ClientConfiguration.Instance.ReplaysDirectory;

    public static string FileExtension => ClientConfiguration.Instance.ReplayFileExtension;

    public static string SearchPattern => "*." + FileExtension;

    /// <summary>Installed game package and version written to replay metadata.</summary>
    public static string GameClientVersion
    {
        get
        {
            string gameVersion = string.IsNullOrWhiteSpace(Updater.GameVersion) ? "Unknown" : Updater.GameVersion;

            return $"{ClientConfiguration.Instance.LocalGame} {gameVersion}".Trim();
        }
    }

    public static DirectoryInfo GetDirectory()
        => SafePath.GetDirectory(ProgramConstants.GamePath, DirectoryName);

    public static FileInfo GetFile(string fileName)
        => SafePath.GetFile(ProgramConstants.GamePath, DirectoryName, fileName);

    public static string GetRelativePath(string fileName)
        => SafePath.CombineFilePath(DirectoryName, fileName);

    /// <summary>Adds replay metadata when recording is enabled in spawn.ini.</summary>
    public static void PrepareRecording(IniFile spawnIni, string mapName)
    {
        if (!IsSupported)
            return;

        if (!spawnIni.GetBooleanValue("Settings", "EnableReplayRecording", false))
            return;

        spawnIni.SetStringValue("Settings", "GameClientVersion", GameClientVersion);
        spawnIni.SetStringValue("Settings", "ReplayFileOut", BuildRecordingPath(mapName));

        ReplayFileHashes.Write(spawnIni);
    }

    public static string BuildRecordingPath(string mapName)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture);
        string safeMapName = SanitizeForFileName(mapName);

        string baseName = string.IsNullOrWhiteSpace(safeMapName)
            ? timestamp
            : timestamp + " " + safeMapName;

        if (baseName.Length > MaxRecordingBaseFileNameLength)
            baseName = baseName.SubstringSurrogateAware(0, MaxRecordingBaseFileNameLength).TrimEnd();

        string fileName = baseName + "." + FileExtension;

        int counter = 1;
        while (GetFile(fileName).Exists)
        {
            fileName = $"{baseName} ({counter})." + FileExtension;
            counter++;
        }

        return GetRelativePath(fileName);
    }

    /// <summary>Cached result for a replay file at a specific size and timestamp.</summary>
    private readonly struct CachedReplay
    {
        public CachedReplay(FileInfo file, ReplayGame? replay)
        {
            length = file.Length;
            lastWriteTicks = file.LastWriteTimeUtc.Ticks;
            Replay = replay;
        }

        private readonly long length;
        private readonly long lastWriteTicks;

        /// <summary>Null when the file could not be parsed.</summary>
        public ReplayGame? Replay { get; }

        public bool Matches(FileInfo file)
            => length == file.Length && lastWriteTicks == file.LastWriteTimeUtc.Ticks;
    }

    private static readonly Dictionary<string, CachedReplay> parsedReplays
        = new Dictionary<string, CachedReplay>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Lists parseable replays newest first.</summary>
    public static List<ReplayGame> List()
    {
        var replays = new List<ReplayGame>();

        DirectoryInfo directory = GetDirectory();
        if (!directory.Exists)
        {
            parsedReplays.Clear();
            return replays;
        }

        var presentFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (FileInfo file in directory.EnumerateFiles(SearchPattern, SearchOption.TopDirectoryOnly))
        {
            presentFiles.Add(file.Name);

            if (!parsedReplays.TryGetValue(file.Name, out CachedReplay cached) || !cached.Matches(file))
            {
                var parsed = new ReplayGame(file.Name);
                cached = new CachedReplay(file, parsed.ParseInfo() ? parsed : null);
                parsedReplays[file.Name] = cached;
            }

            if (cached.Replay != null)
                replays.Add(cached.Replay);
        }

        DropDeletedFromCache(presentFiles);

        return replays.OrderByDescending(replay => replay.RecordedAt).ToList();
    }

    private static void DropDeletedFromCache(HashSet<string> presentFiles)
    {
        List<string> deleted = parsedReplays.Keys.Where(name => !presentFiles.Contains(name)).ToList();

        foreach (string name in deleted)
            parsedReplays.Remove(name);
    }

    public static bool Delete(ReplayGame replay)
    {
        Logger.Log("Deleting replay " + replay.FileName);

        try
        {
            SafePath.DeleteFileIfExists(ProgramConstants.GamePath, DirectoryName, replay.FileName);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log($"ReplayManager: could not delete {replay.FileName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>Applies replay count and size limits. Zero means unlimited.</summary>
    public static void Prune()
    {
        if (!IsSupported)
            return;

        int maxCount = UserINISettings.Instance.MaxKeptReplays;
        int maxSizeMB = UserINISettings.Instance.MaxReplayFolderSizeMB;

        if (maxCount <= 0 && maxSizeMB <= 0)
            return;

        try
        {
            DirectoryInfo directory = GetDirectory();
            if (!directory.Exists)
                return;

            List<FileInfo> files = directory
                .EnumerateFiles(SearchPattern, SearchOption.TopDirectoryOnly)
                .OrderBy(file => file.LastWriteTime)
                .ToList();

            long maxSizeBytes = maxSizeMB > 0 ? maxSizeMB * 1024L * 1024L : long.MaxValue;
            long totalBytes = files.Sum(file => file.Length);
            int fileCount = files.Count;

            foreach (FileInfo file in files)
            {
                bool overCount = maxCount > 0 && fileCount > maxCount;
                bool overSize = totalBytes > maxSizeBytes;

                if (!overCount && !overSize)
                    break;

                long fileBytes = file.Length;

                try
                {
                    file.Delete();
                    Logger.Log($"ReplayManager: pruned {file.Name} ({GetPruneReason(overCount, overSize)} limit)");
                }
                catch (Exception ex)
                {
                    Logger.Log($"ReplayManager: could not delete {file.Name}: {ex.Message}");
                    continue;
                }

                fileCount--;
                totalBytes -= fileBytes;
            }
        }
        catch (Exception ex)
        {
            Logger.Log("ReplayManager: pruning failed: " + ex.Message);
        }
    }

    private static string GetPruneReason(bool overCount, bool overSize)
    {
        if (overCount && overSize)
            return "count and size";

        return overCount ? "count" : "size";
    }

    public static void OpenDirectory()
    {
        try
        {
            DirectoryInfo directory = GetDirectory();
            if (!directory.Exists)
                directory.Create();

            ProcessLauncher.StartShellProcess(directory.FullName);
        }
        catch (Exception ex)
        {
            Logger.Log("ReplayManager: could not open the replay directory: " + ex.Message);
        }
    }

    private static readonly HashSet<char> invalidFileNameChars = BuildInvalidFileNameChars();

    private static HashSet<char> BuildInvalidFileNameChars()
    {
        var invalid = new HashSet<char>(Path.GetInvalidFileNameChars());

        // Use Windows restrictions on every platform because replay files are shared.
        foreach (char character in "<>:\"/\\|?*")
            invalid.Add(character);
        for (char character = '\0'; character < ' '; character++)
            invalid.Add(character);

        return invalid;
    }

    private static string SanitizeForFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var builder = new StringBuilder(name.Length);

        foreach (char character in name)
        {
            if (!invalidFileNameChars.Contains(character))
                builder.Append(character);
        }

        return builder.ToString().Trim().TrimEnd('.');
    }
}