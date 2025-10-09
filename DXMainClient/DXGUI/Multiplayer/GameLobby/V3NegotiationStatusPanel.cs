using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

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
        BackgroundTexture = AssetLoader.LoadTexture("gamecreationoptionsbg.png");
        PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
        DrawBorders = true;

        lblTitle = new XNALabel(WindowManager);
        lblTitle.Name = nameof(lblTitle);
        lblTitle.Text = "Tunnel Negotiation Status";
        lblTitle.ClientRectangle = new Rectangle(PANEL_PADDING, 8, 0, 0);
        lblTitle.FontIndex = 1;

        btnClose = new XNAClientButton(WindowManager);
        btnClose.Name = nameof(btnClose);
        btnClose.IdleTexture = AssetLoader.LoadTexture("optionsButtonClose.png");
        btnClose.HoverTexture = AssetLoader.LoadTexture("optionsButtonClose_c.png");
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

    public void UpdateNegotiationStatus(List<string> players, NegotiationDataManager negotiationData)
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

                var status = negotiationData.GetNegotiationStatus(players[i], players[j]);
                var ping = negotiationData.GetPing(players[i], players[j]);

                UpdateCell(statusCell, status, ping);
                statusCell.AnchorPoint = new Vector2(CELL_WIDTH / 2f, CELL_HEIGHT / 2f);

                cellPanel.AddChild(statusCell);
                statusCells[(players[i], players[j])] = statusCell;
            }
        }
    }

    private void UpdateCell(XNALabel cell, NegotiationStatus status, int? ping)
    {
        cell.CenterOnParent();
        switch (status)
        {
            case NegotiationStatus.NotStarted:
                cell.Text = "-";
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

}