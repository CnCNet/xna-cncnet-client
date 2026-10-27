#nullable enable

using System;
using System.Threading;

using ClientCore.Launch.Spawner;

namespace ClientCore.Launch;

/// <summary>
/// Tracks whether a game is running and tells the rest of the client when one starts or exits.
/// </summary>
public static class GameSessionManager
{
    private static readonly ManualResetEventSlim gameNotRunning = new(true);

    /// <summary>The launcher every launch goes through.</summary>
    public static IGameLauncher Launcher { get; set; } = new SpawnerGameLauncher();

    /// <summary>Raised on the launching thread just before the game process is created.</summary>
    public static event Action? GameStarting;

    /// <summary>Raised on the launching thread once the game process has started.</summary>
    public static event Action? GameStarted;

    /// <summary>
    /// Raised when the game exits, on the thread the exit is detected on. Also raised on the
    /// launching thread when the operating system fails to start the game process.
    /// </summary>
    public static event Action? GameExited;

    /// <summary>The game that is running, or null.</summary>
    public static GameSession? Current { get; private set; }

    public static bool IsGameRunning => !gameNotRunning.IsSet;

    /// <summary>
    /// Blocks the calling thread until the game exits. The wait does not depend on the client's
    /// update loop, so the client's crash handler can call this after the UI thread has crashed.
    /// </summary>
    public static void WaitForGameExit() => gameNotRunning.Wait();

    internal static void OnGameStarting() => GameStarting?.Invoke();

    internal static void OnProcessStarting() => gameNotRunning.Reset();

    internal static void OnGameStarted(GameSession session)
    {
        // A game that exits right away may already have been reported as exited.
        if (session.IsRunning)
            Current = session;

        GameStarted?.Invoke();
    }

    internal static void OnGameExited()
    {
        Current = null;
        gameNotRunning.Set();
        SinglePlayerSavedGameManager.PruneSavedGames();
        GameExited?.Invoke();
    }
}
