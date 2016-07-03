using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Net;

namespace DTAClient.domain.Multiplayer.CnCNet
{
    public class CnCNetTunnel
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public string Country { get; set; }
        public string CountryCode { get; set; }
        public string Name { get; set; }
        public bool RequiresPassword { get; set; }
        public int Clients { get; set; }
        public int MaxClients { get; set; }
        public int PingInMs { get; set; }
        public bool Official { get; set; }
        public bool Recommended { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Version { get; set; }
        public double Distance { get; set; }

        /// <summary>
        /// Gets a list of player ports to use from a specific tunnel server.
        /// </summary>
        /// <returns>A list of player ports to use.</returns>
        public List<int> GetPlayerPortInfo(int playerCount)
        {
            try
            {
                Logger.Log("Contacting tunnel at " + Address + ":" + Port);

                string addressString = string.Format("http://{0}:{1}/request?clients={2}",
                    Address, Port, playerCount);
                Logger.Log("Downloading from " + addressString);

                WebClient client = new WebClient();
                string data = client.DownloadString(addressString);

                data = data.Replace("[", String.Empty);
                data = data.Replace("]", String.Empty);

                string[] portIDs = data.Split(new char[] { ',' });
                List<int> playerPorts = new List<int>();

                foreach (string _port in portIDs)
                {
                    playerPorts.Add(Convert.ToInt32(_port));
                    Logger.Log("Added port " + _port);
                }

                return playerPorts;
            }
            catch (Exception ex)
            {
                Logger.Log("Unable to connect to the specified tunnel server. Returned error message: " + ex.Message);
            }

            return new List<int>();
        }
    }
}
