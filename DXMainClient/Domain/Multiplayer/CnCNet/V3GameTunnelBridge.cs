#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Bridges UDP traffic between the local game and remote players
/// using V3 tunnels
/// </summary>
/// <remarks>
/// <para>Each remote player's traffic goes out on whatever paths its <see cref="V3PairLink"/>
/// currently lists. Normally that is one, but during a failover or a trial it is two or three,
/// so the game keeps running while a pair changes tunnel.</para>
///
/// <para>To make those copies safe for any game, every datagram sent through a tunnel carries a
/// 2-byte sequence number after the game's payload, and the receiving bridge drops any sequence
/// number it has already delivered before stripping the trailer and passing the datagram on. The
/// game never sees the trailer or the duplicates.</para>
/// </remarks>
public class V3GameTunnelBridge
{
    /// <summary>Sender and receiver IDs prefixed to every packet sent through a tunnel.</summary>
    private const int HEADER_SIZE = 8;

    /// <summary>The per-recipient sequence number appended to every packet.</summary>
    private const int TRAILER_SIZE = 2;

    private const int MAX_DATAGRAM_SIZE = 65507;

    /// <summary>
    /// Holds the outgoing packet: the 8-byte header followed by the game's datagram, received
    /// directly into place, then the trailer. Only touched by the bridge thread.
    /// </summary>
    private readonly byte[] _sendBuffer = new byte[MAX_DATAGRAM_SIZE];

    private readonly uint _localId;
    private readonly List<V3PlayerInfo> _otherPlayers;
    private readonly ushort[] _nextSequence;
    private readonly DuplicateFilter[] _duplicateFilters;
    private readonly bool[] _loggedNoPath;
    private readonly TunnelHandler _tunnelHandler;
    private readonly Thread _bridgeThread;
    private readonly UdpClient _localGameClient;
    private volatile IPEndPoint? _gameEndpoint;
    private volatile bool _isRunning = false;
    private bool _loggedTransientReceiveError;
    public bool IsRunning => _isRunning;

    public V3GameTunnelBridge(
        uint localId,
        UdpClient localGameClient,
        List<V3PlayerInfo> allPlayers,
        TunnelHandler tunnelHandler)
    {
        _localId = localId;
        _tunnelHandler = tunnelHandler;
        _localGameClient = localGameClient;
        _otherPlayers = allPlayers.Where(p => p.Id != _localId).ToList();
        _nextSequence = new ushort[_otherPlayers.Count];
        _duplicateFilters = new DuplicateFilter[_otherPlayers.Count];
        _loggedNoPath = new bool[_otherPlayers.Count];

        int localPort = ((IPEndPoint)_localGameClient.Client.LocalEndPoint!).Port;
        Logger.Log($"V3GameTunnelBridge: Local ID={_localId}, Local Port={localPort}");
        Logger.Log($"V3GameTunnelBridge: Will forward to {_otherPlayers.Count} other players");

        _bridgeThread = new Thread(BridgeWorker)
        {
            Name = "CnCNetV3GameTunnelBridge",
            IsBackground = true
        };
    }

    /// <summary>
    /// Drops datagrams already delivered, by sequence number: a 64-wide window behind the newest
    /// one seen. Only used on the communicator's receive thread.
    /// </summary>
    private struct DuplicateFilter
    {
        private bool _started;
        private ushort _newest;
        private ulong _seen;

        public bool Accept(ushort sequence)
        {
            if (!_started)
            {
                _started = true;
                _newest = sequence;
                _seen = 1;
                return true;
            }

            int ahead = (short)(ushort)(sequence - _newest);
            if (ahead > 0)
            {
                _seen = ahead >= 64 ? 1 : (_seen << ahead) | 1;
                _newest = sequence;
                return true;
            }

            int behind = -ahead;

            // Far behind the window is a sender that restarted its count, not a duplicate.
            if (behind >= 1024)
            {
                _newest = sequence;
                _seen = 1;
                return true;
            }

            if (behind >= 64)
                return false;

            ulong bit = 1UL << behind;
            if ((_seen & bit) != 0)
                return false;

            _seen |= bit;
            return true;
        }
    }

    /// <summary>
    /// Starts the game tunnel bridge, registers tunnel packet handler, launches
    /// the worker thread to forward game traffic between the game and other players.
    /// </summary>
    public void Start()
    {
        if (_isRunning)
            return;

        Logger.Log("=== V3GameTunnelBridge Starting ===");

        var localEP = (IPEndPoint)_localGameClient.Client.LocalEndPoint!;
        Logger.Log($"Local Server: {localEP}");

        Logger.Log("Player mappings:");
        foreach (var player in _otherPlayers)
        {
            if (player.Tunnel != null)
                Logger.Log($" Player {player.Name}: {player.Tunnel.Address}:{player.Tunnel.Port}");
        }
        Logger.Log("=============================================");

        _tunnelHandler.RegisterV3PacketHandler(_localId, 0, OnTunnelPacketReceived);

        _isRunning = true;
        _bridgeThread.Start();
        Logger.Log("V3GameTunnelBridge: Started successfully");
    }

    /// <summary>
    /// Stops the game tunnel bridge and unregisters packet handlers. The local/game UDP
    /// socket is left open — it's owned by TunnelHandler and reused for the next game
    /// launched in this lobby, not closed here.
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
            return;

        _isRunning = false;

        // Unregister before returning: the handler forwards straight into the socket.
        _tunnelHandler.UnregisterV3PacketHandler(_localId, 0);

        // Let the worker leave its receive loop before returning. Poll's 500ms timeout bounds the wait.
        if (Thread.CurrentThread != _bridgeThread)
            _bridgeThread.Join(1000);

        Logger.Log("V3GameTunnelBridge: Stopped");
    }

    private int IndexOfPlayer(uint id)
    {
        for (int i = 0; i < _otherPlayers.Count; i++)
        {
            if (_otherPlayers[i].Id == id)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Handles packets received from remote players via the tunnels.
    /// Forwards the received payload to the locally running game once its endpoint is known.
    /// </summary>
    /// <param name="senderId">The ID of the player who sent the packet.</param>
    /// <param name="receiverId">The ID of the recipient player.</param>
    /// <param name="packetType">The type of received tunnel packet.</param>
    /// <param name="payload">The raw data payload to forward to the game.</param>
    /// <param name="receivedTime">The timestamp when the packet was received.</param>
    /// <param name="tunnel">The tunnel through which the packet arrived.</param>
    private void OnTunnelPacketReceived(uint senderId, uint receiverId,
        TunnelPacketType packetType, ReadOnlyMemory<byte> payload, long receivedTime, CnCNetTunnel tunnel)
    {
        int index = IndexOfPlayer(senderId);
        if (index < 0)
            return;

        var path = _otherPlayers[index].Link?.FindAcceptingPath(tunnel);
        if (path == null)
            return;

        // Game data is proof of life too, so it counts towards the path's liveness and loss.
        path.CountReceived(V3Clock.NowMs);

        if (payload.Length <= TRAILER_SIZE)
            return;

        ushort sequence = BinaryPrimitives.ReadUInt16LittleEndian(payload.Span[^TRAILER_SIZE..]);
        if (!_duplicateFilters[index].Accept(sequence))
            return;

        var gameEndpoint = _gameEndpoint;
        if (gameEndpoint == null)
            return;

        // Runs on the communicator's receive thread, so Stop() can close the socket underneath it.
        // Snapshotting keeps a shutdown from turning into a null deref.
        var gameSocket = _isRunning ? _localGameClient.Client : null;
        if (gameSocket == null)
            return;

        try
        {
            // Forward straight from the received buffer to avoid copying every game packet.
            var datagram = payload[..^TRAILER_SIZE];
            if (MemoryMarshal.TryGetArray(datagram, out ArraySegment<byte> segment))
                gameSocket.SendTo(segment.Array!, segment.Offset, segment.Count, SocketFlags.None, gameEndpoint);
            else
                gameSocket.SendTo(datagram.ToArray(), SocketFlags.None, gameEndpoint);
        }
        catch (ObjectDisposedException)
        {
            // Shutdown raced this send; the bridge is going away regardless.
        }
        catch (Exception ex)
        {
            Logger.Log($"V3GameTunnelBridge: Error sending to game: {ex.Message}");
        }
    }

    /// <summary>
    /// Records where the game is listening, copying the endpoint rather than storing it:
    /// ReceiveFrom reuses its EndPoint instance across calls.
    /// </summary>
    private void CaptureGameEndpoint(EndPoint remoteEndPoint)
    {
        if (remoteEndPoint is not IPEndPoint ipEndPoint || _gameEndpoint?.Equals(ipEndPoint) == true)
            return;

        _gameEndpoint = new IPEndPoint(ipEndPoint.Address, ipEndPoint.Port);
    }

    /// <summary>
    /// The background worker that receives data from the local game client
    /// and forwards it through the appropriate tunnel to remote players.
    /// Also captures the game's UDP endpoint once the first packet is received.
    /// </summary>
    private void BridgeWorker()
    {
        try
        {
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            while (_isRunning)
            {
                try
                {
                    // Snapshot the socket for the whole iteration. Closing the UdpClient nulls its
                    // Client property, so reading it twice can hand back a socket and then null if
                    // Stop() lands in between.
                    var gameSocket = _localGameClient.Client;
                    if (gameSocket == null)
                        break;

                    if (gameSocket.Poll(500_000, SelectMode.SelectRead)) // 500ms
                    {
                        // Received straight into the send buffer past the header, so the packet
                        // sent onward is the same memory with the IDs written in front of it.
                        int received = gameSocket.ReceiveFrom(
                            _sendBuffer, HEADER_SIZE, _sendBuffer.Length - HEADER_SIZE - TRAILER_SIZE,
                            SocketFlags.None, ref remoteEndPoint);

                        CaptureGameEndpoint(remoteEndPoint);

                        if (received < 4)
                        {
                            Logger.Log($"V3GameTunnelBridge: Ignoring too-short game packet (length={received})");
                            continue;
                        }

                        ushort receiverId = BinaryPrimitives.ReadUInt16BigEndian(
                            _sendBuffer.AsSpan(HEADER_SIZE + 2));

                        int index = _otherPlayers.FindIndex(p => p.PlayerGameId == receiverId);
                        if (index < 0)
                        {
                            Logger.Log($"V3GameTunnelBridge: No matching recipient found for receiverId={receiverId}");
                            continue;
                        }

                        SendToPlayer(index, received);
                    }
                }
                catch (ObjectDisposedException)
                {
                    Logger.Log("V3GameTunnelBridge: Local server socket disposed, exiting");
                    break;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                {
                    continue;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted ||
                                                 ex.SocketErrorCode == SocketError.OperationAborted)
                {
                    Logger.Log("V3GameTunnelBridge: Local server socket closed, exiting");
                    break;
                }
                catch (SocketException ex)
                {
                    // Notably ConnectionReset on Windows (ICMP port-unreachable from a send to the
                    // game's endpoint). The bridge must outlive transient socket errors — exiting
                    // here would silently disconnect the player for the rest of the match.
                    if (!_loggedTransientReceiveError)
                    {
                        _loggedTransientReceiveError = true;
                        Logger.Log($"V3GameTunnelBridge: Transient receive error, continuing: {ex.SocketErrorCode} - {ex.Message}");
                    }
                }
            }
        }
        catch (ObjectDisposedException)
        {
            Logger.Log("V3GameTunnelBridge: Local server shutdown");
        }
        catch (Exception ex)
        {
            Logger.Log($"V3GameTunnelBridge: Local server receive error: {ex.Message}");
        }

        Logger.Log("V3GameTunnelBridge: Bridge worker thread stopped");
    }

    /// <summary>
    /// Frames the game datagram sitting in the send buffer and sends it on every path the
    /// recipient's link currently lists.
    /// </summary>
    private void SendToPlayer(int index, int received)
    {
        var recipient = _otherPlayers[index];
        var paths = recipient.Link?.SendPaths;

        if (paths == null || paths.Length == 0)
        {
            if (!_loggedNoPath[index])
            {
                _loggedNoPath[index] = true;
                Logger.Log($"V3GameTunnelBridge: Cannot send to {recipient.Name} - no path assigned");
            }

            return;
        }

        _loggedNoPath[index] = false;

        BinaryPrimitives.WriteUInt32LittleEndian(_sendBuffer, _localId);
        BinaryPrimitives.WriteUInt32LittleEndian(_sendBuffer.AsSpan(4), recipient.Id);
        BinaryPrimitives.WriteUInt16LittleEndian(_sendBuffer.AsSpan(HEADER_SIZE + received), _nextSequence[index]++);

        int length = HEADER_SIZE + received + TRAILER_SIZE;
        foreach (var path in paths)
        {
            _tunnelHandler.SendRawPacket(path.Tunnel, _sendBuffer, length);
            path.CountSent();
        }
    }
}
