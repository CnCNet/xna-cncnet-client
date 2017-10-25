using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    class PlayerContextMenu : XNAContextMenu
    {
        public PlayerContextMenu(WindowManager windowManager) : base(windowManager)
        {
        }

        public void Show()
        {
            Point cursorPoint = Parent.GetCursorPoint();

            ClientRectangle = new Rectangle(cursorPoint.X, cursorPoint.Y,
                Width, Height);

            // Position context menu so it never gets outside of the parent's borders

            if (Right > Parent.Width)
            {
                X = cursorPoint.X - Width;
            }

            if (Bottom > Parent.Height)
            {
                Y = cursorPoint.Y - Height;
            }

            Enable();
        }
    }
}
