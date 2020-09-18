using System.Collections.Generic;

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

        void OnChannelTopicChanged(string userName, string channelName, string topic);

        void OnUserListReceived(string channelName, string[] userList);

        void OnWhoReplyReceived(string ident, string hostName, string userName, string extraInfo);

        void OnChannelFull(string channelName);

        void OnTargetChangeTooFast(string channelName, string message);

        void OnChannelInviteOnly(string channelName);

        void OnIncorrectChannelPassword(string channelName);

        void OnCTCPParsed(string channelName, string userName, string message);

        void OnNoticeMessageParsed(string notice, string userName);

        void OnUserJoinedChannel(string channelName, string hostName, string userName, string ident);

        void OnUserLeftChannel(string channelName, string userName);

        void OnUserQuitIRC(string userName);

        void OnChatMessageReceived(string receiver, string senderName, string senderIdent, string message);

        void OnPrivateMessageReceived(string sender, string message);

        void OnChannelModesChanged(string userName, string channelName, string modeString, List<string> modeParameters);

        void OnUserKicked(string channelName, string userName);

        void OnErrorReceived(string errorMessage);

        void OnNameAlreadyInUse();

        void OnBannedFromChannel(string channelName);

        void OnUserNicknameChange(string oldNickname, string newNickname);

        // **********************
        // Connection-related methods
        // **********************

        void OnAttemptedServerChanged(string serverName);

        void OnConnectAttemptFailed();

        void OnConnectionLost(string reason);

        void OnReconnectAttempt();

        void OnDisconnected();

        void OnConnected();

        bool GetDisconnectStatus();

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
