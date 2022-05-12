using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

public class QmRequestResponseSpawnSettings
{
    [JsonProperty("UIMapName")]
    public string UIMapName { get; set; }

    [JsonProperty("MapHash")]
    public string MapHash { get; set; }

    [JsonProperty("Seed")]
    public int Seed { get; set; }

    [JsonProperty("GameID")]
    public int GameId { get; set; }

    [JsonProperty("WOLGameID")]
    public int WOLGameId { get; set; }

    [JsonProperty("Host")]
    public string Host { get; set; }

    [JsonProperty("IsSpectator")]
    public string IsSpectator { get; set; }

    [JsonProperty("Name")]
    public string Name { get; set; }

    [JsonProperty("Port")]
    public int Port { get; set; }

    [JsonProperty("Side")]
    public int SideId { get; set; }

    [JsonProperty("Color")]
    public int ColorId { get; set; }

    [JsonProperty("GameSpeed")]
    public string GameSpeedId { get; set; }

    [JsonProperty("Credits")]
    public string Credits { get; set; }

    [JsonProperty("UnitCount")]
    public string UnitCount { get; set; }

    [JsonProperty("SuperWeapons")]
    public string SuperWeapons { get; set; }

    [JsonProperty("Tournament")]
    public string Tournament { get; set; }

    [JsonProperty("ShortGame")]
    public string ShortGame { get; set; }

    [JsonProperty("Bases")]
    public string Bases { get; set; }

    [JsonProperty("MCVRedeploy")]
    public string MCVRedeploy { get; set; }

    [JsonProperty("MultipleFactory")]
    public string MultipleFactory { get; set; }

    [JsonProperty("Crates")]
    public string Crates { get; set; }

    [JsonProperty("GameMode")]
    public string GameMode { get; set; }

    [JsonProperty("FrameSendRate")]
    public string FrameSendRate { get; set; }

    [JsonProperty("DisableSWvsYuri")]
    public string DisableSWvsYuri { get; set; }
}