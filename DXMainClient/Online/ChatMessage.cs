using Microsoft.Xna.Framework;
using System;

namespace DTAClient.Online
{
    public class ChatMessage
    {
        /// <summary>
        /// Creates a new ChatMessage instance.
        /// </summary>
        /// <param name="sender">The sender of the message. Use null for none (system messages).</param>
        /// <param name="color">The color of the message.</param>
        /// <param name="dateTime">The date and time of the message.</param>
        /// <param name="message">The message.</param>
        public ChatMessage(string sender, Color color, DateTime dateTime, string message)
        {
            Sender = sender;
            Color = color;
            DateTime = dateTime;
            Message = message;
        }

        /// <summary>
        /// Creates a chat message with the date and time set to the current system date and time.
        /// </summary>
        /// <param name="sender">The sender of the message. Use null for none (system messages).</param>
        /// <param name="color">The color of the message.</param>
        /// <param name="message">The message.</param>
        public ChatMessage(string sender, Color color, string message) : this(sender, color, DateTime.Now, message) { }

        /// <summary>
        /// Creates a chat message that has no sender and has the date and time set to the
        /// current system date and time.
        /// </summary>
        /// <param name="color">The color of the message.</param>
        /// <param name="message">The message.</param>
        public ChatMessage(Color color, string message) : this(null, color, DateTime.Now, message) { }

        public string Sender { get; private set; }
        public Color Color { get; private set; }
        public DateTime DateTime { get; private set; }
        public string Message { get; private set; }
    }
}
