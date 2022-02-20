using System;
using System.Collections.Generic;
using System.Linq;
using ClientGUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    public class XNAAdvancedContextMenu : XNAPanel
    {
        public XNAAdvancedContextMenu(WindowManager windowManager) : base(windowManager)
        {
        }

        // protected override int DrawItem(int index, Point point)
        // {
        //     XNAContextMenuItem xnaContextMenuItem = Items[index];
        //
        //     switch (xnaContextMenuItem)
        //     {
        //         case XNAClientContextMenuDividerItem dividerItem:
        //             return DrawDivider(dividerItem, point);
        //         case XNAClientContextSubMenuItem subMenuItem:
        //             return DrawSubMenuItem(subMenuItem, index, point);
        //         default:
        //             return base.DrawItem(index, point);
        //             ;
        //     }
        // }

        // private int DrawDivider(XNAClientContextMenuDividerItem dividerItem, Point point)
        // {
        //     int lineY = dividerItem.GetLineY(point.Y);
        //     FillRectangle(new Rectangle(point.X, point.Y, Width - 2, dividerItem.Height ?? ItemHeight), BackColor);
        //     DrawLine(new Vector2(0, lineY), new Vector2(Width, lineY), BorderColor);
        //     return dividerItem.Height ?? ItemHeight;
        // }

        // private int DrawSubMenuItem(XNAClientContextSubMenuItem subMenuItem, int index, Point point)
        // {
        //     int arrowContainerSize = subMenuItem.Height ?? ItemHeight;
        //
        //     var containerRectangle = new Rectangle(Width - arrowContainerSize, point.Y, arrowContainerSize, arrowContainerSize);
        //     var topLeftVector = new Vector2(Width - arrowContainerSize + subMenuItem.ArrowGap, point.Y + subMenuItem.ArrowGap);
        //     var bottomLeftVector = new Vector2(Width - arrowContainerSize + subMenuItem.ArrowGap, point.Y + arrowContainerSize - subMenuItem.ArrowGap);
        //     var rightVector = new Vector2(Width - subMenuItem.ArrowGap, point.Y + (arrowContainerSize / 2));
        //     bool mouseInContainer = containerRectangle.Contains(GetCursorPoint());
        //
        //     DrawLine(topLeftVector, rightVector, BorderColor, subMenuItem.ArrowThickness);
        //     DrawLine(bottomLeftVector, rightVector, BorderColor, subMenuItem.ArrowThickness);
        //     DrawLine(topLeftVector, bottomLeftVector, BorderColor, subMenuItem.ArrowThickness);
        //
        //     if (mouseInContainer)
        //         FillRectangle(containerRectangle, new Color(BorderColor, 0.1f));
        //     else if (HoveredIndex == index)
        //         FillRectangle(new Rectangle(point.X, point.Y, Width - 2 - arrowContainerSize, arrowContainerSize), FocusColor);
        //     else
        //         FillRectangle(new Rectangle(point.X, point.Y, Width - 2 - arrowContainerSize, arrowContainerSize), BackColor);
        //
        //     int x1 = point.X + TextHorizontalPadding;
        //     if (subMenuItem.Texture != null)
        //     {
        //         Renderer.DrawTexture(subMenuItem.Texture, new Rectangle(point.X + 1, point.Y + 1, subMenuItem.Texture.Width, subMenuItem.Texture.Height), Color.White);
        //         x1 += subMenuItem.Texture.Width + 2;
        //     }
        //
        //     Color color = subMenuItem.Selectable ? GetItemTextColor(subMenuItem) : DisabledItemColor;
        //     DrawStringWithShadow(subMenuItem.Text, FontIndex, new Vector2(x1, point.Y + TextVerticalPadding), color);
        //     if (subMenuItem.HintText != null)
        //     {
        //         int x2 = Width - TextHorizontalPadding - (int)Renderer.GetTextDimensions(subMenuItem.HintText, HintFontIndex).X;
        //         DrawStringWithShadow(subMenuItem.HintText, HintFontIndex, new Vector2(x2, point.Y + TextVerticalPadding), color);
        //     }
        //
        //     return subMenuItem.Height ?? ItemHeight;
        // }
    }
}
