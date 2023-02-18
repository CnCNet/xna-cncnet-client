using System;

namespace DTAClient.Domain.Multiplayer.CnCNet;

internal readonly record struct InternetGatewayDeviceResponse(Uri Location, string Server, string CacheControl, string Ext, string SearchTarget, string Usn);