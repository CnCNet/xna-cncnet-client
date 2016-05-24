using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.Online
{
    /// <summary>
    /// An interface for handling IRC messages.
    /// </summary>
    public interface IConnectionManager
    {
        void OnWelcomeMessageReceived(string message);

        void OnGenericServerMessageReceived(string message);

        void OnAwayMessageReceived(string userName, string reason);

        void OnChannelTopicReceived(string channelName, string topic);

        void OnUserListReceived(string channelName, string[] userList);

        void OnWhoReplyReceived(string userName, string extraInfo);

        void OnChannelFull(string channelName);

        void OnIncorrectChannelPassword(string channelName);

        void OnCTCPParsed(string channelName, string userName, string message);

        void OnNoticeMessageParsed(string notice, string userName);

        void OnUserJoinedChannel(string channelName, string userName, string userAddress);

        void OnUserLeftChannel(string channelName, string userName);

        void OnUserQuitIRC(string userName);

        void OnChatMessageReceived(string receiver, string sender, string message);

        void OnPrivateMessageReceived(string sender, string message);

        void OnChannelModesChanged(string userName, string channelName, string modeString);

        void OnUserKicked(string channelName, string userName);

        void OnErrorReceived(string errorMessage);

        // **********************
        // Connection-related methods
        // **********************

        void OnAttemptedServerChanged(string serverName);

        void OnConnectAttemptFailed();

        void OnConnectionLost(string reason);

        void OnReconnectAttempt();

        void OnDisconnected();

        void OnConnected();

        //public EventHandler<ServerMessageEventArgs> WelcomeMessageReceived;
        //public EventHandler<ServerMessageEventArgs> GenericServerMessageReceived;
        //public EventHandler<UserAwayEventArgs> AwayMessageReceived;
        //public EventHandler<ChannelTopicEventArgs> ChannelTopicReceived;
        //public EventHandler<UserListEventArgs> UserListReceived;
        //public EventHandler<WhoEventArgs> WhoReplyReceived;
        //public EventHandler<ChannelEventArgs> ChannelFull;
        //public EventHandler<ChannelEventArgs> IncorrectChannelPassword;

        //public event EventHandler<AttemptedServerEventArgs> AttemptedServerChanged;
        //public event EventHandler ConnectAttemptFailed;
        //public event EventHandler<ConnectionLostEventArgs> ConnectionLost;
        //public event EventHandler ReconnectAttempt;
    }
}
