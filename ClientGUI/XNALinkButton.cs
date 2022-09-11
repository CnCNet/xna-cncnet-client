using System;
using Rampastring.XNAUI;
using Rampastring.Tools;
using ClientCore;

namespace ClientGUI
{
    public class XNALinkButton : XNAClientButton
    {
        public XNALinkButton(WindowManager windowManager) : base(windowManager)
        {
        }

        public string URL { get; set; }

        private ToolTip toolTip;

        private void CreateToolTip()
        {
            if (toolTip == null)
                toolTip = new ToolTip(WindowManager, this);
        }

        public override void Initialize()
        {
            base.Initialize();

            CreateToolTip();
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
                CreateToolTip();
                toolTip.Text = value.Replace("@", Environment.NewLine);
                return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        public override void OnLeftClick()
        {
            ProcessLauncher.StartShellProcess(URL);

            base.OnLeftClick();
        }
    }
}