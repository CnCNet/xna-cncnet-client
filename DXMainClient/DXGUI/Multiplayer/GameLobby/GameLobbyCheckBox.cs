using System.Collections.Generic;
using System.Linq;

using ClientCore.Extensions;

using DTAClient.DXGUI.Generic;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

public class GameLobbyCheckBox : GameSessionCheckBox
{
    public GameLobbyCheckBox(WindowManager windowManager) : base(windowManager) { }
    
    public bool IsMultiplayer { get; set; }

    /// <summary>
    /// The last host-defined value for this check box.
    /// Defaults to the default value of Checked after the check-box
    /// has been initialized, but its value is only changed by user interaction.
    /// </summary>
    public bool HostChecked { get; set; }

    /// <summary>
    /// The last value that the local player gave for this check box.
    /// Defaults to the default value of Checked after the check-box
    /// has been initialized, but its value is only changed by user interaction.
    /// </summary>
    public bool UserChecked { get; set; }

    /// <summary>
    /// The side indices that this check box disallows when checked.
    /// Defaults to -1, which means none.
    /// </summary>
    public List<int> DisallowedSideIndices = new();
    
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
                configView.CheckBoxes.Add(this);
                break;
            }

            parent = parent.Parent;
        }

        base.Initialize();
    }

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "CheckedMP":
                if (IsMultiplayer)
                    Checked = Conversions.BooleanFromString(value, false);
                return;
            case "Checked":
                bool checkedValue = Conversions.BooleanFromString(value, false);
                HostChecked = checkedValue;
                UserChecked = checkedValue;
                break;  // let base method handle it too as we're not replacing it fully
            case "DisallowedSideIndex":
            case "DisallowedSideIndices":
                List<int> sides = value.SplitWithCleanup()
                    .Select(s => Conversions.IntFromString(s, -1))
                    .Distinct()
                    .ToList();
                DisallowedSideIndices.AddRange(sides.Where(s => !DisallowedSideIndices.Contains(s)));
                return;
        }
        
        base.ParseControlINIAttribute(iniFile, key, value);
    }
    
    /// <summary>
    /// Applies the check-box's disallowed side index to a bool
    /// array that determines which sides are disabled.
    /// </summary>
    /// <param name="disallowedArray">An array that determines which sides are disabled.</param>
    public void ApplyDisallowedSideIndex(bool[] disallowedArray)
    {
        if (DisallowedSideIndices == null || DisallowedSideIndices.Count == 0)
            return;

        if (Checked != reversed)
        {
            for (int i = 0; i < DisallowedSideIndices.Count; i++)
            {
                int sideNotAllowed = DisallowedSideIndices[i];
                disallowedArray[sideNotAllowed] = true;
            }
        }
    }
    
    public override void OnLeftClick(InputEventArgs inputEventArgs)
    {
        // FIXME there's a discrepancy with how base XNAUI handles this
        // it doesn't set handled if changing the setting is not allowed
        inputEventArgs.Handled = true;
            
        if (!AllowChanges)
            return;

        base.OnLeftClick(inputEventArgs);
        UserChecked = Checked;
    }
}