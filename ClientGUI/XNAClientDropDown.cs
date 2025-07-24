using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Rampastring.Tools;
using System;
using ClientCore;
using ClientCore.Extensions;

namespace ClientGUI
{
    public class XNAClientDropDown : XNADropDown, IToolTipContainer
    {
        public ToolTip ToolTip { get; private set; }

        private string _initialToolTipText;
        public string ToolTipText
        {
            get => Initialized ? ToolTip?.Text : _initialToolTipText;
            set
            {
                if (Initialized)
                    ToolTip.Text = value;
                else
                    _initialToolTipText = value;
            }
        }

        public XNAClientDropDown(WindowManager windowManager) : base(windowManager) { }

        public override void Initialize()
        {
            ClickSoundEffect = new EnhancedSoundEffect("dropdown.wav");

            base.Initialize();

            ToolTip = new ToolTip(WindowManager, this) { Text = _initialToolTipText };
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == "ToolTip")
            {
                ToolTipText = value.FromIniString();
                return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public override void OnMouseLeftDown(InputEventArgs inputEventArgs)
        {
            // no need to set Handled to true since we're not "consuming" the event here, just augmenting
            base.OnMouseLeftDown(inputEventArgs);
            UpdateToolTipBlock();
        }

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
