using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using dtasetup.gui;
using dtasetup.domain;

namespace dtasetup.domain.cncnet5
{
    public static class CnCNetInfoController
    {
        public static int getCnCNetGameCount()
        {
            try
            {
                WebClient client = new WebClient();

                Stream data = client.OpenRead("http://api.cncnet.org/status");
                StreamReader reader = new StreamReader(data);
                string xml = reader.ReadToEnd();

                data.Close();
                reader.Close();

                xml = xml.Replace("{", String.Empty);
                xml = xml.Replace("}", String.Empty);
                xml = xml.Replace("\"", String.Empty);
                string[] values = xml.Split(new char[] { ',' });

                int numGames = -1;

                foreach (string value in values)
                {
                    if (value.Contains(MainClientConstants.CNCNET_LIVE_STATUS_ID))
                    {
                        numGames = Convert.ToInt32(value.Substring(MainClientConstants.CNCNET_LIVE_STATUS_ID.Length + 1));
                        return numGames;
                    }
                }

                return numGames;
            }
            catch
            {
                return -1;
            }
        }
    }
}
