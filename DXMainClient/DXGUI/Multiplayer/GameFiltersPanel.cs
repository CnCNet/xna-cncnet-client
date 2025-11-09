using System;
using System.Collections.Generic;
using System.Linq;
using ClientCore;
using ClientGUI;
using ClientCore.Extensions;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer
{
    public class GameFiltersPanel : XNAPanel
    {
        private const int minPlayerCount = 2;
        private const int maxPlayerCount = 8;
        private const int GAP = 12;

        private XNAClientCheckBox chkBoxFriendsOnly;
        private XNAClientCheckBox chkBoxHideLockedGames;
        private XNAClientCheckBox chkBoxHidePasswordedGames;
        private XNAClientCheckBox chkBoxHideIncompatibleGames;
        private XNAClientDropDown ddMaxPlayerCount;

        private GameLobbyBase gameLobby;
        private List<GameOptionFilterControl> gameOptionFilterControls = [];
        private bool gameOptionFiltersCreated = false;

        private class GameOptionFilterControl
        {
            public string OptionName { get; set; }
            public bool IsCheckbox { get; set; }
            public XNAClientDropDown DropDown { get; set; }
            public XNALabel Label { get; set; }
            public int IconX { get; set; }
            public int IconY { get; set; }
            public Texture2D CurrentIcon { get; set; }
            public string EnabledIcon { get; set; }
            public string DisabledIcon { get; set; }
            public string Icon { get; set; }
        }

        public GameFiltersPanel(WindowManager windowManager, GameLobbyBase gameLobby) : base(windowManager)
        {
            this.gameLobby = gameLobby;
        }

        public override void Initialize()
        {
            base.Initialize();

            Name = "GameFiltersWindow";
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0), Width, Height);

            var lblTitle = new XNALabel(WindowManager);
            lblTitle.Name = nameof(lblTitle);
            lblTitle.Text = "Game Filters".L10N("Client:Main:GameFilters");
            lblTitle.ClientRectangle = new Rectangle(
                GAP, GAP, 120, UIDesignConstants.BUTTON_HEIGHT
            );

            chkBoxFriendsOnly = new XNAClientCheckBox(WindowManager);
            chkBoxFriendsOnly.Name = nameof(chkBoxFriendsOnly);
            chkBoxFriendsOnly.Text = "Show Friend Games Only".L10N("Client:Main:FriendGameOnly");
            chkBoxFriendsOnly.ClientRectangle = new Rectangle(
                GAP, lblTitle.Y + UIDesignConstants.BUTTON_HEIGHT + GAP,
                0, 0
            );

            chkBoxHideLockedGames = new XNAClientCheckBox(WindowManager);
            chkBoxHideLockedGames.Name = nameof(chkBoxHideLockedGames);
            chkBoxHideLockedGames.Text = "Hide Locked Games".L10N("Client:Main:HideLockedGame");
            chkBoxHideLockedGames.ClientRectangle = new Rectangle(
                GAP, chkBoxFriendsOnly.Y + UIDesignConstants.BUTTON_HEIGHT + GAP,
                0, 0
            );

            chkBoxHidePasswordedGames = new XNAClientCheckBox(WindowManager);
            chkBoxHidePasswordedGames.Name = nameof(chkBoxHidePasswordedGames);
            chkBoxHidePasswordedGames.Text = "Hide Passworded Games".L10N("Client:Main:HidePasswordGame");
            chkBoxHidePasswordedGames.ClientRectangle = new Rectangle(
                GAP, chkBoxHideLockedGames.Y + UIDesignConstants.BUTTON_HEIGHT + GAP,
                0, 0
            );

            chkBoxHideIncompatibleGames = new XNAClientCheckBox(WindowManager);
            chkBoxHideIncompatibleGames.Name = nameof(chkBoxHideIncompatibleGames);
            chkBoxHideIncompatibleGames.Text = "Hide Incompatible Games".L10N("Client:Main:HideIncompatibleGame");
            chkBoxHideIncompatibleGames.ClientRectangle = new Rectangle(
                GAP, chkBoxHidePasswordedGames.Y + UIDesignConstants.BUTTON_HEIGHT + GAP,
                0, 0
            );

            ddMaxPlayerCount = new XNAClientDropDown(WindowManager);
            ddMaxPlayerCount.Name = nameof(ddMaxPlayerCount);
            ddMaxPlayerCount.ClientRectangle = new Rectangle(
                GAP, chkBoxHideIncompatibleGames.Y + UIDesignConstants.BUTTON_HEIGHT + GAP,
                40, UIDesignConstants.BUTTON_HEIGHT
            );
            for (int i = minPlayerCount; i <= maxPlayerCount; i++)
            {
                ddMaxPlayerCount.AddItem(i.ToString());
            }

            var lblMaxPlayerCount = new XNALabel(WindowManager);
            lblMaxPlayerCount.Name = nameof(lblMaxPlayerCount);
            lblMaxPlayerCount.Text = "Max Player Count".L10N("Client:Main:MaxPlayerCount");
            lblMaxPlayerCount.ClientRectangle = new Rectangle(
                ddMaxPlayerCount.X + ddMaxPlayerCount.Width + GAP, ddMaxPlayerCount.Y,
                0, UIDesignConstants.BUTTON_HEIGHT
            );

            var btnResetDefaults = new XNAClientButton(WindowManager);
            btnResetDefaults.Name = nameof(btnResetDefaults);
            btnResetDefaults.Text = "Reset Defaults".L10N("Client:Main:ResetDefaults");
            btnResetDefaults.ClientRectangle = new Rectangle(
                GAP, ddMaxPlayerCount.Y + UIDesignConstants.BUTTON_HEIGHT + GAP,
                UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT
            );
            btnResetDefaults.LeftClick += BtnResetDefaults_LeftClick;

            var btnSave = new XNAClientButton(WindowManager);
            btnSave.Name = nameof(btnSave);
            btnSave.Text = "Save".L10N("Client:Main:ButtonSave");
            btnSave.ClientRectangle = new Rectangle(
                GAP, btnResetDefaults.Y + UIDesignConstants.BUTTON_HEIGHT + GAP,
                UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT
            );
            btnSave.LeftClick += BtnSave_LeftClick;

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = nameof(btnCancel);
            btnCancel.Text = "Cancel".L10N("Client:Main:ButtonCancel");
            btnCancel.ClientRectangle = new Rectangle(
                Width - GAP - UIDesignConstants.BUTTON_WIDTH_92, btnSave.Y,
                UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT
            );
            btnCancel.LeftClick += BtnCancel_LeftClick;

            AddChild(lblTitle);
            AddChild(chkBoxFriendsOnly);
            AddChild(chkBoxHideLockedGames);
            AddChild(chkBoxHidePasswordedGames);
            AddChild(chkBoxHideIncompatibleGames);
            AddChild(lblMaxPlayerCount);
            AddChild(ddMaxPlayerCount);
            AddChild(btnResetDefaults);
            AddChild(btnSave);
            AddChild(btnCancel);
        }

        private void CreateGameOptionFilters()
        {
            // Note: broadcasted checkboxes are converted to dropdowns so we can have a third - undefined - value.

            if (gameLobby == null)
                return;

            int currentY = ddMaxPlayerCount.Y + UIDesignConstants.BUTTON_HEIGHT + GAP;
            const int iconLabelSpacing = 6;
            const int itemVerticalSpacing = 4;
            const int minLabelRowHeight = 18;

            int dropdownWidth = (Width - (GAP * 3)) / 2;

            var divider = CreateDivider(currentY);
            AddChild(divider);
            currentY += divider.Height + GAP;

            int leftColumnX = GAP;
            int rightColumnX = Width / 2 + GAP / 2;
            int filterIndex = 0;
            int maxItemHeight = 0;

            // Create filters for broadcastable checkboxes
            var broadcastableCheckboxes = gameLobby.CheckBoxes.Where(cb => cb.BroadcastToLobby && cb.IconShownInFilters).ToList();
            foreach (var checkbox in broadcastableCheckboxes)
            {
                var filterControl = new GameOptionFilterControl
                {
                    OptionName = checkbox.Name,
                    IsCheckbox = true,
                    EnabledIcon = checkbox.EnabledIcon,
                    DisabledIcon = checkbox.DisabledIcon
                };

                Texture2D icon = null;
                if (!string.IsNullOrEmpty(checkbox.EnabledIcon))
                    icon = AssetLoader.LoadTexture(checkbox.EnabledIcon);

                int iconWidth = icon?.Width ?? 0;
                int iconHeight = icon?.Height ?? 0;

                bool isLeftColumn = (filterIndex % 2 == 0);
                int columnX = isLeftColumn ? leftColumnX : rightColumnX;
                int rowY = currentY + (filterIndex / 2) * maxItemHeight;

                if (icon != null)
                {
                    filterControl.IconX = columnX;
                    filterControl.IconY = rowY;
                }

                var label = new XNALabel(WindowManager)
                {
                    Name = $"lbl{checkbox.Name}Filter",
                    Text = checkbox.Text ?? checkbox.Name,
                    ClientRectangle = new Rectangle(
                    columnX + iconWidth + (iconWidth > 0 ? iconLabelSpacing : 0), rowY,
                    0, UIDesignConstants.BUTTON_HEIGHT)
                };

                int topRowHeight = Math.Max(iconHeight, minLabelRowHeight);

                var dropdown = new XNAClientDropDown(WindowManager)
                {
                    Name = $"dd{checkbox.Name}Filter",
                    ClientRectangle = new Rectangle(columnX, rowY + topRowHeight + itemVerticalSpacing, dropdownWidth, UIDesignConstants.BUTTON_HEIGHT)
                };
                dropdown.AddItem("All".L10N("Client:Main:FilterAll"));
                dropdown.AddItem("On".L10N("Client:Main:FilterOn"));
                dropdown.AddItem("Off".L10N("Client:Main:FilterOff"));
                dropdown.SelectedIndex = 0;
                dropdown.SelectedIndexChanged += FilterDropDown_SelectedIndexChanged;

                filterControl.DropDown = dropdown;
                filterControl.Label = label;

                int itemHeight = topRowHeight + itemVerticalSpacing + UIDesignConstants.BUTTON_HEIGHT + GAP;
                if (itemHeight > maxItemHeight)
                    maxItemHeight = itemHeight;

                gameOptionFilterControls.Add(filterControl);
                AddChild(dropdown);
                AddChild(label);

                filterIndex++;
            }

            // Create filters for broadcastable dropdowns
            var broadcastableDropdowns = gameLobby.DropDowns.Where(dd => dd.BroadcastToLobby && dd.IconShownInFilters).ToList();
            foreach (var lobbyDropdown in broadcastableDropdowns)
            {
                var filterControl = new GameOptionFilterControl
                {
                    OptionName = lobbyDropdown.Name,
                    IsCheckbox = false,
                    Icon = lobbyDropdown.Icon
                };

                Texture2D icon = null;
                if (!string.IsNullOrEmpty(lobbyDropdown.Icon))
                    icon = AssetLoader.LoadTexture(lobbyDropdown.Icon);

                int iconWidth = icon?.Width ?? 0;
                int iconHeight = icon?.Height ?? 0;

                bool isLeftColumn = (filterIndex % 2 == 0);
                int columnX = isLeftColumn ? leftColumnX : rightColumnX;
                int rowY = currentY + (filterIndex / 2) * maxItemHeight;

                if (icon != null)
                {
                    filterControl.IconX = columnX;
                    filterControl.IconY = rowY;
                }

                var label = new XNALabel(WindowManager)
                {
                    Name = $"lbl{lobbyDropdown.Name}Filter",
                    Text = lobbyDropdown.OptionName ?? lobbyDropdown.Name,
                    ClientRectangle = new Rectangle(
                    columnX + iconWidth + (iconWidth > 0 ? iconLabelSpacing : 0), rowY,
                    0, UIDesignConstants.BUTTON_HEIGHT)
                };

                int topRowHeight = Math.Max(iconHeight, minLabelRowHeight);

                var dropdown = new XNAClientDropDown(WindowManager)
                {
                    Name = $"dd{lobbyDropdown.Name}Filter",
                    ClientRectangle = new Rectangle(columnX, rowY + topRowHeight + itemVerticalSpacing, dropdownWidth, UIDesignConstants.BUTTON_HEIGHT)
                };
                dropdown.AddItem("All".L10N("Client:Main:FilterAll"));

                foreach (var item in lobbyDropdown.Items)
                    dropdown.AddItem(new XNADropDownItem { Text = item.Text, Tag = item.Tag });

                dropdown.SelectedIndex = 0;
                dropdown.SelectedIndexChanged += FilterDropDown_SelectedIndexChanged;

                filterControl.DropDown = dropdown;
                filterControl.Label = label;

                int itemHeight = topRowHeight + itemVerticalSpacing + UIDesignConstants.BUTTON_HEIGHT + GAP;
                if (itemHeight > maxItemHeight)
                    maxItemHeight = itemHeight;

                gameOptionFilterControls.Add(filterControl);
                AddChild(dropdown);
                AddChild(label);

                filterIndex++;
            }

            if (gameOptionFilterControls.Count > 0)
            {
                int numRows = (filterIndex + 1) / 2;
                currentY += numRows * maxItemHeight;

                var secondDivider = CreateDivider(currentY);
                AddChild(secondDivider);

                currentY += secondDivider.Height + GAP;

                var btnResetDefaults = Children.FirstOrDefault(c => c.Name == "btnResetDefaults") as XNAClientButton;
                if (btnResetDefaults != null)
                {
                    btnResetDefaults.ClientRectangle = new Rectangle(
                        GAP, currentY,
                        UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT
                    );
                }
            }

            UpdateFilterIcons();
        }

        private void FilterDropDown_SelectedIndexChanged(object sender, EventArgs e) => UpdateFilterIcons();

        private void UpdateFilterIcons()
        {
            foreach (var filterControl in gameOptionFilterControls)
            {
                if (filterControl.IconX == 0 && filterControl.IconY == 0)
                    continue;

                string iconToShow = null;

                if (filterControl.IsCheckbox)
                {
                    // For checkboxes: 0 = All, 1 = On, 2 = Off
                    int selectedIndex = filterControl.DropDown.SelectedIndex;
                    if (selectedIndex == 0 || selectedIndex == 1) // All or On
                        iconToShow = filterControl.EnabledIcon;
                    else if (selectedIndex == 2) // Off
                        iconToShow = filterControl.DisabledIcon;
                }
                else
                {
                    // For dropdowns, always show the icon
                    iconToShow = filterControl.Icon;
                }

                filterControl.CurrentIcon = !string.IsNullOrEmpty(iconToShow) ? AssetLoader.LoadTexture(iconToShow) : null;
            }
        }

        private void BtnSave_LeftClick(object sender, EventArgs e)
        {
            Save();
            Disable();
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Cancel();
        }

        private void BtnResetDefaults_LeftClick(object sender, EventArgs e)
        {
            ResetDefaults();
        }

        private void Save()
        {
            var userIniSettings = UserINISettings.Instance;
            userIniSettings.ShowFriendGamesOnly.Value = chkBoxFriendsOnly.Checked;
            userIniSettings.HideLockedGames.Value = chkBoxHideLockedGames.Checked;
            userIniSettings.HidePasswordedGames.Value = chkBoxHidePasswordedGames.Checked;
            userIniSettings.HideIncompatibleGames.Value = chkBoxHideIncompatibleGames.Checked;
            userIniSettings.MaxPlayerCount.Value = int.Parse(ddMaxPlayerCount.SelectedItem.Text);

            // Save game option filters (only non-default values)
            var gameOptionFiltersSection = userIniSettings.SettingsIni.GetSection(UserINISettings.GAME_OPTION_FILTERS);
            gameOptionFiltersSection?.RemoveAllKeys();

            foreach (var filterControl in gameOptionFilterControls)
            {
                if (filterControl.IsCheckbox)
                {
                    // UI: 0 = All, 1 = On, 2 = Off
                    // Storage: null = All, true = On, false = Off
                    bool? filterValue = filterControl.DropDown.SelectedIndex switch
                    {
                        0 => null,   // All
                        1 => true,   // On
                        2 => false,  // Off
                        _ => null
                    };
                    if (filterValue != null) // Only save if not "All"
                        userIniSettings.SetCheckboxFilterValue(filterControl.OptionName, filterValue);
                }
                else
                {
                    // UI: 0 = All, 1+ = game option indices
                    // Storage: null = All, otherwise actual index
                    int? filterValue = filterControl.DropDown.SelectedIndex == 0 ? null : filterControl.DropDown.SelectedIndex - 1;
                    if (filterValue != null) // if not "All"
                        userIniSettings.SetDropdownFilterValue(filterControl.OptionName, filterValue);
                }
            }

            UserINISettings.Instance.SaveSettings();
        }

        private void Load()
        {
            var userIniSettings = UserINISettings.Instance;
            chkBoxFriendsOnly.Checked = userIniSettings.ShowFriendGamesOnly.Value;
            chkBoxHideLockedGames.Checked = userIniSettings.HideLockedGames.Value;
            chkBoxHidePasswordedGames.Checked = userIniSettings.HidePasswordedGames.Value;
            chkBoxHideIncompatibleGames.Checked = userIniSettings.HideIncompatibleGames.Value;
            ddMaxPlayerCount.SelectedIndex = ddMaxPlayerCount.Items.FindIndex(i => i.Text == userIniSettings.MaxPlayerCount.Value.ToString());

            foreach (var filterControl in gameOptionFilterControls)
            {
                if (filterControl.IsCheckbox)
                {
                    // Storage: null = All, true = On, false = Off
                    // UI: 0 = All, 1 = On, 2 = Off
                    bool? filterValue = userIniSettings.GetCheckboxFilterValue(filterControl.OptionName);
                    filterControl.DropDown.SelectedIndex = filterValue switch
                    {
                        null => 0,   // All
                        true => 1,   // On
                        false => 2   // Off
                    };
                }
                else
                {
                    // Storage: null = All, otherwise actual index
                    // UI: 0 = All, 1+ = game option indices
                    int? filterValue = userIniSettings.GetDropdownFilterValue(filterControl.OptionName);
                    filterControl.DropDown.SelectedIndex = filterValue == null ? 0 : filterValue.Value + 1;
                }
            }

            UpdateFilterIcons();
        }

        private void ResetDefaults()
        {
            UserINISettings.Instance.ResetGameFilters();
            Load();
        }

        public void Show()
        {
            if (!gameOptionFiltersCreated)
            {
                CreateGameOptionFilters();

                var btnResetDefaults = Children.FirstOrDefault(c => c.Name == "btnResetDefaults") as XNAClientButton;
                var btnSave = Children.FirstOrDefault(c => c.Name == "btnSave") as XNAClientButton;
                var btnCancel = Children.FirstOrDefault(c => c.Name == "btnCancel") as XNAClientButton;

                if (btnResetDefaults != null && btnSave != null && btnCancel != null)
                {
                    btnSave.ClientRectangle = new Rectangle(
                        GAP, btnResetDefaults.Y + UIDesignConstants.BUTTON_HEIGHT + GAP,
                        UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT
                    );

                    btnCancel.ClientRectangle = new Rectangle(
                        Width - GAP - UIDesignConstants.BUTTON_WIDTH_92, btnSave.Y,
                        UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT
                    );
                }

                gameOptionFiltersCreated = true;
            }

            Load();
            Enable();
        }

        public void Cancel()
        {
            Disable();
        }

        private XNAPanel CreateDivider(int y, int height = 1)
        {
            var dividerPanel = new XNAPanel(WindowManager)
            {
                DrawBorders = true,
                ClientRectangle = new Rectangle(0, y, Width, height)
            };
            return dividerPanel;
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            foreach (var filterControl in gameOptionFilterControls)
            {
                if (filterControl.CurrentIcon != null)
                {
                    DrawTexture(filterControl.CurrentIcon,
                        new Rectangle(filterControl.IconX, filterControl.IconY,
                            filterControl.CurrentIcon.Width, filterControl.CurrentIcon.Height),
                        Color.White);
                }
            }
        }
    }
}
