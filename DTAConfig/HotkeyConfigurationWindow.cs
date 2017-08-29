using ClientGUI;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

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

        public HotkeyConfigurationWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        private readonly Hotkey[] hotkeys = new Hotkey[]
        {
            new Hotkey("Chat to allies", CATEGORY_MULTIPLAYER, "Chat to players in your team.", "ChatToAllies"),
            new Hotkey("Chat to everyone", CATEGORY_MULTIPLAYER, "Chat to all players in the game (same as F8).", "ChatToAll"),
            new Hotkey("Alliance", CATEGORY_CONTROL, "Form an alliance with the owner of a selected object.", "ToggleAlliance"),
            new Hotkey("Deploy Object", CATEGORY_CONTROL, "Deploy selected units.", "DeployObject"),
            new Hotkey("Grant Control", CATEGORY_CONTROL, "Give control of your units to the owner of a selected object.", "GrantControl"),
            new Hotkey("Guard", CATEGORY_CONTROL, "Make your selected units guard the nearby area and automatically attack enemies.", "GuardObject"),
            new Hotkey("Scatter", CATEGORY_CONTROL, "Make your selected units scatter.", "ScatterObject"),
            new Hotkey("Select One Unit Less", CATEGORY_CONTROL, "Randomly unselect one of your selected units.", "SelectOneUnitLess"),
            new Hotkey("Stop Object", CATEGORY_CONTROL, "Stop your selected units.", "StopObject"),
            new Hotkey("Center View", CATEGORY_INTERFACE, "Center the camera to the selected objects.", "CenterView"),
            new Hotkey("Options Menu", CATEGORY_INTERFACE, "Open the in-game Options menu.", "Options"),
            new Hotkey("Center Base", CATEGORY_INTERFACE, "Center the camera on your base.", "CenterBase"),
            new Hotkey("Follow", CATEGORY_INTERFACE, "Make the selected objects follow another object.", "Follow"),
            new Hotkey("View Bookmark 1", CATEGORY_INTERFACE, "Center the camera around bookmark 1.", "View1"),
            new Hotkey("View Bookmark 2", CATEGORY_INTERFACE, "Center the camera around bookmark 2.", "View2"),
            new Hotkey("View Bookmark 3", CATEGORY_INTERFACE, "Center the camera around bookmark 3.", "View3"),
            new Hotkey("View Bookmark 4", CATEGORY_INTERFACE, "Center the camera around bookmark 4.", "View4"),
            new Hotkey("Set Bookmark 1", CATEGORY_INTERFACE, "Sets bookmark 1.", "SetView1"),
            new Hotkey("Set Bookmark 2", CATEGORY_INTERFACE, "Sets bookmark 2.", "SetView2"),
            new Hotkey("Set Bookmark 3", CATEGORY_INTERFACE, "Sets bookmark 3.", "SetView3"),
            new Hotkey("Set Bookmark 4", CATEGORY_INTERFACE, "Sets bookmark 4.", "SetView4"),
            new Hotkey("Scroll North", CATEGORY_INTERFACE, "Scroll the camera towards the north.", "ScrollNorth"),
            new Hotkey("Scroll South", CATEGORY_INTERFACE, "Scroll the camera towards the south.", "ScrollSouth"),
            new Hotkey("Scroll East", CATEGORY_INTERFACE, "Scroll the camera towards the east.", "ScrollEast"),
            new Hotkey("Scroll West", CATEGORY_INTERFACE, "Scroll the camera towards the west.", "ScrollWest"),
            new Hotkey("Sidebar Up", CATEGORY_INTERFACE, "Scroll the sidebar up.", "SidebarUp"),
            new Hotkey("Structure List Up", CATEGORY_INTERFACE, "Scroll the sidebar's structure list up.", "LeftSidebarUp"),
            new Hotkey("Unit List Up", CATEGORY_INTERFACE, "Scroll the sidebar's unit list up.", "RightSidebarUp"),
            new Hotkey("Sidebar Page Up", CATEGORY_INTERFACE, "Scroll the sidebar up by a page.", "SidebarPageUp"),
            new Hotkey("Structure List Page Up", CATEGORY_INTERFACE, "Scroll the sidebar's structure list up by a page.", "LeftSidebarPageUp"),
            new Hotkey("Unit List Page Up", CATEGORY_INTERFACE, "Scroll the sidebar's unit list up by a page.", "RightSidebarPageUp"),
            new Hotkey("Sidebar Down", CATEGORY_INTERFACE, "Scroll the sidebar down.", "SidebarDown"),
            new Hotkey("Structure List Down", CATEGORY_INTERFACE, "Scroll the sidebar's structure list down.", "LeftSidebarDown"),
            new Hotkey("Unit List Down", CATEGORY_INTERFACE, "Scroll the sidebar's unit list down.", "RightSidebarDown"),
            new Hotkey("Sidebar Page Down", CATEGORY_INTERFACE, "Scroll the sidebar down by a page.", "SidebarPageDown"),
            new Hotkey("Structure List Page Down", CATEGORY_INTERFACE, "Scroll the sidebar's structure list down by a page.", "LeftSidebarPageDown"),
            new Hotkey("Unit List Page Down", CATEGORY_INTERFACE, "Scroll the sidebar's unit list down by a page.", "RightSidebarPageDown"),
            new Hotkey("Goto Radar Event", CATEGORY_INTERFACE, "Center the camera around the latest radar event.", "CenterOnRadarEvent"),
            new Hotkey("Radar Toggle", CATEGORY_INTERFACE, "Toggle between the radar and the kill count screen (multiplayer only).", "ToggleRadar"),
            new Hotkey("Power Mode", CATEGORY_INTERFACE, "Enable power mode (allows powering structures on and off).", "TogglePower"),
            new Hotkey("Repair Mode", CATEGORY_INTERFACE, "Enable repair mode.", "ToggleRepair"),
            new Hotkey("Waypoint Mode", CATEGORY_INTERFACE, "Enable waypoint mode.", "WaypointMode"),
            new Hotkey("Screen Capture", CATEGORY_INTERFACE, "Takes a screenshot and saves it to the \"Screenshots\" sub-directory in your game directory.", "ScreenCapture"),
            new Hotkey("Delete Waypoint", CATEGORY_INTERFACE, "Deletes a waypoint.", "DeleteWaypoint"),
            new Hotkey("Toggle Info Panel", CATEGORY_INTERFACE, "Toggles the state of the sidebar info panel.", "ToggleInfoPanel"),
            new Hotkey("Place Building", CATEGORY_INTERFACE, "Places a finished building.", "PlaceBuilding"),
            new Hotkey("Repeat Last Building", CATEGORY_INTERFACE, "Repeats the last finished building.", "RepeatBuilding"),
            // new Hotkey("Toggle Help", ...)
            new Hotkey("Next Unit", CATEGORY_SELECTION, "Select the next unit.", "NextObject"),
            new Hotkey("Previous Unit", CATEGORY_SELECTION, "Select the previous unit.", "PreviousObject"),
            new Hotkey("Select Same Type", CATEGORY_SELECTION, "Select all units on the screen that are the type of your currently selected units.", "SelectType"),
            new Hotkey("Select View", CATEGORY_SELECTION, "Select all units on the screen.", "SelectView"),
            new Hotkey("Add Select Team 1", CATEGORY_TEAM, "Select team 1 without unselecting already selected objects", "TeamAddSelect_1"),
            new Hotkey("Add Select Team 2", CATEGORY_TEAM, "Select team 2 without unselecting already selected objects", "TeamAddSelect_2"),
            new Hotkey("Add Select Team 3", CATEGORY_TEAM, "Select team 3 without unselecting already selected objects", "TeamAddSelect_3"),
            new Hotkey("Add Select Team 4", CATEGORY_TEAM, "Select team 4 without unselecting already selected objects", "TeamAddSelect_4"),
            new Hotkey("Add Select Team 5", CATEGORY_TEAM, "Select team 5 without unselecting already selected objects", "TeamAddSelect_5"),
            new Hotkey("Add Select Team 6", CATEGORY_TEAM, "Select team 6 without unselecting already selected objects", "TeamAddSelect_6"),
            new Hotkey("Add Select Team 7", CATEGORY_TEAM, "Select team 7 without unselecting already selected objects", "TeamAddSelect_7"),
            new Hotkey("Add Select Team 8", CATEGORY_TEAM, "Select team 8 without unselecting already selected objects", "TeamAddSelect_8"),
            new Hotkey("Add Select Team 9", CATEGORY_TEAM, "Select team 9 without unselecting already selected objects", "TeamAddSelect_9"),
            new Hotkey("Add Select Team 10", CATEGORY_TEAM, "Select team 10 without unselecting already selected objects", "TeamAddSelect_10"),
            new Hotkey("Center Team 1", CATEGORY_TEAM, "Center the camera around team 1", "TeamCenter_1"),
            new Hotkey("Center Team 2", CATEGORY_TEAM, "Center the camera around team 2", "TeamCenter_2"),
            new Hotkey("Center Team 3", CATEGORY_TEAM, "Center the camera around team 3", "TeamCenter_3"),
            new Hotkey("Center Team 4", CATEGORY_TEAM, "Center the camera around team 4", "TeamCenter_4"),
            new Hotkey("Center Team 5", CATEGORY_TEAM, "Center the camera around team 5", "TeamCenter_5"),
            new Hotkey("Center Team 6", CATEGORY_TEAM, "Center the camera around team 6", "TeamCenter_6"),
            new Hotkey("Center Team 7", CATEGORY_TEAM, "Center the camera around team 7", "TeamCenter_7"),
            new Hotkey("Center Team 8", CATEGORY_TEAM, "Center the camera around team 8", "TeamCenter_8"),
            new Hotkey("Center Team 9", CATEGORY_TEAM, "Center the camera around team 9", "TeamCenter_9"),
            new Hotkey("Center Team 10", CATEGORY_TEAM, "Center the camera around team 10", "TeamCenter_10"),
            new Hotkey("Create Team 1", CATEGORY_TEAM, "Creates team 1", "TeamCreate_1"),
            new Hotkey("Create Team 2", CATEGORY_TEAM, "Creates team 2", "TeamCreate_2"),
            new Hotkey("Create Team 3", CATEGORY_TEAM, "Creates team 3", "TeamCreate_3"),
            new Hotkey("Create Team 4", CATEGORY_TEAM, "Creates team 4", "TeamCreate_4"),
            new Hotkey("Create Team 5", CATEGORY_TEAM, "Creates team 5", "TeamCreate_5"),
            new Hotkey("Create Team 6", CATEGORY_TEAM, "Creates team 6", "TeamCreate_6"),
            new Hotkey("Create Team 7", CATEGORY_TEAM, "Creates team 7", "TeamCreate_7"),
            new Hotkey("Create Team 8", CATEGORY_TEAM, "Creates team 8", "TeamCreate_8"),
            new Hotkey("Create Team 9", CATEGORY_TEAM, "Creates team 9", "TeamCreate_9"),
            new Hotkey("Create Team 10", CATEGORY_TEAM, "Creates team 10", "TeamCreate_10"),
            new Hotkey("Select Team 1", CATEGORY_TEAM, "Selects team 1", "TeamSelect_1"),
            new Hotkey("Select Team 2", CATEGORY_TEAM, "Selects team 2", "TeamSelect_2"),
            new Hotkey("Select Team 3", CATEGORY_TEAM, "Selects team 3", "TeamSelect_3"),
            new Hotkey("Select Team 4", CATEGORY_TEAM, "Selects team 4", "TeamSelect_4"),
            new Hotkey("Select Team 5", CATEGORY_TEAM, "Selects team 5", "TeamSelect_5"),
            new Hotkey("Select Team 6", CATEGORY_TEAM, "Selects team 6", "TeamSelect_6"),
            new Hotkey("Select Team 7", CATEGORY_TEAM, "Selects team 7", "TeamSelect_7"),
            new Hotkey("Select Team 8", CATEGORY_TEAM, "Selects team 8", "TeamSelect_8"),
            new Hotkey("Select Team 9", CATEGORY_TEAM, "Selects team 9", "TeamSelect_9"),
            new Hotkey("Select Team 10", CATEGORY_TEAM, "Selects team 10", "TeamSelect_10"),
        };

        private XNAClientDropDown ddCategory;
        private XNAListBox lbHotkeys;
        private XNAButton btnOK;

        private XNALabel lblCurrentCommand;
        private XNALabel lblDescription;
        private XNALabel lblCurrentHotkey;

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
                lblCategory.ClientRectangle.Y - 1, 100, ddCategory.Height);
            ddCategory.AddItem(CATEGORY_MULTIPLAYER);
            ddCategory.AddItem(CATEGORY_CONTROL);
            ddCategory.AddItem(CATEGORY_INTERFACE);
            ddCategory.AddItem(CATEGORY_SELECTION);
            ddCategory.AddItem(CATEGORY_TEAM);

            lbHotkeys = new XNAListBox(WindowManager);
            lbHotkeys.Name = "lbHotkeys";
            lbHotkeys.ClientRectangle = new Rectangle(12, ddCategory.Bottom + 12, 
                ddCategory.Right - 12, ClientRectangle.Height - ddCategory.Bottom - 12);

            ddCategory.SelectedIndexChanged += DdCategory_SelectedIndexChanged;
            ddCategory.SelectedIndex = 0;

            AddChild(lbHotkeys);
            AddChild(lblCategory);
            AddChild(ddCategory);

            base.Initialize();

            CenterOnParent();
        }

        private void DdCategory_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            lbHotkeys.Clear();
            string category = ddCategory.SelectedItem.Text;
            foreach (var hotkey in hotkeys)
            {
                if (hotkey.Category == category)
                {
                    lbHotkeys.AddItem(new XNAListBoxItem() { TextColor = lbHotkeys.DefaultItemColor,
                        Tag = hotkey, Text = hotkey.UIName } );
                }
            }
        }

        class Hotkey
        {
            public Hotkey(string uiName, string category, string description, string iniName)
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
            public int KeyNumber { get; set; }
        }
    }
}
