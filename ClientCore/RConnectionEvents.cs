/// @author Rami "Rampastring" Pasanen
/// http://www.moddb.com/members/rampastring
/// @version 16. 12. 2014

using System;
using System.Collections.Generic;
using System.Text;

namespace ClientCore
{
    /// <summary>
    /// Contains events used for handling data sent by the CnCNet server.
    /// </summary>
    public class RConnectionBridge
    {
        public delegate void NoParamEventHandler();
        public delegate void StringEventHandler(string message);
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
        public delegate void ChannelModeEventHandler(string sender, string channelName, string modeString);
        public delegate void IncorrectPasswordEventHandler(string channelName);
        public delegate void UserKickedEventHandler(string channelName, string userName);

        public event NoParamEventHandler ConnectionCompleted;
        public event StringEventHandler ServerMessageParsed;
        public event StringEventHandler WelcomeMessageParsed;
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
        public event ChannelModeEventHandler OnChannelModesChanged;
        public event IncorrectPasswordEventHandler OnIncorrectPassword;
        public event UserKickedEventHandler OnUserKicked;

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

        public void DoUserKicked(string channelName, string userName)
        {
            if (OnUserKicked != null)
                OnUserKicked(channelName, userName);
        }

        public event StringEventHandler OnMessageSent;

        /// <summary>
        /// Sends a message to the CnCNet server.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendMessage(string message)
        {
            OnMessageSent(message);
        }
    }
}
