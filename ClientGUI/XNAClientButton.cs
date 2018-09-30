using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;

namespace ClientGUI
{
    public class XNAClientButton : XNAButton
    {
        public XNAClientButton(WindowManager windowManager) : base(windowManager)
        {
            FontIndex = 1;
        }

        public override void Initialize()
        {
            int width = Width;
            if (IdleTexture == null)
                IdleTexture = AssetLoader.LoadTexture(width + "pxbtn.png");

            if (HoverTexture == null)
                HoverTexture = AssetLoader.LoadTexture(width + "pxbtn_c.png");

            if (HoverSoundEffect == null)
                HoverSoundEffect = AssetLoader.LoadSound("button.wav");

            base.Initialize();
        }
    }
}
