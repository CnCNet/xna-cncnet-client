using Microsoft.Xna.Framework;
using System;

namespace DTAClient.Online
{
    public class ChatMessage
    {
        /// <summary>
        /// Creates a new ChatMessage instance.
        /// </summary>
        /// <param name="senderName">The sender of the message. Use null for none (system messages).</param>
        /// <param name="color">The color of the message.</param>
        /// <param name="dateTime">The date and time of the message.</param>
        /// <param name="message">The message.</param>
        public ChatMessage(string senderName, Color color, DateTime dateTime, string message)
        {
            SenderName = senderName;
            Color = color;
            DateTime = dateTime;
            Message = message;
        }

        /// <summary>
        /// Creates a chat message with the date and time set to the current system date and time.
        /// </summary>
        /// <param name="senderName">The sender of the message. Use null for none (system messages).</param>
        /// <param name="color">The color of the message.</param>
        /// <param name="message">The message.</param>
        public ChatMessage(string senderName, Color color, string message) : this(senderName, color, DateTime.Now, message) { }

        /// <summary>
        /// Creates a new ChatMessage instance.
        /// </summary>
        /// <param name="senderName">The sender of the message. Use null for none (system messages).</param>
        /// <param name="ident">The IRC identifier of the sender.</param>
        /// <param name="senderIsAdmin">The sender of the message is a channel admin.</param>
        /// <param name="color">The color of the message.</param>
        /// <param name="dateTime">The date and time of the message.</param>
        /// <param name="message">The message.</param>
        public ChatMessage(string senderName, string ident, bool senderIsAdmin, Color color, DateTime dateTime, string message) : this(senderName, color, dateTime, message)
        {
            SenderIdent = ident;
            SenderIsAdmin = senderIsAdmin;
        }

        /// <summary>
        /// Creates a chat message that has no sender and has the date and time set to the
        /// current system date and time.
        /// </summary>
        /// <param name="color">The color of the message.</param>
        /// <param name="message">The message.</param>
        public ChatMessage(Color color, string message) : this(null, color, DateTime.Now, message) { }

        /// <summary>
        /// Creates a chat message that has no sender and has the date and time set to the
        /// current system date and time.
        /// </summary>
        /// <param name="message">The message.</param>
        public ChatMessage(string message) : this(Color.White, message) { }

        public string SenderName { get; private set; }
        public string SenderIdent { get; private set; }
        public Color Color { get; private set; }
        public DateTime DateTime { get; private set; }
        public string Message { get; private set; }
        public bool SenderIsAdmin { get; private set; }

        public bool IsUser => SenderIdent != null;
    }
}
