#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

using ClientCore.Launch.Spawner;

using Rampastring.Tools;

namespace ClientCore.Launch;

/// <summary>
/// Stores the request a multiplayer match was started with next to its saved games, so the
/// match can be set up again when a save is loaded.
/// </summary>
public static class SavedMatchStore
{
    private const int SCHEMA_VERSION = 1;
    private const string SAVED_GAMES_DIRECTORY = "Saved Games";
    private const string REQUEST_FILE_NAME = "spawnSG.json";
    private const string SPAWN_INI_FILE_NAME = "spawnSG.ini";

    private static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };

    private static FileInfo RequestFile => SafePath.GetFile(ProgramConstants.GamePath, SAVED_GAMES_DIRECTORY, REQUEST_FILE_NAME);

    private static FileInfo SpawnIniFile => SafePath.GetFile(ProgramConstants.GamePath, SAVED_GAMES_DIRECTORY, SPAWN_INI_FILE_NAME);

    /// <summary>Whether a saved match is stored, either as a request or as a saved spawn.ini.</summary>
    public static bool Exists => RequestFile.Exists || SpawnIniFile.Exists;

    /// <summary>
    /// Stores the request a match was started with. Call after spawnSG.ini has been written for the
    /// same match; a null request only removes the request of a previous match.
    /// </summary>
    public static void Save(MatchLaunchRequest? request)
    {
        FileInfo requestFile = RequestFile;

        if (requestFile.Exists)
            requestFile.Delete();

        if (request == null)
        {
            Logger.Log("SavedMatchStore: no launch request to store with the saved games.");
            return;
        }

        FileInfo spawnIniFile = SpawnIniFile;
        string spawnIniSHA1 = spawnIniFile.Exists ? Utilities.CalculateSHA1ForFile(spawnIniFile.FullName) : string.Empty;

        File.WriteAllText(requestFile.FullName, Serialize(request, spawnIniSHA1));
        Logger.Log("SavedMatchStore: stored the launch request with the saved games.");
    }

    internal static string Serialize(MatchLaunchRequest request, string spawnIniSHA1)
        => JsonSerializer.Serialize(new StoredMatch
        {
            SchemaVersion = SCHEMA_VERSION,
            SpawnIniSHA1 = spawnIniSHA1,
            Request = request,
        }, jsonOptions);

    /// <summary>Reads a stored request and the SHA1 of the spawnSG.ini stored with it, or returns null.</summary>
    internal static MatchLaunchRequest? Deserialize(string json, out string spawnIniSHA1)
    {
        StoredMatch? stored = JsonSerializer.Deserialize<StoredMatch>(json, jsonOptions);
        spawnIniSHA1 = stored?.SpawnIniSHA1 ?? string.Empty;

        if (stored?.Request == null || stored.SchemaVersion != SCHEMA_VERSION)
            return null;

        return stored.Request;
    }

    /// <summary>
    /// Returns the stored request, or null when there is none, it can't be read, or it belongs to an
    /// older match than spawnSG.ini (for example, after an older client saved a newer match).
    /// </summary>
    public static MatchLaunchRequest? LoadRequest()
    {
        FileInfo requestFile = RequestFile;

        if (!requestFile.Exists)
            return null;

        MatchLaunchRequest? request;
        string spawnIniSHA1;

        try
        {
            request = Deserialize(File.ReadAllText(requestFile.FullName), out spawnIniSHA1);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or IOException)
        {
            Logger.Log("SavedMatchStore: could not read the stored launch request: " + ex.Message);
            return null;
        }

        if (request == null)
        {
            Logger.Log("SavedMatchStore: the stored launch request is empty or has an unknown version.");
            return null;
        }

        FileInfo spawnIniFile = SpawnIniFile;

        if (spawnIniFile.Exists && Utilities.CalculateSHA1ForFile(spawnIniFile.FullName) != spawnIniSHA1)
        {
            Logger.Log("SavedMatchStore: spawnSG.ini has changed since the launch request was stored; ignoring the request.");
            return null;
        }

        return request;
    }

    /// <summary>
    /// Returns the stored match's spawn.ini: regenerated from the stored request when there is one,
    /// otherwise read from spawnSG.ini. Returns an empty INI when no match is stored.
    /// </summary>
    public static IniFile LoadSpawnIni()
    {
        MatchLaunchRequest? request = LoadRequest();

        if (request != null)
            return SpawnIniWriter.WriteMatch(request, SpawnIniDialect.ForClientType(ClientConfiguration.Instance.ClientGameType));

        return new IniFile(SpawnIniFile.FullName);
    }

    /// <summary>Returns what the client shows and checks about the stored match.</summary>
    public static SavedMatchInfo LoadInfo() => new(LoadSpawnIni());

    /// <summary>
    /// While saves keep both files, checks that the spawn.ini regenerated from the stored request is
    /// the same as spawnSG.ini, and logs the result.
    /// </summary>
    [Conditional("DEBUG")]
    public static void CheckAgainstSavedSpawnIni(MatchLaunchRequest request)
    {
        FileInfo spawnIniFile = SpawnIniFile;

        if (!spawnIniFile.Exists)
            return;

        IniFile regenerated = SpawnIniWriter.WriteMatch(request, SpawnIniDialect.ForClientType(ClientConfiguration.Instance.ClientGameType));
        IniFile saved = new IniFile(spawnIniFile.FullName);

        if (Render(regenerated) == Render(saved))
        {
            Logger.Log("SAVED MATCH CHECK: OK");
            return;
        }

        string regeneratedPath = Path.Combine(spawnIniFile.DirectoryName!, "spawnSG.regenerated.ini");
        regenerated.WriteIniFile(regeneratedPath);
        Logger.Log("SAVED MATCH CHECK MISMATCH: the spawn.ini regenerated from spawnSG.json differs from spawnSG.ini. " +
            "Compare " + regeneratedPath + " with " + spawnIniFile.FullName);
    }

    private static string Render(IniFile iniFile)
    {
        using var stream = new MemoryStream();
        iniFile.WriteIniStream(stream);

        return Convert.ToBase64String(stream.ToArray());
    }

    private sealed class StoredMatch
    {
        public int SchemaVersion { get; set; }

        /// <summary>The SHA1 of the spawnSG.ini written for the same match, to detect a stale request.</summary>
        public string SpawnIniSHA1 { get; set; } = string.Empty;

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
