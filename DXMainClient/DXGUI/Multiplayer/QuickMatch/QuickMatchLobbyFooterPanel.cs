using System;
using ClientGUI;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Multiplayer.QuickMatch;

public class QuickMatchLobbyFooterPanel : INItializableWindow
{
    private XNAClientButton btnQuickMatch;
    private XNAClientButton btnLogout;
    private XNAClientButton btnExit;

    public EventHandler LogoutEvent;

    public EventHandler ExitEvent;

    public EventHandler QuickMatchEvent;

    public QuickMatchLobbyFooterPanel(WindowManager windowManager) : base(windowManager)
    {
    }

    public override void Initialize()
    {
        IniNameOverride = nameof(QuickMatchLobbyFooterPanel);
        base.Initialize();

        btnLogout = FindChild<XNAClientButton>(nameof(btnLogout));
        btnLogout.LeftClick += (_, _) => LogoutEvent?.Invoke(this, null);

        btnExit = FindChild<XNAClientButton>(nameof(btnExit));
        btnExit.LeftClick += (_, _) => ExitEvent?.Invoke(this, null);

        btnQuickMatch = FindChild<XNAClientButton>(nameof(btnQuickMatch));
        btnQuickMatch.LeftClick += (_, _) => QuickMatchEvent?.Invoke(this, null);
    }
}