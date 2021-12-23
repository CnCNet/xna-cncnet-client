using Rampastring.Tools;
using System.Collections.Generic;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// A helper class for setting up alliances in spawn.ini.
    /// </summary>
    public static class AllianceHolder
    {
        public static void WriteInfoToSpawnIni(
            List<PlayerInfo> players,
            List<PlayerInfo> aiPlayers, 
            List<int> multiCmbIndexes,
            List<PlayerHouseInfo> playerHouseInfos,
            List<TeamStartMapping> teamStartMappings,
            IniFile spawnIni
        )
        {
            List<int> team1MultiMemberIds = new List<int>();
            List<int> team2MultiMemberIds = new List<int>();
            List<int> team3MultiMemberIds = new List<int>();
            List<int> team4MultiMemberIds = new List<int>();

            for (int pId = 0; pId < players.Count; pId++)
            {
                var phi = playerHouseInfos[pId];
                int teamId = players[pId].TeamId;
                if (teamId <= 0)
                    teamId = teamStartMappings?.Find(sa => sa.StartingWaypoint == phi.StartingWaypoint)?.TeamId ?? 0;

                if (teamId > 0)
                {
                    switch (teamId)
                    {
                        case 1:
                            team1MultiMemberIds.Add(multiCmbIndexes.FindIndex(c => c == pId) + 1);
                            break;
                        case 2:
                            team2MultiMemberIds.Add(multiCmbIndexes.FindIndex(c => c == pId) + 1);
                            break;
                        case 3:
                            team3MultiMemberIds.Add(multiCmbIndexes.FindIndex(c => c == pId) + 1);
                            break;
                        case 4:
                            team4MultiMemberIds.Add(multiCmbIndexes.FindIndex(c => c == pId) + 1);
                            break;
                    }
                }
            }

            int multiId = multiCmbIndexes.Count + 1;

            for (int aiId = 0; aiId < aiPlayers.Count; aiId++)
            {
                var phi = playerHouseInfos[multiCmbIndexes.Count + aiId];
                int teamId = aiPlayers[aiId].TeamId;
                if (teamId <= 0)
                    teamId = teamStartMappings?.Find(sa => sa.StartingWaypoint == phi.StartingWaypoint)?.TeamId ?? 0;


                if (teamId > 0)
                {
                    switch (teamId)
                    {
                        case 1:
                            team1MultiMemberIds.Add(multiId);
                            break;
                        case 2:
                            team2MultiMemberIds.Add(multiId);
                            break;
                        case 3:
                            team3MultiMemberIds.Add(multiId);
                            break;
                        case 4:
                            team4MultiMemberIds.Add(multiId);
                            break;
                    }
                }

                multiId++;
            }

            WriteAlliances(team1MultiMemberIds, spawnIni);
            WriteAlliances(team2MultiMemberIds, spawnIni);
            WriteAlliances(team3MultiMemberIds, spawnIni);
            WriteAlliances(team4MultiMemberIds, spawnIni);
        }

        private static void WriteAlliances(List<int> teamHouseMemberIds, IniFile spawnIni)
        {
            foreach (int houseId in teamHouseMemberIds)
            {
                bool selfFound = false;

                for (int allyId = 0; allyId < teamHouseMemberIds.Count; allyId++)
                {
                    int allyHouseId = teamHouseMemberIds[allyId];

                    if (allyHouseId == houseId)
                        selfFound = true;
                    else
                    {
                        spawnIni.SetIntValue("Multi" + houseId + "_Alliances",
                            "HouseAlly" + GetHouseAllyIndexString(allyId, selfFound), allyHouseId - 1);
                    }
                }
            }
        }

        private static string GetHouseAllyIndexString(int allyId, bool selfFound)
        {
            if (selfFound)
                allyId = allyId - 1;

            switch (allyId)
            {
                case 0:
                    return "One";
                case 1:
                    return "Two";
                case 2:
                    return "Three";
                case 3:
                    return "Four";
                case 4:
                    return "Five";
                case 5:
                    return "Six";
                case 6:
                    return "Seven";
            }

            return "None" + allyId;
        }
    }
}
