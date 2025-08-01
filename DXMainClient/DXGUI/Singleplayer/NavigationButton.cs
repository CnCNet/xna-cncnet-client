using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DTAClient.DXGUI.Generic;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Singleplayer
{
    /// <summary>
    /// Used to navigate between different campaign windows.
    /// </summary>
    public class NavigationButton : XNAButton
    {
        public string NextPanel { get; private set; }
        public NavigationButton(WindowManager windowManager) : base(windowManager)
        {
        }
        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            Logger.Log("Initializing Navigation Button: " + Name);
            if (key == "NextPanel")
            {
                NextPanel = value;
                return;
            }
            base.ParseControlINIAttribute(iniFile, key, value);
        }
    }
}
