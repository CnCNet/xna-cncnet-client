using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using Rampastring.Tools;
using System.Diagnostics;

namespace DTAClient.DXGUI.Generic
{
    public class LinkButton : XNAButton
    {
        public LinkButton(WindowManager windowManager) : base(windowManager)
        {
        }

        public string URL { get; set; }

        public override void OnLeftClick()
        {
            base.OnLeftClick();

            Process.Start(URL);
        }

        protected override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            if (key == "URL")
            {
                URL = value;
                return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }
    }
}
