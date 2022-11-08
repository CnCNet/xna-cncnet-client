using System;
using System.Timers;
using DTAClient.Domain.Multiplayer.CnCNet.QuickMatch.Models;

namespace DTAClient.Domain.Multiplayer.CnCNet.QuickMatch;

public class QmMatchFoundTimer : Timer
{
    private const int MatchupFoundTimerInterval = 100;
    public QmRequestSpawnResponseSpawn Spawn { get; set; }

    public QmMatchFoundTimer() : base(MatchupFoundTimerInterval)
    {
        AutoReset = true;
    }

    public int GetInterval() => MatchupFoundTimerInterval;

    public void SetSpawn(QmRequestSpawnResponseSpawn spawn)
    {
        Spawn = spawn;
    }

    public void SetElapsedAction(Action elapsedAction) => Elapsed += (_, _) => elapsedAction();
}