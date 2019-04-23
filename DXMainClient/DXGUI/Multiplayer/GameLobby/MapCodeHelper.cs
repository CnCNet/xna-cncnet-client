using System;
using System.Collections.Generic;
using System.Linq;
using ClientCore;
using DTAClient.Domain.Multiplayer;
using Rampastring.Tools;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    public static class MapCodeHelper
    {
        /// <summary>
        /// Applies code from a component custom INI file to a map INI file.
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
            ApplyMapCode(mapIni, associatedIni);
            if (!String.IsNullOrEmpty(extraIniName))
                ApplyMapCode(mapIni, new IniFile(ProgramConstants.GamePath + extraIniName));
        }

        /// <summary>
        /// Apply map code from an arbitrary INI file to a map INI file.
        /// </summary>
        /// <param name="mapIni">The map INI file.</param>
        /// <param name="mapCodeIni">The INI file to apply to map INI file.</param>
        public static void ApplyMapCode(IniFile mapIni, IniFile mapCodeIni)
        {
            ReplaceMapObjects(mapIni, mapCodeIni, "Aircraft");
            ReplaceMapObjects(mapIni, mapCodeIni, "Infantry");
            ReplaceMapObjects(mapIni, mapCodeIni, "Units");
            ReplaceMapObjects(mapIni, mapCodeIni, "Structures");
            ReplaceMapObjects(mapIni, mapCodeIni, "Terrain");
            IniFile.ConsolidateIniFiles(mapIni, mapCodeIni);
        }

        /// <summary>
        /// Replace all instances of objects defined in specific map section that match ID's with new object ID's.
        /// </summary>
        /// <param name="mapIni">The map INI file.</param>
        /// <param name="mapCodeIni">The INI file to apply to map INI file.</param>
        /// <param name="sectionName">The object section ID.</param>
        private static void ReplaceMapObjects(IniFile mapIni, IniFile mapCodeIni, string sectionName)
        {
            string replaceSectionName = "ReplaceMap" + sectionName;

            List<KeyValuePair<string, string>> objectRemapPairs = GetKeyValuePairs(mapCodeIni, replaceSectionName);
            if (objectRemapPairs.Count < 1) return;

            List<KeyValuePair<string, string>> sectionKeyValuePairs = GetKeyValuePairs(mapIni, sectionName);

            foreach (KeyValuePair<string, string> objectRemapPair in objectRemapPairs)
            {
                List<KeyValuePair<string, string>> matchingSectionKVPs =
                    sectionKeyValuePairs.Where(x => GetObjectID(x.Value, sectionName) == objectRemapPair.Key).ToList();

                foreach (KeyValuePair<string, string> matchingSectionKVP in matchingSectionKVPs)
                {
                    string id = GetObjectID(matchingSectionKVP.Value, sectionName);

                    if (!String.IsNullOrEmpty(objectRemapPair.Value))
                    {
                        mapIni.SetStringValue(sectionName, matchingSectionKVP.Key, matchingSectionKVP.Value.Replace(id, objectRemapPair.Value));
                        Logger.Log("MapCodeHelper: Changed an instance of '" + sectionName + "' object '" + id + "' into '" + objectRemapPair.Value + "'.");
                    }
                    else
                    {
                        mapIni.SetStringValue(sectionName, matchingSectionKVP.Key, "");
                        Logger.Log("MapCodeHelper: Removed an instance of '" + sectionName + "' object '" + id + "'.");
                    }
                }
            }

            mapCodeIni.EraseSectionKeys(replaceSectionName);
        }

        /// <summary>
        /// Get object ID from an object section value.
        /// </summary>
        /// <param name="value">Object section value.</param>
        /// <param name="sectionName">Section ID.</param>
        /// <returns></returns>
        private static string GetObjectID(string value, string sectionName)
        {
            if (sectionName != "Terrain")
            {
                string[] splitValue = value.Split(',');
                if (splitValue.Length < 2) return "N/A";
                else return splitValue[1];
            }
            else
                return value;
        }

        /// <summary>
        /// Get key/value pairs from ini file section.
        /// </summary>
        /// <param name="iniFile">Ini file.</param>
        /// <param name="sectionName">Ini file section.</param>
        /// <returns>List of key/value pairs from the chosen ini file section. If ini file section has no keys, an empty list is returned.</returns>
        private static List<KeyValuePair<string, string>> GetKeyValuePairs(IniFile iniFile, string sectionName)
        {
            IniSection section = iniFile.GetSection(sectionName);
            if (section == null)
                return new List<KeyValuePair<string, string>>();
            return section.Keys;
        }
    }
}
