using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Net.NetworkInformation;
using System.Globalization;
using Rampastring.Tools;

namespace ClientCore.CnCNet5
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

        public delegate void TunnelPingedEventHandler(int tunnelId, int pingInMs, int clients, int maxclients, string tunnelName);
        public static event TunnelPingedEventHandler TunnelPinged;

        public static void DoTunnelPinged(int tunnelId, int pingInMs, int clients, int maxclients, string tunnelName)
        {
            if (TunnelPinged != null)
                TunnelPinged(tunnelId, pingInMs, clients, maxclients, tunnelName);
        }

        /// <summary>
        /// Downloads and parses the list of CnCNet tunnels.
        /// </summary>
        /// <param name="pingOfficialTunnels">True if official tunnel servers should be pinged by this method.</param>
        /// <returns>A list of tunnel servers.</returns>
        public static List<CnCNetTunnel> GetTunnels(bool pingOfficialTunnels)
        {
            List<CnCNetTunnel> returnValue = new List<CnCNetTunnel>();

            string tunnelCacheFile = ProgramConstants.GamePath + "tunnel_cache";

            WebClient client = new WebClient();

            byte[] data;

            try
            {
                data = client.DownloadData("http://cncnet.org/master-list");
            }
            catch (Exception ex)
            {
                Logger.Log("Error when downloading tunnel server info: " + ex.Message);
                Logger.Log("Retrying.");
                try
                {
                    data = client.DownloadData("http://cncnet.org/master-list");
                }
                catch
                {
                    if (!File.Exists(tunnelCacheFile))
                    {
                        Logger.Log("Error occured again; proceeding with empty tunnel list.");
                        return returnValue;
                    }
                    else
                    {
                        Logger.Log("Fetching tunnel server list failed. Using cached tunnel data.");
                        data = File.ReadAllBytes(tunnelCacheFile);
                    }
                }
            }

            string convertedData = Encoding.Default.GetString(data);

            string[] serverList = convertedData.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string servernfo in serverList)
            {
                Logger.Log("Parsing serverinfo: " + servernfo);

                string[] serverInfo = servernfo.Split(new char[] { ';' });

                CnCNetTunnel tunnel = new CnCNetTunnel();

                try
                {
                    string address = serverInfo[0];
                    string[] detailedAddress = address.Split(new char[] { ':' });
                    tunnel.Address = detailedAddress[0];
                    tunnel.Port = Convert.ToInt32(detailedAddress[1]);

                    tunnel.Country = serverInfo[1];
                    tunnel.CountryCode = serverInfo[2];
                    tunnel.Name = serverInfo[3];
                    tunnel.RequiresPassword = Convert.ToBoolean(Convert.ToInt32(serverInfo[4]));
                    tunnel.Clients = Convert.ToInt32(serverInfo[5]);
                    tunnel.MaxClients = Convert.ToInt32(serverInfo[6]);
                    tunnel.Official = (Convert.ToInt32(serverInfo[7]) == 2);
                    if (!tunnel.Official)
                        tunnel.Recommended = (Convert.ToInt32(serverInfo[7]) == 1);
                    tunnel.Latitude = Convert.ToDouble(serverInfo[8], CultureInfo.GetCultureInfo("en-US"));
                    tunnel.Longitude = Convert.ToDouble(serverInfo[9], CultureInfo.GetCultureInfo("en-US"));
                    tunnel.Version = Convert.ToInt32(serverInfo[10]);
                    tunnel.Distance = Convert.ToDouble(serverInfo[11], CultureInfo.GetCultureInfo("en-US"));
                    tunnel.PingInMs = -1;

                    if (tunnel.Official && pingOfficialTunnels)
                    {
                        Ping p = new Ping();
                        try
                        {
                            PingReply reply = p.Send(IPAddress.Parse(detailedAddress[0]), 500);
                            if (reply.Status == IPStatus.Success)
                            {
                                if (reply.RoundtripTime > 0)
                                    tunnel.PingInMs = Convert.ToInt32(reply.RoundtripTime);
                            }
                        }
                        catch
                        {
                        }
                    }

                    Logger.Log("ServerInfo: " + tunnel.Address + " " + tunnel.Port + " " +
                        tunnel.Official + " " + tunnel.RequiresPassword);

                    if (!tunnel.RequiresPassword)
                        returnValue.Add(tunnel);
                }
                catch
                {
                }
            }

            if (returnValue.Count > 0)
            {
                File.Delete(tunnelCacheFile);
                File.WriteAllBytes(tunnelCacheFile, data);
            }

            return returnValue;
        }

        public static void PingTunnels(object _tunnels)
        {
            List<CnCNetTunnel> Tunnels = (List<CnCNetTunnel>)_tunnels;
            for (int tnlId = 0; tnlId < Tunnels.Count; tnlId++)
            {
                CnCNetTunnel tunnel = Tunnels[tnlId];
                if (!tunnel.Official)
                {
                    int pingInMs = -1;
                    Ping p = new Ping();
                    try
                    {
                        PingReply reply = p.Send(IPAddress.Parse(tunnel.Address), 500);
                        if (reply.Status == IPStatus.Success)
                        {
                            if (reply.RoundtripTime > 0)
                                pingInMs = Convert.ToInt32(reply.RoundtripTime);
                        }
                    }
                    catch
                    {
                    }

                    DoTunnelPinged(tnlId, pingInMs, tunnel.Clients, tunnel.MaxClients, tunnel.Name);
                }
            }
        }


    }
}
