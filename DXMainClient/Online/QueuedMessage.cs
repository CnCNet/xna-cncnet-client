using System;

namespace DTAClient.Online
{
    /// <summary>
    /// A queued network message.
    /// </summary>
    public class QueuedMessage
    {
        public QueuedMessage() { }

        public QueuedMessage(string command) { }

        public QueuedMessage(string command, QueuedMessageType type, int priority)
        {
            Command = command;
            MessageType = type;
            Priority = priority;
            Delay = -1;
            SendAt = DateTime.Now;
        }

        public QueuedMessage(string command, QueuedMessageType type, int priority, int delay)
        {
            Command = command;
            MessageType = type;
            Priority = priority;
            Delay = delay;
            SendAt = DateTime.Now.AddMilliseconds(Delay);
        }

        /// <summary>
        /// Message Queue ID
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The command to send to the IRC network.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// The type of the message.
        /// </summary>
        public QueuedMessageType MessageType { get; set; }

        /// <summary>
        /// The priority of the message.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// The amount of milliseconds to delay the message.
        /// </summary>
        public int Delay { get; set; } = -1;

        /// <summary>
        /// The amount of milliseconds to delay the message.
        /// </summary>
        public DateTime SendAt { get; set; } = DateTime.Now;
    }
}
