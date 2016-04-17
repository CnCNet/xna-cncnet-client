using Microsoft.Xna.Framework;
using Rampastring.XNAUI.DXControls;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientCore;
using Rampastring.XNAUI;
using ClientGUI;

namespace dtasetup.DXGUI
{
    public class DXGameInProgressWindow : DXWindow
    {
        public DXGameInProgressWindow(Game game) : base(game)
        {

        }

        public override void Initialize()
        {
            Name = "GameInProgressWindow";
            BackgroundTexture = AssetLoader.LoadTexture("missionselectorbg.png");
            ClientRectangle = new Rectangle(0, 0, 200, 100);
            CenterOnParent();

            DXLabel explanation = new DXLabel(Game);
            explanation.Text = "A game is in progress.";

            AddChild(explanation);

            explanation.CenterOnParent();

            base.Initialize();
        }
    }
}
