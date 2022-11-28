using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Utilities;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

public class QmSide
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("ladder_id")]
    public int LadderId { get; set; }

    [JsonProperty("local_id")]
    public int LocalId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonIgnore]
    public bool IsRandom => Name == QmStrings.RandomSideName;

    public static QmSide CreateRandomSide() => new() { Name = QmStrings.RandomSideName };
}