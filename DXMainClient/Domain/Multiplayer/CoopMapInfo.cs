#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer
{
    public class CoopMapInfo
    {
        [JsonInclude]
        public List<CoopHouseInfo> EnemyHouses = new List<CoopHouseInfo>();

        [JsonInclude]
        public List<CoopHouseInfo> AllyHouses = new List<CoopHouseInfo>();

        [JsonInclude]
        public List<int> DisallowedPlayerSides = new List<int>();

        [JsonInclude]
        public List<int> DisallowedPlayerColors = new List<int>();

        public CoopMapInfo() { }

        public void Initialize(IniSection section)
        {
            DisallowedPlayerSides = section.GetListValue("DisallowedPlayerSides", ',', int.Parse);
            DisallowedPlayerColors = section.GetListValue("DisallowedPlayerColors", ',', int.Parse);
            EnemyHouses = CoopHouseInfo.GetGenericHouseInfoList(section, "EnemyHouse");
            AllyHouses = CoopHouseInfo.GetGenericHouseInfoList(section, "AllyHouse");
        }

    }
}
