/// @author Rami "Rampastring" Pasanen
/// http://www.moddb.com/members/rampastring
/// @version 21. 4. 2015

using System;
using ClientCore.CnCNet5;

namespace ClientCore
{
    /// <summary>
    /// Contains events used for handling data sent by the CnCNet server.
    /// </summary>
    public class RConnectionBridge
    {
        public delegate void NoParamEventHandler();
        public delegate void StringEventHandler(string message);
        public delegate void NoticeEventHandler(string message, string sender);
        public delegate void UserListEventHandler(string[] userNames, string channelName);
        public delegate void ChannelJoinEventHandler(string channelName, string userName, string address);
        public delegate void ChannelLeaveEventHandler(string channelName, string userName);
        public delegate void TopicMessageParsedEventHandler(string channelName, string message);
        public delegate void PrivmsgParsedEventHandler(string channelName, string message, string sender);
        public delegate void PrivateMessageParsedEventHandler(string message, string sender);
        public delegate void PrivateMessageSentEventHandler(string message, string receiver);
        public delegate void ErrorEventHandler(string message);
        public delegate void AwayEventHandler(string userName, string reason);
        public delegate void ConnectionLostEventHandler(string errorMessage);
        public delegate void CTCPParsedEventHandler(string sender, string channelName, string message);
        public delegate void CTCPGameParsedEventHandler(string sender, string channelName, string message);
        public delegate void CTCPRestartParsedEventHandler(string version, string channel);
        public delegate void ChannelModeEventHandler(string sender, string channelName, string modeString);
        public delegate void IncorrectPasswordEventHandler(string channelName);
        public delegate void ChannelFullEventHandler(string channelName);
        public delegate void UserKickedEventHandler(string channelName, string userName);
        public delegate void WhoEventHandler(string userName, string extraInfo);
        public delegate void QueuedMessageDelegate(QueuedMessage qm);

        public event NoParamEventHandler ConnectionCompleted;
        public event StringEventHandler ServerMessageParsed;
        public event StringEventHandler WelcomeMessageParsed;
        public event NoticeEventHandler NoticeMessageParsed;
        public event UserListEventHandler UserListReceived;
        public event ChannelJoinEventHandler OnUserJoinedChannel;
        public event ChannelLeaveEventHandler OnUserLeaveChannel;
        public event TopicMessageParsedEventHandler TopicMessageParsed;
        public event StringEventHandler OnUserQuit;
        public event PrivmsgParsedEventHandler PrivmsgParsed;
        public event PrivateMessageParsedEventHandler PrivateMessageParsed;
        public event PrivateMessageSentEventHandler PrivateMessageSent;
        public event ErrorEventHandler OnErrorReceived;
        public event AwayEventHandler OnAwayMessageReceived;
        public event ConnectionLostEventHandler OnConnectionLost;
        public event CTCPParsedEventHandler OnCtcpParsed;
        public event CTCPGameParsedEventHandler OnCtcpGameParsed;
        public event CTCPRestartParsedEventHandler OnCtcpRestartParsed;
        public event ChannelModeEventHandler OnChannelModesChanged;
        public event IncorrectPasswordEventHandler OnIncorrectPassword;
        public event ChannelFullEventHandler OnChannelFull;
        public event UserKickedEventHandler OnUserKicked;
        public event WhoEventHandler OnWhoReplyReceived;

        public void DoConnectionCompleted()
        {
            if (ConnectionCompleted != null)
                ConnectionCompleted();
        }

        public void DoServerMessageParsed(string message)
        {
            if (ServerMessageParsed != null)
                ServerMessageParsed(message);
        }

        public void DoNoticeParsed(string message, string sender)
        {
            if (NoticeMessageParsed != null)
                NoticeMessageParsed(message, sender);
        }

        public void DoWelcomeMessageParsed(string message)
        {
            if (WelcomeMessageParsed != null)
                WelcomeMessageParsed(message);
        }

        public void DoUserListReceived(string[] userNames, string channelName)
        {
            if (UserListReceived != null)
                UserListReceived(userNames, channelName);
        }

        public void DoUserJoinedChannel(string channelName, string userName, string ipAddress)
        {
            if (OnUserJoinedChannel != null)
                OnUserJoinedChannel(channelName, userName, ipAddress);
        }

        public void DoUserLeaveChannel(string channelName, string userName)
        {
            if (OnUserLeaveChannel != null)
                OnUserLeaveChannel(channelName, userName);
        }

        public void DoTopicMessageParsed(string channelName, string message)
        {
            if (TopicMessageParsed != null)
                TopicMessageParsed(channelName, message);
        }

        public void DoUserQuit(string userName)
        {
            if (OnUserQuit != null)
                OnUserQuit(userName);
        }

        public void DoPrivmsgParsed(string channelName, string message, string sender)
        {
            if (PrivmsgParsed != null)
                PrivmsgParsed(channelName, message, sender);
        }

        public void DoPrivateMessageParsed(string message, string sender)
        {
            if (PrivateMessageParsed != null)
                PrivateMessageParsed(message, sender);
        }

        public void DoPrivateMessageSent(string message, string receiver)
        {
            if (PrivateMessageSent != null)
                PrivateMessageSent(message, receiver);
        }

        public void DoErrorReceived(string errorMessage)
        {
            if (OnErrorReceived != null)
                OnErrorReceived(errorMessage);
        }

        public void DoAwayMessageReceived(string userName, string reason)
        {
            if (OnAwayMessageReceived != null)
                OnAwayMessageReceived(userName, reason);
        }

        public void DoConnectionLost(string errorMessage)
        {
            if (OnConnectionLost != null)
                OnConnectionLost(errorMessage);
        }

        public void DoCtcpParsed(string sender, string channelName, string message)
        {
            if (OnCtcpParsed != null)
                OnCtcpParsed(sender, channelName, message);
        }

        public void DoCtcpGameParsed(string sender, string channelName, string message)
        {
            if (OnCtcpGameParsed != null)
                OnCtcpGameParsed(sender, channelName, message);
        }

        public void DoCtcpRestartParsed(string version, string channel)
        {
            if (OnCtcpRestartParsed != null)
                OnCtcpRestartParsed(version, channel);
        }

        public void DoChannelModesChanged(string sender, string channelName, string modeString)
        {
            if (OnChannelModesChanged != null)
                OnChannelModesChanged(sender, channelName, modeString);
        }

        public void DoIncorrectPassword(string channelName)
        {
            if (OnIncorrectPassword != null)
                OnIncorrectPassword(channelName);
        }

        public void DoChannelFull(string channelName)
        {
            if (OnChannelFull != null)
                OnChannelFull(channelName);
        }

        public void DoUserKicked(string channelName, string userName)
        {
            if (OnUserKicked != null)
                OnUserKicked(channelName, userName);
        }

        public void DoWhoReplyReceived(string userName, string extraInfo)
        {
            if (OnWhoReplyReceived != null)
                OnWhoReplyReceived(userName, extraInfo);
        }

        public event StringEventHandler OnMessageSent;
        public event NoParamEventHandler OnUIInitialized;

        public event QueuedMessageDelegate OnMessageQueued;

        public void SendQueuedMessage(QueuedMessage qm)
        {
            OnMessageQueued(qm);
        }

        /// <summary>
        /// Sends a message to the CnCNet server.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendMessage(string message)
        {
            OnMessageSent(message);
        }

        /// <summary>
        /// Sends a CTCP message to the CnCNet server.
        /// </summary>
        /// <param name="channelName">The name of the channel where the CTCP message will be sent to.</param>
        /// <param name="message">The message to send.</param>
        public void SendCTCPMessage(string channelName, string message)
        {
            char CTCPChar1 = (char)58;
            char CTCPChar2 = (char)01;

            SendMessage("NOTICE " + channelName + " " + CTCPChar1 + CTCPChar2 + message + CTCPChar2);
        }

        public void SendCTCPMessage(string channelName, string message, QueuedMessageType type, int priority)
        {
            char CTCPChar1 = (char)58;
            char CTCPChar2 = (char)01;

            SendQueuedMessage(new QueuedMessage("NOTICE " + channelName + " " + CTCPChar1 + CTCPChar2 + message + CTCPChar2,
                type, priority));
        }

        /// <summary>
        /// Sends a chat message to a recipient.
        /// </summary>
        /// <param name="recipient">The recipient of the message (a user or a channel).</param>
        /// <param name="colorId">The index of the color that is used for the message.
        /// Specify -1 to not send color information with the message.</param>
        /// <param name="message">The message itself.</param>
        public void SendChatMessage(string recipient, int colorId, string message)
        {
            string colorString = String.Empty;
            if (colorId > -1)
            {
                colorString = Convert.ToString((char)03);
                if (colorId < 10)
                    colorString = colorString + "0" + Convert.ToString(colorId);
                else
                    colorString = colorString + Convert.ToString(colorId);
            }

            string messageToSend = "PRIVMSG " + recipient + " :" + colorString + message;
            SendMessage(messageToSend);
        }

        /// <summary>
        /// Needs to be called once after initializing the UI.
        /// </summary>
        public void DoUIInitialized()
        {
            OnUIInitialized();
        }
    }
}
