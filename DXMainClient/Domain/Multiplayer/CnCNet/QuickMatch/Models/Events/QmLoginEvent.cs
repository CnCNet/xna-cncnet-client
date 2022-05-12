namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models.Events;

public class QmLoginEvent : QmEvent
{
    public string ErrorMessage { get; set; }
}