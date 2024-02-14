using ClientCore;
using Rampastring.Tools;
using System;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    class GameHostInactiveCheck
    {
        private int secondsElapsed;
        private bool inactiveGameWarningMessageSent = false;
        private bool closeInactiveGameEventSent = false;

        public event EventHandler SendInactiveGameWarningMessage;
        public event EventHandler CloseInactiveGame;

        public void Start()
        {
            secondsElapsed++;

            if (secondsElapsed > ClientConfiguration.Instance.InactiveHostWarningMessageSeconds &&
                !inactiveGameWarningMessageSent)
            {
                SendHostInactiveEvent();
            }

            if (secondsElapsed > ClientConfiguration.Instance.InactiveHostKickSeconds &&
                inactiveGameWarningMessageSent &&
                !closeInactiveGameEventSent)
            {
                SendCloseGameEvent();
            }
        }

        public void Reset()
        {
            secondsElapsed = 0;
            closeInactiveGameEventSent = false;
            inactiveGameWarningMessageSent = false;
        }

        private void SendCloseGameEvent()
        {
            CloseInactiveGame?.Invoke(this, null);
            closeInactiveGameEventSent = true;
        }

        private void SendHostInactiveEvent()
        {
            SendInactiveGameWarningMessage?.Invoke(this, null);
            inactiveGameWarningMessageSent = true;
            secondsElapsed = 0;
        }
    }
}
