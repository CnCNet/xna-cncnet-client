using System.Collections.Generic;
using System.IO;
using System.Linq;

using ClientCore;

using Rampastring.Tools;

namespace DTAClient.Domain.Singleplayer
{
    /// <summary>
    /// Parses the missions in Battle.ini files in a manner as closely matching the game's parsing as possible, 
    /// which is necessary for determining the proper campaign id to give the spawner.
    /// </summary>
    public class BattleParser
    {

        // for each battle.ini file, it will first read the battles list,
        // and for each keyed value try to find a campaign,
        // or make one if it doesn't exist. and then it will iterate ALL the campaigns
        // (including those from previous files, the entire current internal list)
        // and try to read them in from their name's section

        private List<IniSection> sections;

        public BattleParser()
        {
            sections = new List<IniSection>();
        }

        public BattleParser(string path)
        {
            sections = new List<IniSection>();
            ParseBattles(path);
        }
        public bool ParseBattles(string path)
        {
            // Read ini entries, overriding duplicates
            Logger.Log("Begin parsing battles in " + path + ".");
            FileInfo iniFileInfo = SafePath.GetFile(ProgramConstants.GamePath, path);

            if (!iniFileInfo.Exists)
            {
                Logger.Log("File " + path + " not found. Ignoring.");
                return false;
            }

            var battleIni = new IniFile(iniFileInfo.FullName);

            var battles = battleIni.GetSection("Battles");

            if (battles == null)
                return false; // File exists but [Battles] doesn't

            // For each unique key...
            var keys = battles.Keys.Distinct().ToList();
            for(int i = 0; i < keys.Count; i++)
            {
                var key = keys[i];

                // ... find the last value associated with it
                var name = battles.Keys.FindLast(k => k.Key == key.Key).Value;

                var section = battleIni.GetSection(name);
                if (section == null)
                    continue; // No corresponding battle ini section exists, discard entry
                AddOrUpdate(section);
            }
            Logger.Log("Finished parsing " + path + ".");
            return true;
        }
        public List<Mission> GetMissions()
        {
            List<Mission> missions = new List<Mission>();

            for (int i = 0; i < sections.Count; i++)
            {
                missions.Add(new Mission(sections[i], i));
            }

            return missions;
        }
        private void AddOrUpdate(IniSection section)
        {
            var i = sections.FindIndex(0, sections.Count, s => s.SectionName == section.SectionName);
            if (i == -1)
            {
                Logger.Log("Adding mission" + section.SectionName + ".");
                sections.Add(section);
            }
            else
            {
                for(int j = 0; j < section.Keys.Count; j++)
                {
                    var kvp = section.Keys[j];
                    sections[i].AddOrReplaceKey(kvp.Key, kvp.Value);
                }
            }
        }
    }
}
