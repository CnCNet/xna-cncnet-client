using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;
using Microsoft.Xna.Framework;
using ClientGUI;

namespace DTAClient.DXGUI.Multiplayer
{
    /// <summary>
    /// A box that notifies users of new private messages.
    /// </summary>
    public class PrivateMessageNotificationBox : XNAWindow
    {
        public PrivateMessageNotificationBox(WindowManager windowManager) : base(windowManager)
        {
        }

        XNALabel lblSender;
        XNAPanel gameIconPanel;
        XNALabel lblMessage;

        public override void Initialize()
        {
            Name = "PrivateMessageNotificationBox";
            BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 196), 1, 1);
            ClientRectangle = new Rectangle(WindowManager.RenderResolutionX - 200, -120, 200, 120);

            XNALabel lblHeader = new XNALabel(WindowManager);
            lblHeader.Name = "lblHeader";
            lblHeader.FontIndex = 1;
            lblHeader.Text = "PRIVATE MESSAGE";
            AddChild(lblHeader);
            lblHeader.CenterOnParent();
            lblHeader.ClientRectangle = new Rectangle(lblHeader.ClientRectangle.X,
                6, lblHeader.ClientRectangle.Width, lblHeader.ClientRectangle.Height);

            gameIconPanel = new XNAPanel(WindowManager);
            gameIconPanel.ClientRectangle = new Rectangle(12, 30, 16, 16);
            gameIconPanel.DrawBorders = false;

            AddChild(gameIconPanel);

            XNALabel lblSender = new XNALabel(WindowManager);

            base.Initialize();
        }


    }
}
