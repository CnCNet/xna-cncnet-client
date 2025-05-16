using System;
using System.Collections.Generic;
using System.Linq;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer
{
    public class PlayerExtraOptionsPanel : XNAWindow
    {
        private const int maxStartCount = 8;
        private const int defaultX = 24;
        private const int defaultTeamStartMappingX = UIDesignConstants.EMPTY_SPACE_SIDES;
        private const int teamMappingPanelWidth = 50;
        private const int teamMappingPanelHeight = 22;
        private readonly string customPresetName = "Custom".L10N("Client:Main:CustomPresetName");

        private XNAClientCheckBox chkBoxForceRandomSides;
        private XNAClientCheckBox chkBoxForceNoTeams;
        private XNAClientCheckBox chkBoxForceRandomColors;
        private XNAClientCheckBox chkBoxForceRandomStarts;
        private XNAClientCheckBox chkBoxUseTeamStartMappings;
        private XNAClientDropDown ddTeamStartMappingPreset;
        private TeamStartMappingsPanel teamStartMappingsPanel;
        private bool _isHost;
        private bool ignoreMappingChanges;

        public EventHandler OptionsChanged;
        public EventHandler OnClose;

        private GameModeMap _gameModeMap;

        public PlayerExtraOptionsPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        public bool IsForcedRandomSides
        {
            get => chkBoxForceRandomSides.Checked;
            set => chkBoxForceNoTeams.Checked = value;
        }

        public bool IsForcedNoTeams
        {
            get => chkBoxForceNoTeams.Checked;
            set => chkBoxForceNoTeams.Checked = value;
        }

        private bool _isForcedNoTeamsAllowChecking = true;
        public bool IsForcedNoTeamsAllowChecking
        {
            get => _isForcedNoTeamsAllowChecking;
            set
            {
                _isForcedNoTeamsAllowChecking = value;
                RefreshChkBoxForceNoTeams_AllowChecking();
            }
        }

        public bool IsForcedRandomColors
        {
            get => chkBoxForceRandomColors.Checked;
            set => chkBoxForceRandomColors.Checked = value;
        }

        public bool IsForcedRandomStarts
        {
            get => chkBoxForceRandomStarts.Checked;
            set => chkBoxForceRandomStarts.Checked = value;
        }

        public bool IsUseTeamStartMappings
        {
            get => chkBoxUseTeamStartMappings.Checked;
            set => chkBoxUseTeamStartMappings.Checked = value;
        }

        public bool IsUseTeamStartMappingsAllowChecking
        {
            get => chkBoxUseTeamStartMappings.AllowChecking;
            set => chkBoxUseTeamStartMappings.AllowChecking = value;
        }

        private void Options_Changed(object sender, EventArgs e) => OptionsChanged?.Invoke(sender, e);

        private void Mapping_Changed(object sender, EventArgs e)
        {
            Options_Changed(sender, e);
            if (ignoreMappingChanges)
                return;

            ddTeamStartMappingPreset.SelectedIndex = 0;
        }

        private void ChkBoxUseTeamStartMappings_Changed(object sender, EventArgs e)
        {
            RefreshTeamStartMappingsPanel();
            chkBoxForceNoTeams.Checked = chkBoxForceNoTeams.Checked || chkBoxUseTeamStartMappings.Checked;
            RefreshChkBoxForceNoTeams_AllowChecking();

            RefreshPresetDropdown();

            Options_Changed(sender, e);
        }

        private void RefreshChkBoxForceNoTeams_AllowChecking()
            => chkBoxForceNoTeams.AllowChecking = IsForcedNoTeamsAllowChecking && !chkBoxUseTeamStartMappings.Checked;

        private void RefreshTeamStartMappingsPanel()
        {
            teamStartMappingsPanel.EnableControls(_isHost && chkBoxUseTeamStartMappings.Checked);

            RefreshTeamStartMappingPanels();
        }

        private void AddLocationAssignments()
        {
            for (int i = 0; i < maxStartCount; i++)
            {
                var teamStartMappingPanel = new TeamStartMappingPanel(WindowManager, i + 1);
                teamStartMappingPanel.ClientRectangle = GetTeamMappingPanelRectangle(i);

                teamStartMappingsPanel.AddMappingPanel(teamStartMappingPanel);
            }

            teamStartMappingsPanel.MappingChanged += Mapping_Changed;
        }

        private Rectangle GetTeamMappingPanelRectangle(int index)
        {
            const int maxColumnCount = 2;
            const int mappingPanelDefaultX = 4;
            const int mappingPanelDefaultY = 0;
            if (index > 0 && index % maxColumnCount == 0) // need to start a new column
                return new Rectangle(((index / maxColumnCount) * (teamMappingPanelWidth + mappingPanelDefaultX)) + 3, mappingPanelDefaultY, teamMappingPanelWidth, teamMappingPanelHeight);

            var lastControl = index > 0 ? teamStartMappingsPanel.GetTeamStartMappingPanels()[index - 1] : null;
            return new Rectangle(lastControl?.X ?? mappingPanelDefaultX, lastControl?.Bottom + 4 ?? mappingPanelDefaultY, teamMappingPanelWidth, teamMappingPanelHeight);
        }

        private void ClearTeamStartMappingSelections()
            => teamStartMappingsPanel.GetTeamStartMappingPanels().ForEach(panel => panel.ClearSelections());

        private void RefreshTeamStartMappingPanels()
        {
            ClearTeamStartMappingSelections();
            var teamStartMappingPanels = teamStartMappingsPanel.GetTeamStartMappingPanels();
            for (int i = 0; i < teamStartMappingPanels.Count; i++)
            {
                var teamStartMappingPanel = teamStartMappingPanels[i];
                teamStartMappingPanel.ClearSelections();
                if (!IsUseTeamStartMappings)
                    continue;

                teamStartMappingPanel.EnableControls(_isHost && chkBoxUseTeamStartMappings.Checked && _gameModeMap != null && _gameModeMap.AllowedStartingLocations.Contains(i + 1));
                RefreshTeamStartMappingPresets(_gameModeMap?.Map?.TeamStartMappingPresets);
            }
        }

        private void RefreshTeamStartMappingPresets(List<TeamStartMappingPreset> teamStartMappingPresets)
        {
            ddTeamStartMappingPreset.Items.Clear();
            ddTeamStartMappingPreset.AddItem(new XNADropDownItem
            {
                Text = customPresetName,
                Tag = new List<TeamStartMapping>()
            });
            ddTeamStartMappingPreset.SelectedIndex = 0;

            if (!(teamStartMappingPresets?.Any() ?? false)) return;

            teamStartMappingPresets.ForEach(preset => ddTeamStartMappingPreset.AddItem(new XNADropDownItem
            {
                Text = preset.Name,
                Tag = preset.TeamStartMappings
            }));
            ddTeamStartMappingPreset.SelectedIndex = 1;
        }

        private void DdTeamMappingPreset_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedItem = ddTeamStartMappingPreset.SelectedItem;
            if (selectedItem?.Text == customPresetName)
                return;

            var teamStartMappings = selectedItem?.Tag as List<TeamStartMapping>;

            ignoreMappingChanges = true;
            teamStartMappingsPanel.SetTeamStartMappings(teamStartMappings);
            ignoreMappingChanges = false;
        }

        private void RefreshPresetDropdown() => ddTeamStartMappingPreset.AllowDropDown = _isHost && chkBoxUseTeamStartMappings.Checked;

        public override void Initialize()
        {
            Name = nameof(PlayerExtraOptionsPanel);
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 255), 1, 1);
            Visible = false;

            var btnClose = new XNAClientButton(WindowManager);
            btnClose.Name = nameof(btnClose);
            btnClose.ClientRectangle = new Rectangle(0, 0, 0, 0);
            btnClose.IdleTexture = AssetLoader.LoadTexture("optionsButtonClose.png");
            btnClose.HoverTexture = AssetLoader.LoadTexture("optionsButtonClose_c.png");
            btnClose.LeftClick += (sender, args) => Disable();
            AddChild(btnClose);

            var lblHeader = new XNALabel(WindowManager);
            lblHeader.Name = nameof(lblHeader);
            lblHeader.Text = "Extra Player Options".L10N("Client:Main:ExtraPlayerOptions");
            lblHeader.ClientRectangle = new Rectangle(defaultX, 4, 0, 18);
            AddChild(lblHeader);

            chkBoxForceRandomSides = new XNAClientCheckBox(WindowManager);
            chkBoxForceRandomSides.Name = nameof(chkBoxForceRandomSides);
            chkBoxForceRandomSides.Text = "Force Random Sides".L10N("Client:Main:ForceRandomSides");
            chkBoxForceRandomSides.ClientRectangle = new Rectangle(defaultX, lblHeader.Bottom + 4, 0, 0);
            chkBoxForceRandomSides.CheckedChanged += Options_Changed;
            AddChild(chkBoxForceRandomSides);

            chkBoxForceRandomColors = new XNAClientCheckBox(WindowManager);
            chkBoxForceRandomColors.Name = nameof(chkBoxForceRandomColors);
            chkBoxForceRandomColors.Text = "Force Random Colors".L10N("Client:Main:ForceRandomColors");
            chkBoxForceRandomColors.ClientRectangle = new Rectangle(defaultX, chkBoxForceRandomSides.Bottom + 4, 0, 0);
            chkBoxForceRandomColors.CheckedChanged += Options_Changed;
            AddChild(chkBoxForceRandomColors);

            chkBoxForceNoTeams = new XNAClientCheckBox(WindowManager);
            chkBoxForceNoTeams.Name = nameof(chkBoxForceNoTeams);
            chkBoxForceNoTeams.Text = "Force No Teams".L10N("Client:Main:ForceNoTeams");
            chkBoxForceNoTeams.ClientRectangle = new Rectangle(defaultX, chkBoxForceRandomColors.Bottom + 4, 0, 0);
            chkBoxForceNoTeams.CheckedChanged += Options_Changed;
            AddChild(chkBoxForceNoTeams);

            chkBoxForceRandomStarts = new XNAClientCheckBox(WindowManager);
            chkBoxForceRandomStarts.Name = nameof(chkBoxForceRandomStarts);
            chkBoxForceRandomStarts.Text = "Force Random Starts".L10N("Client:Main:ForceRandomStarts");
            chkBoxForceRandomStarts.ClientRectangle = new Rectangle(defaultX, chkBoxForceNoTeams.Bottom + 4, 0, 0);
            chkBoxForceRandomStarts.CheckedChanged += Options_Changed;
            AddChild(chkBoxForceRandomStarts);

            /////////////////////////////

            chkBoxUseTeamStartMappings = new XNAClientCheckBox(WindowManager);
            chkBoxUseTeamStartMappings.Name = nameof(chkBoxUseTeamStartMappings);
            chkBoxUseTeamStartMappings.Text = "Enable Auto Allying:".L10N("Client:Main:EnableAutoAllying");
            chkBoxUseTeamStartMappings.ClientRectangle = new Rectangle(chkBoxForceRandomSides.X, chkBoxForceRandomStarts.Bottom + 20, 0, 0);
            chkBoxUseTeamStartMappings.CheckedChanged += ChkBoxUseTeamStartMappings_Changed;
            AddChild(chkBoxUseTeamStartMappings);

            var btnHelp = new XNAClientButton(WindowManager);
            btnHelp.Name = nameof(btnHelp);
            btnHelp.IdleTexture = AssetLoader.LoadTexture("questionMark.png");
            btnHelp.HoverTexture = AssetLoader.LoadTexture("questionMark_c.png");
            btnHelp.LeftClick += BtnHelp_LeftClick;
            btnHelp.ClientRectangle = new Rectangle(chkBoxUseTeamStartMappings.Right + 4, chkBoxUseTeamStartMappings.Y - 1, 0, 0);
            AddChild(btnHelp);

            var lblPreset = new XNALabel(WindowManager);
            lblPreset.Name = nameof(lblPreset);
            lblPreset.Text = "Presets:".L10N("Client:Main:Presets");
            lblPreset.ClientRectangle = new Rectangle(chkBoxUseTeamStartMappings.X, chkBoxUseTeamStartMappings.Bottom + 8, 0, 0);
            AddChild(lblPreset);

            ddTeamStartMappingPreset = new XNAClientDropDown(WindowManager);
            ddTeamStartMappingPreset.Name = nameof(ddTeamStartMappingPreset);
            ddTeamStartMappingPreset.ClientRectangle = new Rectangle(lblPreset.X + 50, lblPreset.Y - 2, 160, 0);
            ddTeamStartMappingPreset.SelectedIndexChanged += DdTeamMappingPreset_SelectedIndexChanged;
            ddTeamStartMappingPreset.AllowDropDown = true;
            AddChild(ddTeamStartMappingPreset);

            teamStartMappingsPanel = new TeamStartMappingsPanel(WindowManager);
            teamStartMappingsPanel.Name = nameof(teamStartMappingsPanel);
            teamStartMappingsPanel.ClientRectangle = new Rectangle(lblPreset.X, ddTeamStartMappingPreset.Bottom + 8, Width, Height - ddTeamStartMappingPreset.Bottom + 4);
            AddChild(teamStartMappingsPanel);

            AddLocationAssignments();

            base.Initialize();

            RefreshTeamStartMappingsPanel();
        }

        private void BtnHelp_LeftClick(object sender, EventArgs args)
        {
            XNAMessageBox.Show(WindowManager, "Auto Allying".L10N("Client:Main:AutoAllyingTitle"),
                ("Auto allying allows the host to assign starting locations to teams, not players.\n" +
                "When players are assigned to spawn locations, they will be auto assigned to teams based on these mappings.\n" +
                "This is best used with random teams and random starts. However, only random teams is required.\n" +
                "Manually specified starts will take precedence.").L10N("Client:Main:AutoAllyingText1") + "\n\n" +
                $"{TeamStartMapping.NO_PLAYER} : " + "Block this location from being assigned to a player.".L10N("Client:Main:AutoAllyingTextNoPlayer") + "\n" +
                $"{TeamStartMapping.NO_TEAM} : " + "Allow a player here, but don't assign a team.".L10N("Client:Main:AutoAllyingTextNoTeamV2")
            );
        }

        public void UpdateForGameModeMap(GameModeMap gameModeMap)
        {
            if (_gameModeMap == gameModeMap)
                return;

            _gameModeMap = gameModeMap;

            RefreshTeamStartMappingPanels();
        }

        public List<TeamStartMapping> GetTeamStartMappings()
            => chkBoxUseTeamStartMappings.Checked ?
                teamStartMappingsPanel.GetTeamStartMappings() : new List<TeamStartMapping>();

        public void EnableControls(bool enable)
        {
            chkBoxForceRandomSides.InputEnabled = enable;
            chkBoxForceRandomColors.InputEnabled = enable;
            chkBoxForceRandomStarts.InputEnabled = enable;
            chkBoxForceNoTeams.InputEnabled = enable;
            chkBoxUseTeamStartMappings.InputEnabled = enable;

            teamStartMappingsPanel.EnableControls(enable && chkBoxUseTeamStartMappings.Checked);
        }

        public PlayerExtraOptions GetPlayerExtraOptions()
            => new PlayerExtraOptions()
            {
                IsForceRandomSides = IsForcedRandomSides,
                IsForceRandomColors = IsForcedRandomColors,
                IsForceRandomStarts = IsForcedRandomStarts,
                IsForceNoTeams = IsForcedNoTeams,
                IsUseTeamStartMappings = IsUseTeamStartMappings,
                TeamStartMappings = GetTeamStartMappings()
            };

        public void SetPlayerExtraOptions(PlayerExtraOptions playerExtraOptions)
        {
            chkBoxForceRandomSides.Checked = playerExtraOptions.IsForceRandomSides;
            chkBoxForceRandomColors.Checked = playerExtraOptions.IsForceRandomColors;
            chkBoxForceNoTeams.Checked = playerExtraOptions.IsForceNoTeams;
            chkBoxForceRandomStarts.Checked = playerExtraOptions.IsForceRandomStarts;
            chkBoxUseTeamStartMappings.Checked = playerExtraOptions.IsUseTeamStartMappings;
            teamStartMappingsPanel.SetTeamStartMappings(playerExtraOptions.TeamStartMappings);
        }

        public void SetIsHost(bool isHost)
        {
            _isHost = isHost;
            RefreshPresetDropdown();
            EnableControls(_isHost);
        }
    }
}
