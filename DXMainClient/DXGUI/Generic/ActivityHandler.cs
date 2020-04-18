using ClientCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    interface IActivityHandler
    {
        void Start();
        void Reset();
        void Dispose();
        event EventHandler PromptInactiveMessage;
        event EventHandler PromptRemoval;
    }

    public class ActivityHandler : IActivityHandler
    {
        private Timer activityTimer;
        private int timeElapsed;

        public event EventHandler PromptInactiveMessage;
        public event EventHandler PromptRemoval;

        private bool hasPromptedInactiveMessage = false;
        private bool hasPromptedRemoval = false;

        public ActivityHandler()
        {
        }

        public void Start()
        {
            StartTimer();
        }

        public void Reset()
        {
            ResetTimer();
        }

        public void Dispose()
        {
            RemoveTimer();
        }

        private void StartTimer()
        {
            activityTimer = new Timer(1000);
            activityTimer.AutoReset = true;
            activityTimer.Elapsed += onActivityTimerElapsed;
            activityTimer.Enabled = true;
        }

        private void ResetTimer()
        {
            timeElapsed = 0;
            hasPromptedRemoval = false;
            hasPromptedInactiveMessage = false;
        }

        private void RemoveTimer()
        {
            ResetTimer();
            activityTimer.Elapsed -= onActivityTimerElapsed;
            activityTimer.Dispose();
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

        private void onActivityTimerElapsed(object sender, ElapsedEventArgs e)
        {
            timeElapsed++;

            if (timeElapsed > ClientConfiguration.Instance.InactiveHostMessagePromptSeconds 
                && !hasPromptedInactiveMessage)
            {
                PromptInactiveMessageEvent();
            }
            else if (timeElapsed > ClientConfiguration.Instance.InactiveHostKickSeconds 
                && !hasPromptedRemoval)
            {
                PromptRemovalEvent();
            }
        }
    }
}
