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

        private ToolTip toolTip;

        public override void Initialize()
        {
            base.Initialize();

            toolTip = new ToolTip(WindowManager, this);
        }

        public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            if (key == "URL")
            {
                URL = value;
                return;
            }
            else if (key == "ToolTip")
            {
                toolTip.Text = value.Replace("@", Environment.NewLine);
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
