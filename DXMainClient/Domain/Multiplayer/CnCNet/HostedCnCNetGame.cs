using System;
using DTAClient.Online;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    public class HostedCnCNetGame : GenericHostedGame
    {
        public HostedCnCNetGame() { }

        public HostedCnCNetGame(string channelName, string revision, string gamever, int maxPlayers,
            string roomName, bool passworded,
            bool tunneled, 
            string[] players, string adminName, string mapName, string gameMode)
        {
            ChannelName = channelName;
            Revision = revision;
            GameVersion = gamever;
            MaxPlayers = maxPlayers;
            RoomName = roomName;
            Passworded = passworded;
            Tunneled = tunneled;
            Players = players;
            HostName = adminName;
            Map = mapName;
            GameMode = gameMode;
        }

        public string ChannelName { get; set; }
        public string Revision { get; set; }
        public bool Tunneled { get; set; }
        public bool IsLadder { get; set; }
        public string MatchID { get; set; }
        public CnCNetTunnel TunnelServer { get; set; }

        public override int Ping => TunnelServer.PingInMs;

        public override bool Equals(GenericHostedGame other) => 
            other is HostedCnCNetGame hostedCnCNetGame ? 
                string.Equals(hostedCnCNetGame.ChannelName, ChannelName, StringComparison.InvariantCultureIgnoreCase) :
                base.Equals(other);
    }
}
