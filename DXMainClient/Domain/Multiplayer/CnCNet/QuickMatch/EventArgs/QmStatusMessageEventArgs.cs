using System;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch
{
    public class QmStatusMessageEventArgs : EventArgs
    {
        public string Message { get; }

        public Action CancelAction { get; }

        public QmStatusMessageEventArgs(string message, Action cancelAction = null)
        {
            Message = message;
            CancelAction = cancelAction;
        }
    }
}
