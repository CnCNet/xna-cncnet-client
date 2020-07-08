using ClientCore;
using DTAClient.Online;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    public class TunnelHandler : GameComponent
    {
        private const double CURRENT_TUNNEL_REFRESH_INTERVAL = 5.0;
        private const uint CYCLES_PER_TUNNEL_LIST_REFRESH = 24;
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

        public List<CnCNetTunnel> Tunnels { get; private set; } = new List<CnCNetTunnel>();
        public CnCNetTunnel CurrentTunnel { get; set; } = null;

        public event EventHandler TunnelsRefreshed;
        public event EventHandler CurrentTunnelPinged;
        public event Action<int> TunnelPinged;

        private WindowManager wm;
        private CnCNetManager connectionManager;

        private TimeSpan timeSinceTunnelRefresh = TimeSpan.MaxValue;
        private uint skipCount = 0;

        private void DoTunnelPinged(int tunnelIndex) => 
            wm.AddCallback((Action<int>) (i => TunnelPinged?.Invoke(i)), tunnelIndex);

        private void DoCurrentTunnelPinged() =>
            CurrentTunnelPinged?.Invoke(this, EventArgs.Empty);

        private void ConnectionManager_Connected(object sender, EventArgs e) => Enabled = true;

        private void ConnectionManager_ConnectionLost(object sender, Online.EventArguments.ConnectionLostEventArgs e) => Enabled = false;

        private void ConnectionManager_Disconnected(object sender, EventArgs e) => Enabled = false;

        private void RefreshTunnelsThreaded()
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

            for (int i = 0; i < Tunnels.Count; i++)
            {
                if (UserINISettings.Instance.PingUnofficialCnCNetTunnels || Tunnels[i].Official || Tunnels[i].Recommended)
                {
                    Tunnels[i].UpdatePing();
                    DoTunnelPinged(i);
                }
            }

            if (CurrentTunnel != null)
            {
                var updatedTunnel = Tunnels.Find(t => t.Address.Equals(CurrentTunnel.Address) && t.Port.Equals(CurrentTunnel.Port));
                if (updatedTunnel != null)
                    CurrentTunnel = updatedTunnel;
                else
                    CurrentTunnel.UpdatePing();

                DoCurrentTunnelPinged();
            }

            TunnelsRefreshed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Downloads and parses the list of CnCNet tunnels.
        /// </summary>
        /// <returns>A list of tunnel servers.</returns>
        private List<CnCNetTunnel> RefreshTunnels()
        {
            string tunnelCacheFile = ProgramConstants.GamePath + "Client\\tunnel_cache";

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
                Logger.Log($"Error when downloading tunnel server info: {ex.Message}");
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

                    returnValue.Add(tunnel);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Caught an exception when parsing a tunnel server: {ex.Message}");
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
                    Logger.Log($"Refreshing tunnel cache file failed! Returned error: {ex.Message}");
                }
            }

            return returnValue;
        }

        public override void Update(GameTime gameTime)
        {
            if (timeSinceTunnelRefresh > TimeSpan.FromSeconds(CURRENT_TUNNEL_REFRESH_INTERVAL))
            {
                if (skipCount % CYCLES_PER_TUNNEL_LIST_REFRESH == 0)
                {
                    skipCount = 0;
                    RefreshTunnelsThreaded();
                }
                else if (CurrentTunnel != null)
                {
                    CurrentTunnel.UpdatePing();
                    DoCurrentTunnelPinged();

                    int tunnelIndex = Tunnels.IndexOf(CurrentTunnel);
                    if (tunnelIndex > -1)
                        DoTunnelPinged(Tunnels.IndexOf(CurrentTunnel));
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
