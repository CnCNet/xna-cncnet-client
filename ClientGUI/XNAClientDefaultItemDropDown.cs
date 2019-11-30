using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    /// <summary>
    /// A drop-down control that has a default drop-down item with an optional string label displayed next to its text.
    /// </summary>
    public class XNAClientDefaultItemDropDown : XNAClientDropDown
    {
        /// <summary>
        /// String label displayed next to the default drop-down item text.
        /// </summary>
        public string DefaultItemLabel { get; set; }

        /// <summary>
        /// Index of the default drop-down item.
        /// </summary>
        public int DefaultItemIndex { get; set; } = -1;

        /// <summary>
        /// Creates a new default item drop-down control.
        /// </summary>
        /// <param name="windowManager">The WindowManager associated with this control.</param>
        public XNAClientDefaultItemDropDown(WindowManager windowManager) : base(windowManager)
        {
        }

        protected override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "DefaultItemLabel":
                    DefaultItemLabel = value;
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        /// <summary>
        /// Draws the drop-down.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            if (DefaultItemIndex > -1 && DefaultItemIndex < Items.Count)
            {
                XNADropDownItem defaultItem = Items[DefaultItemIndex];
                string defaultItemOriginalText = defaultItem.Text;
                defaultItem.Text += " " + DefaultItemLabel;

                base.Draw(gameTime);

                defaultItem.Text = defaultItemOriginalText;
            }
            else
            {
                base.Draw(gameTime);
            }

        }
    }
}
