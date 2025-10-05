using DTAClient.DXGUI.Generic;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

public class GameLobbyDropDown : GameSessionDropDown
{
    public GameLobbyDropDown(WindowManager windowManager) : base(windowManager) { }
    
    public int HostSelectedIndex { get; set; }

    public int UserSelectedIndex { get; set; }

    public override void Initialize()
    {
        // Find the game lobby that this control belongs to and register ourselves as a game option.

        XNAControl parent = Parent;
        while (true)
        {
            if (parent == null)
                break;

            // oh no, we have a circular class reference here!
            if (parent is GameLobbyBase configView)
            {
                configView.DropDowns.Add(this);
                break;
            }

            parent = parent.Parent;
        }

        base.Initialize();
    }

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        if (key == "DefaultIndex")
        {
            int index = int.Parse(value);
            HostSelectedIndex = index;
            UserSelectedIndex = index;
            // don't return, let base method handle it's part too
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }
    
    public override void OnLeftClick(InputEventArgs inputEventArgs)
    {
        // FIXME there's a discrepancy with how base XNAUI handles this
        // it doesn't set handled if changing the setting is not allowed
        inputEventArgs.Handled = true;
            
        if (!AllowDropDown)
            return;

        base.OnLeftClick(inputEventArgs);
        UserSelectedIndex = SelectedIndex;
    }
}