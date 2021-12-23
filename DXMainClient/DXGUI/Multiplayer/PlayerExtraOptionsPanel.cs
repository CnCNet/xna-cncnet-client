using System;
using System.Collections.Generic;
using System.Linq;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer
{
    public class PlayerExtraOptionsPanel : XNAPanel
    {
        private const int maxStartCount = 8;
        private const int defaultX = 24;
        private const int locAssignmentPanelWidth = 100;
        private const int locAssingmentPanelHeight = 22;
        private const string customPresetName = "Custom";

        private XNAClientCheckBox chkBoxForceRandomSides;
        private XNAClientCheckBox chkBoxForceRandomTeams;
        private XNAClientCheckBox chkBoxForceRandomColors;
        private XNAClientCheckBox chkBoxForceRandomStarts;
        private XNAClientCheckBox chkBoxUseTeamStartMappings;
        private XNAClientDropDown ddTeamStartMappingPreset;
        private TeamStartMappingsPanel teamStartMappingsPanel;
        private bool _isHost;
        private bool ignoreMappingChanges;

        public EventHandler OptionsChanged;
        public EventHandler OnClose;

        private Map _map;

        public PlayerExtraOptionsPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        public bool IsForcedRandomSides() => chkBoxForceRandomSides.Checked;
        public bool IsForcedRandomTeams() => chkBoxForceRandomTeams.Checked;
        public bool IsForcedRandomColors() => chkBoxForceRandomColors.Checked;
        public bool IsForcedRandomStarts() => chkBoxForceRandomStarts.Checked;
        public bool IsUseTeamStartMappings() => chkBoxUseTeamStartMappings.Checked;

        private void Options_Changed(object sender, EventArgs e)
        {
            OptionsChanged?.Invoke(sender, e);
        }

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
            chkBoxForceRandomTeams.Checked = chkBoxForceRandomTeams.Checked || chkBoxUseTeamStartMappings.Checked;
            chkBoxForceRandomTeams.AllowChecking = !chkBoxUseTeamStartMappings.Checked;

            chkBoxForceRandomStarts.Checked = chkBoxForceRandomStarts.Checked || chkBoxUseTeamStartMappings.Checked;
            chkBoxForceRandomStarts.AllowChecking = !chkBoxUseTeamStartMappings.Checked;

            RefreshPresetDropdown();

            Options_Changed(sender, e);
        }

        private void RefreshTeamStartMappingsPanel()
        {
            teamStartMappingsPanel.EnableControls(_isHost && chkBoxUseTeamStartMappings.Checked);

            RefreshTeamStartMappingPanels();
        }

        private void AddLocationAssignments()
        {
            for (var i = 0; i < maxStartCount; i++)
            {
                var teamStartMappingPanel = new TeamStartMappingPanel(WindowManager);
                teamStartMappingPanel.ClientRectangle = GetLocationAssignmentPanelRectangle(i - 1);

                teamStartMappingsPanel.AddMappingPanel(teamStartMappingPanel);
            }

            teamStartMappingsPanel.MappingChanged += Mapping_Changed;
        }

        private Rectangle GetLocationAssignmentPanelRectangle(int index)
        {
            if (index == 3) // need to start a new column
                return new Rectangle(defaultX + locAssignmentPanelWidth + 10, 0, locAssignmentPanelWidth, locAssingmentPanelHeight);

            var lastControl = index >= 0 ? teamStartMappingsPanel.GetTeamStartMappingPanels()[index] : null;
            return new Rectangle(lastControl?.X ?? defaultX, lastControl?.Bottom + 4 ?? 0, locAssignmentPanelWidth, locAssingmentPanelHeight);
        }

        private void ClearTeamStartMappingSelections() =>
            teamStartMappingsPanel.GetTeamStartMappingPanels().ForEach(panel => panel.ClearSelections());

        private void RefreshTeamStartMappingPanels()
        {
            ClearTeamStartMappingSelections();
            var teamStartMappingPanels = teamStartMappingsPanel.GetTeamStartMappingPanels();
            for (var i = 0; i < teamStartMappingPanels.Count; i++)
            {
                var teamStartMappingPanel = teamStartMappingPanels[i];
                teamStartMappingPanel.ClearSelections();
                if (!IsUseTeamStartMappings())
                    continue;

                teamStartMappingPanel.EnableControls(_isHost && chkBoxUseTeamStartMappings.Checked && i < _map?.MaxPlayers);
                teamStartMappingPanel.UpdateStartCount(_map?.MaxPlayers ?? 0);
                RefreshTeamStartMappingPresets(_map?.TeamStartMappingPresets);
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

        private void RefreshPresetDropdown()
        {
            ddTeamStartMappingPreset.Visible = _isHost && chkBoxUseTeamStartMappings.Checked;
        }

        public override void Initialize()
        {
            Name = nameof(PlayerExtraOptionsPanel);
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 255), 1, 1);
            Visible = false;

            var btnClose = new XNAClientButton(WindowManager);
            btnClose.ClientRectangle = new Rectangle(0, 0, 0, 0);
            btnClose.IdleTexture = AssetLoader.LoadTexture("openedComboBoxArrow.png");
            btnClose.HoverTexture = AssetLoader.LoadTexture("openedComboBoxArrow.png");
            btnClose.LeftClick += (sender, args) => Disable();
            AddChild(btnClose);

            var lblHeader = new XNALabel(WindowManager);
            lblHeader.Name = nameof(lblHeader);
            lblHeader.Text = "Extra Player Options";
            lblHeader.ClientRectangle = new Rectangle(defaultX, 4, 0, 18);
            AddChild(lblHeader);

            chkBoxForceRandomSides = new XNAClientCheckBox(WindowManager);
            chkBoxForceRandomSides.Name = nameof(chkBoxForceRandomSides);
            chkBoxForceRandomSides.Text = "Force Random Sides";
            chkBoxForceRandomSides.ClientRectangle = new Rectangle(defaultX, lblHeader.Bottom + 4, 0, 0);
            chkBoxForceRandomSides.CheckedChanged += Options_Changed;
            AddChild(chkBoxForceRandomSides);

            chkBoxForceRandomColors = new XNAClientCheckBox(WindowManager);
            chkBoxForceRandomColors.Name = nameof(chkBoxForceRandomColors);
            chkBoxForceRandomColors.Text = "Force Random Colors";
            chkBoxForceRandomColors.ClientRectangle = new Rectangle(defaultX, chkBoxForceRandomSides.Bottom + 4, 0, 0);
            chkBoxForceRandomColors.CheckedChanged += Options_Changed;
            AddChild(chkBoxForceRandomColors);

            chkBoxForceRandomTeams = new XNAClientCheckBox(WindowManager);
            chkBoxForceRandomTeams.Name = nameof(chkBoxForceRandomTeams);
            chkBoxForceRandomTeams.Text = "Force Random Teams";
            chkBoxForceRandomTeams.ClientRectangle = new Rectangle(defaultX, chkBoxForceRandomColors.Bottom + 4, 0, 0);
            chkBoxForceRandomTeams.CheckedChanged += Options_Changed;
            AddChild(chkBoxForceRandomTeams);

            chkBoxForceRandomStarts = new XNAClientCheckBox(WindowManager);
            chkBoxForceRandomStarts.Name = nameof(chkBoxForceRandomStarts);
            chkBoxForceRandomStarts.Text = "Force Random Starts";
            chkBoxForceRandomStarts.ClientRectangle = new Rectangle(defaultX, chkBoxForceRandomTeams.Bottom + 4, 0, 0);
            chkBoxForceRandomStarts.CheckedChanged += Options_Changed;
            AddChild(chkBoxForceRandomStarts);

            chkBoxUseTeamStartMappings = new XNAClientCheckBox(WindowManager);
            chkBoxUseTeamStartMappings.Name = nameof(chkBoxUseTeamStartMappings);
            chkBoxUseTeamStartMappings.Text = "Use Team/Start Mappings:";
            chkBoxUseTeamStartMappings.ClientRectangle = new Rectangle(defaultX, chkBoxForceRandomStarts.Bottom + 4, 0, 0);
            chkBoxUseTeamStartMappings.CheckedChanged += ChkBoxUseTeamStartMappings_Changed;
            AddChild(chkBoxUseTeamStartMappings);

            ddTeamStartMappingPreset = new XNAClientDropDown(WindowManager);
            ddTeamStartMappingPreset.Name = nameof(ddTeamStartMappingPreset);
            ddTeamStartMappingPreset.ClientRectangle = new Rectangle(chkBoxUseTeamStartMappings.Right + 4, chkBoxUseTeamStartMappings.Y - 2, 150, 0);
            ddTeamStartMappingPreset.SelectedIndexChanged += DdTeamMappingPreset_SelectedIndexChanged;
            ddTeamStartMappingPreset.Visible = false;
            AddChild(ddTeamStartMappingPreset);

            teamStartMappingsPanel = new TeamStartMappingsPanel(WindowManager);
            teamStartMappingsPanel.ClientRectangle = new Rectangle(0, chkBoxUseTeamStartMappings.Bottom + 4, Width, Height - chkBoxUseTeamStartMappings.Bottom + 4);
            AddChild(teamStartMappingsPanel);

            AddLocationAssignments();

            base.Initialize();

            RefreshTeamStartMappingsPanel();
        }

        public void UpdateForMap(Map map)
        {
            if (_map == map)
                return;

            _map = map;

            RefreshTeamStartMappingPanels();
        }

        public List<TeamStartMapping> GetTeamStartMappings()
        {
            return chkBoxUseTeamStartMappings.Checked ? teamStartMappingsPanel.GetTeamStartMappings() : new List<TeamStartMapping>();
        }

        public void EnableControls(bool enable)
        {
            chkBoxForceRandomSides.InputEnabled = enable;
            chkBoxForceRandomColors.InputEnabled = enable;
            chkBoxForceRandomStarts.InputEnabled = enable;
            chkBoxForceRandomTeams.InputEnabled = enable;
            chkBoxUseTeamStartMappings.InputEnabled = enable;

            teamStartMappingsPanel.EnableControls(enable && chkBoxUseTeamStartMappings.Checked);
        }

        public PlayerExtraOptions GetPlayerExtraOptions() =>
            new PlayerExtraOptions()
            {
                IsForceRandomSides = IsForcedRandomSides(),
                IsForceRandomColors = IsForcedRandomColors(),
                IsForceRandomStarts = IsForcedRandomStarts(),
                IsForceRandomTeams = IsForcedRandomTeams(),
                IsUseTeamStartMappings = IsUseTeamStartMappings(),
                TeamStartMappings = GetTeamStartMappings()
            };

        public void SetPlayerExtraOptions(PlayerExtraOptions playerExtraOptions)
        {
            chkBoxForceRandomSides.Checked = playerExtraOptions.IsForceRandomSides;
            chkBoxForceRandomColors.Checked = playerExtraOptions.IsForceRandomColors;
            chkBoxForceRandomTeams.Checked = playerExtraOptions.IsForceRandomTeams;
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
