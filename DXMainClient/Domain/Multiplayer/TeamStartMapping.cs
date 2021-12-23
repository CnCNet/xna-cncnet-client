using System;
using System.Collections.Generic;
using System.Linq;
using ClientCore;
using Newtonsoft.Json;

namespace DTAClient.Domain.Multiplayer
{
    public class TeamStartMapping
    {
        private const char SEPARATOR = ':';
        private const char LIST_SEPARATOR = ',';
        private const string INVALID_TEAM_MAPPING_ERROR = "Invalid team mapping \"{0}\"";

        [JsonProperty("t")]
        public string Team { get; set; }

        [JsonProperty("s")]
        public int Start { get; set; }

        [JsonIgnore]
        public bool IsValid => !string.IsNullOrEmpty(Team) && Start > 0;

        [JsonIgnore]
        public int TeamIndex => ProgramConstants.TEAMS.IndexOf(Team);

        [JsonIgnore]
        public int StartIndex => Start - 1;

        /// <summary>
        /// We write these out in the form A:1 where A is the team and 1 is the starting location on the map
        /// as it would appear in the map preview.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Team}{SEPARATOR}{Start}";
        }

        /// <summary>
        /// Parse the incoming string into a <see cref="TeamStartMapping"/>.
        /// This should be the exact opposite of the <see cref="ToString"/> method.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static TeamStartMapping FromString(string str)
        {
            var parts = str.Split(SEPARATOR);
            if (parts.Length < 2 ||
                !int.TryParse(parts[1], out var start))
                throw new Exception(string.Format(INVALID_TEAM_MAPPING_ERROR, str));

            return new TeamStartMapping()
            {
                Team = parts[0],
                Start = start
            };
        }

        /// <summary>
        /// Write these out in a delimited list.
        /// </summary>
        /// <param name="teamStartMappings"></param>
        /// <returns></returns>
        public static string ToListString(List<TeamStartMapping> teamStartMappings)
        {
            return string.Join(LIST_SEPARATOR.ToString(), teamStartMappings.Select(mapping => mapping.ToString()));
        }

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

            return parts.Select(FromString).ToList();
        }
    }
}
