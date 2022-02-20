using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    public class XNAAdvancedContextMenuItem : XNAAdvancedContextMenuItem<object>
    {
        public XNAAdvancedContextMenuItem(WindowManager windowManager) : base(windowManager)
        {
        }
    }
    
    public class XNAAdvancedContextMenuItem<T> : XNAPanel
    {
        public T Item { get; set; }
        
        /// <summary>The text of the context menu item.</summary>
        public string Text { get; set; }

        /// <summary>
        /// The hint text of the context menu item.
        /// Drawn in the end of the item.
        /// </summary>
        public string HintText { get; set; }

        /// <summary>
        /// Determines whether the context menu item is enabled
        /// (can be clicked on).
        /// </summary>
        public bool Selectable { get; set; } = true;

        /// <summary>Determines whether the context menu item is visible.</summary>
        public bool Visible { get; set; } = true;

        /// <summary>
        /// The height of the context menu item.
        /// If null, the common item height is used.
        /// </summary>
        public int? Height { get; set; }

        /// <summary>
        /// The font index of the context menu item.
        /// If null, the common font index is used.
        /// </summary>
        public int? FontIndex { get; set; }

        /// <summary>The texture of the context menu item.</summary>
        public Texture2D Texture { get; set; }

        /// <summary>
        /// The background color of the context menu item.
        /// If null, the common background color is used.
        /// </summary>
        public Color? BackgroundColor { get; set; }

        /// <summary>
        /// The color of the context menu item's text.
        /// If null, the common text color is used.
        /// </summary>
        public Color? TextColor { get; set; }

        /// <summary>The method that is called when the item is selected.</summary>
        public Action SelectAction { get; set; }

        /// <summary>
        /// When the context menu is shown, this function is called
        /// to determine whether this item should be selectable.
        /// If null, the value of the Enabled property is not changed.
        /// </summary>
        public Func<bool> SelectableChecker { get; set; }

        /// <summary>
        /// When the context menu is shown, this function is called
        /// to determine whether this item should be visible.
        /// If null, the value of the Visible property is not changed.
        /// </summary>
        public Func<bool> VisibilityChecker { get; set; }

        /// <summary>The Y position of the item's text.</summary>
        public float TextY { get; set; }

        public XNAAdvancedContextMenuItem(WindowManager windowManager) : base(windowManager)
        {
            Height = 22;
        }
    }
}
