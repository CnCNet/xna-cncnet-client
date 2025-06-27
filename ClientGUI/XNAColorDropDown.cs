using System.Linq;

using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    public class XNAColorDropDown : XNAClientDropDown
    {
        private const int PADDING = 3;
        public ItemsKind ItemsDrawMode { get; set; } = ItemsKind.TextAndIcon;

        public int ColorTextureWidth;
        public int ColorTextureHeigth;
        public Texture2D RandomColorTexture { get; private set; }

        public XNAColorDropDown(WindowManager windowManager) : base(windowManager) 
        {
            ColorTextureWidth = Height - PADDING;
            ColorTextureHeigth = Height - PADDING;
            RandomColorTexture = AssetLoader.LoadTexture("randomicon.png");
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
                            Items.ForEach(item =>
                            {
                                if (!item.Text.Contains("Random".L10N("Client:Main:RandomColor")))
                                    item.Texture = AssetLoader.CreateTexture(
                                        item.TextColor ?? AssetLoader.GetColorFromString("255,255,255"),
                                        Width - PADDING,
                                        Height - PADDING);

                                item.Text = string.Empty;
                            });
                            FixLastItemLength();

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
                case nameof(ColorTextureHeigth):
                    ColorTextureHeigth = Conversions.IntFromString(value, ColorTextureHeigth);
                    break;
                case nameof(RandomColorTexture):
                    RandomColorTexture = AssetLoader.LoadTexture(value);
                    Items
                        .Where(item => item.Text.Contains("Random".L10N("Client:Main:RandomColor")))
                        .ToList()
                        .ForEach(item => item.Texture = RandomColorTexture);
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

            if (!text.Contains("Random".L10N("Client:Main:RandomColor")))
                item.Texture = AssetLoader.CreateTexture(color, ColorTextureWidth, ColorTextureHeigth);
            else
                item.Texture = RandomColorTexture;

            Items.Add(item);
        }

        public void FixLastItemLength(int _padding = 2)
        {
            var lastItem = Items[Items.Count - 1];
            lastItem.Texture = AssetLoader.CreateTexture(
                        lastItem.TextColor ?? AssetLoader.GetColorFromString("255,255,255"),
                        lastItem.Texture.Width,
                        lastItem.Texture.Height - _padding);
            Items[Items.Count - 1] = lastItem;
        }

        public enum ItemsKind
        {
            Text,
            Icon,
            TextAndIcon
        }
    }
}
