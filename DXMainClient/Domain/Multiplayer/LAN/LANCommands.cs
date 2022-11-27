#pragma warning disable SA1310
namespace DTAClient.Domain.Multiplayer.LAN;

internal static class LANCommands
{
    public const string PLAYER_READY_REQUEST = "READY";
    public const string CHAT_GAME_LOADING_COMMAND = "CHAT";
    public const string CHAT_LOBBY_COMMAND = "GLCHAT";
    public const string RETURN = "RETURN";
    public const string GET_READY = "GETREADY";
    public const string PLAYER_OPTIONS_REQUEST = "POREQ";
    public const string PLAYER_OPTIONS_BROADCAST = "POPTS";
    public const string PLAYER_JOIN = "JOIN";
    public const string PLAYER_QUIT_COMMAND = "QUIT";
    public const string GAME_OPTIONS = "OPTS";
    public const string LAUNCH_GAME = "LAUNCH";
    public const string FILE_HASH = "FHASH";
    public const string DICE_ROLL = "DR";
    public const string PING = "PING";
    public const string OPTIONS = "OPTS";
    public const string PLAYER_EXTRA_OPTIONS = "PEOPTS";
    public const string READY_STATUS = "READY";
    public const string GAME_START = "START";
    public const string CHAT = "CHAT";
    public const string ALIVE = "ALIVE";
    public const string QUIT = "QUIT";
    public const string GAME = "GAME";
}