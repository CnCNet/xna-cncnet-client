using System;
using Rampastring.Tools;
using Rampastring.XNAUI;
using ClientCore;
using ClientGUI;
using System.Collections.Generic;
using DTAClient.Domain.Multiplayer;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    public enum CheckBoxMapScoringMode
    {
        /// <summary>
        /// The value of the check box makes no difference for scoring maps.
        /// </summary>
        Irrelevant = 0,

        /// <summary>
        /// The check box prevents map scoring when it's checked.
        /// </summary>
        DenyWhenChecked = 1,

        /// <summary>
        /// The check box prevents map scoring when it's unchecked.
        /// </summary>
        DenyWhenUnchecked = 2
    }

    /// <summary>
    /// A game option check box for the game lobby.
    /// </summary>
    public class GameLobbyCheckBox : XNAClientCheckBox
    {
        public GameLobbyCheckBox(WindowManager windowManager) : base (windowManager) { }

        public bool IsMultiplayer { get; set; }

        /// <summary>
        /// The last host-defined value for this check box.
        /// Defaults to the default value of Checked after the check-box
        /// has been initialized, but its value is only changed by user interaction.
        /// </summary>
        public bool HostChecked { get; set; }

        /// <summary>
        /// The last value that the local player gave for this check box.
        /// Defaults to the default value of Checked after the check-box
        /// has been initialized, but its value is only changed by user interaction.
        /// </summary>
        public bool UserChecked { get; set; }

        /// <summary>
        /// The side index that this check box disallows when checked.
        /// Defaults to -1, which means none.
        /// </summary>
        public int DisallowedSideIndex { get; set; } = -1;

        public bool AllowChanges { get; set; } = true;

        public CheckBoxMapScoringMode MapScoringMode { get; private set; } = CheckBoxMapScoringMode.Irrelevant;

        private string spawnIniOption;

        private string customIniPath;

        private bool reversed;

        private bool defaultValue;

        private string enabledSpawnIniValue = "True";
        private string disabledSpawnIniValue = "False";

        public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "SpawnIniOption":
                    spawnIniOption = value;
                    return;
                case "EnabledSpawnIniValue":
                    enabledSpawnIniValue = value;
                    return;
                case "DisabledSpawnIniValue":
                    disabledSpawnIniValue = value;
                    return;
                case "CustomIniPath":
                    customIniPath = value;
                    return;
                case "Reversed":
                    reversed = Conversions.BooleanFromString(value, false);
                    return;
                case "CheckedMP":
                    if (IsMultiplayer)
                        Checked = Conversions.BooleanFromString(value, false);
                    return;
                case "Checked":
                    bool checkedValue = Conversions.BooleanFromString(value, false);
                    Checked = checkedValue;
                    defaultValue = checkedValue;
                    HostChecked = checkedValue;
                    UserChecked = checkedValue;
                    return;
                case "DisallowedSideIndex":
                    DisallowedSideIndex = Conversions.IntFromString(value, DisallowedSideIndex);
                    return;
                case "MapScoringMode":
                    MapScoringMode = (CheckBoxMapScoringMode)Enum.Parse(typeof(CheckBoxMapScoringMode), value);
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        /// <summary>
        /// Applies the check-box's associated code to the spawn INI file.
        /// </summary>
        /// <param name="spawnIni">The spawn INI file.</param>
        public void ApplySpawnINICode(IniFile spawnIni)
        {
            if (string.IsNullOrEmpty(spawnIniOption))
                return;

            string value = disabledSpawnIniValue;
            if (Checked != reversed)
            {
                value = enabledSpawnIniValue;
            }

            spawnIni.SetStringValue("Settings", spawnIniOption, value);
        }

        /// <summary>
        /// Applies the check-box's associated code to the map INI file.
        /// </summary>
        /// <param name="mapIni">The map INI file.</param>
        /// <param name="gameMode">Currently selected gamemode, if set.</param>
        public void ApplyMapCode(IniFile mapIni, GameMode gameMode)
        {
            if (Checked == reversed || String.IsNullOrEmpty(customIniPath))
                return;

            MapCodeHelper.ApplyMapCode(mapIni, customIniPath, gameMode);
        }

        /// <summary>
        /// Applies the check-box's disallowed side index to a bool
        /// array that determines which sides are disabled.
        /// </summary>
        /// <param name="disallowedArray">An array that determines which sides are disabled.</param>
        public void ApplyDisallowedSideIndex(bool[] disallowedArray)
        {
            if (DisallowedSideIndex < 0)
                return;

            if (Checked != reversed)
                disallowedArray[DisallowedSideIndex] = true;
        }

        public override void OnLeftClick()
        {
            if (!AllowChanges)
                return;

            base.OnLeftClick();
            UserChecked = Checked;
        }
    }
}
