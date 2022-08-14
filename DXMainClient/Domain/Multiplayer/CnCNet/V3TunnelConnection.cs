using Rampastring.Tools;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// Handles connections to version 3 CnCNet tunnel servers.
    /// </summary>
    internal sealed class V3TunnelConnection
    {
        public V3TunnelConnection(CnCNetTunnel tunnel, uint senderId)
        {
            this.tunnel = tunnel;
            SenderId = senderId;
        }

        public event EventHandler Connected;
        public event EventHandler ConnectionFailed;
        public event EventHandler ConnectionCut;

        public delegate void MessageDelegate(byte[] data, uint senderId);
        public event MessageDelegate MessageReceived;

        public uint SenderId { get; set; }

        private bool aborted;
        public bool Aborted
        {
            get { lock (locker) return aborted; }
            private set { lock (locker) aborted = value; }
        }

        private CnCNetTunnel tunnel;
        private Socket tunnelSocket;
        private EndPoint tunnelEndPoint;

        private readonly object locker = new();

        public void ConnectAsync()
        {
            Thread thread = new Thread(DoConnect);
            thread.Start();
        }

        private void DoConnect()
        {
            Logger.Log($"Attempting to establish connection to V3 tunnel server " +
                $"{tunnel.Name} ({tunnel.Address}:{tunnel.Port})");

            tunnelSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            tunnelSocket.SendTimeout = Constants.TUNNEL_CONNECTION_TIMEOUT;
            tunnelSocket.ReceiveTimeout = Constants.TUNNEL_RECEIVE_TIMEOUT;

            Logger.Log("Connection to tunnel server established. Entering receive loop.");
            Connected?.Invoke(this, EventArgs.Empty);

            ReceiveLoop();
        }

        private void WriteSenderIdToBuffer(byte[] buffer) =>
            Array.Copy(BitConverter.GetBytes(SenderId), buffer, sizeof(uint));

        private void ReceiveLoop()
        {
            try
            {
                while (true)
                {
                    if (Aborted)
                    {
                        DoClose();
                        Logger.Log("Exiting receive loop.");
                        return;
                    }

                    byte[] buffer = new byte[1024];
                    int size = tunnelSocket.ReceiveFrom(buffer, ref tunnelEndPoint);

                    if (size < 8)
                    {
                        Logger.Log("Invalid data packet from tunnel server");
                        continue;
                    }

                    byte[] data = new byte[size - 8];
                    Array.Copy(buffer, 8, data, 0, data.Length);
                    uint senderId = BitConverter.ToUInt32(buffer, 0);

                    MessageReceived?.Invoke(data, senderId);
                }
            }
            catch (SocketException ex)
            {
                Logger.Log("Socket exception in V3 tunnel receive loop: " + ex.Message);
                DoClose();
                ConnectionCut?.Invoke(this, EventArgs.Empty);
            }
        }

        public void CloseConnection()
        {
            Logger.Log("Closing connection to the tunnel server.");
            Aborted = true;
        }

        private void DoClose()
        {
            Aborted = true;

            if (tunnelSocket != null)
            {
                tunnelSocket.Close();
                tunnelSocket = null;
            }

            Logger.Log("Connection to tunnel server closed.");
        }

        public void SendData(byte[] data, uint receiverId)
        {
            byte[] packet = new byte[data.Length + 8]; // 8 = sizeof(uint) * 2
            WriteSenderIdToBuffer(packet);
            Array.Copy(BitConverter.GetBytes(receiverId), 0, packet, 4, sizeof(uint));
            Array.Copy(data, 0, packet, 8, data.Length);

            lock (locker)
            {
                if (!aborted)
                    tunnelSocket.SendTo(packet, tunnelEndPoint);
            }
        }
    }
}