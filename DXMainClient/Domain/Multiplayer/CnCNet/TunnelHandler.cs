using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.Extensions;
using DTAClient.Online;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;

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

        private readonly WindowManager wm;

        private TimeSpan timeSinceTunnelRefresh = TimeSpan.MaxValue;
        private uint skipCount;

        public event EventHandler TunnelsRefreshed;

        public event EventHandler CurrentTunnelPinged;

        public event Action<int> TunnelPinged;

        public TunnelHandler(WindowManager wm, CnCNetManager connectionManager)
            : base(wm.Game)
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

        private async ValueTask RefreshTunnelsAsync()
        {
            List<CnCNetTunnel> tunnels = await DoRefreshTunnelsAsync();
            wm.AddCallback(() => HandleRefreshedTunnels(tunnels));
        }

        private void HandleRefreshedTunnels(List<CnCNetTunnel> tunnels)
        {
            if (tunnels.Count > 0)
                Tunnels = tunnels;

            TunnelsRefreshed?.Invoke(this, EventArgs.Empty);

            for (int i = 0; i < Tunnels.Count; i++)
            {
                if (UserINISettings.Instance.PingUnofficialCnCNetTunnels || Tunnels[i].Official || Tunnels[i].Recommended)
                    PingListTunnelAsync(i).HandleTask();
            }

            if (CurrentTunnel != null)
            {
                CnCNetTunnel updatedTunnel = Tunnels.Find(t => t.Hash.Equals(CurrentTunnel.Hash, StringComparison.OrdinalIgnoreCase));

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
                    PingCurrentTunnel();
                }
            }
        }

        private async ValueTask PingListTunnelAsync(int index)
        {
            await Tunnels[index].UpdatePingAsync();
            DoTunnelPinged(index);
        }

        private void PingCurrentTunnel(bool checkTunnelList = false)
            => PingCurrentTunnelAsync(checkTunnelList).HandleTask();

        private async ValueTask PingCurrentTunnelAsync(bool checkTunnelList = false)
        {
            await CurrentTunnel.UpdatePingAsync();
            DoCurrentTunnelPinged();

            if (checkTunnelList)
            {
                int tunnelIndex = Tunnels.FindIndex(t => t.Hash.Equals(CurrentTunnel.Hash));

                if (tunnelIndex > -1)
                    DoTunnelPinged(tunnelIndex);
            }
        }

        /// <summary>
        /// Downloads and parses the list of CnCNet tunnels.
        /// </summary>
        /// <returns>A list of tunnel servers.</returns>
        private static async ValueTask<List<CnCNetTunnel>> DoRefreshTunnelsAsync()
        {
            FileInfo tunnelCacheFile = SafePath.GetFile(ProgramConstants.ClientUserFilesPath, "tunnel_cache");
            var returnValue = new List<CnCNetTunnel>();
            string data;

            Logger.Log("Fetching tunnel server info.");

            try
            {
                data = await Constants.CnCNetHttpClient.GetStringAsync(ProgramConstants.CNCNET_TUNNEL_LIST_URL);
            }
            catch (HttpRequestException ex)
            {
                ProgramConstants.LogException(ex, "Error when downloading tunnel server info. Retrying.");
                try
                {
                    data = await Constants.CnCNetHttpClient.GetStringAsync(ProgramConstants.CNCNET_TUNNEL_LIST_URL);
                }
                catch (HttpRequestException ex1)
                {
                    ProgramConstants.LogException(ex1);
                    if (!tunnelCacheFile.Exists)
                    {
                        Logger.Log("Tunnel cache file doesn't exist!");
                        return returnValue;
                    }

                    Logger.Log("Fetching tunnel server list failed. Using cached tunnel data.");
                    data = await File.ReadAllTextAsync(tunnelCacheFile.FullName);
                }
            }

            string[] serverList = data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            // skip the header
            foreach (string serverInfo in serverList.Skip(1))
            {
                try
                {
                    var tunnel = CnCNetTunnel.Parse(serverInfo);

                    if (tunnel == null)
                        continue;

                    if (tunnel.RequiresPassword)
                        continue;

                    if (tunnel.Version is not Constants.TUNNEL_VERSION_2 and not Constants.TUNNEL_VERSION_3)
                        continue;

                    if (tunnel.Version is Constants.TUNNEL_VERSION_2 && !UserINISettings.Instance.UseLegacyTunnels)
                        continue;

                    if (tunnel.Version is Constants.TUNNEL_VERSION_3 && UserINISettings.Instance.UseLegacyTunnels)
                        continue;

                    returnValue.Add(tunnel);
                }
                catch (Exception ex)
                {
                    ProgramConstants.LogException(ex, "Caught an exception when parsing a tunnel server.");
                }
            }

            if (!returnValue.Any())
                return returnValue;

            try
            {
                if (tunnelCacheFile.Exists)
                    tunnelCacheFile.Delete();

                DirectoryInfo clientDirectoryInfo = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath);

                if (!clientDirectoryInfo.Exists)
                    clientDirectoryInfo.Create();

                await File.WriteAllTextAsync(tunnelCacheFile.FullName, data);
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, "Refreshing tunnel cache file failed!");
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
                    RefreshTunnelsAsync().HandleTask();
                }
                else if (CurrentTunnel != null)
                {
                    PingCurrentTunnel(true);
                }

                timeSinceTunnelRefresh = TimeSpan.Zero;
                skipCount++;
            }
            else
            {
                timeSinceTunnelRefresh += gameTime.ElapsedGameTime;
            }

            base.Update(gameTime);
        }
    }
}