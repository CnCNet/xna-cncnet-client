using System;
using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Events;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Services;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.QuickMatch
{
    public class QuickMatchWindow : INItializableWindow
    {
        private readonly QmService qmService;
        private readonly QmSettingsService qmSettingsService;

        private QuickMatchLoginPanel loginPanel;

        private QuickMatchLobbyPanel lobbyPanel;

        private XNAPanel headerGameLogo;

        public QuickMatchWindow(WindowManager windowManager) : base(windowManager)
        {
            qmService = QmService.GetInstance();
            qmService.QmEvent += HandleQmEvent;
            qmSettingsService = QmSettingsService.GetInstance();
        }

        public override void Initialize()
        {
            Name = nameof(QuickMatchWindow);
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 255), 1, 1);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            base.Initialize();

            loginPanel = FindChild<QuickMatchLoginPanel>(nameof(loginPanel));
            loginPanel.Exit += (sender, args) => Disable();

            lobbyPanel = FindChild<QuickMatchLobbyPanel>(nameof(lobbyPanel));
            lobbyPanel.Exit += (sender, args) => Disable();

            headerGameLogo = FindChild<XNAPanel>(nameof(headerGameLogo));

            WindowManager.CenterControlOnScreen(this);

            EnabledChanged += EnabledChangedEvent;
        }

        private void HandleQmEvent(object sender, QmEvent qmEvent)
        {
            switch (qmEvent)
            {
                case QmUserAccountSelectedEvent e:
                    HandleUserAccountSelected(e.UserAccount);
                    return;
                case QmErrorMessageEvent e:
                    HandleErrorMessageEvent(e);
                    return;
            }
        }

        private void HandleErrorMessageEvent(QmErrorMessageEvent e)
            => XNAMessageBox.Show(WindowManager, e.ErrorTitle, e.ErrorMessage);

        private void HandleUserAccountSelected(QmUserAccount userAccount)
        {
            headerGameLogo.BackgroundTexture = qmSettingsService.GetSettings().GetLadderHeaderLogo(userAccount.Ladder.Abbreviation);
            if (headerGameLogo.BackgroundTexture == null)
                return;

            // Resize image to ensure proper ratio and spacing from right edge
            float imageRatio = (float)headerGameLogo.BackgroundTexture.Width / headerGameLogo.BackgroundTexture.Height;
            int newImageWidth = (int)imageRatio * headerGameLogo.Height;
            headerGameLogo.ClientRectangle = new Rectangle(headerGameLogo.Parent.Right - newImageWidth - headerGameLogo.Parent.X, headerGameLogo.Y, newImageWidth, headerGameLogo.Height);
        }

        private void EnabledChangedEvent(object sender, EventArgs e)
        {
            if (!Enabled)
            {
                loginPanel.Disable();
                lobbyPanel.Disable();
                return;
            }

            loginPanel.Enable();
        }
    }
}