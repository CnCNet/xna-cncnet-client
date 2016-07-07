using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace DTAClient.domain.Multiplayer.LAN
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
