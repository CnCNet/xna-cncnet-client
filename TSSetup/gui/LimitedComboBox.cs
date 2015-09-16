using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace dtasetup.gui
{
    public class LimitedComboBox : ComboBox
    {
        bool _canDropDown = true;

        public bool CanDropDown
        {
            get { return _canDropDown; }
            set { _canDropDown = value; }
        }

        /// <summary>
        /// Prevent the combobox dropdown from working
        /// http://stackoverflow.com/questions/5337834/prevent-dropdown-area-from-opening-of-combobox-control-in-windows-forms
        /// </summary>
        protected override void WndProc(ref  Message m)
        {
            if (!CanDropDown &&
               (m.Msg == 0x201 || // WM_LBUTTONDOWN
                m.Msg == 0x203)) // WM_LBUTTONDBLCLK
                return;
            base.WndProc(ref m);
        }
    }
}
