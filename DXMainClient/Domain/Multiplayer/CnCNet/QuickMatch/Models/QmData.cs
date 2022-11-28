using System.Collections.Generic;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

public class QmData
{
    public List<QmLadder> Ladders { get; set; }
        
    public List<QmUserAccount> UserAccounts { get; set; }
}