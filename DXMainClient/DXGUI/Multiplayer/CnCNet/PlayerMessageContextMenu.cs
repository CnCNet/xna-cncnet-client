using DTAClient.Online;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Multiplayer.CnCNet
{
    public class PlayerMessageContextMenu : PlayerContextMenu
    {
        public PlayerMessageContextMenu(
            WindowManager windowManager,
            CnCNetManager connectionManager,
            CnCNetUserData cncnetUserData,
            PrivateMessagingWindow pmWindow
        ) : base(windowManager, connectionManager, cncnetUserData, pmWindow)
        {
        }
    }
}
