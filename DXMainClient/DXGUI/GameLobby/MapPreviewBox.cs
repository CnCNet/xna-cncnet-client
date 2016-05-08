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

namespace DTAClient.DXGUI.GameLobby
{
    /// <summary>
    /// The picture box for displaying the map preview.
    /// </summary>
    public class MapPreviewBox : DXPanel
    {
        const int MAX_STARTING_LOCATIONS = 8;

        public delegate void StartingLocationSelectedEventHandler(object sender, 
            StartingLocationEventArgs e);

        public event StartingLocationSelectedEventHandler StartingLocationSelected;

        public MapPreviewBox(WindowManager windowManager, 
            List<PlayerInfo> players, List<PlayerInfo> aiPlayers,
            List<MultiplayerColor> mpColors, string[] sides)
            : base(windowManager)
        {
            this.players = players;
            this.aiPlayers = aiPlayers;
            this.mpColors = mpColors;

            sideTextures = new Texture2D[sides.Length + 1];
            for (int i = 1; i <= sides.Length; i++)
                sideTextures[i] = AssetLoader.LoadTexture(sides[i - 1] + "icon.png");
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

        List<MultiplayerColor> mpColors;
        List<PlayerInfo> players;
        List<PlayerInfo> aiPlayers;

        DXPanel[] startingLocationIndicators;
        List<PlayerInfo>[] playersOnStartingLocations;

        Texture2D[] sideTextures;

        string[] teamIds = new string[] { String.Empty, "[A] ", "[B] ", "[C] ", "[D] " };

        Rectangle textureRectangle;

        Texture2D texture;

        bool disposeTextures = true;

        bool useNearestNeighbour = false;

        public override void Initialize()
        {
            startingLocationIndicators = new DXPanel[MAX_STARTING_LOCATIONS];

            disposeTextures = !MCDomainController.Instance.GetMapPreviewPreloadStatus();

            playersOnStartingLocations = new List<PlayerInfo>[MAX_STARTING_LOCATIONS];

            // Init starting location indicators
            for (int i = 0; i < MAX_STARTING_LOCATIONS; i++)
            {
                DXPanel indicator = new DXPanel(WindowManager);
                indicator.Name = "startingLocationIndicator" + i;
                indicator.DrawBorders = false;
                indicator.BackgroundTexture = AssetLoader.LoadTexture(string.Format("slocindicator{0}.png", i + 1));
                indicator.ClientRectangle = indicator.BackgroundTexture.Bounds;
                indicator.Tag = i;
                indicator.LeftClick += Indicator_LeftClick;
                indicator.Visible = false;
                indicator.Enabled = false;

                playersOnStartingLocations[i] = new List<PlayerInfo>();
                startingLocationIndicators[i] = indicator;

                AddChild(indicator);
            }

            base.Initialize();
        }

        /// <summary>
        /// Allows the user to select their starting location by clicking on one of them
        /// in the map preview.
        /// </summary>
        private void Indicator_LeftClick(object sender, EventArgs e)
        {
            DXPanel indicator = (DXPanel)sender;

            StartingLocationSelected?.Invoke(this,
                new StartingLocationEventArgs((int)indicator.Tag));
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
                DXPanel indicator = startingLocationIndicators[i];

                Point location = new Point(
                    texturePositionX + (int)(Map.StartingLocations[i].X * ratio),
                    texturePositionY + (int)(Map.StartingLocations[i].Y * ratio));

                indicator.ClientRectangle = new Rectangle(location, indicator.ClientRectangle.Size);
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
            foreach (List<PlayerInfo> list in playersOnStartingLocations)
                list.Clear();

            foreach (PlayerInfo pInfo in players)
            {
                if (pInfo.StartingLocation > 0)
                    playersOnStartingLocations[pInfo.StartingLocation - 1].Add(pInfo);
            }

            foreach (PlayerInfo aiInfo in aiPlayers)
            {
                if (aiInfo.StartingLocation > 0)
                    playersOnStartingLocations[aiInfo.StartingLocation - 1].Add(aiInfo);
            }
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

            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].Visible)
                {
                    Children[i].Draw(gameTime);
                }
            }

            Vector2 textSize = Renderer.GetTextDimensions("@", FontIndex);

            for (int i = 0; i < startingLocationIndicators.Length; i++)
            {
                DXPanel indicator = startingLocationIndicators[i];

                if (!indicator.Visible)
                    continue;

                indicator.Draw(gameTime);

                Rectangle displayRectangle = indicator.WindowRectangle();

                int y = displayRectangle.Y +
                    (indicator.ClientRectangle.Height - (int)textSize.Y) / 2;

                foreach (PlayerInfo pInfo in playersOnStartingLocations[i])
                {
                    Color remapColor = Color.White;
                    if (pInfo.ColorId > 0)
                        remapColor = mpColors[pInfo.ColorId - 1].XnaColor;

                    string text = teamIds[pInfo.TeamId] + pInfo.Name;

                    int textXPosition = 3;

                    if (pInfo.SideId < sideTextures.Length && pInfo.SideId > 0)
                    {
                        Texture2D sideTexture = sideTextures[pInfo.SideId];

                        Vector2 playerTextSize = Renderer.GetTextDimensions(text, FontIndex);

                        Renderer.DrawTexture(sideTexture,
                            new Rectangle(displayRectangle.Right + textXPosition + (int)playerTextSize.X + 2,
                            y, sideTexture.Width, sideTexture.Height), Color.White);
                    }

                    Renderer.DrawStringWithShadow(text, FontIndex,
                        new Vector2(displayRectangle.Right + textXPosition,
                        y), remapColor);
                    y += (int)textSize.Y + 3;
                }
            }

            if (useNearestNeighbour)
            {
                Renderer.EndDraw();
                Renderer.BeginDraw();
            }
        }
    }

    public class StartingLocationEventArgs : EventArgs
    {
        public StartingLocationEventArgs(int startingLocationIndex)
        {
            StartingLocationIndex = startingLocationIndex;
        }

        public int StartingLocationIndex { get; set; }
    }
}