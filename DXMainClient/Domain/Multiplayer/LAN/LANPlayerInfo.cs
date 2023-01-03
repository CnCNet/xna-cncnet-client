using ClientCore;
using Microsoft.Xna.Framework;
using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DTAClient.Domain.Multiplayer.LAN
{
    internal sealed class LANPlayerInfo : PlayerInfo
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
        private const int SEND_TIMEOUT = 1000;

        public TimeSpan TimeSinceLastReceivedMessage { get; set; }
        public TimeSpan TimeSinceLastSentMessage { get; set; }

        public Socket TcpClient { get; private set; }

        private readonly Encoding encoding;

        private string overMessage = string.Empty;

        public void SetClient(Socket client)
        {
            if (TcpClient != null)
                throw new InvalidOperationException("TcpClient has already been set for this LANPlayerInfo!");

            TcpClient = client;
        }

        /// <summary>
        /// Updates logic timers for the player.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        /// <returns>True if the player is still considered connected, otherwise false.</returns>
        public async Task<bool> UpdateAsync(GameTime gameTime)
        {
            TimeSinceLastReceivedMessage += gameTime.ElapsedGameTime;
            TimeSinceLastSentMessage += gameTime.ElapsedGameTime;

            if (TimeSinceLastSentMessage > TimeSpan.FromSeconds(SEND_PING_TIMEOUT)
                || TimeSinceLastReceivedMessage > TimeSpan.FromSeconds(SEND_PING_TIMEOUT))
                await SendMessageAsync(LANCommands.PING, default).ConfigureAwait(false);

            if (TimeSinceLastReceivedMessage > TimeSpan.FromSeconds(DROP_TIMEOUT))
                return false;

            return true;
        }

        public override IPAddress IPAddress
        {
            get
            {
                if (TcpClient != null)
                    return ((IPEndPoint)TcpClient.RemoteEndPoint).Address.MapToIPv4();

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
        public async ValueTask SendMessageAsync(string message, CancellationToken cancellationToken)
        {
            message += ProgramConstants.LAN_MESSAGE_SEPARATOR;

            const int charSize = sizeof(char);
            int bufferSize = message.Length * charSize;
            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(bufferSize);
            Memory<byte> buffer = memoryOwner.Memory[..bufferSize];
            int bytes = encoding.GetBytes(message.AsSpan(), buffer.Span);
            using var timeoutCancellationTokenSource = new CancellationTokenSource(SEND_TIMEOUT);
            using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, cancellationToken);

            buffer = buffer[..bytes];

            try
            {
                await TcpClient.SendAsync(buffer, linkedCancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, "Sending message to " + ToString() + " failed!");
            }

            TimeSinceLastSentMessage = TimeSpan.Zero;
        }

        public override string ToString()
            => Name + " (" + IPAddress + ")";

        /// <summary>
        /// Starts receiving messages from the player.
        /// </summary>
        public async ValueTask StartReceiveLoopAsync(CancellationToken cancellationToken)
        {
            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(4096);

            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead = 0;
                Memory<byte> message = memoryOwner.Memory[..4096];

                try
                {
                    bytesRead = await TcpClient.ReceiveAsync(message, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    ProgramConstants.LogException(ex, "Connection error with client " + Name + "; removing.");
                }

                if (bytesRead > 0)
                {
                    string msg = encoding.GetString(message.Span[..bytesRead]);

                    msg = overMessage + msg;

                    while (true)
                    {
                        int index = msg.IndexOf(ProgramConstants.LAN_MESSAGE_SEPARATOR, StringComparison.OrdinalIgnoreCase);

                        if (index == -1)
                        {
                            overMessage = msg;
                            break;
                        }

                        MessageReceived?.Invoke(this, new NetworkMessageEventArgs(msg[..index]));
                        msg = msg[(index + 1)..];
                    }

                    continue;
                }

                ConnectionLost?.Invoke(this, EventArgs.Empty);
                break;
            }
        }
    }
}