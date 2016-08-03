using Microsoft.Xna.Framework.Graphics;
using System;
using System.Net;

namespace DTAClient.Domain.Multiplayer.LAN
{
    public class LANLobbyUser
    {
        public LANLobbyUser(string name, Texture2D gameTexture, IPEndPoint endPoint)
        {
            Name = name;
            GameTexture = gameTexture;
            EndPoint = endPoint;
        }

        public string Name { get; private set; }
        public Texture2D GameTexture { get; private set; }
        public IPEndPoint EndPoint { get; private set; }
        public TimeSpan TimeWithoutRefresh { get; set; }
    }
}
