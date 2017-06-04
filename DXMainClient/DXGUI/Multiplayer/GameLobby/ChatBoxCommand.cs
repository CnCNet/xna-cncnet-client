using System;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// A command that can be executed by typing a message starting with / on
    /// a multiplayer game lobby's chat box.
    /// </summary>
    public class ChatBoxCommand
    {
        public ChatBoxCommand(string command, string description, bool hostOnly, Action<string> action)
        {
            Command = command;
            Description = description;
            HostOnly = hostOnly;
            Action = action;
        }

        public string Command { get; private set; }
        public string Description { get; private set; }
        public bool HostOnly { get; private set; }
        public Action<string> Action { get; private set; }
    }
}
