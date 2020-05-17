using System;

namespace DTAClient.Online.EventArguments
{
    public class GameInvitationEventArgs : EventArgs
    {
        public GameInvitationEventArgs(string sender, string roomName, string password)
        {
            Sender = sender;
            RoomName = roomName;
            Password = password;
        }

        public string Sender { get; private set; }

        public string RoomName { get; private set; }

        public string Password { get; private set; }
    }
}
