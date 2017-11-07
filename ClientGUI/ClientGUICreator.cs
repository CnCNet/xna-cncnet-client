using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientGUI
{
    /// <summary>
    /// A GUI creator that also includes ClientGUI's custom controls in addition
    /// to the controls of Rampastring.XNAUI.
    /// </summary>
    public class ClientGUICreator : GUICreator
    {
        public ClientGUICreator()
        {
            AddControl(typeof(XNAClientButton));
            AddControl(typeof(XNAClientCheckBox));
            AddControl(typeof(XNAClientDropDown));
            AddControl(typeof(XNALinkButton));
            AddControl(typeof(XNAExtraPanel));
        }
    }
}
