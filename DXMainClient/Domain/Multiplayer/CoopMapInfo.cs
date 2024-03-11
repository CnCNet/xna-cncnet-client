using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer;

public class CoopMapInfo
{
    [JsonInclude]
    public List<CoopHouseInfo> EnemyHouses = [];

    [JsonInclude]
    public List<CoopHouseInfo> AllyHouses = [];

    [JsonInclude]
    public List<int> DisallowedPlayerSides = [];

    [JsonInclude]
    public List<int> DisallowedPlayerColors = [];

    public void SetHouseInfos(IniSection iniSection)
    {
        EnemyHouses = GetGenericHouseInfo(iniSection, "EnemyHouse");
        AllyHouses = GetGenericHouseInfo(iniSection, "AllyHouse");
    }

    private List<CoopHouseInfo> GetGenericHouseInfo(IniSection iniSection, string keyName)
    {
        List<CoopHouseInfo> houseList = [];

        for (int i = 0; ; i++)
        {
            string[] houseInfo = iniSection.GetStringValue(keyName + i, string.Empty).Split(
                new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (houseInfo.Length == 0)
            {
                break;
            }

            int[] info = Conversions.IntArrayFromStringArray(houseInfo);
            _ = new CoopHouseInfo(info[0], info[1], info[2]);

            houseList.Add(new CoopHouseInfo(info[0], info[1], info[2]));
        }

        return houseList;
    }
}