using System;
using System.Timers;
using ClientCore;
using ClientGUI;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    public class GameHostInactiveCheck
    {
        private readonly WindowManager windowManager;
        private readonly Timer timer;
        private bool warningShown;
        private DateTime startDttm;
        private static int WarningSeconds => ClientConfiguration.Instance.InactiveHostWarningMessageSeconds;
        private static int CloseSeconds => WarningSeconds + ClientConfiguration.Instance.InactiveHostKickSeconds;

        public event EventHandler CloseEvent;

        public GameHostInactiveCheck(WindowManager windowManager)
        {
            this.windowManager = windowManager;
            timer = CreateTimer();
        }


        private Timer CreateTimer()
        {
            var _timer = new Timer();
            _timer.AutoReset = true;
            _timer.Interval = 1000;
            _timer.Elapsed += TimerOnElapsed;
            return _timer;
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            double secondsElapsed = (DateTime.UtcNow - startDttm).TotalSeconds;

            if (secondsElapsed > WarningSeconds && !warningShown)
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
            startDttm = DateTime.UtcNow;
            warningShown = false;
        }

        public void Stop() => timer.Stop();

        private void SendCloseEvent()
        {
            Stop();
            CloseEvent?.Invoke(this, null);
        }

        private void ShowWarning()
        {
            warningShown = true;
            XNAMessageBox hostInactiveWarningMessageBox = new XNAMessageBox(
                windowManager,
                ClientConfiguration.Instance.InactiveHostWarningTitle,
                ClientConfiguration.Instance.InactiveHostWarningMessage,
                XNAMessageBoxButtons.OK
            );
            hostInactiveWarningMessageBox.OKClickedAction = box => Reset();
            hostInactiveWarningMessageBox.Show();
        }
    }
}
