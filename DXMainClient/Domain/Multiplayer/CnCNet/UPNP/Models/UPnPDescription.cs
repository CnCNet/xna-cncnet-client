using System.Runtime.Serialization;

namespace DTAClient.Domain.Multiplayer.CnCNet.UPNP;

[DataContract(Name = "root", Namespace = UPnPConstants.UPnPDevice10Namespace)]
internal readonly record struct UPnPDescription(
    [property: DataMember(Name = "specVersion", Order = 0)] SpecVersion SpecVersion,
    [property: DataMember(Name = "systemVersion", Order = 1)] SystemVersion SystemVersion,
    [property: DataMember(Name = "device", Order = 2)] Device Device);