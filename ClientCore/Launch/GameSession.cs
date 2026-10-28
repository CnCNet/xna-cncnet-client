#nullable enable

using System;
using System.Collections.Generic;

namespace ClientCore.Launch;

/// <summary>How a game run ended.</summary>
public sealed record GameSessionResult(TimeSpan Duration, int? ExitCode);

/// <summary>One run of the game, from the moment its process starts until it exits.</summary>
public sealed class GameSession
{
    private readonly object locker = new();
    private readonly List<Action<GameSessionResult>> endedHandlers = [];
    private GameSessionResult? result;

    internal GameSession(LaunchRequest request)
    {
        Request = request;
        StartedAt = DateTime.Now;
    }

    /// <summary>The request the game was launched with.</summary>
    public LaunchRequest Request { get; }

    public DateTime StartedAt { get; }

    public bool IsRunning
    {
        get
        {
            lock (locker)
                return result == null;
        }
    }

    /// <summary>
    /// Runs the handler once the game has exited. It runs on the thread the exit is
    /// detected on, or right away on the calling thread if the game has already exited.
    /// </summary>
    public void WhenEnded(Action<GameSessionResult> handler)
    {
        GameSessionResult? endedResult;

        lock (locker)
        {
            endedResult = result;

            if (endedResult == null)
            {
                endedHandlers.Add(handler);
                return;
            }
        }

        handler(endedResult);
    }

    internal void End(int? exitCode)
    {
        var endedResult = new GameSessionResult(DateTime.Now - StartedAt, exitCode);
        Action<GameSessionResult>[] handlers;

        lock (locker)
        {
            result = endedResult;
            handlers = endedHandlers.ToArray();
            endedHandlers.Clear();
        }

        foreach (Action<GameSessionResult> handler in handlers)
            handler(endedResult);
    }
}
