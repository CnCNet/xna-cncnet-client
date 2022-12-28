using System;

namespace DTAClient.Domain.Multiplayer.CnCNet;

internal sealed class DataReceivedEventArgs : EventArgs
{
    public DataReceivedEventArgs(uint playerId, Memory<byte> gameData)
    {
        PlayerId = playerId;
        GameData = gameData;
    }

    public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;

    public uint PlayerId { get; }

    public Memory<byte> GameData { get; }
}