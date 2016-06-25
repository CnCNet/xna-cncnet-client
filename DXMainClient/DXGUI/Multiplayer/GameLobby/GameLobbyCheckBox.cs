
using Microsoft.Xna.Framework;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rampastring.Tools;
using Rampastring.XNAUI;
using ClientCore;
using Utilities = Rampastring.Tools.Utilities;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// A game option check box for the game lobby.
    /// </summary>
    public class GameLobbyCheckBox : XNACheckBox
    {
        public GameLobbyCheckBox(WindowManager windowManager) : base (windowManager) { }

        public string SpawnIniOption { get; set; }

        public string CustomIniPath { get; set; }

        public bool Reversed { get; set; }

        public bool IsMultiplayer { get; set; }

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
                    Reversed = Conversions.BooleanFromString(value, false);
                    return;
                case "CheckedMP":
                    if (IsMultiplayer)
                        Checked = Conversions.BooleanFromString(value, false);
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
            if (String.IsNullOrEmpty(SpawnIniOption))
                return;

            spawnIni.SetBooleanValue("Settings", SpawnIniOption, Checked != Reversed);
        }

        public void ApplyMapCode(IniFile mapIni)
        {
            if (Checked == Reversed || String.IsNullOrEmpty(CustomIniPath))
                return;

            IniFile associatedIni = new IniFile(ProgramConstants.GamePath + CustomIniPath);
            IniFile.ConsolidateIniFiles(mapIni, associatedIni);
        }
    }
}
