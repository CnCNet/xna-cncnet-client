using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Utilities;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Events;

public class QmErrorMessageEvent : QmEvent
{
    public string ErrorTitle { get; }

    public string ErrorMessage { get; }

    public QmErrorMessageEvent(string errorMessage, string errorTitle = null)
    {
        ErrorMessage = errorMessage;
        ErrorTitle = errorTitle ?? QmStrings.GenericErrorTitle;
    }
}