using ClientCore;
using Localization;
using Rampastring.Tools;
using System;
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

        IConnectionManager connectionManager;

        /// <summary>
        /// The list of CnCNet / GameSurge IRC servers to connect to.
        /// </summary>
        private static readonly IList<Server> Servers = new List<Server>
        {
            new Server("Burstfire.UK.EU.GameSurge.net", "GameSurge London, UK", new int[3] { 6667, 6668, 7000 }),
            new Server("ColoCrossing.IL.US.GameSurge.net", "GameSurge Chicago, IL", new int[5] { 6660, 6666, 6667, 6668, 6669 }),
            new Server("Gameservers.NJ.US.GameSurge.net", "GameSurge Newark, NJ", new int[7] { 6665, 6666, 6667, 6668, 6669, 7000, 8080 }),
            new Server("Krypt.CA.US.GameSurge.net", "GameSurge Santa Ana, CA", new int[4] { 6666, 6667, 6668, 6669 }),
            new Server("NuclearFallout.WA.US.GameSurge.net", "GameSurge Seattle, WA", new int[2] { 6667, 5960 }),
            new Server("Portlane.SE.EU.GameSurge.net", "GameSurge Stockholm, Sweden", new int[5] { 6660, 6666, 6667, 6668, 6669 }),
            new Server("Prothid.NY.US.GameSurge.Net", "GameSurge NYC, NY", new int[7] { 5960, 6660, 6666, 6667, 6668, 6669, 6697 }),
            new Server("TAL.DE.EU.GameSurge.net", "GameSurge Wuppertal, Germany", new int[5] { 6660, 6666, 6667, 6668, 6669 }),
            new Server("208.167.237.120", "GameSurge IP 208.167.237.120", new int[7] {  6660, 6666, 6667, 6668, 6669, 7000, 8080 }),
            new Server("192.223.27.109", "GameSurge IP 192.223.27.109", new int[7] {  6660, 6666, 6667, 6668, 6669, 7000, 8080 }),
            new Server("108.174.48.100", "GameSurge IP 108.174.48.100", new int[7] { 6660, 6666, 6667, 6668, 6669, 7000, 8080 }),
            new Server("208.146.35.105", "GameSurge IP 208.146.35.105", new int[7] { 6660, 6666, 6667, 6668, 6669, 7000, 8080 }),
            new Server("195.8.250.180", "GameSurge IP 195.8.250.180", new int[7] { 6660, 6666, 6667, 6668, 6669, 7000, 8080 }),
            new Server("91.217.189.76", "GameSurge IP 91.217.189.76", new int[7] { 6660, 6666, 6667, 6668, 6669, 7000, 8080 }),
            new Server("195.68.206.250", "GameSurge IP 195.68.206.250", new int[7] { 6660, 6666, 6667, 6668, 6669, 7000, 8080 }),
            new Server("irc.gamesurge.net", "GameSurge", new int[1] { 6667 }),
        }.AsReadOnly();

        bool _isConnected = false;
        public bool IsConnected
        {
            get { return _isConnected; }
        }

        bool _attemptingConnection = false;
        public bool AttemptingConnection
        {
            get { return _attemptingConnection; }
        }

        Random _rng = new Random();
        public Random Rng
        {
            get { return _rng; }
        }

        private List<QueuedMessage> MessageQueue = new List<QueuedMessage>();
        private TimeSpan MessageQueueDelay;

        private NetworkStream serverStream;
        private TcpClient tcpClient;

        volatile int reconnectCount = 0;

        private volatile bool connectionCut = false;
        private volatile bool welcomeMessageReceived = false;
        private volatile bool sendQueueExited = false;
        bool _disconnect = false;
        private bool disconnect
        {
            get
            {
                lock (locker)
                    return _disconnect;
            }
            set
            {
                lock (locker)
                    _disconnect = value;
            }
        }

        private string overMessage;

        private readonly Encoding encoding = Encoding.UTF8;

        /// <summary>
        /// A list of server IPs that have dropped our connection.
        /// The client skips these servers when attempting to re-connect, to
        /// prevent a server that first accepts a connection and then drops it
        /// right afterwards from preventing online play.
        /// </summary>
        private readonly List<string> failedServerIPs = new List<string>();
        private volatile string currentConnectedServerIP;

        private static readonly object locker = new object();
        private static readonly object messageQueueLocker = new object();

        private static bool idSet = false;
        private static string systemId;
        private static readonly object idLocker = new object();

        public static void SetId(string id)
        {
            lock (idLocker)
            {
                int maxLength = ID_LENGTH - (ClientConfiguration.Instance.LocalGame.Length + 1);
                systemId = Utilities.CalculateSHA1ForString(id).Substring(0, maxLength);
                idSet = true;
            }
        }

        public static bool IsIdSet()
        {
            lock (idLocker)
            {
                return idSet;
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
            disconnect = false;

            MessageQueueDelay = TimeSpan.FromMilliseconds(ClientConfiguration.Instance.SendSleep);

            Thread connection = new Thread(ConnectToServer);
            connection.Start();
        }

        /// <summary>
        /// Attempts to connect to CnCNet.
        /// </summary>
        private void ConnectToServer()
        {
            IList<Server> sortedServerList = GetServerListSortedByLatency();

            foreach (Server server in sortedServerList)
            {
                try
                {
                    for (int i = 0; i < server.Ports.Length; i++)
                    {
                        connectionManager.OnAttemptedServerChanged(server.Name);

                        TcpClient client = new TcpClient(AddressFamily.InterNetwork);
                        var result = client.BeginConnect(server.Host, server.Ports[i], null, null);
                        result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3), false);

                        Logger.Log("Attempting connection to " + server.Host + ":" + server.Ports[i]);

                        if (!client.Connected)
                        {
                            Logger.Log("Connecting to " + server.Host + " port " + server.Ports[i] + " timed out!");
                            continue; // Start all over again, using the next port
                        }
                        else if (client.Connected)
                        {
                            Logger.Log("Succesfully connected to " + server.Host + " on port " + server.Ports[i]);
                            client.EndConnect(result);

                            _isConnected = true;
                            _attemptingConnection = false;

                            connectionManager.OnConnected();

                            Thread sendQueueHandler = new Thread(RunSendQueue);
                            sendQueueHandler.Start();

                            tcpClient = client;
                            serverStream = tcpClient.GetStream();
                            serverStream.ReadTimeout = 1000;

                            currentConnectedServerIP = server.Host;
                            HandleComm(client);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Unable to connect to the server. " + ex.Message);
                }
            }

            Logger.Log("Connecting to CnCNet failed!");
            // Clear the failed server list in case connecting to all servers has failed
            failedServerIPs.Clear();
            _attemptingConnection = false;
            connectionManager.OnConnectAttemptFailed();
        }

        private void HandleComm(object client)
        {
            int errorTimes = 0;

            byte[] message = new byte[1024];
            int bytesRead;

            Register();

            Timer timer = new Timer(new TimerCallback(AutoPing), null, 30000, 120000);

            connectionCut = true;

            while (true)
            {
                bytesRead = 0;

                if (connectionManager.GetDisconnectStatus())
                {
                    connectionManager.OnDisconnected();
                    connectionCut = false; // This disconnect is intentional
                    break;
                }

                try
                {
                    bytesRead = serverStream.Read(message, 0, 1024);
                }
                catch (Exception ex)
                {
                    errorTimes++;

                    if (errorTimes > 30) // TODO Figure out if this hacky check is actually necessary
                    {
                        Logger.Log("Disconnected from CnCNet due to a socket error. Message: " + ex.Message);
                        failedServerIPs.Add(currentConnectedServerIP);
                        connectionManager.OnConnectionLost(ex.Message);
                        break;
                    }
                    else if (connectionManager.GetDisconnectStatus())
                    {
                        connectionManager.OnDisconnected();
                        connectionCut = false; // This disconnect is intentional
                        break;
                    }

                    continue;
                }

                if (bytesRead == 0)
                {
                    errorTimes++;

                    if (errorTimes > 30) // TODO Figure out if this hacky check is actually necessary
                    {
                        failedServerIPs.Add(currentConnectedServerIP);
                        Logger.Log("Disconnected from CnCNet.");
                        connectionManager.OnConnectionLost("Server disconnected.".L10N("UI:Main:ServerDisconnected"));
                        break;
                    }

                    continue;
                }

                errorTimes = 0;

                // A message has been succesfully received
                string msg = encoding.GetString(message, 0, bytesRead);
                Logger.Log("Message received: " + msg);

                HandleMessage(msg);
                timer.Change(30000, 30000);
            }

            timer.Change(Timeout.Infinite, Timeout.Infinite);
            timer.Dispose();

            _isConnected = false;
            disconnect = false;

            if (connectionCut)
            {
                while (!sendQueueExited)
                    Thread.Sleep(100);

                reconnectCount++;

                if (reconnectCount > MAX_RECONNECT_COUNT)
                {
                    Logger.Log("Reconnect attempt count exceeded!");
                    return;
                }

                Thread.Sleep(RECONNECT_WAIT_DELAY);

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
        private IList<Server> GetServerListSortedByLatency()
        {
            // Resolve the hostnames.
            ICollection<Task<IEnumerable<Tuple<IPAddress, string, int[]>>>>
                dnsTasks = new List<Task<IEnumerable<Tuple<IPAddress, string, int[]>>>>(Servers.Count);

            foreach (Server server in Servers)
            {
                string serverHostnameOrIPAddress = server.Host;
                string serverName = server.Name;
                int[] serverPorts = server.Ports;

                Task<IEnumerable<Tuple<IPAddress, string, int[]>>> dnsTask = new Task<IEnumerable<Tuple<IPAddress, string, int[]>>>(() =>
                {
                    Logger.Log($"Attempting to DNS resolve {serverName} ({serverHostnameOrIPAddress}).");
                    ICollection<Tuple<IPAddress, string, int[]>> _serverInfos = new List<Tuple<IPAddress, string, int[]>>();

                    try
                    {
                        // If hostNameOrAddress is an IP address, this address is returned without querying the DNS server.
                        IEnumerable<IPAddress> serverIPAddresses = Dns.GetHostAddresses(serverHostnameOrIPAddress)
                                                                      .Where(IPAddress => IPAddress.AddressFamily == AddressFamily.InterNetwork);

                        Logger.Log($"DNS resolved {serverName} ({serverHostnameOrIPAddress}): " +
                            $"{string.Join(", ", serverIPAddresses.Select(item => item.ToString()))}");

                        // Store each IPAddress in a different tuple.
                        foreach (IPAddress serverIPAddress in serverIPAddresses)
                        {
                            _serverInfos.Add(new Tuple<IPAddress, string, int[]>(serverIPAddress, serverName, serverPorts));
                        }
                    }
                    catch (SocketException ex)
                    {
                        Logger.Log($"Caught an exception when DNS resolving {serverName} ({serverHostnameOrIPAddress}) Lobby server: {ex.Message}");
                    }

                    return _serverInfos;
                });

                dnsTask.Start();
                dnsTasks.Add(dnsTask);
            }

            Task.WaitAll(dnsTasks.ToArray());

            // Group the tuples by IPAddress to merge duplicate servers.
            IEnumerable<IGrouping<IPAddress, Tuple<string, int[]>>>
                serverInfosGroupedByIPAddress = dnsTasks.SelectMany(dnsTask => dnsTask.Result)      // Tuple<IPAddress, serverName, serverPorts>
                                                        .GroupBy(
                                                            serverInfo => serverInfo.Item1,         // IPAddress
                                                            serverInfo => new Tuple<string, int[]>(
                                                                serverInfo.Item2,                   // serverName
                                                                serverInfo.Item3                    // serverPorts
                                                            )
                                                        );

            // Process each group:
            //   1. Get IPAddress.
            //   2. Concatenate serverNames.
            //   3. Remove duplicate ports.
            //   4. Construct and return a tuple that contains the IPAddress, concatenated serverNames and unique ports.
            IEnumerable<Tuple<IPAddress, string, int[]>> serverInfos = serverInfosGroupedByIPAddress.Select(serverInfoGroup =>
            {
                IPAddress ipAddress = serverInfoGroup.Key;
                string serverNames = string.Join(", ", serverInfoGroup.Select(serverInfo => serverInfo.Item1));
                int[] serverPorts = serverInfoGroup.SelectMany(serverInfo => serverInfo.Item2).Distinct().ToArray();

                return new Tuple<IPAddress, string, int[]>(ipAddress, serverNames, serverPorts);
            });

            // Do logging.
            foreach (Tuple<IPAddress, string, int[]> serverInfo in serverInfos)
            {
                string serverIPAddress = serverInfo.Item1.ToString();
                string serverNames = string.Join(", ", serverInfo.Item2.ToString());
                string serverPorts = string.Join(", ", serverInfo.Item3.Select(port => port.ToString()));

                Logger.Log($"Got a Lobby server. IP: {serverIPAddress}; Name: {serverNames}; Ports: {serverPorts}.");
            }

            Logger.Log($"The number of Lobby servers is {serverInfos.Count()}.");

            // Test the latency.
            ICollection<Task<Tuple<Server, long>>> pingTasks = new List<Task<Tuple<Server, long>>>(serverInfos.Count());

            foreach (Tuple<IPAddress, string, int[]> serverInfo in serverInfos)
            {
                IPAddress serverIPAddress = serverInfo.Item1;
                string serverNames = serverInfo.Item2;
                int[] serverPorts = serverInfo.Item3;

                if (failedServerIPs.Contains(serverIPAddress.ToString()))
                {
                    Logger.Log($"Skipped a failed server {serverNames} ({serverIPAddress}).");
                    continue;
                }

                Task<Tuple<Server, long>> pingTask = new Task<Tuple<Server, long>>(() =>
                {
                    Logger.Log($"Attempting to ping {serverNames} ({serverIPAddress}).");
                    Server server = new Server(serverIPAddress.ToString(), serverNames, serverPorts);

                    using (Ping ping = new Ping())
                    {
                        try
                        {
                            PingReply pingReply = ping.Send(serverIPAddress, MAXIMUM_LATENCY);

                            if (pingReply.Status == IPStatus.Success)
                            {
                                long pingInMs = pingReply.RoundtripTime;
                                Logger.Log($"The latency in milliseconds to the server {serverNames} ({serverIPAddress}): {pingInMs}.");

                                return new Tuple<Server, long>(server, pingInMs);
                            }
                            else
                            {
                                Logger.Log($"Failed to ping the server {serverNames} ({serverIPAddress}): " +
                                    $"{Enum.GetName(typeof(IPStatus), pingReply.Status)}.");

                                return new Tuple<Server, long>(server, long.MaxValue);
                            }
                        }
                        catch (PingException ex)
                        {
                            Logger.Log($"Caught an exception when pinging {serverNames} ({serverIPAddress}) Lobby server: {ex.Message}");

                            return new Tuple<Server, long>(server, long.MaxValue);
                        }
                    }
                });

                pingTask.Start();
                pingTasks.Add(pingTask);
            }

            Task.WaitAll(pingTasks.ToArray());

            // Sort the servers by latency.
            IOrderedEnumerable<Tuple<Server, long>>
                sortedServerAndLatencyResults = pingTasks.Select(task => task.Result)              // Tuple<Server, Latency>
                                                         .OrderBy(taskResult => taskResult.Item2); // Latency

            // Do logging.
            foreach (Tuple<Server, long> serverAndLatencyResult in sortedServerAndLatencyResults)
            {
                string serverIPAddress = serverAndLatencyResult.Item1.Host;
                long serverLatencyValue = serverAndLatencyResult.Item2;
                string serverLatencyString = serverLatencyValue <= MAXIMUM_LATENCY ? serverLatencyValue.ToString() : "DNF";

                Logger.Log($"Lobby server IP: {serverIPAddress}, latency: {serverLatencyString}.");
            }

            return sortedServerAndLatencyResults.Select(taskResult => taskResult.Item1).ToList(); // Server
        }

        public void Disconnect()
        {
            disconnect = true;
            SendMessage("QUIT");

            tcpClient.Close();
            serverStream.Close();
        }

        #region Handling commands

        /// <summary>
        /// Checks if a message from the IRC server is a partial or full
        /// message, and handles it accordingly.
        /// </summary>
        /// <param name="message">The message.</param>
        private void HandleMessage(string message)
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
                    PerformCommand(command);

                    msg = msg.Remove(0, commandEndIndex + 1);
                }
                else
                {
                    string command = msg.Substring(0, msg.Length - 1);
                    PerformCommand(command);
                    break;
                }
            }
        }

        /// <summary>
        /// Handles a specific command received from the IRC server.
        /// </summary>
        private void PerformCommand(string message)
        {
            string prefix = String.Empty;
            string command = String.Empty;
            message = message.Replace("\r", String.Empty);
            List<string> parameters = new List<string>();
            ParseIrcMessage(message, out prefix, out command, out parameters);
            string paramString = String.Empty;
            foreach (string param in parameters) { paramString = paramString + param + ","; }
            Logger.Log("RMP: " + prefix + " " + command + " " + paramString);

            try
            {
                bool success = false;
                int commandNumber = -1;
                success = Int32.TryParse(command, out commandNumber);

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
                            Register();
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
                        string noticeParamString = String.Empty;
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
                        if (parameters[1].StartsWith('\u0001'.ToString() + "ACTION"))
                            privmsg = privmsg.Substring(1).Remove(privmsg.Length - 2);
                        foreach (string recipient in recipients)
                        {
                            if (recipient.StartsWith("#"))
                                connectionManager.OnChatMessageReceived(recipient, pmsgUserName, pmsgIdent, privmsg);
                            else if (recipient == ProgramConstants.PLAYERNAME)
                                connectionManager.OnPrivateMessageReceived(pmsgUserName, privmsg);
                            //else if (pmsgUserName == ProgramConstants.PLAYERNAME)
                            //{
                            //    DoPrivateMessageSent(privmsg, recipient);
                            //}
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
                            QueueMessage(new QueuedMessage("PONG " + parameters[0], QueuedMessageType.SYSTEM_MESSAGE, 5000));
                            Logger.Log("PONG " + parameters[0]);
                        }
                        else
                        {
                            QueueMessage(new QueuedMessage("PONG", QueuedMessageType.SYSTEM_MESSAGE, 5000));
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
            catch
            {
                Logger.Log("Warning: Failed to parse command " + message);
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
            prefix = command = String.Empty;
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
                command = String.Empty;
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

        private void RunSendQueue()
        {
            while (_isConnected)
            {
                string message = String.Empty;

                lock (messageQueueLocker)
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

                if (String.IsNullOrEmpty(message))
                {
                    Thread.Sleep(10);
                    continue;
                }

                SendMessage(message);

                Thread.Sleep(MessageQueueDelay);
            }

            lock (messageQueueLocker)
            {
                MessageQueue.Clear();
            }

            sendQueueExited = true;
        }

        /// <summary>
        /// Sends a PING message to the server to indicate that we're still connected.
        /// </summary>
        /// <param name="data">Just a dummy parameter so that this matches the delegate System.Threading.TimerCallback.</param>
        private void AutoPing(object data)
        {
            SendMessage("PING LAG" + new Random().Next(100000, 999999));
        }

        /// <summary>
        /// Registers the user.
        /// </summary>
        private void Register()
        {
            if (welcomeMessageReceived)
                return;

            Logger.Log("Registering.");

            var defaultGame = ClientConfiguration.Instance.LocalGame;

            string realname = ProgramConstants.GAME_VERSION + " " + defaultGame + " CnCNet";

            SendMessage(string.Format("USER {0} 0 * :{1}", defaultGame + "." +
                systemId, realname));

            SendMessage("NICK " + ProgramConstants.PLAYERNAME);
        }

        public void ChangeNickname()
        {
            SendMessage("NICK " + ProgramConstants.PLAYERNAME);
        }

        public void QueueMessage(QueuedMessageType type, int priority, string message, bool replace = false)
        {
            QueuedMessage qm = new QueuedMessage(message, type, priority, replace);
            QueueMessage(qm);
        }

        public void QueueMessage(QueuedMessageType type, int priority, int delay, string message)
        {
            QueuedMessage qm = new QueuedMessage(message, type, priority, delay);
            QueueMessage(qm);
            Logger.Log("Setting delay to " + delay + "ms for " + qm.ID);
        }

        /// <summary>
        /// Send a message to the CnCNet server.
        /// </summary>
        /// <param name="message">The message to send.</param>
        private void SendMessage(string message)
        {
            if (serverStream == null)
                return;

            Logger.Log("SRM: " + message);

            byte[] buffer = encoding.GetBytes(message + "\r\n");
            if (serverStream.CanWrite)
            {
                try
                {
                    serverStream.Write(buffer, 0, buffer.Length);
                    serverStream.Flush();
                }
                catch (IOException ex)
                {
                    Logger.Log("Sending message to the server failed! Reason: " + ex.Message);
                }
            }
        }

        private int NextQueueID { get; set; } = 0;

        /// <summary>
        /// This will attempt to replace a previously queued message of the same type.
        /// </summary>
        /// <param name="qm">The new message to replace with</param>
        /// <returns>Whether or not a replace occurred</returns>
        private bool ReplaceMessage(QueuedMessage qm)
        {
            lock (messageQueueLocker)
            {
                var previousMessageIndex = MessageQueue.FindIndex(m => m.MessageType == qm.MessageType);
                if (previousMessageIndex == -1)
                    return false;
                
                MessageQueue[previousMessageIndex] = qm;
                return true;
            }
        }

        /// <summary>
        /// Adds a message to the send queue.
        /// </summary>
        /// <param name="qm">The message to queue.</param>
        /// <param name="replace">If true, attempt to replace a previous message of the same type</param>
        public void QueueMessage(QueuedMessage qm)
        {
            if (!_isConnected)
                return;

            if (qm.Replace && ReplaceMessage(qm))
                return;

            qm.ID = NextQueueID++;

            lock (messageQueueLocker)
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
                        SendMessage(qm.Command);
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
