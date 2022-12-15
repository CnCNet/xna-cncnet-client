using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using System;
using Rampastring.Tools;
using ClientCore;

namespace ClientGUI
{
    public class XNAClientCheckBox : XNACheckBox, IHasToolTip
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

        public XNAClientCheckBox(WindowManager windowManager) : base(windowManager) { }

        public override void Initialize()
        {
            CheckSoundEffect = new EnhancedSoundEffect("checkbox.wav");

            base.Initialize();

            ToolTip = new ToolTip(WindowManager, this) { Text = _initialToolTipText };
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == "ToolTip")
            {
                ToolTipText = value.Replace(ProgramConstants.INI_NEWLINE_PATTERN, Environment.NewLine);
                return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }
    }
}
