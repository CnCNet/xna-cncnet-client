using System;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Events;

public class QmRequestingMatchEvent : QmEvent, IQmOverlayStatusEvent
{
    public Tuple<string, Action> CancelAction { get; }

    public QmRequestingMatchEvent(Action cancelAction)
    {
        CancelAction = new Tuple<string, Action>("Cancel", cancelAction);
    }
}