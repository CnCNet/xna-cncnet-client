namespace DTAClient.Online
{
    /// <summary>
    /// The type of a CnCNet IRC network message.
    /// </summary>
    public enum QueuedMessageType
    {
        UNDEFINED,
        CHAT_MESSAGE,
        SYSTEM_MESSAGE,
        GAME_SETTINGS_MESSAGE,
        GAME_PLAYERS_MESSAGE,
        GAME_PLAYERS_READY_STATUS_MESSAGE,
        GAME_LOCKED_MESSAGE,
        GAME_GET_READY_MESSAGE,
        GAME_NOTIFICATION_MESSAGE,
        GAME_HOSTING_MESSAGE,
        GAME_CHEATER_MESSAGE,
        GAME_BROADCASTING_MESSAGE,
        WHOIS_MESSAGE,
        INSTANT_MESSAGE,
        GAME_PLAYERS_EXTRA_MESSAGE,
    }
}
