using System.Runtime.Serialization;

namespace DTAClient.Domain.Multiplayer.CnCNet;

[DataContract(Name = "root", Namespace = "urn:schemas-upnp-org:device-1-0")]
internal readonly record struct UPnPDescription(
    [property: DataMember(Name = "specVersion", Order = 0)] SpecVersion SpecVersion,
    [property: DataMember(Name = "systemVersion", Order = 1)] SystemVersion SystemVersion,
    [property: DataMember(Name = "device", Order = 2)] Device Device);