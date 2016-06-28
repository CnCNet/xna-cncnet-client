using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientCore.CnCNet5
{
    /// <summary>
    /// The type of a network message.
    /// </summary>
    public enum QueuedMessageType
    {
        UNDEFINED,
        CHAT_MESSAGE,
        GAME_SETTINGS_MESSAGE,
        GAME_PLAYERS_MESSAGE,
        GAME_BROADCASTING_MESSAGE,
        GAME_PLAYERS_READY_STATUS_MESSAGE,
        GAME_LOCKED_MESSAGE,
        SYSTEM_MESSAGE,
        WHOIS_MESSAGE,
    }
}
