using ClientGUI;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    /// <summary>
    /// A panel that hides itself if it's clicked while none of its children
    /// are the focus of input.
    /// </summary>
    public class PrivateMessagingPanel : DarkeningPanel
    {
        public PrivateMessagingPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void OnLeftClick(InputEventArgs inputEventArgs)
        {
            inputEventArgs.Handled = true;
            
            if (GetActiveChild() == null)
                Hide();

            base.OnLeftClick(inputEventArgs);
        }
    }
}
