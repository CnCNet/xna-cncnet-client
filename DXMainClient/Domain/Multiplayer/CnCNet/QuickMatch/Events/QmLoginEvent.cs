namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Events;

public class QmLoginEvent : QmEvent
{
    public string ErrorMessage { get; set; }
}