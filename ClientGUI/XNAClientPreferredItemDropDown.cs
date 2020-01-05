using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    /// <summary>
    /// A drop-down control that has a preferred drop-down item with an optional string label displayed next to its text.
    /// </summary>
    public class XNAClientPreferredItemDropDown : XNAClientDropDown
    {
        /// <summary>
        /// String label displayed next to the preferred drop-down item text.
        /// </summary>
        public string PreferredItemLabel { get; set; }

        /// <summary>
        /// Index of the preferred drop-down item.
        /// </summary>
        public int PreferredItemIndex { get; set; } = -1;

        /// <summary>
        /// Creates a new preferred item drop-down control.
        /// </summary>
        /// <param name="windowManager">The WindowManager associated with this control.</param>
        public XNAClientPreferredItemDropDown(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "PreferredItemLabel":
                    PreferredItemLabel = value;
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        /// <summary>
        /// Draws the drop-down.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            if (PreferredItemIndex > -1 && PreferredItemIndex < Items.Count)
            {
                XNADropDownItem preferredItem = Items[PreferredItemIndex];
                string preferredItemOriginalText = preferredItem.Text;
                preferredItem.Text += " " + PreferredItemLabel;

                base.Draw(gameTime);

                preferredItem.Text = preferredItemOriginalText;
            }
            else
            {
                base.Draw(gameTime);
            }

        }
    }
}
