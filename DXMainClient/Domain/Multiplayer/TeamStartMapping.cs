using System;
using System.Collections.Generic;
using System.Linq;
using ClientCore;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer
{
    public class TeamStartMapping
    {
        private const char LIST_SEPARATOR = ',';
        
        public const string NO_TEAM = "x";
        public const string RANDOM_TEAM = "-";
        public static readonly List<string> TEAMS = new List<string>() { NO_TEAM, RANDOM_TEAM }.Concat(ProgramConstants.TEAMS).ToList();

        [JsonProperty("t")]
        public string Team { get; set; }

        [JsonProperty("s")]
        public int Start { get; set; }

        [JsonIgnore]
        public bool IsValid => TeamIndex != -1;

        [JsonIgnore]
        public int TeamIndex => TEAMS.IndexOf(Team);

        [JsonIgnore]
        public int TeamId => ProgramConstants.TEAMS.IndexOf(Team) + 1;

        [JsonIgnore]
        public int StartingWaypoint => Start - 1;

        [JsonIgnore]
        public bool IsBlock => Team == NO_TEAM;

        /// <summary>
        /// Write these out in a delimited list.
        /// </summary>
        /// <param name="teamStartMappings"></param>
        /// <returns></returns>
        public static string ToListString(List<TeamStartMapping> teamStartMappings) 
            => string.Join(LIST_SEPARATOR.ToString(), teamStartMappings.Select(mapping => mapping.Team));

        /// <summary>
        /// This parses a list of <see cref="TeamStartMapping"/> classes that were written out as a list
        /// for either message purposes or a map INI.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static List<TeamStartMapping> FromListString(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return new List<TeamStartMapping>();
            
            var parts = str.Split(LIST_SEPARATOR);

            return parts.Select((part, index) => new TeamStartMapping()
            {
                Team = part,
                Start = index + 1
            }).ToList();
        }
    }
}
