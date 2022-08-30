using ClientCore;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    class GameHostInactiveCheck
    {
        private int secondsElapsed;
        private bool hasPromptedInactiveMessage = false;
        private bool hasPromptedRemoval = false;

        public event EventHandler PromptInactiveMessage;
        public event EventHandler PromptRemoval;
        public void CheckHostIsActive()
        {
            secondsElapsed++;

            Logger.Log("GameHostInactiveCheck ** Checking");

            if (secondsElapsed > ClientConfiguration.Instance.InactiveHostMessagePromptSeconds
                && !hasPromptedInactiveMessage)
            {
                PromptInactiveMessageEvent();
            }
            else if (secondsElapsed > ClientConfiguration.Instance.InactiveHostKickSeconds
                && !hasPromptedRemoval)
            {
                PromptRemovalEvent();
            }
        }

        public void HostIsActive()
        {
            ResetTimer();
        }

        private void ResetTimer()
        {
            secondsElapsed = 0;
            hasPromptedRemoval = false;
            hasPromptedInactiveMessage = false;
        }

        private void PromptRemovalEvent()
        {
            PromptRemoval(this, null);
            hasPromptedRemoval = true;
        }

        private void PromptInactiveMessageEvent()
        {
            PromptInactiveMessage(this, null);
            hasPromptedInactiveMessage = true;
        }
    }
}
