using System;

namespace DTAClient.Domain.Multiplayer.CnCNet;

internal sealed class DataReceivedEventArgs : EventArgs
{
    public DataReceivedEventArgs(uint playerId, ReadOnlyMemory<byte> gameData)
    {
        PlayerId = playerId;
        GameData = gameData;
    }

    public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;

    public uint PlayerId { get; }

    public ReadOnlyMemory<byte> GameData { get; }
}