using Microsoft.Xna.Framework;
using Rampastring.XNAUI.DXControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// A game option drop-down for the game lobby.
    /// </summary>
    public class GameLobbyDropDown : XNADropDown
    {
        public GameLobbyDropDown(WindowManager windowManager) : base(windowManager) { }

        public DropDownDataWriteMode DataWriteMode { get; set; }

        public string SpawnIniOption { get; set; }

        public string OptionName { get; set; }

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
                    SelectedIndex = int.Parse(value);
                    return;
                case "OptionName":
                    OptionName = value;
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        /// <summary>
        /// Applies the drop down's associated code to spawn.ini.
        /// </summary>
        /// <param name="spawnIni">The spawn INI file.</param>
        public void ApplySpawnIniCode(IniFile spawnIni)
        {
            if (String.IsNullOrEmpty(SpawnIniOption))
            {
                Logger.Log("GameLobbyDropDown.WriteSpawnIniCode: " + Name + " has no associated spawn INI option!");
                return;
            }

            switch (DataWriteMode)
            {
                case DropDownDataWriteMode.BOOLEAN:
                    spawnIni.SetBooleanValue("Settings", SpawnIniOption, SelectedIndex > 0);
                    break;
                case DropDownDataWriteMode.INDEX:
                    spawnIni.SetIntValue("Settings", SpawnIniOption, SelectedIndex);
                    break;
                default:
                case DropDownDataWriteMode.STRING:
                    spawnIni.SetStringValue("Settings", SpawnIniOption, Items[SelectedIndex].Text);
                    break;
            }
        }
    }
}
