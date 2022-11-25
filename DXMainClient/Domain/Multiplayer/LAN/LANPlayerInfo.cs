using ClientCore;
using Microsoft.Xna.Framework;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DTAClient.Domain.Multiplayer.LAN
{
    public class LANPlayerInfo : PlayerInfo
    {
        public LANPlayerInfo(Encoding encoding)
        {
            this.encoding = encoding;
            Port = ProgramConstants.LAN_INGAME_PORT;
        }

        public event EventHandler<NetworkMessageEventArgs> MessageReceived;
        public event EventHandler ConnectionLost;

        private const double SEND_PING_TIMEOUT = 10.0;
        private const double DROP_TIMEOUT = 20.0;

        public TimeSpan TimeSinceLastReceivedMessage { get; set; }
        public TimeSpan TimeSinceLastSentMessage { get; set; }

        public Socket TcpClient { get; private set; }

        private readonly Encoding encoding;

        private string overMessage = string.Empty;

        private CancellationTokenSource cancellationTokenSource;

        public void SetClient(Socket client)
        {
            if (TcpClient != null)
                throw new InvalidOperationException("TcpClient has already been set for this LANPlayerInfo!");

            TcpClient = client;
            TcpClient.SendTimeout = 1000;
        }

        /// <summary>
        /// Updates logic timers for the player.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        /// <returns>True if the player is still considered connected, otherwise false.</returns>
        public async Task<bool> UpdateAsync(GameTime gameTime)
        {
            try
            {
                TimeSinceLastReceivedMessage += gameTime.ElapsedGameTime;
                TimeSinceLastSentMessage += gameTime.ElapsedGameTime;

                if (TimeSinceLastSentMessage > TimeSpan.FromSeconds(SEND_PING_TIMEOUT)
                    || TimeSinceLastReceivedMessage > TimeSpan.FromSeconds(SEND_PING_TIMEOUT))
                    await SendMessageAsync("PING", cancellationTokenSource?.Token ?? default);

                if (TimeSinceLastReceivedMessage > TimeSpan.FromSeconds(DROP_TIMEOUT))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }

            return false;
        }

        public override string IPAddress
        {
            get
            {
                if (TcpClient != null)
                    return ((IPEndPoint)TcpClient.RemoteEndPoint).Address.ToString();

                return base.IPAddress;
            }

            set
            {
                base.IPAddress = value;
            }
        }

        /// <summary>
        /// Sends a message to the player over the network.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public async Task SendMessageAsync(string message, CancellationToken cancellationToken)
        {
            message += ProgramConstants.LAN_MESSAGE_SEPARATOR;

            const int charSize = sizeof(char);
            int bufferSize = message.Length * charSize;
            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(bufferSize);
            Memory<byte> buffer = memoryOwner.Memory[..bufferSize];
            int bytes = encoding.GetBytes(message.AsSpan(), buffer.Span);

            buffer = buffer[..bytes];

            try
            {
                await TcpClient.SendAsync(buffer, SocketFlags.None, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                PreStartup.LogException(ex, "Sending message to " + ToString() + " failed!");
            }

            TimeSinceLastSentMessage = TimeSpan.Zero;
        }

        public override string ToString()
            => Name + " (" + IPAddress + ")";

        /// <summary>
        /// Starts receiving messages from the player asynchronously.
        /// </summary>
        public Task StartReceiveLoopAsync(CancellationToken cancellationToken)
            => ReceiveMessagesAsync(cancellationToken);

        /// <summary>
        /// Receives messages sent by the client,
        /// and hands them over to another class via an event.
        /// </summary>
        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(1024);

                while (!cancellationToken.IsCancellationRequested)
                {
                    int bytesRead;
                    Memory<byte> message = memoryOwner.Memory[..1024];

                    try
                    {
                        bytesRead = await TcpClient.ReceiveAsync(message, SocketFlags.None, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        ConnectionLost?.Invoke(this, EventArgs.Empty);
                        break;
                    }
                    catch (Exception ex)
                    {
                        PreStartup.LogException(ex, "Socket error with client " + Name + "; removing.");
                        ConnectionLost?.Invoke(this, EventArgs.Empty);
                        break;
                    }

                    if (bytesRead > 0)
                    {
                        string msg = encoding.GetString(message.Span[..bytesRead]);

                        msg = overMessage + msg;

                        List<string> commands = new List<string>();

                        while (true)
                        {
                            int index = msg.IndexOf(ProgramConstants.LAN_MESSAGE_SEPARATOR);

                            if (index == -1)
                            {
                                overMessage = msg;
                                break;
                            }

                            commands.Add(msg.Substring(0, index));
                            msg = msg.Substring(index + 1);
                        }

                        foreach (string cmd in commands)
                        {
                            MessageReceived?.Invoke(this, new NetworkMessageEventArgs(cmd));
                        }

                        continue;
                    }

                    ConnectionLost?.Invoke(this, EventArgs.Empty);
                    break;
                }
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }
    }
}