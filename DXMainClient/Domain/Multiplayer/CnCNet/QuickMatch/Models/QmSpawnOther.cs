using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

public class QmSpawnOther
{
    private const string DEFAULT_IP = "127.0.0.1";

    [JsonProperty("Name")]
    public string Name { get; set; }

    [JsonProperty("Side")]
    public int Side { get; set; }

    [JsonProperty("Color")]
    public int Color { get; set; }

    [JsonProperty("Ip")]
    public string Ip
    {
        get => _ip ?? DEFAULT_IP;
        set => _ip = value;
    }

    [JsonProperty("Port")]
    public int Port { get; set; }

    [JsonProperty("IPv6")]
    public string IPv6 { get; set; }

    [JsonProperty("PortV6")]
    public int PortV6 { get; set; }

    [JsonProperty("LanIP")]
    public string LanIP
    {
        get => _lanIp ?? DEFAULT_IP;
        set => _lanIp = value;
    }

    [JsonProperty("LanPort")]
    public int LanPort { get; set; }

    private string _ip { get; set; }

    private string _lanIp { get; set; }
}