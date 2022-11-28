namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Requests;

public class QmReadyRequest : QmUpdateRequest
{

    public QmReadyRequest(int seed) : base (seed)
    {
        Status = "Ready";
    }
}