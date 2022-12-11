using System;
using Rampastring.XNAUI;
using Rampastring.Tools;
using ClientCore;

namespace ClientGUI
{
    public class XNALinkButton : XNAClientButton
    {
        public XNALinkButton(WindowManager windowManager) : base(windowManager) { }

        public string URL { get; set; }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == "URL")
            {
                URL = value;
                return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public override void OnLeftClick()
        {
            ProcessLauncher.StartShellProcess(URL);

            base.OnLeftClick();
        }
    }
}