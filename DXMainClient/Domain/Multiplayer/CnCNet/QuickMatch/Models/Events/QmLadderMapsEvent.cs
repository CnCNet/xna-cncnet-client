using System.Collections.Generic;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models.Events;

public class QmLadderMapsEvent : QmEvent
{
    public IEnumerable<QmLadderMap> LadderMaps { get; }

    public QmLadderMapsEvent(IEnumerable<QmLadderMap> qmLadderMaps)
    {
        LadderMaps = qmLadderMaps;
    }
}