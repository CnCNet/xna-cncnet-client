#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;
using ClientCore.Extensions;
using ClientGUI;
using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAClient.Domain.Multiplayer.CnCNet.Matchmaking
{
    public sealed class MatchmakingService : IDisposable
    {
        private readonly MatchmakingApiService apiService;
        private readonly ApiSpawnService spawnService;
        private readonly WindowManager windowManager;

        private readonly Func<string> localPlayerNameProvider;
        private readonly Func<int> chosenSideProvider;
        private readonly Func<bool> canJoinQueueProvider;
        private readonly Action<string> addNoticeAction;
        private readonly Action<bool> setQueueUiStateAction;

        private readonly Timer pollTimer;

        private bool isInQueue;
        private bool isBusy;
        private int consecutiveErrors;
        private string? activeSearchingLadder;
        private DateTime lastActionTime = DateTime.MinValue;
        private const double ActionCooldownMs = 1000;

        public MatchmakingService(
            MatchmakingApiService apiService,
            ApiSpawnService spawnService,
            WindowManager windowManager,
            Func<string> localPlayerNameProvider,
            Func<int> chosenSideProvider,
            Func<bool> canJoinQueueProvider,
            Action<string> addNoticeAction,
            Action<bool> setQueueUiStateAction)
        {
            this.apiService = apiService;
            this.spawnService = spawnService;
            this.windowManager = windowManager;
            this.localPlayerNameProvider = localPlayerNameProvider;
            this.chosenSideProvider = chosenSideProvider;
            this.canJoinQueueProvider = canJoinQueueProvider;
            this.addNoticeAction = addNoticeAction;
            this.setQueueUiStateAction = setQueueUiStateAction;

            pollTimer = new Timer();
            pollTimer.AutoReset = false;
            pollTimer.Elapsed += PollTimer_Elapsed;
        }

        public bool IsInQueue => isInQueue;

        public DateTime? QueueStartTime { get; private set; }

        private string LocalPlayerName => localPlayerNameProvider();

        public void ToggleQueue()
        {
            if (isInQueue)
            {
                LeaveQueue(true);
                return;
            }

            if (DateTime.Now.Subtract(lastActionTime).TotalMilliseconds < ActionCooldownMs)
            {
                return;
            }

            if (isBusy)
            {
                return;
            }

            lastActionTime = DateTime.Now;
            StartQueue();
        }

        public void CancelQueue()
        {
            if (isInQueue)
            {
                LeaveQueue(true);
            }
        }

        public Task<Dictionary<string, int>?> GetQueueCountsAsync()
        {
            return apiService.GetQueueCountsAsync();
        }

        public async void StartQueue()
        {
            if (isInQueue || isBusy)
            {
                return;
            }

            if (!canJoinQueueProvider())
            {
                SafeAddNotice("Cannot join matchmaking queue while in a game room or loading.".L10N("Client:Matchmaking:CannotJoinInRoom"));

                return;
            }

            isBusy = true;

            string ladder = MatchmakingSettings.Instance.Ladder;
            activeSearchingLadder = ladder;
            bool casual = MatchmakingSettings.Instance.Casual;
            int side = chosenSideProvider();

            QmMatchRequest request = CreateMatchRequest("match me up", casual, side);

            Logger.Log($"[Matchmaking] Starting search for {LocalPlayerName} on ladder {ladder} (casual={casual}, side={side})");

            isInQueue = true;
            QueueStartTime = DateTime.UtcNow;
            consecutiveErrors = 0;
            SafeSetQueueUiState(true);
            SafeAddNotice("Searching for opponent...".L10N("Client:Matchmaking:SearchingNotice"));

            QmMatchResponse? response = await apiService.SendMatchRequestAsync(ladder, LocalPlayerName, request);

            isBusy = false;

            if (!isInQueue)
            {
                Logger.Log("[Matchmaking] Initial request completed but queue was cancelled by user.");
                return;
            }

            if (response == null || response.Type == "error")
            {
                string desc = response?.Description ?? response?.Message ?? "Network error";
                Logger.Log($"[Matchmaking] Initial request failed: {desc}. Retrying in 3s...");
                consecutiveErrors++;
                pollTimer.Interval = 3000;
                pollTimer.Start();

                return;
            }

            HandleResponse(response);
        }

        public async void LeaveQueue(bool notifyServer)
        {
            if (!isInQueue)
            {
                return;
            }

            Logger.Log($"[Matchmaking] Leaving queue. notifyServer={notifyServer}");

            pollTimer.Stop();
            isInQueue = false;
            isBusy = false;
            QueueStartTime = null;
            consecutiveErrors = 0;
            SafeSetQueueUiState(false);
            SafeAddNotice("Matchmaking search cancelled.".L10N("Client:Matchmaking:CancelledNotice"));

            if (notifyServer)
            {
                try
                {
                    string ladder = activeSearchingLadder ?? MatchmakingSettings.Instance.Ladder;
                    activeSearchingLadder = null;
                    QmMatchRequest request = new QmMatchRequest
                    {
                        Type = "quit",
                        Casual = MatchmakingSettings.Instance.Casual,
                        Version = "2.0"
                    };

                    await apiService.SendMatchRequestAsync(ladder, LocalPlayerName, request);
                }
                catch (Exception ex)
                {
                    Logger.Log($"[Matchmaking] Error sending quit request: {ex.Message}");
                }
            }
        }

        private void HandleResponse(QmMatchResponse response)
        {
            if (!isInQueue)
            {
                return;
            }

            switch (response.Type.ToLowerInvariant())
            {
                case "please wait":
                case "checkback":
                    consecutiveErrors = 0;
                    int waitSeconds = response.CheckBack > 0 ? Math.Min(response.CheckBack, 2) : 2;
                    Logger.Log($"[Matchmaking] Queued. Waiting {waitSeconds}s for next check...");
                    pollTimer.Interval = 1500;
                    pollTimer.Start();
                    break;

                case "spawn":
                    consecutiveErrors = 0;
                    Logger.Log("[Matchmaking] Match found! Spawning game...");
                    SafeAddNotice("Match found! Launching game...".L10N("Client:Matchmaking:MatchFoundNotice"));

                    pollTimer.Stop();
                    isInQueue = false;
                    QueueStartTime = null;
                    SafeSetQueueUiState(false);

                    bool filesWritten = spawnService.WriteSpawnFiles(response);

                    if (!filesWritten)
                    {
                        SafeAddNotice("Error preparing match files.".L10N("Client:Matchmaking:SpawnError"));

                        return;
                    }

                    windowManager.AddCallback(new Action(() =>
                    {
                        GameProcessLogic.StartGameProcess(windowManager);
                    }), null);

                    break;

                case "quit":
                    consecutiveErrors = 0;
                    Logger.Log("[Matchmaking] Server confirmed quit.");
                    LeaveQueue(false);
                    break;

                case "error":
                default:
                    string desc = response.Description ?? response.Message ?? response.Type;
                    Logger.Log($"[Matchmaking] Error or unexpected response: {desc}");

                    if (isInQueue && consecutiveErrors < 3)
                    {
                        consecutiveErrors++;
                        Logger.Log($"[Matchmaking] Transient error ({consecutiveErrors}/3). Retrying in 2s...");
                        pollTimer.Interval = 2000;
                        pollTimer.Start();
                        break;
                    }

                    SafeAddNotice(string.Format("Matchmaking error: {0}".L10N("Client:Matchmaking:ErrorFormat"), desc));
                    LeaveQueue(false);
                    break;
            }
        }

        private async void PollTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (!isInQueue)
            {
                return;
            }

            string ladder = MatchmakingSettings.Instance.Ladder;
            bool casual = MatchmakingSettings.Instance.Casual;
            int side = chosenSideProvider();

            QmMatchRequest request = CreateMatchRequest("match me up", casual, side);

            QmMatchResponse? response = await apiService.SendMatchRequestAsync(ladder, LocalPlayerName, request);

            if (!isInQueue)
            {
                Logger.Log("[Matchmaking] Poll request completed but queue was cancelled by user.");
                return;
            }

            if (response == null)
            {
                pollTimer.Interval = 3000;
                pollTimer.Start();

                return;
            }

            HandleResponse(response);
        }

        private void SafeSetQueueUiState(bool inQueue)
        {
            windowManager.AddCallback(new Action(() =>
            {
                setQueueUiStateAction(inQueue);
            }), null);
        }

        private void SafeAddNotice(string message)
        {
            windowManager.AddCallback(new Action(() =>
            {
                addNoticeAction(message);
            }), null);
        }

        private QmMatchRequest CreateMatchRequest(string type, bool casual, int side)
        {
            string localIp = GetLocalIpAddress();

            return new QmMatchRequest
            {
                Type = type,
                Casual = casual,
                Side = side,
                Version = "2.0",
                LanIp = localIp,
                LanPort = 50000,
                IpAddress = localIp,
                IpPort = 50000
            };
        }

        private static string GetLocalIpAddress()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                if (socket.LocalEndPoint is IPEndPoint endPoint)
                {
                    return endPoint.Address.ToString();
                }
            }
            catch
            {
                // Fallback below
            }

            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
            }
            catch
            {
                // Fallback below
            }

            return "127.0.0.1";
        }

        public void Dispose()
        {
            pollTimer.Dispose();
            apiService.Dispose();
        }
    }
}
