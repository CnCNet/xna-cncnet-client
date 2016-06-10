using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.Online
{
    /// <summary>
    /// The type of a CnCNet IRC network message.
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
        GAME_GET_READY_MESSAGE,
        GAME_NOTIFICATION_MESSAGE,
        GAME_HOSTING_MESSAGE,
        SYSTEM_MESSAGE,
        WHOIS_MESSAGE,
    }
}
