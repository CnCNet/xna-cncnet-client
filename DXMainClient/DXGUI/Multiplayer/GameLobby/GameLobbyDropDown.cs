﻿using System;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using ClientGUI;
using ClientCore;
using DTAClient.Domain.Multiplayer;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// A game option drop-down for the game lobby.
    /// </summary>
    public class GameLobbyDropDown : XNAClientDropDown
    {
        public GameLobbyDropDown(WindowManager windowManager) : base(windowManager) { }

        public string OptionName { get; private set; }

        public int UserDefinedIndex { get; set; }

        private DropDownDataWriteMode dataWriteMode = DropDownDataWriteMode.BOOLEAN;

        private string spawnIniOption = string.Empty;

        private int defaultIndex;

        protected override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "Items":
                    string[] items = value.Split(',');
                    string[] itemlabels = iniFile.GetStringValue(Name, "ItemLabels", "").Split(',');
                    for (int i = 0; i < items.Length; i++)
                    {
                        XNADropDownItem item = new XNADropDownItem();
                        if (itemlabels.Length > i && !String.IsNullOrEmpty(itemlabels[i]))
                        {
                            item.Text = itemlabels[i];
                            item.Tag = items[i];
                        }
                        else item.Text = items[i];
                        item.TextColor = UISettings.AltColor;
                        AddItem(item);
                    }
                    return;
                case "DataWriteMode":
                    if (value.ToUpper() == "INDEX")
                        dataWriteMode = DropDownDataWriteMode.INDEX;
                    else if (value.ToUpper() == "BOOLEAN")
                        dataWriteMode = DropDownDataWriteMode.BOOLEAN;
                    else if (value.ToUpper() == "MAPCODE")
                        dataWriteMode = DropDownDataWriteMode.MAPCODE;
                    else
                        dataWriteMode = DropDownDataWriteMode.STRING;
                    return;
                case "SpawnIniOption":
                    spawnIniOption = value;
                    return;
                case "DefaultIndex":
                    SelectedIndex = int.Parse(value);
                    defaultIndex = SelectedIndex;
                    UserDefinedIndex = SelectedIndex;
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
            if (dataWriteMode == DropDownDataWriteMode.MAPCODE || SelectedIndex < 0 || SelectedIndex >= Items.Count)
                return;

            if (String.IsNullOrEmpty(spawnIniOption))
            {
                Logger.Log("GameLobbyDropDown.WriteSpawnIniCode: " + Name + " has no associated spawn INI option!");
                return;
            }

            switch (dataWriteMode)
            {
                case DropDownDataWriteMode.BOOLEAN:
                    spawnIni.SetBooleanValue("Settings", spawnIniOption, SelectedIndex > 0);
                    break;
                case DropDownDataWriteMode.INDEX:
                    spawnIni.SetIntValue("Settings", spawnIniOption, SelectedIndex);
                    break;
                default:
                case DropDownDataWriteMode.STRING:
                    if (Items[SelectedIndex].Tag != null)
                    {
                        spawnIni.SetStringValue("Settings", spawnIniOption, Items[SelectedIndex].Tag.ToString());
                    }
                    else
                    {
                        spawnIni.SetStringValue("Settings", spawnIniOption, Items[SelectedIndex].Text);
                    }
                    break;
            }

        }

        /// <summary>
        /// Applies the drop down's associated code to the map INI file.
        /// </summary>
        /// <param name="mapIni">The map INI file.</param>
        /// <param name="gameMode">Currently selected gamemode, if set.</param>
        public void ApplyMapCode(IniFile mapIni, GameMode gameMode)
        {
            if (dataWriteMode != DropDownDataWriteMode.MAPCODE || SelectedIndex < 0 || SelectedIndex >= Items.Count) return;

            string customIniPath;
            if (Items[SelectedIndex].Tag != null) customIniPath = Items[SelectedIndex].Tag.ToString();
            else customIniPath = Items[SelectedIndex].Text;

            MapCodeHelper.ApplyMapCode(mapIni, customIniPath, gameMode);
        }

    }
}
