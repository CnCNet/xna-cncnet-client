#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

using ClientCore;
using ClientCore.Extensions;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer
{
    public abstract class GameModeMapBase
    {
        public const int MAX_PLAYERS = 8;

        /// <summary>
        /// The maximum amount of players supported by the map or a game mode (such as a 2v2 mode).
        /// </summary>
        [JsonInclude]
        public int? MaxPlayers { get; private set; }

        /// <summary>
        /// The minimum amount of players supported by the map or a game mode.
        /// </summary>
        [JsonInclude]
        public int? MinPlayers { get; private set; }

        /// <summary>
        /// Whether to use MaxPlayers for limiting the player count of the map or a game mode.
        /// If false (which is the default), MaxPlayers is only used for randomizing
        /// players to starting waypoints.
        /// </summary>
        [JsonInclude]
        public bool? EnforceMaxPlayers { get; private set; }

        /// <summary>
        /// The allowed starting locations for this map or game mode.
        /// </summary>
        [JsonInclude]
        public List<int>? AllowedStartingLocations { get; private set; }

        /// <summary>
        /// Controls if the map is meant for a co-operation game mode
        /// (enables briefing logic and forcing options, among others).
        /// </summary>
        [JsonInclude]
        public bool? IsCoop { get; private set; }

        /// <summary>
        /// Contains co-op information.
        /// </summary>
        [JsonInclude]
        public CoopMapInfo? CoopInfo { get; private set; }

        [JsonInclude]
        public int? CoopDifficultyLevel { get; set; }

        /// <summary>
        /// If set, this map cannot be played on Skirmish.
        /// </summary>
        [JsonInclude]
        public bool? MultiplayerOnly { get; private set; }

        /// <summary>
        /// If set, this map cannot be played with AI players.
        /// </summary>
        [JsonInclude]
        public bool? HumanPlayersOnly { get; private set; }

        /// <summary>
        /// If set, players are forced to random starting locations on this map.
        /// </summary>
        [JsonInclude]
        public bool? ForceRandomStartLocations { get; private set; }

        /// <summary>
        /// If set, players are forced to different teams on this map.
        /// </summary>
        [JsonInclude]
        public bool? ForceNoTeams { get; private set; }

        protected void InitializeBaseSettingsFromIniSection(IniSection section, bool isCustomMap)
        {
            // MinPlayers
            MinPlayers = section.GetIntValueOrNull(isCustomMap ? "MinPlayer" : "MinPlayers");

            // MaxPlayers
            if (isCustomMap)
                MaxPlayers = section.GetIntValueOrNull("ClientMaxPlayer") ?? section.GetIntValueOrNull("MaxPlayer");
            else
                MaxPlayers = section.GetIntValueOrNull("MaxPlayers");

            // EnforceMaxPlayers
            EnforceMaxPlayers = section.GetBooleanValueOrNull("EnforceMaxPlayers");

            // AllowedStartingLocations
            List<int>? rawAllowedStartingLocations = section.GetListValueOrNull<int>("AllowedStartingLocations", ',', int.Parse);

            if (rawAllowedStartingLocations != null && rawAllowedStartingLocations.Count > 0)
            {
                // In configuration files, the number starts from 0. While in the code, the number starts from 1.
                AllowedStartingLocations = rawAllowedStartingLocations.Select(x => x + 1).Distinct().OrderBy(x => x).ToList();

                if (AllowedStartingLocations.Max() > MAX_PLAYERS || AllowedStartingLocations.Min() <= 0)
                    throw new Exception(string.Format("Invalid AllowedStartingLocations {0}".L10N("Client:Main:InvalidAllowedStartingLocations"), string.Join(", ", rawAllowedStartingLocations)));
            }

            // IsCoop
            IsCoop = section.GetBooleanValueOrNull("IsCoopMission");

            // CoopInfo
            if (IsCoop ?? false)
            {
                CoopInfo = new CoopMapInfo();
                CoopInfo.Initialize(section);
            }

            // MultiplayerOnly
            MultiplayerOnly = section.GetBooleanValueOrNull(isCustomMap ? "ClientMultiplayerOnly" : "MultiplayerOnly");

            // HumanPlayersOnly
            HumanPlayersOnly = section.GetBooleanValueOrNull("HumanPlayersOnly");

            // ForceRandomStartLocations
            ForceRandomStartLocations = section.GetBooleanValueOrNull("ForceRandomStartLocations");

            // ForceNoTeams
            ForceNoTeams = section.GetBooleanValueOrNull("ForceNoTeams");

            // CoopDifficultyLevel
            CoopDifficultyLevel = section.GetIntValueOrNull("CoopDifficultyLevel");
        }

    }
}
