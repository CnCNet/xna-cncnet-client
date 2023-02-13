using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.Extensions;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet.Replays;

internal sealed class ReplayHandler : IAsyncDisposable
{
    private readonly Dictionary<uint, FileStream> replayFileStreams = new();

    private DateTimeOffset startTimestamp;
    private DirectoryInfo replayDirectory;
    private bool gameStarted;
    private int replayId;
    private uint gameLocalPlayerId;

    public void SetupRecording(int replayId, uint gameLocalPlayerId)
    {
        this.replayId = replayId;
        this.gameLocalPlayerId = gameLocalPlayerId;
        startTimestamp = DateTimeOffset.Now;
        replayDirectory = SafePath.GetDirectory(ProgramConstants.GamePath, ProgramConstants.REPLAYS_DIRECTORY, replayId.ToString(CultureInfo.InvariantCulture));
        gameStarted = false;

        replayDirectory.Create();
        replayFileStreams.Add(gameLocalPlayerId, CreateReplayFileStream());
    }

    public async ValueTask StopRecordingAsync(List<uint> gamePlayerIds, List<PlayerInfo> playerInfos, List<V3GameTunnelHandler> v3GameTunnelHandlers)
    {
        foreach (V3GameTunnelHandler v3GameTunnelHandler in v3GameTunnelHandlers)
        {
            v3GameTunnelHandler.RaiseRemoteHostDataReceivedEvent -= RemoteHostConnection_DataReceivedAsync;
            v3GameTunnelHandler.RaiseLocalGameDataReceivedEvent -= LocalGameConnection_DataReceivedAsync;
        }

        if (!(replayDirectory?.Exists ?? false))
            return;

        FileInfo spawnFile = SafePath.GetFile(replayDirectory.FullName, ProgramConstants.SPAWNER_SETTINGS);
        string settings = null;
        Dictionary<uint, string> playerMappings = new();

        if (spawnFile.Exists)
        {
            settings = await File.ReadAllTextAsync(spawnFile.FullName, CancellationToken.None).ConfigureAwait(false);
            var spawnIni = new IniFile(spawnFile.FullName);
            IniSection settingsSection = spawnIni.GetSection("Settings");
            string playerName = settingsSection.GetStringValue("Name", null);
            uint playerId = gamePlayerIds[playerInfos.Single(q => q.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase)).Index];

            playerMappings.Add(playerId, playerName);

            for (int i = 1; i < settingsSection.GetIntValue("PlayerCount", 0); i++)
            {
                IniSection otherPlayerSection = spawnIni.GetSection($"Other{i}");

                if (otherPlayerSection is not null)
                {
                    playerName = otherPlayerSection.GetStringValue("Name", null);
                    playerId = gamePlayerIds[playerInfos.Single(q => q.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase)).Index];

                    playerMappings.Add(playerId, playerName);
                }
            }
        }

        List<ReplayData> replayDataList = await GenerateReplayDataAsync().ConfigureAwait(false);
        var replay = new Replay(replayId, settings, startTimestamp, gameLocalPlayerId, playerMappings, replayDataList.OrderBy(q => q.TimestampOffset).ToList());
        var tempReplayFileStream = new MemoryStream();

        await using (tempReplayFileStream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(tempReplayFileStream, replay, cancellationToken: CancellationToken.None).ConfigureAwait(false);

            tempReplayFileStream.Position = 0L;

            FileStream replayFileStream = new(
                SafePath.CombineFilePath(replayDirectory.Parent.FullName, FormattableString.Invariant($"{replayId}.cnc")),
                new FileStreamOptions
                {
                    Access = FileAccess.Write,
                    Mode = FileMode.CreateNew,
                    Options = FileOptions.Asynchronous
                });

            await using (replayFileStream.ConfigureAwait(false))
            {
                var compressionStream = new GZipStream(replayFileStream, CompressionMode.Compress);

                await using (compressionStream.ConfigureAwait(false))
                {
                    await tempReplayFileStream.CopyToAsync(compressionStream, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }

        SafePath.DeleteFileIfExists(spawnFile.FullName);
    }

    public async ValueTask DisposeAsync()
    {
        foreach ((_, FileStream fileStream) in replayFileStreams)
            await fileStream.DisposeAsync().ConfigureAwait(false);

        replayFileStreams.Clear();
        replayDirectory?.Refresh();

        if (replayDirectory?.Exists ?? false)
            SafePath.DeleteDirectoryIfExists(true, replayDirectory.FullName);
    }

    public void RemoteHostConnection_DataReceivedAsync(object sender, DataReceivedEventArgs e)
        => SaveReplayDataAsync(((V3RemotePlayerConnection)sender).PlayerId, e).HandleTask();

    public void LocalGameConnection_DataReceivedAsync(object sender, DataReceivedEventArgs e)
    {
        if (!gameStarted)
        {
            gameStarted = true;

            FileInfo spawnFileInfo = SafePath.GetFile(ProgramConstants.GamePath, ProgramConstants.SPAWNER_SETTINGS);

            spawnFileInfo.CopyTo(SafePath.CombineFilePath(replayDirectory.FullName, spawnFileInfo.Name));
        }

        SaveReplayDataAsync(((V3LocalPlayerConnection)sender).PlayerId, e).HandleTask();
    }

    private async ValueTask<List<ReplayData>> GenerateReplayDataAsync()
    {
        var replayDataList = new List<ReplayData>();

        foreach (FileStream fileStream in replayFileStreams.Values.Where(q => q.Length > 0L))
        {
            await fileStream.WriteAsync(new UTF8Encoding().GetBytes(new[] { ']' })).ConfigureAwait(false);

            fileStream.Position = 0L;

            replayDataList.AddRange(await JsonSerializer.DeserializeAsync<List<ReplayData>>(
                fileStream, new JsonSerializerOptions { AllowTrailingCommas = true }, cancellationToken: CancellationToken.None).ConfigureAwait(false));
        }

        return replayDataList;
    }

    private async ValueTask SaveReplayDataAsync(uint playerId, DataReceivedEventArgs e)
    {
        if (!replayFileStreams.TryGetValue(playerId, out FileStream fileStream))
        {
            fileStream = CreateReplayFileStream();

            if (!replayFileStreams.TryAdd(playerId, fileStream))
                await fileStream.DisposeAsync().ConfigureAwait(false);

            replayFileStreams.TryGetValue(playerId, out fileStream);
        }

        if (fileStream.Position is 0L)
            await fileStream.WriteAsync(new UTF8Encoding().GetBytes(new[] { '[' })).ConfigureAwait(false);

        var replayData = new ReplayData(e.Timestamp - startTimestamp, playerId, e.GameData);
        var tempStream = new MemoryStream();

        await using (tempStream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(tempStream, replayData, cancellationToken: CancellationToken.None).ConfigureAwait(false);
            await tempStream.WriteAsync(new UTF8Encoding().GetBytes(new[] { ',' })).ConfigureAwait(false);

            tempStream.Position = 0L;

            await tempStream.CopyToAsync(fileStream).ConfigureAwait(false);
        }
    }

    private FileStream CreateReplayFileStream()
        => new(
            SafePath.CombineFilePath(replayDirectory.FullName, Guid.NewGuid().ToString()),
            new FileStreamOptions
            {
                Access = FileAccess.ReadWrite,
                Mode = FileMode.CreateNew,
                Options = FileOptions.Asynchronous | FileOptions.DeleteOnClose
            });
}