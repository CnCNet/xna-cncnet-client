using System.Collections.Generic;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Events;

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