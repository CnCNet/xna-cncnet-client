using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DTAClient.domain.Multiplayer.CnCNet;
using DTAClient.domain.Multiplayer;

namespace DTAClient.DXGUI.Multiplayer
{
    public class GameListBox : XNAListBox
    {
        public GameListBox(WindowManager windowManager, List<GenericHostedGame> hostedGames,
            string localGameIdentifier)
            : base(windowManager)
        {
            this.hostedGames = hostedGames;
            this.localGameIdentifier = localGameIdentifier;
        }

        List<GenericHostedGame> hostedGames;

        Texture2D txLockedGame;
        Texture2D txIncompatibleGame;
        Texture2D txPasswordedGame;

        string localGameIdentifier;

        GameInformationPanel panelGameInformation;

        bool showGameInfo = false;

        public void Refresh()
        {
            Items.Clear();

            hostedGames.ForEach(AddGameToList);
            GameListBox_HoveredIndexChanged(this, EventArgs.Empty);
        }

        public override void Initialize()
        {
            txLockedGame = AssetLoader.LoadTexture("lockedgame.png");
            txIncompatibleGame = AssetLoader.LoadTexture("incompatible.png");
            txPasswordedGame = AssetLoader.LoadTexture("passwordedgame.png");

            panelGameInformation = new GameInformationPanel(WindowManager);
            panelGameInformation.Name = "panelGameInformation";
            panelGameInformation.BackgroundTexture = AssetLoader.LoadTexture("cncnetlobbypanelbg.png");
            panelGameInformation.ClientRectangle = new Rectangle(0, 0, 235, 220);

            panelGameInformation.Parent = this;
            panelGameInformation.Initialize();

            panelGameInformation.ClearInfo();

            HoveredIndexChanged += GameListBox_HoveredIndexChanged;

            base.Initialize();
        }

        private void GameListBox_HoveredIndexChanged(object sender, EventArgs e)
        {
            if (HoveredIndex < 0 || HoveredIndex >= Items.Count)
            {
                showGameInfo = false;
                return;
            }

            panelGameInformation.ClientRectangle = new Rectangle(ClientRectangle.Width,
                (HoveredIndex - TopIndex) * LineHeight,
                panelGameInformation.ClientRectangle.Width,
                panelGameInformation.ClientRectangle.Height);

            showGameInfo = true;

            HostedCnCNetGame hostedGame = (HostedCnCNetGame)Items[HoveredIndex].Tag;

            panelGameInformation.SetInfo(hostedGame);
        }

        private void AddGameToList(GenericHostedGame hg)
        {
            XNAListBoxItem lbItem = new XNAListBoxItem();
            lbItem.Tag = hg;
            if (hg.Game.InternalName == localGameIdentifier.ToLower())
                lbItem.TextColor = UISettings.AltColor;
            else
                lbItem.TextColor = UISettings.TextColor;

            if (hg.Incompatible || hg.Locked)
            {
                lbItem.TextColor = Color.Gray;
            }

            AddItem(lbItem);
        }

        public override void OnMouseLeave()
        {
            showGameInfo = false;

            base.OnMouseLeave();
        }

        public override void Draw(GameTime gameTime)
        {
            Rectangle windowRectangle = WindowRectangle();

            if (showGameInfo)
                panelGameInformation.Draw(gameTime);

            DrawPanel();

            int height = 2;

            for (int i = TopIndex; i < Items.Count; i++)
            {
                XNAListBoxItem lbItem = Items[i];

                if (height + lbItem.TextLines.Count * LineHeight > ClientRectangle.Height)
                    break;

                int x = TextBorderDistance;

                if (i == HoveredIndex)
                {
                    Renderer.DrawTexture(BorderTexture,
                        new Rectangle(windowRectangle.X + 1, windowRectangle.Y + height, windowRectangle.Width - 2, lbItem.TextLines.Count * LineHeight),
                        GetColorWithAlpha(FocusColor));
                }

                HostedCnCNetGame hg = (HostedCnCNetGame)lbItem.Tag;

                Renderer.DrawTexture(hg.Game.Texture,
                    new Rectangle(windowRectangle.X + x, windowRectangle.Y + height,
                    hg.Game.Texture.Width, hg.Game.Texture.Height), Color.White);

                x += hg.Game.Texture.Width + 2;

                if (hg.Locked)
                {
                    Renderer.DrawTexture(txLockedGame,
                        new Rectangle(windowRectangle.X + x, windowRectangle.Y + height,
                        txLockedGame.Width, txLockedGame.Height), Color.White);
                    x += txLockedGame.Width + 2;
                }

                if (hg.Incompatible)
                {
                    Renderer.DrawTexture(txIncompatibleGame,
                        new Rectangle(windowRectangle.X + x, windowRectangle.Y + height,
                        txIncompatibleGame.Width, txIncompatibleGame.Height), Color.White);
                    x += txIncompatibleGame.Width + 2;
                }

                if (hg.Passworded)
                {
                    Renderer.DrawTexture(txPasswordedGame,
                        new Rectangle(windowRectangle.Right - txPasswordedGame.Width - TextBorderDistance,
                        windowRectangle.Y + height, txPasswordedGame.Width, txPasswordedGame.Height),
                        Color.White);
                }

                string text = hg.RoomName;
                if (hg.IsLoadedGame)
                    text = hg.RoomName + " (Loaded Game)";

                x += lbItem.TextXPadding;

                Renderer.DrawStringWithShadow(text, FontIndex,
                    new Vector2(windowRectangle.X + x, windowRectangle.Y + height),
                    lbItem.TextColor);

                height += LineHeight;
            }
        }
    }
}
