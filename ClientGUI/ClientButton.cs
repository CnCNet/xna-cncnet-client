using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;

namespace ClientGUI
{
    public class XNAClientButton : XNAButton
    {
        public XNAClientButton(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void Initialize()
        {
            int width = ClientRectangle.Width;
            IdleTexture = AssetLoader.LoadTexture(width + "pxbtn.png");
            HoverTexture = AssetLoader.LoadTexture(width + "pxbtn_c.png");

            HoverSoundEffect = AssetLoader.LoadSound("button.wav");

            base.Initialize();
        }
    }
}
