using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Responses;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Events;

public class QmResponseEvent : QmEvent
{
    public QmResponse<QmResponseMessage> Response { get; }

    public QmResponseEvent(QmResponse<QmResponseMessage> response)
    {
        Response = response;
    }
}