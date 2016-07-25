using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
