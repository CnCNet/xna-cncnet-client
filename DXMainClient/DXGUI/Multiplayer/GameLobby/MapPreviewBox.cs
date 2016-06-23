using Rampastring.XNAUI.DXControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using DTAClient.domain.CnCNet;
using Microsoft.Xna.Framework.Graphics;
using ClientCore;
using DTAClient.domain;
using System.IO;
using Rampastring.Tools;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// The picture box for displaying the map preview.
    /// </summary>
    public class MapPreviewBox : XNAPanel
    {
        const int MAX_STARTING_LOCATIONS = 8;

        public delegate void LocalStartingLocationSelectedEventHandler(object sender, 
            LocalStartingLocationEventArgs e);

        public event EventHandler<LocalStartingLocationEventArgs> LocalStartingLocationSelected;

        public event EventHandler StartingLocationApplied;

        public MapPreviewBox(WindowManager windowManager, 
            List<PlayerInfo> players, List<PlayerInfo> aiPlayers,
            List<MultiplayerColor> mpColors, string[] sides, IniFile gameOptionsIni)
            : base(windowManager)
        {
            this.players = players;
            this.aiPlayers = aiPlayers;
            this.mpColors = mpColors;
            this.sides = sides;
            this.gameOptionsIni = gameOptionsIni;
        }

        Map _map;
        public Map Map
        {
            get { return _map; }
            set
            {
                _map = value;
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

        string[] teamIds = new string[] { String.Empty, "[A] ", "[B] ", "[C] ", "[D] " };

        string[] sides;

        PlayerLocationIndicator[] startingLocationIndicators;

        List<MultiplayerColor> mpColors;
        List<PlayerInfo> players;
        List<PlayerInfo> aiPlayers;

        XNAContextMenu contextMenu;

        Rectangle textureRectangle;

        Texture2D texture;

        bool disposeTextures = true;

        bool useNearestNeighbour = false;

        IniFile gameOptionsIni;

        public override void Initialize()
        {
            disposeTextures = !MCDomainController.Instance.GetMapPreviewPreloadStatus();

            startingLocationIndicators = new PlayerLocationIndicator[MAX_STARTING_LOCATIONS];

            Color nameBackgroundColor = AssetLoader.GetRGBAColorFromString(
                DomainController.Instance().GetMapPreviewNameBackgroundColor());

            Color nameBorderColor = AssetLoader.GetRGBAColorFromString(
                DomainController.Instance().GetMapPreviewNameBorderColor());

            contextMenu = new XNAContextMenu(WindowManager);
            contextMenu.Tag = -1;

            double angularVelocity = gameOptionsIni.GetDoubleValue("General", "StartingLocationAngularVelocity", 0.015);
            double reservedAngularVelocity = gameOptionsIni.GetDoubleValue("General", "ReservedStartingLocationAngularVelocity", -0.0075);

            Color hoverRemapColor = AssetLoader.GetRGBAColorFromString(DomainController.Instance().GetMapPreviewStartingLocationHoverRemapColor());

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
                indicator.ReservedAngularVelocity = reservedAngularVelocity;
                indicator.WaypointTexture = AssetLoader.LoadTexture(string.Format("slocindicator{0}.png", i + 1));
                indicator.Tag = i;
                indicator.LeftClick += Indicator_LeftClick;
                indicator.RightClick += Indicator_RightClick;

                startingLocationIndicators[i] = indicator;

                AddChild(indicator);
            }

            contextMenu.ClientRectangle = new Rectangle(0, 0, 150, 2);
            contextMenu.OptionSelected += ContextMenu_OptionSelected;
            AddChild(contextMenu);
            contextMenu.Enabled = false;
            contextMenu.Visible = false;

            base.Initialize();
        }

        private void ContextMenu_OptionSelected(object sender, ContextMenuOptionEventArgs e)
        {
            if (Map.EnforceMaxPlayers)
            {
                foreach (PlayerInfo pInfo in players.Concat(aiPlayers))
                {
                    if (pInfo.StartingLocation == (int)contextMenu.Tag + 1)
                        pInfo.StartingLocation = 0;
                }
            }

            PlayerInfo player;

            if (e.Index >= players.Count)
            {
                int aiIndex = e.Index - players.Count;
                if (aiIndex >= aiPlayers.Count)
                    return;

                player = aiPlayers[aiIndex];
            }
            else
                player = players[e.Index];

            player.StartingLocation = (int)contextMenu.Tag + 1;

            StartingLocationApplied?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Allows the user to select their starting location by clicking on one of them
        /// in the map preview.
        /// </summary>
        private void Indicator_LeftClick(object sender, EventArgs e)
        {
            var indicator = (PlayerLocationIndicator)sender;

            if (!EnableContextMenu)
            {
                if (Map.EnforceMaxPlayers)
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

            //if (Map.EnforceMaxPlayers)
            //{
            //    foreach (PlayerInfo pInfo in players.Concat(aiPlayers))
            //    {
            //        if (pInfo.StartingLocation == (int)indicator.Tag + 1)
            //            return;
            //    }
            //}

            int x = indicator.ClientRectangle.Right;
            int y = indicator.ClientRectangle.Top;

            if (x + contextMenu.ClientRectangle.Width > ClientRectangle.Width)
                x = indicator.ClientRectangle.Left - contextMenu.ClientRectangle.Width;

            if (y + contextMenu.ClientRectangle.Height > ClientRectangle.Height)
                y = ClientRectangle.Height - contextMenu.ClientRectangle.Height;

            contextMenu.ClientRectangle = new Rectangle(x, y, contextMenu.ClientRectangle.Width, contextMenu.ClientRectangle.Height);
            contextMenu.Tag = indicator.Tag;

            int index = 0;
            foreach (PlayerInfo pInfo in players.Concat(aiPlayers))
            {
                contextMenu.Items[index].Selectable = pInfo.StartingLocation != (int)indicator.Tag + 1 && 
                    pInfo.SideId < sides.Length + 1;
                index++;
            }

            contextMenu.Enabled = true;
            contextMenu.Visible = true;
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
            if (disposeTextures && texture != null && !texture.IsDisposed)
                texture.Dispose();

            if (Map == null)
                return;

            if (Map.PreviewTexture == null)
            {
                texture = Map.LoadPreviewTexture();
            }
            else
                texture = Map.PreviewTexture;

            double xRatio = (ClientRectangle.Width - 2) / (double)texture.Width;
            double yRatio = (ClientRectangle.Height - 2) / (double)texture.Height;

            double ratio;

            int texturePositionX = 1;
            int texturePositionY = 1;
            int textureHeight = 0;
            int textureWidth = 0;

            if (xRatio > yRatio)
            {
                ratio = yRatio;
                textureHeight = ClientRectangle.Height - 2;
                textureWidth = (int)(texture.Width * ratio);
                texturePositionX = (int)(ClientRectangle.Width - 2 - textureWidth) / 2;
            }
            else
            {
                ratio = xRatio;
                textureWidth = ClientRectangle.Width - 2;
                textureHeight = (int)(texture.Height * ratio);
                texturePositionY = (int)(ClientRectangle.Height - 2 - textureHeight) / 2;
            }

            useNearestNeighbour = ratio < 1.0;

            Rectangle displayRectangle = WindowRectangle();

            textureRectangle = new Rectangle(displayRectangle.X + texturePositionX, displayRectangle.Y + texturePositionY,
                textureWidth, textureHeight);

            for (int i = 0; i < Map.MaxPlayers; i++)
            {
                PlayerLocationIndicator indicator = startingLocationIndicators[i];

                Point location = new Point(
                    texturePositionX + (int)(Map.StartingLocations[i].X * ratio),
                    texturePositionY + (int)(Map.StartingLocations[i].Y * ratio));

                indicator.SetPosition(location);
                indicator.Enabled = true;
                indicator.Visible = true;
            }

            for (int i = Map.MaxPlayers; i < MAX_STARTING_LOCATIONS; i++)
            {
                startingLocationIndicators[i].Enabled = false;
                startingLocationIndicators[i].Visible = false;
            }
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

            foreach (PlayerInfo pInfo in players.Concat(aiPlayers))
            {
                string text = pInfo.Name;

                if (pInfo.TeamId > 0)
                {
                    text = teamIds[pInfo.TeamId] + text;
                }

                contextMenu.AddItem(id + ". " + text,
                    pInfo.ColorId > 0 ? mpColors[pInfo.ColorId - 1].XnaColor : Color.White);

                id++;
            }
        }

        public override void OnMouseEnter()
        {
            foreach (PlayerLocationIndicator indicator in startingLocationIndicators)
                indicator.BackgroundShown = true;

            base.OnMouseEnter();
        }

        public override void OnMouseLeave()
        {
            foreach (PlayerLocationIndicator indicator in startingLocationIndicators)
                indicator.BackgroundShown = false;

            base.OnMouseLeave();
        }

        public override void OnLeftClick()
        {
            if (Keyboard.IsKeyHeldDown(Keys.LeftControl))
            {
                Process.Start(ProgramConstants.GamePath + Map.PreviewPath);
            }

            base.OnLeftClick();
        }

        public override void Draw(GameTime gameTime)
        {
            if (useNearestNeighbour)
            {
                Renderer.EndDraw();
                Renderer.BeginDraw(SamplerState.PointClamp);
            }

            DrawPanel();

            if (texture != null)
                Renderer.DrawTexture(texture, textureRectangle, Color.White);

            if (useNearestNeighbour)
            {
                Renderer.EndDraw();
                Renderer.BeginDraw();
            }

            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].Visible)
                {
                    Children[i].Draw(gameTime);
                }
            }
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