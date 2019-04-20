using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;

namespace ClientGUI
{
    public class XNAClientTabControl : XNATabControl
    {
        public XNAClientTabControl(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void Initialize()
        {
            if (ClickSound == null)
            {
                ClickSound = new EnhancedSoundEffect("button.wav");
            }
                

            base.Initialize();
        }

        public void AddTab(string text, int width)
        {
            string tabAssetName = width + "pxtab";

            if (AssetLoader.AssetExists(tabAssetName + ".png"))
            {
                AddTab(text, AssetLoader.LoadTexture(tabAssetName + ".png"),
                    AssetLoader.LoadTexture(tabAssetName + "_c.png"));
            }
            else
            {
                AddTab(text, AssetLoader.LoadTexture(width + "pxbtn.png"),
                    AssetLoader.LoadTexture(width + "pxbtn_c.png"));
            }
        }
    }
}
