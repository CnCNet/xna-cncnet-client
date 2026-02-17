using System;
using System.Collections.Generic;
using ClientCore.Extensions;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    public class XNAClientStateButton<T> : XNAButton where T : Enum
    {
        private T _state { get; set; }

        private Dictionary<T, Texture2D> StateTextures { get; set; }

        private string _toolTipText { get; set; }
        private ToolTip _toolTip { get; set; }

        public XNAClientStateButton(WindowManager windowManager, Dictionary<T, Texture2D> textures) : base(windowManager)
        {
            LeftClick += CycleState;
            StateTextures = textures;
        }

        public override void Initialize()
        {
            if (StateTextures == null || StateTextures.Count < 2)
                throw new ArgumentException("State button requires at least 2 states");

            UpdateStateTexture();

            base.Initialize();

            _toolTip = new ToolTip(WindowManager, this);
            SetToolTipText(_toolTipText);
            
            if (Width == 0)
                Width = IdleTexture.Width;
        }

        public void SetState(T state)
        {
            if(!Enum.IsDefined(typeof(T), state))
                throw new IndexOutOfRangeException($"{state} not a valid texture value");

            _state = state;
            UpdateStateTexture();
        }

        public T GetState() => _state;

        private void CycleState(object sender, EventArgs e)
        {
            _state = _state.CycleNext();
            UpdateStateTexture();
        }

        public void SetToolTipText(string text)
        {
            _toolTipText = text ?? string.Empty;
            if (_toolTip != null)
                _toolTip.Text = _toolTipText;
        }

        private void UpdateStateTexture()
        {
            IdleTexture = StateTextures[_state];
        }
    }
}
