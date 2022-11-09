namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Requests;

public class QmNotReadyRequest : QmUpdateRequest
{
    public QmNotReadyRequest(int seed) : base(seed)
    {
        Status = "NotReady";
    }
}