using System;
using System.Net;

namespace DTAClient.Domain.Multiplayer.CnCNet.UPNP;

internal readonly record struct InternetGatewayDeviceResponse(Uri Location, string Server, string CacheControl, string Ext, string SearchTarget, string Usn, IPAddress LocalIpAddress);