using Localization;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch;

public static class QmStrings
{
    // Error Messages
    public static string TokenExpiredError => "QuickMatch token is expired".L10N("QM:Error:TokenExpired");

    public static string NoLaddersFoundError => "No quick match ladders currently found.".L10N("QM:Error:NoLaddersFound");

    public static string NoUserAccountsFoundError => "No user accounts found in quick match. Are you registered for this month?".L10N("QM:Error:NoUserAccountsFound");

    public static string LoadingLadderStatsError => "Error loading ladder stats".L10N("QM:Error:LoadingLadderStats");

    public static string LoadingLadderMapsError => "Error loading ladder maps".L10N("QM:Error:LoadingLadderMaps");

    public static string ServerUnreachableError => "Server unreachable".L10N("QM:Error:ServerUnreachable");

    public static string InvalidUsernamePasswordError => "Invalid username/password".L10N("QM:Error:InvalidUsernamePassword");

    public static string NoSideSelectedError => "No side selected".L10N("QM:Error:NoSideSelected");

    public static string NoLadderSelectedError => "No ladder selected".L10N("QM:Error:NoLadderSelected");

    public static string UnknownError => "Unknown error occurred".L10N("QM:Error:Unknown");

    public static string LoggingInUnknownError => "Error logging in".L10N("QM:Error:LoggingInUnknown");

    public static string CancelingMatchRequestError => "Error canceling match request".L10N("QM:Error:CancelingMatchRequest");

    public static string RequestingMatchUnknownError => "Error requesting match".L10N("QM:Error:RequestingMatchUnknown");

    public static string LoadingLaddersAndAccountsUnknownError => "Error loading ladders and accounts...".L10N("QM:Error:LoadingLaddersAndAccountsUnknown");

    // Error Messages Formatted
    public static string LoadingLadderMapsErrorFormat => "Error loading ladder maps: {0}".L10N("QM:Error:LoadingLadderMapsFormat");

    public static string LoadingLadderStatsErrorFormat => "Error loading ladder stats: {0}".L10N("QM:Error:LoadingLadderStatsFormat");

    public static string LoadingUserAccountsErrorFormat => "Error loading user accounts: {0}".L10N("QM:Error:LoadingUserAccountsFormat");

    public static string LoadingLaddersErrorFormat => "Error loading ladders: {0}".L10N("QM:Error:LoadingLaddersFormat");

    public static string LoggingInUnknownErrorFormat => "Error logging in: {0}, {1}".L10N("QM:Error:LoggingInUnknownFormat");

    public static string RequestingMatchErrorFormat => "Error requesting match: {0}".L10N("QM:Error:RequestingMatchFormat");

    public static string UnableToCreateMatchRequestDataError => "Unable to create match request data".L10N("QM:Error:UnableToCreateMatchRequestDataError");

    // UI Messages
    public static string GenericErrorTitle => "Error".L10N("QM:UI:GenericErrorTitle");

    public static string LogoutConfirmation => "Are you sure you want to log out?".L10N("QM:UI:LogoutConfirmation");

    public static string ConfirmationCaption => "Confirmation".L10N("QM:UI:ConfirmationCaption");

    public static string RequestingMatchStatus => "Requesting match".L10N("QM:UI:RequestingMatchStatus");

    public static string CancelingMatchRequestStatus => "Canceling match request".L10N("QM:UI:CancelingMatchRequest");

    public static string LoadingStats => "Loading stats...".L10N("QM:UI:LoadingStats");

    public static string LoadingLaddersAndAccountsStatus => "Loading ladders and accounts".L10N("QM:UI:LoadingLaddersAndAccountsStatus");

    public static string LoadingLadderMapsStatus => "Loading ladder maps".L10N("QM:UI:LoadingLadderMapsStatus");

    public static string LoggingInStatus => "Logging in".L10N("QM:UI:LoggingInStatus");

    public static string RandomSideName => "Random".L10N("QM:UI:RandomSideName");

    public static string MatchupFoundConfirmMsg => "Matchup found! Are you ready?".L10N("QM:UI:MatchupFoundConfirm:Msg");

    public static string MatchupFoundConfirmYes => "I'm Ready".L10N("QM:UI:MatchupFoundConfirm:Yes");

    public static string MatchupFoundConfirmNo => "Cancel".L10N("QM:UI:MatchupFoundConfirm:No");
}