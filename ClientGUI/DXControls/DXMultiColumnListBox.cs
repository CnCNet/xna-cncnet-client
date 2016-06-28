using ClientCore;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientGUI.DXControls
{
    /// <summary>
    /// A list box with multiple columns.
    /// </summary>
    public class DXMultiColumnListBox : DXPanel
    {
        public DXMultiColumnListBox(Game game) : base(game)
        {

        }

        public delegate void SelectedIndexChangedEventHandler(object sender, EventArgs e);
        public event SelectedIndexChangedEventHandler SelectedIndexChanged;

        int _headerFontIndex = 1;
        public int HeaderFontIndex
        {
            get { return _headerFontIndex; }
            set { _headerFontIndex = value; }
        }

        public int FontIndex { get; set; }

        List<ListBoxColumn> columns = new List<ListBoxColumn>();
        List<DXListBox> listBoxes = new List<DXListBox>();

        bool handleSelectedIndexChanged = true;

        public int SelectedIndex
        {
            get
            {
                return listBoxes[0].SelectedIndex;
            }
            set
            {
                if (handleSelectedIndexChanged)
                {
                    foreach (DXListBox lb in listBoxes)
                        lb.SelectedIndex = value;
                }
            }

        }

        public void AddColumn(string header, int width)
        {
            columns.Add(new ListBoxColumn(header, width));
        }

        public override void Initialize()
        {
            base.Initialize();

            int w = 0;
            foreach (ListBoxColumn column in columns)
            {
                DXLabel header = new DXLabel(Game);
                header.FontIndex = HeaderFontIndex;
                header.ClientRectangle = new Rectangle(3, 2, 0, 0);
                header.Text = column.Header;

                DXPanel headerPanel = new DXPanel(Game);

                AddChild(headerPanel);
                headerPanel.AddChild(header);

                headerPanel.ClientRectangle = new Rectangle(w, 0, column.Width, header.ClientRectangle.Height + 3);

                DXListBox listBox = new DXListBox(Game);
                listBox.FontIndex = FontIndex;
                listBox.ClientRectangle = new Rectangle(w, headerPanel.ClientRectangle.Bottom - 1, column.Width + 2, this.ClientRectangle.Height - headerPanel.ClientRectangle.Bottom + 1);
                listBox.DrawBorders = false;
                listBox.TopIndexChanged += ListBox_TopIndexChanged;
                listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
                listBox.TextBorderDistance = 5;

                listBoxes.Add(listBox);

                AddChild(listBox);

                w += column.Width;
            }

            DXListBox lb = listBoxes[listBoxes.Count - 1];
            lb.ClientRectangle = new Rectangle(lb.ClientRectangle.X, lb.ClientRectangle.Y, lb.ClientRectangle.Width - 2, lb.ClientRectangle.Height);
        }

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!handleSelectedIndexChanged)
                return;

            handleSelectedIndexChanged = false;

            DXListBox lbSender = (DXListBox)sender;

            foreach (DXListBox lb in listBoxes)
            {
                lb.SelectedIndex = lbSender.SelectedIndex;
            }

            SelectedIndex = lbSender.SelectedIndex;

            if (SelectedIndexChanged != null)
                SelectedIndexChanged(this, EventArgs.Empty);

            handleSelectedIndexChanged = true;
        }

        private void ListBox_TopIndexChanged(object sender, EventArgs e)
        {
            foreach (DXListBox lb in listBoxes)
                lb.TopIndex = ((DXListBox)sender).TopIndex;
        }

        public void ClearItems()
        {
            foreach (DXListBox lb in listBoxes)
                lb.Clear();
        }

        public void AddItem(List<string> info, bool selectable)
        {
            if (info.Count != listBoxes.Count)
                throw new Exception("DXMultiColumnListBox.AddItem: Invalid amount of info for added item!");

            for (int i = 0; i < info.Count; i++)
            {
                listBoxes[i].AddItem(info[i], selectable);
            }
        }

        public void AddItem(string[] info, bool selectable)
        {
            if (info.Length != listBoxes.Count)
                throw new Exception("DXMultiColumnListBox.AddItem: Invalid amount of info for added item!");

            for (int i = 0; i < info.Length; i++)
            {
                listBoxes[i].AddItem(info[i], selectable);
            }
        }
    }

    class ListBoxColumn
    {
        public ListBoxColumn(string header, int width)
        {
            Header = header;
            Width = width;
        }

        public string Header { get; set; }

        public int Width { get; set; }
    }
}
