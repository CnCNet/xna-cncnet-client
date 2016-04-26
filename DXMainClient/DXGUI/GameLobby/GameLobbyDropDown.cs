using Microsoft.Xna.Framework;
using Rampastring.XNAUI.DXControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.GameLobby
{
    /// <summary>
    /// A game option drop-down for the game lobby.
    /// </summary>
    class GameLobbyDropDown : DXDropDown
    {
        public GameLobbyDropDown(Game game, WindowManager windowManager) : base(game, windowManager) { }

        public DropDownDataWriteMode DataWriteMode { get; set; }

        public string SpawnIniOption { get; set; }

        protected override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "Items":
                    string[] items = value.Split(',');
                    foreach (string itemValue in items)
                        AddItem(itemValue);
                    return;
                case "DataWriteMode":
                    if (value.ToUpper() == "INDEX")
                        DataWriteMode = DropDownDataWriteMode.INDEX;
                    else if (value.ToUpper() == "BOOLEAN")
                        DataWriteMode = DropDownDataWriteMode.BOOLEAN;
                    else
                        DataWriteMode = DropDownDataWriteMode.STRING;
                    return;
                case "SpawnIniOption":
                    SpawnIniOption = value;
                    return;
                case "DefaultIndex":
                    SelectedIndex = Int32.Parse(value);
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }
    }
}
