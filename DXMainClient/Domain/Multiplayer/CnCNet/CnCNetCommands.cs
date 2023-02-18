#pragma warning disable SA1310
namespace DTAClient.Domain.Multiplayer.CnCNet;

internal static class CnCNetCommands
{
    public const string GAME_INVITE = "INVITE";
    public const string GAME_INVITATION_FAILED = "INVITATION_FAILED";
    public const string NOT_ALL_PLAYERS_PRESENT = "NPRSNT";
    public const string GET_READY = "GTRDY";
    public const string FILE_HASH = "FHSH";
    public const string INVALID_FILE_HASH = "IHSH";
    public const string TUNNEL_PING = "TNLPNG";
    public const string OPTIONS = "OP";
    public const string INVALID_SAVED_GAME_INDEX = "ISGI";
    public const string START_GAME = "START";
    public const string PLAYER_READY = "READY";
    public const string CHANGE_TUNNEL_SERVER = "CHTNL";
    public const string RETURN = "RETURN";
    public const string GET_READY_LOBBY = "GETREADY";
    public const string PLAYER_EXTRA_OPTIONS = "PEO";
    public const string MAP_SHARING_FAIL = "MAPFAIL";
    public const string MAP_SHARING_DOWNLOAD = "MAPOK";
    public const string MAP_SHARING_UPLOAD = "MAPREQ";
    public const string MAP_SHARING_DISABLED = "MAPSDISABLED";
    public const string CHEAT_DETECTED = "CD";
    public const string DICE_ROLL = "DR";
    public const string GAME_START_V3 = "STARTV3";
    public const string TUNNEL_CONNECTION_OK = "TNLOK";
    public const string TUNNEL_CONNECTION_FAIL = "TNLFAIL";
    public const string GAME_START_V2 = "START";
    public const string OPTIONS_REQUEST = "OR";
    public const string READY_REQUEST = "R";
    public const string PLAYER_OPTIONS = "PO";
    public const string GAME_OPTIONS = "GO";
    public const string AI_SPECTATORS = "AISPECS";
    public const string INSUFFICIENT_PLAYERS = "INSFSPLRS";
    public const string TOO_MANY_PLAYERS = "TMPLRS";
    public const string SHARED_COLORS = "CLRS";
    public const string SHARED_STARTING_LOCATIONS = "SLOC";
    public const string LOCK_GAME = "LCKGME";
    public const string NOT_VERIFIED = "NVRFY";
    public const string STILL_IN_GAME = "INGM";
    public const string CHEATER = "MM";
    public const string GAME = "GAME";
    public const string PLAYER_TUNNEL_PINGS = "TNLPINGS";
    public const string PLAYER_P2P_REQUEST = "P2PREQ";
    public const string PLAYER_P2P_PINGS = "P2PPINGS";
    public const string UPDATE = "UPDATE";
}