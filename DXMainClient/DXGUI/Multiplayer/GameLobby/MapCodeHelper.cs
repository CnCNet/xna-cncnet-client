using System;
using ClientCore;
using DTAClient.Domain.Multiplayer;
using Rampastring.Tools;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    public static class MapCodeHelper
    {
        /// <summary>
        /// Applies code from a custom INI file to a map INI file.
        /// </summary>
        /// <param name="mapIni">The map INI file.</param>
        /// <param name="customIniPath">The custom INI file path.</param>
        /// <param name="gameMode">Currently selected gamemode, if set.</param>
        public static void ApplyMapCode(IniFile mapIni, string customIniPath, GameMode gameMode)
        {
            IniFile associatedIni = new IniFile(ProgramConstants.GamePath + customIniPath);
            string extraIniName = null;
            if (gameMode != null)
                extraIniName = associatedIni.GetStringValue("GameModeIncludes", gameMode.Name, null);
            associatedIni.EraseSectionKeys("GameModeIncludes");
            IniFile.ConsolidateIniFiles(mapIni, associatedIni);
            if (!String.IsNullOrEmpty(extraIniName))
                IniFile.ConsolidateIniFiles(mapIni, new IniFile(ProgramConstants.GamePath + extraIniName));
        }
    }
}
