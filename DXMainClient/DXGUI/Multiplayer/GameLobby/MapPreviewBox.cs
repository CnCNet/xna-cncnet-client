using ClientCore;
using DTAClient.Domain.Multiplayer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ClientGUI;
using Localization;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    struct ExtraMapPreviewTexture
    {
        public Texture2D Texture;
        public Point Point;
        public bool Toggleable;

        public ExtraMapPreviewTexture(Texture2D texture, Point point, bool toggleable)
        {
            Texture = texture;
            Point = point;
            Toggleable = toggleable;
        }
    }

    /// <summary>
    /// The picture box for displaying the map preview.
    /// </summary>
    public class MapPreviewBox : XNAPanel
    {
        private const int MAX_STARTING_LOCATIONS = 8;

        public delegate void LocalStartingLocationSelectedEventHandler(object sender, 
            LocalStartingLocationEventArgs e);

        public event EventHandler<LocalStartingLocationEventArgs> LocalStartingLocationSelected;

        public event EventHandler StartingLocationApplied;

        public MapPreviewBox(WindowManager windowManager) : base(windowManager)
        {
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            FontIndex = 1;
        }


        public void SetFields(List<PlayerInfo> players, List<PlayerInfo> aiPlayers, List<MultiplayerColor> mpColors, string[] sides, IniFile gameOptionsIni)
        {
            this.players = players;
            this.aiPlayers = aiPlayers;
            this.mpColors = mpColors;
            this.sides = sides;
            this.gameOptionsIni = gameOptionsIni;

            Color nameBackgroundColor = AssetLoader.GetRGBAColorFromString(
                ClientConfiguration.Instance.MapPreviewNameBackgroundColor);

            Color nameBorderColor = AssetLoader.GetRGBAColorFromString(
                ClientConfiguration.Instance.MapPreviewNameBorderColor);

            double angularVelocity = gameOptionsIni.GetDoubleValue("General", "StartingLocationAngularVelocity", 0.015);
            double reservedAngularVelocity = gameOptionsIni.GetDoubleValue("General", "ReservedStartingLocationAngularVelocity", -0.0075);

            Color hoverRemapColor = AssetLoader.GetRGBAColorFromString(ClientConfiguration.Instance.MapPreviewStartingLocationHoverRemapColor);

            startingLocationIndicators = new PlayerLocationIndicator[MAX_STARTING_LOCATIONS];
            // Init starting location indicators
            for (int i = 0; i < MAX_STARTING_LOCATIONS; i++)
            {
                PlayerLocationIndicator indicator = new PlayerLocationIndicator(WindowManager, mpColors,
                    nameBackgroundColor, nameBorderColor, contextMenu);
                indicator.FontIndex = FontIndex;
                indicator.Visible = false;
                indicator.Enabled = false;
                indicator.AngularVelocity = angularVelocity;
                indicator.HoverRemapColor = hoverRemapColor;
                indicator.ReversedAngularVelocity = reservedAngularVelocity;
                indicator.WaypointTexture = AssetLoader.LoadTexture(string.Format("slocindicator{0}.png", i + 1));
                indicator.Tag = i;
                indicator.LeftClick += Indicator_LeftClick;
                indicator.RightClick += Indicator_RightClick;

                startingLocationIndicators[i] = indicator;

                AddChild(indicator);
            }

            briefingBox = new CoopBriefingBox(WindowManager);
            AddChild(briefingBox);
            briefingBox.Disable();

            ClientRectangleUpdated += (s, e) => UpdateMap();
        }


        private GameModeMap _gameModeMap;
        public GameModeMap GameModeMap
        {
            get => _gameModeMap;
            set
            {
                _gameModeMap = value;
                UpdateMap();
            }
        }

        public int FontIndex { get; set; }

        /// <summary>
        /// Controls whether the context menu is enabled for this map preview box.
        /// Skirmish games and online games where the local player is the host should
        /// set have this set to true.
        /// </summary>
        public bool EnableContextMenu { get; set; }
        public bool EnableStartLocationSelection { get; set; }

        private readonly string[] teamIds = new[] { string.Empty }
            .Concat(ProgramConstants.TEAMS.Select(team => $"[{team}]")).ToArray();

        private string[] sides;

        public int RandomSelectorCount { get; set; }

        private PlayerLocationIndicator[] startingLocationIndicators;

        private List<MultiplayerColor> mpColors;
        private List<PlayerInfo> players;
        private List<PlayerInfo> aiPlayers;

        private XNAContextMenu mainContextMenu;
        private XNAContextMenu contextMenu;
        private Point lastContextMenuPoint;

        private XNAContextMenu mapContextMenu;
        private XNAContextMenuItem toggleFavoriteMapItem;
        private XNAContextMenuItem toggleExtraTexturesItem;
        private XNAClientButton btnToggleFavoriteMap;
        private XNAClientButton btnToggleExtraTextures;

        private CoopBriefingBox briefingBox;

        private Rectangle textureRectangle;

        private Texture2D previewTexture;

        private bool disposeTextures = true;

        private bool useNearestNeighbour = false;

        private IniFile gameOptionsIni;

        private EnhancedSoundEffect sndClickSound;

        private EnhancedSoundEffect sndDropdownSound;

        private List<ExtraMapPreviewTexture> extraTextures = new List<ExtraMapPreviewTexture>(0);

        public EventHandler ToggleFavorite;

        public override void Initialize()
        {
            EnableStartLocationSelection = true;

            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);

#if !WINDOWSGL
            disposeTextures = !UserINISettings.Instance.PreloadMapPreviews;
#endif

            mainContextMenu = new XNAContextMenu(WindowManager);
            mainContextMenu.Name = nameof(mainContextMenu);
            mainContextMenu.ClientRectangle = new Rectangle(0, 0, 150, 2);
            mainContextMenu.Disable();
            AddChild(mainContextMenu);

            contextMenu = new XNAContextMenu(WindowManager);
            contextMenu.Tag = -1;
            contextMenu.ClientRectangle = new Rectangle(0, 0, 150, 2);
            AddChild(contextMenu);
            contextMenu.Disable();

            toggleFavoriteMapItem = new XNAContextMenuItem()
            {
                Text = "Add Favorite".L10N("UI:Main:AddFavorite"),
                SelectAction = ToggleFavoriteMap,
                SelectableChecker = () => GameModeMap != null
            };
            toggleExtraTexturesItem = new XNAContextMenuItem()
            {
                Text = "Hide Extra Icons".L10N("UI:Main:HideExtraIcons"),
                SelectAction = ToggleExtraTextures,
                SelectableChecker = () => GameModeMap != null,
                VisibilityChecker = () => extraTextures.Any(x => x.Toggleable)
            };
            mapContextMenu = new XNAContextMenu(WindowManager);
            mapContextMenu.ClientRectangle = new Rectangle(0, 0, 120, 2);
            mapContextMenu.AddItem(toggleFavoriteMapItem);
            mapContextMenu.AddItem(toggleExtraTexturesItem);

            btnToggleFavoriteMap = new XNAClientButton(WindowManager);
            btnToggleFavoriteMap.IdleTexture = AssetLoader.LoadTexture("favInactive.png");
            btnToggleFavoriteMap.LeftClick += (sender, args) => ToggleFavorite?.Invoke(sender, args);
            btnToggleFavoriteMap.SetToolTipText("Toggle Favorite Map".L10N("UI:Main:ToggleFavoriteMap"));

            btnToggleExtraTextures = new XNAClientButton(WindowManager);
            btnToggleExtraTextures.IdleTexture = AssetLoader.LoadTexture("pvTexturesActive.png");
            btnToggleExtraTextures.LeftClick += (sender, args) => ToggleExtraTextures();
            btnToggleExtraTextures.SetToolTipText("Toggle Extra Icons".L10N("UI:Main:ToggleExtraIcons"));
            btnToggleExtraTextures.Disable();

            AddChild(mapContextMenu);
            mapContextMenu.Disable();

            sndClickSound = new EnhancedSoundEffect("button.wav");

            sndDropdownSound = new EnhancedSoundEffect("dropdown.wav");

            base.Initialize();

            ClientRectangleUpdated += (s, e) => UpdateMap();

            RightClick += MapPreviewBox_RightClick;

            AddChild(btnToggleFavoriteMap);
            AddChild(btnToggleExtraTextures);
        }

        private void MapPreviewBox_RightClick(object sender, EventArgs e)
        {
            if (GameModeMap == null)
                return;

            toggleFavoriteMapItem.Text = GameModeMap.IsFavorite ? "Remove Favorite".L10N("UI:Main:RemoveFavorite") : "Add Favorite".L10N("UI:Main:AddFavorite");
            toggleExtraTexturesItem.Text = UserINISettings.Instance.DisplayToggleableExtraTextures ?
                "Hide Extra Icons".L10N("UI:Main:HideExtraIcons") : "Show Extra Icons".L10N("UI:Main:ShowExtraIcons");

            mapContextMenu.Open(GetCursorPoint());
        }

        private void ToggleFavoriteMap()
        {
            ToggleFavorite?.Invoke(null, null);
        }

        private void ToggleExtraTextures()
        {
            UserINISettings.Instance.DisplayToggleableExtraTextures.Value =
                !UserINISettings.Instance.DisplayToggleableExtraTextures;

            RefreshExtraTexturesBtn();
        }

        private void ContextMenu_OptionSelected(int index)
        {
            SoundPlayer.Play(sndDropdownSound);

            if (GameModeMap.Map.EnforceMaxPlayers)
            {
                foreach (PlayerInfo pInfo in players.Concat(aiPlayers))
                {
                    if (pInfo.StartingLocation == (int)contextMenu.Tag + 1)
                        pInfo.StartingLocation = 0;
                }
            }

            PlayerInfo player;

            if (index >= players.Count)
            {
                int aiIndex = index - players.Count;
                if (aiIndex >= aiPlayers.Count)
                    return;

                player = aiPlayers[aiIndex];
            }
            else
                player = players[index];

            player.StartingLocation = (int)contextMenu.Tag + 1;

            StartingLocationApplied?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Allows the user to select their starting location by clicking on one of them
        /// in the map preview.
        /// </summary>
        private void Indicator_LeftClick(object sender, EventArgs e)
        {
            if (!EnableStartLocationSelection) return;

            var indicator = (PlayerLocationIndicator)sender;

            SoundPlayer.Play(sndClickSound);

            if (!EnableContextMenu)
            {
                if (GameModeMap.Map.EnforceMaxPlayers)
                {
                    foreach (PlayerInfo pInfo in players.Concat(aiPlayers))
                    {
                        if (pInfo.StartingLocation == (int)indicator.Tag + 1)
                            return;
                    }
                }

                LocalStartingLocationSelected?.Invoke(this, new LocalStartingLocationEventArgs((int)indicator.Tag + 1));
                return;
            }

            //if (contextMenu.Visible)
            //{
            //    contextMenu.Visible = false;
            //    contextMenu.Enabled = false;
            //    return;
            //}

            //if (Map.EnforceMaxPlayers)
            //{
            //    foreach (PlayerInfo pInfo in players.Concat(aiPlayers))
            //    {
            //        if (pInfo.StartingLocation == (int)indicator.Tag + 1)
            //            return;
            //    }
            //}

            int x = indicator.Right;
            int y = indicator.Y;

            if (x + contextMenu.Width > Width)
                x = indicator.X - contextMenu.Width;

            if (y + contextMenu.Height > Height)
                y = Height - contextMenu.Height;

            contextMenu.Tag = indicator.Tag;

            int index = 0;
            foreach (PlayerInfo pInfo in players.Concat(aiPlayers))
            {
                contextMenu.Items[index].Selectable = pInfo.StartingLocation != (int)indicator.Tag + 1 && 
                    pInfo.SideId < sides.Length + RandomSelectorCount;
                index++;
            }
            lastContextMenuPoint = new Point(x, y);
            contextMenu.Open(lastContextMenuPoint);
        }

        private void Indicator_RightClick(object sender, EventArgs e)
        {
            var indicator = (PlayerLocationIndicator)sender;

            if (!EnableContextMenu)
            {
                PlayerInfo pInfo = players.Find(p => p.Name == ProgramConstants.PLAYERNAME);

                if (pInfo.StartingLocation == (int)indicator.Tag + 1)
                {
                    LocalStartingLocationSelected?.Invoke(this, new LocalStartingLocationEventArgs(0));
                }

                return;
            }       

            foreach (PlayerInfo pInfo in players.Union(aiPlayers))
            {
                if (pInfo.StartingLocation == (int)indicator.Tag + 1)
                    pInfo.StartingLocation = 0;
            }

            StartingLocationApplied?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Updates the map preview texture's position inside
        /// this control's display rectangle and the 
        /// starting location indicators' positions.
        /// </summary>
        private void UpdateMap()
        {
            if (disposeTextures && previewTexture != null && !previewTexture.IsDisposed)
                previewTexture.Dispose();

            extraTextures.Clear();

            if (GameModeMap == null)
            {
                previewTexture = null;
                briefingBox.Disable();

                contextMenu.Disable();

                foreach (var indicator in startingLocationIndicators)
                    indicator.Disable();

                return;
            }

            if (GameModeMap.Map.PreviewTexture == null)
            {
                previewTexture = GameModeMap.Map.LoadPreviewTexture();
                disposeTextures = true;
            }
            else
            {
                previewTexture = GameModeMap.Map.PreviewTexture;
                disposeTextures = false;
            }

            if (!string.IsNullOrEmpty(GameModeMap.Map.Briefing))
            {
                briefingBox.SetText(GameModeMap.Map.Briefing);
                briefingBox.Enable();
                if (IsActive)
                    briefingBox.SetAlpha(0f);
            }
            else
                briefingBox.Disable();

            double xRatio = (Width - 2) / (double)previewTexture.Width;
            double yRatio = (Height - 2) / (double)previewTexture.Height;

            double ratio;

            int texturePositionX = 1;
            int texturePositionY = 1;
            int textureHeight = 0;
            int textureWidth = 0;

            if (xRatio > yRatio)
            {
                ratio = yRatio;
                textureHeight = Height - 2;
                textureWidth = (int)(previewTexture.Width * ratio);
                texturePositionX = (int)(Width - 2 - textureWidth) / 2;
            }
            else
            {
                ratio = xRatio;
                textureWidth = Width - 2;
                textureHeight = (int)(previewTexture.Height * ratio);
                texturePositionY = (Height - 2 - textureHeight) / 2 + 1;
            }

            useNearestNeighbour = ratio < 1.0;

            textureRectangle = new Rectangle(texturePositionX, texturePositionY,
                textureWidth, textureHeight);

            List<Point> startingLocations = GameModeMap.Map.GetStartingLocationPreviewCoords(new Point(previewTexture.Width, previewTexture.Height));

            for (int i = 0; i < startingLocations.Count && i < GameModeMap.Map.MaxPlayers; i++)
            {
                PlayerLocationIndicator indicator = startingLocationIndicators[i];

                Point location = new Point(
                    texturePositionX + (int)(startingLocations[i].X * ratio),
                    texturePositionY + (int)(startingLocations[i].Y * ratio));

                indicator.SetPosition(location);
                indicator.Enabled = true;
                indicator.Visible = true;
            }

            for (int i = startingLocations.Count; i < MAX_STARTING_LOCATIONS; i++)
            {
                startingLocationIndicators[i].Disable();
            }


            foreach (var mapExtraTexture in GameModeMap.Map.GetExtraMapPreviewTextures())
            {
                // LoadTexture makes use of a texture cache 
                // so we don't need to cache the textures manually
                Texture2D extraTexture = AssetLoader.LoadTexture(mapExtraTexture.TextureName);
                Point location = PreviewTexturePointToControlAreaPoint(
                    GameModeMap.Map.MapPointToMapPreviewPoint(mapExtraTexture.Point,
                    new Point(previewTexture.Width - (extraTexture.Width / 2),
                              previewTexture.Height - (extraTexture.Height / 2)), mapExtraTexture.Level),
                              ratio);

                extraTextures.Add(new ExtraMapPreviewTexture(extraTexture, location, mapExtraTexture.Toggleable));
            }

            int buttonX = Width;

            if (extraTextures.Any(x => x.Toggleable))
            {
                btnToggleExtraTextures.ClientRectangle = new Rectangle(buttonX - 22, 4, 18, 18);
                btnToggleExtraTextures.Enable();
                buttonX = btnToggleExtraTextures.X;
            }
            else
            {
                btnToggleExtraTextures.Disable();
            }

            btnToggleFavoriteMap.ClientRectangle = new Rectangle(buttonX - 22, 4, 18, 18);

            RefreshExtraTexturesBtn();
            RefreshFavoriteBtn();
        }

        public void RefreshFavoriteBtn()
        {
            bool isFav = UserINISettings.Instance.IsFavoriteMap(GameModeMap?.Map.Name, GameModeMap?.GameMode.Name);
            var textureName = isFav ? "favActive.png" : "favInactive.png";
            var hoverTextureName = isFav ? "favActive_c.png" : "favInactive_c.png";
            var hoverTexture = AssetLoader.AssetExists(hoverTextureName) ? AssetLoader.LoadTexture(hoverTextureName) : null;
            btnToggleFavoriteMap.IdleTexture = AssetLoader.LoadTexture(textureName);
            btnToggleFavoriteMap.HoverTexture = hoverTexture;
        }


        public void RefreshExtraTexturesBtn()
        {
            var textureName = UserINISettings.Instance.DisplayToggleableExtraTextures ? "pvTexturesActive.png" : "pvTexturesInactive.png";
            var hoverTextureName = UserINISettings.Instance.DisplayToggleableExtraTextures ? "pvTexturesActive_c.png" : "pvTexturesInactive_c.png";
            var hoverTexture = AssetLoader.AssetExists(hoverTextureName) ? AssetLoader.LoadTexture(hoverTextureName) : null;
            btnToggleExtraTextures.IdleTexture = AssetLoader.LoadTexture(textureName);
            btnToggleExtraTextures.HoverTexture = hoverTexture;
        }

        private Point PreviewTexturePointToControlAreaPoint(Point previewTexturePoint, double scaleRatio)
        {
            return new Point(textureRectangle.X + (int)(previewTexturePoint.X * scaleRatio),
                textureRectangle.Y + (int)(previewTexturePoint.Y * scaleRatio));
        }

        public void UpdateStartingLocationTexts()
        {
            foreach (PlayerLocationIndicator indicator in startingLocationIndicators)
                indicator.Players.Clear();

            foreach (PlayerInfo pInfo in players)
            {
                if (pInfo.StartingLocation > 0)
                    startingLocationIndicators[pInfo.StartingLocation - 1].Players.Add(pInfo);
            }

            foreach (PlayerInfo aiInfo in aiPlayers)
            {
                if (aiInfo.StartingLocation > 0)
                    startingLocationIndicators[aiInfo.StartingLocation - 1].Players.Add(aiInfo);
            }

            foreach (PlayerLocationIndicator indicator in startingLocationIndicators)
                indicator.Refresh();

            contextMenu.ClearItems();

            int id = 1;
            var playerList = players.Concat(aiPlayers).ToList();

            for (int i = 0; i < playerList.Count; i++)
            {
                PlayerInfo pInfo = playerList[i];

                string text = pInfo.Name;

                if (pInfo.TeamId > 0)
                {
                    text = teamIds[pInfo.TeamId] + " " + text;
                }

                int index = i;
                XNAContextMenuItem item = new XNAContextMenuItem()
                {
                    Text = id + ". " + text,
                    TextColor = pInfo.ColorId > 0 ? mpColors[pInfo.ColorId - 1].XnaColor : Color.White,
                    SelectAction = () => ContextMenu_OptionSelected(index),
                };
                contextMenu.AddItem(item);

                id++;
            }

            if (EnableContextMenu && contextMenu.Enabled && contextMenu.Visible)
            {
                contextMenu.Disable();
                contextMenu.Open(lastContextMenuPoint);
            }
        }

        public override void OnMouseEnter()
        {
            foreach (PlayerLocationIndicator indicator in startingLocationIndicators)
                indicator.BackgroundShown = true;

            if (GameModeMap != null && !string.IsNullOrEmpty(GameModeMap.Map.Briefing))
            {
                briefingBox.SetFadeVisibility(false);
            }
            else
                briefingBox.Disable();

            base.OnMouseEnter();
        }

        public override void OnMouseLeave()
        {
            foreach (PlayerLocationIndicator indicator in startingLocationIndicators)
                indicator.BackgroundShown = false;

            if (GameModeMap != null && !string.IsNullOrEmpty(GameModeMap.Map.Briefing))
            {
                briefingBox.SetText(GameModeMap.Map.Briefing);
                briefingBox.SetFadeVisibility(true);
            }

            base.OnMouseLeave();
        }

        public override void OnLeftClick()
        {
            if (Keyboard.IsKeyHeldDown(Keys.LeftControl))
            {
                if (File.Exists(ProgramConstants.GamePath + GameModeMap.Map.PreviewPath))
                    Process.Start(ProgramConstants.GamePath + GameModeMap.Map.PreviewPath);
            }

            base.OnLeftClick();
        }

        public override void Draw(GameTime gameTime)
        {
            DrawPanel();

            if (previewTexture != null)
            {
                Point renderPoint = GetRenderPoint();

                if (useNearestNeighbour)
                {
                    Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, null, SamplerState.PointClamp));
                    DrawPreviewTexture(renderPoint);
                    Renderer.PopSettings();
                }
                else
                {
                    DrawPreviewTexture(renderPoint);
                }

                if (DrawBorders)
                    DrawPanelBorders();

                foreach (var extraTexture in extraTextures)
                {
                    if (!extraTexture.Toggleable || UserINISettings.Instance.DisplayToggleableExtraTextures)
                    {
                        Renderer.DrawTexture(extraTexture.Texture,
                            new Rectangle(renderPoint.X + extraTexture.Point.X,
                            renderPoint.Y + extraTexture.Point.Y,
                            extraTexture.Texture.Width, extraTexture.Texture.Height), Color.White);
                    }
                }
            }
            else if (DrawBorders)
            {
                DrawPanelBorders();
            }

            DrawChildren(gameTime);
        }

        private void DrawPreviewTexture(Point renderPoint)
        {
            Renderer.DrawTexture(previewTexture,
                new Rectangle(renderPoint.X + textureRectangle.X,
                renderPoint.Y + textureRectangle.Y,
                textureRectangle.Width, textureRectangle.Height),
                Color.White);
        }
    }

    public class LocalStartingLocationEventArgs : EventArgs
    {
        public LocalStartingLocationEventArgs(int startingLocationIndex)
        {
            StartingLocationIndex = startingLocationIndex;
        }

        public int StartingLocationIndex { get; set; }
    }
}
