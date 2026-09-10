#nullable enable
using System;

namespace DTAClient.Domain.Multiplayer.CnCNet;

public enum PingQualityTier
{
    Unknown,
    Good,
    Fair,
    Poor,
    Bad
}

public static class PingQualityRules
{
    // Legacy and static modes measure the round trip to the tunnel server.
    public const int GoodMaxMs = 100;
    public const int FairMaxMs = 250;
    public const int PoorMaxMs = 350;

    // Dynamic V3 measures the full player-to-player round trip, via relay or P2P.
    public const int V3GoodMaxMs = 180;
    public const int V3FairMaxMs = 350;
    public const int V3PoorMaxMs = 500;

    public const int HighPingWarningMs = V3PoorMaxMs;
    public const int KickSuggestionMinWorstMs = 300;
    public const int KickSuggestionMinImprovementMs = 150;
    public const int MaterialChangeMinDeltaMs = 25;

    public static PingQualityTier GetTier(PingValue ping)
        => ping.IsValid() ? GetTier(ping.Milliseconds) : PingQualityTier.Unknown;

    public static PingQualityTier GetTier(int milliseconds)
        => GetTier(milliseconds, GoodMaxMs, FairMaxMs, PoorMaxMs);

    public static PingQualityTier GetV3Tier(PingValue ping)
        => ping.IsValid() ? GetV3Tier(ping.Milliseconds) : PingQualityTier.Unknown;

    public static PingQualityTier GetV3Tier(int milliseconds)
        => GetTier(milliseconds, V3GoodMaxMs, V3FairMaxMs, V3PoorMaxMs);

    private static PingQualityTier GetTier(int milliseconds, int goodMaxMs, int fairMaxMs, int poorMaxMs)
    {
        if (milliseconds < 0)
            return PingQualityTier.Unknown;

        if (milliseconds <= goodMaxMs)
            return PingQualityTier.Good;

        if (milliseconds <= fairMaxMs)
            return PingQualityTier.Fair;

        if (milliseconds <= poorMaxMs)
            return PingQualityTier.Poor;

        return PingQualityTier.Bad;
    }

    // Warnings and live-update broadcasts operate on dynamic V3 pair pings.
    public static bool IsHighForWarning(PingValue ping)
        => ping.IsValid() && ping.Milliseconds > HighPingWarningMs;

    public static bool IsMaterialChange(int oldMilliseconds, int newMilliseconds)
        => Math.Abs(newMilliseconds - oldMilliseconds) > MaterialChangeMinDeltaMs ||
           GetV3Tier(oldMilliseconds) != GetV3Tier(newMilliseconds);
}
