﻿using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Rampastring.Tools;

namespace ClientGUI
{
    public class XNAClientButton : XNAButton
    {
        private string _toolTipText { get; set; }
        
        private ToolTip _toolTip { get; set; }
        
        public XNAClientButton(WindowManager windowManager) : base(windowManager)
        {
            FontIndex = 1;
            Height = UIDesignConstants.BUTTON_HEIGHT;
        }

        public override void Initialize()
        {
            int width = Width;

            if (IdleTexture == null)
                IdleTexture = AssetLoader.LoadTexture(width + "pxbtn.png");

            if (HoverTexture == null)
                HoverTexture = AssetLoader.LoadTexture(width + "pxbtn_c.png");

            if (HoverSoundEffect == null)
                HoverSoundEffect = new EnhancedSoundEffect("button.wav");

            base.Initialize();

            if (Width == 0)
                Width = IdleTexture.Width;
            
            _toolTip = new ToolTip(WindowManager, this);
            SetToolTipText(_toolTipText);
        }

        public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            if (key == "MatchTextureSize" && Conversions.BooleanFromString(key, false))
            {
                Width = IdleTexture.Width;
                Height = IdleTexture.Height;
                return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        public void SetToolTipText(string text)
        {
            _toolTipText = text ?? string.Empty;
            if (_toolTip != null)
                _toolTip.Text = _toolTipText;
        }
    }
}
