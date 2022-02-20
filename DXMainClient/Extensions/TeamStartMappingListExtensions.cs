using System.Collections.Generic;
using System.Linq;
using DTAClient.Domain.Multiplayer;

namespace DTAClient.Extensions
{
    public static class TeamStartMappingListExtensions
    {
        public static bool EqualsMappings(this List<TeamStartMapping> teamStartMappings, List<TeamStartMapping> otherTeamStartMappings)
        {
            if (teamStartMappings.Count != otherTeamStartMappings.Count)
                return false;

            // compare team at each mapping at each index
            return !teamStartMappings.Where((t, i) => t.Team != otherTeamStartMappings[i].Team).Any();
        }
    }
}
