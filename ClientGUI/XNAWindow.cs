using ClientCore;
using Rampastring.XNAUI.XNAControls;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using Rampastring.XNAUI;

namespace ClientGUI
{
    /// <summary>
    /// A sub-window to be displayed inside the game window.
    /// Supports easy reading of child controls' attributes from an INI file.
    /// </summary>
    public class XNAWindow : XNAPanel
    {
        private const string GENERIC_WINDOW_INI = "GenericWindow.ini";
        private const string GENERIC_WINDOW_SECTION = "GenericWindow";
        private const string EXTRA_PICTURE_BOXES = "ExtraPictureBoxes";
        private const string EXTRA_CONTROLS = "ExtraControls";

        public XNAWindow(WindowManager windowManager) : base(windowManager)
        {
        }

        private static readonly ClientGUICreator guiCreator = new ClientGUICreator();

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

            List<string> extraControls = iniFile.GetSectionKeys(EXTRA_CONTROLS);

            if (extraControls != null)
            {
                foreach (string key in extraControls)
                {
                    string line = iniFile.GetStringValue(EXTRA_CONTROLS, key, string.Empty);
                    string[] parts = line.Split(':');
                    if (parts.Length != 2)
                        throw new Exception("Invalid ExtraControl specified in " + Name + ": " + line);

                    Logger.Log("Line: " + line);
                    Logger.Log("Control name: " + parts[0]);
                    Logger.Log("Type: " + parts[1]);

                    if (Children.Find(child => child.Name == parts[0]) == null)
                    {
                        var control = guiCreator.CreateControl(WindowManager, parts[1]);
                        control.Name = parts[0];
                        AddChildToFirstIndex(control);
                    }
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
