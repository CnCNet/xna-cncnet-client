using System;
using Microsoft.Xna.Framework.Graphics;
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

        public XNAClientToggleButton(WindowManager windowManager) : base(windowManager)
        {
        }
    }
}
