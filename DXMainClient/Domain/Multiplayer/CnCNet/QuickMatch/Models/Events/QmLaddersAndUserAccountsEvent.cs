using System.Collections.Generic;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models.Events;

public class QmLaddersAndUserAccountsEvent : QmEvent
{
    public IEnumerable<QmLadder> Ladders { get; }

    public IEnumerable<QmUserAccount> UserAccounts { get; }

    public QmLaddersAndUserAccountsEvent(IEnumerable<QmLadder> ladders, IEnumerable<QmUserAccount> userAccounts)
    {
        Ladders = ladders;
        UserAccounts = userAccounts;
    }
}