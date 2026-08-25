#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Represents a parsed UDP packet exchanged through a V3 tunnel.
/// </summary>
public readonly struct ParsedPacket
{
    public uint SenderId { get; init; }
    public uint ReceiverId { get; init; }
    public TunnelPacketType? NegotiationType { get; init; }
    public ReadOnlyMemory<byte> Payload { get; init; }
}

/// <summary>
/// Types of packets exchanged between local and remote tunnels/players
/// during V3 tunnel negotiation or game.
/// </summary>
public enum TunnelPacketType : byte
{
    Connected = 0x01,
    PingRequest = 0x02,
    PingResponse = 0x03,
    TunnelChoice = 0x04,
    TunnelAck = 0x05,
    NegotiationFailed = 0x06,
    Register = 0x07,
    GameData = 0x08,
    P2PInfo = 0x09,    // payload: 4 bytes IPv4 + 2 bytes port (big-endian) — "I have P2P"
    P2PDecline = 0x0A, // payload: empty — "I don't use P2P"

    // Lobby keepalive round trip. The ping payload is 8 bytes of the sender's Stopwatch
    // timestamp; the pong echoes it back so the sender computes RTT statelessly. These are
    // deliberately distinct from PingRequest/PingResponse: reusing negotiation pings would
    // set PingRequestReceived on the receiver and weaken the stale-TunnelChoice guard.
    KeepAlivePing = 0x0B,
    KeepAlivePong = 0x0C,

    // Pre-launch connectivity check. The host sends ProbeRequest to each peer over their
    // negotiated path; the receiver probes its own keepalive targets and answers with
    // ProbeReport, whose payload is 4 bytes (little-endian V3 ID) per unresponsive peer
    // (empty payload = all reachable).
    ProbeRequest = 0x0D,
    ProbeReport = 0x0E,

    // Matchmaking phase, exchanged over the matchmaking servers before a pair negotiates.
    // TunnelList advertises every relay tunnel the sender knows, as a chunk of
    // {4-byte tunnel key, 1-byte ping, 1-byte flags} entries behind a
    // {format version, chunk index, chunk count} header.
    // TunnelSet is the decider's answer: the agreed shortlist, as bare 4-byte tunnel keys in
    // preference order behind a format version byte. See TunnelShortlist for the encoding.
    TunnelList = 0x0F,
    TunnelSet = 0x10
}

/// <summary>
/// Delegate for handling incoming packets.
/// </summary>
/// <param name="senderId">The sender's V3PlayerInfo ID.</param>
/// <param name="receiverId">The receiver's V3PlayerInfoID (0 for register).</param>
/// <param name="packetType">The type of the tunnel packet.</param>
/// <param name="payload">The raw payload data of the packet.</param>
/// <param name="receivedTime">Stopwatch ticks when received.</param>
/// <param name="tunnel">The tunnel from which the packet was received.</param>
public delegate void PacketHandler(uint senderId, uint receiverId,
    TunnelPacketType packetType, ReadOnlyMemory<byte> payload, long receivedTime, CnCNetTunnel tunnel);

/// <summary>
/// Manages UDP communication with V3 tunnel servers.
/// Handles registration, negotiation packets, and forwarding of
/// game data between players through tunnels.
/// </summary>
public class V3TunnelCommunicator
{
    private static readonly byte[] MAGIC_BYTES = [(byte)'C', (byte)'N', (byte)'C', (byte)'N', (byte)'E', (byte)'T']; // CNCNET

    // Maximum size of a single UDP datagram payload, used to size the receive buffer.
    private const int MAX_DATAGRAM_SIZE = 65507;

    private UdpClient? _udpClient;
    private Thread? _receiveThread;
    private volatile bool _running;
    private readonly ConcurrentDictionary<IPEndPoint, CnCNetTunnel> _endpointToTunnel = new();
    private readonly ConcurrentDictionary<CnCNetTunnel, IPEndPoint> _tunnelToEndpoint = new();
    private readonly ConcurrentDictionary<(uint localId, uint remoteId), PacketHandler> _handlers = new();
    private readonly ConcurrentDictionary<IPEndPoint, TaskCompletionSource<byte[]>> _pendingStunQueries = new();
    private readonly ConcurrentDictionary<(uint localId, uint remoteId), string> _p2pPeerNames = new();
    // Every P2P endpoint (advertised and auto-learned) registered for a pair, so
    // CleanupP2PPair can remove a pair's routing entries at the right lifecycle points.
    private readonly ConcurrentDictionary<(uint localId, uint remoteId), ConcurrentDictionary<IPEndPoint, byte>> _p2pEndpointsByPair = new();
    private readonly object _initLock = new();

    public bool IsInitialized => _udpClient != null;

    /// <summary>
    /// The local UDP port the communicator is bound to. P2P candidates must use this
    /// port so the LAN/reflexive endpoints map to the same socket used for game data.
    /// Returns 0 if not initialized.
    /// </summary>
    public int LocalPort => _udpClient != null ? ((IPEndPoint)_udpClient.Client.LocalEndPoint!).Port : 0;

    /// <summary>
    /// Initializes the communicator with the provided V3-compatible tunnels,
    /// sets up UDP socket, and starts the background receive thread.
    /// </summary>
    public void Initialize(List<CnCNetTunnel> tunnels)
    {
        lock (_initLock)
        {
            if (IsInitialized)
                return;

            var v3Tunnels = tunnels.Where(IsV3RelayTunnel).ToList();

            if (v3Tunnels.Count == 0)
            {
                Logger.Log("V3TunnelCommunicator: No V3 tunnels available.");
                return;
            }

            InitializeConnection(v3Tunnels);
            Logger.Log($"V3TunnelCommunicator: initialized with {v3Tunnels.Count} tunnels");
        }
    }

    /// <summary>
    /// Stops the receive thread and closes the UDP socket, allowing re-initialization.
    /// </summary>
    public void Shutdown()
    {
        Thread? receiveThread;
        lock (_initLock)
        {
            _running = false;
            _udpClient?.Close();
            _udpClient = null;
            receiveThread = _receiveThread;
            _receiveThread = null;
            _endpointToTunnel.Clear();
            _tunnelToEndpoint.Clear();
            _handlers.Clear();
            foreach (var tcs in _pendingStunQueries.Values)
                tcs.TrySetCanceled();
            _pendingStunQueries.Clear();
            _p2pPeerNames.Clear();
            _p2pEndpointsByPair.Clear();
            Logger.Log("V3TunnelCommunicator: Shut down");
        }

        receiveThread?.Join();
    }

    /// <summary>
    /// Adds endpoint mappings for any tunnels not already known to the communicator.
    /// Call this after a tunnel list refresh so newly-discovered tunnels are reachable.
    /// </summary>
    public void AddTunnels(List<CnCNetTunnel> tunnels)
    {
        if (!IsInitialized)
            return;

        int added = 0;
        foreach (var tunnel in tunnels.Where(IsV3RelayTunnel))
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(tunnel.Address), tunnel.Port);

            if (_tunnelToEndpoint.TryAdd(tunnel, endpoint))
            {
                _endpointToTunnel[endpoint] = tunnel;
                added++;
            }
        }

        if (added > 0)
            Logger.Log($"V3TunnelCommunicator: Added {added} new tunnel endpoint(s) from refresh");
    }

    /// <summary>
    /// Whether the communicator can route packets through this tunnel. Matchmaking servers
    /// (version 4) qualify: the version marks their role in the master list, but on the wire they
    /// speak the same protocol as a version 3 relay.
    /// </summary>
    private static bool IsV3RelayTunnel(CnCNetTunnel tunnel)
        => (tunnel.Version == 3 || tunnel.Version == CnCNetTunnel.MATCHMAKING_VERSION) && !tunnel.IsDirect;

    /// <summary>
    /// Registers a handler for packets between the specified local and remote IDs.
    /// </summary>
    /// <param name="localId">The local player's V3PlayerInfo ID.</param>
    /// <param name="remoteId">The remote player's V3PlayerInfo ID.</param>
    /// <param name="handler">Delegate to handle packets between these IDs.</param>
    public void RegisterHandler(uint localId, uint remoteId, PacketHandler handler)
    {
        _handlers[(localId, remoteId)] = handler;
        Logger.Log($"V3TunnelCommunicator: Registered handler for {localId} <-> {remoteId}");
    }

    /// <summary>
    /// Removes the handler for the specified local/remote ID pair. P2P routing entries are
    /// deliberately left alone: negotiator disposal happens right after every negotiation
    /// completes, and the game bridge still needs the chosen path's routing afterwards.
    /// Use <see cref="CleanupP2PPair"/> when the pair's P2P state should actually go away.
    /// </summary>
    /// <param name="localId">The local player V3PlayerInfo ID.</param>
    /// <param name="remoteId">The remote player V3PlayerInfo ID.</param>
    public void UnregisterHandler(uint localId, uint remoteId)
    {
        if (_handlers.TryRemove((localId, remoteId), out _))
            Logger.Log($"V3TunnelCommunicator: Unregistered handler for {localId} <-> {remoteId}");
        else
            Logger.Log($"V3TunnelCommunicator: Handler not found for {localId} <-> {remoteId} while attempting unregistration");
    }

    /// <summary>
    /// Removes a pair's P2P endpoints from the routing tables so stale entries don't
    /// accumulate across renegotiations and departed players. <paramref name="keepEndpoint"/>
    /// preserves the chosen path (and the pair's peer-name entry) so a P2P tunnel that won
    /// negotiation keeps working for the game bridge.
    /// </summary>
    /// <param name="localId">The local player V3PlayerInfo ID.</param>
    /// <param name="remoteId">The remote player V3PlayerInfo ID.</param>
    /// <param name="keepEndpoint">A P2P endpoint whose routing entries must survive, or null to remove everything.</param>
    public void CleanupP2PPair(uint localId, uint remoteId, IPEndPoint? keepEndpoint = null)
    {
        var key = (localId, remoteId);
        int removed = 0;

        if (_p2pEndpointsByPair.TryRemove(key, out var endpoints))
        {
            foreach (var ep in endpoints.Keys)
            {
                if (keepEndpoint != null && ep.Equals(keepEndpoint))
                    continue;

                if (_endpointToTunnel.TryRemove(ep, out var tunnel))
                {
                    _tunnelToEndpoint.TryRemove(tunnel, out _);
                    removed++;
                }
            }

            if (keepEndpoint != null)
                _p2pEndpointsByPair.GetOrAdd(key, _ => new ConcurrentDictionary<IPEndPoint, byte>())[keepEndpoint] = 0;
        }

        if (keepEndpoint == null)
            _p2pPeerNames.TryRemove(key, out _);

        if (removed > 0)
            Logger.Log($"V3TunnelCommunicator: Cleaned up {removed} P2P endpoint(s) for {localId} <-> {remoteId}" +
                (keepEndpoint != null ? $", keeping {keepEndpoint}" : string.Empty));
    }

    /// <summary>
    /// Constructs a properly formatted UDP packet for sending through a V3 tunnel.
    /// </summary>
    /// <param name="senderId">Sender's V3PlayerInfo ID.</param>
    /// <param name="receiverId">Receiver's V3PlayerInfo ID.</param>
    /// <param name="packetType">Type of the packet to create.</param>
    /// <param name="payload">Optional payload data (defaults to empty).</param>
    /// <returns>A byte array containing the fully formatted packet.</returns>
    public static byte[] CreatePacket(uint senderId, uint receiverId, TunnelPacketType packetType, byte[]? payload = null)
    {
        const int HeaderSize = 8;

        payload ??= [];

        int extraLength = packetType switch
        {
            TunnelPacketType.Register => 0,
            TunnelPacketType.GameData => 0,
            _ => MAGIC_BYTES.Length + 1
        };

        var packet = new byte[HeaderSize + extraLength + payload.Length];
        var span = packet.AsSpan();

        BinaryPrimitives.WriteUInt32LittleEndian(span, senderId);
        BinaryPrimitives.WriteUInt32LittleEndian(span[4..], receiverId);

        if (packetType == TunnelPacketType.Register)
            return packet;

        if (packetType != TunnelPacketType.GameData)
        {
            MAGIC_BYTES.CopyTo(span[HeaderSize..]);
            span[HeaderSize + MAGIC_BYTES.Length] = (byte)packetType;
            payload.CopyTo(span[(HeaderSize + sizeof(TunnelPacketType) + MAGIC_BYTES.Length)..]);
        }
        else
        {
            payload.CopyTo(span[HeaderSize..]);
        }

        return packet;
    }

    /// <summary>
    /// Sends a registration packet to all known V3 tunnels (or a specified subset of them)
    /// </summary>
    /// <param name="localId">Local V3PlayerInfo ID used for registration.</param>
    /// <param name="tunnels">
    /// Optional list of tunnels to send to.
    /// If omitted, all known tunnels will be targeted.
    /// </param>
    /// <param name="quiet">Skip per-tunnel success logging (periodic keepalive refreshes).</param>
    public void SendRegistrationToTunnels(uint localId, List<CnCNetTunnel>? tunnels = null, bool quiet = false)
    {
        if (!IsInitialized)
            return;

        // Shares the routing tables' predicate so matchmaking servers are registered on like any
        // other relay. A server only forwards to registered clients.
        var targetTunnels = tunnels?.Where(IsV3RelayTunnel).ToList() ??
                            _endpointToTunnel.Values.Where(t => !t.IsDirect).ToList();

        var packet = CreatePacket(localId, 0u, TunnelPacketType.Register);
        foreach (var tunnel in targetTunnels)
        {
            if (!_tunnelToEndpoint.TryGetValue(tunnel, out IPEndPoint? endpoint))
                continue;

            try
            {
                _udpClient!.Send(packet, packet.Length, endpoint);
                if (!quiet)
                    Logger.Log($"V3TunnelCommunicator: Registration sent to {tunnel.Name}");
            }
            catch (Exception ex)
            {
                Logger.Log($"V3TunnelCommunicator: Registration error on {tunnel.Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Sends a packet to the specified receiver through the specified tunnel.
    /// </summary>
    /// <param name="tunnel">Target tunnel.</param>
    /// <param name="senderId">The sender's V3PlayerInfo ID.</param>
    /// <param name="receiverId">The receiver's V3PlayerInfo ID.</param>
    /// <param name="packetType">The type of packet to send.</param>
    /// <param name="payload">Optional payload data.</param>
    public void SendPacket(CnCNetTunnel? tunnel, uint senderId, uint receiverId,
        TunnelPacketType packetType, byte[]? payload = null)
    {
        if (!IsInitialized || tunnel == null)
        {
            Logger.Log($"V3TunnelCommunicator: Cannot send packet - communicator not initialized or tunnel is null");
            return;
        }

        if (!_tunnelToEndpoint.TryGetValue(tunnel, out IPEndPoint? endpoint))
        {
            Logger.Log($"V3TunnelCommunicator: Cannot send packet - no cached endpoint for tunnel {tunnel.Name}");
            return;
        }

        try
        {
            var packet = CreatePacket(senderId, receiverId, packetType, payload);
            _udpClient!.Send(packet, packet.Length, endpoint);
        }
        catch (Exception ex)
        {
            Logger.Log($"V3TunnelCommunicator:  Failed to send {packetType} packet to {tunnel.Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends an already-framed packet from a caller-owned buffer, so a hot path can reuse one
    /// buffer instead of allocating per packet. The first 8 bytes must be the sender and receiver
    /// IDs, as written by <see cref="CreatePacket"/>.
    /// </summary>
    /// <param name="length">Number of bytes in <paramref name="packet"/> to send.</param>
    public void SendRawPacket(CnCNetTunnel? tunnel, byte[] packet, int length)
    {
        if (!IsInitialized || tunnel == null)
            return;

        if (!_tunnelToEndpoint.TryGetValue(tunnel, out IPEndPoint? endpoint))
        {
            Logger.Log($"V3TunnelCommunicator: Cannot send packet - no cached endpoint for tunnel {tunnel.Name}");
            return;
        }

        try
        {
            _udpClient!.Client.SendTo(packet, 0, length, SocketFlags.None, endpoint);
        }
        catch (Exception ex)
        {
            Logger.Log($"V3TunnelCommunicator: Failed to send packet to {tunnel.Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Registers a P2P peer's external endpoint so packets from that address are dispatched
    /// to the appropriate handler. <paramref name="localId"/> and <paramref name="remoteId"/>
    /// are used to auto-learn additional endpoints if the peer sends from an address that
    /// wasn't in the original candidate list.
    /// </summary>
    public void AddP2PTunnel(P2PTunnel tunnel, uint localId, uint remoteId)
    {
        if (!IsInitialized)
            return;

        bool isNewPath = _tunnelToEndpoint.TryAdd(tunnel, tunnel.PeerEndpoint);
        _endpointToTunnel[tunnel.PeerEndpoint] = tunnel;

        if (isNewPath)
            Logger.Log($"V3TunnelCommunicator: Registered P2P path {tunnel.Name}");

        _p2pPeerNames[(localId, remoteId)] = tunnel.PeerName;
        _p2pEndpointsByPair.GetOrAdd((localId, remoteId), _ => new ConcurrentDictionary<IPEndPoint, byte>())[tunnel.PeerEndpoint] = 0;
    }

    /// <summary>
    /// Sends a STUN request via the communicator's own UDP socket and returns the
    /// raw 40-byte response, or null on timeout. Using the communicator's socket
    /// ensures the STUN-discovered external port matches the port used for game data.
    /// </summary>
    public async Task<byte[]?> QueryStunAsync(IPEndPoint stunServer, int timeoutMs = 2000)
    {
        if (!IsInitialized)
            return null;

        var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingStunQueries[stunServer] = tcs;

        try
        {
            var request = StunHelper.CreateRequest();
            _udpClient!.Send(request, request.Length, stunServer);

            using var cts = new CancellationTokenSource(timeoutMs);
            return await tcs.Task.WaitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Logger.Log($"V3TunnelCommunicator: STUN query to {stunServer} timed out");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Log($"V3TunnelCommunicator: STUN query to {stunServer} failed: {ex.Message}");
            return null;
        }
        finally
        {
            _pendingStunQueries.TryRemove(stunServer, out _);
        }
    }

    /// <summary>
    /// Stops Windows from surfacing ICMP "port unreachable" responses to previous sends as
    /// <see cref="SocketError.ConnectionReset"/> exceptions on subsequent receives
    /// (SIO_UDP_CONNRESET). Without this, a send to a closed port (e.g. the game closing its
    /// socket, or a dead P2P peer) poisons the receive loop with spurious exceptions.
    /// No-op on other platforms, which don't have this behavior.
    /// </summary>
    internal static void DisableIcmpPortUnreachableExceptions(Socket socket)
    {
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            return;

        try
        {
            const int SIO_UDP_CONNRESET = unchecked((int)0x9800000C);
            socket.IOControl(SIO_UDP_CONNRESET, new byte[4], null);
        }
        catch (Exception ex)
        {
            Logger.Log($"V3TunnelCommunicator: Could not disable UDP connection reset reporting: {ex.Message}");
        }
    }

    private void InitializeConnection(List<CnCNetTunnel> tunnels)
    {
        _udpClient = new UdpClient(0);
        _udpClient.Client.ReceiveTimeout = 500;
        DisableIcmpPortUnreachableExceptions(_udpClient.Client);

        _endpointToTunnel.Clear();
        _tunnelToEndpoint.Clear();
        foreach (var tunnel in tunnels)
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(tunnel.Address), tunnel.Port);
            _endpointToTunnel[endpoint] = tunnel;
            _tunnelToEndpoint[tunnel] = endpoint;
            Logger.Log($"V3TunnelCommunicator: Added tunnel mapping: {endpoint} -> {tunnel.Name}");
        }

        _running = true;
        _receiveThread = new Thread(ReceivePackets)
        {
            IsBackground = true,
            Name = "V3TunnelReceive"
        };
        _receiveThread.Start();

        Logger.Log($"V3TunnelCommunicator: Initialized V3 tunnel connection with {_endpointToTunnel.Count} tunnels on local port {((IPEndPoint)_udpClient.Client.LocalEndPoint!).Port}");
    }

    /// <summary>
    /// Processes a fully received packet by parsing and dispatching it
    /// to the appropriate registered handler.
    /// </summary>
    /// <remarks>
    /// <paramref name="data"/> is a view over the shared receive buffer and is only
    /// valid for the duration of the (synchronous) handler invocation. Handlers must
    /// copy the payload if they need to retain it past the call.
    /// </remarks>
    /// <param name="data">Raw packet data.</param>
    /// <param name="receivedTime">Timestamp when the packet was received.</param>
    /// <param name="tunnel">The tunnel that the packet arrived from.</param>
    /// <summary>
    /// Invoked on the receive thread when a keepalive pong arrives, with the sender's
    /// V3 ID and the measured round-trip time in milliseconds.
    /// </summary>
    public Action<uint, int>? KeepAlivePongReceived { get; set; }

    /// <summary>
    /// Invoked on the receive thread when a peer asks us to probe our own negotiated
    /// paths (pre-launch connectivity check): the requester's V3 ID and the tunnel the
    /// request arrived from (used to send the report back).
    /// </summary>
    public Action<uint, CnCNetTunnel>? ProbeRequestReceived { get; set; }

    /// <summary>
    /// Invoked on the receive thread when a peer reports its probe result: the
    /// reporter's V3 ID and the V3 IDs of peers it could not reach.
    /// </summary>
    public Action<uint, List<uint>>? ProbeReportReceived { get; set; }

    private void ProcessReceivedPacket(ReadOnlyMemory<byte> data, long receivedTime, CnCNetTunnel tunnel)
    {
        try
        {
            var parsed = ParsePacket(data);
            if (parsed.Payload.Length == 0 && !parsed.NegotiationType.HasValue)
                return;

            // Keepalives are handled at this layer, unconditionally: no per-pair handler is
            // needed, so pings get answered in the lobby, during renegotiation and in-game
            // alike, and they never touch negotiator state.
            if (parsed.NegotiationType == TunnelPacketType.KeepAlivePing)
            {
                SendPacket(tunnel, parsed.ReceiverId, parsed.SenderId,
                    TunnelPacketType.KeepAlivePong, parsed.Payload.ToArray());
                return;
            }

            if (parsed.NegotiationType == TunnelPacketType.KeepAlivePong)
            {
                if (parsed.Payload.Length >= 8)
                {
                    long sentTicks = BinaryPrimitives.ReadInt64LittleEndian(parsed.Payload.Span);
                    double rttMs = (receivedTime - sentTicks) * 1000.0 / Stopwatch.Frequency;

                    // The timestamp is our own Stopwatch value echoed back, so anything
                    // negative or absurd means a corrupt/forged payload — drop it.
                    if (rttMs >= 0 && rttMs <= 120000)
                        KeepAlivePongReceived?.Invoke(parsed.SenderId, (int)Math.Round(rttMs));
                }

                return;
            }

            // Connectivity probes are handled at this layer for the same reason as
            // keepalives: no per-pair handler, no negotiator state.
            if (parsed.NegotiationType == TunnelPacketType.ProbeRequest)
            {
                ProbeRequestReceived?.Invoke(parsed.SenderId, tunnel);
                return;
            }

            if (parsed.NegotiationType == TunnelPacketType.ProbeReport)
            {
                var unresponsiveIds = new List<uint>(parsed.Payload.Length / 4);
                for (int i = 0; i + 4 <= parsed.Payload.Length; i += 4)
                    unresponsiveIds.Add(BinaryPrimitives.ReadUInt32LittleEndian(parsed.Payload.Span[i..]));

                ProbeReportReceived?.Invoke(parsed.SenderId, unresponsiveIds);
                return;
            }

            PacketHandler? handler = null;

            if (parsed.NegotiationType.HasValue)
                _handlers.TryGetValue((parsed.ReceiverId, parsed.SenderId), out handler);
            else if (parsed.Payload.Length > 0)
                _handlers.TryGetValue((parsed.ReceiverId, 0), out handler);

            handler?.Invoke(parsed.SenderId, parsed.ReceiverId,
                parsed.NegotiationType ?? TunnelPacketType.GameData,
                parsed.Payload, receivedTime, tunnel);
        }
        catch (Exception ex)
        {
            Logger.Log($"Packet processing error from {tunnel.Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Parses an incoming raw UDP packet into a <see cref="ParsedPacket"/>.
    /// Detects negotiation vs. game data based on presence of magic bytes.
    /// </summary>
    private static ParsedPacket ParsePacket(ReadOnlyMemory<byte> data)
    {
        const int HeaderSize = 8;

        ReadOnlySpan<byte> span = data.Span;

        if (span.Length < HeaderSize)
            return new ParsedPacket();

        uint senderId = BinaryPrimitives.ReadUInt32LittleEndian(span);
        uint receiverId = BinaryPrimitives.ReadUInt32LittleEndian(span[4..]);

        if (span.Length >= HeaderSize + MAGIC_BYTES.Length + sizeof(TunnelPacketType) &&
            span.Slice(HeaderSize, MAGIC_BYTES.Length).SequenceEqual(MAGIC_BYTES))
        {
            var negotiationType = (TunnelPacketType)span[HeaderSize + MAGIC_BYTES.Length];
            var payload = data[(HeaderSize + sizeof(TunnelPacketType) + MAGIC_BYTES.Length)..];
            return new ParsedPacket
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                NegotiationType = negotiationType,
                Payload = payload
            };
        }

        var gamePayload = data.Length > HeaderSize ? data[HeaderSize..] : ReadOnlyMemory<byte>.Empty;
        return new ParsedPacket
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            NegotiationType = null,
            Payload = gamePayload
        };
    }

    /// <summary>
    /// When a negotiation packet arrives from an endpoint not in <see cref="_endpointToTunnel"/>,
    /// attempts to match it to a known P2P pair by sender/receiver ID and — if found — registers
    /// the unexpected source address as an additional P2P path and dispatches the packet.
    /// This handles peers whose actual sending IP differs from their advertised candidates
    /// (e.g. a mobile hotspot whose local adapter IP was not enumerated by <see cref="P2PEndpointDiscovery"/>).
    /// </summary>
    /// <returns><c>true</c> if the packet was dispatched; <c>false</c> if it remains unrecognised.</returns>
    private bool TryAutoLearnAndDispatch(ReadOnlyMemory<byte> data, long receivedTime, IPEndPoint remoteEndpoint)
    {
        var parsed = ParsePacket(data);
        if (!parsed.NegotiationType.HasValue)
            return false;

        var handlerKey = (parsed.ReceiverId, parsed.SenderId);
        if (!_p2pPeerNames.TryGetValue(handlerKey, out var peerName))
            return false;

        // ReceiveFrom reuses its EndPoint instance for consecutive packets from the same
        // source, so snapshot the endpoint before storing it as a long-lived routing key —
        // a live instance must never end up as a dictionary key.
        remoteEndpoint = new IPEndPoint(remoteEndpoint.Address, remoteEndpoint.Port);

        var learnedTunnel = _endpointToTunnel.GetOrAdd(remoteEndpoint, ep =>
        {
            var t = new P2PTunnel(ep, peerName);
            _p2pEndpointsByPair.GetOrAdd(handlerKey, _ => new ConcurrentDictionary<IPEndPoint, byte>())[ep] = 0;
            Logger.Log($"V3TunnelCommunicator: Auto-learned P2P path {t.Name}");
            return t;
        });

        // Also register the reverse direction so SendPacket can reach the auto-learned tunnel.
        // _endpointToTunnel maps endpoint→tunnel (for receive routing); _tunnelToEndpoint maps
        // tunnel→endpoint (for send routing). Auto-learned tunnels need both populated.
        _tunnelToEndpoint.TryAdd(learnedTunnel, remoteEndpoint);

        ProcessReceivedPacket(data, receivedTime, learnedTunnel);
        return true;
    }

    /// <summary>
    /// Continuously listens for UDP packets from all known tunnel endpoints.
    /// Each packet is parsed and dispatched on arrival.
    /// </summary>
    private void ReceivePackets()
    {
        UdpClient? udpClient = _udpClient;
        if (udpClient == null)
            return;

        byte[] receiveBuffer = new byte[MAX_DATAGRAM_SIZE];
        EndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);

        try
        {
            while (_running)
            {
                try
                {
                    // Snapshot the socket for the whole iteration. Shutdown() closes the UdpClient
                    // from another thread, which nulls its Client property, so re-reading it
                    // mid-iteration can hand back a socket and then null.
                    var socket = udpClient.Client;
                    if (socket == null)
                        break;

                    if (socket.Poll(500_000, SelectMode.SelectRead)) // 500ms
                    {
                        int received = socket.ReceiveFrom(receiveBuffer, ref remoteEndpoint);
                        var receivedTime = Stopwatch.GetTimestamp();

                        if (_endpointToTunnel.TryGetValue((IPEndPoint)remoteEndpoint, out var tunnel))
                            ProcessReceivedPacket(receiveBuffer.AsMemory(0, received), receivedTime, tunnel);
                        else if (_pendingStunQueries.TryRemove((IPEndPoint)remoteEndpoint, out var stunTcs))
                        {
                            var stunData = new byte[received];
                            Buffer.BlockCopy(receiveBuffer, 0, stunData, 0, received);
                            stunTcs.TrySetResult(stunData);
                        }
                        else if (!TryAutoLearnAndDispatch(receiveBuffer.AsMemory(0, received), receivedTime, (IPEndPoint)remoteEndpoint))
                            Logger.Log($"V3TunnelCommunicator: Received packet from unknown endpoint: {remoteEndpoint}");
                    }
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                {
                    continue;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted ||
                                                    ex.SocketErrorCode == SocketError.OperationAborted)
                {
                    Logger.Log("V3TunnelCommunicator: Receive thread socket closed, exiting");
                    break;
                }
                catch (SocketException ex)
                {
                    Logger.Log($"V3TunnelCommunicator: Socket error in receive thread: {ex.SocketErrorCode} - {ex.Message}");

                }
            }
        }
        catch (ObjectDisposedException)
        {
            Logger.Log("V3TunnelCommunicator: Receive thread: Socket disposed");
        }
        catch (Exception ex)
        {
            Logger.Log($"V3TunnelCommunicator: Unexpected error in receive thread: {ex.Message}");
        }
        finally
        {
            Logger.Log("V3TunnelCommunicator: Receive thread exiting");
        }
    }
}
