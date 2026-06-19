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
    /// <summary>
    /// Custom scroll panel that exposes ContentPanel for adding children
    /// </summary>
    internal class GameFiltersScrollPanel : XNAScrollPanel
    {
        public GameFiltersScrollPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        public XNAPanel GetContentPanel() => ContentPanel;
    }

    public class GameFiltersPanel : XNAPanel
    {
        private const int minPlayerCount = 2;
        private const int maxPlayerCount = 8;
        private const int GAP = 12;
        private const int BOTTOM_PANEL_HEIGHT = 60;

        private GameFiltersScrollPanel scrollPanel;
        private XNAPanel bottomPanel;

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
            public XNAPanel IconPanel { get; set; }
            public string EnabledIcon { get; set; }
            public string DisabledIcon { get; set; }
        }

        public GameFiltersPanel(WindowManager windowManager, GameLobbyBase gameLobby) : base(windowManager)
        {
            this.gameLobby = gameLobby;
        }

        private static int GetVerticalScrollbarReserveWidth()
        {
            return AssetLoader.AssetExists("sbUpArrow.png")
                ? AssetLoader.LoadTexture("sbUpArrow.png").Width
                : 0;
        }

        public override void Initialize()
        {
            base.Initialize();

            Name = "GameFiltersWindow";
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0), Width, Height);

            // Create scroll panel for filters content
            scrollPanel = new GameFiltersScrollPanel(WindowManager);
            scrollPanel.Name = "FiltersScrollPanel";
            scrollPanel.ClientRectangle = new Rectangle(0, 0, Width, Height - BOTTOM_PANEL_HEIGHT);
            scrollPanel.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 200), 1, 1);
            scrollPanel.DrawBorders = false;

            // Create bottom panel for Save/Cancel buttons
            bottomPanel = new XNAPanel(WindowManager);
            bottomPanel.Name = "BottomButtonPanel";
            bottomPanel.ClientRectangle = new Rectangle(0, Height - BOTTOM_PANEL_HEIGHT, Width, BOTTOM_PANEL_HEIGHT);
            bottomPanel.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 255), 1, 1);
            bottomPanel.DrawBorders = false;

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
                GAP, (BOTTOM_PANEL_HEIGHT - UIDesignConstants.BUTTON_HEIGHT) / 2,
                UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT
            );
            btnSave.LeftClick += BtnSave_LeftClick;

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = nameof(btnCancel);
            btnCancel.Text = "Cancel".L10N("Client:Main:ButtonCancel");
            btnCancel.ClientRectangle = new Rectangle(
                Width - GAP - UIDesignConstants.BUTTON_WIDTH_92, (BOTTOM_PANEL_HEIGHT - UIDesignConstants.BUTTON_HEIGHT) / 2,
                UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT
            );
            btnCancel.LeftClick += BtnCancel_LeftClick;

            scrollPanel.GetContentPanel().AddChild(lblTitle);
            scrollPanel.GetContentPanel().AddChild(chkBoxFriendsOnly);
            scrollPanel.GetContentPanel().AddChild(chkBoxHideLockedGames);
            scrollPanel.GetContentPanel().AddChild(chkBoxHidePasswordedGames);
            scrollPanel.GetContentPanel().AddChild(chkBoxHideIncompatibleGames);
            scrollPanel.GetContentPanel().AddChild(lblMaxPlayerCount);
            scrollPanel.GetContentPanel().AddChild(ddMaxPlayerCount);
            scrollPanel.GetContentPanel().AddChild(btnResetDefaults);

            bottomPanel.AddChild(btnSave);
            bottomPanel.AddChild(btnCancel);

            AddChild(scrollPanel);
            AddChild(bottomPanel);
        }

        private void CreateGameOptionFilters()
        {
            // Note: broadcasted checkboxes are converted to dropdowns so we can have a third - undefined - value.

            if (gameLobby == null)
                return;

            var broadcastableCheckboxes = gameLobby.CheckBoxes.Where(cb => cb.BroadcastToLobby && cb.ShowInFilters).ToList();
            var broadcastableDropdowns = gameLobby.DropDowns.Where(dd => dd.BroadcastToLobby && dd.ShowInFilters).ToList();

            if (broadcastableCheckboxes.Count == 0 && broadcastableDropdowns.Count == 0)
                return;

            int currentY = ddMaxPlayerCount.Y + UIDesignConstants.BUTTON_HEIGHT + GAP;
            const int iconLabelSpacing = 6;
            const int itemVerticalSpacing = 4;
            const int minLabelRowHeight = 18;
            int verticalScrollbarReserveWidth = GetVerticalScrollbarReserveWidth();
            int contentWidth = Math.Max(scrollPanel.Width - verticalScrollbarReserveWidth, scrollPanel.Width / 2);

            int dropdownWidth = (contentWidth - (GAP * 3)) / 2;

            var divider = CreateDivider(currentY, contentWidth);
            scrollPanel.GetContentPanel().AddChild(divider);
            currentY += divider.Height + GAP;

            int leftColumnX = GAP;
            int rightColumnX = leftColumnX + dropdownWidth + GAP;
            int filterIndex = 0;
            int maxItemHeight = 0;

            // Create filters for broadcastable checkboxes
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

                XNAPanel iconPanel = null;
                if (icon != null)
                {
                    iconPanel = new XNAPanel(WindowManager)
                    {
                        Name = $"icon{checkbox.Name}Filter",
                        ClientRectangle = new Rectangle(columnX, rowY, iconWidth, iconHeight),
                        DrawBorders = false,
                        BackgroundTexture = icon
                    };
                    filterControl.IconPanel = iconPanel;
                }

                var label = new XNALabel(WindowManager)
                {
                    Name = $"lbl{checkbox.Name}Filter",
                    Text = checkbox.Text + ":",
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

                // "All" item has no icon
                dropdown.AddItem(new XNADropDownItem { Text = "All".L10N("Client:Main:FilterAllGames"), Texture = null });

                Texture2D enabledIconTexture = null;
                if (!string.IsNullOrEmpty(checkbox.EnabledIcon))
                    enabledIconTexture = AssetLoader.LoadTexture(checkbox.EnabledIcon);
                dropdown.AddItem(new XNADropDownItem { Text = "On".L10N("Client:Main:FilterOn"), Texture = enabledIconTexture });

                Texture2D disabledIconTexture = null;
                if (!string.IsNullOrEmpty(checkbox.DisabledIcon))
                    disabledIconTexture = AssetLoader.LoadTexture(checkbox.DisabledIcon);
                dropdown.AddItem(new XNADropDownItem { Text = "Off".L10N("Client:Main:FilterOff"), Texture = disabledIconTexture });

                dropdown.SelectedIndex = 0;

                filterControl.DropDown = dropdown;
                filterControl.Label = label;

                int itemHeight = topRowHeight + itemVerticalSpacing + UIDesignConstants.BUTTON_HEIGHT + GAP;
                if (itemHeight > maxItemHeight)
                    maxItemHeight = itemHeight;

                gameOptionFilterControls.Add(filterControl);
                if (iconPanel != null)
                    scrollPanel.GetContentPanel().AddChild(iconPanel);
                scrollPanel.GetContentPanel().AddChild(dropdown);
                scrollPanel.GetContentPanel().AddChild(label);

                filterIndex++;
            }

            // Create filters for broadcastable dropdowns
            foreach (var lobbyDropdown in broadcastableDropdowns)
            {
                var filterControl = new GameOptionFilterControl
                {
                    OptionName = lobbyDropdown.Name,
                    IsCheckbox = false
                };

                // For dropdowns with multiple icons, show the first one initially
                Texture2D icon = lobbyDropdown.Items.Count > 0 ? lobbyDropdown.Items[0].Texture : null;

                int iconWidth = icon?.Width ?? 0;
                int iconHeight = icon?.Height ?? 0;

                bool isLeftColumn = (filterIndex % 2 == 0);
                int columnX = isLeftColumn ? leftColumnX : rightColumnX;
                int rowY = currentY + (filterIndex / 2) * maxItemHeight;

                XNAPanel iconPanel = null;
                if (icon != null)
                {
                    iconPanel = new XNAPanel(WindowManager)
                    {
                        Name = $"icon{lobbyDropdown.Name}Filter",
                        ClientRectangle = new Rectangle(columnX, rowY, iconWidth, iconHeight),
                        DrawBorders = false,
                        BackgroundTexture = icon
                    };
                    filterControl.IconPanel = iconPanel;
                }

                var label = new XNALabel(WindowManager)
                {
                    Name = $"lbl{lobbyDropdown.Name}Filter",
                    Text = lobbyDropdown.OptionName,
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

                dropdown.AddItem(new XNADropDownItem { Text = "All".L10N("Client:Main:FilterAllGames"), Texture = null });

                for (int i = 0; i < lobbyDropdown.Items.Count; i++)
                {
                    var item = lobbyDropdown.Items[i];
                    dropdown.AddItem(new XNADropDownItem { Text = item.Text, Tag = item.Tag, Texture = item.Texture });
                }

                dropdown.SelectedIndex = 0;

                filterControl.DropDown = dropdown;
                filterControl.Label = label;

                int itemHeight = topRowHeight + itemVerticalSpacing + UIDesignConstants.BUTTON_HEIGHT + GAP;
                if (itemHeight > maxItemHeight)
                    maxItemHeight = itemHeight;

                gameOptionFilterControls.Add(filterControl);
                if (iconPanel != null)
                    scrollPanel.GetContentPanel().AddChild(iconPanel);
                scrollPanel.GetContentPanel().AddChild(dropdown);
                scrollPanel.GetContentPanel().AddChild(label);

                filterIndex++;
            }

            if (gameOptionFilterControls.Count > 0)
            {
                int numRows = (filterIndex + 1) / 2;
                currentY += numRows * maxItemHeight;

                var secondDivider = CreateDivider(currentY, contentWidth);
                scrollPanel.GetContentPanel().AddChild(secondDivider);

                currentY += secondDivider.Height + GAP;

                var btnResetDefaults = scrollPanel.GetContentPanel().Children.FirstOrDefault(c => c.Name == "btnResetDefaults") as XNAClientButton;
                if (btnResetDefaults != null)
                {
                    btnResetDefaults.ClientRectangle = new Rectangle(
                        GAP, currentY,
                        UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT
                    );
                }
                UpdateScrollContentHeight();
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
                    // Storage: null = All, 1 = On, 0 = Off
                    int? filterValue = filterControl.DropDown.SelectedIndex switch
                    {
                        0 => null,   // All
                        1 => 1,      // On
                        2 => 0,      // Off
                        _ => null
                    };
                    if (filterValue != null) // Only save if not "All"
                        userIniSettings.SetGameOptionFilterValue(filterControl.OptionName, filterValue);
                }
                else
                {
                    // UI: 0 = All, 1+ = game option indices
                    // Storage: null = All, otherwise actual index
                    int? filterValue = filterControl.DropDown.SelectedIndex == 0 ? null : filterControl.DropDown.SelectedIndex - 1;
                    if (filterValue != null) // if not "All"
                        userIniSettings.SetGameOptionFilterValue(filterControl.OptionName, filterValue);
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
                int? filterValue = userIniSettings.GetGameOptionFilterValue(filterControl.OptionName);

                if (filterControl.IsCheckbox)
                {
                    // Storage: null = All, 1 = On, 0 = Off
                    // UI: 0 = All, 1 = On, 2 = Off
                    filterControl.DropDown.SelectedIndex = filterValue switch
                    {
                        null => 0,   // All
                        1 => 1,      // On
                        0 => 2,      // Off
                        _ => 0
                    };
                }
                else
                {
                    // Storage: null = All, otherwise actual index
                    // UI: 0 = All, 1+ = game option indices
                    filterControl.DropDown.SelectedIndex = filterValue == null ? 0 : filterValue.Value + 1;
                }
            }
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
                gameOptionFiltersCreated = true;
            }

            Load();
            Enable();
        }

        public void Cancel()
        {
            Disable();
        }
        private void UpdateScrollContentHeight()
        {
            var content = scrollPanel.GetContentPanel();
            int bottom = content.Children.Max(c => c.Bottom);
            content.Height = bottom + GAP;
        }

        private XNAPanel CreateDivider(int y, int width, int height = 1)
        {
            var dividerPanel = new XNAPanel(WindowManager)
            {
                DrawBorders = true,
                ClientRectangle = new Rectangle(0, y, width, height)
            };
            return dividerPanel;
        }
    }
}
