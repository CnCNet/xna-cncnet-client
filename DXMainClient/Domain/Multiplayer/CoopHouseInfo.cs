using Rampastring.Tools;
using System.Collections.Generic;
using System;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// Holds information about enemy houses in a co-op map.
    /// </summary>
    public struct CoopHouseInfo
    {
        public CoopHouseInfo(int side, int color, int startingLocation)
        {
            Side = side;
            Color = color;
            StartingLocation = startingLocation;
        }

        /// <summary>
        /// The index of the enemy house's side.
        /// </summary>
        public int Side;

        /// <summary>
        /// The index of the enemy house's color.
        /// </summary>
        public int Color;

        /// <summary>
        /// The starting location waypoint of the enemy house.
        /// </summary>
        public int StartingLocation;

        public static List<CoopHouseInfo> GetGenericHouseInfoList(IniSection iniSection, string keyName)
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
