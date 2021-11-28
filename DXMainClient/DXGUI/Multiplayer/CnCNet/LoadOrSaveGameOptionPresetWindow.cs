using System;
using System.Linq;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using DTAClient.Online.EventArguments;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    public class LoadOrSaveGameOptionPresetWindow : XNAWindow
    {
        private bool _isLoad;

        private readonly XNALabel lblHeader;

        private readonly XNADropDownItem ddiCreatePresetItem;

        private readonly XNADropDownItem ddiSelectPresetItem;

        private readonly XNAClientButton btnLoadSave;

        private readonly XNAClientDropDown ddPresetSelect;

        private readonly XNALabel lblNewPresetName;

        private readonly XNATextBox tbNewPresetName;

        public EventHandler<GameOptionPresetEventArgs> PresetLoaded;

        public EventHandler<GameOptionPresetEventArgs> PresetSaved;

        public LoadOrSaveGameOptionPresetWindow(WindowManager windowManager) : base(windowManager)
        {
            ClientRectangle = new Rectangle(0, 0, 300, 185);

            var margin = 10;

            lblHeader = new XNALabel(WindowManager);
            lblHeader.FontIndex = 1;
            lblHeader.ClientRectangle = new Rectangle(
                margin, margin,
                150, 22
            );

            var lblPresetName = new XNALabel(WindowManager);
            lblPresetName.Text = "Preset Name";
            lblPresetName.ClientRectangle = new Rectangle(
                margin, lblHeader.Bottom + margin,
                150, 18
            );

            ddiCreatePresetItem = new XNADropDownItem();
            ddiCreatePresetItem.Text = "[Create New]";

            ddiSelectPresetItem = new XNADropDownItem();
            ddiSelectPresetItem.Text = "[Select Preset]";
            ddiSelectPresetItem.Selectable = false;

            ddPresetSelect = new XNAClientDropDown(WindowManager);
            ddPresetSelect.ClientRectangle = new Rectangle(
                10, lblPresetName.Bottom + 2,
                150, 22
            );
            ddPresetSelect.SelectedIndexChanged += DropDownPresetSelect_SelectedIndexChanged;

            lblNewPresetName = new XNALabel(WindowManager);
            lblNewPresetName.Text = "New Preset Name";
            lblNewPresetName.ClientRectangle = new Rectangle(
                margin, ddPresetSelect.Bottom + margin,
                150, 18
            );

            tbNewPresetName = new XNATextBox(WindowManager);
            tbNewPresetName.ClientRectangle = new Rectangle(
                10, lblNewPresetName.Bottom + 2,
                150, 22
            );
            tbNewPresetName.TextChanged += (sender, args) => RefreshLoadSaveBtn();

            btnLoadSave = new XNAClientButton(WindowManager);
            btnLoadSave.LeftClick += BtnLoadSave_LeftClick;
            btnLoadSave.ClientRectangle = new Rectangle(
                margin,
                Height - UIDesignConstants.BUTTON_HEIGHT - margin,
                UIDesignConstants.BUTTON_WIDTH_92,
                UIDesignConstants.BUTTON_HEIGHT
            );

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Text = "Cancel";
            btnCancel.ClientRectangle = new Rectangle(
                btnLoadSave.Right + margin,
                btnLoadSave.Y,
                UIDesignConstants.BUTTON_WIDTH_92,
                UIDesignConstants.BUTTON_HEIGHT
            );
            btnCancel.LeftClick += (sender, args) => Disable();

            AddChild(lblHeader);
            AddChild(lblPresetName);
            AddChild(ddPresetSelect);
            AddChild(lblNewPresetName);
            AddChild(tbNewPresetName);
            AddChild(btnLoadSave);
            AddChild(btnCancel);

            Disable();
        }

        /// <summary>
        /// Show the window.
        /// </summary>
        /// <param name="isLoad">The "mode" for the window: load vs save.</param>
        public void Show(bool isLoad)
        {
            _isLoad = isLoad;
            lblHeader.Text = $"{(_isLoad ? "Load" : "Save")} Preset";
            btnLoadSave.Text = _isLoad ? "Load" : "Save";

            if (_isLoad)
                ShowLoad();
            else
                ShowSave();

            CenterOnParent();
            Enable();
        }

        /// <summary>
        /// Callback when the Preset drop down selection has changed
        /// </summary>
        private void DropDownPresetSelect_SelectedIndexChanged(object sender, EventArgs eventArgs)
        {
            if (_isLoad)
                DropDownPresetSelect_SelectedIndexChanged_IsLoad();
            else
                DropDownPresetSelect_SelectedIndexChanged_IsSave();

            RefreshLoadSaveBtn();
        }

        /// <summary>
        /// Callback when the Preset drop down selection has changed during "load" mode
        /// </summary>
        private void DropDownPresetSelect_SelectedIndexChanged_IsLoad()
        {
            if (!IsSelectPresetSelected && ddPresetSelect.Items.Contains(ddiSelectPresetItem))
            {
                ddPresetSelect.Items.Remove(ddiSelectPresetItem);
                ddPresetSelect.SelectedIndex--;
            }
        }

        /// <summary>
        /// Callback when the Preset drop down selection has changed during "save" mode
        /// </summary>
        private void DropDownPresetSelect_SelectedIndexChanged_IsSave()
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
        }

        /// <summary>
        /// Refresh the state of the load/save button
        /// </summary>
        private void RefreshLoadSaveBtn()
        {
            if (_isLoad)
                btnLoadSave.Enabled = !IsSelectPresetSelected;
            else
                btnLoadSave.Enabled = !IsCreatePresetSelected || !IsNewPresetNameFieldEmpty;
        }

        private bool IsCreatePresetSelected => ddPresetSelect.SelectedItem == ddiCreatePresetItem;
        private bool IsSelectPresetSelected => ddPresetSelect.SelectedItem == ddiSelectPresetItem;
        private bool IsNewPresetNameFieldEmpty => string.IsNullOrWhiteSpace(tbNewPresetName.Text);

        /// <summary>
        /// Populate the preset drop down from saved presets
        /// </summary>
        private void LoadPresets()
        {
            ddPresetSelect.Items.Clear();
            ddPresetSelect.Items.Add(_isLoad ? ddiSelectPresetItem : ddiCreatePresetItem);
            ddPresetSelect.SelectedIndex = 0;

            ddPresetSelect.Items.AddRange(GameOptionPresets.Instance
                .GetPresetNames()
                .OrderBy(name => name)
                .Select(name => new XNADropDownItem()
                {
                    Text = name
                }));
        }

        /// <summary>
        /// Show the current window in the "load" mode context
        /// </summary>
        private void ShowLoad()
        {
            LoadPresets();

            // do not show fields to specify a preset name during "load" mode
            lblNewPresetName.Disable();
            tbNewPresetName.Disable();
        }

        /// <summary>
        /// Show the current window in the "save" mode context
        /// </summary>
        private void ShowSave()
        {
            LoadPresets();

            // show fields to specify a preset name during "save" mode
            lblNewPresetName.Enable();
            tbNewPresetName.Enable();
        }

        private void BtnLoadSave_LeftClick(object sender, EventArgs e)
        {
            var selectedItem = ddPresetSelect.Items[ddPresetSelect.SelectedIndex];
            if (_isLoad)
            {
                PresetLoaded?.Invoke(this, new GameOptionPresetEventArgs(selectedItem.Text));
            }
            else
            {
                var presetName = IsCreatePresetSelected ? tbNewPresetName.Text : selectedItem.Text;
                PresetSaved?.Invoke(this, new GameOptionPresetEventArgs(presetName));
            }

            Disable();
        }
    }
}
