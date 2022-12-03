using System.Runtime.Serialization;

namespace DTAClient.Domain.Multiplayer.CnCNet;

[DataContract(Name = "specVersion", Namespace = "urn:schemas-upnp-org:device-1-0")]
internal readonly record struct SpecVersion(
    [property: DataMember(Name = "major", Order = 0)] int Major,
    [property: DataMember(Name = "minor", Order = 1)] int Minor);