using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;

namespace ClientGUI
{
    public class XNAPasswordBox : XNATextBox
    {
        public char VisibleChar;

        public XNAPasswordBox(WindowManager wm) : base(wm)
        {
            VisibleChar = '*';
        }

        public string Password
        {
            get
            {
                return base.Text;
            }
            set
            {
                Text = value;
            }
        }

        public override string Text
        {
            get
            {
                return new string(VisibleChar, base.Text.Length);
            }
            set
            {
                base.Text = value;
            }
        }
    }
}
