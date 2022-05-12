namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models.Events;

public class QmMasterSideSelected : QmEvent
{
    public readonly QmSide Side;

    public QmMasterSideSelected(QmSide side)
    {
        Side = side;
    }
}