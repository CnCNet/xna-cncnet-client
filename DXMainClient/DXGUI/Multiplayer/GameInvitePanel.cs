﻿using System;
using System.Diagnostics;
using System.Windows.Forms;

using ClientCore;
using ClientCore.Extensions;

using ClientGUI;

using DTAClient.Domain.Multiplayer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer
{
    /// <summary>
    /// A UI panel that displays information about a hosted CnCNet or LAN game.
    /// </summary>
    public class GameInvitePanel : XNAPanel
    {
        public GameInvitePanel(WindowManager windowManager, MapLoader mapLoader)
            : base(windowManager)
        {
            this.mapLoader = mapLoader;
            DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
        }

        private readonly MapLoader mapLoader;

        private GenericHostedGame game = null;

        private const int buttonWidth = UIDesignConstants.BUTTON_WIDTH_92;
        private const int buttonHeight = UIDesignConstants.BUTTON_HEIGHT;
        private const int padding = 12;

        private XNALabel lblInviteHeading;
        private XNAClientButton btnInviteAccept;
        private XNAClientButton btnInviteDecline;
        private GameInformationPanel panelGameInformation;

        public event Action AcceptInvite;
        public event Action DeclineInvite;

        public override void Initialize()
        {
            panelGameInformation = new GameInformationPanel(WindowManager, mapLoader);
            panelGameInformation.Name = nameof(panelGameInformation);
            panelGameInformation.BackgroundTexture = AssetLoader.LoadTexture("cncnetlobbypanelbg.png");
            panelGameInformation.DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
            panelGameInformation.Initialize();
            panelGameInformation.ClearInfo();
            panelGameInformation.Disable();
            panelGameInformation.InputEnabled = false;
            panelGameInformation.DrawBorders = false;
            AddChild(panelGameInformation);

            lblInviteHeading = new XNALabel(WindowManager);
            AddChild(lblInviteHeading);

            ClientRectangle = new Rectangle(0, 0, panelGameInformation.Width + 2, padding + lblInviteHeading.Height  + panelGameInformation.Height  + padding + buttonHeight + padding); //...+2 to account for panelGameInformation border width
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 255), 1, 1);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            panelGameInformation.X = 1; //border width

            btnInviteAccept = new XNAClientButton(WindowManager);
            btnInviteAccept.Text = "Accept".L10N("Client:Main:InviteAccept");
            btnInviteAccept.ClientRectangle = new Rectangle(ClientRectangle.Width / 2 - buttonWidth - (padding / 2), panelGameInformation.Y + panelGameInformation.Height + padding, buttonWidth, buttonHeight);
            btnInviteAccept.LeftClick += (s, e) => AcceptInvite?.Invoke();
            btnInviteAccept.Visible = true;
            btnInviteAccept.Name = nameof(btnInviteAccept);
            AddChild(btnInviteAccept);

            btnInviteDecline = new XNAClientButton(WindowManager);
            btnInviteDecline.Text = "Decline".L10N("Client:Main:InviteDecline");
            btnInviteDecline.ClientRectangle = new Rectangle(ClientRectangle.Width / 2 + (padding / 2), btnInviteAccept.Y, buttonWidth, buttonHeight);
            btnInviteDecline.LeftClick += (s, e) => DeclineInvite?.Invoke();
            btnInviteDecline.Visible = true;
            btnInviteDecline.Name = nameof(btnInviteDecline);
            AddChild(btnInviteDecline);
            base.Initialize();
        }

        public void SetInfo(GenericHostedGame game)
        {
            if (game != null)
            {
                this.game = game;
                lblInviteHeading.FontIndex = 1;
                lblInviteHeading.Text = string.Format("{0} IS INVITING YOU TO PLAY".L10N("Client:Main:InviteHeading"), game.HostName);
                lblInviteHeading.CenterOnParentHorizontally();
                lblInviteHeading.Y = ((padding + lblInviteHeading.Height + padding) / 2) - (lblInviteHeading.Height / 2);
                panelGameInformation.Y = lblInviteHeading.Y + lblInviteHeading.Height + padding;
                panelGameInformation.SetInfo(game);
                panelGameInformation.Enable();
            };
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            DrawChildren(gameTime);
        }
    }
}