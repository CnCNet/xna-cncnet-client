using Rampastring.XNAUI.XNAControls;
using System;
using Rampastring.Tools;
using Rampastring.XNAUI;
using ClientCore;
using ClientGUI;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// A game option check box for the game lobby.
    /// </summary>
    public class GameLobbyCheckBox : XNAClientCheckBox
    {
        public GameLobbyCheckBox(WindowManager windowManager) : base (windowManager) { }

        public bool IsMultiplayer { get; set; }

        public bool UserDefinedValue { get; set; }

        private string spawnIniOption;

        private string customIniPath;

        private bool reversed;

        private bool defaultValue;

        protected override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "SpawnIniOption":
                    spawnIniOption = value;
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
                    UserDefinedValue = checkedValue;
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
        }

        public void SetDefaultValue()
        {
            Checked = defaultValue;
        }

        /// <summary>
        /// Applies the check-box's associated code to the spawn INI file.
        /// </summary>
        /// <param name="spawnIni">The spawn INI file.</param>
        public void ApplySpawnINICode(IniFile spawnIni)
        {
            if (String.IsNullOrEmpty(spawnIniOption))
                return;

            spawnIni.SetBooleanValue("Settings", spawnIniOption, Checked != reversed);
        }

        public void ApplyMapCode(IniFile mapIni)
        {
            if (Checked == reversed || String.IsNullOrEmpty(customIniPath))
                return;

            IniFile associatedIni = new IniFile(ProgramConstants.GamePath + customIniPath);
            IniFile.ConsolidateIniFiles(mapIni, associatedIni);
        }
    }
}
