using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using System;
using Rampastring.Tools;

namespace ClientGUI
{
    public class XNAClientCheckBox : XNACheckBox
    {
        private ToolTip toolTip;

        public XNAClientCheckBox(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void Initialize()
        {
            CheckSoundEffect = new EnhancedSoundEffect("checkbox.wav");

            base.Initialize();

            toolTip = new ToolTip(WindowManager, this);
        }

        public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            if (key == "ToolTip")
            {
                toolTip.Text = value.Replace("@", Environment.NewLine);
                return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }
    }
}
