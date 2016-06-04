using Rampastring.XNAUI.DXControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using ClientCore.CnCNet5;
using Microsoft.Xna.Framework;

namespace DTAClient.DXGUI
{
    /// <summary>
    /// A UI panel that displays information about a hosted CnCNet or LAN game.
    /// </summary>
    public class GameInformationPanel : DXPanel
    {
        public GameInformationPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        DXLabel lblGameMode;
        DXLabel lblMap;
        DXLabel lblGameVersion;
        DXLabel lblHost;
        DXLabel lblPlayers;
        DXLabel[] lblPlayerNames;

        public override void Initialize()
        {
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 192), 1, 1);
            DrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            lblGameMode = new DXLabel(WindowManager);
            lblGameMode.ClientRectangle = new Rectangle(3, 3, 0, 0);

            lblMap = new DXLabel(WindowManager);
            lblMap.ClientRectangle = new Rectangle(3, 27, 0, 0);

            lblGameVersion = new DXLabel(WindowManager);
            lblGameVersion.ClientRectangle = new Rectangle(3, 51, 0, 0);

            lblHost = new DXLabel(WindowManager);
            lblHost.ClientRectangle = new Rectangle(3, 75, 0, 0);

            lblPlayers = new DXLabel(WindowManager);
            lblPlayers.ClientRectangle = new Rectangle(3, 99, 0, 0);

            lblPlayerNames = new DXLabel[8];
            for (int i = 0; i < lblPlayerNames.Length / 2; i++)
            {
                DXLabel lblPlayerName1 = new DXLabel(WindowManager);
                lblPlayerName1.ClientRectangle = new Rectangle(lblPlayers.ClientRectangle.X, lblPlayers.ClientRectangle.Y + 24 + i * 20, 0, 0);
                lblPlayerName1.FontIndex = 3;

                DXLabel lblPlayerName2 = new DXLabel(WindowManager);
                lblPlayerName2.ClientRectangle = new Rectangle(lblPlayers.ClientRectangle.X + 100, lblPlayerName1.ClientRectangle.Y, 0, 0);
                lblPlayerName2.FontIndex = lblPlayerName1.FontIndex;

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

            foreach (DXLabel label in lblPlayerNames)
                label.Visible = false;
        }
    }
}
