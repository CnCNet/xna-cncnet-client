// @author Rampastring

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.NetworkInformation;
using Rampastring.Tools;

namespace ClientCore
{
    /// <summary>
    /// A class that makes it easy to asynchronously ping a server.
    /// </summary>
    public static class Pinger
    {
        public delegate void ServerPingedEventHandler(int pingInMs);

        public static event ServerPingedEventHandler OnServerPinged;

        /// <summary>
        /// Starts a new thread to ping an IP address.
        /// Subscribe to the OnServerPinged event to receive the ping info.
        /// </summary>
        /// <param name="ipAddress">The IP address that will be pinged.</param>
        public static void PingServer(string ipAddress)
        {
            Logger.Log("Pinging server at " + ipAddress);

            Thread thread = new Thread(new ParameterizedThreadStart(HandlePing));
            thread.Start(ipAddress);
        }

        /// <summary>
        /// Pings the specified server and then calls the OnServerPinged event.
        /// </summary>
        /// <param name="ipAddress">The IP address that will be pinged.</param>
        private static void HandlePing(object ipAddress)
        {
            Ping ping = new Ping();

            string ip = (string)ipAddress;

            int p = -1;

            try
            {
                PingReply reply = ping.Send(IPAddress.Parse(ip), 1000);
                if (reply.Status == IPStatus.Success)
                {
                    p = (int)reply.RoundtripTime;
                }
            }
            catch (PingException ex)
            {
                Logger.Log("Pinging server " + ip + " failed. Message: " + ex.Message);
            }

            if (OnServerPinged != null)
                OnServerPinged(p);
        }
    }
}
