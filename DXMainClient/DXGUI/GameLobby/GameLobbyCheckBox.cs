
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
    /// A game option check box for the game lobby.
    /// </summary>
    public class GameLobbyCheckBox : DXCheckBox
    {
        public GameLobbyCheckBox(WindowManager windowManager) : base (windowManager) { }

        public string SpawnIniOption { get; set; }

        public string CustomIniPath { get; set; }

        public bool Reversed { get; set; }

        protected override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "SpawnIniOption":
                    SpawnIniOption = value;
                    return;
                case "CustomIniPath":
                    CustomIniPath = value;
                    return;
                case "Reversed":
                    Reversed = Utilities.BooleanFromString(value, false);
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }
    }
}
