using System.Runtime.Serialization;

namespace DTAClient.Domain.Multiplayer.CnCNet.UPNP;

[DataContract(Name = "specVersion", Namespace = UPnPConstants.UPnPDevice10Namespace)]
internal readonly record struct SpecVersion(
    [property: DataMember(Name = "major", Order = 0)] int Major,
    [property: DataMember(Name = "minor", Order = 1)] int Minor);