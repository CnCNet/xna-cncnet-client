using System;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    /// <summary>
    /// This is a combination of a checkbox and a standard button. You must specify
    /// the Checked and Unchecked Textures to render for each button state.
    /// </summary>
    public class XNAClientToggleButton : XNAButton
    {
        public Texture2D CheckedTexture { get; set; }
        public Texture2D UncheckedTexture { get; set; }

        private string _toolTipText { get; set; }
        private ToolTip ToolTip { get; set; }

        private bool _checked { get; set; }

        public override void Initialize()
        {
            if (CheckedTexture == null)
                throw new ArgumentNullException(nameof(CheckedTexture));

            if (UncheckedTexture == null)
                throw new ArgumentNullException(nameof(UncheckedTexture));

            UpdateIdleTexture();

            if (HoverSoundEffect == null)
                HoverSoundEffect = new EnhancedSoundEffect("button.wav");

            base.Initialize();

            ToolTip = new ToolTip(WindowManager, this);
            SetToolTipText(_toolTipText);

            if (Width == 0)
                Width = IdleTexture.Width;
        }

        public bool Checked
        {
            get => _checked;
            set
            {
                _checked = value;
                UpdateIdleTexture();
            }
        }

        private void UpdateIdleTexture()
        {
            IdleTexture = _checked ? CheckedTexture : UncheckedTexture;
        }

        public void SetToolTipText(string text)
        {
            _toolTipText = text ?? string.Empty;
            if (ToolTip != null)
                ToolTip.Text = _toolTipText;
        }

        public XNAClientToggleButton(WindowManager windowManager) : base(windowManager)
        {
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case nameof(CheckedTexture):
                    CheckedTexture = AssetLoader.LoadTexture(value);
                    UpdateIdleTexture();
                    break;
                case nameof(UncheckedTexture):
                    UncheckedTexture = AssetLoader.LoadTexture(value);
                    UpdateIdleTexture();
                    break;
                case nameof(ToolTip):
                    SetToolTipText(value);
                    break;
                default:
                    base.ParseControlINIAttribute(iniFile, key, value);
                    break;
            }
        }
    }
}
