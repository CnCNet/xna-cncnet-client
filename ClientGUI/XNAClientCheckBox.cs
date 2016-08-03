using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;

namespace ClientGUI
{
    public class XNAClientCheckBox : XNACheckBox
    {
        public XNAClientCheckBox(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void Initialize()
        {
            CheckSoundEffect = AssetLoader.LoadSound("checkbox.wav");

            base.Initialize();
        }
    }
}
