#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ClientCore.Enums;

using Rampastring.Tools;

namespace ClientCore
{
    /// <summary>
    /// Applies storage limits to the game's own debug folder: the logs Ares, Phobos and the spawner
    /// write there, and the snapshot folders Ares creates for each crash or desync.
    /// </summary>
    public static class GameLogManager
    {
        private const string DEBUG_DIRECTORY_NAME = "debug";

        /// <summary>Whether the game writes to the debug folder, which only Ares does.</summary>
        public static bool IsSupported => ClientConfiguration.Instance.ClientGameType == ClientType.Ares;

        public static void PruneGameLogs()
        {
            if (!IsSupported)
                return;

            PruneGameLogs(SafePath.GetDirectory(ProgramConstants.GamePath, DEBUG_DIRECTORY_NAME),
                UserINISettings.Instance.MaxGameLogAgeDays.Value,
                UserINISettings.Instance.MaxGameLogFolderSizeMB.Value,
                DateTime.UtcNow);
        }

        internal static void PruneGameLogs(DirectoryInfo directory, int maxAgeDays, int maxFolderSizeMB, DateTime utcNow)
        {
            if (maxAgeDays <= 0 && maxFolderSizeMB <= 0)
                return;

            try
            {
                if (!directory.Exists)
                    return;

                // Each top-level entry is one log file or one snapshot folder, kept or deleted whole so
                // a crash report is never left half there.
                List<GameLogEntry> entries = directory.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly)
                    .Select(GameLogEntry.Create)
                    .OrderByDescending(entry => entry.LastWriteTimeUtc)
                    .ThenBy(entry => entry.Info.Name, StringComparer.Ordinal)
                    .ToList();

                if (maxAgeDays > 0)
                {
                    DateTime threshold = utcNow.AddDays(-maxAgeDays);
                    entries.RemoveAll(entry => entry.LastWriteTimeUtc <= threshold && TryDelete(entry));
                }

                if (maxFolderSizeMB > 0)
                {
                    long maxFolderSizeBytes = maxFolderSizeMB * 1024L * 1024L;
                    long totalSize = entries.Sum(entry => entry.Size);

                    // Always keep the newest entry - the last game's log - even if it alone exceeds the limit.
                    for (int i = entries.Count - 1; i > 0 && totalSize > maxFolderSizeBytes; i--)
                    {
                        // Failed deletions still count as freed and are retried next time.
                        totalSize -= entries[i].Size;
                        TryDelete(entries[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Could not prune game logs: " + ex.Message);
            }
        }

        private static bool TryDelete(GameLogEntry entry)
        {
            try
            {
                if (entry.Info is DirectoryInfo snapshotDirectory)
                    snapshotDirectory.Delete(recursive: true);
                else
                    entry.Info.Delete();

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log("Could not delete game log " + entry.Info.Name + ": " + ex.Message);
                return false;
            }
        }

        private sealed class GameLogEntry
        {
            private GameLogEntry(FileSystemInfo info, long size, DateTime lastWriteTimeUtc)
            {
                Info = info;
                Size = size;
                LastWriteTimeUtc = lastWriteTimeUtc;
            }

            public FileSystemInfo Info { get; }

            public long Size { get; }

            public DateTime LastWriteTimeUtc { get; }

            public static GameLogEntry Create(FileSystemInfo info)
            {
                switch (info)
                {
                    case FileInfo fileInfo:
                        return new GameLogEntry(info, fileInfo.Length, info.LastWriteTimeUtc);

                    case DirectoryInfo snapshotDirectory:
                        // A snapshot is as recent as the newest file in it; the client copies logs into it
                        // after the game exits.
                        long size = 0;
                        DateTime lastWriteTimeUtc = snapshotDirectory.LastWriteTimeUtc;
                        foreach (FileInfo file in snapshotDirectory.EnumerateFiles("*", SearchOption.AllDirectories))
                        {
                            size += file.Length;
                            if (file.LastWriteTimeUtc > lastWriteTimeUtc)
                                lastWriteTimeUtc = file.LastWriteTimeUtc;
                        }

                        return new GameLogEntry(info, size, lastWriteTimeUtc);

                    default:
                        throw new ArgumentException("Unexpected file system info type: " + info.GetType().FullName, nameof(info));
                }
            }
        }
    }
}
