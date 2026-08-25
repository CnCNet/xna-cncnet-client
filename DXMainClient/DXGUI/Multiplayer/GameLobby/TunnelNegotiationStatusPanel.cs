#nullable enable
using System;
using System.Collections.Generic;

using ClientCore.Extensions;

using ClientGUI;

using DTAClient.Domain.Multiplayer.CnCNet;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

/// <summary>
/// A UI component that displays the tunnel negotiation status between players
/// </summary>
public class TunnelNegotiationStatusPanel : XNAPanel
{
    private const int CELL_WIDTH = 90;
    private const int CELL_HEIGHT = 25;
    private const int HEADER_HEIGHT = 30;
    private const int PLAYER_NAME_WIDTH_LHS = 120;
    private const int PANEL_PADDING = 15;
    private const int TITLE_HEIGHT = 25;
    private const int CLOSE_BUTTON_SIZE = 20;
    private const int TAB_HEIGHT = 26;
    private const int LIST_PLAYER_COLUMN_WIDTH = 130;
    private const int LIST_BAR_COLUMN_WIDTH = 160;
    private const int LIST_PING_TEXT_COLUMN_WIDTH = 70;
    private const int LIST_BAR_MAX_WIDTH = 150;
    private const int LIST_BAR_MAX_PING = 500;
    private const int LIST_BAR_HEIGHT = 14;

    private const int LIST_MIN_VISIBLE_ROWS = 3;
    private const int RENEGOTIATE_BUTTON_HEIGHT = 25;

    public event EventHandler? RenegotiateAllRequested;

    private XNALabel lblTitle = null!;
    private XNAPanel matrixPanel = null!;
    private XNAMultiColumnListBox lbPairs = null!;
    private XNAClientTabControl tabControl = null!;
    private XNAClientButton btnClose = null!;
    private XNAClientButton btnRenegotiateAll = null!;
    private int listHeaderHeight;
    private readonly List<XNALabel> playerLabels = new List<XNALabel>();
    private readonly Dictionary<(string, string), XNALabel> statusCells = new Dictionary<(string, string), XNALabel>();
    private static Texture2D? sharedCellBackground;
    private static Texture2D? sharedBarBackground;
    private static Texture2D[]? pingBarTextures;

    public TunnelNegotiationStatusPanel(WindowManager windowManager) : base(windowManager)
    {
    }

    public override void Initialize()
    {
        Name = nameof(TunnelNegotiationStatusPanel);
        ClientRectangle = new Rectangle(0, 0, 500, 300);
        BackgroundTexture = AssetLoader.LoadTexture("ModalBG.png");
        PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
        DrawBorders = true;

        lblTitle = new XNALabel(WindowManager);
        lblTitle.Name = nameof(lblTitle);
        lblTitle.Text = "Tunnel Negotiation Status".L10N("Client:Main:NegStatusTitle");
        lblTitle.FontIndex = 1;
        lblTitle.TextAnchor = LabelTextAnchorInfo.CENTER;
        lblTitle.AnchorPoint = new Vector2(Width / 2f, TITLE_HEIGHT / 2f + 2);

        btnClose = new XNAClientButton(WindowManager);
        btnClose.Name = nameof(btnClose);
        btnClose.IdleTexture = AssetLoader.LoadTexture("optionsButtonClose.png");
        btnClose.HoverTexture = AssetLoader.LoadTexture("optionsButtonClose_c.png");
        btnClose.ClientRectangle = new Rectangle(Width - CLOSE_BUTTON_SIZE - 8, 5, CLOSE_BUTTON_SIZE, CLOSE_BUTTON_SIZE);
        btnClose.LeftClick += BtnClose_LeftClick;

        tabControl = new XNAClientTabControl(WindowManager);
        tabControl.Name = nameof(tabControl);
        tabControl.ClientRectangle = new Rectangle(PANEL_PADDING, TITLE_HEIGHT + 3, 0, 0);
        tabControl.AddTab("List".L10N("Client:Main:NegStatusTabList"), 92);
        tabControl.AddTab("Matrix".L10N("Client:Main:NegStatusTabMatrix"), 92);
        tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

        lbPairs = new XNAMultiColumnListBox(WindowManager);
        lbPairs.Name = nameof(lbPairs);
        lbPairs.ClientRectangle = new Rectangle(PANEL_PADDING, GetContentY(), ListTotalWidth, 0);
        lbPairs.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
        lbPairs.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
        lbPairs.AllowRightClickUnselect = false;

        lbPairs.AddColumn("Player 1".L10N("Client:Main:NegStatusPlayer1Header"), LIST_PLAYER_COLUMN_WIDTH);
        lbPairs.AddColumn("Player 2".L10N("Client:Main:NegStatusPlayer2Header"), LIST_PLAYER_COLUMN_WIDTH);

        // Ping bar column: custom list box that renders a colored bar per row.
        var barHeaderLabel = new XNALabel(WindowManager);
        barHeaderLabel.FontIndex = lbPairs.HeaderFontIndex;
        barHeaderLabel.X = 3;
        barHeaderLabel.Y = 2;
        barHeaderLabel.Text = "Ping".L10N("Client:Main:PingHeader");

        var barHeader = new XNAPanel(WindowManager);
        barHeader.Width = LIST_BAR_COLUMN_WIDTH;
        barHeader.Height = barHeaderLabel.Height + 3;
        barHeader.AddChild(barHeaderLabel);

        var barListBox = new PingBarListBox(WindowManager);
        barListBox.LineHeight = lbPairs.LineHeight;
        lbPairs.AddColumn(barHeader, barListBox);

        lbPairs.AddColumn(string.Empty, LIST_PING_TEXT_COLUMN_WIDTH);

        listHeaderHeight = barHeader.Height;

        matrixPanel = new XNAPanel(WindowManager);
        matrixPanel.Name = nameof(matrixPanel);
        matrixPanel.ClientRectangle = new Rectangle(PANEL_PADDING, GetContentY(), Width - PANEL_PADDING * 2, 0);
        matrixPanel.DrawBorders = false;

        btnRenegotiateAll = new XNAClientButton(WindowManager);
        btnRenegotiateAll.Name = nameof(btnRenegotiateAll);
        btnRenegotiateAll.Text = "Renegotiate All".L10N("Client:Main:RenegotiateAll");
        btnRenegotiateAll.ClientRectangle = new Rectangle(PANEL_PADDING, Height - RENEGOTIATE_BUTTON_HEIGHT - PANEL_PADDING, 160, RENEGOTIATE_BUTTON_HEIGHT);
        btnRenegotiateAll.LeftClick += (s, e) => RenegotiateAllRequested?.Invoke(this, EventArgs.Empty);

        AddChild(lblTitle);
        AddChild(btnClose);
        AddChild(tabControl);
        AddChild(lbPairs);
        AddChild(matrixPanel);
        AddChild(btnRenegotiateAll);

        base.Initialize();

        ApplyLayout(0);
        matrixPanel.Disable();
        btnRenegotiateAll.Disable();
        CenterOnParent();
        Disable();
    }

    private static int ListTotalWidth =>
        LIST_PLAYER_COLUMN_WIDTH * 2 + LIST_BAR_COLUMN_WIDTH + LIST_PING_TEXT_COLUMN_WIDTH;

    private static int GetContentY() => TITLE_HEIGHT + TAB_HEIGHT + PANEL_PADDING;

    public void SetIsHost(bool isHost)
    {
        if (isHost)
            btnRenegotiateAll.Enable();
        else
            btnRenegotiateAll.Disable();
    }

    private void TabControl_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (tabControl.SelectedTab == 0)
        {
            lbPairs.Enable();
            matrixPanel.Disable();
        }
        else
        {
            lbPairs.Disable();
            matrixPanel.Enable();
        }
    }

    private void BtnClose_LeftClick(object? sender, EventArgs e)
    {
        Disable();
    }

    public void UpdateNegotiationStatus(List<string> players, NegotiationDataManager negotiationData, bool inferInProgress = false)
    {
        while (matrixPanel.Children.Count > 0)
            matrixPanel.RemoveChild(matrixPanel.Children[0]);

        playerLabels.Clear();
        statusCells.Clear();

        int previousTopIndex = lbPairs.ItemCount > 0 ? lbPairs.TopIndex : 0;
        lbPairs.ClearItems();

        ApplyLayout(players.Count);
        CenterOnParent();

        if (players.Count < 2)
            return;

        BuildMatrixView(players, negotiationData, inferInProgress);
        BuildListView(players, negotiationData, previousTopIndex, inferInProgress);
    }

    private void ApplyLayout(int playerCount)
    {
        int visiblePlayerCount = Math.Max(playerCount, 2);
        int matrixWidth = PLAYER_NAME_WIDTH_LHS + (visiblePlayerCount * CELL_WIDTH) + (PANEL_PADDING * 2);
        int listWidth = ListTotalWidth + (PANEL_PADDING * 2);

        int matrixContentHeight = HEADER_HEIGHT + (visiblePlayerCount * CELL_HEIGHT);
        int listMinContentHeight = listHeaderHeight + (LIST_MIN_VISIBLE_ROWS * lbPairs.LineHeight) + 4;

        Width = Math.Max(500, Math.Max(matrixWidth, listWidth));
        Height = Math.Max(300, GetContentY() + Math.Max(matrixContentHeight, listMinContentHeight) + PANEL_PADDING + RENEGOTIATE_BUTTON_HEIGHT + PANEL_PADDING);

        lblTitle.AnchorPoint = new Vector2(Width / 2f, TITLE_HEIGHT / 2f + 2);
        btnClose.ClientRectangle = new Rectangle(Width - CLOSE_BUTTON_SIZE - 8, 5, CLOSE_BUTTON_SIZE, CLOSE_BUTTON_SIZE);
        btnRenegotiateAll.ClientRectangle = new Rectangle(
            PANEL_PADDING,
            Height - RENEGOTIATE_BUTTON_HEIGHT - PANEL_PADDING,
            160,
            RENEGOTIATE_BUTTON_HEIGHT);

        int contentY = GetContentY();
        int contentBottom = btnRenegotiateAll.Y - PANEL_PADDING;
        int contentHeight = Math.Max(0, contentBottom - contentY);

        lbPairs.ClientRectangle = new Rectangle(PANEL_PADDING, contentY, ListTotalWidth, contentHeight);
        matrixPanel.ClientRectangle = new Rectangle(PANEL_PADDING, contentY, Width - PANEL_PADDING * 2, contentHeight);
    }

    private void BuildMatrixView(List<string> players, NegotiationDataManager negotiationData, bool inferInProgress = false)
    {
        for (int i = 0; i < players.Count; i++)
        {
            var headerLabel = new XNALabel(WindowManager);
            string displayName = players[i];
            headerLabel.Text = displayName;
            headerLabel.TextAnchor = LabelTextAnchorInfo.CENTER;
            headerLabel.AnchorPoint = new Vector2(
                PLAYER_NAME_WIDTH_LHS + (i * CELL_WIDTH) + (CELL_WIDTH / 2f),
                HEADER_HEIGHT / 2f);
            headerLabel.TextColor = Color.LightBlue;
            matrixPanel.AddChild(headerLabel);
        }

        for (int i = 0; i < players.Count; i++)
        {
            var rowLabel = new XNALabel(WindowManager);
            rowLabel.Text = players[i];
            rowLabel.ClientRectangle = new Rectangle(
                0,
                HEADER_HEIGHT + (i * CELL_HEIGHT),
                PLAYER_NAME_WIDTH_LHS - 5,
                CELL_HEIGHT);
            rowLabel.TextColor = Color.LightBlue;
            matrixPanel.AddChild(rowLabel);
            playerLabels.Add(rowLabel);

            for (int j = 0; j < players.Count; j++)
            {
                if (i == j)
                    continue;

                sharedCellBackground ??= AssetLoader.CreateTexture(new Color(30, 30, 30, 120), 1, 1);

                var cellPanel = new XNAPanel(WindowManager)
                {
                    ClientRectangle = new Rectangle(
                        PLAYER_NAME_WIDTH_LHS + (j * CELL_WIDTH),
                        HEADER_HEIGHT + (i * CELL_HEIGHT),
                        CELL_WIDTH,
                        CELL_HEIGHT),
                    BackgroundTexture = sharedCellBackground,
                    DrawBorders = true
                };
                matrixPanel.AddChild(cellPanel);

                var statusCell = new XNALabel(WindowManager)
                {
                    ClientRectangle = new Rectangle(0, 0, CELL_WIDTH, CELL_HEIGHT),
                    TextAnchor = LabelTextAnchorInfo.CENTER
                };

                var status = negotiationData.GetNegotiationStatus(players[i], players[j]);
                var ping = negotiationData.GetPing(players[i], players[j]);
                var displayStatus = inferInProgress && status == NegotiationStatus.NotStarted
                    ? NegotiationStatus.InProgress : status;

                UpdateCell(statusCell, displayStatus, ping);
                statusCell.AnchorPoint = new Vector2(CELL_WIDTH / 2f, CELL_HEIGHT / 2f);

                cellPanel.AddChild(statusCell);
                statusCells[(players[i], players[j])] = statusCell;
            }
        }
    }

    private void BuildListView(List<string> players, NegotiationDataManager negotiationData,
        int previousTopIndex, bool inferInProgress = false)
    {
        var pairs = new List<(string p1, string p2, NegotiationStatus status, PingValue? ping)>();

        foreach (var (p1, p2) in negotiationData.GetPlayerPairs(players))
        {
            var status = negotiationData.GetNegotiationStatus(p1, p2);
            if (inferInProgress && status == NegotiationStatus.NotStarted)
                status = NegotiationStatus.InProgress;
            var ping = negotiationData.GetPing(p1, p2);
            pairs.Add((p1, p2, status, ping));
        }

        // Worst first, so problems are visible at a glance without scrolling:
        // failed pairs on top, then negotiated pairs from highest to lowest ping.
        pairs.Sort((a, b) =>
        {
            int rankA = GetSortRank(a.status, a.ping);
            int rankB = GetSortRank(b.status, b.ping);
            if (rankA != rankB)
                return rankA.CompareTo(rankB);
            if (a.ping.HasValue && b.ping.HasValue)
                return b.ping.Value.Milliseconds.CompareTo(a.ping.Value.Milliseconds);
            return 0;
        });

        EnsureBarTextures();

        foreach (var (p1, p2, status, ping) in pairs)
        {
            var (pingText, pingColor) = GetListRowLabel(status, ping);

            // The bar column's Tag carries the ping to render; null means no bar.
            object? barPing = status == NegotiationStatus.Succeeded && ping.HasValue && ping.Value.IsValid()
                ? ping.Value.Milliseconds
                : null;

            lbPairs.AddItem(new[]
            {
                new XNAListBoxItem(p1, Color.LightBlue) { Selectable = false },
                new XNAListBoxItem(p2, Color.LightBlue) { Selectable = false },
                new XNAListBoxItem(string.Empty) { Selectable = false, Tag = barPing },
                new XNAListBoxItem(pingText, pingColor) { Selectable = false }
            });
        }

        // Keep the user's scroll position across the frequent rebuilds.
        if (previousTopIndex > 0 && lbPairs.ItemCount > 0)
            lbPairs.SetTopIndex(Math.Min(previousTopIndex, lbPairs.ItemCount - 1));
    }

    private static int GetSortRank(NegotiationStatus status, PingValue? ping) => status switch
    {
        NegotiationStatus.Failed => 0,
        NegotiationStatus.Succeeded when ping.HasValue && ping.Value.IsValid() => 1,
        NegotiationStatus.Succeeded => 2,
        NegotiationStatus.InProgress => 3,
        NegotiationStatus.NotStarted => 4,
        _ => 5
    };

    private static (string text, Color color) GetListRowLabel(NegotiationStatus status, PingValue? ping) => status switch
    {
        NegotiationStatus.NotStarted => ("-", Color.Gray),
        NegotiationStatus.InProgress => ("...", Color.Yellow),
        NegotiationStatus.Succeeded when ping.HasValue => (ping.Value.ToString(), PingQualityVisuals.GetTextColor(ping.Value)),
        NegotiationStatus.Succeeded => ("OK".L10N("Client:Main:NegStatusOK"), Color.LightGreen),
        NegotiationStatus.Failed => ("FAIL".L10N("Client:Main:NegStatusFail"), Color.Red),
        _ => ("?", Color.Gray)
    };

    private static void EnsureBarTextures()
    {
        sharedBarBackground ??= AssetLoader.CreateTexture(new Color(30, 30, 30, 120), 1, 1);

        if (pingBarTextures != null)
            return;

        pingBarTextures = new Texture2D[PingQualityVisuals.TextureCount];
        foreach (PingQualityTier tier in Enum.GetValues(typeof(PingQualityTier)))
        {
            pingBarTextures[PingQualityVisuals.GetTextureIndex(tier)] =
                AssetLoader.CreateTexture(PingQualityVisuals.GetBarColor(tier), 1, 1);
        }
    }

    private static void UpdateCell(XNALabel cell, NegotiationStatus status, PingValue? ping)
    {
        (cell.Text, cell.TextColor) = GetListRowLabel(status, ping);
    }

    /// <summary>
    /// Column list box that renders each pair's ping as a horizontal bar whose fill width
    /// is proportional to the ping and whose color follows the shared ping tiers.
    /// Each item's Tag holds the ping in milliseconds (int), or null for no bar.
    /// Follows the same custom-column pattern as TunnelListBox's flag column.
    /// </summary>
    private class PingBarListBox : XNAListBox
    {
        public PingBarListBox(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void Draw(GameTime gameTime)
        {
            DrawPanel();

            EnsureBarTextures();

            int barHeight = Math.Min(LIST_BAR_HEIGHT, LineHeight - 2);
            int height = 2 - (ViewTop % LineHeight);

            for (int i = TopIndex; i < Items.Count; i++)
            {
                if (height > Height)
                    break;

                if (Items[i].Tag is int ms)
                {
                    int barY = height + ((LineHeight - barHeight) / 2);

                    DrawTexture(sharedBarBackground!,
                        new Rectangle(2, barY, LIST_BAR_MAX_WIDTH, barHeight), Color.White);

                    int fillWidth = Math.Max(2, Math.Min(LIST_BAR_MAX_WIDTH, ms * LIST_BAR_MAX_WIDTH / LIST_BAR_MAX_PING));
                    DrawTexture(pingBarTextures![PingQualityVisuals.GetTextureIndex(ms)],
                        new Rectangle(2, barY, fillWidth, barHeight), Color.White);
                }

                height += LineHeight;
            }

            if (DrawBorders)
                DrawPanelBorders();

            DrawChildren(gameTime);
        }
    }
}
