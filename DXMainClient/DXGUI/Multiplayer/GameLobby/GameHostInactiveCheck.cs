using System;
using System.Timers;
using ClientCore;
using ClientCore.Extensions;
using ClientGUI;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    public class GameHostInactiveCheck
    {
        private readonly WindowManager windowManager;
        private readonly Timer timer;
        private bool isWarningShown;
        private DateTime dttmStart;
        private static int WarningSeconds => ClientConfiguration.Instance.InactiveHostWarningMessageSeconds;
        private static int CloseSeconds => ClientConfiguration.Instance.InactiveHostKickSeconds;

        public event EventHandler CloseEvent;

        public GameHostInactiveCheck(WindowManager windowManager)
        {
            this.windowManager = windowManager;
            timer = new Timer();
            timer.AutoReset = true;
            timer.Interval = 1000;
            timer.Elapsed += TimerOnElapsed;
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            double secondsElapsed = (DateTime.UtcNow - dttmStart).TotalSeconds;

            if (secondsElapsed > WarningSeconds && !isWarningShown)
                ShowWarning();

            if (secondsElapsed > CloseSeconds)
                SendCloseEvent();
        }

        public void Start()
        {
            Reset();
            timer.Start();
        }

        public void Reset()
        {
            dttmStart = DateTime.UtcNow;
            isWarningShown = false;
        }

        public void Stop() => timer.Stop();

        private void SendCloseEvent()
        {
            Stop();
            CloseEvent?.Invoke(this, null);
        }

        private void ShowWarning()
        {
            isWarningShown = true;
            XNAMessageBox hostInactiveWarningMessageBox = new XNAMessageBox(
            windowManager,
                "Are you still here?".L10N("Client:Main:InactiveHostWarningTitle"),
                "Your game may be closed due to inactivity.".L10N("Client:Main:InactiveHostWarningText"),
                XNAMessageBoxButtons.OK
            );
            hostInactiveWarningMessageBox.OKClickedAction = box => Reset();
            hostInactiveWarningMessageBox.Show();
        }
    }
}
