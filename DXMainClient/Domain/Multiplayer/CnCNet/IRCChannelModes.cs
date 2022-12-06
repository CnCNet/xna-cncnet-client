#pragma warning disable SA1310
namespace DTAClient.Domain.Multiplayer.CnCNet;

internal static class IRCChannelModes
{
    public const char BAN = 'b';
    public const char INVITE_ONLY = 'i';
    public const char CHANNEL_KEY = 'k';
    public const char CHANNEL_LIMIT = 'l';
    public const char NO_EXTERNAL_MESSAGES = 'n';
    public const char NO_NICKNAME_CHANGE = 'N';
    public const char SECRET_CHANNEL = 's';
    public static string DEFAULT = $"{CHANNEL_KEY}{CHANNEL_LIMIT}{NO_EXTERNAL_MESSAGES}{NO_NICKNAME_CHANGE}{SECRET_CHANNEL}";
}