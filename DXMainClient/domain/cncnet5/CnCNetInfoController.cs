using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using dtasetup.gui;
using dtasetup.domain;
using System.Threading;
using Rampastring.Tools;
using Rampastring.XNAUI;

namespace dtasetup.domain.cncnet5
{
    /// <summary>
    /// A class for automatic updating of the CnCNet game/player count.
    /// </summary>
    public static class CnCNetInfoController
    {
        public delegate void CnCNetGameCountUpdatedEventHandler(int gameCount);
        public static event CnCNetGameCountUpdatedEventHandler CnCNetGameCountUpdated;

        static bool ServiceDisabled = false;

        public static void InitializeService()
        {
            Logger.Log("Initializing CnCNet live status parsing.");
            ServiceDisabled = false;
            Thread thread = new Thread(RunService);
            thread.Start();
        }

        private static void RunService()
        {
            int ticks = 0;

            while (!ServiceDisabled)
            {
                if (ticks == 10 && CnCNetGameCountUpdated != null)
                {
                    CnCNetGameCountUpdated(GetCnCNetGameCount());
                    ticks = 0;
                }

                Thread.Sleep(1000);
                ticks++;
            }
        }

        public static void DisableService()
        {
            ServiceDisabled = true;
        }

        private static int GetCnCNetGameCount()
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
