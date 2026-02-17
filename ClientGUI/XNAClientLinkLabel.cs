using Rampastring.XNAUI;
using Rampastring.Tools;
using ClientCore;
using Rampastring.XNAUI.XNAControls;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;

namespace ClientGUI
{
    /// <summary>
    /// Link label with customizable URL and tooltip text as well as hover/click sounds.
    /// Also uses hover text color by default.
    /// </summary>
    public class XNAClientLinkLabel : XNALinkLabel, IToolTipContainer
    {
        public EnhancedSoundEffect HoverSoundEffect { get; set; }
        public EnhancedSoundEffect ClickSoundEffect { get; set; }

        private Color? _hoverColor;

        /// <summary>
        /// The color of the label when it's hovered on.
        /// </summary>
        public new Color HoverColor
        {
            get
            {
                return _hoverColor ?? UISettings.ActiveSettings.ButtonHoverColor;
            }
            set { _hoverColor = value; if (IsActive) RemapColor = value; }
        }

        public ToolTip ToolTip { get; private set; }

        private string _initialToolTipText;
        public string ToolTipText
        {
            get => Initialized ? ToolTip?.Text : _initialToolTipText;
            set
            {
                if (Initialized)
                    ToolTip.Text = value;
                else
                    _initialToolTipText = value;
            }
        }

        public XNAClientLinkLabel(WindowManager windowManager) : base(windowManager) { }

        public string URL { get; set; }
        public string UnixURL { get; set; }

        public override void Initialize()
        {
            base.Initialize();

            ToolTip = new ToolTip(WindowManager, this) { Text = _initialToolTipText };
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "ToolTip":
                    ToolTipText = value.FromIniString();
                    return;
                case "URL":
                    URL = value;
                    return;
                case "UnixURL":
                    UnixURL = value;
                    return;
                case "HoverSoundEffect":
                    HoverSoundEffect = new EnhancedSoundEffect(value);
                    return;
                case "ClickSoundEffect":
                    ClickSoundEffect = new EnhancedSoundEffect(value);
                    return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        public override void OnMouseEnter()
        {
            base.OnMouseLeave();

            HoverSoundEffect?.Play();

            RemapColor = HoverColor;
            TextColor = HoverColor;
        }

        public override void OnMouseLeave()
        {
            base.OnMouseLeave();

            RemapColor = IdleColor;
            TextColor = IdleColor;
        }

        public override void OnLeftClick(InputEventArgs inputEventArgs)
        {
            inputEventArgs.Handled = true;
            
            ClickSoundEffect?.Play();

            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();

            if (osVersion == OSVersion.UNIX && !string.IsNullOrEmpty(UnixURL))
                ProcessLauncher.StartShellProcess(UnixURL);
            else if (!string.IsNullOrEmpty(URL))
                ProcessLauncher.StartShellProcess(URL);

            base.OnLeftClick(inputEventArgs);
        }
    }
}