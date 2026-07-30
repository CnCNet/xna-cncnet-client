#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;

using ClientCore;
using ClientCore.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    /// <summary>
    /// A window for configuring in-game hotkeys.
    /// </summary>
    public class HotkeyConfigurationWindow : XNAWindow
    {
        private readonly string HOTKEY_TIP_TEXT = "Press a key...".L10N("Client:DTAConfig:PressAKey");
        private const string KEYBOARD_COMMANDS_INI = "KeyboardCommands.ini";

        public HotkeyConfigurationWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        /// <summary>
        /// Keys that the client doesn't allow to be used regular hotkeys.
        /// </summary>
        private readonly Keys[] keyBlacklist = new Keys[]
        {
            Keys.LeftAlt,
            Keys.RightAlt,
            Keys.LeftControl,
            Keys.RightControl,
            Keys.LeftShift,
            Keys.RightShift
        };

        private readonly List<GameCommand> gameCommands = new List<GameCommand>();

        private XNAClientDropDown ddCategory = null!;
        private XNAMultiColumnListBox lbHotkeys = null!;

        private XNAPanel hotkeyInfoPanel = null!;
        private XNALabel lblCommandCaption = null!;
        private XNALabel lblDescription = null!;
        private XNALabel lblCurrentHotkeyValue = null!;
        private XNALabel lblNewHotkeyValue = null!;
        private XNALabel lblCurrentlyAssignedTo = null!;

        private XNALabel lblDefaultHotkeyValue = null!;
        private XNAClientButton btnResetKey = null!;

        private Hotkey pendingHotkey = Hotkey.None;
        private KeyModifiers lastFrameModifiers;

        public override void Initialize()
        {
            ReadGameCommands();

            Name = "HotkeyConfigurationWindow";
            ClientRectangle = new Rectangle(0, 0, 600, 450);
            BackgroundTexture = AssetLoader.LoadTextureUncached("hotkeyconfigbg.png");

            var lblCategory = new XNALabel(WindowManager);
            lblCategory.Name = "lblCategory";
            lblCategory.ClientRectangle = new Rectangle(12, 12, 0, 0);
            lblCategory.Text = "Category:".L10N("Client:DTAConfig:Category");

            ddCategory = new XNAClientDropDown(WindowManager);
            ddCategory.Name = "ddCategory";
            ddCategory.ClientRectangle = new Rectangle(lblCategory.Right + 12,
                lblCategory.Y - 1, 250, ddCategory.Height);

            HashSet<string> categories = new HashSet<string>();

            foreach (var command in gameCommands)
            {
                if (!categories.Contains(command.Category))
                    categories.Add(command.Category);
            }

            foreach (string category in categories)
                ddCategory.AddItem(category);

            lbHotkeys = new XNAMultiColumnListBox(WindowManager);
            lbHotkeys.Name = "lbHotkeys";
            lbHotkeys.ClientRectangle = new Rectangle(12, ddCategory.Bottom + 12,
                ddCategory.Right - 12, Height - ddCategory.Bottom - 59);
            lbHotkeys.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbHotkeys.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbHotkeys.AddColumn("Command".L10N("Client:DTAConfig:Command"), 150);
            lbHotkeys.AddColumn("Shortcut".L10N("Client:DTAConfig:Shortcut"), lbHotkeys.Width - 150);

            hotkeyInfoPanel = new XNAPanel(WindowManager);
            hotkeyInfoPanel.Name = "HotkeyInfoPanel";
            hotkeyInfoPanel.ClientRectangle = new Rectangle(lbHotkeys.Right + 12,
                ddCategory.Y, Width - lbHotkeys.Right - 24, lbHotkeys.Height + ddCategory.Height + 12);

            lblCommandCaption = new XNALabel(WindowManager);
            lblCommandCaption.Name = "lblCommandCaption";
            lblCommandCaption.FontIndex = 1;
            lblCommandCaption.ClientRectangle = new Rectangle(12, 12, 0, 0);
            lblCommandCaption.Text = "Command name".L10N("Client:DTAConfig:CommandName");

            lblDescription = new XNALabel(WindowManager);
            lblDescription.Name = "lblDescription";
            lblDescription.ClientRectangle = new Rectangle(12, lblCommandCaption.Bottom + 12, 0, 0);
            lblDescription.Text = "Command description".L10N("Client:DTAConfig:CommandDescription");

            var lblCurrentHotkey = new XNALabel(WindowManager);
            lblCurrentHotkey.Name = "lblCurrentHotkey";
            lblCurrentHotkey.ClientRectangle = new Rectangle(lblDescription.X,
                lblDescription.Bottom + 48, 0, 0);
            lblCurrentHotkey.FontIndex = 1;
            lblCurrentHotkey.Text = "Currently assigned hotkey:".L10N("Client:DTAConfig:CurrentHotKey");

            lblCurrentHotkeyValue = new XNALabel(WindowManager);
            lblCurrentHotkeyValue.Name = "lblCurrentHotkeyValue";
            lblCurrentHotkeyValue.ClientRectangle = new Rectangle(lblDescription.X,
                lblCurrentHotkey.Bottom + 6, 0, 0);
            lblCurrentHotkeyValue.Text = "Current hotkey value".L10N("Client:DTAConfig:CurrentHotKeyValue");

            var lblNewHotkey = new XNALabel(WindowManager);
            lblNewHotkey.Name = "lblNewHotkey";
            lblNewHotkey.ClientRectangle = new Rectangle(lblDescription.X,
                lblCurrentHotkeyValue.Bottom + 48, 0, 0);
            lblNewHotkey.FontIndex = 1;
            lblNewHotkey.Text = "New hotkey:".L10N("Client:DTAConfig:NewHotKey");

            lblNewHotkeyValue = new XNALabel(WindowManager);
            lblNewHotkeyValue.Name = "lblNewHotkeyValue";
            lblNewHotkeyValue.ClientRectangle = new Rectangle(lblDescription.X,
                lblNewHotkey.Bottom + 6, 0, 0);
            lblNewHotkeyValue.Text = HOTKEY_TIP_TEXT;

            lblCurrentlyAssignedTo = new XNALabel(WindowManager);
            lblCurrentlyAssignedTo.Name = "lblCurrentlyAssignedTo";
            lblCurrentlyAssignedTo.ClientRectangle = new Rectangle(lblDescription.X,
                lblNewHotkeyValue.Bottom + 12, 0, 0);
            lblCurrentlyAssignedTo.Text = "Currently assigned to:".L10N("Client:DTAConfig:CurrentHotKeyAssign") + "\nKey";

            var btnAssign = new XNAClientButton(WindowManager);
            btnAssign.Name = "btnAssign";
            btnAssign.ClientRectangle = new Rectangle(lblDescription.X,
                lblCurrentlyAssignedTo.Bottom + 24, UIDesignConstants.BUTTON_WIDTH_121, UIDesignConstants.BUTTON_HEIGHT);
            btnAssign.Text = "Assign Hotkey".L10N("Client:DTAConfig:AssignHotkey");
            btnAssign.LeftClick += BtnAssign_LeftClick;

            btnResetKey = new XNAClientButton(WindowManager);
            btnResetKey.Name = "btnResetKey";
            btnResetKey.ClientRectangle = new Rectangle(btnAssign.X, btnAssign.Bottom + 12, btnAssign.Width, 23);
            btnResetKey.Text = "Reset to Default".L10N("Client:DTAConfig:ResetToDefault");
            btnResetKey.LeftClick += BtnReset_LeftClick;

            var lblDefaultHotkey = new XNALabel(WindowManager);
            lblDefaultHotkey.Name = "lblOriginalHotkey";
            lblDefaultHotkey.ClientRectangle = new Rectangle(lblCurrentHotkey.X, btnResetKey.Bottom + 12, 0, 0);
            lblDefaultHotkey.Text = "Default hotkey:".L10N("Client:DTAConfig:DefaultHotKey");

            lblDefaultHotkeyValue = new XNALabel(WindowManager);
            lblDefaultHotkeyValue.Name = "lblDefaultHotkeyValue";
            lblDefaultHotkeyValue.ClientRectangle = new Rectangle(lblDefaultHotkey.Right + 12, lblDefaultHotkey.Y, 0, 0);

            var btnSave = new XNAClientButton(WindowManager);
            btnSave.Name = "btnSave";
            btnSave.ClientRectangle = new Rectangle(12, lbHotkeys.Bottom + 12, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            btnSave.Text = "Save".L10N("Client:DTAConfig:ButtonSave");
            btnSave.LeftClick += BtnSave_LeftClick;

            var btnResetAllKeys = new XNAClientButton(WindowManager);
            btnResetAllKeys.Name = "btnResetAllToDefaults";
            btnResetAllKeys.ClientRectangle = new Rectangle(0, btnSave.Y, UIDesignConstants.BUTTON_WIDTH_121, UIDesignConstants.BUTTON_HEIGHT);
            btnResetAllKeys.Text = "Reset All Keys".L10N("Client:DTAConfig:ResetAllHotkey");
            btnResetAllKeys.LeftClick += BtnResetToDefaults_LeftClick;
            AddChild(btnResetAllKeys);
            btnResetAllKeys.CenterOnParentHorizontally();

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = "btnExit";
            btnCancel.ClientRectangle = new Rectangle(Width - 104, btnSave.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT);
            btnCancel.Text = "Cancel".L10N("Client:DTAConfig:ButtonCancel");
            btnCancel.LeftClick += BtnCancel_LeftClick;

            AddChild(lbHotkeys);
            AddChild(lblCategory);
            AddChild(ddCategory);
            AddChild(hotkeyInfoPanel);
            AddChild(btnSave);
            AddChild(btnCancel);
            hotkeyInfoPanel.AddChild(lblCommandCaption);
            hotkeyInfoPanel.AddChild(lblDescription);
            hotkeyInfoPanel.AddChild(lblCurrentHotkey);
            hotkeyInfoPanel.AddChild(lblCurrentHotkeyValue);
            hotkeyInfoPanel.AddChild(lblNewHotkey);
            hotkeyInfoPanel.AddChild(lblNewHotkeyValue);
            hotkeyInfoPanel.AddChild(lblCurrentlyAssignedTo);
            hotkeyInfoPanel.AddChild(lblDefaultHotkey);
            hotkeyInfoPanel.AddChild(lblDefaultHotkeyValue);
            hotkeyInfoPanel.AddChild(btnAssign);
            hotkeyInfoPanel.AddChild(btnResetKey);

            if (categories.Count > 0)
            {
                hotkeyInfoPanel.Disable();
                lbHotkeys.SelectedIndexChanged += LbHotkeys_SelectedIndexChanged;

                ddCategory.SelectedIndexChanged += DdCategory_SelectedIndexChanged;
                ddCategory.SelectedIndex = 0;
            }
            else
                Logger.Log("No keyboard game commands exist!");

            GameProcessLogic.GameProcessExited += GameProcessLogic_GameProcessExited;

            base.Initialize();

            CenterOnParent();

            Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;
            EnabledChanged += HotkeyConfigurationWindow_EnabledChanged;

            // Load and apply the hotkeys so that if the default keyboard INI file is updated during a client update
            LoadKeyboardINI();
            RefreshHotkeyList();
            WriteKeyboardINI(writeEvenIfSettingsIniAsKeyboardIniHolds: true);
        }

        /// <summary>
        /// Reads game commands from an INI file.
        /// </summary>
        private void ReadGameCommands()
        {
            var gameCommandsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), KEYBOARD_COMMANDS_INI));

            List<string> sections = gameCommandsIni.GetSections();

            HashSet<Hotkey> defaultHotkeys = [];

            foreach (string sectionName in sections)
            {
                var gameCommand = new GameCommand(gameCommandsIni.GetSection(sectionName));
                gameCommands.Add(gameCommand);

                // Check duplicates for default hotkeys
                if (gameCommand.DefaultHotkey != null && gameCommand.DefaultHotkey != Hotkey.None)
                {
                    bool isDuplicate = !defaultHotkeys.Add(gameCommand.DefaultHotkey);

                    if (isDuplicate)
                        throw new Exception("The default hotkey " + gameCommand.DefaultHotkey.ToString() + " for command " + gameCommand.UIName + " is duplicated with another command's default hotkey. Please make sure all default hotkeys in " + KEYBOARD_COMMANDS_INI + " are unique.");
                }
            }
        }

        /// <summary>
        /// Resets the hotkey for the currently selected game command to its
        /// default value.
        /// </summary>
        private void BtnReset_LeftClick(object? sender, EventArgs e)
        {
            if (lbHotkeys.SelectedIndex < 0 || lbHotkeys.SelectedIndex >= lbHotkeys.ItemCount)
            {
                return;
            }

            var command = (GameCommand)lbHotkeys.GetItem(0, lbHotkeys.SelectedIndex).Tag;

            if (command.DefaultHotkey == null)
            {
                command.Hotkey = null;
            }
            else
            {
                command.Hotkey = command.DefaultHotkey;

                // If the hotkey is already assigned to some other command, unbind it
                foreach (var gameCommand in gameCommands)
                {
                    if (gameCommand != command && gameCommand.Hotkey == command.Hotkey)
                        gameCommand.Hotkey = null;
                }
            }

            pendingHotkey = Hotkey.None;
            RefreshHotkeyList();
        }

        private void BtnResetToDefaults_LeftClick(object? sender, EventArgs e)
        {
            foreach (var command in gameCommands)
            {
                if (command.DefaultHotkey == null)
                    command.Hotkey = null;
                else
                    command.Hotkey = command.DefaultHotkey;
            }

            RefreshHotkeyList();
        }

        private void HotkeyConfigurationWindow_EnabledChanged(object? sender, EventArgs e)
        {
            if (Enabled)
            {
                LoadKeyboardINI();
                RefreshHotkeyList();
            }
        }

        /// <summary>
        /// Reloads Keyboard.ini when the game process has exited.
        /// </summary>
        private void GameProcessLogic_GameProcessExited()
        {
            WindowManager.AddCallback(new Action(LoadKeyboardINI), null);
        }

        private void LoadKeyboardINI()
        {
            var keyboardINI = ClientConfiguration.Instance.SettingsIniAsKeyboardIni
                ? UserINISettings.Instance.SettingsIni
                : new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.KeyboardINI));
            var hotkeySection = keyboardINI.GetOrAddSection(ClientConfiguration.Instance.KeyboardHotkeySection);

            // Load the hotkeys from the INI file
            var assignedHotkeys = new HashSet<Hotkey>();
            foreach (var command in gameCommands)
            {
                int? tsHotkey = hotkeySection.GetIntValueOrNull(command.ININame);

                if (tsHotkey.HasValue)
                {
                    Hotkey hotkey = new(tsHotkey.Value);
                    bool isDuplicate = false;
                    if (hotkey != Hotkey.None)
                        isDuplicate = !assignedHotkeys.Add(hotkey);

                    if (!isDuplicate)
                        command.Hotkey = hotkey;
                    else
                        command.Hotkey = null;
                }
                else
                {
                    // Clear any previously assigned hotkey when no value exists in the INI
                    command.Hotkey = null;
                }
            }

            // Assign default hotkeys
            foreach (var command in gameCommands)
            {
                bool hotkeyAssigned = hotkeySection.KeyExists(command.ININame);

                if (!hotkeyAssigned && command.DefaultHotkey != null)
                {
                    // Try assigning the default hotkey if it exists and is not occupied by other commands
                    bool occupied = false;
                    if (command.DefaultHotkey != Hotkey.None)
                    {
                        foreach (var otherCommand in gameCommands)
                        {
                            if (otherCommand != command && command.DefaultHotkey == otherCommand.Hotkey)
                            {
                                occupied = true;
                                break;
                            }
                        }
                    }

                    if (!occupied)
                        command.Hotkey = command.DefaultHotkey;
                }
            }
        }

        private void LbHotkeys_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lbHotkeys.SelectedIndex < 0 || lbHotkeys.SelectedIndex >= lbHotkeys.ItemCount)
            {
                hotkeyInfoPanel.Disable();
                return;
            }

            hotkeyInfoPanel.Enable();
            var command = (GameCommand)lbHotkeys.GetItem(0, lbHotkeys.SelectedIndex).Tag;
            lblCommandCaption.Text = command.UIName;
            lblDescription.Text = Renderer.FixText(command.Description, lblDescription.FontIndex,
                hotkeyInfoPanel.Width - lblDescription.X).Text;
            lblCurrentHotkeyValue.Text = command.Hotkey?.ToStringWithNone();

            lblDefaultHotkeyValue.Text = command.DefaultHotkey?.ToStringWithNone();
            btnResetKey.Enabled = command.DefaultHotkey != command.Hotkey;

            lblNewHotkeyValue.Text = HOTKEY_TIP_TEXT;
            pendingHotkey = Hotkey.None;
            lblCurrentlyAssignedTo.Text = string.Empty;
        }

        private void DdCategory_SelectedIndexChanged(object? sender, EventArgs e)
        {
            lbHotkeys.ClearItems();
            lbHotkeys.TopIndex = 0;
            string category = ddCategory.SelectedItem.Text;
            foreach (var command in gameCommands)
            {
                if (command.Category == category)
                {
                    lbHotkeys.AddItem(new XNAListBoxItem[] {
                        new XNAListBoxItem() { Text = command.UIName, Tag = command },
                        new XNAListBoxItem() { Text = command.Hotkey?.ToString() }
                    });
                }
            }

            lbHotkeys.SelectedIndex = -1;
        }

        private void BtnAssign_LeftClick(object? sender, EventArgs e)
        {
            if (lbHotkeys.SelectedIndex < 0 || lbHotkeys.SelectedIndex >= lbHotkeys.ItemCount)
            {
                return;
            }

            // If the hotkey is already assigned to other command, unbind it
            if (pendingHotkey != Hotkey.None)
            {
                foreach (var gameCommand in gameCommands)
                {
                    if (pendingHotkey == gameCommand.Hotkey)
                        gameCommand.Hotkey = null;
                }
            }

            var command = (GameCommand)lbHotkeys.GetItem(0, lbHotkeys.SelectedIndex).Tag;
            command.Hotkey = pendingHotkey;
            RefreshHotkeyList();
            pendingHotkey = Hotkey.None;
        }

        private void RefreshHotkeyList()
        {
            int selectedIndex = lbHotkeys.SelectedIndex;
            int topIndex = lbHotkeys.TopIndex;
            DdCategory_SelectedIndexChanged(null, EventArgs.Empty);
            lbHotkeys.TopIndex = topIndex;
            lbHotkeys.SelectedIndex = selectedIndex;
        }

        /// <summary>
        /// Detects when the user has pressed a key to generate a new hotkey.
        /// </summary>
        private void Keyboard_OnKeyPressed(object? sender, Rampastring.XNAUI.Input.KeyPressEventArgs e)
        {
            foreach (var blacklistedKey in keyBlacklist)
            {
                if (e.PressedKey == blacklistedKey)
                    return;
            }

            var currentModifiers = GetCurrentModifiers();

            // The XNA keys seem to match the Windows virtual keycodes! This saves us some work
            pendingHotkey = new Hotkey(e.PressedKey, currentModifiers);

            lblCurrentlyAssignedTo.Text = string.Empty;

            foreach (var command in gameCommands)
            {
                if (pendingHotkey == command.Hotkey)
                    lblCurrentlyAssignedTo.Text = "Currently assigned to:".L10N("Client:DTAConfig:CurrentAssignTo") + Environment.NewLine + command.UIName;
            }
        }

        private void BtnCancel_LeftClick(object? sender, EventArgs e)
        {
            Disable();
        }

        private void BtnSave_LeftClick(object? sender, EventArgs e)
        {
            WriteKeyboardINI();
            Disable();
        }

        /// <summary>
        /// Updates the logic of the window.
        /// Used for keeping the "new hotkey" display in sync with the keyboard's
        /// modifier keys.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            var oldModifiers = pendingHotkey.Modifier;
            var currentModifiers = GetCurrentModifiers();

            if ((pendingHotkey.Key == Keys.None && currentModifiers != oldModifiers)
                ||
                (pendingHotkey.Key != Keys.None &&
                lastFrameModifiers == KeyModifiers.None &&
                currentModifiers != lastFrameModifiers))
            {
                pendingHotkey = new Hotkey(Keys.None, currentModifiers);
                lblCurrentlyAssignedTo.Text = string.Empty;
            }

            string displayString = pendingHotkey.ToString();
            if (displayString != string.Empty)
                lblNewHotkeyValue.Text = pendingHotkey.ToString();
            else
                lblNewHotkeyValue.Text = HOTKEY_TIP_TEXT;

            lastFrameModifiers = currentModifiers;
        }

        /// <summary>
        /// Detects which key modifiers (Ctrl, Shift, Alt) the user is currently pressing.
        /// </summary>
        private KeyModifiers GetCurrentModifiers()
        {
            var currentModifiers = KeyModifiers.None;

            if (Keyboard.IsKeyHeldDown(Keys.RightControl) ||
                Keyboard.IsKeyHeldDown(Keys.LeftControl))
            {
                currentModifiers |= KeyModifiers.Ctrl;
            }

            if (Keyboard.IsKeyHeldDown(Keys.RightShift) ||
                Keyboard.IsKeyHeldDown(Keys.LeftShift))
            {
                currentModifiers |= KeyModifiers.Shift;
            }

            if (Keyboard.IsKeyHeldDown(Keys.LeftAlt) ||
                Keyboard.IsKeyHeldDown(Keys.RightAlt))
            {
                currentModifiers |= KeyModifiers.Alt;
            }

            return currentModifiers;
        }

        private bool HasDuplicateHotkeys()
        {
            var assignedHotkeys = new HashSet<Hotkey>();
            foreach (var command in gameCommands)
            {
                if (command.Hotkey != null && command.Hotkey != Hotkey.None)
                {
                    if (assignedHotkeys.Contains(command.Hotkey))
                    {
#if DEBUG
                        Debugger.Break();
#endif

                        return true;
                    }

                    assignedHotkeys.Add(command.Hotkey);
                }
            }

            return false;
        }

        private void WriteKeyboardINI(bool writeEvenIfSettingsIniAsKeyboardIniHolds = false)
        {
            Debug.Assert(!HasDuplicateHotkeys(), "There are duplicate hotkeys assigned. How could this happen?");

            IniFile keyboardIni = ClientConfiguration.Instance.SettingsIniAsKeyboardIni
                ? UserINISettings.Instance.SettingsIni
                : new IniFile(SafePath.CombineFilePath(
                    ProgramConstants.GamePath,
                    ClientConfiguration.Instance.KeyboardINI));

            var hotkeySection = keyboardIni.GetOrAddSection(ClientConfiguration.Instance.KeyboardHotkeySection);
            foreach (var command in gameCommands)
            {
                // Note: we now explicitly differentiate between null and Hotkey.None
                if (command.Hotkey == null)
                {
                    if (hotkeySection.KeyExists(command.ININame))
                        hotkeySection.RemoveKey(command.ININame);
                }
                else
                {
                    hotkeySection.SetStringValue(command.ININame, command.Hotkey.GetTSEncoded().ToString());
                }
            }

            // Do not write INI file if using Settings.ini as Keyboard.ini. The hot keys will be saved when Settings.ini is saved.
            // We choose this policy because, imagine a situation when the user pressed save in the hotkey config window, then decided they don't want changes (not the hotkey changes) they did in the options.
            // If we don't flush here, everything can be restored by hitting a cancel.
            // If we flush here -- the player can't cancel anymore at all.
            if (writeEvenIfSettingsIniAsKeyboardIniHolds || !ClientConfiguration.Instance.SettingsIniAsKeyboardIni)
                keyboardIni.WriteIniFile();
        }

        /// <summary>
        /// A game command that can be assigned into a key on the keyboard.
        /// </summary>
        private class GameCommand
        {
            public GameCommand(string uiName, string category, string description, string iniName)
            {
                UIName = uiName;
                Category = category;
                Description = description;
                ININame = iniName;
            }

            /// <summary>
            /// Creates a game command and parses its information from an INI section.
            /// </summary>
            /// <param name="iniSection">The INI section.</param>
            public GameCommand(IniSection iniSection)
            {
                ININame = iniSection.SectionName;
                UIName = iniSection.GetStringValue("UIName", "Unnamed command")
                    .L10N($"INI:Hotkeys:{ININame}:UIName");
                string category = iniSection.GetStringValue("Category", "Unknown category");
                Category = category.L10N($"INI:HotkeyCategories:{category}");
                Description = iniSection.GetStringValue("Description", "Unknown description")
                    .L10N($"INI:Hotkeys:{ININame}:Description");

                int? defaultTSKey = iniSection.GetIntValueOrNull("DefaultKey");
                DefaultHotkey = defaultTSKey.HasValue ? new Hotkey(defaultTSKey.Value) : null;

                // Note: currently, we treat Hotkey.None as null for default hotkeys, since it doesn't make much sense to have a default hotkey that is explicitly "no hotkey" -- Hotkey.None prevents automatically setting a new hot key via DefaultHotkey from a future update
                if (DefaultHotkey == Hotkey.None)
                    DefaultHotkey = null;
            }

            public string UIName { get; private set; }
            public string Category { get; private set; }
            public string Description { get; private set; }
            public string ININame { get; private set; }
            public Hotkey? Hotkey { get; set; }
            public Hotkey? DefaultHotkey { get; private set; }
        }

        [Flags]
        private enum KeyModifiers
        {
            None = 0,
            Shift = 1,
            Ctrl = 2,
            Alt = 4
        }

        /// <summary>
        /// Represents a keyboard key with modifiers.
        /// </summary>
        private sealed record Hotkey
        {
            public Keys Key { get; }
            public KeyModifiers Modifier { get; }

            public static readonly Hotkey None = new(Keys.None, KeyModifiers.None);

            /// <summary>
            /// Creates a new hotkey by decoding a Tiberian Sun / Red Alert 2
            /// encoded key value.
            /// </summary>
            /// <param name="encodedKeyValue">The encoded key value.</param>
            public Hotkey(int encodedKeyValue)
            {
                Key = (Keys)(encodedKeyValue & 255);
                Modifier = (KeyModifiers)(encodedKeyValue >> 8);
            }

            public Hotkey(Keys key, KeyModifiers modifiers)
            {
                Key = key;
                Modifier = modifiers;
            }

            public override string ToString()
            {
                if (Key == Keys.None && Modifier == KeyModifiers.None)
                    return string.Empty;

                return GetString();
            }

            public string ToStringWithNone()
            {
                if (Key == Keys.None && Modifier == KeyModifiers.None)
                    return "None".L10N("Client:DTAConfig:HotkeyNone");

                return GetString();
            }

            /// <summary>
            /// Creates the display string for this key.
            /// </summary>
            private string GetString()
            {
                string str = "";

                if (Modifier.HasFlag(KeyModifiers.Shift))
                    str += "SHIFT+";

                if (Modifier.HasFlag(KeyModifiers.Ctrl))
                    str += "CTRL+";

                if (Modifier.HasFlag(KeyModifiers.Alt))
                    str += "ALT+";

                if (Key == Keys.None)
                    return str;

                return str + GetKeyDisplayString(Key);
            }

            /// <summary>
            /// Returns the hotkey in the Tiberian Sun / Red Alert 2 Keyboard.ini encoded format.
            /// </summary>
            public int GetTSEncoded()
            {
                return ((int)Modifier << 8) + (int)Key;
            }

            /// <summary>
            /// Returns the display string for an XNA key.
            /// Allows overriding specific key enum names to be more
            /// suitable for the UI.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <returns>A string.</returns>
            private string GetKeyDisplayString(Keys key)
            {
                switch (key)
                {
                    case Keys.D0:
                        return "0";
                    case Keys.D1:
                        return "1";
                    case Keys.D2:
                        return "2";
                    case Keys.D3:
                        return "3";
                    case Keys.D4:
                        return "4";
                    case Keys.D5:
                        return "5";
                    case Keys.D6:
                        return "6";
                    case Keys.D7:
                        return "7";
                    case Keys.D8:
                        return "8";
                    case Keys.D9:
                        return "9";
                    case (Keys)12:
                        return "NumPad5 (NumLock off)";
                    case (Keys)0x10:
                        return "Shift";
                    case (Keys)0x11:
                        return "Ctrl";
                    case (Keys)0x12:
                        return "Alt";
                    default:
                        return key.ToString();
                }
            }
        }
    }
}
