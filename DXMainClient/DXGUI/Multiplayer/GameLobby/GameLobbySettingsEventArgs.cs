using System;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

public class GameLobbySettingsEventArgs(string gameRoomName, int maxPlayers, int skillLevel, string password) : EventArgs
{
    public string GameRoomName { get; } = gameRoomName;
    public int MaxPlayers { get; } = maxPlayers;
    public int SkillLevel { get; } = skillLevel;
    public string Password { get; } = password;
}
