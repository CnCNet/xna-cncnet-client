using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.Extensions;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.CnCNet;
using Rampastring.Tools;

namespace DTAClient.Online
{
    /// <summary>
    /// The CnCNet connection handler.
    /// </summary>
    internal sealed class Connection
    {
        private const int MAX_RECONNECT_COUNT = 8;
        private const int RECONNECT_WAIT_DELAY = 4000;
        private const int ID_LENGTH = 9;
        private const int MAXIMUM_LATENCY = 400;
        private const int SendTimeout = 1000;
        private const int ConnectTimeout = 3000;
        private const int PingInterval = 120000;

        public Connection(IConnectionManager connectionManager)
        {
            this.connectionManager = connectionManager;
        }

        private readonly IConnectionManager connectionManager;

        /// <summary>
        /// The list of CnCNet / GameSurge IRC servers to connect to.
        /// </summary>
        private static readonly IList<Server> Servers = new List<Server>
        {
            new("Burstfire.UK.EU.GameSurge.net", "GameSurge London, UK", new[] { 6667, 6668, 7000 }),
            new("VortexServers.IL.US.GameSurge.net", "GameSurge Chicago, IL", new[] { 6660, 6666, 6667, 6668, 6669 }),
            new("Gameservers.NJ.US.GameSurge.net", "GameSurge Newark, NJ", new[] { 6665, 6666, 6667, 6668, 6669, 7000, 8080 }),
            new("Krypt.CA.US.GameSurge.net", "GameSurge Santa Ana, CA", new[] { 6666, 6667, 6668, 6669 }),
            new("NuclearFallout.WA.US.GameSurge.net", "GameSurge Seattle, WA", new[] { 6667, 5960 }),
            new("Stockholm.SE.EU.GameSurge.net", "GameSurge Stockholm, Sweden", new[] { 6660, 6666, 6667, 6668, 6669 }),
            new("Prothid.NY.US.GameSurge.Net", "GameSurge NYC, NY", new[] { 5960, 6660, 6666, 6667, 6668, 6669 }),
            new("TAL.DE.EU.GameSurge.net", "GameSurge Wuppertal, Germany", new[] { 6660, 6666, 6667, 6668, 6669 }),
            new("irc.gamesurge.net", "GameSurge", new[] { 6667 })
        }.AsReadOnly();

        private bool IsConnected { get; set; }

        public bool AttemptingConnection { get; private set; }

        public Random Rng { get; } = new();

        private readonly List<QueuedMessage> messageQueue = new();
        private TimeSpan messageQueueDelay;

        private Socket socket;

        private volatile int reconnectCount;

        private volatile bool connectionCut;
        private volatile bool welcomeMessageReceived;
        private volatile bool sendQueueExited;

        private string overMessage;

        /// <summary>
        /// A list of server IPs that have dropped our connection.
        /// The client skips these servers when attempting to re-connect, to
        /// prevent a server that first accepts a connection and then drops it
        /// right afterwards from preventing online play.
        /// </summary>
        private readonly List<string> failedServerIPs = new();
        private volatile string currentConnectedServerIP;

        private static readonly SemaphoreSlim messageQueueLocker = new(1, 1);

        private static string systemId;
        private static readonly object idLocker = new();
        private CancellationTokenSource connectionCancellationTokenSource;
        private CancellationTokenSource sendQueueCancellationTokenSource;

        public static void SetId(string id)
        {
            lock (idLocker)
            {
                int maxLength = ID_LENGTH - (ClientConfiguration.Instance.LocalGame.Length + 1);
                systemId = Utilities.CalculateSHA1ForString(id)[..maxLength];
            }
        }

        /// <summary>
        /// Attempts to connect to CnCNet without blocking the calling thread.
        /// </summary>
        public void ConnectAsync()
        {
            if (IsConnected)
                throw new InvalidOperationException("The client is already connected!".L10N("Client:Main:ClientAlreadyConnected"));

            if (AttemptingConnection)
                return; // Maybe we should throw in this case as well?

            welcomeMessageReceived = false;
            connectionCut = false;
            AttemptingConnection = true;

            messageQueueDelay = TimeSpan.FromMilliseconds(ClientConfiguration.Instance.SendSleep);

            connectionCancellationTokenSource?.Dispose();

            connectionCancellationTokenSource = new CancellationTokenSource();

            ConnectToServerAsync(connectionCancellationTokenSource.Token).HandleTask();
        }

        /// <summary>
        /// Attempts to connect to CnCNet.
        /// </summary>
        private async ValueTask ConnectToServerAsync(CancellationToken cancellationToken)
        {
            try
            {
                IList<Server> sortedServerList = await GetServerListSortedByLatencyAsync().ConfigureAwait(false);

                foreach (Server server in sortedServerList)
                {
                    try
                    {
                        foreach (int port in server.Ports)
                        {
                            connectionManager.OnAttemptedServerChanged(server.Name);

                            var client = new Socket(SocketType.Stream, ProtocolType.Tcp);
                            using var timeoutCancellationTokenSource = new CancellationTokenSource(ConnectTimeout);
                            using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, cancellationToken);

                            Logger.Log("Attempting connection to " + server.Host + ":" + port);

                            try
                            {
                                await client.ConnectAsync(
                                    new IPEndPoint(IPAddress.Parse(server.Host), port),
                                    linkedCancellationTokenSource.Token).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) when (timeoutCancellationTokenSource.Token.IsCancellationRequested)
                            {
                                Logger.Log("Connecting to " + server.Host + " port " + port + " timed out!");
                                continue; // Start all over again, using the next port
                            }

                            Logger.Log("Successfully connected to " + server.Host + " on port " + port);

                            IsConnected = true;
                            AttemptingConnection = false;

                            connectionManager.OnConnected();
                            sendQueueCancellationTokenSource?.Dispose();

                            sendQueueCancellationTokenSource = new CancellationTokenSource();

                            RunSendQueueAsync(sendQueueCancellationTokenSource.Token).HandleTask();

                            if (socket?.Connected ?? false)
                                socket.Shutdown(SocketShutdown.Both);

                            socket?.Close();
                            socket = client;

                            currentConnectedServerIP = server.Host;
                            await HandleCommAsync(cancellationToken).ConfigureAwait(false);
                            return;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        ProgramConstants.LogException(ex, "Unable to connect to the server.");
                    }
                }

                Logger.Log("Connecting to CnCNet failed!");
                // Clear the failed server list in case connecting to all servers has failed
                failedServerIPs.Clear();
                AttemptingConnection = false;
                connectionManager.OnConnectAttemptFailed();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async ValueTask HandleCommAsync(CancellationToken cancellationToken)
        {
            int errorTimes = 0;
            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(1024);
            Memory<byte> message = memoryOwner.Memory[..1024];

            await RegisterAsync().ConfigureAwait(false);

            var timer = new System.Timers.Timer(PingInterval)
            {
                Enabled = true
            };

            timer.Elapsed += (_, _) => AutoPingAsync().HandleTask();

            connectionCut = true;

            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead;

                try
                {
                    bytesRead = await socket.ReceiveAsync(message, SocketFlags.None, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ProgramConstants.LogException(ex, "Disconnected from CnCNet due to a socket error.");

                    errorTimes++;

                    if (errorTimes > MAX_RECONNECT_COUNT)
                    {
                        const string errorMessage = "Disconnected from CnCNet after reaching the maximum number of connection retries.";

                        Logger.Log(errorMessage);
                        failedServerIPs.Add(currentConnectedServerIP);
                        connectionManager.OnConnectionLost(errorMessage.L10N("Client:Main:ClientDisconnectedAfterRetries"));
                        break;
                    }

                    continue;
                }

                errorTimes = 0;

                // A message has been successfully received
                string msg = Encoding.UTF8.GetString(message.Span[..bytesRead]);

#if !DEBUG
                Logger.Log("Message received: " + msg);
#endif
                await HandleMessageAsync(msg).ConfigureAwait(false);

                timer.Interval = 30000;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                connectionManager.OnDisconnected();

                connectionCut = false; // This disconnect is intentional
            }

            timer.Enabled = false;

            timer.Dispose();

            IsConnected = false;

            if (connectionCut)
            {
                sendQueueCancellationTokenSource.Cancel();

                while (!sendQueueExited)
                {
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }

                reconnectCount++;

                if (reconnectCount > MAX_RECONNECT_COUNT)
                {
                    Logger.Log("Reconnect attempt count exceeded!");
                    return;
                }

                await Task.Delay(RECONNECT_WAIT_DELAY, cancellationToken).ConfigureAwait(false);

                if (IsConnected || AttemptingConnection)
                {
                    Logger.Log("Cancelling reconnection attempt because the user has attempted to reconnect manually.");
                    return;
                }

                Logger.Log("Attempting to reconnect to CnCNet.");
                connectionManager.OnReconnectAttempt();
            }
        }

        /// <summary>
        /// Get all IP addresses of Lobby servers by resolving the hostname and test the latency to the servers.
        /// The maximum latency is defined in <c>MAXIMUM_LATENCY</c>, see <see cref="Connection.MAXIMUM_LATENCY"/>.
        /// Servers that did not respond to ICMP messages in time will be placed at the end of the list.
        /// </summary>
        /// <returns>A list of Lobby servers sorted by latency.</returns>
        private async ValueTask<IList<Server>> GetServerListSortedByLatencyAsync()
        {
            // Resolve the hostnames.
            IEnumerable<(IPAddress IpAddress, string Name, int[] Ports)>[] servers =
                await ClientCore.Extensions.TaskExtensions.WhenAllSafe(Servers.Select(ResolveServerAsync)).ConfigureAwait(false);

            // Group the tuples by IPAddress to merge duplicate servers.
            IEnumerable<IGrouping<IPAddress, (string Name, int[] Ports)>> serverInfosGroupedByIPAddress = servers
                .SelectMany(server => server)
                .GroupBy(serverInfo => serverInfo.IpAddress, serverInfo => (serverInfo.Name, serverInfo.Ports));
            bool hasIPv6Internet = NetworkHelper.HasIPv6Internet();
            bool hasIPv4Internet = NetworkHelper.HasIPv4Internet();

            // Process each group:
            //   1. Get IPAddress.
            //   2. Concatenate serverNames.
            //   3. Remove duplicate ports.
            //   4. Construct and return a tuple that contains the IPAddress, concatenated serverNames and unique ports.
            (IPAddress IpAddress, string Name, int[] Ports)[] serverInfos = serverInfosGroupedByIPAddress.Select(serverInfoGroup =>
            {
                IPAddress ipAddress = serverInfoGroup.Key;
                string serverNames = string.Join(", ", serverInfoGroup.Where(serverInfo => !"GameSurge".Equals(serverInfo.Name))
                    .Select(serverInfo => serverInfo.Name));
                int[] serverPorts = serverInfoGroup.SelectMany(serverInfo => serverInfo.Ports).Distinct().ToArray();

                return (ipAddress, serverNames, serverPorts);
            }).
            Where(q => (q.ipAddress.AddressFamily is AddressFamily.InterNetworkV6 && hasIPv6Internet)
                || (q.ipAddress.AddressFamily is AddressFamily.InterNetwork && hasIPv4Internet))
            .ToArray();

            // Do logging.
            foreach ((IPAddress ipAddress, string name, int[] ports) in serverInfos)
            {
                string serverIPAddress = ipAddress.ToString();
                string serverNames = string.Join(", ", name);
                string serverPorts = string.Join(", ", ports.Select(port => port.ToString()));

                Logger.Log($"Got a Lobby server. IP: {serverIPAddress}; Name: {serverNames}; Ports: {serverPorts}.");
            }

            Logger.Log($"The number of Lobby servers is {serverInfos.Length}.");

            // Test the latency.
            foreach ((IPAddress ipAddress, string name, int[] _) in serverInfos.Where(q => failedServerIPs.Contains(q.IpAddress.ToString())))
            {
                Logger.Log($"Skipped a failed server {name} ({ipAddress}).");
            }

            (Server Server, IPAddress IpAddress, long Result)[] serverAndLatencyResults =
                await ClientCore.Extensions.TaskExtensions.WhenAllSafe(serverInfos.Where(q => !failedServerIPs.Contains(q.IpAddress.ToString())).Select(PingServerAsync)).ConfigureAwait(false);

            // Sort the servers by AddressFamily & latency.
            (Server Server, IPAddress IpAddress, long Result)[] sortedServerAndLatencyResults = serverAndLatencyResults
                .Where(server => server.IpAddress.AddressFamily is AddressFamily.InterNetworkV6 && server.Result is not long.MaxValue)
                .Select(server => server)
                .OrderBy(taskResult => taskResult.Result)
                .Concat(serverAndLatencyResults
                    .Where(server => server.IpAddress.AddressFamily is AddressFamily.InterNetwork && server.Result is not long.MaxValue)
                    .Select(server => server)
                    .OrderBy(taskResult => taskResult.Result))
                .Concat(serverAndLatencyResults
                    .Where(server => server.IpAddress.AddressFamily is AddressFamily.InterNetworkV6 && server.Result is long.MaxValue)
                    .Select(server => server)
                    .OrderBy(taskResult => taskResult.Result))
                .Concat(serverAndLatencyResults
                    .Where(server => server.IpAddress.AddressFamily is AddressFamily.InterNetwork && server.Result is long.MaxValue)
                    .Select(server => server)
                    .OrderBy(taskResult => taskResult.Result))
                .ToArray();

            // Do logging.
            foreach ((Server _, IPAddress ipAddress, long serverLatencyValue) in sortedServerAndLatencyResults)
            {
                string serverLatencyString = serverLatencyValue <= MAXIMUM_LATENCY ? serverLatencyValue.ToString() : "DNF";

                Logger.Log($"Lobby server IP: {ipAddress}, latency: {serverLatencyString}.");
            }

            int candidateCount = sortedServerAndLatencyResults.Length;
            int closerCount = sortedServerAndLatencyResults.Count(
                serverAndLatencyResult => serverAndLatencyResult.Result <= MAXIMUM_LATENCY);

            Logger.Log($"Lobby servers: {candidateCount} available, {closerCount} fast.");
            connectionManager.OnServerLatencyTested(candidateCount, closerCount);

            return sortedServerAndLatencyResults.Select(taskResult => taskResult.Server).ToList();
        }

        private static async Task<(Server Server, IPAddress IpAddress, long Result)> PingServerAsync((IPAddress IpAddress, string Name, int[] Ports) serverInfo)
        {
            Logger.Log($"Attempting to ping {serverInfo.Name} ({serverInfo.IpAddress}).");
            var server = new Server(serverInfo.IpAddress.ToString(), serverInfo.Name, serverInfo.Ports);
            using var ping = new Ping();

            try
            {
                PingReply pingReply = await ping.SendPingAsync(serverInfo.IpAddress, MAXIMUM_LATENCY).ConfigureAwait(false);

                if (pingReply.Status is IPStatus.Success)
                {
                    long pingInMs = pingReply.RoundtripTime;
                    Logger.Log($"The latency in milliseconds to the server {serverInfo.Name} ({serverInfo.IpAddress}): {pingInMs}.");

                    return (server, serverInfo.IpAddress, pingInMs);
                }

                Logger.Log($"Failed to ping the server {serverInfo.Name} ({serverInfo.IpAddress}): " +
                    $"{Enum.GetName(typeof(IPStatus), pingReply.Status)}.");

                return (server, serverInfo.IpAddress, long.MaxValue);
            }
            catch (PingException ex)
            {
                ProgramConstants.LogException(ex, $"Caught an exception when pinging {serverInfo.Name} ({serverInfo.IpAddress}) Lobby server.");

                return (server, serverInfo.IpAddress, long.MaxValue);
            }
        }

        private static async Task<IEnumerable<(IPAddress IpAddress, string Name, int[] Ports)>> ResolveServerAsync(Server server)
        {
            Logger.Log($"Attempting to DNS resolve {server.Name} ({server.Host}).");

            try
            {
                // If hostNameOrAddress is an IP address, this address is returned without querying the DNS server.
                IPAddress[] serverIPAddresses = (await Dns.GetHostAddressesAsync(server.Host).ConfigureAwait(false))
                    .Where(IPAddress => IPAddress.AddressFamily is AddressFamily.InterNetworkV6 or AddressFamily.InterNetwork)
                    .ToArray();

                Logger.Log($"DNS resolved {server.Name} ({server.Host}): " +
                    $"{string.Join(", ", serverIPAddresses.Select(item => item.ToString()))}");

                // Store each IPAddress in a different tuple.
                return serverIPAddresses.Select(serverIPAddress => (serverIPAddress, server.Name, server.Ports));
            }
            catch (SocketException ex)
            {
                ProgramConstants.LogException(ex, $"Caught an exception when DNS resolving {server.Name} ({server.Host}) Lobby server.");
            }

            return Array.Empty<(IPAddress IpAddress, string Name, int[] Ports)>();
        }

        public async ValueTask DisconnectAsync()
        {
            await SendMessageAsync(IRCCommands.QUIT).ConfigureAwait(false);
            connectionCancellationTokenSource.Cancel();
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        #region Handling commands

        /// <summary>
        /// Checks if a message from the IRC server is a partial or full
        /// message, and handles it accordingly.
        /// </summary>
        /// <param name="message">The message.</param>
        private async ValueTask HandleMessageAsync(string message)
        {
            string msg = overMessage + message;
            overMessage = "";
            while (true)
            {
                int commandEndIndex = msg.IndexOf("\n");

                if (commandEndIndex == -1)
                {
                    overMessage = msg;
                    break;
                }
                else if (msg.Length != commandEndIndex + 1)
                {
                    string command = msg[..(commandEndIndex - 1)];
                    await PerformCommandAsync(command).ConfigureAwait(false);

                    msg = msg.Remove(0, commandEndIndex + 1);
                }
                else
                {
                    string command = msg[..^1];
                    await PerformCommandAsync(command).ConfigureAwait(false);
                    break;
                }
            }
        }

        /// <summary>
        /// Handles a specific command received from the IRC server.
        /// </summary>
        private async ValueTask PerformCommandAsync(string message)
        {
            message = message.Replace("\r", string.Empty);
            ParseIrcMessage(message, out string prefix, out string command, out List<string> parameters);
            string paramString = string.Empty;
            foreach (string param in parameters) { paramString = paramString + param + ","; }
#if !DEBUG
            Logger.Log("RMP: " + prefix + " " + command + " " + paramString);
#endif

            try
            {
                bool success = int.TryParse(command, out int commandNumber);

                if (success)
                {
                    string serverMessagePart = prefix + ": ";

                    switch (commandNumber)
                    {
                        // Command descriptions from https://www.alien.net.au/irc/irc2numerics.html

                        case 001: // Welcome message
                            message = serverMessagePart + parameters[1];
                            welcomeMessageReceived = true;
                            connectionManager.OnWelcomeMessageReceived(message);
                            reconnectCount = 0;
                            break;
                        case 002: // "Your host is x, running version y"
                        case 003: // "This server was created..."
                        case 251: // There are <int> users and <int> invisible on <int> servers
                        case 255: // I have <int> clients and <int> servers
                        case 265: // Local user count
                        case 266: // Global user count
                        case 401: // Used to indicate the nickname parameter supplied to a command is currently unused
                        case 403: // Used to indicate the given channel name is invalid, or does not exist
                        case 404: // Used to indicate that the user does not have the rights to send a message to a channel
                        case 432: // Invalid nickname on registration
                        case 461: // Returned by the server to any command which requires more parameters than the number of parameters given
                        case 465: // Returned to a client after an attempt to register on a server configured to ban connections from that client
                            StringBuilder displayedMessage = new StringBuilder(serverMessagePart);
                            for (int i = 1; i < parameters.Count; i++)
                            {
                                displayedMessage.Append(' ');
                                displayedMessage.Append(parameters[i]);
                            }
                            connectionManager.OnGenericServerMessageReceived(displayedMessage.ToString());
                            break;
                        case 439: // Attempt to send messages too fast
                            connectionManager.OnTargetChangeTooFast(parameters[1], parameters[2]);
                            break;
                        case 252: // Number of operators online
                        case 254: // Number of channels formed
                            message = serverMessagePart + parameters[1] + " " + parameters[2];
                            connectionManager.OnGenericServerMessageReceived(message);
                            break;
                        case 301: // AWAY message
                            string awayTarget = parameters[0];
                            if (awayTarget != ProgramConstants.PLAYERNAME)
                                break;
                            string awayPlayer = parameters[1];
                            string awayReason = parameters[2];
                            connectionManager.OnAwayMessageReceived(awayPlayer, awayReason);
                            break;
                        case 332: // Channel topic message
                            string _target = parameters[0];
                            if (_target != ProgramConstants.PLAYERNAME)
                                break;
                            connectionManager.OnChannelTopicReceived(parameters[1], parameters[2]);
                            break;
                        case 353: // User list (reply to NAMES)
                            string target = parameters[0];
                            if (target != ProgramConstants.PLAYERNAME)
                                break;
                            string channelName = parameters[2];
                            string[] users = parameters[3].Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            connectionManager.OnUserListReceived(channelName, users);
                            break;
                        case 352: // Reply to WHO query
                            string ident = parameters[2];
                            string host = parameters[3];
                            string wUserName = parameters[5];
                            string extraInfo = parameters[7];
                            connectionManager.OnWhoReplyReceived(ident, host, wUserName, extraInfo);
                            break;
                        case 311: // Reply to WHOIS NAME query
                            connectionManager.OnWhoReplyReceived(parameters[2], parameters[3], parameters[1], string.Empty);
                            break;
                        case 433: // Name already in use
                            message = serverMessagePart + parameters[1] + ": " + parameters[2];
                            //connectionManager.OnGenericServerMessageReceived(message);
                            connectionManager.OnNameAlreadyInUse();
                            break;
                        case 451: // Not registered
                            await RegisterAsync().ConfigureAwait(false);
                            connectionManager.OnGenericServerMessageReceived(message);
                            break;
                        case 471: // Returned when attempting to join a channel that is full (basically, player limit met)
                            connectionManager.OnChannelFull(parameters[1]);
                            break;
                        case 473: // Returned when attempting to join an invite-only channel (locked games)
                            connectionManager.OnChannelInviteOnly(parameters[1]);
                            break;
                        case 474: // Returned when attempting to join a channel a user is banned from
                            connectionManager.OnBannedFromChannel(parameters[1]);
                            break;
                        case 475: // Returned when attempting to join a key-locked channel either without a key or with the wrong key
                            connectionManager.OnIncorrectChannelPassword(parameters[1]);
                            break;
                    }

                    return;
                }

                switch (command)
                {
                    case IRCCommands.NOTICE:
                        int noticeExclamIndex = prefix.IndexOf('!');
                        if (noticeExclamIndex > -1)
                        {
                            if (parameters.Count > 1 && parameters[1][0] == 1)
                            {
                                // CTCP
                                string channelName = parameters[0];
                                string ctcpMessage = parameters[1];
                                ctcpMessage = ctcpMessage.Remove(0, 1).Remove(ctcpMessage.Length - 2);
                                string ctcpSender = prefix[..noticeExclamIndex];
                                connectionManager.OnCTCPParsed(channelName, ctcpSender, ctcpMessage);

                                return;
                            }

                            string noticeUserName = prefix[..noticeExclamIndex];
                            string notice = parameters[^1];
                            connectionManager.OnNoticeMessageParsed(notice, noticeUserName);
                            break;
                        }
                        string noticeParamString = string.Empty;
                        foreach (string param in parameters)
                            noticeParamString = noticeParamString + param + " ";
                        connectionManager.OnGenericServerMessageReceived(prefix + " " + noticeParamString);
                        break;
                    case IRCCommands.JOIN:
                        string channel = parameters[0];
                        int atIndex = prefix.IndexOf('@');
                        int exclamIndex = prefix.IndexOf('!');
                        string userName = prefix[..exclamIndex];
                        string ident = prefix.Substring(exclamIndex + 1, atIndex - (exclamIndex + 1));
                        string host = prefix[(atIndex + 1)..];
                        connectionManager.OnUserJoinedChannel(channel, host, userName, ident);
                        break;
                    case IRCCommands.PART:
                        string pChannel = parameters[0];
                        string pUserName = prefix[..prefix.IndexOf('!')];
                        connectionManager.OnUserLeftChannel(pChannel, pUserName);
                        break;
                    case IRCCommands.QUIT:
                        string qUserName = prefix[..prefix.IndexOf('!')];
                        connectionManager.OnUserQuitIRC(qUserName);
                        break;
                    case IRCCommands.PRIVMSG:
                        if (parameters.Count > 1 && Convert.ToInt32(parameters[1][0]) == 1 && !parameters[1].Contains(IRCCommands.PRIVMSG_ACTION))
                        {
                            goto case IRCCommands.NOTICE;
                        }
                        string pmsgUserName = prefix[..prefix.IndexOf('!')];
                        string pmsgIdent = GetIdentFromPrefix(prefix);
                        string[] recipients = new string[parameters.Count - 1];
                        for (int pid = 0; pid < parameters.Count - 1; pid++)
                            recipients[pid] = parameters[pid];
                        string privmsg = parameters[^1];
                        if (parameters[1].StartsWith('\u0001' + IRCCommands.PRIVMSG_ACTION))
                            privmsg = privmsg[1..].Remove(privmsg.Length - 2);
                        foreach (string recipient in recipients)
                        {
                            if (recipient.StartsWith("#"))
                                connectionManager.OnChatMessageReceived(recipient, pmsgUserName, pmsgIdent, privmsg);
                            else if (recipient == ProgramConstants.PLAYERNAME)
                                connectionManager.OnPrivateMessageReceived(pmsgUserName, privmsg);
                        }
                        break;
                    case IRCCommands.MODE:
                        string modeUserName = prefix.Contains('!') ? prefix[..prefix.IndexOf('!')] : prefix;
                        string modeChannelName = parameters[0];
                        string modeString = parameters[1];
                        List<string> modeParameters =
                            parameters.Count > 2 ? parameters.GetRange(2, parameters.Count - 2) : new List<string>();
                        connectionManager.OnChannelModesChanged(modeUserName, modeChannelName, modeString, modeParameters);
                        break;
                    case IRCCommands.KICK:
                        string kickChannelName = parameters[0];
                        string kickUserName = parameters[1];
                        connectionManager.OnUserKicked(kickChannelName, kickUserName);
                        break;
                    case IRCCommands.ERROR:
                        connectionManager.OnErrorReceived(message);
                        break;
                    case IRCCommands.PING:
                        if (parameters.Count > 0)
                        {
                            await QueueMessageAsync(new QueuedMessage(IRCCommands.PONG + " " + parameters[0], QueuedMessageType.SYSTEM_MESSAGE, 5000)).ConfigureAwait(false);
                            Logger.Log(IRCCommands.PONG + " " + parameters[0]);
                        }
                        else
                        {
                            await QueueMessageAsync(new QueuedMessage(IRCCommands.PONG, QueuedMessageType.SYSTEM_MESSAGE, 5000)).ConfigureAwait(false);
                            Logger.Log(IRCCommands.PONG);
                        }
                        break;
                    case IRCCommands.TOPIC:
                        if (parameters.Count < 2)
                            break;

                        connectionManager.OnChannelTopicChanged(prefix[..prefix.IndexOf('!')],
                            parameters[0], parameters[1]);
                        break;
                    case IRCCommands.NICK:
                        int nickExclamIndex = prefix.IndexOf('!');
                        if (nickExclamIndex > -1 || parameters.Count < 1)
                        {
                            string oldNick = prefix[..nickExclamIndex];
                            string newNick = parameters[0];
                            Logger.Log("Nick change - " + oldNick + " -> " + newNick);
                            connectionManager.OnUserNicknameChange(oldNick, newNick);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, "Warning: Failed to parse command " + message);
            }
        }

        private string GetIdentFromPrefix(string prefix)
        {
            int atIndex = prefix.IndexOf('@');
            int exclamIndex = prefix.IndexOf('!');

            if (exclamIndex == -1 || atIndex == -1)
                return string.Empty;

            return prefix.Substring(exclamIndex + 1, atIndex - (exclamIndex + 1));
        }

        /// <summary>
        /// Parses a single IRC message received from the server.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="prefix">(out) The message prefix.</param>
        /// <param name="command">(out) The command.</param>
        /// <param name="parameters">(out) The parameters of the command.</param>
        private void ParseIrcMessage(string message, out string prefix, out string command, out List<string> parameters)
        {
            int prefixEnd = -1;
            prefix = command = string.Empty;
            parameters = new List<string>();

            // Grab the prefix if it is present. If a message begins
            // with a colon, the characters following the colon until
            // the first space are the prefix.
            if (message.StartsWith(":"))
            {
                prefixEnd = message.IndexOf(" ");
                prefix = message.Substring(1, prefixEnd - 1);
            }

            // Grab the trailing if it is present. If a message contains
            // a space immediately following a colon, all characters after
            // the colon are the trailing part.
            int trailingStart = message.IndexOf(" :");
            string trailing = null;
            if (trailingStart >= 0)
                trailing = message[(trailingStart + 2)..];
            else
                trailingStart = message.Length;

            // Use the prefix end position and trailing part start
            // position to extract the command and parameters.
            var commandAndParameters = message.Substring(prefixEnd + 1, trailingStart - prefixEnd - 1).Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (commandAndParameters.Length == 0)
            {
                command = string.Empty;
                Logger.Log("Nonexistant command!");
                return;
            }

            // The command will always be the first element of the array.
            command = commandAndParameters[0];

            // The rest of the elements are the parameters, if they exist.
            // Skip the first element because that is the command.
            if (commandAndParameters.Length > 1)
            {
                for (int id = 1; id < commandAndParameters.Length; id++)
                {
                    parameters.Add(commandAndParameters[id]);
                }
            }

            // If the trailing part is valid add the trailing part to the
            // end of the parameters.
            if (!string.IsNullOrEmpty(trailing))
                parameters.Add(trailing);
        }

        #endregion

        #region Sending commands

        private async ValueTask RunSendQueueAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    string message = string.Empty;

                    await messageQueueLocker.WaitAsync(cancellationToken).ConfigureAwait(false);

                    try
                    {
                        for (int i = 0; i < messageQueue.Count; i++)
                        {
                            QueuedMessage qm = messageQueue[i];
                            if (qm.Delay > 0)
                            {
                                if (qm.SendAt < DateTime.Now)
                                {
                                    message = qm.Command;

                                    Logger.Log("Delayed message sent: " + qm.ID);

                                    messageQueue.RemoveAt(i);
                                    break;
                                }
                            }
                            else
                            {
                                message = qm.Command;
                                messageQueue.RemoveAt(i);
                                break;
                            }
                        }
                    }
                    finally
                    {
                        messageQueueLocker.Release();
                    }

                    if (string.IsNullOrEmpty(message))
                    {
                        await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    await SendMessageAsync(message).ConfigureAwait(false);
                    await Task.Delay(messageQueueDelay, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                await messageQueueLocker.WaitAsync(CancellationToken.None).ConfigureAwait(false);

                try
                {
                    messageQueue.Clear();
                }
                finally
                {
                    messageQueueLocker.Release();
                }

                sendQueueExited = true;
            }
        }

        /// <summary>
        /// Sends a PING message to the server to indicate that we're still connected.
        /// </summary>
        private ValueTask AutoPingAsync()
            => SendMessageAsync(IRCCommands.PING_LAG + new Random().Next(100000, 999999));

        /// <summary>
        /// Registers the user.
        /// </summary>
        private async ValueTask RegisterAsync()
        {
            if (welcomeMessageReceived)
                return;

            Logger.Log("Registering.");

            string defaultGame = ClientConfiguration.Instance.LocalGame;
            string realName = ProgramConstants.GAME_VERSION + " " + defaultGame + " CnCNet";

            await SendMessageAsync(FormattableString.Invariant($"{IRCCommands.USER} {defaultGame}.{systemId} 0 * :{realName}")).ConfigureAwait(false);
            await SendMessageAsync(IRCCommands.NICK + " " + ProgramConstants.PLAYERNAME).ConfigureAwait(false);
        }

        public ValueTask ChangeNicknameAsync()
        {
            return SendMessageAsync(IRCCommands.NICK + " " + ProgramConstants.PLAYERNAME);
        }

        public ValueTask QueueMessageAsync(QueuedMessageType type, int priority, string message, bool replace = false)
        {
            QueuedMessage qm = new QueuedMessage(message, type, priority, replace);
            return QueueMessageAsync(qm);
        }

        public async ValueTask QueueMessageAsync(QueuedMessageType type, int priority, int delay, string message)
        {
            QueuedMessage qm = new QueuedMessage(message, type, priority, delay);
            await QueueMessageAsync(qm).ConfigureAwait(false);
            Logger.Log("Setting delay to " + delay + "ms for " + qm.ID);
        }

        /// <summary>
        /// Send a message to the CnCNet server.
        /// </summary>
        /// <param name="message">The message to send.</param>
        private async ValueTask SendMessageAsync(string message)
        {
            if (!socket?.Connected ?? false)
                return;

            Logger.Log("SRM: " + message);

            const int charSize = sizeof(char);
            int bufferSize = message.Length * charSize;
            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(bufferSize);
            Memory<byte> buffer = memoryOwner.Memory[..bufferSize];
            int bytes = Encoding.UTF8.GetBytes((message + "\r\n").AsSpan(), buffer.Span);

            buffer = buffer[..bytes];

            using var timeoutCancellationTokenSource = new CancellationTokenSource(SendTimeout);

            try
            {
                await socket.SendAsync(buffer, SocketFlags.None, timeoutCancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (SocketException ex)
            {
                ProgramConstants.LogException(ex, "Sending message to the server failed!");
            }
            catch (OperationCanceledException ex)
            {
                ProgramConstants.LogException(ex, "Sending message to the server timed out!");
            }
        }

        private int NextQueueID { get; set; }

        /// <summary>
        /// This will attempt to replace a previously queued message of the same type.
        /// </summary>
        /// <param name="qm">The new message to replace with</param>
        /// <returns>Whether or not a replace occurred</returns>
        private bool ReplaceMessage(QueuedMessage qm)
        {
            messageQueueLocker.Wait();

            try
            {
                var previousMessageIndex = messageQueue.FindIndex(m => m.MessageType == qm.MessageType);
                if (previousMessageIndex == -1)
                    return false;

                messageQueue[previousMessageIndex] = qm;
                return true;
            }
            finally
            {
                messageQueueLocker.Release();
            }
        }

        /// <summary>
        /// Adds a message to the send queue.
        /// </summary>
        /// <param name="qm">The message to queue.</param>
        public async ValueTask QueueMessageAsync(QueuedMessage qm)
        {
            if (!IsConnected)
                return;

            if (qm.Replace && ReplaceMessage(qm))
                return;

            qm.ID = NextQueueID++;

            await messageQueueLocker.WaitAsync().ConfigureAwait(false);

            try
            {
                switch (qm.MessageType)
                {
                    case QueuedMessageType.GAME_BROADCASTING_MESSAGE:
                    case QueuedMessageType.GAME_PLAYERS_MESSAGE:
                    case QueuedMessageType.GAME_SETTINGS_MESSAGE:
                    case QueuedMessageType.GAME_PLAYERS_READY_STATUS_MESSAGE:
                    case QueuedMessageType.GAME_LOCKED_MESSAGE:
                    case QueuedMessageType.GAME_GET_READY_MESSAGE:
                    case QueuedMessageType.GAME_NOTIFICATION_MESSAGE:
                    case QueuedMessageType.GAME_HOSTING_MESSAGE:
                    case QueuedMessageType.WHOIS_MESSAGE:
                    case QueuedMessageType.GAME_CHEATER_MESSAGE:
                        AddSpecialQueuedMessage(qm);
                        break;
                    case QueuedMessageType.INSTANT_MESSAGE:
                        await SendMessageAsync(qm.Command).ConfigureAwait(false);
                        break;
                    default:
                        int placeInQueue = messageQueue.FindIndex(m => m.Priority < qm.Priority);
                        if (ProgramConstants.LOG_LEVEL > 1)
                            Logger.Log("QM Undefined: " + qm.Command + " " + placeInQueue);
                        if (placeInQueue == -1)
                            messageQueue.Add(qm);
                        else
                            messageQueue.Insert(placeInQueue, qm);
                        break;
                }
            }
            finally
            {
                messageQueueLocker.Release();
            }
        }

        /// <summary>
        /// Adds a "special" message to the send queue that replaces
        /// previous messages of the same type in the queue.
        /// </summary>
        /// <param name="qm">The message to queue.</param>
        private void AddSpecialQueuedMessage(QueuedMessage qm)
        {
            int broadcastingMessageIndex = messageQueue.FindIndex(m => m.MessageType == qm.MessageType);

            qm.ID = NextQueueID++;

            if (broadcastingMessageIndex > -1)
            {
                if (ProgramConstants.LOG_LEVEL > 1)
                    Logger.Log("QM Replace: " + qm.Command + " " + broadcastingMessageIndex);
                messageQueue[broadcastingMessageIndex] = qm;
            }
            else
            {
                int placeInQueue = messageQueue.FindIndex(m => m.Priority < qm.Priority);
                if (ProgramConstants.LOG_LEVEL > 1)
                    Logger.Log("QM: " + qm.Command + " " + placeInQueue);
                if (placeInQueue == -1)
                    messageQueue.Add(qm);
                else
                    messageQueue.Insert(placeInQueue, qm);
            }
        }

        #endregion
    }
}