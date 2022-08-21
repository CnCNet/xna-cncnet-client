using ClientCore;
using DTAClient.Online;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    internal sealed class TunnelHandler : GameComponent
    {
        /// <summary>
        /// Determines the time between pinging the current tunnel (if it's set).
        /// </summary>
        private const double CURRENT_TUNNEL_PING_INTERVAL = 20.0;

        /// <summary>
        /// A reciprocal to the value which determines how frequent the full tunnel
        /// refresh would be done instead of just pinging the current tunnel (1/N of 
        /// current tunnel ping refreshes would be substituted by a full list refresh).
        /// Multiply by <see cref="CURRENT_TUNNEL_PING_INTERVAL"/> to get the interval 
        /// between full list refreshes.
        /// </summary>
        private const uint CYCLES_PER_TUNNEL_LIST_REFRESH = 6;

        public TunnelHandler(WindowManager wm, CnCNetManager connectionManager) : base(wm.Game)
        {
            this.wm = wm;

            wm.Game.Components.Add(this);

            Enabled = false;

            connectionManager.Connected += ConnectionManager_Connected;
            connectionManager.Disconnected += ConnectionManager_Disconnected;
            connectionManager.ConnectionLost += ConnectionManager_ConnectionLost;
        }

        public List<CnCNetTunnel> Tunnels { get; private set; } = new();
        public CnCNetTunnel CurrentTunnel { get; set; }

        public event EventHandler TunnelsRefreshed;
        public event EventHandler CurrentTunnelPinged;
        public event Action<int> TunnelPinged;

        private readonly WindowManager wm;

        private TimeSpan timeSinceTunnelRefresh = TimeSpan.MaxValue;
        private uint skipCount;

        private void DoTunnelPinged(int index)
        {
            if (TunnelPinged != null)
                wm.AddCallback(() => TunnelPinged(index));
        }

        private void DoCurrentTunnelPinged()
        {
            if (CurrentTunnelPinged != null)
                wm.AddCallback(() => CurrentTunnelPinged(this, EventArgs.Empty));
        }

        private void ConnectionManager_Connected(object sender, EventArgs e) => Enabled = true;

        private void ConnectionManager_ConnectionLost(object sender, Online.EventArguments.ConnectionLostEventArgs e) => Enabled = false;

        private void ConnectionManager_Disconnected(object sender, EventArgs e) => Enabled = false;

        private async Task RefreshTunnelsAsync()
        {
            try
            {
                List<CnCNetTunnel> tunnels = await DoRefreshTunnelsAsync();
                wm.AddCallback(() => HandleRefreshedTunnels(tunnels));
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }

        private void HandleRefreshedTunnels(List<CnCNetTunnel> tunnels)
        {
            if (tunnels.Count > 0)
                Tunnels = tunnels;

            TunnelsRefreshed?.Invoke(this, EventArgs.Empty);

            for (int i = 0; i < Tunnels.Count; i++)
            {
                if (UserINISettings.Instance.PingUnofficialCnCNetTunnels || Tunnels[i].Official || Tunnels[i].Recommended)
                    PingListTunnelAsync(i);
            }

            if (CurrentTunnel != null)
            {
                var updatedTunnel = Tunnels.Find(t => t.Address == CurrentTunnel.Address && t.Port == CurrentTunnel.Port);
                if (updatedTunnel != null)
                {
                    // don't re-ping if the tunnel still exists in list, just update the tunnel instance and
                    // fire the event handler (the tunnel was already pinged when traversing the tunnel list)
                    CurrentTunnel = updatedTunnel;
                    DoCurrentTunnelPinged();
                }
                else
                {
                    // tunnel is not in the list anymore so it's not updated with a list instance and pinged
                    PingCurrentTunnelAsync();
                }
            }
        }

        private async Task PingListTunnelAsync(int index)
        {
            try
            {
                await Tunnels[index].UpdatePingAsync();
                DoTunnelPinged(index);
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }

        private async Task PingCurrentTunnelAsync(bool checkTunnelList = false)
        {
            try
            {
                await CurrentTunnel.UpdatePingAsync();
                DoCurrentTunnelPinged();

                if (checkTunnelList)
                {
                    int tunnelIndex = Tunnels.FindIndex(t => t.Address == CurrentTunnel.Address && t.Port == CurrentTunnel.Port);
                    if (tunnelIndex > -1)
                        DoTunnelPinged(tunnelIndex);
                }
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }

        /// <summary>
        /// Downloads and parses the list of CnCNet tunnels.
        /// </summary>
        /// <returns>A list of tunnel servers.</returns>
        private async Task<List<CnCNetTunnel>> DoRefreshTunnelsAsync()
        {
            FileInfo tunnelCacheFile = SafePath.GetFile(ProgramConstants.ClientUserFilesPath, "tunnel_cache");

            List<CnCNetTunnel> returnValue = new List<CnCNetTunnel>();
            var httpClientHandler = new HttpClientHandler
            {
#if NETFRAMEWORK
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
#else
                AutomaticDecompression = DecompressionMethods.All
#endif
            };
            using var client = new HttpClient(httpClientHandler, true)
            {
                Timeout = TimeSpan.FromSeconds(100)
            };

            string data;

            Logger.Log("Fetching tunnel server info.");

            try
            {
                data = await client.GetStringAsync(MainClientConstants.CNCNET_TUNNEL_LIST_URL);
            }
            catch (HttpRequestException ex)
            {
                PreStartup.LogException(ex, "Error when downloading tunnel server info. Retrying.");
                try
                {
                    data = await client.GetStringAsync(MainClientConstants.CNCNET_TUNNEL_LIST_URL);
                }
                catch (HttpRequestException ex1)
                {
                    PreStartup.LogException(ex1);
                    if (!tunnelCacheFile.Exists)
                    {
                        Logger.Log("Tunnel cache file doesn't exist!");
                        return returnValue;
                    }

                    Logger.Log("Fetching tunnel server list failed. Using cached tunnel data.");
#if NETFRAMEWORK
                    data = File.ReadAllText(tunnelCacheFile.FullName);
#else
                    data = await File.ReadAllTextAsync(tunnelCacheFile.FullName);
#endif
                }
            }

            string[] serverList = data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            // skip first header item ("address;country;countrycode;name;password;clients;maxclients;official;latitude;longitude;version;distance")
            foreach (string serverInfo in serverList.Skip(1))
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
                catch (Exception ex)
                {
                    PreStartup.LogException(ex, "Caught an exception when parsing a tunnel server.");
                }
            }

            if (returnValue.Count > 0)
            {
                try
                {
                    if (tunnelCacheFile.Exists)
                        tunnelCacheFile.Delete();

                    DirectoryInfo clientDirectoryInfo = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath);

                    if (!clientDirectoryInfo.Exists)
                        clientDirectoryInfo.Create();

#if NETFRAMEWORK
                    File.WriteAllText(tunnelCacheFile.FullName, data);
#else
                    await File.WriteAllTextAsync(tunnelCacheFile.FullName, data);
#endif
                }
                catch (Exception ex)
                {
                    PreStartup.LogException(ex, "Refreshing tunnel cache file failed!");
                }
            }

            return returnValue;
        }

        public override void Update(GameTime gameTime)
        {
            if (timeSinceTunnelRefresh > TimeSpan.FromSeconds(CURRENT_TUNNEL_PING_INTERVAL))
            {
                if (skipCount % CYCLES_PER_TUNNEL_LIST_REFRESH == 0)
                {
                    skipCount = 0;
                    RefreshTunnelsAsync();
                }
                else if (CurrentTunnel != null)
                {
                    PingCurrentTunnelAsync(true);
                }

                timeSinceTunnelRefresh = TimeSpan.Zero;
                skipCount++;
            }
            else
                timeSinceTunnelRefresh += gameTime.ElapsedGameTime;

            base.Update(gameTime);
        }
    }
}