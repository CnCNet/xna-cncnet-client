#nullable enable
using System;
using System.Net;

namespace DTAClient.DXGUI.Multiplayer;

/// <summary>
/// Event arguments for network message received events.
/// </summary>
internal class LANLobbyBroadcastMessageReceivedEventArgs : EventArgs
{
    /// <summary>
    /// The received message data.
    /// </summary>
    public string Data { get; }

    /// <summary>
    /// The endpoint from which the message was received.
    /// </summary>
    public IPEndPoint EndPoint { get; }

    public LANLobbyBroadcastMessageReceivedEventArgs(string data, IPEndPoint endPoint)
    {
        Data = data;
        EndPoint = endPoint;
    }
}
