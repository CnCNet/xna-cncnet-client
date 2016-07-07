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

        public override void OnLeftClick()
        {
            if (Children.Find(c => c.IsActive) == null)
                Hide();

            base.OnLeftClick();
        }
    }
}
