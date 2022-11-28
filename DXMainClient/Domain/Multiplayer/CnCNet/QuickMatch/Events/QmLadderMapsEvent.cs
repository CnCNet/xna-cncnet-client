using System.Collections.Generic;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Events;

public class QmLadderMapsEvent : QmEvent
{
    public IEnumerable<QmLadderMap> LadderMaps { get; }

    public QmLadderMapsEvent(IEnumerable<QmLadderMap> qmLadderMaps)
    {
        LadderMaps = qmLadderMaps;
    }
}