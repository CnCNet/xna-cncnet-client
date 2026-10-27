#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Text.Json;

using ClientCore.Launch.Spawner;

using Rampastring.Tools;

namespace ClientCore.Launch;

/// <summary>
/// Keeps the request a multiplayer match was started with next to its saved games, so the
/// match can be set up again when a save is loaded.
/// </summary>
/// <remarks>
/// The request is recorded as pending when a match launches and becomes the saved match once the
/// match is saved, so launching a match that is never saved doesn't replace the record of older saves.
/// </remarks>
public static class SavedMatchStore
{
    private const int SCHEMA_VERSION = 1;
    private const string SAVED_GAMES_DIRECTORY = "Saved Games";
    private const string MATCH_FILE_NAME = "match.json";
    private const string PENDING_MATCH_FILE_NAME = "match.pending.json";
    private const string MULTIPLAYER_SAVE_PATTERN = "SVGM_*.NET";

    private static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };

    private static DirectoryInfo SavedGamesDirectory => SafePath.GetDirectory(ProgramConstants.GamePath, SAVED_GAMES_DIRECTORY);

    private static FileInfo MatchFile(DirectoryInfo directory) => SafePath.GetFile(directory.FullName, MATCH_FILE_NAME);

    private static FileInfo PendingMatchFile(DirectoryInfo directory) => SafePath.GetFile(directory.FullName, PENDING_MATCH_FILE_NAME);

    /// <summary>Whether a saved match is stored.</summary>
    public static bool Exists => MatchFile(SavedGamesDirectory).Exists;

    /// <summary>Records the request of a multiplayer match that is about to launch, until it is saved.</summary>
    public static void RecordLaunch(MatchLaunchRequest request) => RecordLaunch(request, SavedGamesDirectory);

    internal static void RecordLaunch(MatchLaunchRequest request, DirectoryInfo directory)
    {
        try
        {
            directory.Create();

            File.WriteAllText(PendingMatchFile(directory).FullName, Serialize(request));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.Log("SavedMatchStore: could not record the match's launch request: " + ex.Message);
        }
    }

    /// <summary>
    /// Makes the pending match the saved match. Call when the match is saved for the first time.
    /// Does nothing when no match is pending.
    /// </summary>
    public static void PromotePending() => PromotePending(SavedGamesDirectory);

    internal static void PromotePending(DirectoryInfo directory)
    {
        FileInfo pendingFile = PendingMatchFile(directory);

        if (!pendingFile.Exists)
            return;

        try
        {
            FileInfo matchFile = MatchFile(directory);

            if (matchFile.Exists)
                matchFile.Delete();

            pendingFile.MoveTo(matchFile.FullName);
            Logger.Log("SavedMatchStore: stored the launch request of the saved match.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.Log("SavedMatchStore: could not store the launch request of the saved match: " + ex.Message);
        }
    }

    /// <summary>
    /// Finishes a multiplayer match: keeps its request if the match was saved since it started,
    /// otherwise discards it.
    /// </summary>
    public static void CompletePending(DateTime matchStartedAt) => CompletePending(matchStartedAt, SavedGamesDirectory);

    internal static void CompletePending(DateTime matchStartedAt, DirectoryInfo directory)
    {
        bool saved = directory.Exists && directory.EnumerateFiles(MULTIPLAYER_SAVE_PATTERN)
            .Any(file => file.LastWriteTime >= matchStartedAt);

        if (saved)
        {
            PromotePending(directory);
            return;
        }

        try
        {
            PendingMatchFile(directory).Delete();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logger.Log("SavedMatchStore: could not discard the unsaved match's launch request: " + ex.Message);
        }
    }

    internal static string Serialize(MatchLaunchRequest request)
        => JsonSerializer.Serialize(new StoredMatch
        {
            SchemaVersion = SCHEMA_VERSION,
            Request = request,
        }, jsonOptions);

    /// <summary>Reads a stored request, or returns null if it has an unknown version.</summary>
    internal static MatchLaunchRequest? Deserialize(string json)
    {
        StoredMatch? stored = JsonSerializer.Deserialize<StoredMatch>(json, jsonOptions);

        if (stored?.Request == null || stored.SchemaVersion != SCHEMA_VERSION)
            return null;

        return stored.Request;
    }

    /// <summary>Returns the saved match's request, or null when there is none or it can't be read.</summary>
    public static MatchLaunchRequest? LoadRequest() => LoadRequest(SavedGamesDirectory);

    internal static MatchLaunchRequest? LoadRequest(DirectoryInfo directory)
    {
        FileInfo matchFile = MatchFile(directory);

        if (!matchFile.Exists)
            return null;

        MatchLaunchRequest? request;

        try
        {
            request = Deserialize(File.ReadAllText(matchFile.FullName));
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or IOException)
        {
            Logger.Log("SavedMatchStore: could not read the saved match's launch request: " + ex.Message);
            return null;
        }

        if (request == null)
            Logger.Log("SavedMatchStore: the saved match's launch request is empty or has an unknown version.");

        return request;
    }

    /// <summary>
    /// Returns what the client shows and checks about the saved match. Everything is empty when
    /// no match is stored.
    /// </summary>
    public static SavedMatchInfo LoadInfo()
    {
        MatchLaunchRequest? request = LoadRequest();

        if (request == null)
            return new SavedMatchInfo(new IniFile());

        SpawnIniDialect dialect = SpawnIniDialect.ForClientType(ClientConfiguration.Instance.ClientGameType);

        return new SavedMatchInfo(SpawnIniWriter.WriteMatch(request, dialect));
    }

    private sealed class StoredMatch
    {
        public int SchemaVersion { get; set; }

        public MatchLaunchRequest? Request { get; set; }
    }
}

/// <summary>What the client shows and checks about a stored multiplayer match.</summary>
public sealed class SavedMatchInfo
{
    private const string SETTINGS = SpawnIniWriter.SETTINGS;

    private readonly IniFile spawnIni;

    internal SavedMatchInfo(IniFile spawnIni)
    {
        this.spawnIni = spawnIni;
    }

    /// <summary>The game ID as text, or the given default when there is none.</summary>
    public string GetGameID(string defaultValue) => spawnIni.GetStringValue(SETTINGS, "GameID", defaultValue);

    /// <summary>The game ID as a number, or the given default when there is none.</summary>
    public int GetGameIDNumber(int defaultValue) => spawnIni.GetIntValue(SETTINGS, "GameID", defaultValue);

    /// <summary>The number of human players, or the given default when unknown.</summary>
    public int GetPlayerCount(int defaultValue) => spawnIni.GetIntValue(SETTINGS, "PlayerCount", defaultValue);

    /// <summary>The name of the player who saved the match.</summary>
    public string LocalPlayerName => spawnIni.GetStringValue(SETTINGS, "Name", string.Empty);

    /// <summary>The game color index of the player who saved the match.</summary>
    public int LocalPlayerColor => spawnIni.GetIntValue(SETTINGS, "Color", 0);

    /// <summary>Whether the player who saved the match hosted it.</summary>
    public bool IsHost => spawnIni.GetBooleanValue(SETTINGS, "Host", false);

    /// <summary>Whether the match was played through a CnCNet tunnel.</summary>
    public bool UsedTunnel => spawnIni.SectionExists("Tunnel");

    public string MapSHA1 => spawnIni.GetStringValue(SETTINGS, "MapSHA1", string.Empty);

    public string BroadcastedGameOptionValues => spawnIni.GetStringValue(SETTINGS, "BroadcastedGameOptionValues", string.Empty);

    public string UIMapName => spawnIni.GetStringValue(SETTINGS, "UIMapName", string.Empty);

    public string MapID => spawnIni.GetStringValue(SETTINGS, "MapID", string.Empty);

    public string UIGameMode => spawnIni.GetStringValue(SETTINGS, "UIGameMode", string.Empty);

    /// <summary>The name of another player, numbered from 1, or the given default when unknown.</summary>
    public string GetOtherPlayerName(int otherIndex, string defaultValue)
        => spawnIni.GetStringValue("Other" + otherIndex, "Name", defaultValue);

    /// <summary>The game color index of another player, numbered from 1.</summary>
    public int GetOtherPlayerColor(int otherIndex) => spawnIni.GetIntValue("Other" + otherIndex, "Color", 0);
}
