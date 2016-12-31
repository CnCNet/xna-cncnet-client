using ClientGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using Microsoft.Xna.Framework;
using DTAClient.Online;
using DTAClient.Online.Services;
using Microsoft.Xna.Framework.Graphics;
using DTAClient.Properties;
using System.IO;
using ClientCore;
using Rampastring.Tools;
using DTAClient.Online.EventArguments;
using Microsoft.Xna.Framework.Audio;
using ClientCore.CnCNet5;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    partial class ClanInvitesTab : XNAPanel
    {
        XNALabel lblIncoming;
        XNALabel lblOutgoing;
        XNAListBox lbInInvites;
        XNAListBox lbOutInvites;
        XNAClientButton btnInAccept;
        XNAClientButton btnInDecline;
        XNAClientButton btnOutDelete;
        CnCNetManager cm;

        public ClanInvitesTab(WindowManager windowManager, CnCNetManager cm,
                              Rectangle location) : base(windowManager)
        {
            ClientRectangle = location;
            this.cm = cm;
        }

        public void Refresh()
        {

        }
    }
}
