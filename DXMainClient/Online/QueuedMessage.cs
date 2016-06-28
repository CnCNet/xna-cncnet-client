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
        }

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
    }
}
