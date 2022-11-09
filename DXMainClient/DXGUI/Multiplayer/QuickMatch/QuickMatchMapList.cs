using System;
using System.Collections.Generic;
using System.Linq;
using ClientGUI;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Events;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Services;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.QuickMatch
{
    public class QuickMatchMapList : INItializableWindow
    {
        private const int MouseScrollRate = 6;
        public const int ItemHeight = 22;

        public event EventHandler<QmLadderMap> MapSelectedEvent;

        public event EventHandler<IEnumerable<int>> MapSideSelectedEvent;

        private XNALabel lblVeto;
        private XNALabel lblSides;
        private XNALabel lblMaps;
        private XNAScrollablePanel mapListPanel;
        private readonly QmService qmService;

        public XNAScrollBar scrollBar { get; private set; }

        private QmSide masterQmSide { get; set; }

        public int VetoX => lblVeto?.X ?? 0;

        public int VetoWidth => lblVeto?.Width ?? 0;

        public int SidesX => lblSides?.X ?? 0;

        public int SidesWidth => lblSides?.Width ?? 0;

        public int MapsX => lblMaps?.X ?? 0;

        public int MapsWidth => lblMaps?.Width ?? 0;

        public QuickMatchMapList(WindowManager windowManager) : base(windowManager)
        {
            qmService = QmService.GetInstance();
        }

        public override void Initialize()
        {
            base.Initialize();

            lblVeto = FindChild<XNALabel>(nameof(lblVeto));
            lblSides = FindChild<XNALabel>(nameof(lblSides));
            lblMaps = FindChild<XNALabel>(nameof(lblMaps));
            scrollBar = FindChild<XNAScrollBar>(nameof(scrollBar));
            mapListPanel = FindChild<XNAScrollablePanel>(nameof(mapListPanel));

            MouseScrolled += OnMouseScrolled;

            qmService.QmEvent += HandleQmEvent;
        }

        public void AddItems(IEnumerable<QuickMatchMapListItem> listItems)
        {
            foreach (QuickMatchMapListItem quickMatchMapListItem in listItems)
                AddItem(quickMatchMapListItem);
        }

        public override void Draw(GameTime gameTime)
        {
            var children = MapItemChildren.ToList();
            scrollBar.Length = children.Count * ItemHeight;
            scrollBar.DisplayedPixelCount = mapListPanel.Height - 4;
            scrollBar.Refresh();
            for (int i = 0; i < children.Count; i++)
                children[i].ClientRectangle = new Rectangle(0, (i * ItemHeight) - scrollBar.ViewTop, Width - scrollBar.ScrollWidth, ItemHeight);

            base.Draw(gameTime);
        }

        public void SetMasterSide(QmSide qmSide)
        {
            masterQmSide = qmSide;
            foreach (QuickMatchMapListItem quickMatchMapListItem in MapItemChildren)
                quickMatchMapListItem.SetMasterSide(masterQmSide);
        }

        private void HandleQmEvent(object sender, QmEvent qmEvent)
        {
            switch (qmEvent)
            {
                case QmMasterSideSelected e:
                    SetMasterSide(e.Side);
                    return;
            }
        }

        private void OnMouseScrolled(object sender, EventArgs e)
        {
            int viewTop = GetNewScrollBarViewTop();
            if (viewTop == scrollBar.ViewTop)
                return;

            scrollBar.ViewTop = viewTop;

            foreach (QuickMatchMapListItem quickMatchMapListItem in MapItemChildren.ToList())
            {
                quickMatchMapListItem.CloseDropDowns();
            }
        }

        private void AddItem(QuickMatchMapListItem listItem)
        {
            listItem.LeftClickMap += MapItem_LeftClick;
            listItem.SideSelected += (_, _) => MapSideSelected();
            listItem.SetParentList(this);
            listItem.SetMasterSide(masterQmSide);
            mapListPanel.AddChild(listItem);
        }

        private void MapSideSelected() 
            => MapSideSelectedEvent?.Invoke(this, MapItemChildren.Select(c => c.GetSelectedSide()));

        private int GetNewScrollBarViewTop()
        {
            int scrollWheelValue = Cursor.ScrollWheelValue;
            int viewTop = scrollBar.ViewTop - (scrollWheelValue * MouseScrollRate);
            int maxViewTop = scrollBar.Length - scrollBar.DisplayedPixelCount;

            if (viewTop < 0)
                viewTop = 0;
            else if (viewTop > maxViewTop)
                viewTop = maxViewTop;

            return viewTop;
        }

        private void MapItem_LeftClick(object sender, EventArgs eventArgs)
        {
            var selectedItem = sender as QuickMatchMapListItem;
            foreach (QuickMatchMapListItem quickMatchMapItem in MapItemChildren)
                quickMatchMapItem.Selected = quickMatchMapItem == selectedItem;

            MapSelectedEvent?.Invoke(this, selectedItem?.LadderMap);
        }

        public void Clear()
        {
            foreach (QuickMatchMapListItem child in MapItemChildren.ToList())
                mapListPanel.RemoveChild(child);
        }

        private IEnumerable<QuickMatchMapListItem> MapItemChildren
            => mapListPanel.Children.Select(c => c as QuickMatchMapListItem).Where(i => i != null);
    }
}