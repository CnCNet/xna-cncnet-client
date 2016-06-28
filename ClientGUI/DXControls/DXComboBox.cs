using ClientGUI.DirectX;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientGUI.DXControls
{
    /// <summary>
    /// A combo box, commonly also known as a drop-down box.
    /// </summary>
    public class DXComboBox : DXControl
    {
        public DXComboBox(Game game) : base(game)
        {
            BorderColor = UISettingsLoader.GetDropDownBorderColor();
            FocusColor = UISettingsLoader.GetListBoxFocusColor();
            BackColor = UISettingsLoader.GetUIBackColor();
        }

        public delegate void SelectedIndexChangedEventHandler(object sender, EventArgs e);
        public event SelectedIndexChangedEventHandler SelectedIndexChanged;

        int _itemHeight = 17;
        public int ItemHeight
        {
            get { return _itemHeight; }
            set { _itemHeight = value; }
        }

        public List<DXComboBoxItem> Items = new List<DXComboBoxItem>();

        public bool IsDroppedDown { get; set; }

        bool _allowDropDown = true;
        public bool AllowDropDown
        {
            get { return _allowDropDown; }
            set { _allowDropDown = value; }
        }

        int _selectedIndex = -1;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                int oldSelectedIndex = _selectedIndex;

                _selectedIndex = value;

                if (value != oldSelectedIndex && SelectedIndexChanged != null)
                    SelectedIndexChanged(this, EventArgs.Empty);
            }
        }

        public int FontIndex { get; set; }

        int hoveredIndex = 0;

        public Color BorderColor { get; set; }

        public Color FocusColor { get; set; }

        public Color BackColor { get; set; }

        Texture2D dropDownTexture { get; set; }
        Texture2D dropDownOpenTexture { get; set; }

        bool leftClickHandled = false;

        public void AddItem(DXComboBoxItem item)
        {
            Items.Add(item);
        }

        public void AddItem(string text)
        {
            DXComboBoxItem item = new DXComboBoxItem();
            item.Text = text;
            item.TextColor = UISettingsLoader.GetUIAltColor();

            Items.Add(item);
        }

        public override void Initialize()
        {
            base.Initialize();

            dropDownTexture = AssetLoader.LoadTexture("comboBoxArrow.png");
            dropDownOpenTexture = AssetLoader.LoadTexture("openedComboBoxArrow.png");

            ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, dropDownTexture.Height);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (IsDroppedDown && Cursor.Instance().LeftClicked && !leftClickHandled)
                OnLeftClick();

            leftClickHandled = false;
        }

        public override void OnLeftClick()
        {
            base.OnLeftClick();

            if (!IsDroppedDown)
            {
                Rectangle wr = WindowRectangle();

                IsDroppedDown = true;
                HasExclusiveCursorAccess = true;
                ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, dropDownTexture.Height + 1 + ItemHeight * Items.Count);
                Cursor.Instance().ExclusiveAccessArea = new Rectangle(wr.X, wr.Y, wr.Width, dropDownTexture.Height + 1 + ItemHeight * Items.Count);
                hoveredIndex = -1;
                leftClickHandled = true;
                return;
            }

            Point p = GetCursorPoint();

            if (p.Y > dropDownTexture.Height + 1)
            {
                int y = p.Y - dropDownTexture.Height + 1;
                int itemIndex = y / _itemHeight;

                if (itemIndex >= Items.Count || itemIndex < 0)
                    SelectedIndex = 0;
                else
                    SelectedIndex = itemIndex;
            }

            IsDroppedDown = false;
            HasExclusiveCursorAccess = false;
            Cursor.Instance().ExclusiveAccessArea = Rectangle.Empty;
            ClientRectangle = new Rectangle(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, dropDownTexture.Height);

            leftClickHandled = true;
        }

        public override void OnMouseMove()
        {
            base.OnMouseMove();

            if (!IsDroppedDown)
                return;

            Point p = GetCursorPoint();

            if (p.Y > dropDownTexture.Height + 1)
            {
                int y = p.Y - dropDownTexture.Height + 1;
                int itemIndex = y / _itemHeight;

                hoveredIndex = itemIndex;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            Rectangle wr = WindowRectangle();

            Renderer.FillRectangle(new Rectangle(wr.X + 1, wr.Y + 1, wr.Width - 2, wr.Height - 2), BackColor);
            Renderer.DrawRectangle(new Rectangle(wr.X, wr.Y, wr.Width, dropDownTexture.Height), BorderColor);

            if (SelectedIndex > -1)
            {
                DXComboBoxItem item = Items[SelectedIndex];

                int textX = 2;
                if (item.Texture != null)
                {
                    Renderer.DrawTexture(item.Texture, new Rectangle(wr.X + 1, wr.Y + 2, item.Texture.Width, item.Texture.Height), Color.White);
                    textX += item.Texture.Width + 1;
                }

                Renderer.DrawStringWithShadow(item.Text, FontIndex, new Vector2(wr.X + textX, wr.Y + 2), item.TextColor);
            }

            if (AllowDropDown)
            {
                Rectangle ddRectangle = new Rectangle(wr.X + wr.Width - dropDownTexture.Width,
                    wr.Y, dropDownTexture.Width, dropDownTexture.Height);

                if (IsDroppedDown)
                {
                    Renderer.DrawTexture(dropDownOpenTexture,
                        ddRectangle, GetColorWithAlpha(RemapColor));

                    Renderer.DrawRectangle(new Rectangle(wr.X, wr.Y + dropDownTexture.Height, wr.Width, wr.Height + 1 - dropDownTexture.Height), BorderColor);

                    for (int i = 0; i < Items.Count; i++)
                    {
                        DXComboBoxItem item = Items[i];

                        int y = wr.Y + dropDownTexture.Height + 1 + i * ItemHeight;
                        if (hoveredIndex == i)
                        {
                            Renderer.FillRectangle(new Rectangle(wr.X + 1, y, wr.Width - 2, ItemHeight), FocusColor);
                        }
                        else
                            Renderer.FillRectangle(new Rectangle(wr.X + 1, y, wr.Width - 2, ItemHeight), BackColor);

                        int textX = 2;
                        if (item.Texture != null)
                        {
                            Renderer.DrawTexture(item.Texture, new Rectangle(wr.X + 1, y + 1, item.Texture.Width, item.Texture.Height), Color.White);
                            textX += item.Texture.Width + 1;
                        }

                        Renderer.DrawStringWithShadow(item.Text, FontIndex, new Vector2(wr.X + textX, y + 1), item.TextColor);
                    }
                }
                else
                    Renderer.DrawTexture(dropDownTexture, ddRectangle, RemapColor);
            }

            base.Draw(gameTime);
        }
    }

    public class DXComboBoxItem
    {
        public Color TextColor { get; set; }

        public Texture2D Texture { get; set; }

        public string Text { get; set; }

        bool selectable = true;
        public bool Selectable
        {
            get { return selectable; }
            set { selectable = value; }
        }

        float alpha = 1.0f;
        public float Alpha
        {
            get { return alpha; }
            set
            {
                if (value < 0.0f)
                    alpha = 0.0f;
                else if (value > 1.0f)
                    alpha = 1.0f;
                else
                    alpha = value;
            }
        }
    }
}
