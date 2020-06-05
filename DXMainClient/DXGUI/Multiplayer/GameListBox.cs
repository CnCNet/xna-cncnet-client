using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DTAClient.Domain.Multiplayer;
using System.Linq;
using ClientCore;

namespace DTAClient.DXGUI.Multiplayer
{
    /// <summary>
    /// A list box for listing hosted games.
    /// </summary>
    public class GameListBox : XNAListBox
    {
        private const int GAME_REFRESH_RATE = 1;
        private const int ICON_MARGIN = 2;
        private const int FONT_INDEX = 0;
        private const string LOADED_GAME_TEXT = " (Loaded Game)";

        public GameListBox(WindowManager windowManager,
            string localGameIdentifier)
            : base(windowManager)
        {
            HostedGames = new List<GenericHostedGame>();
            this.localGameIdentifier = localGameIdentifier;
        }

        private int loadedGameTextWidth;

        public List<GenericHostedGame> HostedGames;

        public double GameLifetime { get; set; } = 35.0;

        /// <summary>
        /// A predicate for setting a filter expression for displayed games.
        /// </summary>
        public Predicate<GenericHostedGame> GameMatchesFilter { get; set; }

        private Texture2D txLockedGame;
        private Texture2D txIncompatibleGame;
        private Texture2D txPasswordedGame;

        private string localGameIdentifier;

        private GameInformationPanel panelGameInformation;

        private TimeSpan timeSinceGameRefresh;

        private Color hoverOnGameColor;

        /// <summary>
        /// Removes a game from the list.
        /// </summary>
        /// <param name="index">The index of the game to remove.</param>
        public void RemoveGame(int index)
        {
            if (SelectedIndex == index)
                SelectedIndex = -1;
            else if (SelectedIndex > index)
                SelectedIndex--;

            HostedGames.RemoveAt(index);

            Refresh();
        }

        /// <summary>
        /// Refreshes game information in the game list box.
        /// </summary>
        public void Refresh()
        {
            Items.Clear();

            if (GameMatchesFilter != null)
            {
                foreach (var hg in HostedGames)
                {
                    if (GameMatchesFilter(hg))
                        AddGameToList(hg);
                }
            }
            else HostedGames.ForEach(AddGameToList);

            GameListBox_HoveredIndexChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Adds a game to the game list.
        /// </summary>
        /// <param name="game">The game to add.</param>
        public void AddGame(GenericHostedGame game)
        {
            GenericHostedGame selectedGame = null;
            if (SelectedIndex > -1 && SelectedIndex < HostedGames.Count)
            {
                selectedGame = HostedGames[SelectedIndex];
            }

            HostedGames.Add(game);

            HostedGames = HostedGames.OrderBy(hg => hg.Passworded).OrderBy(hg =>
                hg.GameVersion != ProgramConstants.GAME_VERSION).OrderBy(hg =>
                hg.Game.InternalName.ToUpper() == localGameIdentifier.ToUpper()).OrderBy(hg =>
                hg.Locked).ToList();

            Refresh();

            if (selectedGame != null)
                SelectedIndex = HostedGames.FindIndex(hg => hg == selectedGame);
        }

        /// <summary>
        /// Sorts and refreshes the game information in the game list box.
        /// </summary>
        public void SortAndRefreshHostedGames()
        {
            GenericHostedGame selectedGame = null;
            if (SelectedIndex > -1 && SelectedIndex < HostedGames.Count)
            {
                selectedGame = HostedGames[SelectedIndex];
            }

            HostedGames = HostedGames.OrderBy(hg => hg.Passworded).OrderBy(hg =>
                hg.GameVersion != ProgramConstants.GAME_VERSION).OrderBy(hg =>
                hg.Game.InternalName.ToUpper() == localGameIdentifier.ToUpper()).OrderBy(hg =>
                hg.Locked).ToList();

            Refresh();

            if (selectedGame != null)
                SelectedIndex = HostedGames.FindIndex(hg => hg == selectedGame);
        }

        public void ClearGames()
        {
            Clear();
            HostedGames.Clear();
        }

        protected override int GetRenderTargetWidth() => Width + panelGameInformation.Width;

        public override void Initialize()
        {
            txLockedGame = AssetLoader.LoadTexture("lockedgame.png");
            txIncompatibleGame = AssetLoader.LoadTexture("incompatible.png");
            txPasswordedGame = AssetLoader.LoadTexture("passwordedgame.png");

            panelGameInformation = new GameInformationPanel(WindowManager);
            panelGameInformation.Name = "panelGameInformation";
            panelGameInformation.BackgroundTexture = AssetLoader.LoadTexture("cncnetlobbypanelbg.png");
            panelGameInformation.DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
            panelGameInformation.Initialize();
            panelGameInformation.ClearInfo();
            panelGameInformation.Disable();
            panelGameInformation.InputEnabled = false;
            panelGameInformation.Alpha = 0f;
            AddChild(panelGameInformation);

            HoveredIndexChanged += GameListBox_HoveredIndexChanged;

            hoverOnGameColor = AssetLoader.GetColorFromString(
                ClientConfiguration.Instance.HoverOnGameColor);

            base.Initialize();

            loadedGameTextWidth = (int)Renderer.GetTextDimensions(LOADED_GAME_TEXT, FontIndex).X;
        }

        private void GameListBox_HoveredIndexChanged(object sender, EventArgs e)
        {
            if (HoveredIndex < 0 || HoveredIndex >= Items.Count)
            {
                panelGameInformation.AlphaRate = -0.5f;
                return;
            }

            panelGameInformation.Enable();
            panelGameInformation.X = Width;
            panelGameInformation.Y = Math.Min((HoveredIndex - TopIndex) * LineHeight,
                         Height - panelGameInformation.Height);

            panelGameInformation.AlphaRate = 0.5f;

            var hostedGame = (GenericHostedGame)Items[HoveredIndex].Tag;

            panelGameInformation.SetInfo(hostedGame);
        }

        private void AddGameToList(GenericHostedGame hg)
        {
            int lgTextWidth = hg.IsLoadedGame ? loadedGameTextWidth : 0;
            int maxTextWidth = Width - hg.Game.Texture.Width - 
                (hg.Incompatible ? txIncompatibleGame.Width : 0) -
                (hg.Locked ? txLockedGame.Width : 0) - (hg.Passworded ? txPasswordedGame.Width : 0) - 
                (ICON_MARGIN * 3) - GetScrollBarWidth() - lgTextWidth;

            var lbItem = new XNAListBoxItem();
            lbItem.Tag = hg;
            lbItem.Text = Renderer.GetStringWithLimitedWidth(Renderer.GetSafeString(
                hg.RoomName, FontIndex), FontIndex, maxTextWidth);

            if (hg.Game.InternalName != localGameIdentifier.ToLower())
                lbItem.TextColor = UISettings.ActiveSettings.TextColor;
            //else // made unnecessary by new Rampastring.XNAUI
            //    lbItem.TextColor = UISettings.ActiveSettings.AltColor;

            if (hg.Incompatible || hg.Locked)
            {
                lbItem.TextColor = Color.Gray;
            }

            AddItem(lbItem);
        }

        public override void OnMouseLeave()
        {
            panelGameInformation.AlphaRate = -0.5f;

            base.OnMouseLeave();
        }

        public override void Update(GameTime gameTime)
        {
            timeSinceGameRefresh += gameTime.ElapsedGameTime;

            if (timeSinceGameRefresh.TotalSeconds > GAME_REFRESH_RATE)
            {
                for (int i = 0; i < HostedGames.Count; i++)
                {
                    if (DateTime.Now - HostedGames[i].LastRefreshTime > TimeSpan.FromSeconds(GameLifetime))
                    {
                        HostedGames.RemoveAt(i);
                        i--;

                        if (SelectedIndex == i)
                            SelectedIndex = -1;
                        else if (SelectedIndex > i)
                            SelectedIndex--;
                    }
                }

                Refresh();

                timeSinceGameRefresh = TimeSpan.Zero;
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            DrawPanel();

            int height = 2;

            for (int i = TopIndex; i < Items.Count; i++)
            {
                var lbItem = Items[i];

                if (height + lbItem.TextLines.Count * LineHeight > Height)
                    break;

                int x = TextBorderDistance;

                bool scrollBarDrawn = ScrollBar.IsDrawn() && EnableScrollbar;
                int drawnWidth = !scrollBarDrawn || DrawSelectionUnderScrollbar ? Width - 2 : Width - 2 - ScrollBar.Width;

                if (i == SelectedIndex)
                {
                    FillRectangle(
                        new Rectangle(1, height, drawnWidth, lbItem.TextLines.Count * LineHeight),
                        FocusColor);
                }
                else if (i == HoveredIndex)
                {
                    FillRectangle(
                        new Rectangle(1, height, drawnWidth, lbItem.TextLines.Count * LineHeight),
                        hoverOnGameColor);
                }

                var hostedGame = (GenericHostedGame)lbItem.Tag;

                DrawTexture(hostedGame.Game.Texture,
                    new Rectangle(x, height,
                    hostedGame.Game.Texture.Width, hostedGame.Game.Texture.Height), Color.White);

                x += hostedGame.Game.Texture.Width + ICON_MARGIN;

                if (hostedGame.Locked)
                {
                    DrawTexture(txLockedGame,
                        new Rectangle(x, height,
                        txLockedGame.Width, txLockedGame.Height), Color.White);
                    x += txLockedGame.Width + ICON_MARGIN;
                }

                if (hostedGame.Incompatible)
                {
                    DrawTexture(txIncompatibleGame,
                        new Rectangle(x, height,
                        txIncompatibleGame.Width, txIncompatibleGame.Height), Color.White);
                    x += txIncompatibleGame.Width + ICON_MARGIN;
                }

                if (hostedGame.Passworded)
                {
                    DrawTexture(txPasswordedGame,
                        new Rectangle(Width - txPasswordedGame.Width - TextBorderDistance - (scrollBarDrawn ? ScrollBar.Width : 0),
                        height, txPasswordedGame.Width, txPasswordedGame.Height),
                        Color.White);
                }

                var text = lbItem.Text;
                if (hostedGame.IsLoadedGame)
                    text = lbItem.Text + LOADED_GAME_TEXT;

                x += lbItem.TextXPadding;

                DrawStringWithShadow(text, FontIndex,
                    new Vector2(x, height),
                    lbItem.TextColor);

                height += LineHeight;
            }

            if (DrawBorders)
                DrawPanelBorders();

            DrawChildren(gameTime);
        }
    }
}
