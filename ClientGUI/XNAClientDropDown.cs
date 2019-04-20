using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;

namespace ClientGUI
{
    public class XNAClientDropDown : XNADropDown
    {
        public XNAClientDropDown(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void Initialize()
        {
            ClickSoundEffect = new EnhancedSoundEffect("dropdown.wav");

            base.Initialize();
        }
    }
}
