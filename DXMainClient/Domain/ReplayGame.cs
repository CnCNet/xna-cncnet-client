#nullable enable

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

using ClientCore.Extensions;
using ClientCore.PlatformShim;

using Rampastring.Tools;

namespace DTAClient.Domain;

/// <summary>
/// Whether a listed replay can be played by this build.
/// </summary>
public enum ReplayStatus
{
    Playable,

    /// <summary>A valid replay with an unsupported format version.</summary>
    UnsupportedVersion
}

/// <summary>
/// A replay file. Listing only reads the header and the embedded spawn.ini; the spawn files
/// themselves are re-read on demand.
/// </summary>
public class ReplayGame
{
    public const int MAX_GAME_SPEED_INDEX = 6;

    // 'YRRP' in file order.
    private const uint REPLAY_MAGIC = 0x50525259;

    // Keep both bounds so future formats can retain support for older replays.
    private const uint MIN_SUPPORTED_REPLAY_FORMAT_VERSION = 1;
    private const uint MAX_SUPPORTED_REPLAY_FORMAT_VERSION = 1;

    // Prevent corrupt headers from causing excessive allocations.
    private const uint MAX_EMBEDDED_FILE_SIZE = 32 * 1024 * 1024;

    /// <summary>Size of the version-independent header prefix.</summary>
    private const int STABLE_PREFIX_SIZE = 12;

    /// <summary>Size of the replay header.</summary>
    private const int KNOWN_HEADER_SIZE = 1124;

    private const uint MAX_HEADER_SIZE = 64 * 1024;

    private const int OFFSET_MAGIC = 0;
    private const int OFFSET_FORMAT_VERSION = 4;
    private const int OFFSET_HEADER_SIZE = 8;
    private const int OFFSET_SPAWN_INI_SIZE = 1032;
    private const int OFFSET_SPAWN_MAP_SIZE = 1036;
    private const int OFFSET_RECORDED_GAME_SPEED = 1040;
    private const int OFFSET_RECORDED_UNIX_TIME = 1044;
    private const int OFFSET_TOTAL_FRAMES = 1052;
    private const int OFFSET_FLAGS = 1056;

    private const uint HEADER_FLAG_CLEAN_SHUTDOWN = 1;

    public ReplayGame(string fileName)
    {
        FileName = fileName;
    }

    public string FileName { get; }

    public string GUIName { get; private set; } = string.Empty;

    /// <summary>Whether this build understands the replay format.</summary>
    public ReplayStatus Status { get; private set; } = ReplayStatus.Playable;

    public bool IsPlayable => Status == ReplayStatus.Playable;

    public uint FormatVersion { get; private set; }

    /// <summary>Game package version recorded in the replay.</summary>
    public string GameClientVersion { get; private set; } = string.Empty;

    /// <summary>The lobby's game mode name, e.g. "Battle".</summary>
    public string UIGameMode { get; private set; } = string.Empty;

    /// <summary>Human players, in spawn.ini order, the recording player first.</summary>
    public IReadOnlyList<ReplayPlayer> Players => players;

    private readonly List<ReplayPlayer> players = new List<ReplayPlayer>();

    public DateTime RecordedAt { get; private set; }

    /// <summary>Whether the spawner closed the recording cleanly.</summary>
    public bool IsComplete { get; private set; }

    /// <summary>
    /// How long the recorded game ran. <see cref="TimeSpan.Zero"/> for an incomplete recording.
    /// </summary>
    public TimeSpan Duration { get; private set; }

    /// <summary>Recorded frame count, or zero for an incomplete replay.</summary>
    public uint TotalFrames { get; private set; }

    public int FramesPerSecond { get; private set; }

    private uint spawnIniSize;
    private uint spawnMapSize;

    /// <summary>Header size declared by the replay file.</summary>
    private uint headerSize;

    public bool ParseInfo()
    {
        try
        {
            FileInfo replayFileInfo = ReplayManager.GetFile(FileName);

            if (!replayFileInfo.Exists)
            {
                Logger.Log("Replay file does not exist: " + FileName);
                return false;
            }

            using FileStream stream = replayFileInfo.Open(FileMode.Open, FileAccess.Read);

            // Only the first 12 bytes have a stable layout across format versions.
            byte[] prefix = new byte[STABLE_PREFIX_SIZE];
            if (stream.Length < STABLE_PREFIX_SIZE || !TryReadExactly(stream, prefix, STABLE_PREFIX_SIZE))
            {
                Logger.Log("Replay file is too small to contain a header: " + FileName);
                return false;
            }

            uint magic = ReadUInt32(prefix, OFFSET_MAGIC);
            if (magic != REPLAY_MAGIC)
            {
                Logger.Log($"Invalid replay file magic number: 0x{magic:X8} (expected 0x{REPLAY_MAGIC:X8})");
                return false;
            }

            FormatVersion = ReadUInt32(prefix, OFFSET_FORMAT_VERSION);
            headerSize = ReadUInt32(prefix, OFFSET_HEADER_SIZE);

            if (FormatVersion < MIN_SUPPORTED_REPLAY_FORMAT_VERSION
                || FormatVersion > MAX_SUPPORTED_REPLAY_FORMAT_VERSION)
            {
                Logger.Log($"Replay {FileName} is format version {FormatVersion}; this build reads " +
                    $"{MIN_SUPPORTED_REPLAY_FORMAT_VERSION} to {MAX_SUPPORTED_REPLAY_FORMAT_VERSION}.");

                // Keep unsupported replays visible without parsing versioned fields.
                Status = ReplayStatus.UnsupportedVersion;
                GUIName = Path.GetFileNameWithoutExtension(FileName);
                RecordedAt = replayFileInfo.LastWriteTime;
                return true;
            }

            if (headerSize < KNOWN_HEADER_SIZE || headerSize > MAX_HEADER_SIZE)
            {
                Logger.Log($"Replay {FileName} declares an unusable header size of {headerSize}.");
                return false;
            }

            byte[] header = new byte[KNOWN_HEADER_SIZE];
            Array.Copy(prefix, header, STABLE_PREFIX_SIZE);

            if (stream.Length < KNOWN_HEADER_SIZE
                || !TryReadExactly(stream, header, KNOWN_HEADER_SIZE - STABLE_PREFIX_SIZE, STABLE_PREFIX_SIZE))
            {
                Logger.Log("Replay file is too small to contain a header: " + FileName);
                return false;
            }

            spawnIniSize = ReadUInt32(header, OFFSET_SPAWN_INI_SIZE);
            spawnMapSize = ReadUInt32(header, OFFSET_SPAWN_MAP_SIZE);

            if (!AreEmbeddedSizesValid(stream.Length))
            {
                Logger.Log($"Replay {FileName} declares embedded file sizes that do not fit the file");
                return false;
            }

            RecordedAt = FromUnixTime(ReadUInt64(header, OFFSET_RECORDED_UNIX_TIME), replayFileInfo.LastWriteTime);

            uint totalFrames = ReadUInt32(header, OFFSET_TOTAL_FRAMES);
            int gameSpeed = (int)Math.Min(ReadUInt32(header, OFFSET_RECORDED_GAME_SPEED), (uint)MAX_GAME_SPEED_INDEX);

            IsComplete = (ReadUInt32(header, OFFSET_FLAGS) & HEADER_FLAG_CLEAN_SHUTDOWN) != 0;

            TotalFrames = IsComplete ? totalFrames : 0;
            FramesPerSecond = GetFramesPerSecond(gameSpeed);
            Duration = IsComplete
                ? TimeSpan.FromSeconds(TotalFrames / (double)FramesPerSecond)
                : TimeSpan.Zero;

            // Use the declared size so additive header fields are skipped.
            stream.Seek(headerSize, SeekOrigin.Begin);

            string? spawnIniContent = ReadText(stream, spawnIniSize);

            if (spawnIniContent == null)
            {
                Logger.Log("Replay file ended early while reading its embedded spawn.ini: " + FileName);
                return false;
            }

            ReadEmbeddedSpawnIni(spawnIniContent);

            return true;
        }
        catch (Exception ex)
        {
            Logger.Log("An error occurred while parsing replay " + FileName + ": " + ex.ToString());
            return false;
        }
    }

    /// <summary>
    /// Re-reads the spawn.ini and spawnmap.ini embedded in the replay.
    /// </summary>
    /// <returns>False when either file is absent or the replay can no longer be read.</returns>
    public bool TryReadSpawnFiles(out string spawnIni, out byte[] spawnMap)
    {
        spawnIni = string.Empty;
        spawnMap = Array.Empty<byte>();

        // Campaign replays can load their map from mix files.
        if (!IsPlayable || spawnIniSize == 0)
            return false;

        try
        {
            using FileStream stream = ReplayManager.GetFile(FileName).Open(FileMode.Open, FileAccess.Read);
            stream.Seek(headerSize, SeekOrigin.Begin);

            string? readSpawnIni = ReadText(stream, spawnIniSize);
            byte[]? readSpawnMap = ReadBytes(stream, spawnMapSize);

            if (readSpawnIni == null || readSpawnMap == null)
            {
                Logger.Log("Replay file ended early while reading its spawn files: " + FileName);
                return false;
            }

            spawnIni = readSpawnIni;
            spawnMap = readSpawnMap;

            return true;
        }
        catch (Exception ex)
        {
            Logger.Log("An error occurred while reading the spawn files of " + FileName + ": " + ex.ToString());
            spawnIni = string.Empty;
            spawnMap = Array.Empty<byte>();
            return false;
        }
    }

    /// <summary>Must match SpeedLadder in the spawner's ReplayControls.h.</summary>
    public static readonly int[] PlaybackSpeedLadder =
        { 10, 12, 15, 20, 30, 45, 60, 90, 120, 180, 240, 300, 500, 1000, 2000 };

    /// <summary>Matches GetReplayFPSFromGameSpeed in the spawner.</summary>
    public static int GetFramesPerSecond(int gameSpeed)
    {
        gameSpeed = Math.Max(0, Math.Min(MAX_GAME_SPEED_INDEX, gameSpeed));

        if (gameSpeed <= 0)
            return 60;

        if (gameSpeed == 1)
            return 45;

        return Math.Max(1, 60 / gameSpeed);
    }

    private void ReadEmbeddedSpawnIni(string spawnIniContent)
    {
        using MemoryStream spawnIniStream = new MemoryStream(EncodingExt.UTF8NoBOM.GetBytes(spawnIniContent));
        IniFile spawnIni = new IniFile(spawnIniStream, EncodingExt.UTF8NoBOM, applyBaseIni: false);

        GameClientVersion = spawnIni.GetStringValue("Settings", "GameClientVersion", string.Empty);

        UIGameMode = spawnIni.GetStringValue("Settings", "UIGameMode", string.Empty);

        string mapName = spawnIni.GetStringValue("Settings", "UIMapName", string.Empty);
        GUIName = string.IsNullOrWhiteSpace(mapName)
            ? Path.GetFileNameWithoutExtension(FileName)
            : mapName;

        bool isCampaign = spawnIni.GetBooleanValue("Settings", "IsSinglePlayer", false);

        // Preserve spawn.ini order because the spawner setting ReplayViewPlayer uses these slot indices.
        if (!AddPlayer(spawnIni, "Settings", 0) && isCampaign)
        {
            // A campaign spawn.ini carries no name for the player, so give the slot a generic one.
            players.Add(new ReplayPlayer(0,
                "Player".L10N("Client:Main:ReplayCampaignPlayer"),
                spawnIni.GetIntValue("Settings", "Side", -1),
                false));
        }

        for (int otherId = 1; ; otherId++)
        {
            if (!AddPlayer(spawnIni, "Other" + otherId, otherId))
                break;
        }
    }

    private bool AddPlayer(IniFile spawnIni, string sectionName, int spawnIniIndex)
    {
        string name = spawnIni.GetStringValue(sectionName, "Name", string.Empty);
        if (string.IsNullOrWhiteSpace(name))
            return false;

        players.Add(new ReplayPlayer(
            spawnIniIndex,
            name,
            spawnIni.GetIntValue(sectionName, "Side", -1),
            spawnIni.GetBooleanValue(sectionName, "IsSpectator", false)));

        return true;
    }

    private bool AreEmbeddedSizesValid(long fileLength)
    {
        if (spawnIniSize > MAX_EMBEDDED_FILE_SIZE || spawnMapSize > MAX_EMBEDDED_FILE_SIZE)
            return false;

        return headerSize + (long)spawnIniSize + spawnMapSize <= fileLength;
    }

    /// <summary>Returns null when the file ends before <paramref name="size"/> bytes are read.</summary>
    private static byte[]? ReadBytes(Stream stream, uint size)
    {
        byte[] buffer = new byte[size];

        return TryReadExactly(stream, buffer, (int)size) ? buffer : null;
    }

    /// <summary>Returns null when the file ends before <paramref name="size"/> bytes are read.</summary>
    private static string? ReadText(Stream stream, uint size)
    {
        byte[]? buffer = ReadBytes(stream, size);

        return buffer == null ? null : EncodingExt.UTF8NoBOM.GetString(buffer);
    }

    private static bool TryReadExactly(Stream stream, byte[] buffer, int count)
        => TryReadExactly(stream, buffer, count, 0);

    private static bool TryReadExactly(Stream stream, byte[] buffer, int count, int bufferOffset)
    {
        int offset = 0;
        while (offset < count)
        {
            int read = stream.Read(buffer, bufferOffset + offset, count - offset);
            if (read <= 0)
                return false;

            offset += read;
        }

        return true;
    }

    private static uint ReadUInt32(byte[] header, int offset)
        => BinaryPrimitives.ReadUInt32LittleEndian(new ReadOnlySpan<byte>(header, offset, sizeof(uint)));

    private static ulong ReadUInt64(byte[] header, int offset)
        => BinaryPrimitives.ReadUInt64LittleEndian(new ReadOnlySpan<byte>(header, offset, sizeof(ulong)));

    private static DateTime FromUnixTime(ulong unixTime, DateTime fallback)
    {
        if (unixTime == 0)
            return fallback;

        try
        {
            return DateTimeOffset.FromUnixTimeSeconds((long)unixTime).LocalDateTime;
        }
        catch (ArgumentOutOfRangeException)
        {
            return fallback;
        }
    }
}
