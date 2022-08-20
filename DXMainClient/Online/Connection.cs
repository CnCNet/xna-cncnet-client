using ClientCore;
using Localization;
using Rampastring.Tools;
using System;
#if !NETFRAMEWORK
using System.Buffers;
#endif
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DTAClient.Online
{
    /// <summary>
    /// The CnCNet connection handler.
    /// </summary>
    public class Connection
    {
        private const int MAX_RECONNECT_COUNT = 8;
        private const int RECONNECT_WAIT_DELAY = 4000;
        private const int ID_LENGTH = 9;
        private const int MAXIMUM_LATENCY = 400;

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
            new("ColoCrossing.IL.US.GameSurge.net", "GameSurge Chicago, IL", new[] { 6660, 6666, 6667, 6668, 6669 }),
            new("Gameservers.NJ.US.GameSurge.net", "GameSurge Newark, NJ", new[] { 6665, 6666, 6667, 6668, 6669, 7000, 8080 }),
            new("Krypt.CA.US.GameSurge.net", "GameSurge Santa Ana, CA",new[] { 6666, 6667, 6668, 6669 }),
            new("NuclearFallout.WA.US.GameSurge.net", "GameSurge Seattle, WA", new[] { 6667, 5960 }),
            new("Portlane.SE.EU.GameSurge.net", "GameSurge Stockholm, Sweden", new[] { 6660, 6666, 6667, 6668, 6669 }),
            new("Prothid.NY.US.GameSurge.Net", "GameSurge NYC, NY", new[] { 5960, 6660, 6666, 6667, 6668, 6669, 6697 }),
            new("TAL.DE.EU.GameSurge.net", "GameSurge Wuppertal, Germany", new[] { 6660, 6666, 6667, 6668, 6669 }),
            new("208.167.237.120", "GameSurge IP 208.167.237.120", new[] {  6660, 6666, 6667, 6668, 6669, 7000, 8080 }),
            new("192.223.27.109", "GameSurge IP 192.223.27.109", new[] {  6660, 6666, 6667, 6668, 6669, 7000, 8080 }),
            new("108.174.48.100", "GameSurge IP 108.174.48.100", new[] { 6660, 6666, 6667, 6668, 6669, 7000, 8080 }),
            new("208.146.35.105", "GameSurge IP 208.146.35.105", new[] { 6660, 6666, 6667, 6668, 6669, 7000, 8080 }),
            new("195.8.250.180", "GameSurge IP 195.8.250.180", new[] { 6660, 6666, 6667, 6668, 6669, 7000, 8080 }),
            new("91.217.189.76", "GameSurge IP 91.217.189.76", new[] { 6660, 6666, 6667, 6668, 6669, 7000, 8080 }),
            new("195.68.206.250", "GameSurge IP 195.68.206.250", new[] { 6660, 6666, 6667, 6668, 6669, 7000, 8080 }),
            new("irc.gamesurge.net", "GameSurge", new[] { 6667 }),
        }.AsReadOnly();

        bool _isConnected;
        public bool IsConnected
        {
            get { return _isConnected; }
        }

        bool _attemptingConnection;
        public bool AttemptingConnection
        {
            get { return _attemptingConnection; }
        }

        Random _rng = new();
        public Random Rng
        {
            get { return _rng; }
        }

        private List<QueuedMessage> MessageQueue = new();
        private TimeSpan MessageQueueDelay;

        private Socket socket;

        volatile int reconnectCount;

        private volatile bool connectionCut;
        private volatile bool welcomeMessageReceived;
        private volatile bool sendQueueExited;

        private string overMessage;

        private readonly Encoding encoding = Encoding.UTF8;

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
        private CancellationTokenSource cancellationTokenSource;

        public static void SetId(string id)
        {
            lock (idLocker)
            {
                int maxLength = ID_LENGTH - (ClientConfiguration.Instance.LocalGame.Length + 1);
                systemId = Utilities.CalculateSHA1ForString(id).Substring(0, maxLength);
            }
        }

        /// <summary>
        /// Attempts to connect to CnCNet without blocking the calling thread.
        /// </summary>
        public void ConnectAsync()
        {
            if (_isConnected)
                throw new InvalidOperationException("The client is already connected!".L10N("UI:Main:ClientAlreadyConnected"));

            if (_attemptingConnection)
                return; // Maybe we should throw in this case as well?

            welcomeMessageReceived = false;
            connectionCut = false;
            _attemptingConnection = true;

            MessageQueueDelay = TimeSpan.FromMilliseconds(ClientConfiguration.Instance.SendSleep);

            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();

            ConnectToServerAsync(cancellationTokenSource.Token);
        }

        /// <summary>
        /// Attempts to connect to CnCNet.
        /// </summary>
        private async Task ConnectToServerAsync(CancellationToken cancellationToken)
        {
            try
            {
                IList<Server> sortedServerList = await GetServerListSortedByLatencyAsync();

                foreach (Server server in sortedServerList)
                {
                    try
                    {
                        foreach (int port in server.Ports)
                        {
                            connectionManager.OnAttemptedServerChanged(server.Name);

                            var client = new Socket(SocketType.Stream, ProtocolType.Tcp)
                            {
                                ReceiveTimeout = 1000
                            };

                            Logger.Log("Attempting connection to " + server.Host + ":" + port);

#if NETFRAMEWORK
                            IAsyncResult result = client.BeginConnect(server.Host, port, null, null);
                            result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3), false);
#else
                            try
                            {
                                await client.ConnectAsync(new IPEndPoint(IPAddress.Parse(server.Host), port),
                                    new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token);
                            }
                            catch (OperationCanceledException)
                            { }
#endif

                            if (!client.Connected)
                            {
                                Logger.Log("Connecting to " + server.Host + " port " + port + " timed out!");
                                continue; // Start all over again, using the next port
                            }

                            Logger.Log("Succesfully connected to " + server.Host + " on port " + port);
#if NETFRAMEWORK
                            client.EndConnect(result);
#endif

                            _isConnected = true;
                            _attemptingConnection = false;

                            connectionManager.OnConnected();

                            RunSendQueueAsync(cancellationToken);

                            socket?.Dispose();
                            socket = client;

                            currentConnectedServerIP = server.Host;
                            await HandleCommAsync(cancellationToken);
                            return;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        PreStartup.LogException(ex, "Unable to connect to the server.");
                    }
                }

                Logger.Log("Connecting to CnCNet failed!");
                // Clear the failed server list in case connecting to all servers has failed
                failedServerIPs.Clear();
                _attemptingConnection = false;
                connectionManager.OnConnectAttemptFailed();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }

        private async Task HandleCommAsync(CancellationToken cancellationToken)
        {
            int errorTimes = 0;
#if NETFRAMEWORK
            byte[] message1 = new byte[1024];
            var message = new ArraySegment<byte>(message1);
#else
            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(1024);
            Memory<byte> message = memoryOwner.Memory[..1024];
#endif

            await RegisterAsync();

            var timer = new System.Timers.Timer(120000)
            {
                Enabled = true
            };

            timer.Elapsed += (_, _) => AutoPingAsync();

            connectionCut = true;

            while (!cancellationToken.IsCancellationRequested)
            {
                int bytesRead;

                try
                {
#if NETFRAMEWORK
                    bytesRead = await socket.ReceiveAsync(message, SocketFlags.None);
                }
#else
                    bytesRead = await socket.ReceiveAsync(message, SocketFlags.None, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    connectionManager.OnDisconnected();
                    connectionCut = false; // This disconnect is intentional
                    break;
                }
#endif
                catch (Exception ex)
                {
                    PreStartup.LogException(ex, "Disconnected from CnCNet due to a socket error.");
                    errorTimes++;

                    if (errorTimes > MAX_RECONNECT_COUNT)
                    {
                        const string errorMessage = "Disconnected from CnCNet after reaching the maximum number of connection retries.";
                        Logger.Log(errorMessage);
                        failedServerIPs.Add(currentConnectedServerIP);
                        connectionManager.OnConnectionLost(errorMessage.L10N("UI:Main:ClientDisconnectedAfterRetries"));
                        break;
                    }

                    continue;
                }

                errorTimes = 0;

                // A message has been successfully received
#if NETFRAMEWORK
                string msg = encoding.GetString(message1, 0, bytesRead);
#else
                string msg = encoding.GetString(message.Span[..bytesRead]);
#endif

                Logger.Log("Message received: " + msg);

                await HandleMessageAsync(msg);
                timer.Interval = 30000;
            }

#if NETFRAMEWORK
            if (cancellationToken.IsCancellationRequested)
            {
                connectionManager.OnDisconnected();
                connectionCut = false; // This disconnect is intentional
            }

#endif
            timer.Enabled = false;
            timer.Dispose();

            _isConnected = false;

            if (connectionCut)
            {
                while (!sendQueueExited)
                    await Task.Delay(100);

                reconnectCount++;

                if (reconnectCount > MAX_RECONNECT_COUNT)
                {
                    Logger.Log("Reconnect attempt count exceeded!");
                    return;
                }

                await Task.Delay(RECONNECT_WAIT_DELAY);

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
        private async Task<IList<Server>> GetServerListSortedByLatencyAsync()
        {
            // Resolve the hostnames.
            IEnumerable<(IPAddress IpAddress, string Name, int[] Ports)>[] servers = await Task.WhenAll(Servers.Select(ResolveServerAsync));

            // Group the tuples by IPAddress to merge duplicate servers.
            IEnumerable<IGrouping<IPAddress, (string Name, int[] Ports)>> serverInfosGroupedByIPAddress = servers
                .SelectMany(server => server)
                .GroupBy(serverInfo => serverInfo.IpAddress, serverInfo => (serverInfo.Name, serverInfo.Ports));

            // Process each group:
            //   1. Get IPAddress.
            //   2. Concatenate serverNames.
            //   3. Remove duplicate ports.
            //   4. Construct and return a tuple that contains the IPAddress, concatenated serverNames and unique ports.
            (IPAddress IpAddress, string Name, int[] Ports)[] serverInfos = serverInfosGroupedByIPAddress.Select(serverInfoGroup =>
            {
                IPAddress ipAddress = serverInfoGroup.Key;
                string serverNames = string.Join(", ", serverInfoGroup.Select(serverInfo => serverInfo.Name));
                int[] serverPorts = serverInfoGroup.SelectMany(serverInfo => serverInfo.Ports).Distinct().ToArray();

                return (ipAddress, serverNames, serverPorts);
            }).ToArray();

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

            (Server Server, long Result)[] serverAndLatencyResults =
                await Task.WhenAll(serverInfos.Where(q => !failedServerIPs.Contains(q.IpAddress.ToString())).Select(PingServerAsync));

            // Sort the servers by latency.
            (Server Server, long Result)[] sortedServerAndLatencyResults = serverAndLatencyResults
                .Select(server => server)
                .OrderBy(taskResult => taskResult.Result)
                .ToArray();

            // Do logging.
            foreach ((Server server, long serverLatencyValue) in sortedServerAndLatencyResults)
            {
                string serverIPAddress = server.Host;
                string serverLatencyString = serverLatencyValue <= MAXIMUM_LATENCY ? serverLatencyValue.ToString() : "DNF";

                Logger.Log($"Lobby server IP: {serverIPAddress}, latency: {serverLatencyString}.");
            }

            int candidateCount = sortedServerAndLatencyResults.Length;
            int closerCount = sortedServerAndLatencyResults.Count(
                serverAndLatencyResult => serverAndLatencyResult.Item2 <= MAXIMUM_LATENCY);

            Logger.Log($"Lobby servers: {candidateCount} available, {closerCount} fast.");
            connectionManager.OnServerLatencyTested(candidateCount, closerCount);

            return sortedServerAndLatencyResults.Select(taskResult => taskResult.Server).ToList();
        }

        private static async Task<(Server Server, long Result)> PingServerAsync((IPAddress IpAddress, string Name, int[] Ports) serverInfo)
        {
            Logger.Log($"Attempting to ping {serverInfo.Name} ({serverInfo.IpAddress}).");
            var server = new Server(serverInfo.IpAddress.ToString(), serverInfo.Name, serverInfo.Ports);
            using var ping = new Ping();

            try
            {
                PingReply pingReply = await ping.SendPingAsync(serverInfo.IpAddress, MAXIMUM_LATENCY);

                if (pingReply.Status == IPStatus.Success)
                {
                    long pingInMs = pingReply.RoundtripTime;
                    Logger.Log($"The latency in milliseconds to the server {serverInfo.Name} ({serverInfo.IpAddress}): {pingInMs}.");

                    return (server, pingInMs);
                }

                Logger.Log($"Failed to ping the server {serverInfo.Name} ({serverInfo.IpAddress}): " +
                    $"{Enum.GetName(typeof(IPStatus), pingReply.Status)}.");

                return (server, long.MaxValue);
            }
            catch (PingException ex)
            {
                PreStartup.LogException(ex, $"Caught an exception when pinging {serverInfo.Name} ({serverInfo.IpAddress}) Lobby server.");

                return (server, long.MaxValue);
            }
        }

        private static async Task<IEnumerable<(IPAddress IpAddress, string Name, int[] Ports)>> ResolveServerAsync(Server server)
        {
            Logger.Log($"Attempting to DNS resolve {server.Name} ({server.Host}).");

            try
            {
                // If hostNameOrAddress is an IP address, this address is returned without querying the DNS server.
                IPAddress[] serverIPAddresses = (await Dns.GetHostAddressesAsync(server.Host))
                    .Where(IPAddress => IPAddress.AddressFamily is AddressFamily.InterNetworkV6 or AddressFamily.InterNetwork)
                    .ToArray();

                Logger.Log($"DNS resolved {server.Name} ({server.Host}): " +
                    $"{string.Join(", ", serverIPAddresses.Select(item => item.ToString()))}");

                // Store each IPAddress in a different tuple.
                return serverIPAddresses.Select(serverIPAddress => (serverIPAddress, server.Name, server.Ports));
            }
            catch (SocketException ex)
            {
                PreStartup.LogException(ex, $"Caught an exception when DNS resolving {server.Name} ({server.Host}) Lobby server.");
            }

            return Array.Empty<(IPAddress IpAddress, string Name, int[] Ports)>();
        }

        public async Task DisconnectAsync()
        {
            await SendMessageAsync("QUIT");
            cancellationTokenSource.Cancel();
            socket.Close();
        }

        #region Handling commands

        /// <summary>
        /// Checks if a message from the IRC server is a partial or full
        /// message, and handles it accordingly.
        /// </summary>
        /// <param name="message">The message.</param>
        private async Task HandleMessageAsync(string message)
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
                    string command = msg.Substring(0, commandEndIndex - 1);
                    await PerformCommandAsync(command);

                    msg = msg.Remove(0, commandEndIndex + 1);
                }
                else
                {
                    string command = msg.Substring(0, msg.Length - 1);
                    await PerformCommandAsync(command);
                    break;
                }
            }
        }

        /// <summary>
        /// Handles a specific command received from the IRC server.
        /// </summary>
        private async Task PerformCommandAsync(string message)
        {
            message = message.Replace("\r", string.Empty);
            ParseIrcMessage(message, out string prefix, out string command, out List<string> parameters);
            string paramString = string.Empty;
            foreach (string param in parameters) { paramString = paramString + param + ","; }
            Logger.Log("RMP: " + prefix + " " + command + " " + paramString);

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
                            await RegisterAsync();
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
                    case "NOTICE":
                        int noticeExclamIndex = prefix.IndexOf('!');
                        if (noticeExclamIndex > -1)
                        {
                            if (parameters.Count > 1 && parameters[1][0] == 1)//Conversions.IntFromString(parameters[1].Substring(0, 1), -1) == 1)
                            {
                                // CTCP
                                string channelName = parameters[0];
                                string ctcpMessage = parameters[1];
                                ctcpMessage = ctcpMessage.Remove(0, 1).Remove(ctcpMessage.Length - 2);
                                string ctcpSender = prefix.Substring(0, noticeExclamIndex);
                                connectionManager.OnCTCPParsed(channelName, ctcpSender, ctcpMessage);

                                return;
                            }
                            else
                            {
                                string noticeUserName = prefix.Substring(0, noticeExclamIndex);
                                string notice = parameters[parameters.Count - 1];
                                connectionManager.OnNoticeMessageParsed(notice, noticeUserName);
                                break;
                            }
                        }
                        string noticeParamString = string.Empty;
                        foreach (string param in parameters)
                            noticeParamString = noticeParamString + param + " ";
                        connectionManager.OnGenericServerMessageReceived(prefix + " " + noticeParamString);
                        break;
                    case "JOIN":
                        string channel = parameters[0];
                        int atIndex = prefix.IndexOf('@');
                        int exclamIndex = prefix.IndexOf('!');
                        string userName = prefix.Substring(0, exclamIndex);
                        string ident = prefix.Substring(exclamIndex + 1, atIndex - (exclamIndex + 1));
                        string host = prefix.Substring(atIndex + 1);
                        connectionManager.OnUserJoinedChannel(channel, host, userName, ident);
                        break;
                    case "PART":
                        string pChannel = parameters[0];
                        string pUserName = prefix.Substring(0, prefix.IndexOf('!'));
                        connectionManager.OnUserLeftChannel(pChannel, pUserName);
                        break;
                    case "QUIT":
                        string qUserName = prefix.Substring(0, prefix.IndexOf('!'));
                        connectionManager.OnUserQuitIRC(qUserName);
                        break;
                    case "PRIVMSG":
                        if (parameters.Count > 1 && Convert.ToInt32(parameters[1][0]) == 1 && !parameters[1].Contains("ACTION"))
                        {
                            goto case "NOTICE";
                        }
                        string pmsgUserName = prefix.Substring(0, prefix.IndexOf('!'));
                        string pmsgIdent = GetIdentFromPrefix(prefix);
                        string[] recipients = new string[parameters.Count - 1];
                        for (int pid = 0; pid < parameters.Count - 1; pid++)
                            recipients[pid] = parameters[pid];
                        string privmsg = parameters[parameters.Count - 1];
                        if (parameters[1].StartsWith('\u0001' + "ACTION"))
                            privmsg = privmsg.Substring(1).Remove(privmsg.Length - 2);
                        foreach (string recipient in recipients)
                        {
                            if (recipient.StartsWith("#"))
                                connectionManager.OnChatMessageReceived(recipient, pmsgUserName, pmsgIdent, privmsg);
                            else if (recipient == ProgramConstants.PLAYERNAME)
                                connectionManager.OnPrivateMessageReceived(pmsgUserName, privmsg);
                        }
                        break;
                    case "MODE":
                        string modeUserName = prefix.Substring(0, prefix.IndexOf('!'));
                        string modeChannelName = parameters[0];
                        string modeString = parameters[1];
                        List<string> modeParameters =
                            parameters.Count > 2 ? parameters.GetRange(2, parameters.Count - 2) : new List<string>();
                        connectionManager.OnChannelModesChanged(modeUserName, modeChannelName, modeString, modeParameters);
                        break;
                    case "KICK":
                        string kickChannelName = parameters[0];
                        string kickUserName = parameters[1];
                        connectionManager.OnUserKicked(kickChannelName, kickUserName);
                        break;
                    case "ERROR":
                        connectionManager.OnErrorReceived(message);
                        break;
                    case "PING":
                        if (parameters.Count > 0)
                        {
                            await QueueMessageAsync(new QueuedMessage("PONG " + parameters[0], QueuedMessageType.SYSTEM_MESSAGE, 5000));
                            Logger.Log("PONG " + parameters[0]);
                        }
                        else
                        {
                            await QueueMessageAsync(new QueuedMessage("PONG", QueuedMessageType.SYSTEM_MESSAGE, 5000));
                            Logger.Log("PONG");
                        }
                        break;
                    case "TOPIC":
                        if (parameters.Count < 2)
                            break;

                        connectionManager.OnChannelTopicChanged(prefix.Substring(0, prefix.IndexOf('!')),
                            parameters[0], parameters[1]);
                        break;
                    case "NICK":
                        int nickExclamIndex = prefix.IndexOf('!');
                        if (nickExclamIndex > -1 || parameters.Count < 1)
                        {
                            string oldNick = prefix.Substring(0, nickExclamIndex);
                            string newNick = parameters[0];
                            Logger.Log("Nick change - " + oldNick + " -> " + newNick);
                            connectionManager.OnUserNicknameChange(oldNick, newNick);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                PreStartup.LogException(ex, "Warning: Failed to parse command " + message);
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
                trailing = message.Substring(trailingStart + 2);
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

        private async Task RunSendQueueAsync(CancellationToken cancellationToken)
        {
            try
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        string message = string.Empty;

                        await messageQueueLocker.WaitAsync(cancellationToken);

                        try
                        {
                            for (int i = 0; i < MessageQueue.Count; i++)
                            {
                                QueuedMessage qm = MessageQueue[i];
                                if (qm.Delay > 0)
                                {
                                    if (qm.SendAt < DateTime.Now)
                                    {
                                        message = qm.Command;

                                        Logger.Log("Delayed message sent: " + qm.ID);

                                        MessageQueue.RemoveAt(i);
                                        break;
                                    }
                                }
                                else
                                {
                                    message = qm.Command;
                                    MessageQueue.RemoveAt(i);
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
                            await Task.Delay(10, cancellationToken);
                            continue;
                        }

                        await SendMessageAsync(message);
                        await Task.Delay(MessageQueueDelay, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    await messageQueueLocker.WaitAsync(CancellationToken.None);

                    try
                    {
                        MessageQueue.Clear();
                    }
                    finally
                    {
                        messageQueueLocker.Release();
                    }

                    sendQueueExited = true;
                }
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }

        /// <summary>
        /// Sends a PING message to the server to indicate that we're still connected.
        /// </summary>
        private async Task AutoPingAsync()
        {
            try
            {
                await SendMessageAsync("PING LAG" + new Random().Next(100000, 999999));
            }
            catch (Exception ex)
            {
                PreStartup.HandleException(ex);
            }
        }

        /// <summary>
        /// Registers the user.
        /// </summary>
        private async Task RegisterAsync()
        {
            if (welcomeMessageReceived)
                return;

            Logger.Log("Registering.");

            var defaultGame = ClientConfiguration.Instance.LocalGame;

            string realname = ProgramConstants.GAME_VERSION + " " + defaultGame + " CnCNet";

            await SendMessageAsync(string.Format("USER {0} 0 * :{1}", defaultGame + "." +
                systemId, realname));

            await SendMessageAsync("NICK " + ProgramConstants.PLAYERNAME);
        }

        public Task ChangeNicknameAsync()
        {
            return SendMessageAsync("NICK " + ProgramConstants.PLAYERNAME);
        }

        public Task QueueMessageAsync(QueuedMessageType type, int priority, string message, bool replace = false)
        {
            QueuedMessage qm = new QueuedMessage(message, type, priority, replace);
            return QueueMessageAsync(qm);
        }

        public async Task QueueMessageAsync(QueuedMessageType type, int priority, int delay, string message)
        {
            QueuedMessage qm = new QueuedMessage(message, type, priority, delay);
            await QueueMessageAsync(qm);
            Logger.Log("Setting delay to " + delay + "ms for " + qm.ID);
        }

        /// <summary>
        /// Send a message to the CnCNet server.
        /// </summary>
        /// <param name="message">The message to send.</param>
        private async Task SendMessageAsync(string message)
        {
            if (!socket?.Connected ?? false)
                return;

            Logger.Log("SRM: " + message);

#if NETFRAMEWORK
            byte[] buffer1 = encoding.GetBytes(message + "\r\n");
            var buffer = new ArraySegment<byte>(buffer1);

            try
            {
                await socket.SendAsync(buffer, SocketFlags.None);
#else
            using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(message.Length * 2);
            Memory<byte> buffer = memoryOwner.Memory[..(message.Length * 2)];
            int bytes = encoding.GetBytes((message + "\r\n").AsSpan(), buffer.Span);
            buffer = buffer[..bytes];

            try
            {
                await socket.SendAsync(buffer, SocketFlags.None, CancellationToken.None);
#endif
            }
            catch (IOException ex)
            {
                PreStartup.LogException(ex, "Sending message to the server failed!");
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
                var previousMessageIndex = MessageQueue.FindIndex(m => m.MessageType == qm.MessageType);
                if (previousMessageIndex == -1)
                    return false;

                MessageQueue[previousMessageIndex] = qm;
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
        public async Task QueueMessageAsync(QueuedMessage qm)
        {
            if (!_isConnected)
                return;

            if (qm.Replace && ReplaceMessage(qm))
                return;

            qm.ID = NextQueueID++;

            await messageQueueLocker.WaitAsync();

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
                        await SendMessageAsync(qm.Command);
                        break;
                    default:
                        int placeInQueue = MessageQueue.FindIndex(m => m.Priority < qm.Priority);
                        if (ProgramConstants.LOG_LEVEL > 1)
                            Logger.Log("QM Undefined: " + qm.Command + " " + placeInQueue);
                        if (placeInQueue == -1)
                            MessageQueue.Add(qm);
                        else
                            MessageQueue.Insert(placeInQueue, qm);
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
            int broadcastingMessageIndex = MessageQueue.FindIndex(m => m.MessageType == qm.MessageType);

            qm.ID = NextQueueID++;

            if (broadcastingMessageIndex > -1)
            {
                if (ProgramConstants.LOG_LEVEL > 1)
                    Logger.Log("QM Replace: " + qm.Command + " " + broadcastingMessageIndex);
                MessageQueue[broadcastingMessageIndex] = qm;
            }
            else
            {
                int placeInQueue = MessageQueue.FindIndex(m => m.Priority < qm.Priority);
                if (ProgramConstants.LOG_LEVEL > 1)
                    Logger.Log("QM: " + qm.Command + " " + placeInQueue);
                if (placeInQueue == -1)
                    MessageQueue.Add(qm);
                else
                    MessageQueue.Insert(placeInQueue, qm);
            }
        }

        #endregion
    }
}