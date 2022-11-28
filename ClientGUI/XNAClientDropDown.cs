using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Rampastring.Tools;
using System;

namespace ClientGUI
{
    public class XNAClientDropDown : XNADropDown
    {
        public ToolTip ToolTip { get; set; }

        public bool DisabledMouseScroll { get; set; }

        public XNAClientDropDown(WindowManager windowManager) : base(windowManager)
        {
        }

        private void CreateToolTip()
        {
            if (ToolTip == null)
                ToolTip = new ToolTip(WindowManager, this);
        }

        public override void Initialize()
        {
            ClickSoundEffect = new EnhancedSoundEffect("dropdown.wav");

            CreateToolTip();

            base.Initialize();
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

        public override void OnMouseLeftDown()
        {
            base.OnMouseLeftDown();
            UpdateToolTipBlock();
        }

        public override void OnMouseScrolled()
        {
            if (DisabledMouseScroll)
                return;

            base.OnMouseScrolled();
        }

        public void Close() => CloseDropDown();

        protected override void CloseDropDown()
        {
            base.CloseDropDown();
            UpdateToolTipBlock();
        }

        protected void UpdateToolTipBlock()
        {
            if (DropDownState == DropDownState.CLOSED)
                ToolTip.Blocked = false;
            else
                ToolTip.Blocked = true;
        }
    }
}