#nullable enable

using System;
using System.Collections.Generic;

using ClientCore.Extensions;

using DTAClient.Online;

using Rampastring.Tools;

namespace DTAClient.Domain;

/// <summary>Per-file compatibility hashes stored with a replay.</summary>
public static class ReplayFileHashes
{
    public const string SECTION = "ReplayFileHashes";

    public static void Write(IniFile spawnIni)
    {
        SortedDictionary<string, string> hashes = Collect();

        if (hashes.Count == 0)
            return;

        var section = new IniSection(SECTION);

        // Paths are values because INI keys cannot safely contain '='.
        int index = 0;
        foreach (KeyValuePair<string, string> entry in hashes)
        {
            section.SetStringValue(index.ToString(), entry.Key + "|" + entry.Value);
            index++;
        }

        spawnIni.AddSection(section);

        Logger.Log($"ReplayFileHashes: wrote {hashes.Count} file hashes to spawn.ini");
    }

    /// <summary>Returns differences between recorded and local game files.</summary>
    public static List<string> FindMismatches(IniFile replaySpawnIni)
    {
        var mismatches = new List<string>();

        List<string> keys = replaySpawnIni.GetSectionKeys(SECTION);
        if (keys == null || keys.Count == 0)
            return mismatches;

        SortedDictionary<string, string> local = Collect();
        var recordedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string key in keys)
        {
            string value = replaySpawnIni.GetStringValue(SECTION, key, string.Empty);

            // Use the last separator so paths can contain '|'.
            int separator = value.LastIndexOf('|');
            if (separator <= 0)
                continue;

            string relativePath = FileHashCalculator.NormalizePath(value.Substring(0, separator));
            string recordedHash = value.Substring(separator + 1);
            recordedPaths.Add(relativePath);

            if (!local.TryGetValue(relativePath, out string? localHash))
            {
                mismatches.Add(string.Format("{0} is missing locally".L10N("Client:Main:ReplayFileMissing"), relativePath));
                continue;
            }

            if (string.Equals(localHash, recordedHash, StringComparison.OrdinalIgnoreCase))
                continue;

            mismatches.Add(string.Format("{0}\n    replay: {1}\n    yours:  {2}".L10N("Client:Main:ReplayFileDifferent"), relativePath, recordedHash, localHash));
        }

        foreach (string relativePath in local.Keys)
        {
            if (!recordedPaths.Contains(relativePath))
            {
                mismatches.Add(string.Format("{0} was not recorded with this replay".L10N("Client:Main:ReplayFileNotRecorded"), relativePath));
            }
        }

        return mismatches;
    }

    private static SortedDictionary<string, string> Collect()
    {
        var hashes = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (FileHashCalculator.TrackedFile tracked in new FileHashCalculator().EnumerateTrackedFiles())
        {
            string hash = FileHashCalculator.GetTrackedFileHash(tracked.FullPath);
            if (!string.IsNullOrEmpty(hash))
                hashes[FileHashCalculator.NormalizePath(tracked.RelativePath)] = hash;
        }

        return hashes;
    }
}