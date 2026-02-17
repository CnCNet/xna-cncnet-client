using System;

using ClientCore;

using DTAClient.DXGUI.Generic;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Campaign;

public class CampaignDropDown : GameSessionDropDown
{
    public CampaignDropDown(WindowManager windowManager) : base (windowManager) { }
    
    public override void Initialize()
    {
        // Find the campaign selector that this control belongs to and register ourselves as a game option.

        XNAControl parent = Parent;
        while (true)
        {
            if (parent == null)
                break;

            // oh no, we have a circular class reference here!
            if (parent is CampaignSelector configView)
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
        if (key == "DataWriteMode" && value.ToUpper() == "MAPCODE" && !ClientConfiguration.Instance.CopyMissionsToSpawnmapINI)
        {
            throw new Exception($"Campaign settings can't affect map code if {nameof(ClientConfiguration.Instance.CopyMissionsToSpawnmapINI)} is disabled!\n\n"
                + $"Offending setting control: {Name}");
        }
        
        base.ParseControlINIAttribute(iniFile, key, value);
    }
}