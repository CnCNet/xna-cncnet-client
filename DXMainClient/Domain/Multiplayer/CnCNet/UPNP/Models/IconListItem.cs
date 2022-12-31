using System.Runtime.Serialization;

namespace DTAClient.Domain.Multiplayer.CnCNet.UPNP;

[DataContract(Name = "icon", Namespace = UPnPConstants.UPnPDevice10Namespace)]
internal readonly record struct IconListItem(
    [property: DataMember(Name = "mimetype", Order = 0)] string Mimetype,
    [property: DataMember(Name = "width", Order = 1)] int Width,
    [property: DataMember(Name = "height", Order = 2)] int Height,
    [property: DataMember(Name = "depth", Order = 3)] int Depth,
    [property: DataMember(Name = "url", Order = 4)] string Url);