using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    class IPUtilities
    {
        public static IPEndPoint GetPublicEndPoint(IPAddress serverIP, int destPort, int gamePort)
        {
            // Code by FunkyFr3sh

            using (var udpClient = new UdpClient())
            {
                udpClient.ExclusiveAddressUse = false;
                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, gamePort));

                IAsyncResult iAsyncResult = udpClient.BeginReceive(null, null);
                udpClient.Send(new byte[1], 1, new IPEndPoint(serverIP, destPort));
                iAsyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(750), false);
                if (iAsyncResult.IsCompleted)
                {
                    IPEndPoint remote = null;
                    byte[] data = udpClient.EndReceive(iAsyncResult, ref remote);
                    if (remote.Address.Equals(serverIP) && remote.Port == destPort && data.Length == 8)
                    {
                        byte[] ip = new byte[4];
                        Array.Copy(data, 4, ip, 0, 4);
                        return new IPEndPoint(new IPAddress(ip), BitConverter.ToInt32(data, 0));
                    }
                }
            }

            throw new Exception("No response from server");
        }
    }
}
