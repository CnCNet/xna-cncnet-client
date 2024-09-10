using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System.Collections.Generic;

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
        public List<int> PreferredItemIndexes { get; set; } = new List<int>();

        /// <summary>
        /// Creates a new preferred item drop-down control.
        /// </summary>
        /// <param name="windowManager">The WindowManager associated with this control.</param>
        public XNAClientPreferredItemDropDown(WindowManager windowManager) : base(windowManager)
        {
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "PreferredItemLabel":
                    PreferredItemLabel = value;
                    return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        /// <summary>
        /// Draws the drop-down.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            if (PreferredItemIndexes.Count > 0)
            {
                PreferredItemIndexes.ForEach(i =>
                {
                    XNADropDownItem preferredItem = Items[i];
                    string preferredItemOriginalText = preferredItem.Text;
                    preferredItem.Text += " " + PreferredItemLabel;
                });

                base.Draw(gameTime);

                PreferredItemIndexes.ForEach(i =>
                {
                    XNADropDownItem preferredItem = Items[i];
                    preferredItem.Text = preferredItem.Text.Substring(0, preferredItem.Text.Length - PreferredItemLabel.Length - 1);
                });
            }
            else
            {
                base.Draw(gameTime);
            }

        }
    }
}
