using System;
using System.Collections.Generic;
using System.Linq;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using DTAClient.Online.EventArguments;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    public class TeamStartMappingPresetsWindow : XNAWindow
    {
        private readonly XNALabel lblHeader;

        private readonly XNADropDownItem ddiCreatePresetItem;

        private readonly XNADropDownItem ddiSelectPresetItem;

        private readonly XNAClientButton btnSave;

        private readonly XNAClientButton btnDelete;

        private readonly XNAClientDropDown ddPresetSelect;

        private readonly XNALabel lblNewPresetName;

        private readonly XNATextBox tbNewPresetName;

        private readonly XNAClientCheckBox chkBoxSetDefault;

        public EventHandler<TeamStartMappingPresetEventArgs> PresetSaved;

        public EventHandler<TeamStartMappingPresetEventArgs> PresetDeleted;

        private Map _map;

        public TeamStartMappingPresetsWindow(WindowManager windowManager) : base(windowManager)
        {
            ClientRectangle = new Rectangle(0, 0, 325, 185);

            const int margin = 10;

            lblHeader = new XNALabel(WindowManager);
            lblHeader.Name = nameof(lblHeader);
            lblHeader.FontIndex = 1;
            lblHeader.Text = "Edit Preset".L10N("UI:AutoAllyPresetWindow:EditPreset");
            lblHeader.ClientRectangle = new Rectangle(
                margin, margin,
                150, 22
            );

            var lblPresetName = new XNALabel(WindowManager);
            lblPresetName.Name = nameof(lblPresetName);
            lblPresetName.Text = "Preset".L10N("UI:AutoAllyPresetWindow:Preset");
            lblPresetName.ClientRectangle = new Rectangle(
                margin, lblHeader.Bottom + margin,
                150, 18
            );

            ddiCreatePresetItem = new XNADropDownItem();
            ddiCreatePresetItem.Text = "[Create New]".L10N("UI:AutoAllyPresetWindow:CreateNewPreset");

            ddiSelectPresetItem = new XNADropDownItem();
            ddiSelectPresetItem.Text = "[Select Preset]".L10N("UI:AutoAllyPresetWindow:SelectPreset");
            ddiSelectPresetItem.Selectable = false;

            ddPresetSelect = new XNAClientDropDown(WindowManager);
            ddPresetSelect.Name = nameof(ddPresetSelect);
            ddPresetSelect.ClientRectangle = new Rectangle(
                10, lblPresetName.Bottom + 2,
                150, 22
            );
            ddPresetSelect.SelectedIndexChanged += DropDownPresetSelect_SelectedIndexChanged;

            chkBoxSetDefault = new XNAClientCheckBox(WindowManager);
            chkBoxSetDefault.Name = nameof(chkBoxSetDefault);
            chkBoxSetDefault.ClientRectangle = new Rectangle(ddPresetSelect.Right + 12, ddPresetSelect.Y + 2, 100, 22);
            chkBoxSetDefault.Text = "Default for Map".L10N("UI:AutoAllyPresetWindow:SetDefaultCheckBox");

            lblNewPresetName = new XNALabel(WindowManager);
            lblNewPresetName.Name = nameof(lblNewPresetName);
            lblNewPresetName.Text = "New Preset Name".L10N("UI:AutoAllyPresetWindow:NewPresetName");
            lblNewPresetName.ClientRectangle = new Rectangle(
                margin, ddPresetSelect.Bottom + margin,
                150, 18
            );

            tbNewPresetName = new XNATextBox(WindowManager);
            tbNewPresetName.Name = nameof(tbNewPresetName);
            tbNewPresetName.ClientRectangle = new Rectangle(
                10, lblNewPresetName.Bottom + 2,
                150, 22
            );
            tbNewPresetName.TextChanged += (sender, args) => RefreshUI();

            btnSave = new XNAClientButton(WindowManager);
            btnSave.Name = nameof(btnSave);
            btnSave.LeftClick += BtnSave_LeftClick;
            btnSave.Text = "Save".L10N("UI:AutoAllyPresetWindow:ButtonSave");
            btnSave.ClientRectangle = new Rectangle(
                margin,
                Height - UIDesignConstants.BUTTON_HEIGHT - margin,
                UIDesignConstants.BUTTON_WIDTH_92,
                UIDesignConstants.BUTTON_HEIGHT
            );

            btnDelete = new XNAClientButton(WindowManager);
            btnDelete.Name = nameof(btnDelete);
            btnDelete.Text = "Delete".L10N("UI:AutoAllyPresetWindow:ButtonDelete");
            btnDelete.LeftClick += BtnDelete_LeftClick;
            btnDelete.ClientRectangle = new Rectangle(
                btnSave.Right + margin,
                btnSave.Y,
                UIDesignConstants.BUTTON_WIDTH_92,
                UIDesignConstants.BUTTON_HEIGHT
            );

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = nameof(btnCancel);
            btnCancel.Text = "Cancel".L10N("UI:AutoAllyPresetWindow:ButtonCancel");
            btnCancel.ClientRectangle = new Rectangle(
                btnDelete.Right + margin,
                btnSave.Y,
                UIDesignConstants.BUTTON_WIDTH_92,
                UIDesignConstants.BUTTON_HEIGHT
            );
            btnCancel.LeftClick += (sender, args) => Disable();

            AddChild(lblHeader);
            AddChild(lblPresetName);
            AddChild(ddPresetSelect);
            AddChild(chkBoxSetDefault);
            AddChild(lblNewPresetName);
            AddChild(tbNewPresetName);
            AddChild(btnSave);
            AddChild(btnDelete);
            AddChild(btnCancel);

            Disable();
        }

        public override void Initialize()
        {
            base.Initialize();

            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 255), 1, 1);
        }

        /// <summary>
        /// Show the window.
        /// </summary>
        public void Show(Map map, TeamStartMappingPreset preset)
        {
            _map = map;

            LoadPresets(preset);
            tbNewPresetName.Text = string.Empty;

            RefreshUI();
            CenterOnParent();
            Enable();
        }

        /// <summary>
        /// Refresh the state of the load/save button
        /// </summary>
        private void RefreshUI()
        {
            btnSave.Enabled = !IsCreatePresetSelected || !IsNewPresetNameFieldEmpty;
            btnDelete.Enabled = !IsCreatePresetSelected && !IsSelectPresetSelected;

            RefreshNewPresetNameTextBox();
            RefreshDefaultCheckBox();
        }

        private void RefreshNewPresetNameTextBox()
        {
            lblNewPresetName.Disable();
            tbNewPresetName.Disable();

            if (!IsCreatePresetSelected)
                return;

            lblNewPresetName.Enable();
            tbNewPresetName.Enable();
        }

        private void RefreshDefaultCheckBox()
        {
            chkBoxSetDefault.Disable();
            if (IsCreatePresetSelected && tbNewPresetName.Text.Trim().Length == 0)
                return;

            chkBoxSetDefault.Enable();
            chkBoxSetDefault.Checked = SelectedTeamStartMappingPreset?.IsDefaultForMap ?? false;
        }

        private bool IsCreatePresetSelected => ddPresetSelect.SelectedItem == ddiCreatePresetItem;
        private bool IsSelectPresetSelected => ddPresetSelect.SelectedItem == ddiSelectPresetItem;
        private bool IsNewPresetNameFieldEmpty => string.IsNullOrWhiteSpace(tbNewPresetName.Text);

        private TeamStartMappingPreset SelectedTeamStartMappingPreset => ddPresetSelect.SelectedItem?.Tag as TeamStartMappingPreset;

        private List<TeamStartMappingPreset> AllPresets
            => ddPresetSelect.Items
                .Select(i => i.Tag as TeamStartMappingPreset)
                .Where(preset => preset != null)
                .ToList();

        private XNADropDownItem CreateItem(TeamStartMappingPreset preset)
            => new XNADropDownItem
            {
                Text = preset.Name,
                Tag = preset
            };


        /// <summary>
        /// Populate the preset drop down from saved presets
        /// </summary>
        private void LoadPresets(TeamStartMappingPreset initialPreset)
        {
            ddPresetSelect.Items.Clear();
            ddPresetSelect.Items.Add(ddiCreatePresetItem);

            ddPresetSelect.Items.AddRange(_map.TeamStartMappingPresets
                .Select(CreateItem)
            );

            ddPresetSelect.Items.AddRange(TeamStartMappingUserPresets.Instance
                .GetPresets(_map)
                .OrderBy(preset => preset.Name)
                .Select(CreateItem));

            int initialIndex = ddPresetSelect.Items.FindIndex(i => i.Tag == initialPreset);
            ddPresetSelect.SelectedIndex = initialIndex == -1 ? 0 : initialIndex;
        }

        private void BtnSave_LeftClick(object sender, EventArgs e)
        {
            var selectedItem = ddPresetSelect.Items[ddPresetSelect.SelectedIndex];
            var preset = IsCreatePresetSelected
                ? new TeamStartMappingPreset
                {
                    Name = tbNewPresetName.Text
                }
                : selectedItem.Tag as TeamStartMappingPreset;

            if (preset == null)
                return;

            UpdateDefault(preset);

            PresetSaved?.Invoke(this, new TeamStartMappingPresetEventArgs(_map, preset));

            Disable();
        }

        private void UpdateDefault(TeamStartMappingPreset preset)
        {
            preset.IsDefaultForMap = chkBoxSetDefault.Checked;
            if (!preset.IsDefaultForMap)
                return;

            // clear the default flag for all others
            foreach (TeamStartMappingPreset teamStartMappingPreset in AllPresets.Where(p => p != preset))
                teamStartMappingPreset.IsDefaultForMap = false;
        }

        private void BtnDelete_LeftClick(object sender, EventArgs e)
        {
            if (IsCreatePresetSelected)
                return;

            var selectedItem = ddPresetSelect.Items[ddPresetSelect.SelectedIndex];
            var messageBox = XNAMessageBox.ShowYesNoDialog(WindowManager,
                "Confirm Preset Delete".L10N("UI:AutoAllyPresetWindow:ConfirmPresetDeleteTitle"),
                "Are you sure you want to delete this preset?".L10N("UI:AutoAllyPresetWindow:ConfirmPresetDeleteText") + "\n\n" + selectedItem.Text);
            messageBox.YesClickedAction = box =>
            {
                PresetDeleted?.Invoke(this, new TeamStartMappingPresetEventArgs(_map, selectedItem.Tag as TeamStartMappingPreset));
                ddPresetSelect.Items.Remove(selectedItem);
                ddPresetSelect.SelectedIndex = 0;
            };
        }

        /// <summary>
        /// Callback when the Preset drop down selection has changed
        /// </summary>
        private void DropDownPresetSelect_SelectedIndexChanged(object sender, EventArgs eventArgs)
        {
            if (IsCreatePresetSelected)
            {
                // show the field to specify a new name when "create" option is selected in drop down
                tbNewPresetName.Enable();
                lblNewPresetName.Enable();
            }
            else
            {
                // hide the field to specify a new name when an existing preset is selected
                tbNewPresetName.Disable();
                lblNewPresetName.Disable();
            }

            RefreshUI();
        }
    }
}
