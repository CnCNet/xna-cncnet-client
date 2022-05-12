namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models.Events;

public class QmLadderStatsEvent : QmEvent
{
    public QmLadderStats LadderStats { get; }

    public QmLadderStatsEvent(QmLadderStats ladderStats)
    {
        LadderStats = ladderStats;
    }
}