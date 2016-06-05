using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DTAClient.domain.CnCNet;
using Rampastring.XNAUI;
using DTAClient.Online;
using ClientCore;
using DTAClient.Online.EventArguments;
using Microsoft.Xna.Framework;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    class CnCNetGameLobby : MultiplayerGameLobby
    {
        public CnCNetGameLobby(WindowManager windowManager, string iniName, 
            List<GameMode> GameModes, CnCNetManager connectionManager) : 
            base(windowManager, iniName, GameModes)
        {
            this.connectionManager = connectionManager;
            localGame = DomainController.Instance().GetDefaultGame();
        }

        Channel channel;
        CnCNetManager connectionManager;
        string localGame;

        string tunnelAddress;
        int tunnelPort;

        public void SetUp(Channel channel, bool isHost, int playerLimit, 
            string tunnelAddress, int tunnelPort)
        {
            this.channel = channel;
            channel.MessageAdded += Channel_MessageAdded;
            channel.CTCPReceived += Channel_CTCPReceived;
            channel.UserKicked += Channel_UserKicked;

            if (isHost)
            {
                channel.UserAdded += Channel_UserAdded;
                channel.UserQuitIRC += Channel_UserQuitIRC;
                channel.UserLeft += Channel_UserLeft;

                connectionManager.SendCustomMessage(new QueuedMessage(
                    string.Format("MODE {0} +klnNs {1} {2}", channel.ChannelName, 
                    channel.Password, playerLimit),
                    QueuedMessageType.SYSTEM_MESSAGE, 50));

                connectionManager.SendCustomMessage(new QueuedMessage(
                    string.Format("TOPIC {0} :{1}", channel.ChannelName, 
                    ProgramConstants.CNCNET_PROTOCOL_REVISION + ";" + localGame.ToLower()),
                    QueuedMessageType.SYSTEM_MESSAGE, 50));
            }
            else
            {
                channel.ChannelModesChanged += Channel_ChannelModesChanged;
            }

            this.tunnelAddress = tunnelAddress;
            this.tunnelPort = tunnelPort;

            connectionManager.ConnectionLost += ConnectionManager_ConnectionLost;
            connectionManager.Disconnected += ConnectionManager_Disconnected;

            Refresh(isHost);
        }

        public void Clear()
        {
            channel.MessageAdded -= Channel_MessageAdded;
            channel.CTCPReceived -= Channel_CTCPReceived;
            channel.UserKicked -= Channel_UserKicked;

            if (IsHost)
            {
                channel.UserAdded -= Channel_UserAdded;
                channel.UserQuitIRC -= Channel_UserQuitIRC;
                channel.UserLeft -= Channel_UserLeft;
            }
            else
            {
                channel.ChannelModesChanged -= Channel_ChannelModesChanged;
            }

            connectionManager.ConnectionLost -= ConnectionManager_ConnectionLost;
            connectionManager.Disconnected -= ConnectionManager_Disconnected;
        }

        private void ConnectionManager_Disconnected(object sender, EventArgs e)
        {
            HandleConnectionLoss();
        }

        private void ConnectionManager_ConnectionLost(object sender, ConnectionLostEventArgs e)
        {
            HandleConnectionLoss();
        }

        private void HandleConnectionLoss()
        {
            Clear();
            this.Visible = false;
            this.Enabled = false;
        }

        protected override void BtnLeaveGame_LeftClick(object sender, EventArgs e)
        {
            Clear();
            channel.Leave();
            this.Visible = false;
            this.Enabled = false;
        }

        private void Channel_UserQuitIRC(object sender, UserNameEventArgs e)
        {
            RemovePlayer(e.UserName);
        }

        private void Channel_UserLeft(object sender, UserNameEventArgs e)
        {
            RemovePlayer(e.UserName);
        }

        private void Channel_UserKicked(object sender, UserNameEventArgs e)
        {
            if (e.UserName == ProgramConstants.PLAYERNAME)
            {
                Clear();
                this.Visible = false;
                this.Enabled = false;
            }
        }

        private void Channel_UserAdded(object sender, UserEventArgs e)
        {
            PlayerInfo pInfo = new PlayerInfo(e.User.Name);
            Players.Add(pInfo);
            CopyPlayerDataToUI();
        }

        private void RemovePlayer(string playerName)
        {
            PlayerInfo pInfo = Players.Find(p => p.Name == playerName);

            if (pInfo != null)
            {
                Players.Remove(pInfo);

                CopyPlayerDataToUI();
            }
        }

        private void Channel_ChannelModesChanged(object sender, ChannelModeEventArgs e)
        {
            if (e.ModeString == "+i")
                AddNotice("The game room has been locked.");
            else if (e.ModeString == "-i")
                AddNotice("The game room has been unlocked.");
        }

        private void Channel_CTCPReceived(object sender, ChannelCTCPEventArgs e)
        {
            
            throw new NotImplementedException();
        }

        private void Channel_MessageAdded(object sender, IRCMessageEventArgs e)
        {
            if (e.Message.Sender == null)
                lbChatMessages.AddItem(string.Format("[{0}] {1}",
                    e.Message.DateTime.ToShortTimeString(),
                    Renderer.GetSafeString(e.Message.Message, lbChatMessages.FontIndex)),
                    e.Message.Color, true);
            else
                lbChatMessages.AddItem(string.Format("[{0}] {1}: {2}",
                    e.Message.DateTime.ToShortTimeString(), e.Message.Sender,
                    Renderer.GetSafeString(e.Message.Message, lbChatMessages.FontIndex)),
                    e.Message.Color, true);

            if (lbChatMessages.GetLastDisplayedItemIndex() == lbChatMessages.Items.Count - 2)
            {
                lbChatMessages.ScrollToBottom();
            }
        }

        protected override void HostLaunchGame()
        {
            throw new NotImplementedException();
        }

        protected override void RequestPlayerOptions(int side, int color, int start, int team)
        {
            channel.SendCTCPMessage(
                string.Format("OPTS {0};{1};{2};{3}", side, color, start, team),
                QueuedMessageType.GAME_SETTINGS_MESSAGE, 6);
        }

        protected override void RequestReadyStatus()
        {
            channel.SendCTCPMessage("READY 1", QueuedMessageType.GAME_PLAYERS_READY_STATUS_MESSAGE, 5);
        }

        protected override void AddNotice(string message, Color color)
        {
            channel.AddMessage(new IRCMessage(null, color, DateTime.Now, message));
        }
    }
}
