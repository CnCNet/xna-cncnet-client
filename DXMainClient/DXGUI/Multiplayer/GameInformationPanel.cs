﻿using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using DTAClient.Domain.Multiplayer;
using ClientCore.Extensions;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using System.Net.NetworkInformation;
using System;
using ClientCore;

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
        private XNALabel lblPingValue;
        private XNALabel lblPlayers;
        private XNALabel lblSkillLevel;

        private XNALabel[] lblPlayerNames;
        private Texture2D[] pingTextures;

        private GenericHostedGame game = null;
        private Texture2D mapTexture;
        private Texture2D pingTexture;
        private Texture2D noMapPreviewTexture;

        private int leftColumnPositionX = 0;
        private int rightColumnPositionX = 0;
        private int topStartingPositionY = 0;
        private int mapPreviewPositionY = 0;
        private const int rowHeight = 24;
        private const int initialPanelHeight = 260;
        private const int columnWidth = 235;

        private string[] skillLevelOptions;

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, columnWidth * 2, initialPanelHeight);
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 255), 1, 1);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            lblGameInformation = new XNALabel(WindowManager);
            lblGameInformation.FontIndex = 1;
            lblGameInformation.Text = "GAME INFORMATION".L10N("Client:Main:GameInfo");

            noMapPreviewTexture = AssetLoader.LoadTexture("noMapPreview.png");

            leftColumnPositionX = 10;
            rightColumnPositionX = Width / 2 - 10;
            topStartingPositionY = 30;
            mapPreviewPositionY = topStartingPositionY + (rowHeight * 2 + 15); // 2 Labels down, incase map name spills to next line

            // Right Column
            // Includes Game mode, Map name, and the Map preview (See DrawMapPreview for that)
            lblGameMode = new XNALabel(WindowManager);
            lblGameMode.ClientRectangle = new Rectangle(rightColumnPositionX, topStartingPositionY, 0, 0);

            lblMap = new XNALabel(WindowManager);
            lblMap.ClientRectangle = new Rectangle(rightColumnPositionX, topStartingPositionY + rowHeight, 0, 0);


            // Left Column
            // Includes Host, Crates, Superweapons, Ping, Version, and Players
            lblHost = new XNALabel(WindowManager);
            lblHost.ClientRectangle = new Rectangle(leftColumnPositionX, topStartingPositionY, 0, 0);

            lblPing = new XNALabel(WindowManager);
            lblPing.ClientRectangle = new Rectangle(leftColumnPositionX, topStartingPositionY + rowHeight, 0, 0);

            lblPingValue = new XNALabel(WindowManager);
            lblPingValue.ClientRectangle = new Rectangle(leftColumnPositionX + 44, topStartingPositionY + rowHeight, 0, 0); // Enough space for ping texture before value

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

            pingTextures = new Texture2D[5]
            {
                AssetLoader.LoadTexture("ping0.png"),
                AssetLoader.LoadTexture("ping1.png"),
                AssetLoader.LoadTexture("ping2.png"),
                AssetLoader.LoadTexture("ping3.png"),
                AssetLoader.LoadTexture("ping4.png")
            };

            AddChild(lblGameMode);
            AddChild(lblMap);
            AddChild(lblGameVersion);
            AddChild(lblHost);
            AddChild(lblPing);
            AddChild(lblPingValue);
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
            this.game = game;

            // we don't have the ID of a map here
            string translatedMapName = string.IsNullOrEmpty(game.Map)
                ? "Unknown".L10N("Client:Main:Unknown") : mapLoader.TranslatedMapNames.ContainsKey(game.Map)
                ? mapLoader.TranslatedMapNames[game.Map] : game.Map;

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

            lblPing.Text = "Ping:".L10N("Client:Main:GameInfoPing");
            lblPing.Visible = true;

            lblPingValue.Text = game.Ping > 0 ? game.Ping.ToString() + " ms" : "Ping: Unknown".L10N("Client:Main:GameInfoPingUnknown");
            lblPingValue.Visible = true;

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

            pingTexture = GetTextureForPing(game.Ping);

            if (mapLoader != null)
                mapTexture = mapLoader.GameModeMaps.Find(m => m.Map.Name == game.Map)?.Map.LoadPreviewTexture() ?? noMapPreviewTexture;
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

            if (mapTexture != null && !mapTexture.IsDisposed)
                mapTexture.Dispose();

            if (pingTexture != null && !pingTexture.IsDisposed)
                pingTexture.Dispose();
        }

        public override void Draw(GameTime gameTime)
        {
            if (Alpha > 0.0f)
            {
                base.Draw(gameTime);

                if (game != null)
                {
                    if (pingTexture != null)
                        DrawTexture(pingTexture, new Rectangle(lblPing.ClientRectangle.X + 26, lblPing.Y, pingTexture.Width, pingTexture.Height), Color.White);

                    if (mapTexture != null)
                        RenderMapPreview();
                }
            }
        }

        private void RenderMapPreview()
        {
            int maxPreviewHeight = 150;

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

        private Texture2D GetTextureForPing(int ping)
        {
            switch (ping)
            {
                case int p when (p > 350):
                    return pingTextures[4];
                case int p when (p > 250):
                    return pingTextures[3];
                case int p when (p > 100):
                    return pingTextures[2];
                case int p when (p >= 0):
                    return pingTextures[1];
                default:
                    return pingTextures[0];
            }
        }
    }
}
