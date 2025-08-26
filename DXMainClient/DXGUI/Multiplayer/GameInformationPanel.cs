using System;
using System.Diagnostics;

using ClientCore;
using ClientCore.Extensions;

using DTAClient.Domain.Multiplayer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer
{
    /// <summary>
    /// A UI panel that displays information about a hosted CnCNet or LAN game.
    /// </summary>
    public class GameInformationPanel : XNAPanel
    {
        private const int MAX_PLAYERS = 8;

        public GameInformationPanel(WindowManager windowManager, MapLoader mapLoader)
            : base(windowManager)
        {
            this.mapLoader = mapLoader;
            DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
        }

        private MapLoader mapLoader;

        private XNALabel lblGameInformation;
        private XNALabel lblGameMode;
        private XNALabel lblMap;
        private XNALabel lblGameVersion;
        private XNALabel lblHost;
        private XNALabel lblPing;
        private XNALabel lblPlayers;
        private XNALabel lblSkillLevel;

        private XNALabel[] lblPlayerNames;

        private GenericHostedGame game = null;

        private bool disposeTextures = false;
        private Texture2D mapTexture = null;
        private Texture2D noMapPreviewTexture = null;

        private const int leftColumnPositionX = 10;
        private int rightColumnPositionX = 0;
        private int mapPreviewPositionY = 0;
        private const int columnMargin = 10;
        private const int topStartingPositionY = 30;
        private const int rowHeight = 24;
        private const int initialPanelHeight = 260;
        private const int columnWidth = 235;
        private const int maxPreviewHeight = 150;
        private const int mapPreviewMargin = 15;

        private string[] skillLevelOptions;

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, columnWidth * 2, initialPanelHeight);
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 255), 1, 1);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            lblGameInformation = new XNALabel(WindowManager);
            lblGameInformation.FontIndex = 1;
            lblGameInformation.Text = "GAME INFORMATION".L10N("Client:Main:GameInfo");

            if (AssetLoader.AssetExists("noMapPreview.png"))
                noMapPreviewTexture = AssetLoader.LoadTexture("noMapPreview.png");

            rightColumnPositionX = Width / 2 - columnMargin;
            mapPreviewPositionY = topStartingPositionY + (rowHeight * 2 + mapPreviewMargin); // 2 Labels down, incase map name spills to next line

            // Right Column
            // Includes Game mode, Map name, and the Map preview (See RenderMapPreview for that)
            lblGameMode = new XNALabel(WindowManager);
            lblGameMode.ClientRectangle = new Rectangle(rightColumnPositionX, topStartingPositionY, 0, 0);

            lblMap = new XNALabel(WindowManager);
            lblMap.ClientRectangle = new Rectangle(rightColumnPositionX, topStartingPositionY + rowHeight, 0, 0);


            // Left Column
            // Includes Host, Ping, Version, and Players
            lblHost = new XNALabel(WindowManager);
            lblHost.ClientRectangle = new Rectangle(leftColumnPositionX, topStartingPositionY, 0, 0);

            lblPing = new XNALabel(WindowManager);
            lblPing.ClientRectangle = new Rectangle(leftColumnPositionX, topStartingPositionY + rowHeight, 0, 0);

            lblGameVersion = new XNALabel(WindowManager);
            lblGameVersion.ClientRectangle = new Rectangle(leftColumnPositionX, topStartingPositionY + (rowHeight * 2), 0, 0);

            lblSkillLevel = new XNALabel(WindowManager);
            lblSkillLevel.ClientRectangle = new Rectangle(leftColumnPositionX, topStartingPositionY + (rowHeight * 3), 0, 0);

            lblPlayers = new XNALabel(WindowManager);
            lblPlayers.ClientRectangle = new Rectangle(leftColumnPositionX, topStartingPositionY + (rowHeight * 4), 0, 0);

            lblPlayerNames = new XNALabel[MAX_PLAYERS];
            for (int i = 0; i < lblPlayerNames.Length / 2; i++)
            {
                XNALabel lblPlayerName1 = new XNALabel(WindowManager);
                lblPlayerName1.ClientRectangle = new Rectangle(lblPlayers.X, lblPlayers.Y + rowHeight + i * 20, 0, 0);
                lblPlayerName1.RemapColor = UISettings.ActiveSettings.AltColor;

                XNALabel lblPlayerName2 = new XNALabel(WindowManager);
                lblPlayerName2.ClientRectangle = new Rectangle(lblPlayers.X + 115, lblPlayerName1.Y, 0, 0);
                lblPlayerName2.RemapColor = UISettings.ActiveSettings.AltColor;

                AddChild(lblPlayerName1);
                AddChild(lblPlayerName2);

                lblPlayerNames[i] = lblPlayerName1;
                lblPlayerNames[(lblPlayerNames.Length / 2) + i] = lblPlayerName2;
            }

            AddChild(lblGameMode);
            AddChild(lblMap);
            AddChild(lblGameVersion);
            AddChild(lblHost);
            AddChild(lblPing);
            AddChild(lblPlayers);
            AddChild(lblGameInformation);
            AddChild(lblSkillLevel);

            lblGameInformation.CenterOnParent();
            lblGameInformation.ClientRectangle = new Rectangle(lblGameInformation.X, 6,
                lblGameInformation.Width, lblGameInformation.Height);

            skillLevelOptions = ClientConfiguration.Instance.SkillLevelOptions.Split(',');

            base.Initialize();
        }

        public void SetInfo(GenericHostedGame game)
        {
            ClearInfo();

            this.game = game;

            string translatedMapName = "Unknown".L10N("Client:Main:Unknown");

            if (!string.IsNullOrEmpty(game.MapHash) && mapLoader != null)
            {
                var mapEntry = mapLoader.GameModeMaps
                    .Find(m => m.Map.SHA1.Equals(game.MapHash, StringComparison.OrdinalIgnoreCase));

                if (mapEntry != null)
                    translatedMapName = mapEntry.Map.Name ?? mapEntry.Map.UntranslatedName;
                else if (!string.IsNullOrEmpty(game.Map))
                    translatedMapName = game.Map; // fallback to broadcasted name
            }
            else if (!string.IsNullOrEmpty(game.Map))
            {
                translatedMapName = game.Map;
            }

            string translatedGameModeName = string.IsNullOrEmpty(game.GameMode)
                ? "Unknown".L10N("Client:Main:Unknown") : game.GameMode.L10N($"INI:GameModes:{game.GameMode}:UIName", notify: false);

            lblGameMode.Text = Renderer.GetStringWithLimitedWidth("Game mode:".L10N("Client:Main:GameInfoGameMode") + " " + Renderer.GetSafeString(translatedGameModeName, lblGameMode.FontIndex),
               lblGameMode.FontIndex, Width - lblGameMode.X);
            lblGameMode.Visible = true;

            lblMap.Text = Renderer.GetStringWithLimitedWidth("Map:".L10N("Client:Main:GameInfoMap") + " " + Renderer.GetSafeString(translatedMapName, lblMap.FontIndex),
                            lblMap.FontIndex, Width - lblMap.X);
            lblMap.Visible = true;

            lblMap.Text = Renderer.FixText(lblMap.Text, lblMap.FontIndex, columnWidth).Text;
            lblMap.Visible = true;

            lblGameVersion.Text = "Game version:".L10N("Client:Main:GameInfoGameVersion") + " " + Renderer.GetSafeString(game.GameVersion, lblGameVersion.FontIndex);
            lblGameVersion.Visible = true;

            lblHost.Text = "Host:".L10N("Client:Main:GameInfoHost") + " " + Renderer.GetSafeString(game.HostName, lblHost.FontIndex);
            lblHost.Visible = true;

            lblPing.Text = game.Ping > 0 ? "Ping:".L10N("Client:Main:GameInfoPing") + " " + game.Ping.ToString() + " ms" : "Ping: Unknown".L10N("Client:Main:GameInfoPingUnknown");
            lblPing.Visible = true;

            lblPlayers.Visible = true;
            lblPlayers.Text = "Players".L10N("Client:Main:GameInfoPlayers") + " (" + game.Players.Length + " / " + game.MaxPlayers + "):";

            for (int i = 0; i < game.Players.Length && i < MAX_PLAYERS; i++)
            {
                lblPlayerNames[i].Visible = true;
                lblPlayerNames[i].Text = Renderer.GetSafeString(game.Players[i], lblPlayerNames[i].FontIndex);
            }

            for (int i = game.Players.Length; i < MAX_PLAYERS; i++)
            {
                lblPlayerNames[i].Visible = false;
            }

            string skillLevel = skillLevelOptions[game.SkillLevel];
            string localizedSkillLevel = skillLevel.L10N($"INI:ClientDefinitions:SkillLevel:{game.SkillLevel}");
            lblSkillLevel.Text = "Preferred Skill Level:".L10N("Client:Main:GameInfoSkillLevel") + " " + localizedSkillLevel;

            lblGameInformation.Visible = true;

            if (mapLoader != null && !string.IsNullOrEmpty(game.MapHash))
            {
                mapTexture = mapLoader.GameModeMaps
                    .Find(m => m.Map.SHA1.Equals(game.MapHash, StringComparison.OrdinalIgnoreCase) &&
                               m.Map.IsPreviewTextureCached())?.Map?.LoadPreviewTexture();

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
            lblGameMode.Visible = false;
            lblMap.Visible = false;
            lblGameVersion.Visible = false;
            lblHost.Visible = false;
            lblPing.Visible = false;
            lblPlayers.Visible = false;
            lblGameInformation.Visible = false;

            foreach (XNALabel label in lblPlayerNames)
                label.Visible = false;

            if (mapTexture != null && disposeTextures)
            {
                Debug.Assert(!mapTexture.IsDisposed, "mapTexture should not be disposed.");
                mapTexture.Dispose();
                mapTexture = null;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            if (Alpha > 0.0f)
            {
                base.Draw(gameTime);

                if (game != null && mapTexture != null)
                    RenderMapPreview();
            }
        }

        private void RenderMapPreview()
        {
            // Calculate map preview area based on right half of ClientRectangle
            double xRatio = (ClientRectangle.Width / 2 - 10) / (double)mapTexture.Width;
            double yRatio = (ClientRectangle.Height - 20) / (double)mapTexture.Height;

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

            int texturePositionX = rightColumnPositionX + (ClientRectangle.Width / 2 - textureWidth) / 2; // Center in the right column
            int texturePositionY = mapPreviewPositionY;

            DrawTexture(
                mapTexture,
                new Rectangle(texturePositionX, texturePositionY, textureWidth, textureHeight),
                Color.White
            );
        }
    }
}
