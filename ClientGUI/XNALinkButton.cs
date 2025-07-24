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
        public string Arguments { get; set; }

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

            if (key == "Arguments")
            {
                Arguments = value;
                return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public override void OnLeftClick(InputEventArgs inputEventArgs)
        {
            inputEventArgs.Handled = true;
            
            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();

            if (osVersion == OSVersion.UNIX && !string.IsNullOrEmpty(UnixURL))
                ProcessLauncher.StartShellProcess(UnixURL, Arguments);
            else if (!string.IsNullOrEmpty(URL))
                ProcessLauncher.StartShellProcess(URL, Arguments);

            base.OnLeftClick(inputEventArgs);
        }
    }
}
