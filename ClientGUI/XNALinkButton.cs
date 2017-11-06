using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using Rampastring.Tools;
using System.Diagnostics;

namespace ClientGUI
{
    public class XNALinkButton : XNAClientButton
    {
        public XNALinkButton(WindowManager windowManager) : base(windowManager)
        {
        }

        public string URL { get; set; }

        protected override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            if (key == "URL")
            {
                URL = value;
                return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        public override void OnLeftClick()
        {
            Process.Start(URL);
            base.OnLeftClick();
        }
    }
}
