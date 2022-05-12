namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models.Events;

public class QmUserAccountSelectedEvent : QmEvent
{
    public QmUserAccount UserAccount { get; }

    public QmUserAccountSelectedEvent(QmUserAccount userAccount)
    {
        UserAccount = userAccount;
    }
}