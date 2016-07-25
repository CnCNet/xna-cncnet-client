using Rampastring.Tools;
using System;
using System.Collections.Generic;

namespace DTAClient.domain.Multiplayer
{
    public class CoopMapInfo
    {
        public List<CoopHouseInfo> EnemyHouses = new List<CoopHouseInfo>();
        public List<CoopHouseInfo> AllyHouses = new List<CoopHouseInfo>();
        public List<int> DisallowedPlayerSides = new List<int>();
        public List<int> DisallowedPlayerColors = new List<int>();

        public void SetHouseInfos(IniFile iniFile, string section)
        {
            EnemyHouses = GetGenericHouseInfo(iniFile, section, "EnemyHouse");
            AllyHouses = GetGenericHouseInfo(iniFile, section, "AllyHouse");
        }

        private List<CoopHouseInfo> GetGenericHouseInfo(IniFile iniFile, string section, string keyName)
        {
            var houseList = new List<CoopHouseInfo>();

            for (int i = 0; ; i++)
            {
                string[] houseInfo = iniFile.GetStringValue(section, keyName + i, string.Empty).Split(
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
