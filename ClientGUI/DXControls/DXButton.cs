using ClientCore;
using ClientGUI.DirectX;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientGUI.DXControls
{
    /// <summary>
    /// A basic button.
    /// </summary>
    public class DXButton : DXControl
    {
        public DXButton(Game game) : base(game) 
        {
            AlphaRate = 0.005f;
            TextColorIdle = UISettingsLoader.GetUIAltColor();
            TextColorHover = UISettingsLoader.GetUIAltColor();
        }

        public Texture2D IdleTexture { get; set; }

        public Texture2D HoverTexture { get; set; }

        public SoundEffect HoverSoundEffect { get; set; }
        SoundEffectInstance hoverSoundInstance;

        public SoundEffect ClickSoundEffect { get; set; }
        SoundEffectInstance clickSoundInstance { get; set; }

        public float AlphaRate { get; set; }
        float idleTextureAlpha = 1.0f;
        float hoverTextureAlpha = 0.0f;
        ButtonAnimationMode animationMode;

        public Keys HotKey { get; set; }

        public int FontIndex { get; set; }

        bool allowClick = true;
        public bool AllowClick
        {
            get { return allowClick; }
            set { allowClick = value; }
        }

        string text = String.Empty;
        public override string Text
        {
            get { return text; }
            set
            {
                text = value;
                if (adaptiveText)
                {
                    Vector2 textSize = Renderer.GetTextDimensions(text, FontIndex);
                    if (textSize.X < ClientRectangle.Width)
                    {
                        TextXPosition = (int)((ClientRectangle.Width - textSize.X) / 2);
                    }
                    else if (textSize.X > ClientRectangle.Width)
                    {
                        TextXPosition = (int)((textSize.X - ClientRectangle.Width) / -2);
                    }

                    if (textSize.Y < ClientRectangle.Height)
                    {
                        TextYPosition = (int)((ClientRectangle.Height - textSize.Y) / 2);
                    }
                    else if (textSize.Y > ClientRectangle.Height)
                    {
                        TextYPosition = Convert.ToInt32((textSize.Y - ClientRectangle.Height) / -2);
                    }
                }
            }
        }

        public int TextXPosition { get; set; }
        public int TextYPosition { get; set; }

        public Color TextColorIdle { get; set; }

        public Color TextColorHover { get; set; }

        Color textColor = Color.White;

        bool adaptiveText = true;
        public bool AdaptiveText
        {
            get { return adaptiveText; }
            set { adaptiveText = value; }
        }

        public override void OnMouseEnter()
        {
            base.OnMouseEnter();

            if (!AllowClick)
                return;

            animationMode = ButtonAnimationMode.HIGHLIGHT;
            idleTextureAlpha = 0.5f;
            hoverTextureAlpha = 0.75f;
            textColor = TextColorHover;

            if (HoverSoundEffect != null)
            {
                hoverSoundInstance.Play();
            }
        }

        public override void OnMouseLeave()
        {
            base.OnMouseLeave();

            if (!AllowClick)
                return;

            animationMode = ButtonAnimationMode.RETURN;
            idleTextureAlpha = 0.75f;
            hoverTextureAlpha = 0.5f;
            textColor = TextColorIdle;
        }

        public override void OnLeftClick()
        {
            if (!AllowClick)
                return;

            if (ClickSoundEffect != null)
                clickSoundInstance.Play();

            base.OnLeftClick();
        }

        public override void Initialize()
        {
            if (HoverSoundEffect != null)
                hoverSoundInstance = HoverSoundEffect.CreateInstance();

            if (ClickSoundEffect != null)
                clickSoundInstance = ClickSoundEffect.CreateInstance();

            textColor = TextColorIdle;
        }

        protected override void ParseAttributeFromINI(IniFile iniFile, string key)
        {
            switch (key)
            {
                case "TextColorIdle":
                    TextColorIdle = AssetLoader.GetColorFromString(iniFile.GetStringValue(Name, "TextColorIdle", "255,255,255"));
                    return;
                case "TextColorHover":
                    TextColorHover = AssetLoader.GetColorFromString(iniFile.GetStringValue(Name, "TextColorHover", "0,0,0"));
                    return;
                case "HoverSoundEffect":
                    HoverSoundEffect = AssetLoader.LoadSound(iniFile.GetStringValue(Name, "HoverSoundEffect", String.Empty));
                    return;
                case "ClickSoundEffect":
                    ClickSoundEffect = AssetLoader.LoadSound(iniFile.GetStringValue(Name, "ClickSoundEffect", String.Empty));
                    return;
                case "AdaptiveText":
                    AdaptiveText = iniFile.GetBooleanValue(Name, "AdaptiveText", true);
                    return;
                case "AlphaRate":
                    AlphaRate = iniFile.GetSingleValue(Name, "AlphaRate", 0.01f);
                    return;
                case "FontIndex":
                    FontIndex = iniFile.GetIntValue(Name, "FontIndex", 0);
                    return;
                case "IdleTexture":
                    IdleTexture = AssetLoader.LoadTexture(iniFile.GetStringValue(Name, "IdleTexture", String.Empty));
                    return;
                case "HoverTexture":
                    HoverTexture = AssetLoader.LoadTexture(iniFile.GetStringValue(Name, "HoverTexture", String.Empty));
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (animationMode == ButtonAnimationMode.HIGHLIGHT)
            {
                idleTextureAlpha -= AlphaRate;
                if (idleTextureAlpha < 0.0f)
                {
                    idleTextureAlpha = 0.0f;
                }

                hoverTextureAlpha += AlphaRate;
                if (hoverTextureAlpha >= 1.0f)
                {
                    hoverTextureAlpha = 1.0f;
                }
            }
            else
            {
                hoverTextureAlpha -= AlphaRate;
                if (hoverTextureAlpha < 0.0f)
                {
                    hoverTextureAlpha = 0.0f;
                }

                idleTextureAlpha += AlphaRate;
                if (idleTextureAlpha >= 1.0f)
                {
                    idleTextureAlpha = 1.0f;
                }
            }

            if (RKeyboard.PressedKeys.Contains(HotKey))
                OnLeftClick();
        }

        public override void Draw(GameTime gameTime)
        {
            Rectangle windowRectangle = WindowRectangle();

            if (IdleTexture != null)
            {
                if (idleTextureAlpha > 0f)
                    Renderer.DrawTexture(IdleTexture, windowRectangle, 
                        new Color(RemapColor.R, RemapColor.G, RemapColor.B, (int)(idleTextureAlpha * Alpha * 255)));

                if (HoverTexture != null && hoverTextureAlpha > 0f)
                    Renderer.DrawTexture(HoverTexture, windowRectangle, 
                        new Color(RemapColor.R, RemapColor.G, RemapColor.B, (int)(hoverTextureAlpha * Alpha * 255)));
            }

            Vector2 textPosition = new Vector2(windowRectangle.X + TextXPosition, windowRectangle.Y + TextYPosition);

            if (!Enabled || !AllowClick)
                Renderer.DrawStringWithShadow(text, FontIndex, textPosition, GetColorWithAlpha(Color.DarkGray));
            else
                Renderer.DrawStringWithShadow(text, FontIndex, textPosition, GetColorWithAlpha(textColor));

            base.Draw(gameTime);
        }
    }

    enum ButtonAnimationMode
    {
        NONE,
        HIGHLIGHT,
        RETURN
    }
}
