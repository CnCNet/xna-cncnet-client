using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            ClickSoundEffect = AssetLoader.LoadSound("dropdown.wav");

            base.Initialize();
        }
    }
}
