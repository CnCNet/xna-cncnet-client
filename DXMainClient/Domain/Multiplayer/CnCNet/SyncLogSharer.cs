#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using ClientCore;
using ClientCore.PlatformShim;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// Uploads SYNC*.TXT desync logs to the CnCNet map API after a multiplayer game desyncs.
    /// Call <see cref="SetGameInfo"/> before the game launches, then <see cref="UploadSyncLogs"/>
    /// after the game exits and sync files have been found.
    /// </summary>
    public static class SyncLogSharer
    {
        private const int UPLOAD_TIMEOUT = 30000; // ms
        private const int FILE_BUFFER_SIZE = 65536;

        // Set by CnCNetGameLobby before game start. All values are shared across every
        // client in the same game session, so they can be used to derive the game_hash.
        private static string _lastMapSha1 = string.Empty;
        private static string _lastMapName = string.Empty;
        private static string _lastGameMode = string.Empty;
        private static string _lastGameSlug = string.Empty;
        private static string _lastPlayerName = string.Empty;
        private static int _lastRandomSeed;

        /// <summary>
        /// Store the game metadata before launching. Must be called on the game-launch path.
        /// </summary>
        public static void SetGameInfo(
            string mapSha1,
            string mapName,
            string gameMode,
            string gameSlug,
            string playerName,
            int randomSeed)
        {
            _lastMapSha1 = mapSha1 ?? string.Empty;
            _lastMapName = mapName ?? string.Empty;
            _lastGameMode = gameMode ?? string.Empty;
            _lastGameSlug = gameSlug ?? string.Empty;
            _lastPlayerName = playerName ?? string.Empty;
            _lastRandomSeed = randomSeed;
        }

        /// <summary>
        /// Finds SYNC*.TXT files written after <paramref name="gameStarted"/> in
        /// <paramref name="syncErrorLogsDirectory"/> and uploads the first chunk of each (index 0–7).
        /// Runs in a background thread; fire and forget.
        /// </summary>
        /// <param name="syncErrorLogsDirectory">
        ///     The directory where <c>CopySyncErrorLogs</c> deposited the renamed files.
        /// </param>
        /// <param name="gameStarted">
        ///     The approximate time the game started, used to identify files from this session.
        /// </param>
        public static void UploadSyncLogs(string syncErrorLogsDirectory, DateTime gameStarted)
        {
            string uploadUrl = ClientConfiguration.Instance.CnCNetSyncLogUploadURL;
            if (string.IsNullOrWhiteSpace(uploadUrl))
            {
                Logger.Log("SyncLogSharer: Upload URL not configured, skipping.");
                return;
            }

            string gameHash = ComputeGameHash(_lastMapSha1, _lastRandomSeed, _lastGameSlug);

            // Collect the SYNC files that were just copied (one per player slot, index 0–7).
            // Non-Ares: CopySyncErrorLogs names them SYNC{n}_yyyy_MM_dd_HH_mm.TXT.
            // Ares: files are copied as SYNC{n}.TXT into the snapshot directory.
            // SYNC{i}*.TXT matches both; the CreationTime filter isolates this session.
            var filesToUpload = new List<(int index, string path)>();
            for (int i = 0; i < 8; i++)
            {
                DirectoryInfo dir = new DirectoryInfo(syncErrorLogsDirectory);
                if (!dir.Exists) break;

                FileInfo found = dir
                    .EnumerateFiles($"SYNC{i}*.TXT")
                    .Where(f => f.CreationTime >= gameStarted.AddSeconds(-5))
                    .OrderByDescending(f => f.CreationTime)
                    .FirstOrDefault();

                if (found != null)
                    filesToUpload.Add((i, found.FullName));
            }

            if (filesToUpload.Count == 0)
            {
                Logger.Log("SyncLogSharer: No recent SYNC files found to upload.");
                return;
            }

            Logger.Log($"SyncLogSharer: Uploading {filesToUpload.Count} sync log(s) for game hash {gameHash[..8]}…");

            ThreadPool.QueueUserWorkItem(_ =>
            {
                foreach ((int index, string path) in filesToUpload)
                {
                    try
                    {
                        UploadFile(uploadUrl, path, index, gameHash);
                        Logger.Log($"SyncLogSharer: Uploaded SYNC{index} successfully.");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"SyncLogSharer: Failed to upload SYNC{index}: {ex.Message}");
                    }
                }
            });
        }

        private static void UploadFile(string uploadUrl, string filePath, int syncIndex, string gameHash)
        {
            ServicePointManager.Expect100Continue = false;

            using FileStream stream = File.OpenRead(filePath);

            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x", NumberFormatInfo.InvariantInfo);
            WebRequest request = WebRequest.Create(uploadUrl);
            request.Timeout = UPLOAD_TIMEOUT;
            request.Method = "POST";
            request.ContentType = $"multipart/form-data; boundary={boundary}";
            boundary = "--" + boundary;

            var fields = new Dictionary<string, string>
            {
                { "game_hash",        gameHash },
                { "player_name",      _lastPlayerName },
                { "sync_file_index",  syncIndex.ToString() },
                { "map_sha1",         _lastMapSha1 },
                { "map_name",         _lastMapName },
                { "game_mode",        _lastGameMode },
                { "game_slug",        _lastGameSlug },
            };

            using Stream requestStream = request.GetRequestStream();

            // Write form text fields.
            foreach (KeyValuePair<string, string> kvp in fields)
            {
                WriteFormField(requestStream, boundary, kvp.Key, kvp.Value);
            }

            // Write the file.
            string fileName = Path.GetFileName(filePath);
            byte[] header = Encoding.ASCII.GetBytes(
                $"{boundary}\r\nContent-Disposition: form-data; name=\"file\"; filename=\"{fileName}\"\r\n" +
                $"Content-Type: text/plain\r\n\r\n");
            requestStream.Write(header, 0, header.Length);

            byte[] buffer = new byte[FILE_BUFFER_SIZE];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                requestStream.Write(buffer, 0, read);

            byte[] footer = Encoding.ASCII.GetBytes($"\r\n{boundary}--\r\n");
            requestStream.Write(footer, 0, footer.Length);

            using WebResponse response = request.GetResponse();
            using StreamReader reader = new StreamReader(response.GetResponseStream()!);
            Logger.Log($"SyncLogSharer: Server response for SYNC{syncIndex}: " + reader.ReadToEnd());
        }

        private static void WriteFormField(Stream stream, string boundary, string name, string value)
        {
            byte[] bytes = EncodingExt.UTF8NoBOM.GetBytes(
                $"{boundary}\r\nContent-Disposition: form-data; name=\"{name}\"\r\n\r\n{value}\r\n");
            stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Derives a stable, session-unique identifier from values shared by all game clients.
        /// The random seed is set once per game and is identical on every machine.
        /// Format: <c>SHA1("{mapSha1}|{randomSeed}|{gameSlug}")</c> as a lowercase hex string.
        /// </summary>
        private static string ComputeGameHash(string mapSha1, int randomSeed, string gameSlug)
        {
            string input = $"{mapSha1}|{randomSeed}|{gameSlug}";
            using SHA1 sha1 = SHA1.Create();
            byte[] hash = sha1.ComputeHash(EncodingExt.UTF8NoBOM.GetBytes(input));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
