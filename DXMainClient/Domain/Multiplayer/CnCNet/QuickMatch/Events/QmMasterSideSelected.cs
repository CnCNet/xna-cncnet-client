using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Events;

public class QmMasterSideSelected : QmEvent
{
    public readonly QmSide Side;

    public QmMasterSideSelected(QmSide side)
    {
        Side = side;
    }
}