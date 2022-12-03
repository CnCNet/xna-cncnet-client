using System;

namespace DTAClient.Domain.Multiplayer.CnCNet;

internal sealed class GameDataReceivedEventArgs : EventArgs
{
    public GameDataReceivedEventArgs(uint playerId, ReadOnlyMemory<byte> gameData)
    {
        PlayerId = playerId;
        GameData = gameData;
    }

    public uint PlayerId { get; }

    public ReadOnlyMemory<byte> GameData { get; }
}