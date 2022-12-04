﻿using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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

        public void SetHouseInfos(IniSection iniSection)
        {
            EnemyHouses = GetGenericHouseInfo(iniSection, "EnemyHouse");
            AllyHouses = GetGenericHouseInfo(iniSection, "AllyHouse");
        }

        private List<CoopHouseInfo> GetGenericHouseInfo(IniSection iniSection, string keyName)
        {
            var houseList = new List<CoopHouseInfo>();

            for (int i = 0; ; i++)
            {
                string[] houseInfo = iniSection.GetStringValue(keyName + i, string.Empty).Split(
                    new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (houseInfo.Length == 0)
                    break;

                int[] info = Conversions.IntArrayFromStringArray(houseInfo);
                var chInfo = new CoopHouseInfo(info[0], info[1], info[2]);

                houseList.Add(new CoopHouseInfo(info[0], info[1], info[2]));
            }

            return houseList;
        }
    }
}
