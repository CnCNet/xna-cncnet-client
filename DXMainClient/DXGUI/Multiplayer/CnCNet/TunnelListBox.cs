using DTAClient.Domain.Multiplayer.CnCNet;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    /// <summary>
    /// A list box for listing CnCNet tunnel servers.
    /// </summary>
    class TunnelListBox : XNAMultiColumnListBox
    {
        private static readonly Dictionary<string, int> countryCodeFlagOffsets = ParseCountryCodeFlagOffsets();
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

            flagsSpriteSheet = AssetLoader.LoadTexture("flags16.png");

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

        public event EventHandler ListRefreshed;

        private readonly TunnelHandler tunnelHandler;
        private Texture2D flagsSpriteSheet;

        private int bestTunnelIndex = 0;
        private int lowestTunnelRating = int.MaxValue;

        private bool isManuallySelectedTunnel;
        private string manuallySelectedTunnelAddress;


        /// <summary>
        /// Selects a tunnel from the list with the given address.
        /// </summary>
        /// <param name="address">The address of the tunnel server to select.</param>
        public void SelectTunnel(string address)
        {
            int index = tunnelHandler.Tunnels.FindIndex(t => t.Address == address);
            if (index > -1)
            {
                SelectedIndex = index;
                isManuallySelectedTunnel = true;
                manuallySelectedTunnelAddress = address;
            }
        }

        /// <summary>
        /// Gets whether or not a tunnel from the list with the given address is selected.
        /// </summary>
        /// <param name="address">The address of the tunnel server</param>
        /// <returns>True if tunnel with given address is selected, otherwise false.</returns>
        public bool IsTunnelSelected(string address) =>
            tunnelHandler.Tunnels.FindIndex(t => t.Address == address) == SelectedIndex;

        private void TunnelHandler_TunnelsRefreshed(object sender, EventArgs e)
        {
            ClearItems();

            int tunnelIndex = 0;

            foreach (CnCNetTunnel tunnel in tunnelHandler.Tunnels)
            {
                List<string> info = new List<string>();

                info.Add(""); // Flag column
                info.Add(tunnel.Name);
                info.Add(Conversions.BooleanToString(tunnel.Official, BooleanStringStyle.YESNO));
                if (tunnel.PingInMs < 0)
                    info.Add("Unknown".L10N("Client:Main:UnknownPing"));
                else
                    info.Add(tunnel.PingInMs + " ms");
                info.Add(tunnel.Clients + " / " + tunnel.MaxClients);

                AddItem(info, true);

                if ((tunnel.Official || tunnel.Recommended) && tunnel.PingInMs > -1)
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

            if (tunnelHandler.Tunnels.Count > 0)
            {
                if (!isManuallySelectedTunnel)
                {
                    SelectedIndex = bestTunnelIndex;
                    isManuallySelectedTunnel = false;
                }
                else
                {
                    int manuallySelectedIndex = tunnelHandler.Tunnels.FindIndex(t => t.Address == manuallySelectedTunnelAddress);

                    if (manuallySelectedIndex == -1)
                    {
                        SelectedIndex = bestTunnelIndex;
                        isManuallySelectedTunnel = false;
                    }
                    else
                        SelectedIndex = manuallySelectedIndex;
                }
            }

            ListRefreshed?.Invoke(this, EventArgs.Empty);
        }

        private void TunnelHandler_TunnelPinged(int tunnelIndex)
        {
            XNAListBoxItem lbItem = GetItem(3, tunnelIndex);
            CnCNetTunnel tunnel = tunnelHandler.Tunnels[tunnelIndex];

            if (tunnel.PingInMs == -1)
                lbItem.Text = "Unknown".L10N("Client:Main:UnknownPing");
            else
            {
                lbItem.Text = tunnel.PingInMs + " ms";
                int rating = GetTunnelRating(tunnel);

                if (isManuallySelectedTunnel)
                    return;

                if ((tunnel.Recommended || tunnel.Official) && rating < lowestTunnelRating)
                {
                    bestTunnelIndex = tunnelIndex;
                    lowestTunnelRating = rating;
                    SelectedIndex = tunnelIndex;
                }
            }
        }

        private int GetTunnelRating(CnCNetTunnel tunnel)
        {
            double usageRatio = (double)tunnel.Clients / tunnel.MaxClients;

            if (usageRatio == 0)
                usageRatio = 0.1;

            usageRatio *= 100.0;

            return Convert.ToInt32(Math.Pow(tunnel.PingInMs, 2.0) * usageRatio);
        }

        private void TunnelListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!IsValidIndexSelected())
                return;

            isManuallySelectedTunnel = true;
            manuallySelectedTunnelAddress = tunnelHandler.Tunnels[SelectedIndex].Address;
        }

        private static Dictionary<string, int> ParseCountryCodeFlagOffsets()
        {
            // 16px version from
            // https://github.com/lafeber/world-flags-sprite
            // Offsets are located in the css files.

            var offsets = new Dictionary<string, int>();

            offsets["ad"] = 352; offsets["ae"] = 368; offsets["af"] = 384; offsets["ag"] = 400;
            offsets["ai"] = 416; offsets["al"] = 432; offsets["am"] = 448; offsets["ao"] = 464;
            offsets["aq"] = 480; offsets["ar"] = 496; offsets["as"] = 512; offsets["at"] = 528;
            offsets["au"] = 544; offsets["aw"] = 560; offsets["ax"] = 576; offsets["az"] = 592;
            offsets["ba"] = 608; offsets["bb"] = 624; offsets["bd"] = 640; offsets["be"] = 656;
            offsets["bf"] = 672; offsets["bg"] = 688; offsets["bh"] = 704; offsets["bi"] = 720;
            offsets["bj"] = 736; offsets["bl"] = 1424; offsets["bm"] = 752; offsets["bn"] = 768;
            offsets["bo"] = 784; offsets["bq"] = 2752; offsets["br"] = 800; offsets["bs"] = 816;
            offsets["bt"] = 832; offsets["bv"] = 2768; offsets["bw"] = 848; offsets["by"] = 864;
            offsets["bz"] = 880; offsets["ca"] = 896; offsets["cd"] = 912; offsets["cf"] = 928;
            offsets["cg"] = 944; offsets["ch"] = 960; offsets["ci"] = 976; offsets["ck"] = 992;
            offsets["cl"] = 1008; offsets["cm"] = 1024; offsets["cn"] = 1040; offsets["co"] = 1056;
            offsets["cp"] = 1424; offsets["cr"] = 1072; offsets["cu"] = 1088; offsets["cv"] = 1104;
            offsets["cw"] = 3920; offsets["cy"] = 1120; offsets["cz"] = 1136; offsets["de"] = 1152;
            offsets["dj"] = 1168; offsets["dk"] = 1184; offsets["dm"] = 1200; offsets["do"] = 1216;
            offsets["dz"] = 1232; offsets["ec"] = 1248; offsets["ee"] = 1264; offsets["eg"] = 1280;
            offsets["eh"] = 1296; offsets["er"] = 1312; offsets["es"] = 1328; offsets["et"] = 1344;
            offsets["fi"] = 1360; offsets["fj"] = 1376; offsets["fm"] = 1392; offsets["fo"] = 1408;
            offsets["fr"] = 1424; offsets["ga"] = 1440; offsets["gb"] = 1456; offsets["gd"] = 1472;
            offsets["ge"] = 1488; offsets["gg"] = 1504; offsets["gh"] = 1520; offsets["gi"] = 1536;
            offsets["gl"] = 1552; offsets["gm"] = 1568; offsets["gn"] = 1584; offsets["gp"] = 1600;
            offsets["gq"] = 1616; offsets["gr"] = 1632; offsets["gt"] = 1648; offsets["gu"] = 1664;
            offsets["gw"] = 1680; offsets["gy"] = 1696; offsets["hk"] = 1712; offsets["hn"] = 1728;
            offsets["hr"] = 1744; offsets["ht"] = 1760; offsets["hu"] = 1776; offsets["id"] = 1792;
            offsets["ie"] = 1808; offsets["il"] = 1824; offsets["im"] = 1840; offsets["in"] = 1856;
            offsets["iq"] = 1872; offsets["ir"] = 1888; offsets["is"] = 1904; offsets["it"] = 1920;
            offsets["je"] = 1936; offsets["jm"] = 1952; offsets["jo"] = 1968; offsets["jp"] = 1984;
            offsets["ke"] = 2000; offsets["kg"] = 2016; offsets["kh"] = 2032; offsets["ki"] = 2048;
            offsets["km"] = 2064; offsets["kn"] = 2080; offsets["kp"] = 2096; offsets["kr"] = 2112;
            offsets["kw"] = 2128; offsets["ky"] = 2144; offsets["kz"] = 2160; offsets["la"] = 2176;
            offsets["lb"] = 2192; offsets["lc"] = 2208; offsets["li"] = 2224; offsets["lk"] = 2240;
            offsets["lr"] = 2256; offsets["ls"] = 2272; offsets["lt"] = 2288; offsets["lu"] = 2304;
            offsets["lv"] = 2320; offsets["ly"] = 2336; offsets["ma"] = 2352; offsets["mc"] = 1792;
            offsets["md"] = 2368; offsets["me"] = 2384; offsets["mf"] = 1424; offsets["mg"] = 2400;
            offsets["mh"] = 2416; offsets["mk"] = 2432; offsets["ml"] = 2448; offsets["mm"] = 2464;
            offsets["mn"] = 2480; offsets["mo"] = 2496; offsets["mq"] = 2512; offsets["mr"] = 2528;
            offsets["ms"] = 2544; offsets["mt"] = 2560; offsets["mu"] = 2576; offsets["mv"] = 2592;
            offsets["mw"] = 2608; offsets["mx"] = 2624; offsets["my"] = 2640; offsets["mz"] = 2656;
            offsets["na"] = 2672; offsets["nc"] = 2688; offsets["ne"] = 2704; offsets["ng"] = 2720;
            offsets["ni"] = 2736; offsets["nl"] = 2752; offsets["no"] = 2768; offsets["np"] = 2784;
            offsets["nq"] = 2768; offsets["nr"] = 2800; offsets["nu"] = 3952; offsets["nz"] = 2816;
            offsets["om"] = 2832; offsets["pa"] = 2848; offsets["pe"] = 2864; offsets["pf"] = 2880;
            offsets["pg"] = 2896; offsets["ph"] = 2912; offsets["pk"] = 2928; offsets["pl"] = 2944;
            offsets["pr"] = 2960; offsets["ps"] = 2976; offsets["pt"] = 2992; offsets["pw"] = 3008;
            offsets["py"] = 3024; offsets["qa"] = 3040; offsets["re"] = 3056; offsets["ro"] = 3072;
            offsets["rs"] = 3088; offsets["ru"] = 3104; offsets["rw"] = 3120; offsets["sa"] = 3136;
            offsets["sb"] = 3152; offsets["sc"] = 3168; offsets["sd"] = 3184; offsets["se"] = 3200;
            offsets["sg"] = 3216; offsets["sh"] = 1456; offsets["si"] = 3232; offsets["sj"] = 2768;
            offsets["sk"] = 3248; offsets["sl"] = 3264; offsets["sm"] = 3280; offsets["sn"] = 3296;
            offsets["so"] = 3312; offsets["sr"] = 3328; offsets["ss"] = 3936; offsets["st"] = 3344;
            offsets["sv"] = 3360; offsets["sx"] = 3904; offsets["sy"] = 3376; offsets["sz"] = 3392;
            offsets["tc"] = 3408; offsets["td"] = 3424; offsets["tg"] = 3440; offsets["th"] = 3456;
            offsets["tj"] = 3472; offsets["tl"] = 3488; offsets["tm"] = 3504; offsets["tn"] = 3520;
            offsets["to"] = 3536; offsets["tr"] = 3552; offsets["tt"] = 3568; offsets["tv"] = 3584;
            offsets["tw"] = 3600; offsets["tz"] = 3616; offsets["ua"] = 3632; offsets["ug"] = 3648;
            offsets["us"] = 3664; offsets["uy"] = 3680; offsets["uz"] = 3696; offsets["va"] = 3712;
            offsets["vc"] = 3728; offsets["ve"] = 3744; offsets["vg"] = 3760; offsets["vi"] = 3776;
            offsets["vn"] = 3792; offsets["vu"] = 3808; offsets["ws"] = 3824; offsets["ye"] = 3840;
            offsets["yt"] = 1424; offsets["za"] = 3856; offsets["zm"] = 3872; offsets["zw"] = 3888;

            return offsets;
        }

        private static Rectangle? GetFlagRectangle(string countryCode)
        {
            if (string.IsNullOrEmpty(countryCode))
                return null;

            string code = countryCode.ToLowerInvariant();
            if (countryCodeFlagOffsets.TryGetValue(code, out int yOffset))
            {
                return new Rectangle(0, yOffset, FLAG_WIDTH, FLAG_HEIGHT);
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

                for (int i = TopIndex; i < Items.Count && i < tunnelHandler.Tunnels.Count; i++)
                {
                    if (height > Height)
                        break;

                    Rectangle? flagRect = GetFlagRectangle(tunnelHandler.Tunnels[i].CountryCode);

                    if (flagRect.HasValue)
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