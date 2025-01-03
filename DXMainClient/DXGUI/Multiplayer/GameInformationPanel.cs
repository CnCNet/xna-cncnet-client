using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using DTAClient.Domain.Multiplayer;
using ClientCore.Extensions;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using System.Net.NetworkInformation;

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
        private XNALabel[] lblPlayerNames;
        private Texture2D[] pingTextures;
        private GenericHostedGame game = null;
        private Texture2D mapTexture;

        private int rightColumnPositionX = 0;
        private int mapPreviewPositionY = 0;

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, 470, 264); // 235 was original, doubling size for map preview
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 255), 1, 1);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            lblGameInformation = new XNALabel(WindowManager);
            lblGameInformation.FontIndex = 1;
            lblGameInformation.Text = "GAME INFORMATION".L10N("Client:Main:GameInfo");

            rightColumnPositionX = Width / 2 - 10; // Right side, with a margin of 10
            mapPreviewPositionY = 30 + 48; // 2x Labels down

            lblGameMode = new XNALabel(WindowManager);
            lblGameMode.ClientRectangle = new Rectangle(6, 30, 0, 0);

            lblMap = new XNALabel(WindowManager);
            lblMap.ClientRectangle = new Rectangle(6, 54, 0, 0);

            lblGameVersion = new XNALabel(WindowManager);
            lblGameVersion.ClientRectangle = new Rectangle(6, 78, 0, 0);

            lblHost = new XNALabel(WindowManager);
            lblHost.ClientRectangle = new Rectangle(6, 102, 0, 0);

            lblPing = new XNALabel(WindowManager);
            lblPing.ClientRectangle = new Rectangle(6, 126, 0, 0);

            lblPingValue = new XNALabel(WindowManager);
            lblPingValue.ClientRectangle = new Rectangle(lblPing.X + 46, 126, 0, 0); // Enough space for ping texture before value

            lblPlayers = new XNALabel(WindowManager);
            lblPlayers.ClientRectangle = new Rectangle(6, 150, 0, 0);

            lblPlayerNames = new XNALabel[MAX_PLAYERS];
            for (int i = 0; i < lblPlayerNames.Length / 2; i++)
            {
                XNALabel lblPlayerName1 = new XNALabel(WindowManager);
                lblPlayerName1.ClientRectangle = new Rectangle(lblPlayers.X, lblPlayers.Y + 24 + i * 20, 0, 0);
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

            lblGameInformation.CenterOnParent();
            lblGameInformation.ClientRectangle = new Rectangle(lblGameInformation.X, 6,
                lblGameInformation.Width, lblGameInformation.Height);

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
                lblGameMode.FontIndex, Width - lblGameMode.X * 2);
            lblGameMode.Visible = true;

            lblMap.Text = Renderer.GetStringWithLimitedWidth("Map:".L10N("Client:Main:GameInfoMap") + " " + Renderer.GetSafeString(translatedMapName, lblMap.FontIndex),
                lblMap.FontIndex, Width - lblMap.X * 2);
            lblMap.Visible = true;

            lblGameVersion.Text = "Game version:".L10N("Client:Main:GameInfoGameVersion") + " " + Renderer.GetSafeString(game.GameVersion, lblGameVersion.FontIndex);
            lblGameVersion.Visible = true;

            lblHost.Text = "Host:".L10N("Client:Main:GameInfoHost") + " " + Renderer.GetSafeString(game.HostName, lblHost.FontIndex);
            lblHost.Visible = true;

            lblPing.Text = "Ping:".L10N("Client: Main: GameInfoPing");
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
        }

        public void ClearInfo()
        {
            lblGameMode.Visible = false;
            lblMap.Visible = false;
            lblGameVersion.Visible = false;
            lblHost.Visible = false;
            lblPing.Visible = false;
            lblPlayers.Visible = false;

            foreach (XNALabel label in lblPlayerNames)
                label.Visible = false;

            if (mapTexture != null && !mapTexture.IsDisposed)
                mapTexture.Dispose();
        }

        public override void Draw(GameTime gameTime)
        {
            if (Alpha > 0.0f)
            {
                base.Draw(gameTime);

                if (game != null)
                {
                    Texture2D pingTexture = GetTextureForPing(game.Ping);
                    DrawTexture(pingTexture, new Rectangle(lblPing.ClientRectangle.X + 26, lblPing.Y, pingTexture.Width, pingTexture.Height), Color.White);

                    if (mapLoader != null)
                    {
                        mapTexture = mapLoader.GameModeMaps.Find((m) => m.Map.Name == game.Map)?.Map.LoadPreviewTexture();

                        if (mapTexture != null)
                        {
                            RenderMapPreview(mapTexture);
                        }
                    }
                    else
                    {
                        Logger.Log($"mapLoader is null {mapLoader}");
                    }
                }
                else
                {
                    Logger.Log("PingTextures is null or empty");
                }
            }
        }

        private void RenderMapPreview(Texture2D mapPreview)
        {
            // Calculate map preview area based on right half of ClientRectangle
            double xRatio = (ClientRectangle.Width / 2 - 10) / (double)mapTexture.Width;
            double yRatio = (ClientRectangle.Height - 20) / (double)mapTexture.Height;

            double ratio;

            int texturePositionX = rightColumnPositionX;
            int texturePositionY = mapPreviewPositionY; // Align map preview Y position with lblGameMode
            int textureHeight = 0;
            int textureWidth = 0;

            if (xRatio > yRatio)
            {
                ratio = yRatio;
                textureHeight = ClientRectangle.Height - 20;
                textureWidth = (int)(mapTexture.Width * ratio);
                texturePositionX += (ClientRectangle.Width / 2 - textureWidth) / 2; // Center it in the right side
            }
            else
            {
                ratio = xRatio;
                textureWidth = ClientRectangle.Width / 2 - 10;
                textureHeight = (int)(mapTexture.Height * ratio);
            }

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
