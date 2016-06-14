using Rampastring.XNAUI.DXControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using ClientCore.CnCNet5;
using Microsoft.Xna.Framework;
using HostedGame = DTAClient.domain.CnCNet.HostedGame;
using ClientCore;
using Microsoft.Xna.Framework.Graphics;

namespace DTAClient.DXGUI.Multiplayer
{
    public class GameListBox : DXListBox
    {
        public GameListBox(WindowManager windowManager, List<HostedGame> hostedGames,
            string localGameIdentifier)
            : base(windowManager)
        {
            this.hostedGames = hostedGames;
            this.localGameIdentifier = localGameIdentifier;
        }

        List<HostedGame> hostedGames;

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

            HostedGame hostedGame = (HostedGame)Items[HoveredIndex].Tag;

            panelGameInformation.SetInfo(hostedGame);
        }

        private void AddGameToList(HostedGame hg)
        {
            DXListBoxItem lbItem = new DXListBoxItem();
            lbItem.Tag = hg;
            if (hg.GameIdentifier == localGameIdentifier)
                lbItem.TextColor = UISettings.AltColor;
            else
                lbItem.TextColor = UISettings.TextColor;

            if (hg.IsIncompatible || hg.IsLocked)
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
                DXListBoxItem lbItem = Items[i];

                if (height + lbItem.TextLines.Count * LineHeight > ClientRectangle.Height)
                    break;

                int x = TextBorderDistance;

                if (i == HoveredIndex)
                {
                    Renderer.DrawTexture(BorderTexture,
                        new Rectangle(windowRectangle.X + 1, windowRectangle.Y + height, windowRectangle.Width - 2, lbItem.TextLines.Count * LineHeight),
                        GetColorWithAlpha(FocusColor));
                }

                HostedGame hg = (HostedGame)lbItem.Tag;

                Renderer.DrawTexture(hg.GameTexture,
                    new Rectangle(windowRectangle.X + x, windowRectangle.Y + height,
                    hg.GameTexture.Width, hg.GameTexture.Height), Color.White);

                x += hg.GameTexture.Width + 2;

                if (hg.IsLocked)
                {
                    Renderer.DrawTexture(txLockedGame,
                        new Rectangle(windowRectangle.X + x, windowRectangle.Y + height,
                        txLockedGame.Width, txLockedGame.Height), Color.White);
                    x += txLockedGame.Width + 2;
                }

                if (hg.IsIncompatible)
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

                x += lbItem.TextXPadding;

                Renderer.DrawStringWithShadow(hg.RoomName, FontIndex,
                    new Vector2(windowRectangle.X + x, windowRectangle.Y + height),
                    lbItem.TextColor);

                height += LineHeight;
            }
        }
    }
}
