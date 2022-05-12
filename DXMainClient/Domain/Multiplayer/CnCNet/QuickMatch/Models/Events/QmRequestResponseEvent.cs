namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models.Events;

public class QmRequestResponseEvent : QmEvent
{
    public QmRequestResponse Response { get; }

    public QmRequestResponseEvent(QmRequestResponse response)
    {
        Response = response;
    }
}