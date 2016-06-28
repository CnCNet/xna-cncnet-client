using ClientGUI.DirectX;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientCore;
using Microsoft.Xna.Framework.Audio;

namespace ClientGUI.DXControls
{
    public class DXTrackbar : DXPanel
    {
        public DXTrackbar(Game game) : base(game)
        {

        }

        public int MinValue { get; set; }

        public int MaxValue { get; set; }

        int value = 0;
        public int Value
        {
            get { return value; }
            set
            {
                if (value > MaxValue)
                    this.value = MaxValue;
                else if (value < MinValue)
                    this.value = MinValue;
                else
                    this.value = value;
            }
        }

        public SoundEffect SoundEffectOnClick { get; set; }
        SoundEffectInstance clickEffectInstance;

        public Texture2D ButtonTexture {get; set; }

        public override void Initialize()
        {
            base.Initialize();

            ButtonTexture = AssetLoader.LoadTexture("trackbarButton.png");

            if (SoundEffectOnClick != null)
                clickEffectInstance = SoundEffectOnClick.CreateInstance();
        }

        protected override void ParseAttributeFromINI(IniFile iniFile, string key)
        {
            switch (key)
            {
                case "MinValue":
                    MinValue = iniFile.GetIntValue(Name, "MinValue", 0);
                    return;
                case "MaxValue":
                    MaxValue = iniFile.GetIntValue(Name, "MaxValue", 10);
                    return;
                case "Value":
                    Value = iniFile.GetIntValue(Name, "Value", 0);
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key);
        }

        public override void OnLeftClick()
        {
            Rectangle windowRect = WindowRectangle();

            int xOffset = Cursor.Instance().Location.X - windowRect.X;

            int tabCount = MaxValue - MinValue;

            int pixelsPerTab = windowRect.Width / tabCount;

            int currentTab = 0;

            for (int i = 0; i <= tabCount; i++)
            {
                if (i * pixelsPerTab - (pixelsPerTab / 2) < xOffset)
                {
                    currentTab = i;
                }
                else
                {
                    Value = currentTab + MinValue;
                    base.OnLeftClick();

                    if (clickEffectInstance != null)
                        clickEffectInstance.Play();

                    return;
                }
            }

            Value = MaxValue;

            base.OnLeftClick();

            if (clickEffectInstance != null)
                clickEffectInstance.Play();
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            Rectangle windowRectangle = WindowRectangle();

            int tabIndex = Value - MinValue;

            int tabCount = MaxValue - MinValue;

            int pixelsPerTab = windowRectangle.Width / tabCount;

            int tabLocationX = tabIndex * pixelsPerTab - (ButtonTexture.Width / 2);

            if (tabIndex == 0)
                tabLocationX += ButtonTexture.Width / 2;
            else if (tabIndex == tabCount)
                tabLocationX -= ButtonTexture.Width / 2;

            Renderer.DrawTexture(ButtonTexture,
                new Rectangle(windowRectangle.X + tabLocationX, windowRectangle.Y, ButtonTexture.Width, windowRectangle.Height),
                GetColorWithAlpha(Color.White));
        }
    }
}
