using Microsoft.Xna.Framework;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

using System;
using System.Collections.Generic;
using ClientGUI;
using System.Collections.Concurrent;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// A UI component that displays the tunnel negotiation status between players
    /// </summary>
    public class TunnelNegotiationStatusPanel : XNAPanel
    {
        private const int CELL_WIDTH = 70;
        private const int CELL_HEIGHT = 25;
        private const int HEADER_HEIGHT = 30;
        private const int PLAYER_NAME_WIDTH_LHS = 120;
        private const int PANEL_PADDING = 15;
        private const int TITLE_HEIGHT = 25;
        private const int CLOSE_BUTTON_SIZE = 20;

        private XNALabel lblTitle;
        private XNAPanel matrixPanel;
        private XNAClientButton btnClose;
        private readonly List<XNALabel> playerLabels = new List<XNALabel>();
        private readonly Dictionary<(string, string), XNALabel> statusCells = new Dictionary<(string, string), XNALabel>();

        public TunnelNegotiationStatusPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void Initialize()
        {
            Name = nameof(TunnelNegotiationStatusPanel);
            ClientRectangle = new Rectangle(0, 0, 500, 300);
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 180), 1, 1);
            DrawBorders = true;

            lblTitle = new XNALabel(WindowManager);
            lblTitle.Name = nameof(lblTitle);
            lblTitle.Text = "Tunnel Negotiation Status";
            lblTitle.ClientRectangle = new Rectangle(PANEL_PADDING, 8, 0, 0);
            lblTitle.FontIndex = 1;

            btnClose = new XNAClientButton(WindowManager);
            btnClose.Name = nameof(btnClose);
            btnClose.Text = "X";
            btnClose.ClientRectangle = new Rectangle(Width - CLOSE_BUTTON_SIZE - 8, 5, CLOSE_BUTTON_SIZE, CLOSE_BUTTON_SIZE);
            btnClose.LeftClick += BtnClose_LeftClick;

            matrixPanel = new XNAPanel(WindowManager);
            matrixPanel.Name = nameof(matrixPanel);
            matrixPanel.ClientRectangle = new Rectangle(PANEL_PADDING, TITLE_HEIGHT + PANEL_PADDING,
                Width - (PANEL_PADDING * 2), Height - TITLE_HEIGHT - (PANEL_PADDING * 2));
            matrixPanel.DrawBorders = false;

            AddChild(lblTitle);
            AddChild(btnClose);
            AddChild(matrixPanel);

            base.Initialize();

            CenterOnParent();
            Disable();
        }

        private void BtnClose_LeftClick(object sender, EventArgs e)
        {
            Disable();
        }

        public void UpdateNegotiationStatus(List<string> players,
            ConcurrentDictionary<string, ConcurrentDictionary<string, NegotiationStatus>> statuses,
            ConcurrentDictionary<string, ConcurrentDictionary<string, int>> pingMatrix)
        {
            while (matrixPanel.Children.Count > 0)
                matrixPanel.RemoveChild(matrixPanel.Children[0]);

            playerLabels.Clear();
            statusCells.Clear();

            if (players.Count < 2)
                return;

            int requiredWidth = PLAYER_NAME_WIDTH_LHS + (players.Count * CELL_WIDTH) + (PANEL_PADDING * 2);
            int requiredHeight = TITLE_HEIGHT + HEADER_HEIGHT + (players.Count * CELL_HEIGHT) + (PANEL_PADDING * 3);

            Width = Math.Max(500, requiredWidth);
            Height = Math.Max(300, requiredHeight);

            btnClose.ClientRectangle = new Rectangle(Width - CLOSE_BUTTON_SIZE - 8, 5, CLOSE_BUTTON_SIZE, CLOSE_BUTTON_SIZE);

            matrixPanel.ClientRectangle = new Rectangle(PANEL_PADDING, TITLE_HEIGHT + PANEL_PADDING,
                Width - (PANEL_PADDING * 2), Height - TITLE_HEIGHT - (PANEL_PADDING * 2));

            CenterOnParent();

            //player names along top
            for (int i = 0; i < players.Count; i++)
            {
                var headerLabel = new XNALabel(WindowManager);
                string displayName = players[i].Length > 10 ? players[i].Substring(0, 10) + ".." : players[i];
                headerLabel.Text = displayName;
                headerLabel.ClientRectangle = new Rectangle(
                    PLAYER_NAME_WIDTH_LHS + (i * CELL_WIDTH),
                    0,
                    CELL_WIDTH,
                    HEADER_HEIGHT);
                headerLabel.TextAnchor = LabelTextAnchorInfo.CENTER;
                headerLabel.TextColor = Color.LightBlue;
                matrixPanel.AddChild(headerLabel);
            }

            for (int i = 0; i < players.Count; i++)
            {
                //player name on left
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
                    {
                        // ignore diagonal
                        var selfCell = new XNALabel(WindowManager)
                        {
                            Text = "—",
                            ClientRectangle = new Rectangle(
                                PLAYER_NAME_WIDTH_LHS + (j * CELL_WIDTH),
                                HEADER_HEIGHT + (i * CELL_HEIGHT),
                                CELL_WIDTH,
                                CELL_HEIGHT),
                            TextAnchor = LabelTextAnchorInfo.CENTER,
                            TextColor = Color.Gray
                        };
                        matrixPanel.AddChild(selfCell);
                        continue;
                    }

                    var cellPanel = new XNAPanel(WindowManager)
                    {
                        ClientRectangle = new Rectangle(
                            PLAYER_NAME_WIDTH_LHS + (j * CELL_WIDTH),
                            HEADER_HEIGHT + (i * CELL_HEIGHT),
                            CELL_WIDTH,
                            CELL_HEIGHT),
                        BackgroundTexture = AssetLoader.CreateTexture(new Color(30, 30, 30, 120), 1, 1),
                        DrawBorders = true
                    };
                    matrixPanel.AddChild(cellPanel);

                    var statusCell = new XNALabel(WindowManager)
                    {
                        ClientRectangle = new Rectangle(0, 0, CELL_WIDTH, CELL_HEIGHT),
                        TextAnchor = LabelTextAnchorInfo.CENTER
                    };
                    statusCell.AnchorPoint = new Vector2(CELL_WIDTH / 2f, CELL_HEIGHT / 2f);

                    var status = GetNegotiationStatus(players[i], players[j], statuses);
                    var ping = GetPing(players[i], players[j], pingMatrix);

                    UpdateCell(statusCell, status, ping);
                    statusCell.AnchorPoint = new Vector2(CELL_WIDTH / 2f, CELL_HEIGHT / 2f);

                    cellPanel.AddChild(statusCell);
                    statusCells[(players[i], players[j])] = statusCell;
                }
            }
        }

        private NegotiationStatus GetNegotiationStatus(string player1, string player2,
            ConcurrentDictionary<string, ConcurrentDictionary<string, NegotiationStatus>> statuses)
        {
            // Check both directions, either player could be the reporter
            if (statuses.TryGetValue(player1, out var player1Statuses) &&
                player1Statuses.TryGetValue(player2, out var status))
                return status;

            if (statuses.TryGetValue(player2, out var player2Statuses) &&
                player2Statuses.TryGetValue(player1, out status))
                return status;

            return NegotiationStatus.NotStarted;
        }

        private int? GetPing(string player1, string player2,
            ConcurrentDictionary<string, ConcurrentDictionary<string, int>> pingMatrix)
        {
            if (pingMatrix.TryGetValue(player1, out var player1Pings) &&
                player1Pings.TryGetValue(player2, out var ping))
                return ping;

            if (pingMatrix.TryGetValue(player2, out var player2Pings) &&
                player2Pings.TryGetValue(player1, out ping))
                return ping;

            return null;
        }

        private void UpdateCell(XNALabel cell, NegotiationStatus status, int? ping)
        {
            cell.CenterOnParent();
            switch (status)
            {
                case NegotiationStatus.NotStarted:
                    cell.Text = "—";
                    cell.TextColor = Color.Gray;
                    break;
                case NegotiationStatus.InProgress:
                    cell.Text = "...";
                    cell.TextColor = Color.Yellow;
                    break;
                case NegotiationStatus.Succeeded:
                    if (ping.HasValue && ping.Value > 0)
                    {
                        cell.Text = $"{ping}ms";
                        if (ping < 50)
                            cell.TextColor = Color.LightGreen;
                        else if (ping < 100)
                            cell.TextColor = Color.Yellow;
                        else if (ping < 200)
                            cell.TextColor = Color.Orange;
                        else
                            cell.TextColor = Color.Red;
                    }
                    else
                    {
                        cell.Text = "OK";
                        cell.TextColor = Color.LightGreen;
                    }
                    break;
                case NegotiationStatus.Failed:
                    cell.Text = "FAIL";
                    cell.TextColor = Color.Red;
                    break;
                default:
                    cell.Text = "?";
                    cell.TextColor = Color.Gray;
                    break;
            }
        }

        public string GetStatusSummary(List<string> players,
            ConcurrentDictionary<string, ConcurrentDictionary<string, NegotiationStatus>> statuses)
        {
            if (players.Count < 2)
                return "No negotiations needed";

            int total = 0;
            int succeeded = 0;
            int failed = 0;
            int inProgress = 0;

            // Count unique player pairs (avoid counting both A->B and B->A)
            for (int i = 0; i < players.Count; i++)
            {
                for (int j = i + 1; j < players.Count; j++)
                {
                    total++;
                    var status = GetNegotiationStatus(players[i], players[j], statuses);
                    switch (status)
                    {
                        case NegotiationStatus.Succeeded:
                            succeeded++;
                            break;
                        case NegotiationStatus.Failed:
                            failed++;
                            break;
                        case NegotiationStatus.InProgress:
                            inProgress++;
                            break;
                    }
                }
            }

            if (total == 0)
                return "No negotiations needed";

            if (inProgress > 0)
                return $"Negotiations: {succeeded}/{total} complete ({inProgress} in progress)";
            else if (failed > 0)
                return $"Negotiations: {succeeded}/{total} succeeded ({failed} failed)";
            else if (succeeded == total)
                return "All negotiations complete!";
            else
                return $"Negotiations: {succeeded}/{total} complete";
        }
    }
}