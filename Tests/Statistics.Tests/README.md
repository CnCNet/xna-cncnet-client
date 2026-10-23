# Statistics parser tests

From the repository root, with the SDK specified by `global.json`:

```sh
dotnet restore DXClient.slnx -p:Configuration=UniversalGLRelease
dotnet test Tests/Statistics.Tests/Statistics.Tests.csproj -p:Configuration=UniversalGLRelease --no-restore
```

The suite covers the shared Westwood packet reader, Dune 2000 regressions,
and Red Alert statistics, including the supplied resignation dump in
`TestData/stats.dmp`. Synthetic packets exercise nonzero big-endian counters,
outcomes, spectators, alliances, missing participants, and malformed data.

Red Alert uses `DURA` for elapsed seconds, `AFPS` for frame rate, and `HRVn`
for harvested resources. `TIME` is a start timestamp and `CRDn` is remaining
credits. Player fields use suffixes 1–8; Dune 2000 uses 0–7.

Red Alert has no direct loss or score field. Losses are the positive differences
between built and remaining counts for each object type; deployments, sales,
and other noncombat removals can therefore count as losses. Score and the
lobby's side/color metadata are preserved. CnCNet outcomes use defeat,
resignation, spectator, disconnect, and mutual-alliance flags. A local defeat
or resignation completes that player's statistics, as with the text parser;
surviving hostile players are not awarded a win while their game continues.

Format references: [CnCNet spawner statistics](https://github.com/CnCNet/ra-patches/blob/master/src/spawner_stats.asm),
[original RA statistics](https://github.com/electronicarts/CnC_Remastered_Collection/blob/master/REDALERT/STATS.CPP),
and [original house outcome logic](https://github.com/electronicarts/CnC_Remastered_Collection/blob/master/REDALERT/HOUSE.CPP).
