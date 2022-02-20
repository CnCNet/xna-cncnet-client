using System;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace clientdx.DXGUI.Generic
{
    public class TeamStartMappingPresetMenuItem : XNAClientContextSubMenuItem<TeamStartMappingPreset>
    {
        public Action<TeamStartMappingPreset> SelectAction;

        public TeamStartMappingPresetMenuItem(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void OnLeftClick()
        {
            SelectAction?.Invoke(Item);
        }
    }
}
