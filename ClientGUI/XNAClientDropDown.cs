#nullable enable

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using ClientCore.Extensions;

namespace ClientGUI
{
    /// <summary>
    /// A drop-down control with tooltip support.
    /// </summary>
    public class XNAClientDropDown : XNADropDown, IToolTipContainer
    {
        /// <summary>The tooltip control attached to this drop-down.</summary>
        public ToolTip ToolTip { get; private set; } = null!;

        private string _initialToolTipText = string.Empty;

        /// <summary>
        /// Gets or sets the tooltip text displayed when the mouse hovers over the control.
        /// </summary>
        public string ToolTipText
        {
            get => Initialized && ToolTip != null ? ToolTip.Text : _initialToolTipText;
            set
            {
                if (Initialized && ToolTip != null)
                    ToolTip.Text = value;
                else
                    _initialToolTipText = value;
            }
        }

        public XNAClientDropDown(WindowManager windowManager) : base(windowManager) { }

        /// <summary>
        /// Initializes the drop-down with a click sound effect and a tooltip.
        /// </summary>
        public override void Initialize()
        {
            ClickSoundEffect = new EnhancedSoundEffect("dropdown.wav");
            base.Initialize();

            ToolTip = new ToolTip(WindowManager, this) { Text = _initialToolTipText };
        }

        protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
        {
            if (key == "ToolTip")
            {
                ToolTipText = value.FromIniString();
                return;
            }

            base.ParseControlINIAttribute(iniFile, key, value);
        }

        /// <summary>
        /// Opens the dropdown and updates the tooltip's blocked state.
        /// </summary>
        public override void OnMouseLeftDown(InputEventArgs inputEventArgs)
        {
            base.OnMouseLeftDown(inputEventArgs);

            UpdateToolTipBlock();
        }

        protected override void CloseDropDown()
        {
            base.CloseDropDown();
            UpdateToolTipBlock();
        }

        protected void UpdateToolTipBlock()
        {
            if (ToolTip == null)
                return;
            ToolTip.Blocked = DropDownState != DropDownState.CLOSED;
        }
    }
}
