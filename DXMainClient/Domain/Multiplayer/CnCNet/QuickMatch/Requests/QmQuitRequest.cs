using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Utilities;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Requests;

public class QmQuitRequest : QmRequest
{
    public QmQuitRequest()
    {
        Type = QmRequestTypes.Quit;
    }
}