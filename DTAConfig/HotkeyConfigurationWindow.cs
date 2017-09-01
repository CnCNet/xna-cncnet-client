using ClientCore;
using ClientGUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;

namespace DTAConfig
{
    /// <summary>
    /// A window for configuring in-game hotkeys.
    /// </summary>
    public class HotkeyConfigurationWindow : XNAWindow
    {
        private const string CATEGORY_MULTIPLAYER = "Multiplayer";
        private const string CATEGORY_CONTROL = "Control";
        private const string CATEGORY_INTERFACE = "Interface";
        private const string CATEGORY_SELECTION = "Selection";
        private const string CATEGORY_TEAM = "Team";

        private const string KEYBOARD_INI = "Keyboard.ini";

        public HotkeyConfigurationWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        private readonly GameCommand[] keyCommands = new GameCommand[]
        {
            new GameCommand("Chat to allies", CATEGORY_MULTIPLAYER, "Chat to players in your team.", "ChatToAllies"),
            new GameCommand("Chat to everyone", CATEGORY_MULTIPLAYER, "Chat to all players in the game (same as F8).", "ChatToAll"),
            new GameCommand("Alliance", CATEGORY_CONTROL, "Form an alliance with the owner of a selected object.", "ToggleAlliance"),
            new GameCommand("Deploy Object", CATEGORY_CONTROL, "Deploy selected units.", "DeployObject"),
            new GameCommand("Grant Control", CATEGORY_CONTROL, "Give control of your units to the owner of a selected object.", "GrantControl"),
            new GameCommand("Guard", CATEGORY_CONTROL, "Make your selected units guard the nearby area and automatically attack enemies.", "GuardObject"),
            new GameCommand("Scatter", CATEGORY_CONTROL, "Make your selected units scatter.", "ScatterObject"),
            new GameCommand("Select One Unit Less", CATEGORY_CONTROL, "Randomly unselect one of your selected units.", "SelectOneUnitLess"),
            new GameCommand("Stop Object", CATEGORY_CONTROL, "Stop your selected units.", "StopObject"),
            new GameCommand("Center View", CATEGORY_INTERFACE, "Center the camera to the selected objects.", "CenterView"),
            new GameCommand("Options Menu", CATEGORY_INTERFACE, "Open the in-game Options menu.", "Options"),
            new GameCommand("Center Base", CATEGORY_INTERFACE, "Center the camera on your base.", "CenterBase"),
            new GameCommand("Follow", CATEGORY_INTERFACE, "Make the selected objects follow another object.", "Follow"),
            new GameCommand("View Bookmark 1", CATEGORY_INTERFACE, "Center the camera around bookmark 1.", "View1"),
            new GameCommand("View Bookmark 2", CATEGORY_INTERFACE, "Center the camera around bookmark 2.", "View2"),
            new GameCommand("View Bookmark 3", CATEGORY_INTERFACE, "Center the camera around bookmark 3.", "View3"),
            new GameCommand("View Bookmark 4", CATEGORY_INTERFACE, "Center the camera around bookmark 4.", "View4"),
            new GameCommand("Set Bookmark 1", CATEGORY_INTERFACE, "Sets bookmark 1.", "SetView1"),
            new GameCommand("Set Bookmark 2", CATEGORY_INTERFACE, "Sets bookmark 2.", "SetView2"),
            new GameCommand("Set Bookmark 3", CATEGORY_INTERFACE, "Sets bookmark 3.", "SetView3"),
            new GameCommand("Set Bookmark 4", CATEGORY_INTERFACE, "Sets bookmark 4.", "SetView4"),
            new GameCommand("Scroll North", CATEGORY_INTERFACE, "Scroll the camera towards the north.", "ScrollNorth"),
            new GameCommand("Scroll South", CATEGORY_INTERFACE, "Scroll the camera towards the south.", "ScrollSouth"),
            new GameCommand("Scroll East", CATEGORY_INTERFACE, "Scroll the camera towards the east.", "ScrollEast"),
            new GameCommand("Scroll West", CATEGORY_INTERFACE, "Scroll the camera towards the west.", "ScrollWest"),
            new GameCommand("Sidebar Up", CATEGORY_INTERFACE, "Scroll the sidebar up.", "SidebarUp"),
            new GameCommand("Structure List Up", CATEGORY_INTERFACE, "Scroll the sidebar's structure list up.", "LeftSidebarUp"),
            new GameCommand("Unit List Up", CATEGORY_INTERFACE, "Scroll the sidebar's unit list up.", "RightSidebarUp"),
            new GameCommand("Sidebar Page Up", CATEGORY_INTERFACE, "Scroll the sidebar up by a page.", "SidebarPageUp"),
            new GameCommand("Structure List Page Up", CATEGORY_INTERFACE, "Scroll the sidebar's structure list up by a page.", "LeftSidebarPageUp"),
            new GameCommand("Unit List Page Up", CATEGORY_INTERFACE, "Scroll the sidebar's unit list up by a page.", "RightSidebarPageUp"),
            new GameCommand("Sidebar Down", CATEGORY_INTERFACE, "Scroll the sidebar down.", "SidebarDown"),
            new GameCommand("Structure List Down", CATEGORY_INTERFACE, "Scroll the sidebar's structure list down.", "LeftSidebarDown"),
            new GameCommand("Unit List Down", CATEGORY_INTERFACE, "Scroll the sidebar's unit list down.", "RightSidebarDown"),
            new GameCommand("Sidebar Page Down", CATEGORY_INTERFACE, "Scroll the sidebar down by a page.", "SidebarPageDown"),
            new GameCommand("Structure List Page Down", CATEGORY_INTERFACE, "Scroll the sidebar's structure list down by a page.", "LeftSidebarPageDown"),
            new GameCommand("Unit List Page Down", CATEGORY_INTERFACE, "Scroll the sidebar's unit list down by a page.", "RightSidebarPageDown"),
            new GameCommand("Goto Radar Event", CATEGORY_INTERFACE, "Center the camera around the latest radar event.", "CenterOnRadarEvent"),
            new GameCommand("Radar Toggle", CATEGORY_INTERFACE, "Toggle between the radar and the kill count screen (multiplayer only).", "ToggleRadar"),
            new GameCommand("Power Mode", CATEGORY_INTERFACE, "Enable power mode (allows powering structures on and off).", "TogglePower"),
            new GameCommand("Repair Mode", CATEGORY_INTERFACE, "Enable repair mode.", "ToggleRepair"),
            new GameCommand("Waypoint Mode", CATEGORY_INTERFACE, "Enable waypoint mode.", "WaypointMode"),
            new GameCommand("Screen Capture", CATEGORY_INTERFACE, "Takes a screenshot and saves it to the \"Screenshots\" sub-directory in your game directory.", "ScreenCapture"),
            new GameCommand("Delete Waypoint", CATEGORY_INTERFACE, "Deletes a waypoint.", "DeleteWaypoint"),
            new GameCommand("Toggle Info Panel", CATEGORY_INTERFACE, "Toggles the state of the sidebar info panel.", "ToggleInfoPanel"),
            new GameCommand("Place Building", CATEGORY_INTERFACE, "Places a finished building.", "PlaceBuilding"),
            new GameCommand("Repeat Last Building", CATEGORY_INTERFACE, "Repeats the last finished building.", "RepeatBuilding"),
            // new Hotkey("Toggle Help", ...)
            new GameCommand("Next Unit", CATEGORY_SELECTION, "Select the next unit.", "NextObject"),
            new GameCommand("Previous Unit", CATEGORY_SELECTION, "Select the previous unit.", "PreviousObject"),
            new GameCommand("Select Same Type", CATEGORY_SELECTION, "Select all units on the screen that are the type of your currently selected units.", "SelectType"),
            new GameCommand("Select View", CATEGORY_SELECTION, "Select all units on the screen.", "SelectView"),
            new GameCommand("Add Select Team 1", CATEGORY_TEAM, "Select team 1 without unselecting already selected objects", "TeamAddSelect_1"),
            new GameCommand("Add Select Team 2", CATEGORY_TEAM, "Select team 2 without unselecting already selected objects", "TeamAddSelect_2"),
            new GameCommand("Add Select Team 3", CATEGORY_TEAM, "Select team 3 without unselecting already selected objects", "TeamAddSelect_3"),
            new GameCommand("Add Select Team 4", CATEGORY_TEAM, "Select team 4 without unselecting already selected objects", "TeamAddSelect_4"),
            new GameCommand("Add Select Team 5", CATEGORY_TEAM, "Select team 5 without unselecting already selected objects", "TeamAddSelect_5"),
            new GameCommand("Add Select Team 6", CATEGORY_TEAM, "Select team 6 without unselecting already selected objects", "TeamAddSelect_6"),
            new GameCommand("Add Select Team 7", CATEGORY_TEAM, "Select team 7 without unselecting already selected objects", "TeamAddSelect_7"),
            new GameCommand("Add Select Team 8", CATEGORY_TEAM, "Select team 8 without unselecting already selected objects", "TeamAddSelect_8"),
            new GameCommand("Add Select Team 9", CATEGORY_TEAM, "Select team 9 without unselecting already selected objects", "TeamAddSelect_9"),
            new GameCommand("Add Select Team 10", CATEGORY_TEAM, "Select team 10 without unselecting already selected objects", "TeamAddSelect_10"),
            new GameCommand("Center Team 1", CATEGORY_TEAM, "Center the camera around team 1", "TeamCenter_1"),
            new GameCommand("Center Team 2", CATEGORY_TEAM, "Center the camera around team 2", "TeamCenter_2"),
            new GameCommand("Center Team 3", CATEGORY_TEAM, "Center the camera around team 3", "TeamCenter_3"),
            new GameCommand("Center Team 4", CATEGORY_TEAM, "Center the camera around team 4", "TeamCenter_4"),
            new GameCommand("Center Team 5", CATEGORY_TEAM, "Center the camera around team 5", "TeamCenter_5"),
            new GameCommand("Center Team 6", CATEGORY_TEAM, "Center the camera around team 6", "TeamCenter_6"),
            new GameCommand("Center Team 7", CATEGORY_TEAM, "Center the camera around team 7", "TeamCenter_7"),
            new GameCommand("Center Team 8", CATEGORY_TEAM, "Center the camera around team 8", "TeamCenter_8"),
            new GameCommand("Center Team 9", CATEGORY_TEAM, "Center the camera around team 9", "TeamCenter_9"),
            new GameCommand("Center Team 10", CATEGORY_TEAM, "Center the camera around team 10", "TeamCenter_10"),
            new GameCommand("Create Team 1", CATEGORY_TEAM, "Creates team 1", "TeamCreate_1"),
            new GameCommand("Create Team 2", CATEGORY_TEAM, "Creates team 2", "TeamCreate_2"),
            new GameCommand("Create Team 3", CATEGORY_TEAM, "Creates team 3", "TeamCreate_3"),
            new GameCommand("Create Team 4", CATEGORY_TEAM, "Creates team 4", "TeamCreate_4"),
            new GameCommand("Create Team 5", CATEGORY_TEAM, "Creates team 5", "TeamCreate_5"),
            new GameCommand("Create Team 6", CATEGORY_TEAM, "Creates team 6", "TeamCreate_6"),
            new GameCommand("Create Team 7", CATEGORY_TEAM, "Creates team 7", "TeamCreate_7"),
            new GameCommand("Create Team 8", CATEGORY_TEAM, "Creates team 8", "TeamCreate_8"),
            new GameCommand("Create Team 9", CATEGORY_TEAM, "Creates team 9", "TeamCreate_9"),
            new GameCommand("Create Team 10", CATEGORY_TEAM, "Creates team 10", "TeamCreate_10"),
            new GameCommand("Select Team 1", CATEGORY_TEAM, "Selects team 1", "TeamSelect_1"),
            new GameCommand("Select Team 2", CATEGORY_TEAM, "Selects team 2", "TeamSelect_2"),
            new GameCommand("Select Team 3", CATEGORY_TEAM, "Selects team 3", "TeamSelect_3"),
            new GameCommand("Select Team 4", CATEGORY_TEAM, "Selects team 4", "TeamSelect_4"),
            new GameCommand("Select Team 5", CATEGORY_TEAM, "Selects team 5", "TeamSelect_5"),
            new GameCommand("Select Team 6", CATEGORY_TEAM, "Selects team 6", "TeamSelect_6"),
            new GameCommand("Select Team 7", CATEGORY_TEAM, "Selects team 7", "TeamSelect_7"),
            new GameCommand("Select Team 8", CATEGORY_TEAM, "Selects team 8", "TeamSelect_8"),
            new GameCommand("Select Team 9", CATEGORY_TEAM, "Selects team 9", "TeamSelect_9"),
            new GameCommand("Select Team 10", CATEGORY_TEAM, "Selects team 10", "TeamSelect_10"),
        };

        private XNAClientDropDown ddCategory;
        private XNAMultiColumnListBox lbHotkeys;
        private XNAButton btnOK;

        private XNALabel lblCurrentCommand;
        private XNALabel lblDescription;
        private XNALabel lblCurrentHotkey;

        private IniFile keyboardINI;

        public override void Initialize()
        {
            Name = "HotkeyConfigurationWindow";
            ClientRectangle = new Rectangle(0, 0, 400, 300);

            var lblCategory = new XNALabel(WindowManager);
            lblCategory.Name = "lblCategory";
            lblCategory.ClientRectangle = new Rectangle(12, 12, 0, 0);
            lblCategory.Text = "Category:";

            ddCategory = new XNAClientDropDown(WindowManager);
            ddCategory.Name = "ddCategory";
            ddCategory.ClientRectangle = new Rectangle(lblCategory.Right + 12, 
                lblCategory.ClientRectangle.Y - 1, 200, ddCategory.Height);
            ddCategory.AddItem(CATEGORY_MULTIPLAYER);
            ddCategory.AddItem(CATEGORY_CONTROL);
            ddCategory.AddItem(CATEGORY_INTERFACE);
            ddCategory.AddItem(CATEGORY_SELECTION);
            ddCategory.AddItem(CATEGORY_TEAM);

            lbHotkeys = new XNAMultiColumnListBox(WindowManager);
            lbHotkeys.Name = "lbHotkeys";
            lbHotkeys.ClientRectangle = new Rectangle(12, ddCategory.Bottom + 12, 
                ddCategory.Right - 12, ClientRectangle.Height - ddCategory.Bottom - 24);
            lbHotkeys.AddColumn("Command", 150);
            lbHotkeys.AddColumn("Shortcut", lbHotkeys.Width - 150);

            AddChild(lbHotkeys);
            AddChild(lblCategory);
            AddChild(ddCategory);

            LoadKeyboardINI();

            ddCategory.SelectedIndexChanged += DdCategory_SelectedIndexChanged;
            ddCategory.SelectedIndex = 0;

            base.Initialize();

            CenterOnParent();

            Keyboard.OnKeyPressed += Keyboard_OnKeyPressed;
            //Game.Window.TextInput += Window_TextInput;

            //EventInput.Initialize(Game.Window);
        }

        private void LoadKeyboardINI()
        {
            keyboardINI = new IniFile(ProgramConstants.GamePath + KEYBOARD_INI);
            foreach (var command in keyCommands)
            {
                int hotkey = keyboardINI.GetIntValue("Hotkey", command.ININame, 0);

                command.Hotkey = new Hotkey(hotkey);
            }
        }

        private void DdCategory_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            lbHotkeys.ClearItems();
            string category = ddCategory.SelectedItem.Text;
            foreach (var command in keyCommands)
            {
                if (command.Category == category)
                {
                    lbHotkeys.AddItem(new string[] { command.UIName, command.Hotkey.ToString() }, true );
                }
            }
        }

        private void Keyboard_OnKeyPressed(object sender, Rampastring.XNAUI.Input.KeyPressEventArgs e)
        {
            return;
            // The XNA keys seem to match the Windows virtual keycodes! This saves us some work
        }



        class GameCommand
        {
            public GameCommand(string uiName, string category, string description, string iniName)
            {
                UIName = uiName;
                Category = category;
                Description = description;
                ININame = iniName;
            }

            public string UIName { get; private set; }
            public string Category { get; private set; }
            public string Description { get; private set; }
            public string ININame { get; private set; }
            public Hotkey Hotkey { get; set; }
        }

        [Flags]
        private enum KeyModifiers
        {
            Shift = 1,
            Ctrl = 2,
            Alt = 4
        }

        struct Hotkey
        {
            public Hotkey(int encodedKeyValue)
            {
                Key = (Keys)(encodedKeyValue & 255);
                Modifier = (KeyModifiers)(encodedKeyValue >> 8);
            }

            public Keys Key { get; private set; }
            public KeyModifiers Modifier { get; private set; }

            public override string ToString()
            {
                if (Key == Keys.None)
                    return string.Empty;

                string str = "";

                if (Modifier.HasFlag(KeyModifiers.Shift))
                    str += "SHIFT+";

                if (Modifier.HasFlag(KeyModifiers.Ctrl))
                    str += "CTRL+";

                if (Modifier.HasFlag(KeyModifiers.Alt))
                    str += "ALT+";

                return str + Key.ToString();
            }
        }
    }
}
