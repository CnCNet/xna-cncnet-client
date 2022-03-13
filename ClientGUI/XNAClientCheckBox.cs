﻿using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using System;
using Rampastring.Tools;

namespace ClientGUI
{
    public class XNAClientCheckBox : XNACheckBox
    {
        public ToolTip ToolTip { get; set; }

        public XNAClientCheckBox(WindowManager windowManager) : base(windowManager)
        {
        }

        private void CreateToolTip()
        {
            if (ToolTip == null)
                ToolTip = new ToolTip(WindowManager, this);
        }

        public override void Initialize()
        {
            CheckSoundEffect = new EnhancedSoundEffect("checkbox.wav");

            base.Initialize();

            CreateToolTip();
        }

        public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            if (key == "ToolTip")
            {
                CreateToolTip();
                ToolTip.Text = value.Replace("@", Environment.NewLine);
                return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }
    }
}
