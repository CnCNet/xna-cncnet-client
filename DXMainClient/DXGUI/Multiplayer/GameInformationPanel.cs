﻿using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using DTAClient.Domain.Multiplayer;

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
        }

        XNALabel lblGameInformation;
        XNALabel lblGameMode;
        XNALabel lblMap;
        XNALabel lblGameVersion;
        XNALabel lblHost;
        XNALabel lblPing;
        XNALabel lblPlayers;
        XNALabel[] lblPlayerNames;

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, 235, 264);
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 255), 1, 1);
            DrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            lblGameInformation = new XNALabel(WindowManager);
            lblGameInformation.FontIndex = 1;
            lblGameInformation.Text = "GAME INFORMATION";

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

            lblPlayerNames = new XNALabel[MAX_PLAYERS];
            for (int i = 0; i < lblPlayerNames.Length / 2; i++)
            {
                XNALabel lblPlayerName1 = new XNALabel(WindowManager);
                lblPlayerName1.ClientRectangle = new Rectangle(lblPlayers.X, lblPlayers.Y + 24 + i * 20, 0, 0);
                lblPlayerName1.RemapColor = UISettings.AltColor;

                XNALabel lblPlayerName2 = new XNALabel(WindowManager);
                lblPlayerName2.ClientRectangle = new Rectangle(lblPlayers.X + 115, lblPlayerName1.Y, 0, 0);
                lblPlayerName2.RemapColor = UISettings.AltColor;

                AddChild(lblPlayerName1);
                AddChild(lblPlayerName2);

                lblPlayerNames[i] = lblPlayerName1;
                lblPlayerNames[(lblPlayerNames.Length / 2) + i] = lblPlayerName2;
            }

            AddChild(lblGameMode);
            AddChild(lblMap);
            AddChild(lblGameVersion);
            AddChild(lblHost);
            AddChild(lblPing);
            AddChild(lblPlayers);
            AddChild(lblGameInformation);

            lblGameInformation.CenterOnParent();
            lblGameInformation.ClientRectangle = new Rectangle( lblGameInformation.X, 6,
                lblGameInformation.Width, lblGameInformation.Height);

            base.Initialize();
        }

        public void SetInfo(GenericHostedGame game)
        {
            lblGameMode.Text = Renderer.GetStringWithLimitedWidth("Game mode: " + Renderer.GetSafeString(game.GameMode, lblGameMode.FontIndex),
                lblGameMode.FontIndex, Width - lblGameMode.X * 2);
            lblGameMode.Visible = true;
            lblMap.Text = Renderer.GetStringWithLimitedWidth("Map: " + Renderer.GetSafeString(game.Map, lblMap.FontIndex),
                lblMap.FontIndex, Width - lblMap.X * 2);
            lblMap.Visible = true;
            lblGameVersion.Text = "Game version: " + Renderer.GetSafeString(game.GameVersion, lblGameVersion.FontIndex);
            lblGameVersion.Visible = true;
            lblHost.Text = "Host: " + Renderer.GetSafeString(game.HostName, lblHost.FontIndex);
            lblHost.Visible = true;
            lblPing.Text = game.Ping > 0 ? "Ping: " + game.Ping.ToString() + " ms" : "Ping: Unknown";
            lblPing.Visible = true;
            lblPlayers.Visible = true;
            lblPlayers.Text = "Players (" + game.Players.Length + " / " + game.MaxPlayers + "):";

            for (int i = 0; i < game.Players.Length; i++)
            {
                lblPlayerNames[i].Visible = true;
                lblPlayerNames[i].Text = Renderer.GetSafeString(game.Players[i], lblPlayerNames[i].FontIndex);
            }

            for (int i = game.Players.Length; i < MAX_PLAYERS; i++)
            {
                lblPlayerNames[i].Visible = false;
            }
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
    }
}
