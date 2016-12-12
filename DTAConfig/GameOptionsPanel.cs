using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientCore;
using Rampastring.XNAUI;
using ClientGUI;
using Rampastring.XNAUI.XNAControls;
using Microsoft.Xna.Framework;

namespace DTAConfig
{
    class GameOptionsPanel : XNAOptionsPanel
    {
        public GameOptionsPanel(WindowManager windowManager, UserINISettings iniSettings)
            : base(windowManager, iniSettings)
        {
        }

        XNALabel lblScrollRateValue;

        XNATrackbar trbScrollRate;
        XNAClientCheckBox chkTargetLines;
        XNAClientCheckBox chkScrollCoasting;
        XNAClientCheckBox chkTooltips;
#if YR
        XNAClientCheckBox chkShowHiddenObjects;
#endif

        XNATextBox tbPlayerName;

        public override void Initialize()
        {
            base.Initialize();

            Name = "GameOptionsPanel";

            var lblScrollRate = new XNALabel(WindowManager);
            lblScrollRate.Name = "lblScrollRate";
            lblScrollRate.ClientRectangle = new Rectangle(12,
                14, 0, 0);
            lblScrollRate.Text = "Scroll Rate:";

            lblScrollRateValue = new XNALabel(WindowManager);
            lblScrollRateValue.Name = "lblScrollRateValue";
            lblScrollRateValue.FontIndex = 1;
            lblScrollRateValue.Text = "3";
            lblScrollRateValue.ClientRectangle = new Rectangle(
                ClientRectangle.Width - lblScrollRateValue.ClientRectangle.Width - 12,
                lblScrollRate.ClientRectangle.Y, 0, 0);

            trbScrollRate = new XNATrackbar(WindowManager);
            trbScrollRate.Name = "trbClientVolume";
            trbScrollRate.ClientRectangle = new Rectangle(
                lblScrollRate.ClientRectangle.Right + 32,
                lblScrollRate.ClientRectangle.Y - 2,
                lblScrollRateValue.ClientRectangle.X - lblScrollRate.ClientRectangle.Right - 47,
                22);
            trbScrollRate.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            trbScrollRate.MinValue = 0;
            trbScrollRate.MaxValue = 6;
            trbScrollRate.ValueChanged += TrbScrollRate_ValueChanged;

            chkScrollCoasting = new XNAClientCheckBox(WindowManager);
            chkScrollCoasting.Name = "chkScrollCoasting";
            chkScrollCoasting.ClientRectangle = new Rectangle(
                lblScrollRate.ClientRectangle.X,
                trbScrollRate.ClientRectangle.Bottom + 20, 0, 0);
            chkScrollCoasting.Text = "Scroll Coasting";

            chkTargetLines = new XNAClientCheckBox(WindowManager);
            chkTargetLines.Name = "chkTargetLines";
            chkTargetLines.ClientRectangle = new Rectangle(
                lblScrollRate.ClientRectangle.X,
                chkScrollCoasting.ClientRectangle.Bottom + 24, 0, 0);
            chkTargetLines.Text = "Target Lines";

            chkTooltips = new XNAClientCheckBox(WindowManager);
            chkTooltips.Name = "chkTooltips";

#if YR
            chkShowHiddenObjects = new XNAClientCheckBox(WindowManager);
            chkShowHiddenObjects.Name = "chkShowHiddenObjects";
            chkShowHiddenObjects.ClientRectangle = new Rectangle(
                lblScrollRate.ClientRectangle.X,
                chkTargetLines.ClientRectangle.Bottom + 24, 0, 0);
            chkShowHiddenObjects.Text = "Show Hidden Objects";

            chkTooltips.ClientRectangle = new Rectangle(
                lblScrollRate.ClientRectangle.X,
                chkShowHiddenObjects.ClientRectangle.Bottom + 24, 0, 0);

            AddChild(chkShowHiddenObjects);
#else
            chkTooltips.ClientRectangle = new Rectangle(
                lblScrollRate.ClientRectangle.X,
                chkTargetLines.ClientRectangle.Bottom + 24, 0, 0);
#endif



            chkTooltips.Text = "Tooltips";

            var lblPlayerName = new XNALabel(WindowManager);
            lblPlayerName.Name = "lblPlayerName";
            lblPlayerName.ClientRectangle = new Rectangle(
                lblScrollRate.ClientRectangle.X,
                chkTooltips.ClientRectangle.Bottom + 30, 0, 0);
            lblPlayerName.Text = "Player Name*:";

            tbPlayerName = new XNATextBox(WindowManager);
            tbPlayerName.Name = "tbPlayerName";
#if YR
            tbPlayerName.MaximumTextLength = 12;
#else
            tbPlayerName.MaximumTextLength = 16;
#endif
            tbPlayerName.ClientRectangle = new Rectangle(trbScrollRate.ClientRectangle.X,
                lblPlayerName.ClientRectangle.Y - 2, 200, 19);
            tbPlayerName.Text = ProgramConstants.PLAYERNAME;

            var lblNotice = new XNALabel(WindowManager);
            lblNotice.Name = "lblNotice";
            lblNotice.ClientRectangle = new Rectangle(lblPlayerName.ClientRectangle.X,
                lblPlayerName.ClientRectangle.Bottom + 30, 0, 0);
            lblNotice.Text = "* If you are currently connected to CnCNet, you need to restart the client" + 
                Environment.NewLine + "for changes to take effect.";

            AddChild(lblScrollRate);
            AddChild(lblScrollRateValue);
            AddChild(trbScrollRate);
            AddChild(chkScrollCoasting);
            AddChild(chkTargetLines);
            AddChild(chkTooltips);
            AddChild(lblPlayerName);
            AddChild(tbPlayerName);
            AddChild(lblNotice);
        }

        private void TrbScrollRate_ValueChanged(object sender, EventArgs e)
        {
            lblScrollRateValue.Text = trbScrollRate.Value.ToString();
        }

        public override void Load()
        {
            int scrollRate = ReverseScrollRate(IniSettings.ScrollRate);

            if (scrollRate >= trbScrollRate.MinValue && scrollRate <= trbScrollRate.MaxValue)
            {
                trbScrollRate.Value = scrollRate;
                lblScrollRateValue.Text = scrollRate.ToString();
            }

            chkScrollCoasting.Checked = !Convert.ToBoolean(IniSettings.ScrollCoasting);
            chkTargetLines.Checked = IniSettings.TargetLines;
            chkTooltips.Checked = IniSettings.Tooltips;
#if YR
            chkShowHiddenObjects.Checked = IniSettings.ShowHiddenObjects;
#endif
            tbPlayerName.Text = ProgramConstants.PLAYERNAME;
        }

        public override bool Save()
        {
            IniSettings.ScrollRate.Value = ReverseScrollRate(trbScrollRate.Value);

            IniSettings.ScrollCoasting.Value = Convert.ToInt32(!chkScrollCoasting.Checked);
            IniSettings.TargetLines.Value = chkTargetLines.Checked;
            IniSettings.Tooltips.Value = chkTooltips.Checked;

#if YR
            IniSettings.ShowHiddenObjects.Value = chkShowHiddenObjects.Checked;
#endif

            string playerName = tbPlayerName.Text;
            playerName = playerName.Replace(",", string.Empty);
            playerName = Renderer.GetSafeString(playerName, 0);
            playerName.Trim();

            if (playerName.Length > 0)
                IniSettings.PlayerName.Value = tbPlayerName.Text;

            return false;
        }

        private int ReverseScrollRate(int scrollRate)
        {
            switch (scrollRate)
            {
                case 0:
                    scrollRate = 6;
                    break;
                case 1:
                    scrollRate = 5;
                    break;
                case 2:
                    scrollRate = 4;
                    break;
                case 3:
                    scrollRate = 3;
                    break;
                case 4:
                    scrollRate = 2;
                    break;
                case 5:
                    scrollRate = 1;
                    break;
                case 6:
                    scrollRate = 0;
                    break;
            }

            return scrollRate;
        }
    }
}
