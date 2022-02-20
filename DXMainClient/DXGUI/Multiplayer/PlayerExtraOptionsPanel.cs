using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using clientdx.DXGUI.Generic;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using DTAClient.DXGUI.Generic;
using DTAClient.DXGUI.Multiplayer.CnCNet;
using DTAClient.Extensions;
using DTAClient.Online.EventArguments;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer
{
    public class PlayerExtraOptionsPanel : XNAPanel
    {
        private const int maxStartCount = 8;
        private const int defaultX = 24;
        private const int defaultTeamStartMappingX = 200;
        private const int teamMappingPanelWidth = 50;
        private const int teamMappingPanelHeight = 22;

        private XNAClientCheckBox chkBoxForceRandomSides;
        private XNAClientCheckBox chkBoxForceRandomTeams;
        private XNAClientCheckBox chkBoxForceRandomColors;
        private XNAClientCheckBox chkBoxForceRandomStarts;
        private XNAClientCheckBox chkBoxUseTeamStartMappings;
        private XNATextBox tbTeamStartMappingPreset;
        private TeamStartMappingPresetMenu menuTeamStratMappingPresets;
        private XNAClientButton btnTeamStartMappingPresetMenu;
        private TeamStartMappingsPanel teamStartMappingsPanel;
        private PresetsWindow teamMappingPresetWindow;
        private bool _isHost;

        public EventHandler OptionsChanged;
        public EventHandler OnClose;
        public EventHandler<UserDefinedTeamStartMappingPresetEventArgs> PresetSaved;
        public EventHandler<string> PresetDeleted;

        private Map _map;

        public PlayerExtraOptionsPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        public bool IsForcedRandomSides() => chkBoxForceRandomSides.Checked;
        public bool IsForcedRandomTeams() => chkBoxForceRandomTeams.Checked;
        public bool IsForcedRandomColors() => chkBoxForceRandomColors.Checked;
        public bool IsForcedRandomStarts() => chkBoxForceRandomStarts.Checked;
        public bool IsUseTeamStartMappings() => chkBoxUseTeamStartMappings.Checked;

        private void Options_Changed(object sender, EventArgs e) => OptionsChanged?.Invoke(sender, e);

        private void Mapping_Changed(object sender, EventArgs e)
        {
            Options_Changed(sender, e);

            tbTeamStartMappingPreset.Text = TeamStartMappingPreset.CustomPresetName;
        }

        private void ChkBoxUseTeamStartMappings_Changed(object sender, EventArgs e)
        {
            RefreshTeamStartMappingsPanel();
            chkBoxForceRandomTeams.Checked = chkBoxForceRandomTeams.Checked || chkBoxUseTeamStartMappings.Checked;
            chkBoxForceRandomTeams.AllowChecking = !chkBoxUseTeamStartMappings.Checked;
            btnTeamStartMappingPresetMenu.AllowClick = chkBoxUseTeamStartMappings.Checked;

            // chkBoxForceRandomStarts.Checked = chkBoxForceRandomStarts.Checked || chkBoxUseTeamStartMappings.Checked;
            // chkBoxForceRandomStarts.AllowChecking = !chkBoxUseTeamStartMappings.Checked;

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

        public void RefreshTeamStartMappingPanels()
        {
            ClearTeamStartMappingSelections();
            var teamStartMappingPanels = teamStartMappingsPanel.GetTeamStartMappingPanels();
            for (int i = 0; i < teamStartMappingPanels.Count; i++)
            {
                var teamStartMappingPanel = teamStartMappingPanels[i];
                teamStartMappingPanel.ClearSelections();
                if (!IsUseTeamStartMappings())
                    continue;

                teamStartMappingPanel.EnableControls(_isHost && chkBoxUseTeamStartMappings.Checked && i < _map?.MaxPlayers);
                RefreshTeamStartMappingPresets(_map?.TeamStartMappingPresets);
            }
        }

        private TeamStartMappingPreset FindFirstPreset()
            => menuTeamStratMappingPresets.GetPresets()
                .FirstOrDefault(preset => !preset.IsCustom);

        private void RefreshTeamStartMappingPresets(List<TeamStartMappingPreset> teamStartMappingPresets)
        {
            menuTeamStratMappingPresets.ReinitItems();
            tbTeamStartMappingPreset.Text = TeamStartMappingPreset.CustomPresetName;

            if (!(teamStartMappingPresets?.Any() ?? false)) return;

            var mapPresets = teamStartMappingPresets.Where(p => !p.IsUserDefined).ToList();
            var userDefinedPresets = teamStartMappingPresets.Except(mapPresets).ToList();

            menuTeamStratMappingPresets.AddPresetList(userDefinedPresets, "User Presets:", TeamStartMappingPresetSelected);
            menuTeamStratMappingPresets.AddPresetList(mapPresets, "Map Presets:", TeamStartMappingPresetSelected);

            // this will select either the first Map preset or UserDefined preset
            tbTeamStartMappingPreset.Text = FindFirstPreset()?.Name ?? TeamStartMappingPreset.CustomPresetName;
        }

        private List<TeamStartMappingPreset> GetTeamStartMappingPresets()
            => menuTeamStratMappingPresets.GetPresets();

        private void TeamStartMappingPresetSelected(TeamStartMappingPreset teamStartMappingPreset)
        {
            if (teamStartMappingPreset == null)
                return;

            tbTeamStartMappingPreset.Text = teamStartMappingPreset.Name;
            teamStartMappingsPanel.MappingChanged -= Mapping_Changed;
            teamStartMappingsPanel.SetTeamStartMappings(teamStartMappingPreset.TeamStartMappings);
            teamStartMappingsPanel.MappingChanged += Mapping_Changed;
        }

        private void BtnTeamStartMappingPresetMenu_LeftClick(object sender, EventArgs e)
        {
            var point = new Point(btnTeamStartMappingPresetMenu.X - menuTeamStratMappingPresets.Width + 1, btnTeamStartMappingPresetMenu.Y + btnTeamStartMappingPresetMenu.Height - 1);
            menuTeamStratMappingPresets.Open(point, GetTeamStartMappingPreset());
        }

        private void RefreshPresetDropdown() 
            => btnTeamStartMappingPresetMenu.AllowClick = _isHost && chkBoxUseTeamStartMappings.Checked;

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

            /////////////////////////////

            chkBoxUseTeamStartMappings = new XNAClientCheckBox(WindowManager);
            chkBoxUseTeamStartMappings.Name = nameof(chkBoxUseTeamStartMappings);
            chkBoxUseTeamStartMappings.Text = "Enable Auto Allying:";
            chkBoxUseTeamStartMappings.ClientRectangle = new Rectangle(defaultTeamStartMappingX, lblHeader.Y, 0, 0);
            chkBoxUseTeamStartMappings.CheckedChanged += ChkBoxUseTeamStartMappings_Changed;
            AddChild(chkBoxUseTeamStartMappings);

            var btnHelp = new XNAClientButton(WindowManager);
            btnHelp.Name = nameof(btnHelp);
            btnHelp.IdleTexture = AssetLoader.LoadTexture("questionMark.png");
            btnHelp.HoverTexture = AssetLoader.LoadTexture("questionMark.png");
            btnHelp.LeftClick += BtnHelp_LeftClick;
            btnHelp.ClientRectangle = new Rectangle(chkBoxUseTeamStartMappings.Right + 4, chkBoxUseTeamStartMappings.Y - 1, 0, 0);
            AddChild(btnHelp);

            var lblPreset = new XNALabel(WindowManager);
            lblPreset.Name = nameof(lblPreset);
            lblPreset.Text = "Presets:";
            lblPreset.ClientRectangle = new Rectangle(chkBoxUseTeamStartMappings.X, chkBoxUseTeamStartMappings.Bottom + 8, 0, 0);
            AddChild(lblPreset);

            tbTeamStartMappingPreset = new XNATextBox(WindowManager);
            tbTeamStartMappingPreset.Name = nameof(tbTeamStartMappingPreset);
            tbTeamStartMappingPreset.InputEnabled = false;
            tbTeamStartMappingPreset.ClientRectangle = new Rectangle(lblPreset.X + 50, lblPreset.Y - 2, 143, 22);
            AddChild(tbTeamStartMappingPreset);

            btnTeamStartMappingPresetMenu = new XNAClientButton(WindowManager);
            btnTeamStartMappingPresetMenu.IdleTexture = AssetLoader.LoadTexture("menu-sm.png");
            btnTeamStartMappingPresetMenu.HoverTexture = AssetLoader.LoadTexture("menu-sm.png");
            btnTeamStartMappingPresetMenu.Name = nameof(btnTeamStartMappingPresetMenu);
            btnTeamStartMappingPresetMenu.ClientRectangle = new Rectangle(tbTeamStartMappingPreset.Right - 1, tbTeamStartMappingPreset.Y, 20, 22);
            btnTeamStartMappingPresetMenu.LeftClick += BtnTeamStartMappingPresetMenu_LeftClick;
            btnTeamStartMappingPresetMenu.AllowClick = false;
            AddChild(btnTeamStartMappingPresetMenu);

            menuTeamStratMappingPresets = new TeamStartMappingPresetMenu(WindowManager);
            menuTeamStratMappingPresets.Name = nameof(menuTeamStratMappingPresets);
            // menuTeamStratMappingPresets.ItemHeight = 20;
            menuTeamStratMappingPresets.ClientRectangle = new Rectangle(0, 0, tbTeamStartMappingPreset.Width, 0);
            AddChild(menuTeamStratMappingPresets);

            teamStartMappingsPanel = new TeamStartMappingsPanel(WindowManager);
            teamStartMappingsPanel.ClientRectangle = new Rectangle(200, tbTeamStartMappingPreset.Bottom + 8, Width, Height - tbTeamStartMappingPreset.Bottom + 4);
            AddChild(teamStartMappingsPanel);

            AddLocationAssignments();
            AddTeamStartMappingPresetWindow();

            base.Initialize();

            RefreshTeamStartMappingsPanel();
        }

        private void AddTeamStartMappingPresetWindow()
        {
            teamMappingPresetWindow = new PresetsWindow(WindowManager);
            teamMappingPresetWindow.PresetSaved += AddUserDefinedTeamStartMappingPreset;
            teamMappingPresetWindow.PresetDeleted += (sender, presetArgs) => PresetDeleted?.Invoke(sender, presetArgs.PresetName);
            AddChild(teamMappingPresetWindow);
        }

        private void AddUserDefinedTeamStartMappingPreset(object sender, PresetWindowEventArgs presetArgs)
        {
            if (string.IsNullOrWhiteSpace(presetArgs.PresetName))
                return;

            if (presetArgs.IsNew && GetTeamStartMappingPresets().Any(p => p.Name == presetArgs.PresetName))
            {
                XNAMessageBox.Show(WindowManager, "Warning", "A preset with this name already exists");
                return;
            }
            
            PresetSaved?.Invoke(this, new UserDefinedTeamStartMappingPresetEventArgs
            {
                Preset = new TeamStartMappingPreset
                {
                    Name = presetArgs.PresetName,
                    TeamStartMappings = GetTeamStartMappings(),
                    IsUserDefined = true
                }
            });
        }

        private void BtnHelp_LeftClick(object sender, EventArgs args)
        {
            XNAMessageBox.Show(WindowManager, "Auto Allying",
                "Auto allying allows the host to assign starting locations to teams, not players.\n" +
                "When players are assigned to spawn locations, they will be auto assigned to teams based on these mappings.\n" +
                "This is best used with random teams and random starts. However, only random teams is required.\n" +
                "Manually specified starts will take precedence.\n\n" +
                $"{TeamStartMapping.NO_TEAM} : Block this location from being assigned to a player.\n" +
                $"{TeamStartMapping.RANDOM_TEAM} : Allow a player here, but don't assign a team."
            );
        }

        public void UpdateForMap(Map map)
        {
            if (_map == map)
                return;

            _map = map;

            RefreshTeamStartMappingPanels();
        }

        public List<TeamStartMapping> GetTeamStartMappings()
            => chkBoxUseTeamStartMappings.Checked ? teamStartMappingsPanel.GetTeamStartMappings() : new List<TeamStartMapping>();

        public TeamStartMappingPreset GetTeamStartMappingPreset()
        {
            if (!IsUseTeamStartMappings())
                return null;

            return GetTeamStartMappingPresets().First(p => p.Name == tbTeamStartMappingPreset.Text);
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

        public PlayerExtraOptions GetPlayerExtraOptions()
            => new PlayerExtraOptions()
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
            SetTeamStartMappingsPreset(playerExtraOptions.TeamStartMappings); 
        }

        private void SetTeamStartMappingsPreset(List<TeamStartMapping> teamStartMappings)
        {
            var preset = GetTeamStartMappingPresets()
                .FirstOrDefault(p => p.TeamStartMappings.EqualsMappings(teamStartMappings));

            if (preset != null)
                tbTeamStartMappingPreset.Text = preset.Name;
        }

        public void SetIsHost(bool isHost)
        {
            _isHost = isHost;
            RefreshPresetDropdown();
            EnableControls(_isHost);
        }
    }
}
