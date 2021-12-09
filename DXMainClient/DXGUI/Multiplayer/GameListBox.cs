using System;
using System.Collections.Generic;
using System.Linq;
using ClientCore;
using ClientCore.Enums;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

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
            string localGameIdentifier, Predicate<GenericHostedGame> gameMatchesFilter)
            : base(windowManager)
        {
            HostedGames = new List<GenericHostedGame>();
            this.localGameIdentifier = localGameIdentifier;
            GameMatchesFilter = gameMatchesFilter;
        }

        private int loadedGameTextWidth;

        public List<GenericHostedGame> HostedGames;

        public double GameLifetime { get; set; } = 35.0;

        /// <summary>
        /// A predicate for setting a filter expression for displayed games.
        /// </summary>
        private Predicate<GenericHostedGame> GameMatchesFilter { get; }

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
            HostedGames.RemoveAt(index);

            Refresh();
        }

        /// <summary>
        /// Compares each listed XNAListBoxItem item in the GameListBox to the refernece XNAListBoxItem item for equality.
        /// </summary>
        /// <param name="referencedItem">The XNAListBoxItem to compare against</param>
        /// <returns>bool</returns>
        private static Predicate<XNAListBoxItem> GameListMatch(XNAListBoxItem referencedItem) => listedItem =>
        {
            var referencedGame = (GenericHostedGame)referencedItem?.Tag;
            var listedGame = (GenericHostedGame)listedItem?.Tag;

            if (referencedGame == null || listedGame == null)
                return false;

            return referencedGame.Equals(listedGame);
        };
        
        /// <summary>
        /// Refreshes game information in the game list box.
        /// </summary>
        public void Refresh()
        {
            var selectedItem = SelectedItem;
            var hoveredItem = HoveredItem;
            
            Items.Clear();

            GetSortedAndFilteredGames()
                .ToList()
                .ForEach(AddGameToList);

            if (selectedItem != null)
                SelectedIndex = Items.FindIndex(GameListMatch(selectedItem));
            if (hoveredItem != null)
                HoveredIndex = Items.FindIndex(GameListMatch(hoveredItem));

            ShowGamePanelInfoForIndex(IsValidGameIndex(SelectedIndex) ? SelectedIndex : HoveredIndex);
        }

        /// <summary>
        /// Adds a game to the game list.
        /// </summary>
        /// <param name="game">The game to add.</param>
        public void AddGame(GenericHostedGame game)
        {
            HostedGames.Add(game);

            Refresh();
        }

        private IEnumerable<GenericHostedGame> GetSortedAndFilteredGames()
        {
            var sortedGames = GetSortedGames();

            return GameMatchesFilter == null ? sortedGames : sortedGames.Where(hg => GameMatchesFilter(hg));
        }

        private IEnumerable<GenericHostedGame> GetSortedGames()
        {
            var sortedGames =
                HostedGames
                    .OrderBy(hg => hg.Locked)
                    .ThenBy(hg => string.Equals(hg.Game.InternalName, localGameIdentifier, StringComparison.CurrentCultureIgnoreCase))
                    .ThenBy(hg => hg.GameVersion != ProgramConstants.GAME_VERSION)
                    .ThenBy(hg => hg.Passworded);
            
            switch ((SortDirection)UserINISettings.Instance.SortState.Value)
            {
                case SortDirection.Asc:
                    sortedGames = sortedGames.ThenBy(hg => hg.RoomName);
                    break;
                case SortDirection.Desc:
                    sortedGames = sortedGames.ThenByDescending(hg => hg.RoomName);
                    break;
            }

            return sortedGames;
        }

        /// <summary>
        /// Sorts and refreshes the game information in the game list box.
        /// </summary>
        public void SortAndRefreshHostedGames()
        {
            Refresh();
        }

        public void ClearGames()
        {
            Clear();
            HostedGames.Clear();
        }

        public override void Initialize()
        {
            base.Initialize();

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
            Parent.AddChild(panelGameInformation); // make this a child of our parent so it's not drawn on our rendertarget

            SelectedIndexChanged += GameListBox_SelectedIndexChanged;
            HoveredIndexChanged += GameListBox_HoveredIndexChanged;

            hoverOnGameColor = AssetLoader.GetColorFromString(
                ClientConfiguration.Instance.HoverOnGameColor);

            loadedGameTextWidth = (int)Renderer.GetTextDimensions(LOADED_GAME_TEXT, FontIndex).X;
        }

        private bool IsValidGameIndex(int index)
        {
            return index >= 0 && index < Items.Count;
        }

        private void ShowGamePanelInfoForIndex(int index)
        {
            if (!IsValidGameIndex(index))
            {
                panelGameInformation.AlphaRate = -0.5f;
                return;
            }

            panelGameInformation.Enable();
            panelGameInformation.X = Right;
            panelGameInformation.Y = Y;

            panelGameInformation.AlphaRate = 0.5f;

            var hostedGame = (GenericHostedGame)Items[index].Tag;
            panelGameInformation.SetInfo(hostedGame);
        }

        private void GameListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowGamePanelInfoForIndex(SelectedIndex);
        }

        private void GameListBox_HoveredIndexChanged(object sender, EventArgs e)
        {
            if (!IsValidGameIndex(SelectedIndex))
                ShowGamePanelInfoForIndex(HoveredIndex);
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
