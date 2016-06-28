using ClientGUI.DirectX;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientCore;

namespace ClientGUI.DXControls
{
    /// <summary>
    /// A static label control.
    /// </summary>
    public class DXLabel : DXControl
    {
        public DXLabel(Game game) : base(game)
        {
            RemapColor = UISettingsLoader.GetUILabelColor();
        }

        public int FontIndex { get; set; }

        public override void Initialize()
        {
            base.Initialize();

            Vector2 textSize = Renderer.GetTextDimensions(Text, FontIndex);
            ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y, (int)textSize.X, (int)textSize.Y);
        }

        protected override void ParseAttributeFromINI(IniFile iniFile, string key)
        {
            switch (key)
            {
                case "FontIndex":
                    FontIndex = iniFile.GetIntValue(Name, "FontIndex", 0);
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key);
        }

        public override void Draw(GameTime gameTime)
        {
            Renderer.DrawStringWithShadow(Text, FontIndex, new Vector2(GetLocationX(), GetLocationY()), GetRemapColor());
        }
    }
}
