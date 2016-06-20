using Rampastring.XNAUI.DXControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using DTAClient.domain.CnCNet;

namespace DTAClient.DXGUI.Multiplayer
{
    /// <summary>
    /// A UI panel that displays information about a hosted CnCNet or LAN game.
    /// </summary>
    public class GameInformationPanel : XNAPanel
    {
        public GameInformationPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        XNALabel lblGameInformation;
        XNALabel lblGameMode;
        XNALabel lblMap;
        XNALabel lblGameVersion;
        XNALabel lblHost;
        XNALabel lblPlayers;
        XNALabel[] lblPlayerNames;

        public override void Initialize()
        {
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

            lblPlayers = new XNALabel(WindowManager);
            lblPlayers.ClientRectangle = new Rectangle(6, 126, 0, 0);

            lblPlayerNames = new XNALabel[8];
            for (int i = 0; i < lblPlayerNames.Length / 2; i++)
            {
                XNALabel lblPlayerName1 = new XNALabel(WindowManager);
                lblPlayerName1.ClientRectangle = new Rectangle(lblPlayers.ClientRectangle.X, lblPlayers.ClientRectangle.Y + 24 + i * 20, 0, 0);
                lblPlayerName1.RemapColor = UISettings.AltColor;

                XNALabel lblPlayerName2 = new XNALabel(WindowManager);
                lblPlayerName2.ClientRectangle = new Rectangle(lblPlayers.ClientRectangle.X + 115, lblPlayerName1.ClientRectangle.Y, 0, 0);
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
            AddChild(lblPlayers);
            AddChild(lblGameInformation);

            lblGameInformation.CenterOnParent();
            lblGameInformation.ClientRectangle = new Rectangle( lblGameInformation.ClientRectangle.X, 6,
                lblGameInformation.ClientRectangle.Width, lblGameInformation.ClientRectangle.Height);

            base.Initialize();
        }

        public void SetInfo(HostedGame game)
        {
            lblGameMode.Text = "Game mode: " + game.GameMode;
            lblGameMode.Visible = true;
            lblMap.Text = "Map: " + game.MapName;
            lblMap.Visible = true;
            lblGameVersion.Text = "Game version: " + game.Version;
            lblGameVersion.Visible = true;
            lblHost.Text = "Host: " + game.Admin;
            lblHost.Visible = true;
            lblPlayers.Visible = true;
            lblPlayers.Text = "Players (" + game.Players.Count + " / " + game.MaxPlayers + "):";

            for (int i = 0; i < game.Players.Count; i++)
            {
                lblPlayerNames[i].Visible = true;
                lblPlayerNames[i].Text = game.Players[i];
            }
        }

        public void ClearInfo()
        {
            lblGameMode.Visible = false;
            lblMap.Visible = false;
            lblGameVersion.Visible = false;
            lblHost.Visible = false;
            lblPlayers.Visible = false;

            foreach (XNALabel label in lblPlayerNames)
                label.Visible = false;
        }
    }
}
