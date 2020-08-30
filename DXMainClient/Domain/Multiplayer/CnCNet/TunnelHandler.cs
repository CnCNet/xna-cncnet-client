using ClientCore;
using DTAClient.Online;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    public class TunnelHandler : GameComponent
    {
        private const double TUNNEL_LIST_REFRESTH_INTERVAL = 120.0;
        private const int SUPPORTED_TUNNEL_VERSION = 2;

        public TunnelHandler(WindowManager wm, CnCNetManager connectionManager) : base(wm.Game)
        {
            this.wm = wm;
            this.connectionManager = connectionManager;

            wm.Game.Components.Add(this);

            Enabled = false;

            connectionManager.Connected += ConnectionManager_Connected;
            connectionManager.Disconnected += ConnectionManager_Disconnected;
            connectionManager.ConnectionLost += ConnectionManager_ConnectionLost;
        }

        public List<CnCNetTunnel> Tunnels = new List<CnCNetTunnel>();

        public event EventHandler TunnelsRefreshed;

        public delegate void TunnelPingedEventHandler(int tunnelIndex);
        public event TunnelPingedEventHandler TunnelPinged;

        WindowManager wm;
        CnCNetManager connectionManager;

        TimeSpan timeSinceTunnelRefresh = TimeSpan.MaxValue;

        private void DoTunnelPinged(int tunnelIndex, int pingInMs)
        {
            wm.AddCallback(new Action<int, int>(HandleTunnelPinged), tunnelIndex, pingInMs);
        }

        private void HandleTunnelPinged(int tunnelIndex, int pingInMs)
        {
            Tunnels[tunnelIndex].PingInMs = pingInMs;

            TunnelPinged?.Invoke(tunnelIndex);
        }

        private void ConnectionManager_Connected(object sender, EventArgs e)
        {
            Enabled = true;
        }

        private void ConnectionManager_ConnectionLost(object sender, Online.EventArguments.ConnectionLostEventArgs e)
        {
            Enabled = false;
        }

        private void ConnectionManager_Disconnected(object sender, EventArgs e)
        {
            Enabled = false;
        }

        private void RefreshTunnelsAsync()
        {
            Thread thread = new Thread(RefreshTunnelsMain);
            thread.Start();
        }

        private void RefreshTunnelsMain()
        {
            List<CnCNetTunnel> tunnels = RefreshTunnels();
            wm.AddCallback(new Action<List<CnCNetTunnel>>(HandleRefreshedTunnels), tunnels);
        }

        private void HandleRefreshedTunnels(List<CnCNetTunnel> tunnels)
        {
            if (tunnels.Count > 0)
                Tunnels = tunnels;

            TunnelsRefreshed?.Invoke(this, EventArgs.Empty);

            if (UserINISettings.Instance.PingUnofficialCnCNetTunnels)
                PingUnofficialTunnelsAsync(Tunnels);
        }

        /// <summary>
        /// Downloads and parses the list of CnCNet tunnels.
        /// </summary>
        /// <returns>A list of tunnel servers.</returns>
        private List<CnCNetTunnel> RefreshTunnels()
        {
            string tunnelCacheFile = ProgramConstants.GamePath + "Client/tunnel_cache";

            List<CnCNetTunnel> returnValue = new List<CnCNetTunnel>();

            WebClient client = new WebClient();

            byte[] data;

            Logger.Log("Fetching tunnel server info.");

            try
            {
                data = client.DownloadData(MainClientConstants.CNCNET_TUNNEL_LIST_URL);
            }
            catch (Exception ex)
            {
                Logger.Log("Error when downloading tunnel server info: " + ex.Message);
                Logger.Log("Retrying.");
                try
                {
                    data = client.DownloadData(MainClientConstants.CNCNET_TUNNEL_LIST_URL);
                }
                catch
                {
                    if (!File.Exists(tunnelCacheFile))
                    {
                        Logger.Log("Tunnel cache file doesn't exist!");
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

            foreach (string serverInfo in serverList)
            {
                try
                {
                    CnCNetTunnel tunnel = CnCNetTunnel.Parse(serverInfo);

                    if (tunnel == null)
                        continue;

                    if (tunnel.RequiresPassword)
                        continue;
                        
                    if (tunnel.Version != SUPPORTED_TUNNEL_VERSION)
                        continue;

                    if (tunnel.Official || tunnel.Recommended)
                    {
                        Ping p = new Ping();
                        try
                        {
                            PingReply reply = p.Send(IPAddress.Parse(tunnel.Address), 500);
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
                    if (File.Exists(tunnelCacheFile))
                        File.Delete(tunnelCacheFile);
                    if (!Directory.Exists(ProgramConstants.GamePath + "Client"))
                        Directory.CreateDirectory(ProgramConstants.GamePath + "Client");
                    File.WriteAllBytes(tunnelCacheFile, data);
                }
                catch (Exception ex)
                {
                    Logger.Log("Refreshing tunnel cache file failed! Returned error: " + ex.Message);
                }
            }

            return returnValue;
        }

        private void PingUnofficialTunnelsAsync(List<CnCNetTunnel> tunnels)
        {
            Thread thread = new Thread(new ParameterizedThreadStart(PingTunnels));
            thread.Start(tunnels);
        }

        private void PingTunnels(object _tunnels)
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

                    if (pingInMs > -1)
                        tunnel.PingInMs = pingInMs;

                    DoTunnelPinged(tnlId, pingInMs);
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (timeSinceTunnelRefresh > TimeSpan.FromSeconds(TUNNEL_LIST_REFRESTH_INTERVAL))
            {
                RefreshTunnelsAsync();
                timeSinceTunnelRefresh = TimeSpan.Zero;
            }
            else
                timeSinceTunnelRefresh += gameTime.ElapsedGameTime;

            base.Update(gameTime);
        }
    }
}
