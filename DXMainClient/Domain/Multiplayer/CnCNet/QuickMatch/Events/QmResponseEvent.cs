using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Responses;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Events;

public class QmResponseEvent : QmEvent
{
    public QmResponse Response { get; }

    public QmResponseEvent(QmResponse response)
    {
        Response = response;
    }
}