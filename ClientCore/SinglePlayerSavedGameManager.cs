using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Rampastring.Tools;

namespace ClientCore
{
    /// <summary>Applies optional storage limits to single-player saved games.</summary>
    public static class SinglePlayerSavedGameManager
    {
        public static void PruneSavedGames()
        {
            PruneSavedGames(SafePath.GetDirectory(ProgramConstants.GamePath, "Saved Games"),
                UserINISettings.Instance.MaxKeptSavedGames.Value,
                UserINISettings.Instance.MaxSavedGameFolderSizeMB.Value);
        }

        internal static void PruneSavedGames(DirectoryInfo directory, int maxKeptSavedGames, int maxFolderSizeMB)
        {
            if (maxKeptSavedGames <= 0 && maxFolderSizeMB <= 0)
                return;

            try
            {
                if (!directory.Exists)
                    return;

                // Match only single-player saves, leaving multiplayer saves and metadata alone.
                List<FileInfo> saves = directory.EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                    .Where(file => string.Equals(file.Extension, ".SAV", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(file => file.LastWriteTimeUtc)
                    .ThenBy(file => file.Name, StringComparer.Ordinal)
                    .ToList();

                int remainingCount = saves.Count;
                long totalSize = saves.Sum(file => file.Length);
                long maxFolderSizeBytes = maxFolderSizeMB * 1024L * 1024L;

                // Always preserve the newest save, even when it alone exceeds the size limit.
                for (int i = saves.Count - 1; i > 0; i--)
                {
                    if ((maxKeptSavedGames <= 0 || remainingCount <= maxKeptSavedGames) &&
                        (maxFolderSizeMB <= 0 || totalSize <= maxFolderSizeBytes))
                        break;

                    FileInfo save = saves[i];
                    try
                    {
                        long size = save.Length;
                        save.Delete();
                        remainingCount--;
                        totalSize -= size;
                        Logger.Log("Deleted old single-player saved game: " + save.Name);
                    }
                    catch (Exception ex)
                    {
                        // Failed deletions still count towards both limits and can be retried next time.
                        Logger.Log("Could not delete single-player saved game " + save.Name + ": " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Could not prune single-player saved games: " + ex.Message);
            }
        }
    }
}
