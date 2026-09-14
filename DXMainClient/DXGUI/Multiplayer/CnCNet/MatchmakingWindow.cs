#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ClientCore;
using ClientCore.Extensions;
using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet.Matchmaking;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    public class MatchmakingWindow : XNAWindow
    {
        private readonly MatchmakingService matchmakingService;

        private XNALabel lblTitle = null!;
        private XNAListBox lbModes = null!;
        private XNALabel lblModeHeader = null!;
        private XNALabel lblModeDescription = null!;
        private XNALabel lblRulesHeader = null!;
        private XNALabel lblRulesDescription = null!;
        private XNALabel lblStatus = null!;

        private XNAClientButton btnFindMatch = null!;
        private XNAClientButton btnClose = null!;

        private TimeSpan queueCountsTimer = TimeSpan.Zero;
        private readonly TimeSpan QueueCountsInterval = TimeSpan.FromSeconds(1.5);
        private bool isFetchingCounts;
        private Dictionary<string, int> cachedQueueCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public MatchmakingWindow(WindowManager windowManager, MatchmakingService matchmakingService) : base(windowManager)
        {
            this.matchmakingService = matchmakingService;
        }

        public override void Initialize()
        {
            if (Initialized)
                return;

            Name = nameof(MatchmakingWindow);
            ClientRectangle = new Rectangle(0, 0, 560, 360);
            BackgroundTexture = AssetLoader.LoadTexture("gamecreationoptionsbg.png");
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            lblTitle = new XNALabel(WindowManager)
            {
                Name = nameof(lblTitle),
                ClientRectangle = new Rectangle(16, 14, 0, 0),
                Text = "ONLINE CASUAL MATCHMAKING".L10N("Client:Matchmaking:Title"),
                FontIndex = 1
            };
            AddChild(lblTitle);

            var lblModesTitle = new XNALabel(WindowManager);
            lblModesTitle.Name = "lblModesTitle";
            lblModesTitle.ClientRectangle = new Rectangle(16, 42, 0, 0);
            lblModesTitle.Text = "Select Game Mode:".L10N("Client:Matchmaking:SelectMode");
            AddChild(lblModesTitle);

            lbModes = new XNAListBox(WindowManager)
            {
                Name = nameof(lbModes),
                ClientRectangle = new Rectangle(16, 62, 528, 110)
            };
            lbModes.SelectedIndexChanged += LbModes_SelectedIndexChanged;
            AddChild(lbModes);

            lblModeHeader = new XNALabel(WindowManager)
            {
                Name = nameof(lblModeHeader),
                ClientRectangle = new Rectangle(16, 180, 0, 0),
                Text = "Faction Balance:".L10N("Client:Matchmaking:FactionBalance"),
                FontIndex = 1
            };
            AddChild(lblModeHeader);

            lblModeDescription = new XNALabel(WindowManager)
            {
                Name = nameof(lblModeDescription),
                ClientRectangle = new Rectangle(16, 198, 528, 32),
                Text = string.Empty
            };
            AddChild(lblModeDescription);

            lblRulesHeader = new XNALabel(WindowManager)
            {
                Name = nameof(lblRulesHeader),
                ClientRectangle = new Rectangle(16, 236, 0, 0),
                Text = "Competitive Rules & Settings:".L10N("Client:Matchmaking:RulesHeader"),
                FontIndex = 1
            };
            AddChild(lblRulesHeader);

            lblRulesDescription = new XNALabel(WindowManager)
            {
                Name = nameof(lblRulesDescription),
                ClientRectangle = new Rectangle(16, 254, 528, 30),
                Text = "10,000 Credits | Game Speed 1 | Short Game | No Yuri | Crates Off".L10N("Client:Matchmaking:RulesDescription")
            };
            AddChild(lblRulesDescription);

            lblStatus = new XNALabel(WindowManager)
            {
                Name = nameof(lblStatus),
                ClientRectangle = new Rectangle(16, 292, 0, 0),
                Text = "Status: Ready".L10N("Client:Matchmaking:StatusReady")
            };
            AddChild(lblStatus);

            btnFindMatch = new XNAClientButton(WindowManager)
            {
                Name = nameof(btnFindMatch),
                ClientRectangle = new Rectangle(16, Height - 36, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT),
                Text = "Find Match".L10N("Client:Matchmaking:FindMatch")
            };
            btnFindMatch.LeftClick += BtnFindMatch_LeftClick;
            AddChild(btnFindMatch);

            btnClose = new XNAClientButton(WindowManager)
            {
                Name = nameof(btnClose),
                ClientRectangle = new Rectangle(Width - 16 - UIDesignConstants.BUTTON_WIDTH_92, btnFindMatch.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT),
                Text = "Close".L10N("Client:Main:ButtonClose")
            };
            btnClose.LeftClick += BtnClose_LeftClick;
            AddChild(btnClose);

            PopulateModesList();

            CenterOnParent();
            base.Initialize();

            UpdateSelectedModeDetails();
        }

        private string GetModeBaseTitle(MatchmakingModeInfo mode)
        {
            if (mode.Id.Equals("1v1", StringComparison.OrdinalIgnoreCase))
                return "1v1 - Solo Duel (2 Players: 1 Allies vs 1 Soviet)";
            if (mode.Id.Equals("2v2", StringComparison.OrdinalIgnoreCase))
                return "2v2 - Team Match (4 Players: Each Team 1 Allies + 1 Soviet)";
            if (mode.Id.Equals("3v3", StringComparison.OrdinalIgnoreCase))
                return "3v3 - Faction War (6 Players: Team A Allies vs Team B Soviets)";
            if (mode.Id.Equals("2v2v2v2", StringComparison.OrdinalIgnoreCase))
                return "2v2v2v2 - 4-Way Brawl (8 Players: 4 Teams of 1 Allies + 1 Soviet)";
            if (mode.Id.Equals("4v4", StringComparison.OrdinalIgnoreCase))
                return "4v4 - Team Battle (8 Players: 4 vs 4)";

            return $"{mode.UIName} ({mode.PlayerCount} Players)";
        }

        private void PopulateModesList()
        {
            lbModes.Clear();
            var modes = MatchmakingConfig.Instance.Modes;

            if (modes.Count == 0)
            {
                lbModes.AddItem(new XNAListBoxItem("1v1 - Quick Match (2 Players)") { Tag = "1v1" });
                return;
            }

            int selectedIdx = 0;
            for (int i = 0; i < modes.Count; i++)
            {
                var mode = modes[i];
                string baseTitle = GetModeBaseTitle(mode);
                string ladder = MatchmakingConfig.Instance.GetLadderForMode(mode);
                int count = cachedQueueCounts.TryGetValue(ladder, out int c) ? c : 0;
                string title = $"{baseTitle}  [{count} in queue]";

                var item = new XNAListBoxItem(title) { Tag = mode };
                lbModes.AddItem(item);

                if (mode == MatchmakingConfig.Instance.CurrentMode)
                {
                    selectedIdx = i;
                }
            }

            lbModes.SelectedIndex = selectedIdx;
        }

        private void UpdateModeItemTexts()
        {
            foreach (XNAListBoxItem item in lbModes.Items)
            {
                if (item.Tag is MatchmakingModeInfo mode)
                {
                    string ladder = MatchmakingConfig.Instance.GetLadderForMode(mode);
                    int count = cachedQueueCounts.TryGetValue(ladder, out int c) ? c : 0;
                    string baseTitle = GetModeBaseTitle(mode);
                    item.Text = $"{baseTitle}  [{count} in queue]";
                }
            }
        }

        private async void RefreshQueueCountsAsync()
        {
            if (isFetchingCounts || !Enabled || !Visible)
                return;

            isFetchingCounts = true;
            try
            {
                var counts = await matchmakingService.GetQueueCountsAsync();
                if (counts != null)
                {
                    cachedQueueCounts = new Dictionary<string, int>(counts, StringComparer.OrdinalIgnoreCase);
                    WindowManager.AddCallback(new Action(() =>
                    {
                        UpdateModeItemTexts();
                    }), null);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[MatchmakingWindow] Error updating queue counts: {ex.Message}");
            }
            finally
            {
                isFetchingCounts = false;
            }
        }

        private void LbModes_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lbModes.SelectedItem?.Tag is MatchmakingModeInfo mode)
            {
                MatchmakingConfig.Instance.CurrentMode = mode;
                UpdateSelectedModeDetails();
            }
        }

        private void UpdateSelectedModeDetails()
        {
            if (lblModeDescription == null || lblRulesDescription == null)
                return;

            var mode = MatchmakingConfig.Instance.CurrentMode;
            if (mode == null)
                return;

            if (mode.Id.Equals("1v1", StringComparison.OrdinalIgnoreCase))
            {
                lblModeDescription.Text = "Two players. One player is assigned a random Allied country,\nand the opponent is assigned a random Soviet country.".L10N("Client:Matchmaking:1v1Desc");
            }
            else if (mode.Id.Equals("2v2", StringComparison.OrdinalIgnoreCase))
            {
                lblModeDescription.Text = "Four players in 2 teams. Both teams are synchronized:\nTeam A (1 Allies + 1 Soviet) vs Team B (1 Allies + 1 Soviet).".L10N("Client:Matchmaking:2v2Desc");
            }
            else if (mode.Id.Equals("3v3", StringComparison.OrdinalIgnoreCase))
            {
                lblModeDescription.Text = "Six players in 2 teams. One team is 100% Allied,\nand the opposing team is 100% Soviet (all random countries).".L10N("Client:Matchmaking:3v3Desc");
            }
            else if (mode.Id.Equals("2v2v2v2", StringComparison.OrdinalIgnoreCase))
            {
                lblModeDescription.Text = "Eight players across 4 teams of two. Every single team has\nexactly 1 Allied and 1 Soviet player with random countries.".L10N("Client:Matchmaking:MultiTeamDesc");
            }
            else if (mode.Id.Equals("4v4", StringComparison.OrdinalIgnoreCase))
            {
                lblModeDescription.Text = "Eight players in 2 teams. Both teams are synchronized:\nTeam A (2 Allies + 2 Soviet) vs Team B (2 Allies + 2 Soviet).".L10N("Client:Matchmaking:4v4Desc");
            }
            else
            {
                lblModeDescription.Text = mode.Description;
            }

            int mapCount = mode.Maps.Count;
            lblRulesDescription.Text = $"10k Credits | Speed 1 | Short Game | No Yuri | Crates Off ({mapCount} Maps Available)";
        }

        public void Open()
        {
            Enable();
            PopulateModesList();
            UpdateSelectedModeDetails();
            UpdateQueueUi(matchmakingService.IsInQueue);
            queueCountsTimer = TimeSpan.Zero;
            RefreshQueueCountsAsync();
        }

        public void UpdateQueueUi(bool inQueue)
        {
            if (inQueue)
            {
                btnFindMatch.Text = "Cancel Search".L10N("Client:Matchmaking:CancelSearch");
                lbModes.Disable();
                TimeSpan elapsed = matchmakingService.QueueStartTime.HasValue
                    ? DateTime.UtcNow - matchmakingService.QueueStartTime.Value
                    : TimeSpan.Zero;
                lblStatus.Text = $"Status: Searching for {MatchmakingConfig.Instance.CurrentMode?.UIName ?? "match"}... ({elapsed.Minutes:D2}:{elapsed.Seconds:D2})";
            }
            else
            {
                btnFindMatch.Text = "Find Match".L10N("Client:Matchmaking:FindMatch");
                lbModes.Enable();
                lblStatus.Text = "Status: Ready".L10N("Client:Matchmaking:StatusReady");
            }

            RefreshQueueCountsAsync();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (matchmakingService.IsInQueue && matchmakingService.QueueStartTime.HasValue)
            {
                TimeSpan elapsed = DateTime.UtcNow - matchmakingService.QueueStartTime.Value;
                lblStatus.Text = $"Status: Searching for {MatchmakingConfig.Instance.CurrentMode?.UIName ?? "match"}... ({elapsed.Minutes:D2}:{elapsed.Seconds:D2})";
            }

            if (Enabled && Visible)
            {
                queueCountsTimer += gameTime.ElapsedGameTime;
                if (queueCountsTimer >= QueueCountsInterval)
                {
                    queueCountsTimer = TimeSpan.Zero;
                    RefreshQueueCountsAsync();
                }
            }
        }

        private void BtnFindMatch_LeftClick(object? sender, EventArgs e)
        {
            var mode = MatchmakingConfig.Instance.CurrentMode;
            if (matchmakingService.IsInQueue)
            {
                if (mode != null)
                {
                    string ladder = MatchmakingConfig.Instance.GetLadderForMode(mode);
                    if (cachedQueueCounts.TryGetValue(ladder, out int c) && c > 0)
                    {
                        cachedQueueCounts[ladder] = c - 1;
                    }
                    else
                    {
                        cachedQueueCounts[ladder] = 0;
                    }
                    UpdateModeItemTexts();
                }

                matchmakingService.LeaveQueue(true);
            }
            else
            {
                if (mode != null)
                {
                    string ladder = MatchmakingConfig.Instance.GetLadderForMode(mode);
                    if (cachedQueueCounts.TryGetValue(ladder, out int c))
                    {
                        cachedQueueCounts[ladder] = c + 1;
                    }
                    else
                    {
                        cachedQueueCounts[ladder] = 1;
                    }
                    UpdateModeItemTexts();
                }

                matchmakingService.StartQueue();
            }
        }

        private void BtnClose_LeftClick(object? sender, EventArgs e)
        {
            Disable();
        }
    }
}
