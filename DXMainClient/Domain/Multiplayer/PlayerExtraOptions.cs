using Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.Domain.Multiplayer
{
    public class PlayerExtraOptions
    {
        private static string INVALID_OPTIONS_MESSAGE => "Invalid player extra options message".L10N("UI:Main:InvalidPlayerExtraOptionsMessage");
        private static string MAPPING_ERROR_PREFIX => "Auto Allying:".L10N("UI:Main:AutoAllyingPrefix");
        protected static string NOT_ALL_MAPPINGS_ASSIGNED => MAPPING_ERROR_PREFIX + " " + "You must have all mappings assigned.".L10N("UI:Main:NotAllMappingsAssigned");
        protected static string MULTIPLE_MAPPINGS_ASSIGNED_TO_SAME_START => MAPPING_ERROR_PREFIX + " " + "Multiple mappings assigned to the same start location.".L10N("UI:Main:MultipleMappingsAssigned");
        protected static string ONLY_ONE_TEAM => MAPPING_ERROR_PREFIX + " " + "You must have more than one team assigned.".L10N("UI:Main:OnlyOneTeam");
        private const char MESSAGE_SEPARATOR = ';';

        public const string CNCNET_MESSAGE_KEY = "PEO";
        public const string LAN_MESSAGE_KEY = "PEOPTS";

        public bool IsForceRandomSides { get; set; }
        public bool IsForceRandomColors { get; set; }
        public bool IsForceRandomTeams { get; set; }
        public bool IsForceRandomStarts { get; set; }
        public bool IsUseTeamStartMappings { get; set; }
        public List<TeamStartMapping> TeamStartMappings { get; set; }

        public string GetTeamMappingsError()
        {
            if (!IsUseTeamStartMappings)
                return null;

            var distinctStartLocations = TeamStartMappings.Select(m => m.Start).Distinct();
            if (distinctStartLocations.Count() != TeamStartMappings.Count)
                return MULTIPLE_MAPPINGS_ASSIGNED_TO_SAME_START; // multiple mappings are using the same spawn location

            var distinctTeams = TeamStartMappings.Select(m => m.Team).Distinct();
            if (distinctTeams.Count() < 2)
                return ONLY_ONE_TEAM; // must have more than one team assigned

            return null;
        }

        public string ToCncnetMessage() => $"{CNCNET_MESSAGE_KEY} {ToString()}";

        public string ToLanMessage() => $"{LAN_MESSAGE_KEY} {ToString()}";

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(IsForceRandomSides ? "1" : "0");
            stringBuilder.Append(IsForceRandomColors ? "1" : "0");
            stringBuilder.Append(IsForceRandomTeams ? "1" : "0");
            stringBuilder.Append(IsForceRandomStarts ? "1" : "0");
            stringBuilder.Append(IsUseTeamStartMappings ? "1" : "0");
            stringBuilder.Append(MESSAGE_SEPARATOR);
            stringBuilder.Append(TeamStartMapping.ToListString(TeamStartMappings));

            return stringBuilder.ToString();
        }

        public static PlayerExtraOptions FromMessage(string message)
        {
            var parts = message.Split(MESSAGE_SEPARATOR);
            if (parts.Length < 2)
                throw new Exception(INVALID_OPTIONS_MESSAGE);

            var boolParts = parts[0].ToCharArray();
            if (boolParts.Length < 5)
                throw new Exception(INVALID_OPTIONS_MESSAGE);

            return new PlayerExtraOptions
            {
                IsForceRandomSides = boolParts[0] == '1',
                IsForceRandomColors = boolParts[1] == '1',
                IsForceRandomTeams = boolParts[2] == '1',
                IsForceRandomStarts = boolParts[3] == '1',
                IsUseTeamStartMappings = boolParts[4] == '1',
                TeamStartMappings = TeamStartMapping.FromListString(parts[1])
            };
        }

        public bool IsDefault()
        {
            var defaultPLayerExtraOptions = new PlayerExtraOptions();
            return IsForceRandomColors == defaultPLayerExtraOptions.IsForceRandomColors &&
                   IsForceRandomStarts == defaultPLayerExtraOptions.IsForceRandomStarts &&
                   IsForceRandomTeams == defaultPLayerExtraOptions.IsForceRandomTeams &&
                   IsForceRandomSides == defaultPLayerExtraOptions.IsForceRandomSides &&
                   IsUseTeamStartMappings == defaultPLayerExtraOptions.IsUseTeamStartMappings;
        }
    }
}
