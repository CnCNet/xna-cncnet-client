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
        public string UnixURL { get; set; }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == "URL")
            {
                URL = value;
                return;
            }

            if (key == "UnixURL")
            {
                UnixURL = value;
                return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public override void OnLeftClick()
        {
            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();

            if (UnixURL.Length != 0 && osVersion == OSVersion.UNIX)
                ProcessLauncher.StartShellProcess(UnixURL);
            else if (URL.Length != 0)
                ProcessLauncher.StartShellProcess(URL);

            base.OnLeftClick();
        }
    }
}