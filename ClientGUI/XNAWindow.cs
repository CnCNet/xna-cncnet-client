﻿using ClientCore;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using Rampastring.XNAUI;

namespace ClientGUI
{
    /// <summary>
    /// A sub-window to be displayed inside the game window.
    /// Supports easy reading of child controls' attributes from an INI file.
    /// </summary>
    public class XNAWindow : XNAWindowBase
    {
        private const string GENERIC_WINDOW_INI = "GenericWindow.ini";
        private const string GENERIC_WINDOW_SECTION = "GenericWindow";
        private const string EXTRA_CONTROLS = "ExtraControls";

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

        protected virtual void SetAttributesFromIni()
        {
            if (SafePath.GetFile(ProgramConstants.GetResourcePath(), FormattableString.Invariant($"{Name}.ini")).Exists)
                GetINIAttributes(new CCIniFile(SafePath.CombineFilePath(ProgramConstants.GetResourcePath(), FormattableString.Invariant($"{Name}.ini"))));
            else if (SafePath.GetFile(ProgramConstants.GetBaseResourcePath(), FormattableString.Invariant($"{Name}.ini")).Exists)
                GetINIAttributes(new CCIniFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), FormattableString.Invariant($"{Name}.ini"))));
            else if (SafePath.GetFile(ProgramConstants.GetResourcePath(), GENERIC_WINDOW_INI).Exists)
                GetINIAttributes(new CCIniFile(SafePath.CombineFilePath(ProgramConstants.GetResourcePath(), GENERIC_WINDOW_INI)));
            else
                GetINIAttributes(new CCIniFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), GENERIC_WINDOW_INI)));
        }

        /// <summary>
        /// Reads this window's attributes from an INI file.
        /// </summary>
        protected virtual void GetINIAttributes(IniFile iniFile)
        {
            ThemeIni = iniFile;

            List<string> keys = iniFile.GetSectionKeys(Name);

            if (keys != null)
            {
                foreach (string key in keys)
                    ParseINIAttribute(iniFile, key, iniFile.GetStringValue(Name, key, String.Empty));
            }
            else
            {
                keys = iniFile.GetSectionKeys(GENERIC_WINDOW_SECTION);

                if (keys != null)
                {
                    foreach (string key in keys)
                        ParseINIAttribute(iniFile, key, iniFile.GetStringValue(GENERIC_WINDOW_SECTION, key, String.Empty));
                }
            }

            ParseExtraControls(iniFile, EXTRA_CONTROLS);
            ReadChildControlAttributes(iniFile);
        }

        public override void Initialize()
        {
            base.Initialize();

            SetAttributesFromIni();
        }
    }
}
