namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models.Events;

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