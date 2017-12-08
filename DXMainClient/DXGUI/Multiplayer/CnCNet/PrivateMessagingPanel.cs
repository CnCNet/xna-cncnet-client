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
            bool hideControl = true;

            foreach (var child in Children)
            {
                if (child.IsActive)
                {
                    hideControl = false;
                    break;
                }
            }

            if (hideControl)
                Hide();

            base.OnLeftClick();
        }


    }
}
