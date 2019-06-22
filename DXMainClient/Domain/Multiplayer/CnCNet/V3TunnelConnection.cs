using Rampastring.Tools;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    public enum ConnectionState
    {
        NotConnected = 0,
        WaitingForPassword = 1,
        WaitingForVerification = 2,
        Connected = 3
    }

    /// <summary>
    /// Handles connections to version 3 CnCNet tunnel servers.
    /// </summary>
    class V3TunnelConnection
    {
        private const int PASSWORD_REQUEST_SIZE = 512;
        private const int PASSWORD_MESSAGE_SIZE = 12;

        public V3TunnelConnection(CnCNetTunnel tunnel, uint senderId)
        {
            this.tunnel = tunnel;
            SenderId = senderId;
        }

        public event EventHandler Connected;
        public event EventHandler ConnectionFailed;

        public delegate void MessageDelegate(byte[] data, uint senderId);
        public event MessageDelegate MessageReceived;

        public uint SenderId { get; set; }

        public ConnectionState State { get; private set; }

        private bool aborted = false;
        public bool Aborted
        {
            get { lock (locker) return aborted; }
            private set { lock (locker) aborted = value; }
        }

        private CnCNetTunnel tunnel;
        private Socket tunnelSocket;
        private EndPoint tunnelEndPoint;

        private readonly object locker = new object();

        public void ConnectAsync()
        {
            Thread thread = new Thread(new ThreadStart(DoConnect));
            thread.Start();
        }

        private void DoConnect()
        {
            Logger.Log($"Attempting to establish connection to V3 tunnel server " +
                $"{tunnel.Name} ({tunnel.Address}:{tunnel.Port})");

            tunnelSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            tunnelSocket.SendTimeout = Constants.TUNNEL_CONNECTION_TIMEOUT;
            tunnelSocket.ReceiveTimeout = Constants.TUNNEL_CONNECTION_TIMEOUT;

            try
            {
                byte[] buffer = new byte[PASSWORD_REQUEST_SIZE];
                WriteSenderIdToBuffer(buffer);
                tunnelEndPoint = new IPEndPoint(tunnel.IPAddress, tunnel.Port);
                tunnelSocket.SendTo(buffer, tunnelEndPoint);
                State = ConnectionState.WaitingForPassword;
                Logger.Log("Sent ID, waiting for password.");

                buffer = new byte[PASSWORD_MESSAGE_SIZE];
                tunnelSocket.ReceiveFrom(buffer, ref tunnelEndPoint);

                byte[] password = new byte[4];
                Array.Copy(buffer, 8, password, 0, password.Length);
                Logger.Log("Password received, sending it back for verification.");

                // Echo back the password
                // <sender ID><4 bytes of anything><password>
                buffer = new byte[PASSWORD_MESSAGE_SIZE];
                WriteSenderIdToBuffer(buffer);
                Array.Copy(password, 0, buffer, 8, password.Length);
                tunnelSocket.SendTo(buffer, tunnelEndPoint);
                State = ConnectionState.Connected;

                Logger.Log("Connection to tunnel server established. Entering receive loop.");
                Connected?.Invoke(this, EventArgs.Empty);
            }
            catch (SocketException ex)
            {
                Logger.Log($"Failed to establish connection to tunnel server. Message: " + ex.Message);
                tunnelSocket.Close();
                ConnectionFailed?.Invoke(this, EventArgs.Empty);
                return;
            }

            tunnelSocket.ReceiveTimeout = Constants.TUNNEL_RECEIVE_TIMEOUT;
            ReceiveLoop();
        }

        private void WriteSenderIdToBuffer(byte[] buffer) =>
            Array.Copy(BitConverter.GetBytes(SenderId), buffer, sizeof(uint));

        private void ReceiveLoop()
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

        public void CloseConnection()
        {
            Logger.Log("Closing connection to the tunnel server.");
            Aborted = true;
        }

        private void DoClose()
        {
            if (tunnelSocket != null)
            {
                tunnelSocket.Close();
                tunnelSocket = null;
                State = ConnectionState.NotConnected;
            }

            Logger.Log("Connection to tunnel server closed.");
        }

        public void SendData(byte[] data, uint receiverId)
        {
            byte[] packet = new byte[data.Length + 8]; // 8 = sizeof(uint) * 2
            WriteSenderIdToBuffer(packet);
            Array.Copy(BitConverter.GetBytes(receiverId), 0, packet, 4, sizeof(uint));
            Array.Copy(data, 0, packet, 8, data.Length);

            tunnelSocket.SendTo(packet, tunnelEndPoint);
        }
    }
}
