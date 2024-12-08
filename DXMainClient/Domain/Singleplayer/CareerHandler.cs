using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClientCore;

using Rampastring.Tools;

namespace DTAClient.Domain.Singleplayer
{
    /// <summary>
    /// Reads and writes campaign career data for tracking unlocked missions and variable states
    /// </summary>
    public static class CareerHandler
    {
        private const string SP_CAREER_FILE = "Client/spcareer.dat";
        private const string SP_CAREER_FILE_BACKUP = "Client/spcareer_backup.dat";
        private const string MISSIONS_SECTION = "Missions";
        private const string VARIABLES_SECTION = "Variables";
        private const int RANK_MIN = 1;
        private const int RANK_MAX = 3;

        // Data format:
        // [Missions]
        // GDI1A=0,0 ; isunlocked, completion_state
        //
        // [Variables]
        // Credits=0 ; int value

        // Slight modification from how DTA stores this info, writing to an INI file
        // that then gets base64-encoded to make it difficult to tamper with.

        public static void ReadCareerData(List<Mission> missions, Dictionary<string, int> variables)
        {

            Logger.Log("Loading single-player career data.");
            string filePath = ProgramConstants.GamePath + SP_CAREER_FILE;
            if (!File.Exists(filePath))
            {
                return;
            }

            string b64data = File.ReadAllText(filePath, Encoding.Unicode);
            //byte[] decoded = Convert.FromBase64String(b64data);
            // IniFile iniFile;
            // 
            // using (var memoryStream = new MemoryStream(decoded))
            // {
            //     iniFile = new IniFile(memoryStream, Encoding.UTF8);
            // }

            IniFile iniFile = new IniFile(filePath);
            var missionsSection = iniFile.GetSection(MISSIONS_SECTION);
            if (missionsSection != null)
            {
                foreach (var kvp in missionsSection.Keys)
                {
                    string missionName = kvp.Key;
                    string[] unlockAndRank = kvp.Value.Split(',');
                    if (unlockAndRank.Length != 2)
                    {
                        Logger.Log("Invalid mission clear data for mission " + missionName + ": " + kvp.Value);
                        continue;
                    }
                    bool isUnlocked = unlockAndRank[0] == "1";
                    int rank = Conversions.IntFromString(unlockAndRank[1], 0);
                    Mission mission = missions.Find(m => m.InternalName == missionName);
                    if (mission != null)
                    {
                        if (mission.RequiresUnlocking)
                            mission.IsUnlocked = isUnlocked;
                        if (rank >= RANK_MIN && rank <= RANK_MAX)
                            mission.Rank = (CompletionState)rank;
                    }
                }
            }

            var variablesSection = iniFile.GetSection(VARIABLES_SECTION);
            if (variablesSection != null)
            {
                foreach (var kvp in variablesSection.Keys)
                {
                    string name = kvp.Key;
                    int value = -1;
                    if (int.TryParse(kvp.Value, out value))
                    {
                        Logger.Log("Invalid career data for career variable " + name + ": " + kvp.Value);
                        continue;
                    }
                    
                    if (variables.ContainsKey(name))
                        variables[name] = value;
                }
            }
        }

        public static void WriteCareerData(List<Mission> missions, Dictionary<string, int> variables)
        {
            Logger.Log("Writing single-player career data.");
            try
            {
                if (File.Exists(ProgramConstants.GamePath + SP_CAREER_FILE))
                {
                    File.Copy(ProgramConstants.GamePath + SP_CAREER_FILE,
                        ProgramConstants.GamePath + SP_CAREER_FILE_BACKUP, true);
                }
            }
            catch (IOException ex)
            {
                Logger.Log("FAILED to refresh back-up of SP career data due to IOException: " + ex.Message);
                return;
            }
            IniFile careerIni = new IniFile();
            foreach (var mission in missions)
            {
                bool isUnlocked = mission.IsUnlocked;
                int rank = (int)mission.Rank;
                if ((isUnlocked && mission.RequiresUnlocking) || rank > 0)
                {
                    careerIni.SetStringValue(
                        MISSIONS_SECTION,
                        mission.InternalName,
                        $"{(isUnlocked ? "1" : "0")},{rank.ToString(CultureInfo.InvariantCulture)} ");
                }
            }
            foreach (var variable in variables)
            {
                careerIni.SetStringValue(
                    VARIABLES_SECTION,
                    variable.Key,
                    $"{variable.Value}");
            }
            careerIni.WriteIniFile(ProgramConstants.GamePath + SP_CAREER_FILE);
            Logger.Log("Completed writing single-player career data.");
        }
    }
}
