using System;
using System.Collections.Generic;

using ClientCore;
using ClientCore.Extensions;

using ClientGUI;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAConfig;

/// <summary>
/// A window for configuring in-game hotkeys.
/// </summary>
public class HotkeyConfigurationWindow : XNAWindow
{
    private readonly string HOTKEY_TIP_TEXT = "Press a key...".L10N("Client:DTAConfig:PressAKey");
    private const string HOTKEY_INI_SECTION = "Hotkey";
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

    private readonly List<GameCommand> gameCommands = [];

    private XNAClientDropDown ddCategory;
    private XNAMultiColumnListBox lbHotkeys;

    private XNAPanel hotkeyInfoPanel;
    private XNALabel lblCommandCaption;
    private XNALabel lblDescription;
    private XNALabel lblCurrentHotkeyValue;
    private XNALabel lblNewHotkeyValue;
    private XNALabel lblCurrentlyAssignedTo;

    private XNALabel lblDefaultHotkeyValue;
    private XNAClientButton btnResetKey;

    private IniFile keyboardINI;

    private Hotkey pendingHotkey;
    private KeyModifiers lastFrameModifiers;

    public override void Initialize()
    {
        ReadGameCommands();

        Name = "HotkeyConfigurationWindow";
        ClientRectangle = new Rectangle(0, 0, 600, 450);
        BackgroundTexture = AssetLoader.LoadTextureUncached("hotkeyconfigbg.png");

        XNALabel lblCategory = new(WindowManager)
        {
            Name = "lblCategory",
            ClientRectangle = new Rectangle(12, 12, 0, 0),
            Text = "Category:".L10N("Client:DTAConfig:Category")
        };

        ddCategory = new XNAClientDropDown(WindowManager)
        {
            Name = "ddCategory"
        };
        ddCategory.ClientRectangle = new Rectangle(lblCategory.Right + 12,
            lblCategory.Y - 1, 250, ddCategory.Height);

        HashSet<string> categories = [];

        foreach (GameCommand command in gameCommands)
        {
            if (!categories.Contains(command.Category))
            {
                _ = categories.Add(command.Category);
            }
        }

        foreach (string category in categories)
        {
            ddCategory.AddItem(category);
        }

        lbHotkeys = new XNAMultiColumnListBox(WindowManager)
        {
            Name = "lbHotkeys",
            ClientRectangle = new Rectangle(12, ddCategory.Bottom + 12,
            ddCategory.Right - 12, Height - ddCategory.Bottom - 59),
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED,
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1)
        };
        lbHotkeys.AddColumn("Command".L10N("Client:DTAConfig:Command"), 150);
        lbHotkeys.AddColumn("Shortcut".L10N("Client:DTAConfig:Shortcut"), lbHotkeys.Width - 150);

        hotkeyInfoPanel = new XNAPanel(WindowManager)
        {
            Name = "HotkeyInfoPanel",
            ClientRectangle = new Rectangle(lbHotkeys.Right + 12,
            ddCategory.Y, Width - lbHotkeys.Right - 24, lbHotkeys.Height + ddCategory.Height + 12)
        };

        lblCommandCaption = new XNALabel(WindowManager)
        {
            Name = "lblCommandCaption",
            FontIndex = 1,
            ClientRectangle = new Rectangle(12, 12, 0, 0),
            Text = "Command name".L10N("Client:DTAConfig:CommandName")
        };

        lblDescription = new XNALabel(WindowManager)
        {
            Name = "lblDescription",
            ClientRectangle = new Rectangle(12, lblCommandCaption.Bottom + 12, 0, 0),
            Text = "Command description".L10N("Client:DTAConfig:CommandDescription")
        };

        XNALabel lblCurrentHotkey = new(WindowManager)
        {
            Name = "lblCurrentHotkey",
            ClientRectangle = new Rectangle(lblDescription.X,
            lblDescription.Bottom + 48, 0, 0),
            FontIndex = 1,
            Text = "Currently assigned hotkey:".L10N("Client:DTAConfig:CurrentHotKey")
        };

        lblCurrentHotkeyValue = new XNALabel(WindowManager)
        {
            Name = "lblCurrentHotkeyValue",
            ClientRectangle = new Rectangle(lblDescription.X,
            lblCurrentHotkey.Bottom + 6, 0, 0),
            Text = "Current hotkey value".L10N("Client:DTAConfig:CurrentHotKeyValue")
        };

        XNALabel lblNewHotkey = new(WindowManager)
        {
            Name = "lblNewHotkey",
            ClientRectangle = new Rectangle(lblDescription.X,
            lblCurrentHotkeyValue.Bottom + 48, 0, 0),
            FontIndex = 1,
            Text = "New hotkey:".L10N("Client:DTAConfig:NewHotKey")
        };

        lblNewHotkeyValue = new XNALabel(WindowManager)
        {
            Name = "lblNewHotkeyValue",
            ClientRectangle = new Rectangle(lblDescription.X,
            lblNewHotkey.Bottom + 6, 0, 0),
            Text = HOTKEY_TIP_TEXT
        };

        lblCurrentlyAssignedTo = new XNALabel(WindowManager)
        {
            Name = "lblCurrentlyAssignedTo",
            ClientRectangle = new Rectangle(lblDescription.X,
            lblNewHotkeyValue.Bottom + 12, 0, 0),
            Text = "Currently assigned to:".L10N("Client:DTAConfig:CurrentHotKeyAssign") + "\nKey"
        };

        XNAClientButton btnAssign = new(WindowManager)
        {
            Name = "btnAssign",
            ClientRectangle = new Rectangle(lblDescription.X,
            lblCurrentlyAssignedTo.Bottom + 24, UIDesignConstants.BUTTON_WIDTH_121, UIDesignConstants.BUTTON_HEIGHT),
            Text = "Assign Hotkey".L10N("Client:DTAConfig:AssignHotkey")
        };
        btnAssign.LeftClick += BtnAssign_LeftClick;

        btnResetKey = new XNAClientButton(WindowManager)
        {
            Name = "btnResetKey",
            ClientRectangle = new Rectangle(btnAssign.X, btnAssign.Bottom + 12, btnAssign.Width, 23),
            Text = "Reset to Default".L10N("Client:DTAConfig:ResetToDefault")
        };
        btnResetKey.LeftClick += BtnReset_LeftClick;

        XNALabel lblDefaultHotkey = new(WindowManager)
        {
            Name = "lblOriginalHotkey",
            ClientRectangle = new Rectangle(lblCurrentHotkey.X, btnResetKey.Bottom + 12, 0, 0),
            Text = "Default hotkey:".L10N("Client:DTAConfig:DefaultHotKey")
        };

        lblDefaultHotkeyValue = new XNALabel(WindowManager)
        {
            Name = "lblDefaultHotkeyValue",
            ClientRectangle = new Rectangle(lblDefaultHotkey.Right + 12, lblDefaultHotkey.Y, 0, 0)
        };

        XNAClientButton btnSave = new(WindowManager)
        {
            Name = "btnSave",
            ClientRectangle = new Rectangle(12, lbHotkeys.Bottom + 12, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT),
            Text = "Save".L10N("Client:DTAConfig:ButtonSave")
        };
        btnSave.LeftClick += BtnSave_LeftClick;

        XNAClientButton btnResetAllKeys = new(WindowManager)
        {
            Name = "btnResetAllToDefaults",
            ClientRectangle = new Rectangle(0, btnSave.Y, UIDesignConstants.BUTTON_WIDTH_121, UIDesignConstants.BUTTON_HEIGHT),
            Text = "Reset All Keys".L10N("Client:DTAConfig:ResetAllHotkey")
        };
        btnResetAllKeys.LeftClick += BtnResetToDefaults_LeftClick;
        AddChild(btnResetAllKeys);
        btnResetAllKeys.CenterOnParentHorizontally();

        XNAClientButton btnCancel = new(WindowManager)
        {
            Name = "btnExit",
            ClientRectangle = new Rectangle(Width - 104, btnSave.Y, UIDesignConstants.BUTTON_WIDTH_92, UIDesignConstants.BUTTON_HEIGHT),
            Text = "Cancel".L10N("Client:DTAConfig:ButtonCancel")
        };
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
        {
            Logger.Log("No keyboard game commands exist!");
        }

        GameProcessLogic.GameProcessExited += GameProcessLogic_GameProcessExited;

        base.Initialize();

        CenterOnParent();

        Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;
        EnabledChanged += HotkeyConfigurationWindow_EnabledChanged;
    }

    /// <summary>
    /// Reads game commands from an INI file.
    /// </summary>
    private void ReadGameCommands()
    {
        IniFile gameCommandsIni = new(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), KEYBOARD_COMMANDS_INI));

        List<string> sections = gameCommandsIni.GetSections();

        foreach (string sectionName in sections)
        {
            gameCommands.Add(new GameCommand(gameCommandsIni.GetSection(sectionName)));
        }
    }

    /// <summary>
    /// Resets the hotkey for the currently selected game command to its
    /// default value.
    /// </summary>
    private void BtnReset_LeftClick(object sender, EventArgs e)
    {
        if (lbHotkeys.SelectedIndex < 0 || lbHotkeys.SelectedIndex >= lbHotkeys.ItemCount)
        {
            return;
        }

        GameCommand command = (GameCommand)lbHotkeys.GetItem(0, lbHotkeys.SelectedIndex).Tag;
        command.Hotkey = command.DefaultHotkey;

        // If the hotkey is already assigned to some other command, unbind it
        foreach (GameCommand gameCommand in gameCommands)
        {
            if (pendingHotkey.Equals(gameCommand.Hotkey))
            {
                gameCommand.Hotkey = new Hotkey(Keys.None, KeyModifiers.None);
            }
        }

        pendingHotkey = new Hotkey(Keys.None, KeyModifiers.None);
        RefreshHotkeyList();
    }

    private void BtnResetToDefaults_LeftClick(object sender, EventArgs e)
    {
        foreach (GameCommand command in gameCommands)
        {
            command.Hotkey = command.DefaultHotkey;
        }

        RefreshHotkeyList();
    }

    private void HotkeyConfigurationWindow_EnabledChanged(object sender, EventArgs e)
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
        keyboardINI = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.KeyboardINI));

        if (SafePath.GetFile(ProgramConstants.GamePath, ClientConfiguration.Instance.KeyboardINI).Exists)
        {
            foreach (GameCommand command in gameCommands)
            {
                int hotkey = keyboardINI.GetIntValue("Hotkey", command.ININame, 0);

                Hotkey hotkeyStruct = new(hotkey);
                command.Hotkey = new Hotkey(GetKeyOverride(hotkeyStruct.Key), hotkeyStruct.Modifier);
            }
        }
        else
        {
            foreach (GameCommand command in gameCommands)
            {
                command.Hotkey = command.DefaultHotkey;
            }
        }
    }

    private void LbHotkeys_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (lbHotkeys.SelectedIndex < 0 || lbHotkeys.SelectedIndex >= lbHotkeys.ItemCount)
        {
            hotkeyInfoPanel.Disable();
            return;
        }

        hotkeyInfoPanel.Enable();
        GameCommand command = (GameCommand)lbHotkeys.GetItem(0, lbHotkeys.SelectedIndex).Tag;
        lblCommandCaption.Text = command.UIName;
        lblDescription.Text = Renderer.FixText(command.Description, lblDescription.FontIndex,
            hotkeyInfoPanel.Width - lblDescription.X).Text;
        lblCurrentHotkeyValue.Text = command.Hotkey.ToStringWithNone();

        lblDefaultHotkeyValue.Text = command.DefaultHotkey.ToStringWithNone();
        btnResetKey.Enabled = !command.Hotkey.Equals(command.DefaultHotkey);

        lblNewHotkeyValue.Text = HOTKEY_TIP_TEXT;
        pendingHotkey = new Hotkey(Keys.None, KeyModifiers.None);
        lblCurrentlyAssignedTo.Text = string.Empty;
    }

    private void DdCategory_SelectedIndexChanged(object sender, EventArgs e)
    {
        lbHotkeys.ClearItems();
        lbHotkeys.TopIndex = 0;
        string category = ddCategory.SelectedItem.Text;
        foreach (GameCommand command in gameCommands)
        {
            if (command.Category == category)
            {
                lbHotkeys.AddItem(new XNAListBoxItem[] {
                    new() { Text = command.UIName, Tag = command },
                    new() { Text = command.Hotkey.ToString() }
                });
            }
        }

        lbHotkeys.SelectedIndex = -1;
    }

    private void BtnAssign_LeftClick(object sender, EventArgs e)
    {
        if (lbHotkeys.SelectedIndex < 0 || lbHotkeys.SelectedIndex >= lbHotkeys.ItemCount)
        {
            return;
        }

        // If the hotkey is already assigned to other command, unbind it
        foreach (GameCommand gameCommand in gameCommands)
        {
            if (pendingHotkey.Equals(gameCommand.Hotkey))
            {
                gameCommand.Hotkey = new Hotkey(Keys.None, KeyModifiers.None);
            }
        }

        GameCommand command = (GameCommand)lbHotkeys.GetItem(0, lbHotkeys.SelectedIndex).Tag;
        command.Hotkey = pendingHotkey;
        RefreshHotkeyList();
        pendingHotkey = new Hotkey(Keys.None, KeyModifiers.None);
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
    private void Keyboard_OnKeyPressed(object sender, Rampastring.XNAUI.Input.KeyPressEventArgs e)
    {
        foreach (Keys blacklistedKey in keyBlacklist)
        {
            if (e.PressedKey == blacklistedKey)
            {
                return;
            }
        }

        KeyModifiers currentModifiers = GetCurrentModifiers();

        // The XNA keys seem to match the Windows virtual keycodes! This saves us some work
        pendingHotkey = new Hotkey(GetKeyOverride(e.PressedKey), currentModifiers);

        lblCurrentlyAssignedTo.Text = string.Empty;

        foreach (GameCommand command in gameCommands)
        {
            if (pendingHotkey.Equals(command.Hotkey))
            {
                lblCurrentlyAssignedTo.Text = "Currently assigned to:".L10N("Client:DTAConfig:CurrentAssignTo") + Environment.NewLine + command.UIName;
            }
        }
    }

    private void BtnCancel_LeftClick(object sender, EventArgs e)
    {
        Disable();
    }

    private void BtnSave_LeftClick(object sender, EventArgs e)
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

        KeyModifiers oldModifiers = pendingHotkey.Modifier;
        KeyModifiers currentModifiers = GetCurrentModifiers();

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
        lblNewHotkeyValue.Text = displayString != string.Empty ? pendingHotkey.ToString() : HOTKEY_TIP_TEXT;

        lastFrameModifiers = currentModifiers;
    }

    /// <summary>
    /// Detects which key modifiers (Ctrl, Shift, Alt) the user is currently pressing.
    /// </summary>
    private KeyModifiers GetCurrentModifiers()
    {
        KeyModifiers currentModifiers = KeyModifiers.None;

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

    private void WriteKeyboardINI()
    {
        IniFile keyboardIni = new();
        foreach (GameCommand command in gameCommands)
        {
            keyboardIni.SetStringValue("Hotkey", command.ININame, command.Hotkey.GetTSEncoded().ToString());
        }

        keyboardIni.WriteIniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, ClientConfiguration.Instance.KeyboardINI));
    }

    /// <summary>
    /// Allows defining keys that match other keys for in-game purposes
    /// and should be displayed as those keys instead.
    /// </summary>
    /// <param name="key">The key.</param>
    private Keys GetKeyOverride(Keys key)
    {
        // 12 is actually NumPad5 for the game
        return key == (Keys)12 ? Keys.NumPad5 : key;
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
            DefaultHotkey = new Hotkey(iniSection.GetIntValue("DefaultKey", 0));
        }

        public string UIName { get; private set; }
        public string Category { get; private set; }
        public string Description { get; private set; }
        public string ININame { get; private set; }
        public Hotkey Hotkey { get; set; }
        public Hotkey DefaultHotkey { get; private set; }
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
    private struct Hotkey
    {
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

        public Keys Key { get; private set; }
        public KeyModifiers Modifier { get; private set; }

        public override string ToString()
        {
            return Key == Keys.None && Modifier == KeyModifiers.None ? string.Empty : GetString();
        }

        public string ToStringWithNone()
        {
            return Key == Keys.None && Modifier == KeyModifiers.None ? "None".L10N("Client:DTAConfig:HotkeyNone") : GetString();
        }

        /// <summary>
        /// Creates the display string for this key.
        /// </summary>
        private string GetString()
        {
            string str = "";

            if (Modifier.HasFlag(KeyModifiers.Shift))
            {
                str += "SHIFT+";
            }

            if (Modifier.HasFlag(KeyModifiers.Ctrl))
            {
                str += "CTRL+";
            }

            if (Modifier.HasFlag(KeyModifiers.Alt))
            {
                str += "ALT+";
            }

            return Key == Keys.None ? str : str + GetKeyDisplayString(Key);
        }

        /// <summary>
        /// Returns the hotkey in the Tiberian Sun / Red Alert 2 Keyboard.ini encoded format.
        /// </summary>
        public int GetTSEncoded()
        {
            return ((int)Modifier << 8) + (int)Key;
        }

        public override bool Equals(object obj)
        {
            if (obj is not Hotkey)
            {
                return false;
            }

            Hotkey hotkey = (Hotkey)obj;
            return hotkey.Key == Key && hotkey.Modifier == Modifier;
        }

        public override int GetHashCode()
        {
            return GetTSEncoded();
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
            return key switch
            {
                Keys.D0 => "0",
                Keys.D1 => "1",
                Keys.D2 => "2",
                Keys.D3 => "3",
                Keys.D4 => "4",
                Keys.D5 => "5",
                Keys.D6 => "6",
                Keys.D7 => "7",
                Keys.D8 => "8",
                Keys.D9 => "9",
                _ => key.ToString(),
            };
        }
    }
}