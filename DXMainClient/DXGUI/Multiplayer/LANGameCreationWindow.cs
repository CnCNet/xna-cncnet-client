using ClientGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Microsoft.Xna.Framework;

namespace DTAClient.DXGUI.Multiplayer
{
    /// <summary>
    /// A window that makes it possible for a LAN player who's hosting a game
    /// to pick between hosting a new game and hosting a loaded game.
    /// </summary>
    class LANGameCreationWindow : XNAWindow
    {
        public LANGameCreationWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        XNALabel lblDescription;

        XNAButton btnNewGame;
        XNAButton btnLoadGame;
        XNAButton btnCancel;

        public override void Initialize()
        {
            Name = "LANGameCreationWindow";
            BackgroundTexture = AssetLoader.LoadTexture("gamecreationoptionsbg.png");

            lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = "lblDescription";
            lblDescription.Text = "Select session type";

            btnNewGame = new XNAButton(WindowManager);
            btnNewGame.Name = "btnNewGame";
            btnNewGame.ClientRectangle = new Rectangle(12, 42, 133, 23);
            btnNewGame.IdleTexture = AssetLoader.LoadTexture("133pxbtn.png");
            btnNewGame.HoverTexture = AssetLoader.LoadTexture("133pxbtn_c.png");
            btnNewGame.Text = "New Game";
            btnNewGame.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnNewGame.LeftClick += BtnNewGame_LeftClick;

            btnLoadGame = new XNAButton(WindowManager);
            btnLoadGame.Name = "btnLoadGame";
            btnLoadGame.ClientRectangle = new Rectangle(btnNewGame.ClientRectangle.Right + 12,
                btnNewGame.ClientRectangle.Y, 133, 23);
            btnLoadGame.IdleTexture = btnNewGame.IdleTexture;
            btnLoadGame.HoverTexture = btnNewGame.HoverTexture;
            btnLoadGame.Text = "Load Multiplayer Game";
            btnLoadGame.HoverSoundEffect = btnNewGame.HoverSoundEffect;
            btnLoadGame.LeftClick += BtnLoadGame_LeftClick;

            btnCancel = new XNAButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(btnLoadGame.ClientRectangle.Right + 12,
                btnNewGame.ClientRectangle.Y, 133, 23);
            btnCancel.IdleTexture = btnNewGame.IdleTexture;
            btnCancel.HoverTexture = btnNewGame.HoverTexture;
            btnCancel.Text = "Cancel";
            btnCancel.HoverSoundEffect = btnNewGame.HoverSoundEffect;
            btnCancel.LeftClick += BtnCancel_LeftClick;

            AddChild(btnNewGame);

            base.Initialize();
        }

        private void BtnNewGame_LeftClick(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BtnLoadGame_LeftClick(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
