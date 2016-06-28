using ClientGUI.DirectX;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientGUI.DXControls
{
    /// <summary>
    /// A control that has multiple tabs, of which only one can be selected at a time.
    /// </summary>
    public class DXTabControl : DXControl
    {
        public DXTabControl(Game game) : base(game)
        {
            TextColor = UISettingsLoader.GetUIAltColor();
        }

        public delegate void SelectedIndexChangedEventHandler(object sender, EventArgs e);
        public event SelectedIndexChangedEventHandler SelectedIndexChanged;

        int _selectedTab = 0;
        public int SelectedTab
        {
            get { return _selectedTab; }
            set
            {
                if (_selectedTab == value)
                    return;

                _selectedTab = value;
                if (SelectedIndexChanged != null)
                    SelectedIndexChanged(this, EventArgs.Empty);
            }
        }

        public int FontIndex { get; set; }

        public bool DisposeTexturesOnTabRemove { get; set; }

        public Color TextColor { get; set; }

        List<Tab> Tabs = new List<Tab>();

        public SoundEffect SoundOnClick { get; set; }
        SoundEffectInstance _soundInstance;

        public override void Initialize()
        {
            base.Initialize();

            if (SoundOnClick != null)
                _soundInstance = SoundOnClick.CreateInstance();
        }

        public void MakeSelectable(int index)
        {
            Tabs[index].Selectable = true;
        }

        public void MakeUnselectable(int index)
        {
            Tabs[index].Selectable = false;
        }

        public void RemoveTab(int index)
        {
            if (DisposeTexturesOnTabRemove)
            {
                Tabs[index].DefaultTexture.Dispose();
                Tabs[index].PressedTexture.Dispose();
            }

            Tabs.RemoveAt(index);
        }

        public void RemoveTab(string text)
        {
            int index = Tabs.FindIndex(t => t.Text == text);

            Tabs.RemoveAt(index);
        }

        public void AddTab(string text, Texture2D defaultTexture, Texture2D pressedTexture, bool selectable)
        {
            Tab tab = new Tab(text, defaultTexture, pressedTexture, selectable);
            Tabs.Add(tab);

            Vector2 textSize = Renderer.GetTextDimensions(text, FontIndex);
            tab.TextXPosition = (defaultTexture.Width - (int)textSize.X) / 2;
            tab.TextYPosition = (defaultTexture.Height - (int)textSize.Y) / 2;

            ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y,
                ClientRectangle.Width + defaultTexture.Width,
                defaultTexture.Height);
        }

        public override void OnLeftClick()
        {
            base.OnLeftClick();

            Point p = GetCursorPoint();

            int w = 0;
            int i = 0;
            foreach (Tab tab in Tabs)
            {
                w += tab.DefaultTexture.Width;

                if (p.X < w)
                {
                    if (tab.Selectable)
                    {
                        if (_soundInstance != null)
                        {
                            if (_soundInstance.State == SoundState.Playing)
                            {
                                _soundInstance.Stop();
                            }

                            _soundInstance.Play();
                        }

                        SelectedTab = i;
                    }

                    return;
                }

                i++;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            int x = 0;

            Rectangle windowRectangle = WindowRectangle();

            for (int i = 0; i < Tabs.Count; i++)
            {
                Tab tab = Tabs[i];

                Texture2D texture = tab.DefaultTexture;

                if (i == SelectedTab)
                    texture = tab.PressedTexture;

                Renderer.DrawTexture(texture, new Rectangle(windowRectangle.X + x, windowRectangle.Y,
                    tab.DefaultTexture.Width, tab.DefaultTexture.Height), RemapColor);

                Renderer.DrawStringWithShadow(tab.Text, FontIndex,
                    new Vector2(windowRectangle.X + x + tab.TextXPosition, windowRectangle.Y + tab.TextYPosition),
                    TextColor);

                x += tab.DefaultTexture.Width;
            }
        }
    }

    class Tab
    {
        public Tab() { }

        public Tab(string text, Texture2D defaultTexture, Texture2D pressedTexture, bool selectable)
        {
            Text = text;
            DefaultTexture = defaultTexture;
            PressedTexture = pressedTexture;
            Selectable = selectable;
        }

        public Texture2D DefaultTexture { get; set; }

        public Texture2D PressedTexture { get; set; }

        public string Text { get; set; }

        public bool Selectable { get; set; }

        public int TextXPosition { get; set; }

        public int TextYPosition { get; set; }
    }
}
