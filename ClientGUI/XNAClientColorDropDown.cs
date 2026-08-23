using System.Collections.Generic;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    public class XNAClientColorDropDown : XNAClientDropDown
    {
        private const int VERTICAL_PADDING = 3;
        private const int HORIZONTAL_PADDING = 2;
        public ItemsKind ItemsDrawMode { get; private set; } = ItemsKind.TextAndIcon;

        public int ColorTextureWidth { get; private set; }
        public int ColorTextureHeight { get; private set; }
        public Texture2D RandomColorTexture { get; private set; }
        public Texture2D DisabledItemTexture { get; private set; }

        private Dictionary<int, Texture2D> itemColorTextures = new Dictionary<int, Texture2D>();

        public XNAClientColorDropDown(WindowManager windowManager) : base(windowManager)
        {
            ColorTextureWidth = Height - VERTICAL_PADDING;
            ColorTextureHeight = Height - HORIZONTAL_PADDING;
            RandomColorTexture = AssetLoader.LoadTexture("randomicon.png");
            DisabledItemTexture = AssetLoader.CreateTexture(DisabledItemColor, ColorTextureWidth, ColorTextureHeight);
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case nameof(ItemsDrawMode):
                    ItemsDrawMode = value.FromIniString().ToEnum<ItemsKind>();

                    switch (ItemsDrawMode)
                    {
                        case ItemsKind.Text:
                            for (int i = 0; i < Items.Count; i++)
                            {
                                // Text mode: use transparent 1x1 texture as placeholder
                                var texture = AssetLoader.CreateTexture(AssetLoader.GetRGBAColorFromString("0,0,0,0"), 1, 1);
                                Items[i].Texture = texture;
                                itemColorTextures[i] = texture;
                            }
                            break;
                        case ItemsKind.Icon:
                            ColorTextureWidth = Width - VERTICAL_PADDING;
                            ColorTextureHeight = Height - HORIZONTAL_PADDING;

                            for (int i = 0; i < Items.Count; i++)
                            {
                                if (i != 0) // Skip random color item
                                {
                                    var texture = AssetLoader.CreateTexture(
                                        Items[i].TextColor ?? Color.White,
                                        ColorTextureWidth,
                                        ColorTextureHeight);
                                    Items[i].Texture = texture;
                                    itemColorTextures[i] = texture;
                                }

                                Items[i].Text = string.Empty;
                            }

                            DisabledItemTexture = AssetLoader.CreateTexture(DisabledItemColor, Width - VERTICAL_PADDING, Height - HORIZONTAL_PADDING);

                            break;
                        case ItemsKind.TextAndIcon:
                            break;
                        default:
                            break;
                    }

                    return;
                case nameof(ColorTextureWidth):
                    ColorTextureWidth = Conversions.IntFromString(value, ColorTextureWidth);
                    break;
                case nameof(ColorTextureHeight):
                    ColorTextureHeight = Conversions.IntFromString(value, ColorTextureHeight);
                    break;
                case nameof(RandomColorTexture):
                    RandomColorTexture = AssetLoader.LoadTexture(value);
                    Items[0].Texture = RandomColorTexture;
                    break;
                case nameof(DisabledItemTexture):
                    DisabledItemTexture = AssetLoader.LoadTexture(value);
                    break;
                default:
                    base.ParseControlINIAttribute(iniFile, key, value);
                    return;
            }
        }

        public new virtual void AddItem(string text, Color color)
        {
            var item = new XNADropDownItem();

            item.Text = text;
            item.TextColor = color;

            int index = Items.Count;

            if (index > 0) // Not the random color item
            {
                var texture = AssetLoader.CreateTexture(color, ColorTextureWidth, ColorTextureHeight);
                item.Texture = texture;
                itemColorTextures[index] = texture;
            }
            else
            {
                item.Texture = RandomColorTexture;
            }

            Items.Add(item);
        }

        /// <summary>
        /// Enables or disables the color texture for an item by swapping between the color texture and disabled texture.
        /// </summary>
        /// <param name="itemIndex">The index of the item.</param>
        /// <param name="enabled">If true, sets the color texture. If false, sets the disabled texture.</param>
        public void SetItemColorEnabled(int itemIndex, bool enabled)
        {
            if (itemIndex < 0 || itemIndex >= Items.Count)
                return;

            // Skip random color
            if (itemIndex == 0)
                return;

            // Skip if in text only mode
            if (ItemsDrawMode == ItemsKind.Text)
                return;

            if (enabled)
            {
                if (itemColorTextures.TryGetValue(itemIndex, out var colorTexture))
                    Items[itemIndex].Texture = colorTexture;
            }
            else
            {
                Items[itemIndex].Texture = DisabledItemTexture;
            }
        }

        public enum ItemsKind
        {
            Text,
            Icon,
            TextAndIcon
        }
    }
}
