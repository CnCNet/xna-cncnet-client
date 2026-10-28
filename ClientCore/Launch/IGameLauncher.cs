#nullable enable

namespace ClientCore.Launch;

/// <summary>Starts the game for a launch request.</summary>
public interface IGameLauncher
{
    /// <summary>Whether this launcher can start the given kind of launch.</summary>
    bool Supports(LaunchRequest request);

    /// <summary>
    /// Prepares and starts the game. On success, the result's session reports when the game exits.
    /// </summary>
    LaunchResult Launch(LaunchRequest request);
}

/// <summary>Why a launch failed.</summary>
public enum LaunchFailureReason
{
    None,

    /// <summary>The client was still preprocessing INI files.</summary>
    PreprocessingIncomplete,

    /// <summary>The operating system could not start the executable.</summary>
    ProcessStartFailed,
}

/// <summary>The outcome of a launch: either a running game session or the reason the game didn't start.</summary>
public sealed class LaunchResult
{
    private LaunchResult(GameSession? session, LaunchFailureReason failureReason, string executableName, string errorMessage)
    {
        Session = session;
        FailureReason = failureReason;
        ExecutableName = executableName;
        ErrorMessage = errorMessage;
    }

    /// <summary>The running game, or null when the launch failed.</summary>
    public GameSession? Session { get; }

    public LaunchFailureReason FailureReason { get; }

    /// <summary>The executable that failed to start, if the process could not be started.</summary>
    public string ExecutableName { get; }

    /// <summary>The operating system's error message, if the process could not be started.</summary>
    public string ErrorMessage { get; }

    public bool Succeeded => Session != null;

    public static LaunchResult Started(GameSession session)
        => new(session, LaunchFailureReason.None, string.Empty, string.Empty);

    public static LaunchResult Failed(LaunchFailureReason reason, string executableName = "", string errorMessage = "")
        => new(null, reason, executableName, errorMessage);
}
