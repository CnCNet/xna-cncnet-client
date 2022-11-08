namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

public class QmReadyRequest : QmUpdateRequest
{

    public QmReadyRequest(int seed) : base (seed)
    {
        Status = "Ready";
    }
}