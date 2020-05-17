using System;

namespace DTAClient.Online.EventArguments
{
    public class GameInvitationFailedEventArgs : EventArgs
    {
        public GameInvitationFailedEventArgs(string sender)
        {
            Sender = sender;
        }

        public string Sender { get; private set; }
    }
}
