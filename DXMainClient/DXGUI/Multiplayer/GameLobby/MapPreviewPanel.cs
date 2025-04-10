using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClientCore.Extensions;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

using DTAClient.Domain.Multiplayer;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{

    /// <summary>
    /// A UI panel that displays information about a hosted CnCNet or LAN game.
    /// </summary>
    public class MapPreviewPanel : XNAPanel
    {
        public MapPreviewPanel(WindowManager windowManager, MapLoader mapLoader)
            : base(windowManager)
        {
            this.mapLoader = mapLoader;
            DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
        }

        private MapLoader mapLoader;

        private XNALabel lblMapPreview;
        private XNALabel lblMapName;
        private XNALabel lblMapMaxPlayers;

        private GameModeMap map = null;

        private bool disposeTextures = false;
        private Texture2D mapTexture = null;
        private Texture2D noMapPreviewTexture = null;

        private int mapPreviewPositionY = 0;
        private const int initialPanelHeight = 200;
        private const int initialPanelWidth = 235;
        private const int maxPreviewHeight = 150;
        private const int mapPreviewMargin = 1; //border
        private const int padding = 6;

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, initialPanelWidth, initialPanelHeight);
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 255), 1, 1);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            lblMapPreview = new XNALabel(WindowManager);
            lblMapPreview.FontIndex = 1;
            lblMapPreview.Text = "MAP PREVIEW".L10N("Client:Main:MapPreview");
            AddChild(lblMapPreview);

            lblMapName = new XNALabel(WindowManager);
            lblMapName.FontIndex = 1;
            lblMapName.Text = "";
            AddChild(lblMapName);

            lblMapMaxPlayers = new XNALabel(WindowManager);
            lblMapMaxPlayers.FontIndex = 1;
            lblMapMaxPlayers.Text = "";
            AddChild(lblMapMaxPlayers);

            if (AssetLoader.AssetExists("noMapPreview.png"))
                noMapPreviewTexture = AssetLoader.LoadTexture("noMapPreview.png");

            lblMapPreview.CenterOnParent();
            lblMapPreview.ClientRectangle = new Rectangle(lblMapPreview.X, padding,
                lblMapPreview.Width, lblMapPreview.Height);
            lblMapName.ClientRectangle = new Rectangle(padding, lblMapPreview.Y + lblMapPreview.Height + padding,
                lblMapName.Width, lblMapName.Height);
            lblMapMaxPlayers.ClientRectangle = new Rectangle(padding, lblMapName.Y + lblMapName.Height + padding,
                lblMapMaxPlayers.Width, lblMapMaxPlayers.Height);

            base.Initialize();
        }

        public void SetInfo(GameModeMap map)
        {
            ClearInfo();

            this.map = map;

            lblMapPreview.Visible = true;
            lblMapName.Text = map.Map.Name;
            lblMapName.Visible = true;
            lblMapMaxPlayers.Text = "Max players: ".L10N("Client:Main:MaxPlayers") + map.Map.MaxPlayers.ToString();
            lblMapMaxPlayers.Y = lblMapName.Y + lblMapName.Height + padding;
            lblMapMaxPlayers.Visible = true;

            mapPreviewPositionY = lblMapMaxPlayers.Y + lblMapMaxPlayers.Height + padding;
            this.Height = mapPreviewPositionY + maxPreviewHeight + padding;
            if (mapLoader != null && map != null)
            {
                mapTexture = map.Map.IsPreviewTextureCached() ? map.Map.LoadPreviewTexture() : null;
                if (mapTexture == null && noMapPreviewTexture != null)
                {
                    Debug.Assert(!noMapPreviewTexture.IsDisposed, "noMapPreviewTexture should not be disposed.");
                    mapTexture = noMapPreviewTexture;
                    disposeTextures = false;
                }
                else
                {
                    disposeTextures = true;
                }
            }
        }

        public void ClearInfo()
        {
            lblMapPreview.Visible = false;
            lblMapName.Visible = false;
            lblMapMaxPlayers.Visible = false;

            if (mapTexture != null && disposeTextures)
            {
                Debug.Assert(!mapTexture.IsDisposed, "mapTexture should not be disposed.");
                mapTexture.Dispose();
                mapTexture = null;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (map != null && mapTexture != null)
                RenderMapPreview();
        }

        private void RenderMapPreview()
        {
            // Calculate map preview area 
            double xRatio = (ClientRectangle.Width - (mapPreviewMargin*2)) / (double)mapTexture.Width;
            double yRatio = (ClientRectangle.Height - mapPreviewPositionY) / (double)mapTexture.Height;

            double ratio = Math.Min(xRatio, yRatio); // Choose the smaller ratio for scaling
            int textureWidth = (int)(mapTexture.Width * ratio);
            int textureHeight = (int)(mapTexture.Height * ratio);

            // Apply max height constraint
            if (textureHeight > maxPreviewHeight)
            {
                ratio = maxPreviewHeight / (double)mapTexture.Height;
                textureHeight = maxPreviewHeight;
                textureWidth = (int)(mapTexture.Width * ratio); // Recalculate width to maintain aspect ratio
            }

            int texturePositionX = mapPreviewMargin + ((ClientRectangle.Width- (mapPreviewMargin*2)) / 2 - (textureWidth/2)); 
            int texturePositionY = mapPreviewPositionY;

            DrawTexture(
                mapTexture,
                new Rectangle(texturePositionX, texturePositionY, textureWidth, textureHeight),
                Color.White
            );
        }
    }
}
