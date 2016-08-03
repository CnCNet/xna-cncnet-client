using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using ClientCore;

namespace DTAConfig
{
    abstract class XNAOptionsPanel : XNAPanel
    {
        public XNAOptionsPanel(WindowManager windowManager, 
            UserINISettings iniSettings) : base(windowManager)
        {
            IniSettings = iniSettings;
        }

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(12, 47,
                Parent.ClientRectangle.Width - 24,
                Parent.ClientRectangle.Height - 94);
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            DrawMode = PanelBackgroundImageDrawMode.STRETCHED;

            base.Initialize();
        }

        protected UserINISettings IniSettings { get; private set; }

        /// <summary>
        /// Saves the options of this panel.
        /// Returns a bool that determines whether the 
        /// client needs to restart for changes to apply.
        /// </summary>
        public abstract bool Save();

        /// <summary>
        /// Loads the options of this panel.
        /// </summary>
        public abstract void Load();
    }
}
