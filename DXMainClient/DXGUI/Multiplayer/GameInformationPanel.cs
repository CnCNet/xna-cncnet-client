using System.Linq;
using DTAClient.DXGUI.Generic;
using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using DTAClient.Domain.Multiplayer;
using Localization;
using Microsoft.Xna.Framework.Graphics;

namespace DTAClient.DXGUI.Multiplayer
{
    /// <summary>
    /// A UI panel that displays information about a hosted CnCNet or LAN game.
    /// </summary>
    public class GameInformationPanel : XNAPanel
    {
        private const int MAX_PLAYERS = 8;

        public GameInformationPanel(WindowManager windowManager) : base(windowManager)
        {
            DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
        }

        private XNALabel lblGameInformation;
        private XNALabel lblGameMode;
        private XNALabel lblMap;
        private XNALabel lblGameVersion;
        private XNALabel lblHost;
        private XNALabel lblPing;
        private XNALabel lblPlayers;
        private XNALabel[] lblPlayerNames;
        private XNAPanel pnlIconLegend;

        private const int BaseHeight = 256;

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, 235, BaseHeight);
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 255), 1, 1);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            lblGameInformation = new XNALabel(WindowManager);
            lblGameInformation.FontIndex = 1;
            lblGameInformation.Text = "GAME INFORMATION".L10N("UI:Main:GameInfo");

            lblGameMode = new XNALabel(WindowManager);
            lblGameMode.ClientRectangle = new Rectangle(6, 30, 0, 0);

            lblMap = new XNALabel(WindowManager);
            lblMap.ClientRectangle = new Rectangle(6, 54, 0, 0);

            lblGameVersion = new XNALabel(WindowManager);
            lblGameVersion.ClientRectangle = new Rectangle(6, 78, 0, 0);

            lblHost = new XNALabel(WindowManager);
            lblHost.ClientRectangle = new Rectangle(6, 102, 0, 0);

            lblPing = new XNALabel(WindowManager);
            lblPing.ClientRectangle = new Rectangle(6, 126, 0, 0);

            lblPlayers = new XNALabel(WindowManager);
            lblPlayers.ClientRectangle = new Rectangle(6, 150, 0, 0);

            var playerNamePanel = new XNAPanel(WindowManager);
            playerNamePanel.ClientRectangle = new Rectangle(lblPlayers.X, lblPlayers.Y + 20, Width, 80);
            playerNamePanel.DrawBorders = false;

            lblPlayerNames = new XNALabel[MAX_PLAYERS];
            for (int i = 0; i < lblPlayerNames.Length / 2; i++)
            {
                int y = i * 20;
                XNALabel lblPlayerName1 = new XNALabel(WindowManager);
                lblPlayerName1.ClientRectangle = new Rectangle(0, y, 0, 20);
                lblPlayerName1.RemapColor = UISettings.ActiveSettings.AltColor;

                XNALabel lblPlayerName2 = new XNALabel(WindowManager);
                lblPlayerName2.ClientRectangle = new Rectangle( 115, y, 0, 20);
                lblPlayerName2.RemapColor = UISettings.ActiveSettings.AltColor;

                playerNamePanel.AddChild(lblPlayerName1);
                playerNamePanel.AddChild(lblPlayerName2);

                lblPlayerNames[i] = lblPlayerName1;
                lblPlayerNames[(lblPlayerNames.Length / 2) + i] = lblPlayerName2;
            }

            pnlIconLegend = new XNAPanel(WindowManager);
            pnlIconLegend.ClientRectangle = new Rectangle(0, playerNamePanel.Bottom + 2, Width, 0);
            pnlIconLegend.DrawBorders = false;

            AddChild(lblGameMode);
            AddChild(lblMap);
            AddChild(lblGameVersion);
            AddChild(lblHost);
            AddChild(lblPing);
            AddChild(lblPlayers);
            AddChild(lblGameInformation);
            AddChild(CreateDivider(lblPlayers.Y - 2));
            AddChild(playerNamePanel);
            AddChild(pnlIconLegend);

            lblGameInformation.CenterOnParent();
            lblGameInformation.ClientRectangle = new Rectangle( lblGameInformation.X, 6,
                lblGameInformation.Width, lblGameInformation.Height);

            base.Initialize();
        }

        public void SetInfo(GenericHostedGame game)
        {
            lblGameMode.Text = Renderer.GetStringWithLimitedWidth("Game mode:".L10N("UI:Main:GameInfoGameMode") + " " + Renderer.GetSafeString(game.GameMode, lblGameMode.FontIndex),
                lblGameMode.FontIndex, Width - lblGameMode.X * 2);
            lblGameMode.Visible = true;
            lblMap.Text = Renderer.GetStringWithLimitedWidth("Map:".L10N("UI:Main:GameInfoMap") + " " + Renderer.GetSafeString(game.Map, lblMap.FontIndex),
                lblMap.FontIndex, Width - lblMap.X * 2);
            lblMap.Visible = true;
            lblGameVersion.Text = "Game version:".L10N("UI:Main:GameInfoGameVersion")+ " " + Renderer.GetSafeString(game.GameVersion, lblGameVersion.FontIndex);
            lblGameVersion.Visible = true;
            lblHost.Text = "Host:".L10N("UI:Main:GameInfoHost") + " " + Renderer.GetSafeString(game.HostName, lblHost.FontIndex);
            lblHost.Visible = true;
            lblPing.Text = game.Ping > 0 ? "Ping:".L10N("UI:Main:GameInfoPing") + " " + game.Ping.ToString() + " ms" : "Ping: Unknown".L10N("UI:Main:GameInfoPingUnknown");
            lblPing.Visible = true;
            lblPlayers.Visible = true;
            lblPlayers.Text = "Players".L10N("UI:Main:GameInfoPlayers") + " (" + game.Players.Length + " / " + game.MaxPlayers + "):";

            for (int i = 0; i < game.Players.Length && i < MAX_PLAYERS; i++)
            {
                lblPlayerNames[i].Visible = true;
                lblPlayerNames[i].Text = Renderer.GetSafeString(game.Players[i], lblPlayerNames[i].FontIndex);
            }

            for (int i = game.Players.Length; i < MAX_PLAYERS; i++)
            {
                lblPlayerNames[i].Visible = false;
            }

            SetLegendInfo(game);
        }

        private void SetLegendInfo(GenericHostedGame game)
        {
            ClearLegendIconPanel();
            
            pnlIconLegend.AddChild(CreateDivider(0));
            
            if (game.Locked)
                AddLegendIcon(AssetTextures.LockedGame, "Game is locked".L10N("UI:GameInformationPanel:LockedGame"));
            if(game.Passworded)
                AddLegendIcon(AssetTextures.PasswordedGame, "Game is passworded".L10N("UI:GameInformationPanel:PasswordedGame"));
            if(game.Incompatible)
                AddLegendIcon(AssetTextures.IncompatibleGame, "Incompatible client version".L10N("UI:GameInformationPanel:IncompatibleGame"));

            pnlIconLegend.Visible = pnlIconLegend.Children.Count > 1;
            Height = BaseHeight;
            if (pnlIconLegend.Visible)
                Height += pnlIconLegend.Height;
        }

        private XNAPanel CreateDivider(int y)
        {
            var dividerPanel = new XNAPanel(WindowManager);
            dividerPanel.DrawBorders = true;
            dividerPanel.ClientRectangle = new Rectangle(0, y, Width, 1);
            return dividerPanel;
        }

        private void AddLegendIcon(Texture2D icon, string label)
        {
            var lastChild = pnlIconLegend.Children.LastOrDefault();
            var iconPanel = new GameInformationIconPanel(WindowManager, icon, label);
            int y = lastChild?.Bottom ?? 0;
            const int height = 18;
            iconPanel.ClientRectangle = new Rectangle(0, y, pnlIconLegend.Width, height);
            pnlIconLegend.AddChild(iconPanel);

            pnlIconLegend.Height = (pnlIconLegend.Children.Count - 1) * height;
        }

        private void ClearLegendIconPanel()
        {
            foreach (XNAControl xnaControl in pnlIconLegend.Children.ToList())
                pnlIconLegend.RemoveChild(xnaControl);
        }

        public void ClearInfo()
        {
            lblGameMode.Visible = false;
            lblMap.Visible = false;
            lblGameVersion.Visible = false;
            lblHost.Visible = false;
            lblPing.Visible = false;
            lblPlayers.Visible = false;

            foreach (XNALabel label in lblPlayerNames)
                label.Visible = false;
        }

        public override void Draw(GameTime gameTime)
        {
            if (Alpha > 0.0f)
                base.Draw(gameTime);
        }
    }
}
