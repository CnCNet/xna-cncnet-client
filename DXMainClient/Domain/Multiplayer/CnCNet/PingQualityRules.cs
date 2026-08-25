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
    public const int GoodMaxMs = 100;
    public const int FairMaxMs = 250;
    public const int PoorMaxMs = 350;

    public const int HighPingWarningMs = PoorMaxMs;
    public const int KickSuggestionMinWorstMs = 300;
    public const int KickSuggestionMinImprovementMs = 150;
    public const int MaterialChangeMinDeltaMs = 25;

    public static PingQualityTier GetTier(PingValue ping)
        => ping.IsValid() ? GetTier(ping.Milliseconds) : PingQualityTier.Unknown;

    public static PingQualityTier GetTier(int milliseconds)
    {
        if (milliseconds < 0)
            return PingQualityTier.Unknown;

        if (milliseconds <= GoodMaxMs)
            return PingQualityTier.Good;

        if (milliseconds <= FairMaxMs)
            return PingQualityTier.Fair;

        if (milliseconds <= PoorMaxMs)
            return PingQualityTier.Poor;

        return PingQualityTier.Bad;
    }

    public static bool IsHighForWarning(PingValue ping)
        => ping.IsValid() && ping.Milliseconds > HighPingWarningMs;

    public static bool IsMaterialChange(int oldMilliseconds, int newMilliseconds)
        => Math.Abs(newMilliseconds - oldMilliseconds) > MaterialChangeMinDeltaMs ||
           GetTier(oldMilliseconds) != GetTier(newMilliseconds);
}
