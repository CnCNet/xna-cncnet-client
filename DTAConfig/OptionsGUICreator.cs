using ClientGUI;
using DTAConfig.CustomSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAConfig
{
    /// <summary>
    /// A GUI creator that also includes DTAConfig's custom controls in addition
    /// to the controls of ClientGUI and Rampastring.XNAUI.
    /// </summary>
    internal class OptionsGUICreator : ClientGUICreator
    {
        public OptionsGUICreator()
        {
            AddControl(typeof(FileSettingCheckBox));
            AddControl(typeof(CustomSettingFileCheckBox));
            AddControl(typeof(CustomSettingFileDropDown));
        }
    }
}
