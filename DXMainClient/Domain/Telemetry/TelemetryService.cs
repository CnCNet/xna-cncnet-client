#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
#if NETFRAMEWORK
using System.Security.Authentication;
#endif
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using ClientCore;
using ClientCore.Display;

using ClientGUI;

using Rampastring.Tools;

namespace DTAClient.Domain.Telemetry;

/// <summary>
/// Sends anonymous usage statistics to TelemetryDeck.
/// No identifiers are sent: every client launch reports under a new random ID,
/// and active users are counted through first-launch-of-the-period flags instead.
/// </summary>
internal static class TelemetryService
{
    private const string INGEST_URL = "https://nom.telemetrydeck.com/v2/";
    private const int REQUEST_TIMEOUT_MILLISECONDS = 10000;
    private const int SHUTDOWN_TIMEOUT_MILLISECONDS = 2000;
    private const int SYSTEM_SPECIFICATIONS_TIMEOUT_MILLISECONDS = 15000;
    private const string UNKNOWN = "Unknown";
    private const string OTHER = "Other";

    private static readonly HttpClient httpClient = new HttpClient(new HttpClientHandler()
    {
#if NETFRAMEWORK
        // .NET Framework uses the protocols the OS enables by default. On Windows 7 that
        // excludes TLS 1.2, which TelemetryDeck requires, so request it explicitly.
        SslProtocols = SslProtocols.Tls12,
#endif
    })
    {
        Timeout = Timeout.InfiniteTimeSpan
    };

    /// <summary>
    /// A random ID that only lives for this client launch.
    /// TelemetryDeck requires a user ID, so this is used as both the user and the session ID.
    /// </summary>
    private static readonly string launchId = Guid.NewGuid().ToString("N");

    private static readonly TaskCompletionSource<SystemSpecifications?> systemSpecificationsSource = new();

    /// <summary>
    /// Common resolutions that are reported as-is. Any other resolution is reported as "Other".
    /// </summary>
    private static readonly HashSet<string> commonResolutions = new()
    {
        "640x480", "800x600", "1024x768", "1280x720", "1280x800", "1280x1024",
        "1360x768", "1366x768", "1440x900", "1536x864", "1600x900", "1680x1050",
        "1920x1080", "1920x1200", "2560x1080", "2560x1440", "2560x1600",
        "3440x1440", "3840x1600", "3840x2160", "5120x1440", "5120x2880",
    };

    private static readonly object locker = new();
    private static readonly List<Dictionary<string, object>> pendingSignals = new();
    private static Task sendTask = Task.CompletedTask;

    private static Dictionary<string, object> activityPayload = new();

    /// <summary>
    /// Whether the game has a TelemetryDeck app ID configured.
    /// </summary>
    public static bool IsAvailable => ClientConfiguration.Instance.TelemetryAvailable;

    /// <summary>
    /// Whether telemetry is available and the user hasn't opted out of it.
    /// </summary>
    public static bool IsEnabled => IsAvailable && UserINISettings.Instance.TelemetryEnabled;

    /// <summary>
    /// Works out whether this is the first launch of the day, week and month,
    /// and stores the date of this launch. Call once at startup.
    /// </summary>
    public static void Initialize()
    {
        if (!IsEnabled)
        {
            Logger.Log("Telemetry is disabled.");
            return;
        }

        var settings = UserINISettings.Instance;
        DateTime today = DateTime.UtcNow.Date;
        string currentMonth = today.ToString("yyyy-MM", CultureInfo.InvariantCulture);

        bool hasLastActiveDate = DateTime.TryParseExact(settings.TelemetryLastActiveDate, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime lastActiveDate);

        bool isNewInstall = string.IsNullOrEmpty(settings.TelemetryInstallMonth) && settings.IsFirstRun;

        // Installations from before telemetry existed have no known install month.
        // Report them as unknown so that they don't inflate the cohort of the current month.
        if (string.IsNullOrEmpty(settings.TelemetryInstallMonth))
            settings.TelemetryInstallMonth.Value = isNewInstall ? currentMonth : UNKNOWN;

        activityPayload = new Dictionary<string, object>
        {
            ["Activity.firstLaunchToday"] = !hasLastActiveDate || lastActiveDate != today,
            ["Activity.firstLaunchThisWeek"] = !hasLastActiveDate || GetStartOfWeek(lastActiveDate) != GetStartOfWeek(today),
            ["Activity.firstLaunchThisMonth"] = !hasLastActiveDate || lastActiveDate.Year != today.Year || lastActiveDate.Month != today.Month,
            ["Activity.newInstall"] = isNewInstall,
            ["Activity.installMonth"] = settings.TelemetryInstallMonth.Value,
        };

        settings.TelemetryLastActiveDate.Value = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        settings.SaveSettings();
    }

    /// <summary>
    /// Provides the hardware information for the client start signal.
    /// Pass null if it isn't available on this platform.
    /// </summary>
    public static void SetSystemSpecifications(SystemSpecifications? specifications)
        => systemSpecificationsSource.TrySetResult(specifications);

    /// <summary>
    /// Sends the client start signal with system information, client settings and activity flags.
    /// Call once the client has finished loading.
    /// </summary>
    public static void ReportClientStarted()
    {
        if (!IsEnabled)
            return;

        var payload = new Dictionary<string, object>(activityPayload);
        AddSystemInformation(payload);
        AddClientSettings(payload);

        // The hardware query runs in a separate thread and can take a while,
        // so wait for it in the background instead of blocking the UI.
        Task.Run(async () =>
        {
            try
            {
                Task<SystemSpecifications?> specificationsTask = systemSpecificationsSource.Task;
                Task completedTask = await Task.WhenAny(specificationsTask,
                    Task.Delay(SYSTEM_SPECIFICATIONS_TIMEOUT_MILLISECONDS)).ConfigureAwait(false);

                if (completedTask == specificationsTask && specificationsTask.Result != null)
                    AddHardwareInformation(payload, specificationsTask.Result);

                Signal("Client.started", payload);
            }
            catch (Exception ex)
            {
                Logger.Log("Telemetry: Failed to report client start: " + ex.Message);
            }
        });
    }

    /// <summary>
    /// Queues a signal and sends it in the background.
    /// Does nothing if the user has opted out of telemetry.
    /// </summary>
    /// <param name="type">The signal type, for example "Client.started".</param>
    /// <param name="payload">A flat set of values that describe the signal.</param>
    /// <param name="floatValue">An optional number that TelemetryDeck can sum and average.</param>
    public static void Signal(string type, Dictionary<string, object> payload, double? floatValue = null)
    {
        if (!IsEnabled)
            return;

        var fullPayload = new Dictionary<string, object>(GetDefaultPayload());
        foreach (var pair in payload)
            fullPayload[pair.Key] = pair.Value;

        var signal = new Dictionary<string, object>
        {
            ["appID"] = ClientConfiguration.Instance.TelemetryDeckAppID,
            ["clientUser"] = launchId,
            ["sessionID"] = launchId,
            ["type"] = type,
            ["payload"] = fullPayload,
#if DEBUG
            ["isTestMode"] = true,
#endif
        };

        if (floatValue.HasValue)
            signal["floatValue"] = floatValue.Value;

        lock (locker)
        {
            pendingSignals.Add(signal);
            sendTask = sendTask.ContinueWith(_ => SendPendingSignalsAsync()).Unwrap();
        }
    }

    /// <summary>
    /// Waits a short while for signals that are still being sent. Call when the client exits.
    /// </summary>
    public static void Shutdown()
    {
        Task task;

        lock (locker)
            task = sendTask;

        try
        {
            if (!task.Wait(SHUTDOWN_TIMEOUT_MILLISECONDS))
                Logger.Log("Telemetry: Signals were still being sent on exit and were dropped.");
        }
        catch (Exception ex)
        {
            Logger.Log("Telemetry: Failed to send signals on exit: " + ex.Message);
        }
    }

    private static async Task SendPendingSignalsAsync()
    {
        List<Dictionary<string, object>> signals;

        lock (locker)
        {
            if (pendingSignals.Count == 0)
                return;

            signals = new List<Dictionary<string, object>>(pendingSignals);
            pendingSignals.Clear();
        }

        try
        {
            string json = JsonSerializer.Serialize(signals);
#if DEBUG
            Logger.Log("Telemetry: Sending " + json);
#endif
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var cts = new CancellationTokenSource(REQUEST_TIMEOUT_MILLISECONDS);
            using var response = await httpClient.PostAsync(INGEST_URL, content, cts.Token).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                Logger.Log($"Telemetry: Sent {signals.Count} signal(s).");
            }
            else
            {
                string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Logger.Log($"Telemetry: Sending {signals.Count} signal(s) failed with status {(int)response.StatusCode}: {responseBody}");
            }
        }
        catch (Exception ex)
        {
            // Telemetry must never disrupt the client, so failed signals are dropped.
            Logger.Log($"Telemetry: Sending {signals.Count} signal(s) failed: {DescribeException(ex)}");
        }
    }

    /// <summary>
    /// Values that are included in every signal.
    /// </summary>
    private static Dictionary<string, object> GetDefaultPayload() => new()
    {
        ["Client.version"] = GitVersionInformation.AssemblySemVer,
        ["Client.engine"] = GetEngine(),
        ["Client.runtime"] = RuntimeInformation.FrameworkDescription,
        ["Game.name"] = ClientConfiguration.Instance.LocalGame,
        ["Game.version"] = ProgramConstants.GAME_VERSION,
    };

    /// <summary>
    /// Lists the type and message of the exception and its inner exceptions,
    /// so that TLS and certificate failures can be told apart from network failures.
    /// </summary>
    private static string DescribeException(Exception ex)
    {
        var parts = new List<string>();

        for (Exception? current = ex; current != null; current = current.InnerException)
            parts.Add(current.GetType().Name + ": " + current.Message);

        return string.Join(" ---> ", parts);
    }

    private static void AddSystemInformation(Dictionary<string, object> payload)
    {
        AddOperatingSystem(payload);
        payload["System.osDescription"] = RuntimeInformation.OSDescription;
        payload["System.osArchitecture"] = RuntimeInformation.OSArchitecture.ToString();
        payload["System.processArchitecture"] = RuntimeInformation.ProcessArchitecture.ToString();
        payload["System.logicalProcessors"] = Environment.ProcessorCount;
        payload["System.culture"] = CultureInfo.CurrentCulture.Name;

        try
        {
            payload["System.desktopResolution"] = GetResolutionBucket(XNAScreenResolutionManager.DesktopResolution);
        }
        catch (Exception ex)
        {
            Logger.Log("Telemetry: Failed to get the desktop resolution: " + ex.Message);
        }
    }

    /// <summary>
    /// Adds the operating system as one of a fixed set of values, so that dashboards can group by it.
    /// </summary>
    private static void AddOperatingSystem(Dictionary<string, object> payload)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                payload["System.os"] = "Linux";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                payload["System.os"] = "macOS";
            else
                payload["System.os"] = OTHER;

            return;
        }

        // Wine prefixes can claim to be any Windows version, so check for Wine first.
        bool isWine;

        try
        {
            isWine = Program.IsRunningUnderWine();
        }
        catch (Exception ex)
        {
            Logger.Log("Telemetry: Failed to check for Wine: " + ex.Message);
            isWine = false;
        }

        if (isWine)
        {
            payload["System.os"] = "Wine";
            return;
        }

        // The application manifests declare support for Windows 7 to 10,
        // so the version isn't capped by compatibility shims.
        Version version = Environment.OSVersion.Version;

        string os = (version.Major, version.Minor) switch
        {
            (6, 1) => "Win7",
            (6, 2) => "Win8",
            (6, 3) => "Win8.1",
            (10, 0) when version.Build >= 22000 => "Win11",
            (10, 0) => "Win10",
            _ => OTHER,
        };

        payload["System.os"] = os;
        payload["System.osBuild"] = version.Build;

        // Windows 7 SP1 is build 7601, the original release is build 7600.
        if (os == "Win7")
            payload["System.win7SP1"] = version.Build >= 7601;
    }

    private static void AddClientSettings(Dictionary<string, object> payload)
    {
        var settings = UserINISettings.Instance;

        payload["Settings.translation"] = settings.Translation.Value;
        payload["Settings.theme"] = settings.ClientTheme.Value;
        payload["Settings.renderer"] = string.IsNullOrEmpty(settings.Renderer) ? "Default" : settings.Renderer.Value;
        payload["Settings.ingameResolution"] = GetResolutionBucket(new ScreenResolution(settings.IngameScreenWidth, settings.IngameScreenHeight));
        payload["Settings.windowedMode"] = settings.WindowedMode.Value;
        payload["Settings.borderlessWindowedMode"] = settings.BorderlessWindowedMode.Value;
        payload["Settings.clientResolution"] = GetResolutionBucket(new ScreenResolution(settings.ClientResolutionX, settings.ClientResolutionY));
        payload["Settings.borderlessWindowedClient"] = settings.BorderlessWindowedClient.Value;
        payload["Settings.checkForUpdates"] = settings.CheckForUpdates.Value;
        payload["Settings.discordIntegration"] = settings.DiscordIntegration.Value;
        payload["Settings.steamIntegration"] = settings.SteamIntegration.Value;
        payload["Settings.enableP2P"] = settings.EnableP2P.Value;
    }

    private static void AddHardwareInformation(Dictionary<string, object> payload, SystemSpecifications specifications)
    {
        if (specifications.CpuName != null)
            payload["System.cpu"] = specifications.CpuName;

        if (specifications.VideoControllers.Count > 0)
            payload["System.gpu"] = string.Join("; ", specifications.VideoControllers);

        if (specifications.TotalMemoryBytes > 0)
            payload["System.memory"] = GetMemoryBucket(specifications.TotalMemoryBytes);
    }

    /// <summary>
    /// Rounds the memory size to a common bucket, so that unusual sizes can't tell machines apart.
    /// </summary>
    private static string GetMemoryBucket(ulong totalMemoryBytes)
    {
        double gigabytes = totalMemoryBytes / 1073741824.0;

        if (gigabytes < 6)
            return "4 GB or less";
        if (gigabytes < 12)
            return "8 GB";
        if (gigabytes < 24)
            return "16 GB";
        if (gigabytes < 48)
            return "32 GB";
        if (gigabytes < 96)
            return "64 GB";

        return "128 GB or more";
    }

    /// <summary>
    /// Returns the resolution if it's a common one, or "Other",
    /// so that unusual resolutions can't tell machines apart.
    /// </summary>
    private static string GetResolutionBucket(ScreenResolution resolution)
    {
        string text = resolution.ToString();
        return commonResolutions.Contains(text) ? text : OTHER;
    }

    private static DateTime GetStartOfWeek(DateTime date)
        => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    private static string GetEngine()
    {
#if XNA
        return "WindowsXNA";
#elif DX
        return "WindowsDX";
#elif GL && ISWINDOWS
        return "WindowsGL";
#else
        return "UniversalGL";
#endif
    }
}
