using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    class V3TunnelHandler
    {
        public string TunnelCacheFilePath { get; set; }

        private const string CNCNET_TUNNEL_LIST_URL = "http://cncnet.org/master-list";

        /// <summary>
        /// Downloads and parses the list of CnCNet tunnels.
        /// </summary>
        /// <returns>A list of tunnel servers.</returns>
        private List<CnCNetTunnel> RefreshTunnels()
        {
            List<CnCNetTunnel> returnValue = new List<CnCNetTunnel>();

            WebClient client = new WebClient();

            byte[] data;

            Logger.Log("Fetching tunnel server info.");

            try
            {
                data = client.DownloadData(CNCNET_TUNNEL_LIST_URL);
            }
            catch (WebException ex)
            {
                Logger.Log("Error when downloading tunnel server info: " + ex.Message);
                Logger.Log("Retrying.");
                try
                {
                    data = client.DownloadData(CNCNET_TUNNEL_LIST_URL);
                }
                catch (WebException)
                {
                    if (!File.Exists(TunnelCacheFilePath))
                    {
                        Logger.Log("Tunnel cache file doesn't exist!");
                        return returnValue;
                    }
                    else
                    {
                        Logger.Log("Fetching tunnel server list failed. Using cached tunnel data.");
                        data = File.ReadAllBytes(TunnelCacheFilePath);
                    }
                }
            }

            string convertedData = Encoding.Default.GetString(data);

            string[] serverList = convertedData.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string serverInfo in serverList)
            {
                try
                {
                    CnCNetTunnel tunnel = CnCNetTunnel.Parse(serverInfo);

                    if (tunnel == null)
                        continue;

                    if (tunnel.RequiresPassword)
                        continue;

                    if (tunnel.Version != Constants.TUNNEL_VERSION_2 &&
                        tunnel.Version != Constants.TUNNEL_VERSION_3)
                        continue;

                    returnValue.Add(tunnel);
                }
                catch
                {
                }
            }

            if (returnValue.Count > 0)
            {
                try
                {
                    if (File.Exists(TunnelCacheFilePath))
                        File.Delete(TunnelCacheFilePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(TunnelCacheFilePath));
                    File.WriteAllBytes(TunnelCacheFilePath, data);
                }
                catch (Exception ex)
                {
                    Logger.Log("Refreshing tunnel cache file failed! Returned error: " + ex.Message);
                }
            }

            return returnValue;
        }
    }
}
