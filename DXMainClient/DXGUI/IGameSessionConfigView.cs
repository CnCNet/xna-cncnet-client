using System.Collections.Generic;

using DTAClient.DXGUI.Generic;

namespace DTAClient.DXGUI;

public interface IGameSessionConfigView
{
    List<GameLobbyCheckBox> CheckBoxes { get; }
    List<GameLobbyDropDown> DropDowns { get; }
}