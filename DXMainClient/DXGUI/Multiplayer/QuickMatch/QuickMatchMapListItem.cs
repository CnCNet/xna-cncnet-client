using System;
using System.Linq;
using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.QuickMatch
{
    public class QuickMatchMapListItem : XNAPanel
    {
        public event EventHandler LeftClickMap;

        public event EventHandler<QmSide> SideSelected;

        public readonly QmLadderMap LadderMap;
        private readonly QmLadder ladder;
        private XNAClientCheckBox cbVeto;
        private XNAClientDropDown ddSide;
        private XNAPanel panelMap;
        private XNALabel lblMap;
        private Color defaultTextColor;
        private QmSide masterQmSide;

        private XNAPanel topBorder;
        private XNAPanel bottomBorder;
        private XNAPanel rightBorder;

        private QuickMatchMapList ParentList;

        private bool selected;

        public int OpenedDownWindowBottom => GetWindowRectangle().Bottom + (ddSide.ItemHeight * ddSide.Items.Count);

        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                panelMap.BackgroundTexture = selected ? AssetLoader.CreateTexture(new Color(255, 0, 0), 1, 1) : null;
            }
        }

        public QuickMatchMapListItem(WindowManager windowManager, QmLadderMap ladderMap, QmLadder ladder) : base(windowManager)
        {
            LadderMap = ladderMap;
            this.ladder = ladder;
        }

        public override void Initialize()
        {
            base.Initialize();
            DrawBorders = false;

            topBorder = new XNAPanel(WindowManager);
            topBorder.DrawBorders = true;
            AddChild(topBorder);

            bottomBorder = new XNAPanel(WindowManager);
            bottomBorder.DrawBorders = true;
            AddChild(bottomBorder);

            rightBorder = new XNAPanel(WindowManager);
            rightBorder.DrawBorders = true;
            AddChild(rightBorder);

            cbVeto = new XNAClientCheckBox(WindowManager);
            cbVeto.CheckedChanged += CbVeto_CheckChanged;

            ddSide = new XNAClientDropDown(WindowManager);
            ddSide.DisabledMouseScroll = true;
            defaultTextColor = ddSide.TextColor;
            ddSide.SelectedIndexChanged += Side_Selected;
            AddChild(ddSide);

            panelMap = new XNAPanel(WindowManager);
            panelMap.LeftClick += Map_LeftClicked;
            panelMap.DrawBorders = false;
            AddChild(panelMap);

            lblMap = new XNALabel(WindowManager);
            lblMap.LeftClick += Map_LeftClicked;
            lblMap.ClientRectangle = new Rectangle(4, 2, panelMap.Width, panelMap.Height);
            panelMap.AddChild(lblMap);
            AddChild(cbVeto);

            InitUI();
        }

        public void SetMasterSide(QmSide qmSide)
        {
            masterQmSide = qmSide;

            if (!(ddSide?.Items.Any() ?? false))
                return;

            ddSide.SelectedIndex = masterQmSide == null ? 0 : ddSide.SelectedIndex = ddSide.Items.FindIndex(i => ((QmSide)i.Tag).Name == qmSide.Name);
        }

        public void SetParentList(QuickMatchMapList parentList) => ParentList = parentList;

        public int GetSelectedSide() => ddSide.SelectedIndex;

        public override void Draw(GameTime gameTime)
        {
            ddSide.OpenUp = OpenedDownWindowBottom > ParentList.scrollBar.GetWindowRectangle().Bottom;

            base.Draw(gameTime);
        }

        private void CbVeto_CheckChanged(object sender, EventArgs e)
        {
            ddSide.TextColor = cbVeto.Checked ? UISettings.ActiveSettings.DisabledItemColor : defaultTextColor;
            lblMap.TextColor = cbVeto.Checked ? UISettings.ActiveSettings.DisabledItemColor : defaultTextColor;
            ddSide.AllowDropDown = !cbVeto.Checked;
        }

        private void Side_Selected(object sender, EventArgs e) => SideSelected?.Invoke(this, ddSide.SelectedItem?.Tag as QmSide);

        private void Map_LeftClicked(object sender, EventArgs eventArgs) => LeftClickMap?.Invoke(this, EventArgs.Empty);

        private void InitUI()
        {
            ddSide.Items.Clear();
            foreach (int ladderMapAllowedSideId in LadderMap.AllowedSideIds)
            {
                QmSide side = ladder.Sides.FirstOrDefault(s => s.LocalId == ladderMapAllowedSideId);
                if (side == null)
                    continue;

                ddSide.AddItem(new XNADropDownItem() { Text = side.Name, Tag = side });
            }

            var randomSide = QmSide.CreateRandomSide();
            ddSide.AddItem(new XNADropDownItem() { Text = randomSide.Name, Tag = randomSide });

            if (ddSide.Items.Count > 0)
                ddSide.SelectedIndex = 0;

            if (masterQmSide != null)
                SetMasterSide(masterQmSide);

            lblMap.Text = LadderMap.Description;

            cbVeto.ClientRectangle = new Rectangle(ParentList.VetoX, 0, ParentList.VetoWidth, QuickMatchMapList.ItemHeight);
            ddSide.ClientRectangle = new Rectangle(ParentList.SidesX, 0, ParentList.SidesWidth, QuickMatchMapList.ItemHeight);
            panelMap.ClientRectangle = new Rectangle(ParentList.MapsX, 0, ParentList.MapsWidth, QuickMatchMapList.ItemHeight);

            topBorder.ClientRectangle = new Rectangle(panelMap.X, panelMap.Y, panelMap.Width, 1);
            bottomBorder.ClientRectangle = new Rectangle(panelMap.X, panelMap.Bottom, panelMap.Width, 1);
            rightBorder.ClientRectangle = new Rectangle(panelMap.Right, panelMap.Y, 1, panelMap.Height);
        }

        public bool IsVetoed() => cbVeto.Checked;

        public bool ContainsPointVertical(Point point) => Y < point.Y && Y + Height < point.Y;

        public void CloseDropDowns()
        {
            ddSide.Close();
        }
    }
}