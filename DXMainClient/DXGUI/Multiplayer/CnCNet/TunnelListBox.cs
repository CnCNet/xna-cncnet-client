using DTAClient.Domain.Multiplayer.CnCNet;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using ClientCore;
using System.IO;
using System.Reflection;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    /// <summary>
    /// A list box for listing CnCNet tunnel servers.
    /// </summary>
    class TunnelListBox : XNAMultiColumnListBox
    {
        private static readonly Dictionary<string, Point> CountryCodeFlagPositions = ParseCountryCodeFlagPositions();
        private const int FLAG_WIDTH = 16;
        private const int FLAG_HEIGHT = 16;

        public TunnelListBox(WindowManager windowManager, TunnelHandler tunnelHandler) : base(windowManager)
        {
            this.tunnelHandler = tunnelHandler;

            tunnelHandler.TunnelsRefreshed += TunnelHandler_TunnelsRefreshed;
            tunnelHandler.TunnelPinged += TunnelHandler_TunnelPinged;

            SelectedIndexChanged += TunnelListBox_SelectedIndexChanged;

            int headerHeight = (int)Renderer.GetTextDimensions("Name", HeaderFontIndex).Y;

            Width = 466;
            Height = LineHeight * 12 + headerHeight + 3;
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);

            using Stream flagsStream = Assembly.GetAssembly(typeof(GameCollection)).GetManifestResourceStream("DTAClient.Icons.flags16.png");
            using var flagsPNG = SixLabors.ImageSharp.Image.Load(flagsStream);
            flagsSpriteSheet = AssetLoader.TextureFromImage(flagsPNG);

            var flagListBox = new FlagListBox(windowManager, tunnelHandler, flagsSpriteSheet);
            flagListBox.FontIndex = FontIndex;
            flagListBox.LineHeight = LineHeight;

            var flagHeader = new XNAPanel(windowManager);
            flagHeader.Width = 20;
            flagHeader.Height = headerHeight + 3;

            AddColumn(flagHeader, flagListBox);

            AddColumn("Name".L10N("Client:Main:NameHeader"), 210);
            AddColumn("Official".L10N("Client:Main:OfficialHeader"), 70);
            AddColumn("Ping".L10N("Client:Main:PingHeader"), 76);
            AddColumn("Players".L10N("Client:Main:PlayersHeader"), 90);
            AllowRightClickUnselect = false;
            AllowKeyboardInput = true;
        }

        private int? _targetVersion;
        public int? TargetVersion
        {
            get => _targetVersion;
            set
            {
                if (_targetVersion != value)
                {
                    _targetVersion = value;
                    isManuallySelectedTunnel = false;
                    manuallySelectedTunnelKey = null;
                    if (ItemCount > 0)
                        TunnelHandler_TunnelsRefreshed(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler ListRefreshed;

        private readonly TunnelHandler tunnelHandler;
        private Texture2D flagsSpriteSheet;

        private int bestTunnelIndex = 0;
        private int lowestTunnelRating = int.MaxValue;

        private bool isManuallySelectedTunnel;
        private string manuallySelectedTunnelKey;

        private List<CnCNetTunnel> GetFilteredTunnels()
        {
            int targetVersion = TargetVersion ?? ((TunnelMode)UserINISettings.Instance.TunnelMode.Value == TunnelMode.V2Legacy ? 2 : 3);
            return tunnelHandler.Tunnels.Where(tunnel => tunnel.Version == targetVersion).ToList();
        }

        private static string GetTunnelKey(CnCNetTunnel tunnel) => GetTunnelKey(tunnel.Address, tunnel.Port);

        private static string GetTunnelKey(string address, int port) => $"{address}:{port}";

        /// <summary>
        /// Selects a tunnel from the list with the given address and port.
        /// </summary>
        /// <param name="address">The address of the tunnel server to select.</param>
        /// <param name="port">The port of the tunnel server to select.</param>
        public void SelectTunnel(string address, int port)
        {
            int index = GetFilteredTunnels().FindIndex(t => t.Address == address && t.Port == port);
            if (index > -1)
            {
                SelectedIndex = index;
                isManuallySelectedTunnel = true;
                manuallySelectedTunnelKey = GetTunnelKey(address, port);
            }
        }

        /// <summary>
        /// Gets whether or not a tunnel from the list with the given address and port is selected.
        /// </summary>
        /// <param name="address">The address of the tunnel server</param>
        /// <param name="port">The port of the tunnel server</param>
        /// <returns>True if tunnel with given address is selected, otherwise false.</returns>
        public bool IsTunnelSelected(string address, int port)
        {
            return GetFilteredTunnels().FindIndex(t => t.Address == address && t.Port == port) == SelectedIndex;
        }

        private void TunnelHandler_TunnelsRefreshed(object sender, EventArgs e)
        {
            ClearItems();

            var filteredTunnels = GetFilteredTunnels();
            int tunnelIndex = 0;
            bestTunnelIndex = 0;
            lowestTunnelRating = int.MaxValue;

            foreach (CnCNetTunnel tunnel in filteredTunnels)
            {
                List<string> info = new List<string>();

                info.Add(""); // Flag column
                info.Add(tunnel.Name);
                info.Add(Conversions.BooleanToString(tunnel.Official, BooleanStringStyle.YESNO));
                info.Add(tunnel.Ping.ToString());
                info.Add(tunnel.Clients + " / " + tunnel.MaxClients);

                AddItem(info, true);

                XNAListBoxItem flagItem = GetItem(0, tunnelIndex);
                if (flagItem != null)
                    flagItem.Tag = GetFlagRectangle(tunnel.CountryCode);

                if ((tunnel.Official || tunnel.Recommended) && tunnel.Ping.IsValid())
                {
                    int rating = GetTunnelRating(tunnel);
                    if (rating < lowestTunnelRating)
                    {
                        bestTunnelIndex = tunnelIndex;
                        lowestTunnelRating = rating;
                    }
                }

                tunnelIndex++;
            }

            if (filteredTunnels.Count > 0)
            {
                if (!isManuallySelectedTunnel)
                {
                    SelectedIndex = bestTunnelIndex;
                    isManuallySelectedTunnel = false;
                }
                else
                {
                    int manuallySelectedIndex = filteredTunnels.FindIndex(t => GetTunnelKey(t) == manuallySelectedTunnelKey);

                    if (manuallySelectedIndex == -1)
                    {
                        SelectedIndex = bestTunnelIndex;
                        isManuallySelectedTunnel = false;
                        manuallySelectedTunnelKey = null;
                    }
                    else
                        SelectedIndex = manuallySelectedIndex;
                }
            }

            ListRefreshed?.Invoke(this, EventArgs.Empty);
        }

        private void TunnelHandler_TunnelPinged(string address, int port)
        {
            var filteredTunnels = GetFilteredTunnels();

            CnCNetTunnel tunnel = tunnelHandler.Tunnels.FirstOrDefault(t => t.Address == address && t.Port == port);
            if (tunnel == null)
                return;

            int filteredIndex = filteredTunnels.FindIndex(t => t.Address == address && t.Port == port);
            if (filteredIndex == -1)
                return;

            XNAListBoxItem lbItem = GetItem(3, filteredIndex);
            lbItem.Text = tunnel.Ping.ToString();

            if (tunnel.Ping.IsValid())
            {
                int rating = GetTunnelRating(tunnel);

                if (isManuallySelectedTunnel)
                    return;

                if ((tunnel.Recommended || tunnel.Official) && rating < lowestTunnelRating)
                {
                    bestTunnelIndex = filteredIndex;
                    lowestTunnelRating = rating;
                    SelectedIndex = filteredIndex;
                }
            }
        }

        private int GetTunnelRating(CnCNetTunnel tunnel)
        {
            double usageRatio = (double)tunnel.Clients / tunnel.MaxClients;

            if (usageRatio == 0)
                usageRatio = 0.1;

            usageRatio *= 100.0;

            return Convert.ToInt32(Math.Pow(tunnel.Ping.Milliseconds, 2.0) * usageRatio);
        }

        public CnCNetTunnel GetSelectedTunnel()
        {
            if (!IsValidIndexSelected())
                return null;

            return GetFilteredTunnels()[SelectedIndex];
        }

        private void TunnelListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!IsValidIndexSelected())
                return;

            var filteredTunnels = GetFilteredTunnels();
            if (SelectedIndex >= 0 && SelectedIndex < filteredTunnels.Count)
            {
                isManuallySelectedTunnel = true;
                manuallySelectedTunnelKey = GetTunnelKey(filteredTunnels[SelectedIndex]);
            }
        }

        private static Dictionary<string, Point> ParseCountryCodeFlagPositions()
        {
            // 16px version from
            // https://github.com/lafeber/world-flags-sprite
            // The original vertical strip (offsets in its css files) is repacked
            // into a 16x16 grid, as XNA cannot load textures larger than 2048px.
            return new Dictionary<string, Point>
            {
                ["ad"] = new(96, 16),    ["ae"] = new(112, 16),   ["af"] = new(128, 16),   ["ag"] = new(144, 16),
                ["ai"] = new(160, 16),   ["al"] = new(176, 16),   ["am"] = new(192, 16),   ["ao"] = new(208, 16),
                ["aq"] = new(224, 16),   ["ar"] = new(240, 16),   ["as"] = new(0, 32),     ["at"] = new(16, 32),
                ["au"] = new(32, 32),    ["aw"] = new(48, 32),    ["ax"] = new(64, 32),    ["az"] = new(80, 32),
                ["ba"] = new(96, 32),    ["bb"] = new(112, 32),   ["bd"] = new(128, 32),   ["be"] = new(144, 32),
                ["bf"] = new(160, 32),   ["bg"] = new(176, 32),   ["bh"] = new(192, 32),   ["bi"] = new(208, 32),
                ["bj"] = new(224, 32),   ["bl"] = new(144, 80),   ["bm"] = new(240, 32),   ["bn"] = new(0, 48),
                ["bo"] = new(16, 48),    ["bq"] = new(192, 160),  ["br"] = new(32, 48),    ["bs"] = new(48, 48),
                ["bt"] = new(64, 48),    ["bv"] = new(208, 160),  ["bw"] = new(80, 48),    ["by"] = new(96, 48),
                ["bz"] = new(112, 48),   ["ca"] = new(128, 48),   ["cd"] = new(144, 48),   ["cf"] = new(160, 48),
                ["cg"] = new(176, 48),   ["ch"] = new(192, 48),   ["ci"] = new(208, 48),   ["ck"] = new(224, 48),
                ["cl"] = new(240, 48),   ["cm"] = new(0, 64),     ["cn"] = new(16, 64),    ["co"] = new(32, 64),
                ["cp"] = new(144, 80),   ["cr"] = new(48, 64),    ["cu"] = new(64, 64),    ["cv"] = new(80, 64),
                ["cw"] = new(80, 240),   ["cy"] = new(96, 64),    ["cz"] = new(112, 64),   ["de"] = new(128, 64),
                ["dj"] = new(144, 64),   ["dk"] = new(160, 64),   ["dm"] = new(176, 64),   ["do"] = new(192, 64),
                ["dz"] = new(208, 64),   ["ec"] = new(224, 64),   ["ee"] = new(240, 64),   ["eg"] = new(0, 80),
                ["eh"] = new(16, 80),    ["er"] = new(32, 80),    ["es"] = new(48, 80),    ["et"] = new(64, 80),
                ["fi"] = new(80, 80),    ["fj"] = new(96, 80),    ["fm"] = new(112, 80),   ["fo"] = new(128, 80),
                ["fr"] = new(144, 80),   ["ga"] = new(160, 80),   ["gb"] = new(176, 80),   ["gd"] = new(192, 80),
                ["ge"] = new(208, 80),   ["gg"] = new(224, 80),   ["gh"] = new(240, 80),   ["gi"] = new(0, 96),
                ["gl"] = new(16, 96),    ["gm"] = new(32, 96),    ["gn"] = new(48, 96),    ["gp"] = new(64, 96),
                ["gq"] = new(80, 96),    ["gr"] = new(96, 96),    ["gt"] = new(112, 96),   ["gu"] = new(128, 96),
                ["gw"] = new(144, 96),   ["gy"] = new(160, 96),   ["hk"] = new(176, 96),   ["hn"] = new(192, 96),
                ["hr"] = new(208, 96),   ["ht"] = new(224, 96),   ["hu"] = new(240, 96),   ["id"] = new(0, 112),
                ["ie"] = new(16, 112),   ["il"] = new(32, 112),   ["im"] = new(48, 112),   ["in"] = new(64, 112),
                ["iq"] = new(80, 112),   ["ir"] = new(96, 112),   ["is"] = new(112, 112),  ["it"] = new(128, 112),
                ["je"] = new(144, 112),  ["jm"] = new(160, 112),  ["jo"] = new(176, 112),  ["jp"] = new(192, 112),
                ["ke"] = new(208, 112),  ["kg"] = new(224, 112),  ["kh"] = new(240, 112),  ["ki"] = new(0, 128),
                ["km"] = new(16, 128),   ["kn"] = new(32, 128),   ["kp"] = new(48, 128),   ["kr"] = new(64, 128),
                ["kw"] = new(80, 128),   ["ky"] = new(96, 128),   ["kz"] = new(112, 128),  ["la"] = new(128, 128),
                ["lb"] = new(144, 128),  ["lc"] = new(160, 128),  ["li"] = new(176, 128),  ["lk"] = new(192, 128),
                ["lr"] = new(208, 128),  ["ls"] = new(224, 128),  ["lt"] = new(240, 128),  ["lu"] = new(0, 144),
                ["lv"] = new(16, 144),   ["ly"] = new(32, 144),   ["ma"] = new(48, 144),   ["mc"] = new(0, 112),
                ["md"] = new(64, 144),   ["me"] = new(80, 144),   ["mf"] = new(144, 80),   ["mg"] = new(96, 144),
                ["mh"] = new(112, 144),  ["mk"] = new(128, 144),  ["ml"] = new(144, 144),  ["mm"] = new(160, 144),
                ["mn"] = new(176, 144),  ["mo"] = new(192, 144),  ["mq"] = new(208, 144),  ["mr"] = new(224, 144),
                ["ms"] = new(240, 144),  ["mt"] = new(0, 160),    ["mu"] = new(16, 160),   ["mv"] = new(32, 160),
                ["mw"] = new(48, 160),   ["mx"] = new(64, 160),   ["my"] = new(80, 160),   ["mz"] = new(96, 160),
                ["na"] = new(112, 160),  ["nc"] = new(128, 160),  ["ne"] = new(144, 160),  ["ng"] = new(160, 160),
                ["ni"] = new(176, 160),  ["nl"] = new(192, 160),  ["no"] = new(208, 160),  ["np"] = new(224, 160),
                ["nq"] = new(208, 160),  ["nr"] = new(240, 160),  ["nu"] = new(112, 240),  ["nz"] = new(0, 176),
                ["om"] = new(16, 176),   ["pa"] = new(32, 176),   ["pe"] = new(48, 176),   ["pf"] = new(64, 176),
                ["pg"] = new(80, 176),   ["ph"] = new(96, 176),   ["pk"] = new(112, 176),  ["pl"] = new(128, 176),
                ["pr"] = new(144, 176),  ["ps"] = new(160, 176),  ["pt"] = new(176, 176),  ["pw"] = new(192, 176),
                ["py"] = new(208, 176),  ["qa"] = new(224, 176),  ["re"] = new(240, 176),  ["ro"] = new(0, 192),
                ["rs"] = new(16, 192),   ["ru"] = new(32, 192),   ["rw"] = new(48, 192),   ["sa"] = new(64, 192),
                ["sb"] = new(80, 192),   ["sc"] = new(96, 192),   ["sd"] = new(112, 192),  ["se"] = new(128, 192),
                ["sg"] = new(144, 192),  ["sh"] = new(176, 80),   ["si"] = new(160, 192),  ["sj"] = new(208, 160),
                ["sk"] = new(176, 192),  ["sl"] = new(192, 192),  ["sm"] = new(208, 192),  ["sn"] = new(224, 192),
                ["so"] = new(240, 192),  ["sr"] = new(0, 208),    ["ss"] = new(96, 240),   ["st"] = new(16, 208),
                ["sv"] = new(32, 208),   ["sx"] = new(64, 240),   ["sy"] = new(48, 208),   ["sz"] = new(64, 208),
                ["tc"] = new(80, 208),   ["td"] = new(96, 208),   ["tg"] = new(112, 208),  ["th"] = new(128, 208),
                ["tj"] = new(144, 208),  ["tl"] = new(160, 208),  ["tm"] = new(176, 208),  ["tn"] = new(192, 208),
                ["to"] = new(208, 208),  ["tr"] = new(224, 208),  ["tt"] = new(240, 208),  ["tv"] = new(0, 224),
                ["tw"] = new(16, 224),   ["tz"] = new(32, 224),   ["ua"] = new(48, 224),   ["ug"] = new(64, 224),
                ["us"] = new(80, 224),   ["uy"] = new(96, 224),   ["uz"] = new(112, 224),  ["va"] = new(128, 224),
                ["vc"] = new(144, 224),  ["ve"] = new(160, 224),  ["vg"] = new(176, 224),  ["vi"] = new(192, 224),
                ["vn"] = new(208, 224),  ["vu"] = new(224, 224),  ["ws"] = new(240, 224),  ["ye"] = new(0, 240),
                ["yt"] = new(144, 80),   ["za"] = new(16, 240),   ["zm"] = new(32, 240),   ["zw"] = new(48, 240)
            };
        }

        private static Rectangle? GetFlagRectangle(string countryCode)
        {
            if (string.IsNullOrEmpty(countryCode))
                return null;

            string code = countryCode.ToLowerInvariant();
            if (CountryCodeFlagPositions.TryGetValue(code, out Point position))
            {
                return new Rectangle(position.X, position.Y, FLAG_WIDTH, FLAG_HEIGHT);
            }

            return null;
        }

        /// <summary>
        /// Custom listbox that draws country flags.
        /// </summary>
        private class FlagListBox : XNAListBox
        {
            private readonly TunnelHandler tunnelHandler;
            private readonly Texture2D flagsSpriteSheet;

            public FlagListBox(WindowManager windowManager, TunnelHandler tunnelHandler, Texture2D flagsSpriteSheet)
                : base(windowManager)
            {
                this.tunnelHandler = tunnelHandler;
                this.flagsSpriteSheet = flagsSpriteSheet;
            }

            public override void Draw(GameTime gameTime)
            {
                DrawPanel();

                int height = 2 - (ViewTop % LineHeight);

                for (int i = TopIndex; i < Items.Count; i++)
                {
                    if (height > Height)
                        break;

                    Rectangle? flagRect = Items[i].Tag as Rectangle?;

                    if (flagRect.HasValue && flagsSpriteSheet != null)
                    {
                        int x = (Width - FLAG_WIDTH) / 2;
                        DrawTexture(flagsSpriteSheet,
                            flagRect.Value,
                            new Rectangle(x, height, FLAG_WIDTH, FLAG_HEIGHT),
                            Color.White);
                    }

                    height += LineHeight;
                }

                if (DrawBorders)
                    DrawPanelBorders();

                DrawChildren(gameTime);
            }
        }
    }
}
