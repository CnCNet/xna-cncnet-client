#pragma warning disable SA1310
namespace DTAClient.Domain.Multiplayer.CnCNet;

internal static class IRCCommands
{
    public const string JOIN = "JOIN";
    public const string QUIT = "QUIT";
    public const string NOTICE = "NOTICE";
    public const string PART = "PART";
    public const string PRIVMSG = "PRIVMSG";
    public const string MODE = "MODE";
    public const string KICK = "KICK";
    public const string ERROR = "ERROR";
    public const string PING = "PING";
    public const string PONG = "PONG";
    public const string TOPIC = "TOPIC";
    public const string NICK = "NICK";
    public const string PRIVMSG_ACTION = "ACTION";
    public const string PING_LAG = "PING LAG";
    public const string AWAY = "AWAY";
    public const string WHOIS = "WHOIS";
    public const string USER = "USER";
}