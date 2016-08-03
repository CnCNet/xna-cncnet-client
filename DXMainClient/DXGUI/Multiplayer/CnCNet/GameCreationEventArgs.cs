using DTAClient.Domain.Multiplayer.CnCNet;
using System;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    class GameCreationEventArgs : EventArgs
    {
        public GameCreationEventArgs(string roomName, int maxPlayers, 
            string password, CnCNetTunnel tunnel)
        {
            GameRoomName = roomName;
            MaxPlayers = maxPlayers;
            Password = password;
            Tunnel = tunnel;
        }

        public string GameRoomName { get; private set; }
        public int MaxPlayers { get; private set; }
        public string Password { get; private set; }
        public CnCNetTunnel Tunnel { get; private set; }
    }
}
