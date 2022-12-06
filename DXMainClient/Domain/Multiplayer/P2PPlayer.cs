using System.Collections.Generic;
using System.Net;

namespace DTAClient.Domain.Multiplayer;

internal readonly record struct P2PPlayer(
    string RemotePlayerName,
    ushort[] RemotePorts,
    List<(IPAddress RemoteIpAddress, long Ping)> LocalPingResults,
    List<(IPAddress RemoteIpAddress, long Ping)> RemotePingResults,
    bool Enabled);