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
            List<MultiplayerColor> mpColors)
            : base(windowManager)
        {
            this.players = players;
            this.aiPlayers = aiPlayers;
            this.mpColors = mpColors;
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

        Rectangle textureRectangle;

        Texture2D texture;

        bool disposeTextures = true;

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
                if (File.Exists(ProgramConstants.GamePath + Map.PreviewPath))
                    texture = AssetLoader.LoadTextureUncached(ProgramConstants.GamePath + Map.PreviewPath);
                else
                    texture = AssetLoader.LoadTexture("nopreview.png");
            }
            else
                texture = Map.PreviewTexture;

            double xyRatio = texture.Width / (double)texture.Height;
            double intendedRatio = ClientRectangle.Width / (double)ClientRectangle.Height;

            double ratioDifference = xyRatio - intendedRatio;

            int texturePositionX = 0;
            int texturePositionY = 0;

            double scaleRatio = 0.0;

            if (ratioDifference > 0.0)
            {
                texturePositionX = (int)(ratioDifference * texture.Height) / 2;
                scaleRatio = texture.Height / (double)ClientRectangle.Height;
            }
            else
            {
                texturePositionY = (int)(-ratioDifference * texture.Width) / 2;
                scaleRatio = texture.Width / (double)ClientRectangle.Width;
            }

            textureRectangle = new Rectangle(texturePositionX, texturePositionY,
                texture.Width, texture.Height);

            for (int i = 0; i < Map.MaxPlayers; i++)
            {
                DXPanel indicator = startingLocationIndicators[i];

                Point location = new Point(
                    (int)(Map.StartingLocations[i].X * scaleRatio),
                    (int)(Map.StartingLocations[i].Y * scaleRatio));

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

        private void UpdateStartingLocationTexts()
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
            if (texture != null)
                Renderer.DrawTexture(texture, textureRectangle, Color.White);

            Vector2 textSize = Renderer.GetTextDimensions("@", FontIndex);

            for (int i = 0; i < startingLocationIndicators.Length; i++)
            {
                if (!startingLocationIndicators[i].Visible)
                    continue;

                int y = startingLocationIndicators[i].ClientRectangle.Y +
                    (startingLocationIndicators[i].ClientRectangle.Height - (int)textSize.Y) / 2;
                foreach (PlayerInfo pInfo in playersOnStartingLocations[i])
                {
                    Color remapColor = Color.White;
                    if (pInfo.ColorId > 0)
                        remapColor = mpColors[pInfo.ColorId - 1].XnaColor;

                    Renderer.DrawStringWithShadow(pInfo.Name, FontIndex,
                        new Vector2(startingLocationIndicators[i].ClientRectangle.Right + 3,
                        y), remapColor);
                    y += (int)textSize.Y + 3;
                }
            }

            base.Draw(gameTime);
        }
    }

    public class StartingLocationEventArgs : EventArgs
    {
        public StartingLocationEventArgs(int startingLocationIndex)
        {
            StartingLocationIndex = startingLocationIndex;
        }

        int StartingLocationIndex { get; set; }
    }
}