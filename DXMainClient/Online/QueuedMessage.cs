using System;

namespace DTAClient.Online
{
    /// <summary>
    /// A queued network message.
    /// </summary>
    public class QueuedMessage
    {
        private const int DEFAULT_DELAY = -1;
        private const int REPLACE_DELAY = 1;
        
        public QueuedMessage(string command, QueuedMessageType type, int priority) : 
            this(command, type, priority, DEFAULT_DELAY, false)
        {
        }

        public QueuedMessage(string command, QueuedMessageType type, int priority, bool replace) : 
            this(command, type, priority, replace ? REPLACE_DELAY : DEFAULT_DELAY, replace)
        {
        }

        public QueuedMessage(string command, QueuedMessageType type, int priority, int delay) :
            this(command, type, priority, delay, false)
        {
        }

        private QueuedMessage(string command, QueuedMessageType type, int priority, int delay, bool replace)
        {
            Command = command;
            MessageType = type;
            Priority = priority;
            Delay = delay;
            SendAt = Delay < 0  ? DateTime.Now : DateTime.Now.AddMilliseconds(Delay);
            Replace = replace;
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
        public int Delay { get; set; }

        /// <summary>
        /// The amount of milliseconds to delay the message.
        /// </summary>
        public DateTime SendAt { get; set; }

        /// <summary>
        /// This can be used to replace a message on the queue to help prevent flooding purposes.
        /// This should be used with at least a small delay.
        /// </summary>
        public bool Replace { get; set; } = false;
    }
}
