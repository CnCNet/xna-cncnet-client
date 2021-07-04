using System;
using ClientCore;
using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig.OptionPanels
{
    class GameFilterOptionsPanel : XNAOptionsPanel
    {
        private const int minPlayerCount = 2;
        private const int maxPlayerCount = 8;

        private XNAClientCheckBox chkBoxSortAlpha;
        private XNAClientCheckBox chkBoxFriendsOnly;
        private XNAClientCheckBox chkBoxHideLockedGames;
        private XNAClientCheckBox chkBoxHidePasswordedGames;
        private XNAClientCheckBox chkBoxHideIncompatibleGames;
        private XNAClientDropDown ddMaxPlayerCount;
        private UserINISettings userIniSettings;

        public GameFilterOptionsPanel(WindowManager windowManager, UserINISettings userIniSettings) : base(windowManager, userIniSettings)
        {
            this.userIniSettings = userIniSettings;
        }

        public override void Initialize()
        {
            base.Initialize();

            Name = "GameFiltersWindow";

            const int gap = 12;

            chkBoxSortAlpha = new XNAClientCheckBox(WindowManager);
            chkBoxSortAlpha.Name = nameof(chkBoxSortAlpha);
            chkBoxSortAlpha.Text = "Sort Alphabetically";
            chkBoxSortAlpha.ClientRectangle = new Rectangle(
                gap, gap,
                0, 0
            );

            chkBoxFriendsOnly = new XNAClientCheckBox(WindowManager);
            chkBoxFriendsOnly.Name = nameof(chkBoxFriendsOnly);
            chkBoxFriendsOnly.Text = "Show Friend Games Only";
            chkBoxFriendsOnly.ClientRectangle = new Rectangle(
                gap, chkBoxSortAlpha.Y + UIDesignConstants.BUTTON_HEIGHT + gap,
                0, 0
            );

            chkBoxHideLockedGames = new XNAClientCheckBox(WindowManager);
            chkBoxHideLockedGames.Name = nameof(chkBoxHideLockedGames);
            chkBoxHideLockedGames.Text = "Hide Locked Games";
            chkBoxHideLockedGames.ClientRectangle = new Rectangle(
                gap, chkBoxFriendsOnly.Y + UIDesignConstants.BUTTON_HEIGHT + gap,
                0, 0
            );

            chkBoxHidePasswordedGames = new XNAClientCheckBox(WindowManager);
            chkBoxHidePasswordedGames.Name = nameof(chkBoxHidePasswordedGames);
            chkBoxHidePasswordedGames.Text = "Hide Passworded Games";
            chkBoxHidePasswordedGames.ClientRectangle = new Rectangle(
                gap, chkBoxHideLockedGames.Y + UIDesignConstants.BUTTON_HEIGHT + gap,
                0, 0
            );

            chkBoxHideIncompatibleGames = new XNAClientCheckBox(WindowManager);
            chkBoxHideIncompatibleGames.Name = nameof(chkBoxHideIncompatibleGames);
            chkBoxHideIncompatibleGames.Text = "Hide Incompatible Games";
            chkBoxHideIncompatibleGames.ClientRectangle = new Rectangle(
                gap, chkBoxHidePasswordedGames.Y + UIDesignConstants.BUTTON_HEIGHT + gap,
                0, 0
            );

            ddMaxPlayerCount = new XNAClientDropDown(WindowManager);
            ddMaxPlayerCount.Name = nameof(ddMaxPlayerCount);
            ddMaxPlayerCount.ClientRectangle = new Rectangle(
                gap, chkBoxHideIncompatibleGames.Y + UIDesignConstants.BUTTON_HEIGHT + gap,
                40, UIDesignConstants.BUTTON_HEIGHT
            );
            for (var i = minPlayerCount; i <= maxPlayerCount; i++)
            {
                ddMaxPlayerCount.AddItem(i.ToString());
            }

            var lblMaxPlayerCount = new XNALabel(WindowManager);
            lblMaxPlayerCount.Name = nameof(lblMaxPlayerCount);
            lblMaxPlayerCount.Text = "Max Player Count";
            lblMaxPlayerCount.ClientRectangle = new Rectangle(
                ddMaxPlayerCount.X + ddMaxPlayerCount.Width + gap, ddMaxPlayerCount.Y,
                0, UIDesignConstants.BUTTON_HEIGHT
            );

            var btnResetDefaults = new XNAClientButton(WindowManager);
            btnResetDefaults.Name = nameof(btnResetDefaults);
            btnResetDefaults.Text = "Reset Defaults";
            btnResetDefaults.ClientRectangle = new Rectangle(
                gap, ddMaxPlayerCount.Y + UIDesignConstants.BUTTON_HEIGHT + gap,
                UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT
            );
            btnResetDefaults.LeftClick += ResetDefaults;

            AddChild(chkBoxSortAlpha);
            AddChild(chkBoxFriendsOnly);
            AddChild(chkBoxHideLockedGames);
            AddChild(chkBoxHidePasswordedGames);
            AddChild(chkBoxHideIncompatibleGames);
            AddChild(lblMaxPlayerCount);
            AddChild(ddMaxPlayerCount);
            AddChild(btnResetDefaults);
        }

        public override bool Save()
        {
            userIniSettings.SortAlpha.Value = chkBoxSortAlpha.Checked;
            userIniSettings.ShowFriendGamesOnly.Value = chkBoxFriendsOnly.Checked;
            userIniSettings.HideLockedGames.Value = chkBoxHideLockedGames.Checked;
            userIniSettings.HidePasswordedGames.Value = chkBoxHidePasswordedGames.Checked;
            userIniSettings.HideIncompatibleGames.Value = chkBoxHideIncompatibleGames.Checked;
            userIniSettings.MaxPlayerCount.Value = int.Parse(ddMaxPlayerCount.SelectedItem.Text);

            return false;
        }

        public override void Load()
        {
            base.Load();

            chkBoxSortAlpha.Checked = userIniSettings.SortAlpha.Value;
            chkBoxFriendsOnly.Checked = userIniSettings.ShowFriendGamesOnly.Value;
            chkBoxHideLockedGames.Checked = userIniSettings.HideLockedGames.Value;
            chkBoxHidePasswordedGames.Checked = userIniSettings.HidePasswordedGames.Value;
            chkBoxHideIncompatibleGames.Checked = userIniSettings.HideIncompatibleGames.Value;
            ddMaxPlayerCount.SelectedIndex = ddMaxPlayerCount.Items.FindIndex(i => i.Text == userIniSettings.MaxPlayerCount.Value.ToString());
        }

        public void ResetDefaults(object sender, EventArgs e)
        {
            userIniSettings.ResetGameFilters();
            Load();
        }
    }
}
