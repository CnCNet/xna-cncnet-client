using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet;
using System;
using System.Collections.Generic;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    class GameCreationEventArgs : EventArgs
    {
        public GameCreationEventArgs(string roomName, int maxPlayers, 
            string password, CnCNetTunnel tunnel, List<PlayerInfo> players)
        {
            GameRoomName = roomName;
            MaxPlayers = maxPlayers;
            Password = password;
            Tunnel = tunnel;
            Players = players;
        }

        public string GameRoomName { get; private set; }
        public int MaxPlayers { get; private set; }
        public List<PlayerInfo> Players { get; private set; }
        public string Password { get; private set; }
        public CnCNetTunnel Tunnel { get; private set; }
    }
}
