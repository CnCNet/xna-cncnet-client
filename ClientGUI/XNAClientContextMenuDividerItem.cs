using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    public class XNAClientContextMenuDividerItem : XNAPanel
    {
        public XNAClientContextMenuDividerItem(WindowManager windowManager) : base(windowManager)
        {
            Height = 4;
            Text = string.Empty;
        }

        public int GetLineY(int relativeY) => relativeY + (int)Math.Ceiling((double)Height / 2);
    }
}
