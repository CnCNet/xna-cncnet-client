using ClientCore;
using Rampastring.Tools;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Represents a parsed UDP packet exchanged through a V3 tunnel.
/// </summary>
public readonly ref struct ParsedPacket
{
    public uint SenderId { get; init; }
    public uint ReceiverId { get; init; }
    public TunnelPacketType? NegotiationType { get; init; }
    public ReadOnlySpan<byte> Payload { get; init; }
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
    GameData = 0x08
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
    TunnelPacketType packetType, byte[] payload, long receivedTime, CnCNetTunnel tunnel);

/// <summary>
/// Manages UDP communication with V3 tunnel servers.
/// Handles registration, negotiation packets, and forwarding of
/// game data between players through tunnels.
/// </summary>
public class V3TunnelCommunicator
{
    private readonly static byte[] MAGIC_BYTES = [0x45, 0x4A, 0x45, 0x4A, 0x45, 0x4A]; //EJEJEJ

    private UdpClient _udpClient;
    private Thread _receiveThread; 
    private volatile bool _running;
    private readonly ConcurrentDictionary<IPEndPoint, CnCNetTunnel> _endpointToTunnel = new();
    private readonly ConcurrentDictionary<(uint localId, uint remoteId), PacketHandler> _handlers = new();
    private readonly object _initLock = new();

    public bool IsInitialized => _udpClient != null;

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

            var v3Tunnels = tunnels.Where(t => t.Version == 3 &&
                (UserINISettings.Instance.PingUnofficialCnCNetTunnels || t.Official || t.Recommended))
                .ToList();

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
    /// Removes the handler for the specified local/remote ID pair.
    /// </summary>
    /// <param name="localId">The local player V3PlayerInfo ID.</param>
    /// <param name="remoteId">The remote player V3PlayerInfo ID.</param>
    public void UnregisterHandler(uint localId, uint remoteId)
    {
        _handlers.TryRemove((localId, remoteId), out _);
        Logger.Log($"V3TunnelCommunicator: Unregistered handler for {localId} <-> {remoteId}");
    }

    /// <summary>
    /// Constructs a properly formatted UDP packet for sending through a V3 tunnel.
    /// </summary>
    /// <param name="senderId">Sender's V3PlayerInfo ID.</param>
    /// <param name="receiverId">Receiver's V3PlayerInfo ID.</param>
    /// <param name="packetType">Type of the packet to create.</param>
    /// <param name="payload">Optional payload data (defaults to empty).</param>
    /// <returns>A byte array containing the fully formatted packet.</returns>
    public static byte[] CreatePacket(uint senderId, uint receiverId, TunnelPacketType packetType, byte[] payload = null)
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
    public void SendRegistrationToTunnels(uint localId, List<CnCNetTunnel> tunnels = null)
    {
        if (!IsInitialized)
            return;

        var targetTunnels = tunnels?.Where(t => t.Version == 3).ToList() ??
                            [.. _endpointToTunnel.Values];

        var packet = CreatePacket(localId, 0u, TunnelPacketType.Register);
        foreach (var tunnel in targetTunnels)
        {
            try
            {
                _udpClient.Send(packet, packet.Length, tunnel.Address, tunnel.Port);
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
    public void SendPacket(CnCNetTunnel tunnel, uint senderId, uint receiverId,
        TunnelPacketType packetType, byte[] payload = null)
    {
        if (!IsInitialized || tunnel == null)
        {
            Logger.Log($"V3TunnelCommunicator: Cannot send packet - communicator not initialized or tunnel is null");
            return;
        }

        try
        {
            var packet = CreatePacket(senderId, receiverId, packetType, payload);
            _udpClient.Send(packet, packet.Length, tunnel.Address, tunnel.Port);
        }
        catch (Exception ex)
        {
            Logger.Log($"V3TunnelCommunicator:  Failed to send {packetType} packet to {tunnel.Name}: {ex.Message}");
        }
    }

    private void InitializeConnection(List<CnCNetTunnel> tunnels)
    {
        _udpClient = new UdpClient(0);
        _udpClient.Client.ReceiveTimeout = 500;

        _endpointToTunnel.Clear();
        foreach (var tunnel in tunnels)
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(tunnel.Address), tunnel.Port);
            _endpointToTunnel[endpoint] = tunnel;
            Logger.Log($"V3TunnelCommunicator: Added tunnel mapping: {endpoint} -> {tunnel.Name}");
        }

        _running = true;
        _receiveThread = new Thread(ReceivePackets)
        {
            IsBackground = true,
            Name = "V3TunnelReceive"
        };
        _receiveThread.Start();

        Logger.Log($"V3TunnelCommunicator: Initialized V3 tunnel connection with {_endpointToTunnel.Count} tunnels on local port {((IPEndPoint)_udpClient.Client.LocalEndPoint).Port}");
    }

    /// <summary>
    /// Processes a fully received packet by parsing and dispatching it
    /// to the appropriate registered handler.
    /// </summary>
    /// <param name="data">Raw packet data.</param>
    /// <param name="receivedTime">Timestamp when the packet was received.</param>
    /// <param name="tunnel">The tunnel that the packet arrived from.</param>
    private void ProcessReceivedPacket(byte[] data, long receivedTime, CnCNetTunnel tunnel)
    {
        try
        {
            var parsed = ParsePacket(data.AsSpan());
            if (parsed.Payload.Length == 0 && !parsed.NegotiationType.HasValue)
                return;

            PacketHandler handler = null;

            if (parsed.NegotiationType.HasValue)
                _handlers.TryGetValue((parsed.ReceiverId, parsed.SenderId), out handler);
            else if (parsed.Payload.Length > 0)
                _handlers.TryGetValue((parsed.ReceiverId, 0), out handler);

            handler?.Invoke(parsed.SenderId, parsed.ReceiverId,
                parsed.NegotiationType ?? TunnelPacketType.GameData,
                parsed.Payload.ToArray(), receivedTime, tunnel);
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
    private static ParsedPacket ParsePacket(ReadOnlySpan<byte> data)
    {
        const int HeaderSize = 8;

        if (data.Length < HeaderSize)
            return new ParsedPacket();

        uint senderId = BinaryPrimitives.ReadUInt32LittleEndian(data);
        uint receiverId = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]);

        if (data.Length >= HeaderSize + MAGIC_BYTES.Length + sizeof(TunnelPacketType) &&
            data.Slice(HeaderSize, MAGIC_BYTES.Length).SequenceEqual(MAGIC_BYTES))
        {
            var negotiationType = (TunnelPacketType)data[HeaderSize + MAGIC_BYTES.Length];
            var payload = data[(HeaderSize + sizeof(TunnelPacketType) + MAGIC_BYTES.Length)..];
            return new ParsedPacket
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                NegotiationType = negotiationType,
                Payload = payload
            };
        }

        var gamePayload = data.Length > HeaderSize ? data[HeaderSize..] : [];
        return new ParsedPacket
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            NegotiationType = null,
            Payload = gamePayload
        };
    }

    /// <summary>
    /// Continuously listens for UDP packets from all known tunnel endpoints.
    /// Each packet is parsed and dispatched on arrival.
    /// </summary>
    private void ReceivePackets()
    {
        try
        {
            IPEndPoint remoteEndpoint = new(IPAddress.Any, 0);
            while (_running)
            {
                try
                {
                    if (_udpClient.Client.Poll(500_000, SelectMode.SelectRead)) // 500ms
                    {
                        byte[] data = _udpClient.Receive(ref remoteEndpoint);
                        var receivedTime = Stopwatch.GetTimestamp();

                        if (_endpointToTunnel.TryGetValue(remoteEndpoint, out var tunnel))
                            ProcessReceivedPacket(data, receivedTime, tunnel);
                        else
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