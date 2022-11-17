using System;
using System.Collections.Generic;
using System.Linq;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Events;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Requests;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Responses;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Services;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Utilities;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.QuickMatch
{
    public class QuickMatchLobbyPanel : INItializableWindow
    {
        private readonly QmService qmService;
        private readonly MapLoader mapLoader;

        public event EventHandler Exit;

        public event EventHandler LogoutEvent;

        private QuickMatchMapList mapList;
        private QuickMatchLobbyFooterPanel footerPanel;
        private XNAClientDropDown ddUserAccounts;
        private XNAClientDropDown ddNicknames;
        private XNAClientDropDown ddSides;
        private XNAPanel mapPreviewBox;
        private XNAPanel settingsPanel;
        private XNAClientButton btnMap;
        private XNAClientButton btnSettings;
        private XNALabel lblStats;

        private readonly EnhancedSoundEffect matchFoundSoundEffect;
        private readonly QmSettings qmSettings;

        public QuickMatchLobbyPanel(WindowManager windowManager, MapLoader mapLoader, QmService qmService, QmSettingsService qmSettingsService) : base(windowManager)
        {
            this.qmService = qmService;
            this.qmService.QmEvent += HandleQmEvent;

            this.mapLoader = mapLoader;

            qmSettings = qmSettingsService.GetSettings();
            matchFoundSoundEffect = new EnhancedSoundEffect(qmSettings.MatchFoundSoundFile);

            IniNameOverride = nameof(QuickMatchLobbyPanel);
        }

        public override void Initialize()
        {
            base.Initialize();

            mapList = FindChild<QuickMatchMapList>(nameof(mapList));
            mapList.MapSelectedEvent += HandleMapSelectedEventEvent;
            mapList.MapSideSelectedEvent += HandleMapSideSelectedEvent;

            footerPanel = FindChild<QuickMatchLobbyFooterPanel>(nameof(footerPanel));
            footerPanel.ExitEvent += (sender, args) => Exit?.Invoke(sender, args);
            footerPanel.LogoutEvent += BtnLogout_LeftClick;
            footerPanel.QuickMatchEvent += BtnQuickMatch_LeftClick;

            ddUserAccounts = FindChild<XNAClientDropDown>(nameof(ddUserAccounts));
            ddUserAccounts.SelectedIndexChanged += HandleUserAccountSelected;

            ddNicknames = FindChild<XNAClientDropDown>(nameof(ddNicknames));

            ddSides = FindChild<XNAClientDropDown>(nameof(ddSides));
            ddSides.SelectedIndexChanged += HandleSideSelected;
            ddSides.DisabledMouseScroll = true;

            mapPreviewBox = FindChild<XNAPanel>(nameof(mapPreviewBox));
            mapPreviewBox.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.CENTERED;

            settingsPanel = FindChild<XNAPanel>(nameof(settingsPanel));

            btnMap = FindChild<XNAClientButton>(nameof(btnMap));
            btnMap.LeftClick += (_, _) => EnableRightPanel(mapPreviewBox);

            btnSettings = FindChild<XNAClientButton>(nameof(btnSettings));
            btnSettings.LeftClick += (_, _) => EnableRightPanel(settingsPanel);

            lblStats = FindChild<XNALabel>(nameof(lblStats));

            EnabledChanged += EnabledChangedEvent;
        }

        private void HandleQmEvent(object sender, QmEvent qmEvent)
        {
            switch (qmEvent)
            {
                case QmLadderMapsEvent e:
                    HandleLadderMapsEvent(e.LadderMaps);
                    return;
                case QmLadderStatsEvent e:
                    HandleLadderStatsEvent(e.LadderStats);
                    return;
                case QmLoadingLadderStatsEvent:
                    HandleLoadingLadderStatsEvent();
                    return;
                case QmLaddersAndUserAccountsEvent e:
                    HandleLoadLadderAndUserAccountsEvent(e);
                    return;
                case QmUserAccountSelectedEvent e:
                    HandleUserAccountSelected(e.UserAccount);
                    return;
                case QmLoginEvent:
                    Enable();
                    return;
                case QmLogoutEvent:
                    HandleLogoutEvent();
                    return;
            }
        }

        private void EnableRightPanel(XNAControl control)
        {
            foreach (XNAControl parentChild in control.Parent.Children)
                parentChild.Disable();

            control.Enable();
        }

        private void EnabledChangedEvent(object sender, EventArgs e)
        {
            if (!Enabled)
                return;

            LoadLaddersAndUserAccountsAsync();
        }

        private void LoadLaddersAndUserAccountsAsync()
        {
            qmService.LoadLaddersAndUserAccountsAsync();
        }

        private void BtnQuickMatch_LeftClick(object sender, EventArgs eventArgs)
            => RequestQuickMatch();

        private void RequestQuickMatch()
        {
            QmRequest matchRequest = CreateMatchRequest();
            if (matchRequest == null)
            {
                XNAMessageBox.Show(WindowManager, QmStrings.GenericErrorTitle, QmStrings.UnableToCreateMatchRequestDataError);
                return;
            }

            qmService.RequestMatchAsync();
        }

        // private void HandleQuickMatchSpawnResponse(QmResponse qmResponse)
        // {
        //     XNAMessageBox.Show(WindowManager, QmStrings.GenericErrorTitle, "qm spawn");
        // }

        private QmMatchRequest CreateMatchRequest()
        {
            QmUserAccount userAccount = GetSelectedUserAccount();
            if (userAccount == null)
            {
                XNAMessageBox.Show(WindowManager, QmStrings.GenericErrorTitle, QmStrings.NoLadderSelectedError);
                return null;
            }

            QmSide side = GetSelectedSide();
            if (side == null)
            {
                XNAMessageBox.Show(WindowManager, QmStrings.GenericErrorTitle, QmStrings.NoSideSelectedError);
                return null;
            }

            if (side.IsRandom)
                side = GetRandomSide();

            return new QmMatchRequest { Side = side.LocalId };
        }

        private QmSide GetRandomSide()
        {
            int randomIndex = new Random().Next(0, ddSides.Items.Count - 2); // account for "Random"
            return ddSides.Items.Select(i => i.Tag as QmSide).ElementAt(randomIndex);
        }

        private QmSide GetSelectedSide()
            => ddSides.SelectedItem?.Tag as QmSide;

        private QmUserAccount GetSelectedUserAccount()
            => ddUserAccounts.SelectedItem?.Tag as QmUserAccount;

        private void BtnLogout_LeftClick(object sender, EventArgs eventArgs)
        {
            XNAMessageBox.ShowYesNoDialog(WindowManager, QmStrings.ConfirmationCaption, QmStrings.LogoutConfirmation, box =>
            {
                qmService.Logout();
                LogoutEvent?.Invoke(this, null);
            });
        }

        private void UserAccountsUpdated(IEnumerable<QmUserAccount> userAccounts)
        {
            ddUserAccounts.Items.Clear();
            foreach (QmUserAccount userAccount in userAccounts)
            {
                ddUserAccounts.AddItem(new XNADropDownItem() { Text = userAccount.Ladder.Name, Tag = userAccount });
            }

            if (ddUserAccounts.Items.Count == 0)
                return;

            string cachedLadder = qmService.GetCachedLadder();
            if (!string.IsNullOrEmpty(cachedLadder))
                ddUserAccounts.SelectedIndex = ddUserAccounts.Items.FindIndex(i => (i.Tag as QmUserAccount)?.Ladder.Abbreviation == cachedLadder);

            if (ddUserAccounts.SelectedIndex < 0)
                ddUserAccounts.SelectedIndex = 0;
        }

        /// <summary>
        /// Called when the QM service has finished the login process
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="userAccounts"></param>
        private void HandleLoadLadderAndUserAccountsEvent(QmLaddersAndUserAccountsEvent e)
            => UserAccountsUpdated(e.UserAccounts);

        private void HandleSideSelected(object sender, EventArgs eventArgs)
            => qmService.SetMasterSide(GetSelectedSide());

        /// <summary>
        /// Called when the user has selected a UserAccount from the drop down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void HandleUserAccountSelected(object sender, EventArgs eventArgs)
        {
            if (ddUserAccounts.SelectedItem?.Tag is not QmUserAccount selectedUserAccount)
                return;

            UpdateNickames(selectedUserAccount);
            UpdateSides(selectedUserAccount);
            mapList.Clear();
            qmService.SetUserAccount(selectedUserAccount);
        }

        private void LoadLadderMapsAsync(QmLadder ladder) => qmService.LoadLadderMapsForAbbrAsync(ladder.Abbreviation);

        private void LoadLadderStatsAsync(QmLadder ladder) => qmService.LoadLadderStatsForAbbrAsync(ladder.Abbreviation);

        /// <summary>
        /// Update the nicknames drop down
        /// </summary>
        /// <param name="selectedUserAccount"></param>
        private void UpdateNickames(QmUserAccount selectedUserAccount)
        {
            ddNicknames.Items.Clear();

            ddNicknames.AddItem(new XNADropDownItem() { Text = selectedUserAccount.Username, Tag = selectedUserAccount });

            ddNicknames.SelectedIndex = 0;
        }

        /// <summary>
        /// Update the top Sides dropdown
        /// </summary>
        /// <param name="selectedUserAccount"></param>
        private void UpdateSides(QmUserAccount selectedUserAccount)
        {
            ddSides.Items.Clear();

            QmLadder ladder = qmService.GetLadderForId(selectedUserAccount.LadderId);
            IEnumerable<QmSide> sides = ladder.Sides.Append(QmSide.CreateRandomSide());

            foreach (QmSide side in sides)
            {
                ddSides.AddItem(new XNADropDownItem { Text = side.Name, Tag = side });
            }

            if (ddSides.Items.Count > 0)
                ddSides.SelectedIndex = 0;
        }

        /// <summary>
        /// Called when the QM service has loaded new ladder maps for the ladder selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="qmLadderMapsEventArgs"></param>
        private void HandleLadderMapsEvent(IEnumerable<QmLadderMap> maps)
        {
            mapList.Clear();
            var ladderMaps = maps?.ToList() ?? new List<QmLadderMap>();
            if (!ladderMaps.Any())
                return;

            if (ddUserAccounts.SelectedItem?.Tag is not QmUserAccount selectedUserAccount)
                return;

            var ladder = qmService.GetLadderForId(selectedUserAccount.LadderId);

            mapList.AddItems(ladderMaps.Select(ladderMap => new QuickMatchMapListItem(WindowManager, ladderMap, ladder)));
        }

        /// <summary>
        /// Called when the QM service has loaded new ladder maps for the ladder selected
        /// </summary>
        private void HandleLadderStatsEvent(QmLadderStats stats)
            => lblStats.Text = stats == null ? "No stats found..." : $"Players waiting: {stats.QueuedPlayerCount}, Recent matches: {stats.RecentMatchCount}";

        /// <summary>
        /// Called when the QM service has loaded new ladder maps for the ladder selected
        /// </summary>
        private void HandleLoadingLadderStatsEvent() => lblStats.Text = QmStrings.LoadingStats;

        private void HandleUserAccountSelected(QmUserAccount userAccount)
        {
            mapPreviewBox.BackgroundTexture = null;
            LoadLadderMapsAsync(userAccount.Ladder);
            LoadLadderStatsAsync(userAccount.Ladder);
        }

        private void HandleMatchedEvent()
        {
        }

        /// <summary>
        /// Called when the user selects a map in the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="qmMap"></param>
        private void HandleMapSelectedEventEvent(object sender, QmLadderMap qmLadderMap)
        {
            if (qmLadderMap == null)
                return;

            Map map = mapLoader.GetMapForSHA(qmLadderMap.Hash);

            mapPreviewBox.BackgroundTexture = map?.LoadPreviewTexture();
            EnableRightPanel(mapPreviewBox);
        }

        private void HandleMapSideSelectedEvent(object sender, IEnumerable<int> mapSides)
        {
            qmService.SetMapSides(mapSides);
        }

        private void HandleLogoutEvent()
        {
            Disable();
            ClearUISelections();
        }

        public void ClearUISelections()
        {
            ddNicknames.Items.Clear();
            ddNicknames.SelectedIndex = -1;
            ddSides.Items.Clear();
            ddSides.SelectedIndex = -1;
            ddUserAccounts.Items.Clear();
            ddUserAccounts.SelectedIndex = -1;
            mapList.Clear();
            mapPreviewBox.BackgroundTexture = null;
        }
    }
}