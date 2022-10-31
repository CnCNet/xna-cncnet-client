using System.Collections.Generic;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

[JsonConverter(typeof(QmRequestSpawnResponseSpawnConverter))]
public class QmRequestSpawnResponseSpawn
{
    [JsonProperty("SpawnLocations")]
    public IDictionary<string, int> SpawnLocations { get; set; }

    [JsonProperty("Settings")]
    public QmRequestResponseSpawnSettings Settings { get; set; }

    /// <summary>
    /// This is NOT part of the typical JSON that is used to serialize/deserialize this class.
    ///
    /// The typical JSON contains explicit properties of "Other1", "Other2", up to "Other7".
    /// Rather than having an explicit property in this class for each one, we use the
    /// <see cref="QmRequestSpawnResponseSpawnConverter"/> to read/write out each property
    /// into the list you see below.
    /// </summary>
    [JsonIgnore]
    public List<QmRequestSpawnResponseSpawnOther> Others { get; set; }
}