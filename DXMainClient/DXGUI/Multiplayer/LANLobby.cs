using ClientGUI;
using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using ClientCore;
using Microsoft.Xna.Framework;
using DTAClient.domain.LAN;
using DTAClient.domain.Multiplayer;
using System.Collections.Generic;
using DTAClient.Properties;

namespace DTAClient.DXGUI.Multiplayer
{
    class LANLobby : XNAWindow
    {
        public LANLobby(WindowManager windowManager, GameCollection gameCollection)
            : base(windowManager)
        {
            this.gameCollection = gameCollection;
        }

        XNAListBox lbPlayerList;
        ChatListBox lbChatMessages;
        GameListBox lbGameList;

        XNAButton btnMainMenu;
        XNAButton btnNewGame;
        XNAButton btnJoinGame;

        XNATextBox tbChatInput;

        XNALabel lblColor;

        XNADropDown ddColor;

        Texture2D unknownGameIcon;

        LANColor[] chatColors;

        string localGame;

        GameCollection gameCollection;

        List<GenericHostedGame> hostedGames = new List<GenericHostedGame>();

        TimeSpan timeSinceGameRefresh = TimeSpan.Zero;

        ToggleableSound sndGameCreated;

        public override void Initialize()
        {
            Name = "LANLobby";
            BackgroundTexture = AssetLoader.LoadTexture("cncnetlobbybg.png");
            localGame = DomainController.Instance().GetDefaultGame();

            btnNewGame = new XNAButton(WindowManager);
            btnNewGame.Name = "btnNewGame";
            btnNewGame.ClientRectangle = new Rectangle(12, ClientRectangle.Height - 29, 133, 23);
            btnNewGame.IdleTexture = AssetLoader.LoadTexture("133pxbtn.png");
            btnNewGame.HoverTexture = AssetLoader.LoadTexture("133pxbtn_c.png");
            btnNewGame.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnNewGame.FontIndex = 1;
            btnNewGame.Text = "Create Game";
            btnNewGame.AllowClick = false;
            btnNewGame.LeftClick += BtnNewGame_LeftClick;

            btnJoinGame = new XNAButton(WindowManager);
            btnJoinGame.Name = "btnJoinGame";
            btnJoinGame.ClientRectangle = new Rectangle(btnNewGame.ClientRectangle.Right + 12,
                btnNewGame.ClientRectangle.Y, 133, 23);
            btnJoinGame.IdleTexture = AssetLoader.LoadTexture("133pxbtn.png");
            btnJoinGame.HoverTexture = AssetLoader.LoadTexture("133pxbtn_c.png");
            btnJoinGame.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnJoinGame.FontIndex = 1;
            btnJoinGame.Text = "Join Game";
            btnJoinGame.AllowClick = false;
            btnJoinGame.LeftClick += BtnJoinGame_LeftClick;

            btnMainMenu = new XNAButton(WindowManager);
            btnMainMenu.Name = "btnMainMenu";
            btnMainMenu.ClientRectangle = new Rectangle(ClientRectangle.Width - 145, btnNewGame.ClientRectangle.Y,
                133, 23);
            btnMainMenu.IdleTexture = AssetLoader.LoadTexture("133pxbtn.png");
            btnMainMenu.HoverTexture = AssetLoader.LoadTexture("133pxbtn_c.png");
            btnMainMenu.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnMainMenu.FontIndex = 1;
            btnMainMenu.Text = "Main Menu";
            btnMainMenu.LeftClick += BtnLogout_LeftClick;

            lbGameList = new GameListBox(WindowManager, hostedGames, localGame);
            lbGameList.Name = "lbGameList";
            lbGameList.ClientRectangle = new Rectangle(btnNewGame.ClientRectangle.X,
                41, btnJoinGame.ClientRectangle.Right - btnNewGame.ClientRectangle.X,
                btnNewGame.ClientRectangle.Top - 47);
            lbGameList.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbGameList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbGameList.DoubleLeftClick += LbGameList_DoubleLeftClick;
            lbGameList.AllowMultiLineItems = false;

            lbPlayerList = new XNAListBox(WindowManager);
            lbPlayerList.Name = "lbPlayerList";
            lbPlayerList.ClientRectangle = new Rectangle(ClientRectangle.Width - 202,
                lbGameList.ClientRectangle.Y, 190,
                btnMainMenu.ClientRectangle.Top - lbGameList.ClientRectangle.Y - 14);
            lbPlayerList.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbPlayerList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbPlayerList.LineHeight = 16;

            lbChatMessages = new ChatListBox(WindowManager);
            lbChatMessages.Name = "lbChatMessages";
            lbChatMessages.ClientRectangle = new Rectangle(lbGameList.ClientRectangle.Right + 9, lbGameList.ClientRectangle.Y,
                lbPlayerList.ClientRectangle.Left - lbGameList.ClientRectangle.Right - 18, lbPlayerList.ClientRectangle.Height);
            lbChatMessages.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbChatMessages.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbChatMessages.LineHeight = 16;

            tbChatInput = new XNATextBox(WindowManager);
            tbChatInput.Name = "tbChatInput";
            tbChatInput.ClientRectangle = new Rectangle(lbChatMessages.ClientRectangle.X,
                btnNewGame.ClientRectangle.Y, lbChatMessages.ClientRectangle.Width,
                btnNewGame.ClientRectangle.Height);
            tbChatInput.Enabled = false;
            tbChatInput.MaximumTextLength = 200;
            tbChatInput.EnterPressed += TbChatInput_EnterPressed;

            lblColor = new XNALabel(WindowManager);
            lblColor.Name = "lblColor";
            lblColor.ClientRectangle = new Rectangle(lbChatMessages.ClientRectangle.X, 14, 0, 0);
            lblColor.FontIndex = 1;
            lblColor.Text = "YOUR COLOR:";

            ddColor = new XNADropDown(WindowManager);
            ddColor.Name = "ddColor";
            ddColor.ClientRectangle = new Rectangle(lblColor.ClientRectangle.X + 95, 12,
                150, 21);
            ddColor.SelectedIndexChanged += DdColor_SelectedIndexChanged;
            ddColor.ClickSoundEffect = AssetLoader.LoadSound("dropdown.wav");

            chatColors = new LANColor[]
            {
                new LANColor("Gray", Color.Gray),
                new LANColor("Metalic", Color.LightGray),
                new LANColor("Green", Color.Green),
                new LANColor("Lime Green", Color.LimeGreen),
                new LANColor("Green Yellow", Color.GreenYellow),
                new LANColor("Goldenrod", Color.Goldenrod),
                new LANColor("Yellow", Color.Yellow),
                new LANColor("Orange", Color.Orange),
                new LANColor("Red", Color.Red),
                new LANColor("Blood Red", Color.DarkRed),
                new LANColor("Pink", Color.DeepPink),
                new LANColor("Purple", Color.MediumPurple),
                new LANColor("Sky Blue", Color.SkyBlue),
                new LANColor("Blue", Color.Blue),
                new LANColor("Brown", Color.Brown),
                new LANColor("Teal", Color.Teal)
            };

            foreach (LANColor color in chatColors)
            {
                ddColor.AddItem(color.Name, color.XNAColor);
            }

            int selectedColor = DomainController.Instance().GetCnCNetChatColor();

            ddColor.SelectedIndex = selectedColor >= ddColor.Items.Count || selectedColor < 0
                ? 0 : selectedColor;

            AddChild(btnNewGame);
            AddChild(btnJoinGame);
            AddChild(btnMainMenu);

            AddChild(lbPlayerList);
            AddChild(lbChatMessages);
            AddChild(lbGameList);
            AddChild(tbChatInput);
            AddChild(lblColor);
            AddChild(ddColor);

            unknownGameIcon = AssetLoader.TextureFromImage(Resources.unknownicon);

            SoundEffect gameCreatedSoundEffect = AssetLoader.LoadSound("gamecreated.wav");

            if (gameCreatedSoundEffect != null)
                sndGameCreated = new ToggleableSound(gameCreatedSoundEffect.CreateInstance());

            base.Initialize();
        }

        private void DdColor_SelectedIndexChanged(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void TbChatInput_EnterPressed(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void LbGameList_DoubleLeftClick(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BtnLogout_LeftClick(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BtnJoinGame_LeftClick(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BtnNewGame_LeftClick(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
