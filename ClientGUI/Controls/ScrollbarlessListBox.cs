using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ClientGUI
{
    /// <summary>
    /// A listbox without a scroll bar.
    /// http://stackoverflow.com/questions/13169900/hide-vertical-scroll-bar-in-listbox-control
    /// </summary>
    public partial class ScrollbarlessListBox : ListBox
    {
        public ScrollbarlessListBox()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        public Color ListBoxFocusColor;
        public List<Color> ItemColors = new List<Color>();

        public void AddItem(string item, Color color)
        {
            ItemColors.Add(color);
            Items.Add(item);
        }

        private bool mShowScroll = false;
        protected override System.Windows.Forms.CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                if (!mShowScroll)
                    cp.Style = cp.Style & ~0x200000;
                return cp;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            base.OnPaintBackground(pevent);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            for (int Index = 0; Index < this.Items.Count; ++Index)
            {
                Rectangle rect = GetItemRectangle(Index);

                if (this.SelectedIndex == Index)
                {
                    if (Index >= ItemColors.Count || ListBoxFocusColor == null)
                        OnDrawItem(new DrawItemEventArgs(e.Graphics, this.Font,
                            rect, Index, DrawItemState.Selected, this.ForeColor, this.BackColor));
                    else
                        OnDrawItem(new DrawItemEventArgs(e.Graphics, this.Font,
                            rect, Index, DrawItemState.Selected, ItemColors[Index], ListBoxFocusColor));
                }
                else
                {
                    if (Index >= ItemColors.Count || ListBoxFocusColor == null)
                        OnDrawItem(new DrawItemEventArgs(e.Graphics, this.Font,
                            rect, Index, DrawItemState.None, this.ForeColor, this.BackColor));
                    else
                        OnDrawItem(new DrawItemEventArgs(e.Graphics, this.Font,
                            rect, Index, DrawItemState.None, ItemColors[Index], this.BackColor));
                }
            }

            base.OnPaint(e);
        }

        public bool ShowScrollbar
        {
            get { return mShowScroll; }
            set
            {
                if (value != mShowScroll)
                {
                    mShowScroll = value;
                    if (IsHandleCreated)
                        RecreateHandle();
                }
            }
        }
    }
}