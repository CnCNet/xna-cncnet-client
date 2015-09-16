using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using dtasetup.gui;

// Generic Json parser.
// Used for getting the amount of players at CnCNet.
// http://cncnet.org/live-status

namespace dtasetup.domain.cncnet
{
    public static class JsonParser
    {
        public static List<JsonValueType> Values = new List<JsonValueType>();

        public static string getValue(string name)
        {
            for (int valueId = 0; valueId < Values.Count; valueId++)
            {
                if (Values[valueId].Name == name)
                    return Values[valueId].Value;
            }

            return "";
        }

        public static void ParseLiveStatus()
        {
            Values.Clear();

            try
            {
                WebClient wb = new WebClient();
                string astr = Encoding.GetEncoding("windows-1252").GetString(wb.DownloadData("http://cncnet.org/live-status"));

                //StreamReader sw = new StreamReader(ProgramConstants.gamepath + ProgramConstants.CNCNET_STATUSFILE);
                string statusString = astr;

                int beginPoint = statusString.IndexOf("{") + 1;
                int endPoint = statusString.IndexOf("}");
                
                bool searchFinished = false;
                int currentPoint = beginPoint;
                int numValues = 1;

                while (!searchFinished)
                {
                    int index = statusString.IndexOf(",", currentPoint);
                    if (index > -1)
                    {
                        currentPoint = index + 1;
                        numValues++;
                    }
                    else
                        searchFinished = true;
                }

                currentPoint = beginPoint;

                for (int valueId = 0; valueId < numValues; valueId++)
                {
                    int stringBeginPoint = statusString.IndexOf("\"", currentPoint) + 1;
                    int stringEndPoint = statusString.IndexOf("\"", stringBeginPoint);
                    int nextCommaPoint = statusString.IndexOf(",", stringBeginPoint);

                    if (nextCommaPoint == -1)
                        nextCommaPoint = endPoint;

                    Values.Add(new JsonValueType(statusString.Substring(stringBeginPoint, stringEndPoint - stringBeginPoint)));
                    Values[Values.Count - 1].Value = statusString.Substring(stringEndPoint + 3, nextCommaPoint - (stringEndPoint + 4));

                    currentPoint = nextCommaPoint + 1;
                }
            }
            catch
            {
            }
        }
    }

    public class JsonValueType
    {
        public JsonValueType(string name)
        {
            Name = name;
        }

        public JsonValueType()
        {
        }

        string aValueName;
        string aValue;

        public string Name
        {
            get { return aValueName; }
            set { aValueName = value; }
        }

        public string Value
        {
            get { return aValue; }
            set { aValue = value; }
        }
    }
}
