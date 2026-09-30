using System;
using System.Collections.Generic;
using System.Linq;

using ClientCore;
using ClientCore.Enums;
using ClientCore.Extensions;

using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.DXGUI.Multiplayer.GameLobby;

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
        private static string LOADED_GAME_TEXT => " (" + "Loaded Game".L10N("Client:Main:LoadedGame") + ")";
        private readonly string[] SkillLevelOptions;

        public GameListBox(WindowManager windowManager, MapLoader mapLoader,
            string localGameIdentifier, GameLobbyBase gameLobby = null,
            Predicate<GenericHostedGame> gameMatchesFilter = null)
            : base(windowManager)
        {
            this.mapLoader = mapLoader;
            this.localGameIdentifier = localGameIdentifier;
            this.gameLobby = gameLobby;
            GameMatchesFilter = gameMatchesFilter;

            SkillLevelOptions = ClientConfiguration.Instance.GetSkillLevelOptions();
        }

        private List<Texture2D?> txSkillLevelIcons = new();

        private int loadedGameTextWidth;

        /// <summary>
        /// The font-derived line height, used as the minimum row height when no
        /// icon is taller than the text.
        /// </summary>
        private int baseLineHeight;

        public List<GenericHostedGame> HostedGames = new();

        public double GameLifetime { get; set; } = 35.0;

        /// <summary>
        /// A predicate for setting a filter expression for displayed games.
        /// </summary>
        private Predicate<GenericHostedGame> GameMatchesFilter { get; }

        private Texture2D txLockedGame;
        private Texture2D txIncompatibleGame;
        private Texture2D txPasswordedGame;

        private string localGameIdentifier;

        private MapLoader mapLoader;

        private GameLobbyBase gameLobby;

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

            Clear();

            var games = GetSortedAndFilteredGames().ToList();

            // Size the rows around the tallest icon, falling back to the font
            // height when the text is taller than any icon. This is applied
            // before adding items so their text is vertically centered correctly.
            LineHeight = Math.Max(baseLineHeight, GetMaxIconHeight(games));

            games.ForEach(AddGameToList);

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

            // Early notify the map preview cache
            mapLoader.PrefetchCachedPreviewImageFromMap(mapLoader.FindMapByHash(game.MapHash));

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
                    .ThenBy(hg => string.Equals(hg.Game.InternalName, localGameIdentifier, StringComparison.InvariantCultureIgnoreCase))
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

            panelGameInformation = new GameInformationPanel(WindowManager, mapLoader, gameLobby);
            panelGameInformation.Name = nameof(panelGameInformation);
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

            InitSkillLevelIcons();

            // Remember the configured/font-derived line height so it can serve as
            // the minimum row height regardless of icon sizes.
            baseLineHeight = LineHeight;
        }

        private void InitSkillLevelIcons()
        {
            for (int i = 0; i < SkillLevelOptions.Length; i++)
            {
                string fileName = $"skillLevel{i}.png";

                txSkillLevelIcons.Add(AssetLoader.AssetExists(fileName)
                    ? AssetLoader.LoadTexture(fileName)
                    : null);
            }
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

        private (List<Texture2D> leftIcons, List<Texture2D> rightIcons) GetGameOptionIcons(GenericHostedGame game)
        {
            var leftIcons = new List<Texture2D>();
            var rightIcons = new List<Texture2D>();

            if (gameLobby == null || game is not HostedCnCNetGame cncnetGame)
                return (leftIcons, rightIcons);

            if (cncnetGame.BroadcastedGameOptionValues == null || cncnetGame.BroadcastedGameOptionValues.Length == 0)
                return (leftIcons, rightIcons);

            var broadcastableSettings = gameLobby.GetBroadcastableSettings();

            for (int i = 0; i < broadcastableSettings.Count && i < cncnetGame.BroadcastedGameOptionValues.Length; i++)
            {
                var setting = broadcastableSettings[i];
                int value = cncnetGame.BroadcastedGameOptionValues[i];

                if (setting is GameLobbyCheckBox checkbox && checkbox.ShowInGameList)
                {
                    string iconName = value != 0 ? checkbox.EnabledIcon : checkbox.DisabledIcon;
                    if (string.IsNullOrEmpty(iconName))
                        continue;

                    Texture2D icon = AssetLoader.LoadTexture(iconName);
                    if (icon != null)
                    {
                        if (checkbox.ShowInGameListOnRight)
                            rightIcons.Add(icon);
                        else
                            leftIcons.Add(icon);
                    }
                }
                else if (setting is GameLobbyDropDown dropdown && dropdown.ShowInGameList)
                {
                    // Use the icon for the selected value
                    if (value >= 0 && value < dropdown.Items.Count)
                    {
                        Texture2D icon = dropdown.Items[value].Texture;
                        if (icon != null)
                        {
                            if (dropdown.ShowInGameListOnRight)
                                rightIcons.Add(icon);
                            else
                                leftIcons.Add(icon);
                        }
                    }
                }
            }

            return (leftIcons, rightIcons);
        }

        /// <summary>
        /// Determines the height of the tallest icon that could be drawn for the
        /// given set of games, so rows can be sized to fit it.
        /// </summary>
        private int GetMaxIconHeight(IEnumerable<GenericHostedGame> games)
        {
            int maxHeight = 0;

            // Status icons are not game-specific, so always account for them.
            if (txLockedGame != null)
                maxHeight = Math.Max(maxHeight, txLockedGame.Height);
            if (txIncompatibleGame != null)
                maxHeight = Math.Max(maxHeight, txIncompatibleGame.Height);
            if (txPasswordedGame != null)
                maxHeight = Math.Max(maxHeight, txPasswordedGame.Height);
            foreach (var icon in txSkillLevelIcons)
            {
                if (icon != null)
                    maxHeight = Math.Max(maxHeight, icon.Height);
            }

            foreach (var game in games)
            {
                bool showGameIcon = ClientConfiguration.Instance.ShowGameIconInGameList
                    || game.Game.InternalName != localGameIdentifier.ToLower();
                if (showGameIcon && game.Game.Texture != null)
                    maxHeight = Math.Max(maxHeight, game.Game.Texture.Height);

                var (leftIcons, rightIcons) = GetGameOptionIcons(game);
                foreach (var icon in leftIcons)
                    maxHeight = Math.Max(maxHeight, icon.Height);
                foreach (var icon in rightIcons)
                    maxHeight = Math.Max(maxHeight, icon.Height);
            }

            return maxHeight;
        }

        private void AddGameToList(GenericHostedGame hg)
        {
            int lgTextWidth = hg.IsLoadedGame ? loadedGameTextWidth : 0;

            var (leftIcons, rightIcons) = GetGameOptionIcons(hg);
            int leftIconsWidth = leftIcons.Count > 0 ?
                (leftIcons.Sum(icon => icon.Width) + (leftIcons.Count * ICON_MARGIN)) : 0;
            int rightIconsWidth = rightIcons.Count > 0 ?
                (rightIcons.Sum(icon => icon.Width) + (rightIcons.Count * ICON_MARGIN)) : 0;

            bool showGameIcon = ClientConfiguration.Instance.ShowGameIconInGameList
                || hg.Game.InternalName != localGameIdentifier.ToLower();
            int gameTextureWidth = showGameIcon ? hg.Game.Texture.Width : 0;

            int skillLevelIndex = hg.SkillLevel;
            int skillLevelIconWidth = 0;
            if (txSkillLevelIcons[skillLevelIndex] != null)
                skillLevelIconWidth = txSkillLevelIcons[skillLevelIndex].Width;

            int maxTextWidth = Width - gameTextureWidth -
                (hg.Incompatible ? txIncompatibleGame.Width : 0) -
                (hg.Locked ? txLockedGame.Width : 0) -
                (hg.Passworded ? txPasswordedGame.Width : 0) -
                skillLevelIconWidth -
                leftIconsWidth - rightIconsWidth -
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

                // left-side game option icons
                var (leftIcons, rightIcons) = GetGameOptionIcons(hostedGame);
                foreach (var icon in leftIcons)
                {
                    DrawTexture(icon,
                        new Rectangle(x, height + (LineHeight - icon.Height) / 2,
                        icon.Width, icon.Height), Color.White);
                    x += icon.Width + ICON_MARGIN;
                }

                bool showGameIcon = ClientConfiguration.Instance.ShowGameIconInGameList
                    || hostedGame.Game.InternalName != localGameIdentifier.ToLower();

                if (showGameIcon)
                {
                    DrawTexture(hostedGame.Game.Texture,
                        new Rectangle(x, height + (LineHeight - hostedGame.Game.Texture.Height) / 2,
                        hostedGame.Game.Texture.Width, hostedGame.Game.Texture.Height), Color.White);

                    x += hostedGame.Game.Texture.Width + ICON_MARGIN;
                }

                if (hostedGame.Locked)
                {
                    DrawTexture(txLockedGame,
                        new Rectangle(x, height + (LineHeight - txLockedGame.Height) / 2,
                        txLockedGame.Width, txLockedGame.Height), Color.White);
                    x += txLockedGame.Width + ICON_MARGIN;
                }

                if (hostedGame.Incompatible)
                {
                    DrawTexture(txIncompatibleGame,
                        new Rectangle(x, height + (LineHeight - txIncompatibleGame.Height) / 2,
                        txIncompatibleGame.Width, txIncompatibleGame.Height), Color.White);
                    x += txIncompatibleGame.Width + ICON_MARGIN;
                }

                // right-side icons (right game option icons, then password, then skill level)
                int rightX = Width - TextBorderDistance - (scrollBarDrawn ? ScrollBar.Width : 0);

                // right-side game option icons (drawn first, from right to left)
                for (int iconIndex = rightIcons.Count - 1; iconIndex >= 0; iconIndex--)
                {
                    var icon = rightIcons[iconIndex];
                    rightX -= icon.Width;
                    DrawTexture(icon,
                        new Rectangle(rightX, height + (LineHeight - icon.Height) / 2, icon.Width, icon.Height), Color.White);
                    rightX -= ICON_MARGIN;
                }

                // password icon
                if (hostedGame.Passworded)
                {
                    rightX -= txPasswordedGame.Width;
                    DrawTexture(txPasswordedGame,
                        new Rectangle(rightX, height + (LineHeight - txPasswordedGame.Height) / 2, txPasswordedGame.Width, txPasswordedGame.Height),
                        Color.White);
                    rightX -= ICON_MARGIN;
                }

                // skill level icon (shown even if passworded)
                int skillLevelIndex = hostedGame.SkillLevel;
                Texture2D txSkillLevelIcon = txSkillLevelIcons[skillLevelIndex];
                if (txSkillLevelIcon != null)
                {
                    rightX -= txSkillLevelIcon.Width;
                    DrawTexture(txSkillLevelIcon,
                        new Rectangle(rightX, height + (LineHeight - txSkillLevelIcon.Height) / 2, txSkillLevelIcon.Width, txSkillLevelIcon.Height),
                        Color.White);
                }

                var text = lbItem.Text;
                if (hostedGame.IsLoadedGame)
                    text = lbItem.Text + LOADED_GAME_TEXT;

                x += lbItem.TextXPadding;

                DrawStringWithShadow(text, FontIndex,
                    new Vector2(x, height + lbItem.TextYPadding),
                    lbItem.TextColor);

                height += LineHeight;
            }

            if (DrawBorders)
                DrawPanelBorders();

            DrawChildren(gameTime);
        }
    }
}
