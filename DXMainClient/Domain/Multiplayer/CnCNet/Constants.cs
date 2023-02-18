using System;
using System.Net;
using System.Net.Http;

namespace DTAClient.Domain.Multiplayer.CnCNet;

internal static class Constants
{
    public static HttpClient CnCNetHttpClient
        => new(
            new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                AutomaticDecompression = DecompressionMethods.All
            },
            true)
        {
            Timeout = TimeSpan.FromSeconds(10),
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
        };

    internal const int TUNNEL_VERSION_2 = 2;
    internal const int TUNNEL_VERSION_3 = 3;
}