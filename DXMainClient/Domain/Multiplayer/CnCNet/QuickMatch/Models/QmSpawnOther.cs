using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

public class QmSpawnOther
{
    [JsonProperty("Name")]
    public string Name { get; set; }

    [JsonProperty("Side")]
    public int Side { get; set; }

    [JsonProperty("Color")]
    public int Color { get; set; }

    [JsonProperty("Ip")]
    public string Ip { get; set; }

    [JsonProperty("Port")]
    public int Port { get; set; }

    [JsonProperty("IPv6")]
    public string IPv6 { get; set; }

    [JsonProperty("PortV6")]
    public int PortV6 { get; set; }

    [JsonProperty("LanIP")]
    public string LanIP { get; set; }

    [JsonProperty("LanPort")]
    public int LanPort { get; set; }
}