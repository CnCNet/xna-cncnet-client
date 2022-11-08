namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

public class QmNotReadyRequest : QmUpdateRequest
{
    public QmNotReadyRequest(int seed) : base(seed)
    {
        Status = "NotReady";
    }
}