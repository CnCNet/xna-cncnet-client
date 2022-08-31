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

        public void CheckHostIsActive()
        {
            secondsElapsed++;

            if (secondsElapsed > ClientConfiguration.Instance.InactiveHostWarningMessageSeconds && !inactiveGameWarningMessageSent)
            {
                SendHostInactiveEvent();
            }

            if (secondsElapsed > ClientConfiguration.Instance.InactiveHostKickSeconds && inactiveGameWarningMessageSent
                && !closeInactiveGameEventSent)
            {
                SendCloseGameEvent();
            }
        }

        public void HostIsAlive()
        {
            ResetTimer();
        }

        private void ResetTimer()
        {
            secondsElapsed = 0;
            closeInactiveGameEventSent = false;
            inactiveGameWarningMessageSent = false;
        }

        private void SendCloseGameEvent()
        {
            CloseInactiveGame(this, null);
            closeInactiveGameEventSent = true;
        }

        private void SendHostInactiveEvent()
        {
            SendInactiveGameWarningMessage(this, null);
            inactiveGameWarningMessageSent = true;
            secondsElapsed = 0;
        }
    }
}