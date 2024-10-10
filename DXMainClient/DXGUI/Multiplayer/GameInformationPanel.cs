using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using DTAClient.Domain.Multiplayer;
using ClientCore.Extensions;
using Microsoft.Xna.Framework.Graphics;
using System.Net.NetworkInformation;
using Rampastring.Tools;

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
        private XNALabel lblCrates;
        private XNALabel lblSuperWeapons;
        private XNALabel[] lblPlayerNames;

        private Texture2D[] PingTextures;

        private GenericHostedGame game = null;
        private Texture2D mapTexture;

        private int LeftColumnPositionX = 0;
        private int RightColumnPositionX = 0;
        private int TopStartingPositionY = 0;
        private int MapPreviewPositionY = 0;
        private int RowHeight = 24;
        private int InitialPanelHeight = 260;

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, 400, InitialPanelHeight);
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 255), 1, 1);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            lblGameInformation = new XNALabel(WindowManager);
            lblGameInformation.FontIndex = 1;
            lblGameInformation.Text = "GAME INFORMATION".L10N("Client:Main:GameInfo");

            LeftColumnPositionX = 10;
            RightColumnPositionX = Width / 2 - 10; // Right side, with a margin of 10
            TopStartingPositionY = 30;
            MapPreviewPositionY = TopStartingPositionY + (RowHeight * 2); // 2 Labels down

            // Right Column
            // Includes Game mode, Map name, and the Map preview (See DrawMapPreview for that)
            lblGameMode = new XNALabel(WindowManager);
            lblGameMode.ClientRectangle = new Rectangle(RightColumnPositionX, TopStartingPositionY, 0, 0);

            lblMap = new XNALabel(WindowManager);
            lblMap.ClientRectangle = new Rectangle(RightColumnPositionX, TopStartingPositionY + RowHeight, 0, 0);


            // Left Column
            // Includes Host, Crates, Superweapons, Ping, Version, and Players
            lblHost = new XNALabel(WindowManager);
            lblHost.ClientRectangle = new Rectangle(LeftColumnPositionX, TopStartingPositionY, 0, 0);

            lblCrates = new XNALabel(WindowManager);
            lblCrates.ClientRectangle = new Rectangle(LeftColumnPositionX, TopStartingPositionY + RowHeight, 0, 0);

            lblSuperWeapons = new XNALabel(WindowManager);
            lblSuperWeapons.ClientRectangle = new Rectangle(LeftColumnPositionX, TopStartingPositionY + (RowHeight * 2), 0, 0);

            lblPing = new XNALabel(WindowManager);
            lblPing.ClientRectangle = new Rectangle(LeftColumnPositionX, TopStartingPositionY + (RowHeight * 3), 0, 0);

            lblGameVersion = new XNALabel(WindowManager);
            lblGameVersion.ClientRectangle = new Rectangle(LeftColumnPositionX, TopStartingPositionY + (RowHeight * 4), 0, 0);

            lblPlayers = new XNALabel(WindowManager);
            lblPlayers.ClientRectangle = new Rectangle(LeftColumnPositionX, TopStartingPositionY + (RowHeight * 5), 0, 0);

            lblPlayerNames = new XNALabel[MAX_PLAYERS];
            for (int i = 0; i < lblPlayerNames.Length / 2; i++)
            {
                XNALabel lblPlayerName1 = new XNALabel(WindowManager);
                lblPlayerName1.ClientRectangle = new Rectangle(lblPlayers.X, lblPlayers.Y + RowHeight + i * 20, 0, 0);
                lblPlayerName1.RemapColor = UISettings.ActiveSettings.AltColor;

                XNALabel lblPlayerName2 = new XNALabel(WindowManager);
                lblPlayerName2.ClientRectangle = new Rectangle(lblPlayers.X + 115, lblPlayerName1.Y, 0, 0);
                lblPlayerName2.RemapColor = UISettings.ActiveSettings.AltColor;

                AddChild(lblPlayerName1);
                AddChild(lblPlayerName2);

                lblPlayerNames[i] = lblPlayerName1;
                lblPlayerNames[(lblPlayerNames.Length / 2) + i] = lblPlayerName2;
            }

            PingTextures = new Texture2D[5]
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
            AddChild(lblPlayers);
            AddChild(lblCrates);
            AddChild(lblSuperWeapons);
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
                lblGameMode.FontIndex, Width - lblGameMode.X);
            lblGameMode.Visible = true;

            lblMap.Text = Renderer.GetStringWithLimitedWidth("Map:".L10N("Client:Main:GameInfoMap") + " " + Renderer.GetSafeString(translatedMapName, lblMap.FontIndex),
                lblMap.FontIndex, Width - lblMap.X);
            lblMap.Visible = true;

            lblGameVersion.Text = "Game version:".L10N("Client:Main:GameInfoGameVersion") + " " + Renderer.GetSafeString(game.GameVersion, lblGameVersion.FontIndex);
            lblGameVersion.Visible = true;

            lblHost.Text = "Host:".L10N("Client:Main:GameInfoHost") + " " + Renderer.GetSafeString(game.HostName, lblHost.FontIndex);
            lblHost.Visible = true;

            lblPing.Text = "Ping:".L10N("Client:Main:GameInfoPing");
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

            if (game.Players.Length > 3)
            {
                Height = InitialPanelHeight + (game.Players.Length - 3) * RowHeight;
            }

            lblCrates.Text = "Crates:".L10N("Client:Main:GameInfoCrates") + " " + Renderer.GetSafeString(game.HasCrates ? "Yes" : "No", lblCrates.FontIndex);
            lblCrates.Visible = true;

            lblSuperWeapons.Text = "Superweapons:".L10N("Client:Main:GameInfoSuperWeapons") + " " + Renderer.GetSafeString(game.HasSuperWeapons ? "Yes" : "No", lblSuperWeapons.FontIndex);
            lblSuperWeapons.Visible = true;
        }

        public void ClearInfo()
        {
            lblGameMode.Visible = false;
            lblMap.Visible = false;
            lblGameVersion.Visible = false;
            lblHost.Visible = false;
            lblPing.Visible = false;
            lblPlayers.Visible = false;
            game = null;

            if (mapTexture != null && !mapTexture.IsDisposed)
                mapTexture.Dispose();

            foreach (XNALabel label in lblPlayerNames)
                label.Visible = false;
        }

        private void RenderMapPreview(Texture2D mapPreview)
        {
            // Calculate map preview area based on right half of ClientRectangle
            double xRatio = (ClientRectangle.Width / 2 - 10) / (double)mapTexture.Width;
            double yRatio = (ClientRectangle.Height - 20) / (double)mapTexture.Height;

            double ratio;

            int texturePositionX = RightColumnPositionX; 
            int texturePositionY = MapPreviewPositionY; // Align map preview Y position with lblGameMode
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


        public override void Draw(GameTime gameTime)
        {
            if (Alpha > 0.0f)
            {
                base.Draw(gameTime);

                // Test with a simple static texture
                if (game != null)
                {
                    Texture2D pingTexture = GetTextureForPing(game.Ping); 
                    DrawTexture(pingTexture, new Rectangle(lblPing.ClientRectangle.X + 24, lblPing.Y, pingTexture.Width, pingTexture.Height), Color.White); // Fixed position

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

        private Texture2D GetTextureForPing(int ping)
        {
            switch (ping)
            {
                case int p when (p > 350):
                    return PingTextures[4];
                case int p when (p > 250):
                    return PingTextures[3];
                case int p when (p > 100):
                    return PingTextures[2];
                case int p when (p >= 0):
                    return PingTextures[1];
                default:
                    return PingTextures[0];
            }
        }
    }
}
