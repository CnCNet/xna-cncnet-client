using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    public class XNAColorDropDown : XNAClientDropDown
    {
        private const int PADDING = 3;
        public ItemsKind ItemsDrawMode { get; set; } = ItemsKind.Text;

        public int ColorTextureWidth;
        public int ColorTextureHeigth;

        public XNAColorDropDown(WindowManager windowManager) : base(windowManager) 
        {
            ColorTextureWidth = Height - PADDING;
            ColorTextureHeigth = Height - PADDING;
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == nameof(ItemsDrawMode))
            {
                ItemsDrawMode = value.FromIniString().ToEnum<ItemsKind>();

                switch (ItemsDrawMode)
                {
                    case ItemsKind.Text:
                        Items.ForEach(item => 
                        { 
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
                        break;
                    case ItemsKind.TextAndIcon:
                    default:
                        break;
                }

                return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public new virtual void AddItem(string text, Color color)
        {
            var item = new XNADropDownItem();

            item.Text = text;
            item.TextColor = color;

            if (!text.Contains("Random".L10N("Client:Main:RandomColor")))
                item.Texture = AssetLoader.CreateTexture(color, ColorTextureWidth, ColorTextureHeigth);
            else
                item.Texture = AssetLoader.LoadTexture("randomicon.png");

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
