using ClientGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;

namespace DTAClient.DXGUI.Multiplayer
{
    /// <summary>
    /// A panel that hides itself if it's clicked while none of its children
    /// are the focus of input.
    /// </summary>
    public class PrivateMessagingPanel : DarkeningPanel
    {
        public PrivateMessagingPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void OnLeftClick()
        {
            if (Children.Find(c => c.IsActive) == null)
                Hide();

            base.OnLeftClick();
        }
    }
}
