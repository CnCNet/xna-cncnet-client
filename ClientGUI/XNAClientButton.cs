using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using ClientCore;

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
            int width = ClientRectangle.Width;
            if (IdleTexture == null)
                IdleTexture = AssetLoader.LoadTexture(width + "pxbtn.png");

            if (HoverTexture == null)
                HoverTexture = AssetLoader.LoadTexture(width + "pxbtn_c.png");

            if (UserINISettings.Instance.ClientButtonSounds)
                HoverSoundEffect = AssetLoader.LoadSound("button.wav");

            UserINISettings.Instance.SettingsSaved += Instance_SettingsSaved;

            base.Initialize();
        }

        private void Instance_SettingsSaved(object sender, EventArgs e)
        {
            if (UserINISettings.Instance.ClientButtonSounds)
                HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            else
                HoverSoundEffect = null;
        }
    }
}
