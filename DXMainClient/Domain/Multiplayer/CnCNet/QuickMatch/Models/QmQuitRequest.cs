namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

public class QmQuitRequest : QmRequest
{
    public QmQuitRequest()
    {
        Type = QmRequestTypes.Quit;
    }
}