using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using DTAClient.Domain.Multiplayer;
using ClientCore.Extensions;

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
        private XNALabel[] lblPlayerNames;

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, 235, 264);
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 255), 1, 1);
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            lblGameInformation = new XNALabel(WindowManager);
            lblGameInformation.FontIndex = 1;
            lblGameInformation.Text = "GAME INFORMATION".L10N("Client:Main:GameInfo");

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

            AddChild(lblGameMode);
            AddChild(lblMap);
            AddChild(lblGameVersion);
            AddChild(lblHost);
            AddChild(lblPing);
            AddChild(lblPlayers);
            AddChild(lblGameInformation);

            lblGameInformation.CenterOnParent();
            lblGameInformation.ClientRectangle = new Rectangle(lblGameInformation.X, 6,
                lblGameInformation.Width, lblGameInformation.Height);

            base.Initialize();
        }

        public void SetInfo(GenericHostedGame game)
        {
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
        }

        public override void Draw(GameTime gameTime)
        {
            if (Alpha > 0.0f)
                base.Draw(gameTime);
        }
    }
}
