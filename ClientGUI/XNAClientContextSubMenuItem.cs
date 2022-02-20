using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    public class XNAClientContextSubMenuItem<T> : XNAAdvancedContextMenuItem<T>
    {
        public XNAAdvancedContextMenu Menu { get; set; }

        public int ArrowGap { get; set; } = 6;
        public int ArrowThickness = 2;

        public XNAClientContextSubMenuItem(WindowManager windowManager) : base(windowManager)
        {
        }
    }
}
