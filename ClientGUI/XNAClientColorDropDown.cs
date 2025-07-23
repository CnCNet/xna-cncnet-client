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
                            Items.ForEach(item =>
                            {
                                // Disposing cause client to crash, so replace texture with transparent one
                                item.Texture = AssetLoader.CreateTexture(AssetLoader.GetRGBAColorFromString("0,0,0,0"), 1, 1);
                            });
                            break;
                        case ItemsKind.Icon:
                            ColorTextureWidth = Width - VERTICAL_PADDING;
                            ColorTextureHeight = Height - HORIZONTAL_PADDING;
                            
                            Items.ForEach(item =>
                            {
                                if (Items[0] != item)
                                    item.Texture = AssetLoader.CreateTexture(
                                        item.TextColor ?? Color.White,
                                        ColorTextureWidth,
                                        ColorTextureHeight);

                                item.Text = string.Empty;
                            });
                            
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

            if (Items.Count > 1)
                item.Texture = AssetLoader.CreateTexture(color, ColorTextureWidth, ColorTextureHeight);
            else
                item.Texture = RandomColorTexture;

            Items.Add(item);
        }

        public enum ItemsKind
        {
            Text,
            Icon,
            TextAndIcon
        }
    }
}
