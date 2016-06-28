using ClientCore;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI.XNAControls;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Rampastring.XNAUI;

namespace ClientGUI
{
    /// <summary>
    /// A sub-window to be displayed inside the game window.
    /// Supports easy reading of child controls' attributes from an INI file.
    /// </summary>
    public class XNAWindow : XNAPanel
    {
        const string GENERIC_WINDOW_INI = "GenericWindow.ini";
        const string GENERIC_WINDOW_SECTION = "GenericWindow";
        const string EXTRA_PICTURE_BOXES = "ExtraPictureBoxes";

        public XNAWindow(WindowManager windowManager) : base(windowManager)
        {

        }

        /// <summary>
        /// The INI file that was used for theming this window.
        /// </summary>
        protected IniFile ThemeIni { get; set; }

        public override float Alpha
        {
            get
            {
                return 1.0f;
            }
        }

        protected void SetAttributesFromIni()
        {
            if (File.Exists(ProgramConstants.GetResourcePath() + Name + ".ini"))
                GetINIAttributes(new CCIniFile(ProgramConstants.GetResourcePath() + Name + ".ini"));
            else if (File.Exists(ProgramConstants.GetBaseResourcePath() + Name + ".ini"))
                GetINIAttributes(new CCIniFile(ProgramConstants.GetBaseResourcePath() + Name + ".ini"));
            else if (File.Exists(ProgramConstants.GetResourcePath() + GENERIC_WINDOW_INI))
                GetINIAttributes(new CCIniFile(ProgramConstants.GetResourcePath() + GENERIC_WINDOW_INI));
            else
                GetINIAttributes(new CCIniFile(ProgramConstants.GetBaseResourcePath() + GENERIC_WINDOW_INI));
        }

        private void GetINIAttributes(IniFile iniFile)
        {
            ThemeIni = iniFile;

            List<string> keys = iniFile.GetSectionKeys(Name);

            if (keys != null)
            {
                foreach (string key in keys)
                    ParseAttributeFromINI(iniFile, key, iniFile.GetStringValue(Name, key, String.Empty));
            }
            else
            {
                keys = iniFile.GetSectionKeys(GENERIC_WINDOW_SECTION);

                if (keys != null)
                {
                    foreach (string key in keys)
                        ParseAttributeFromINI(iniFile, key, iniFile.GetStringValue(GENERIC_WINDOW_SECTION, key, String.Empty));
                }
            }

            List<string> extraPbs = iniFile.GetSectionKeys(EXTRA_PICTURE_BOXES);

            if (extraPbs != null)
            {
                foreach (string key in extraPbs)
                {
                    XNAExtraPanel panel = new XNAExtraPanel(WindowManager);
                    panel.Name = iniFile.GetStringValue(EXTRA_PICTURE_BOXES, key, String.Empty);
                    panel.InputEnabled = false;
                    panel.DrawBorders = false;

                    if (Children.Find(child => child.Name == panel.Name) == null)
                        AddChildToFirstIndex(panel);
                }
            }

            foreach (XNAControl child in Children)
            {
                if (!(child is XNAWindow))
                    child.GetAttributes(iniFile);
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            SetAttributesFromIni();
        }
    }
}
