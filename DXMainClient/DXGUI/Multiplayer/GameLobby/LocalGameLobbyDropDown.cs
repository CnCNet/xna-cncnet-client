#nullable enable

using DTAClient.DXGUI.Generic;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

/// <summary>A persisted lobby option that is not broadcast to other players.</summary>
public class LocalGameLobbyDropDown : GameSessionDropDown
{
    public LocalGameLobbyDropDown(WindowManager windowManager) : base(windowManager) { }

    public override void Initialize()
    {
        // Register separately from broadcast game options.
        XNAControl parent = Parent;
        while (parent != null)
        {
            if (parent is GameLobbyBase gameLobby)
            {
                gameLobby.LocalDropDowns.Add(this);
                break;
            }

            parent = parent.Parent;
        }

        base.Initialize();
    }
}
