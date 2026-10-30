#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet;

/// <summary>
/// Drives every <see cref="V3PairLink"/> from one dedicated thread.
/// </summary>
/// <remarks>
/// Deliberately not the client's update loop: that drops to a few frames per second while a game
/// is running, which would make heartbeats late and bursty enough to look like failures, and it
/// stops altogether if the UI crashes while the game carries on.
/// </remarks>
public sealed class V3LinkScheduler
{
    private const int TICK_MS = 20;

    private readonly Func<bool> _isInGame;
    private readonly object _lock = new();
    private volatile V3PairLink[] _links = [];
    private Thread? _thread;

    public V3LinkScheduler(Func<bool> isInGame)
    {
        _isInGame = isInGame;
    }

    /// <summary>The links currently being driven.</summary>
    public IReadOnlyList<V3PairLink> Links => _links;

    public void Register(V3PairLink link)
    {
        lock (_lock)
        {
            if (!_links.Contains(link))
                _links = [.. _links, link];

            if (_thread == null)
            {
                _thread = new Thread(Run)
                {
                    IsBackground = true,
                    Name = "V3LinkScheduler"
                };
                _thread.Start();
            }
        }
    }

    public void Unregister(V3PairLink link)
    {
        lock (_lock)
            _links = _links.Where(l => l != link).ToArray();
    }

    private void Run()
    {
        while (true)
        {
            long now = V3Clock.NowMs;
            bool inGame = _isInGame();
            bool sawDisposed = false;

            foreach (var link in _links)
            {
                if (link.IsDisposed)
                {
                    sawDisposed = true;
                    continue;
                }

                try
                {
                    link.Tick(now, inGame);
                }
                catch (Exception ex)
                {
                    Logger.Log($"V3LinkScheduler: Tick failed for {link}: {ex}");
                }
            }

            if (sawDisposed)
            {
                lock (_lock)
                    _links = _links.Where(l => !l.IsDisposed).ToArray();
            }

            Thread.Sleep(TICK_MS);
        }
    }
}
