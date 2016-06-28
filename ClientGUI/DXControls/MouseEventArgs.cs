using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientGUI.DXControls
{
    public class MouseEventArgs : EventArgs
    {
        public MouseEventArgs(Point relativeLocation)
        {
            RelativeLocation = relativeLocation;
        }

        /// <summary>
        /// The point of the mouse cursor relative to the control.
        /// </summary>
        public Point RelativeLocation { get; set; }
    }
}
