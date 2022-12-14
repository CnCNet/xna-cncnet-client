using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.Extensions;
using Rampastring.Tools;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// Handles sharing maps.
    /// </summary>
    public static class MapSharer
    {
        public static event EventHandler<MapEventArgs> MapUploadFailed;

        public static event EventHandler<MapEventArgs> MapUploadComplete;

        public static event EventHandler<MapEventArgs> MapUploadStarted;

        public static event EventHandler<SHA1EventArgs> MapDownloadFailed;

        public static event EventHandler<SHA1EventArgs> MapDownloadComplete;

        public static event EventHandler<SHA1EventArgs> MapDownloadStarted;

        private volatile static List<string> MapDownloadQueue = new();
        private volatile static List<Map> MapUploadQueue = new();
        private volatile static List<string> UploadedMaps = new();

        private static readonly object locker = new();

        private const string MAPDB_URL = "https://mapdb.cncnet.org/";

        /// <summary>
        /// Adds a map into the CnCNet map upload queue.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="myGame">The short name of the game that is being played (DTA, TI, MO, etc).</param>
        public static void UploadMap(Map map, string myGame)
        {
            lock (locker)
            {
                if (UploadedMaps.Contains(map.SHA1) || MapUploadQueue.Contains(map))
                {
                    Logger.Log("MapSharer: Already uploading map " + map.BaseFilePath + " - returning.");
                    return;
                }

                MapUploadQueue.Add(map);

                if (MapUploadQueue.Count == 1)
                    UploadAsync(map, myGame.ToLower()).HandleTask();
            }
        }

        private static async ValueTask UploadAsync(Map map, string myGameId)
        {
            MapUploadStarted?.Invoke(null, new MapEventArgs(map));

            Logger.Log("MapSharer: Starting upload of " + map.BaseFilePath);

            (string message, bool success) = await MapUploadAsync(map, myGameId).ConfigureAwait(false);

            if (success)
            {
                MapUploadComplete?.Invoke(null, new MapEventArgs(map));

                lock (locker)
                {
                    UploadedMaps.Add(map.SHA1);
                }

                Logger.Log("MapSharer: Uploading map " + map.BaseFilePath + " completed successfully.");
            }
            else
            {
                MapUploadFailed?.Invoke(null, new MapEventArgs(map));

                Logger.Log("MapSharer: Uploading map " + map.BaseFilePath + " failed! Returned message: " + message);
            }

            lock (locker)
            {
                MapUploadQueue.Remove(map);

                if (MapUploadQueue.Count > 0)
                {
                    Map nextMap = MapUploadQueue[0];

                    Logger.Log("MapSharer: There are additional maps in the queue.");

                    UploadAsync(nextMap, myGameId).HandleTask();
                }
            }
        }

        private static async ValueTask<(string Message, bool Success)> MapUploadAsync(Map map, string gameName)
        {
            using MemoryStream zipStream = CreateZipFile(map.CompleteFilePath);

            try
            {
                var files = new List<FileToUpload>
                    {
                        new("file", FormattableString.Invariant($"{map.SHA1}.zip"), "mapZip", zipStream)
                    };
                var values = new NameValueCollection
                    {
                        { "game", gameName.ToLower() }
                    };
                string response = await UploadFilesAsync(files, values).ConfigureAwait(false);

                if (!response.Contains("Upload succeeded!"))
                    return (response, false);

                Logger.Log("MapSharer: Upload response: " + response);

                return (string.Empty, true);
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex);
                return (ex.Message, false);
            }
        }

        private static async ValueTask<string> UploadFilesAsync(List<FileToUpload> files, NameValueCollection values)
        {
            using var multipartFormDataContent = new MultipartFormDataContent();

            // Write the values
            foreach (string name in values.Keys)
            {
                multipartFormDataContent.Add(new StringContent(values[name]), name);
            }

            // Write the files
            foreach (FileToUpload file in files)
            {
                var streamContent = new StreamContent(file.Stream)
                {
                    Headers = { ContentType = new MediaTypeHeaderValue("application/" + file.ContentType) }
                };
                multipartFormDataContent.Add(streamContent, file.Name, file.Filename);
            }

            HttpResponseMessage httpResponseMessage = await Constants.CnCNetHttpClient.PostAsync($"{MAPDB_URL}upload", multipartFormDataContent).ConfigureAwait(false);

            return await httpResponseMessage.EnsureSuccessStatusCode().Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        private static MemoryStream CreateZipFile(string file)
        {
            var zipStream = new MemoryStream(1024);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true);
            archive.CreateEntryFromFile(SafePath.CombineFilePath(ProgramConstants.GamePath, file), file);

            return zipStream;
        }

        private static void ExtractZipFile(Stream stream, string file)
        {
            using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);

            zipArchive.Entries.FirstOrDefault().ExtractToFile(file, true);
        }

        public static void DownloadMap(string sha1, string myGame, string mapName)
        {
            lock (locker)
            {
                if (MapDownloadQueue.Contains(sha1))
                {
                    Logger.Log("MapSharer: Map " + sha1 + " already exists in the download queue.");
                    return;
                }

                MapDownloadQueue.Add(sha1);

                if (MapDownloadQueue.Count == 1)
                    DownloadAsync(sha1, myGame.ToLower(), mapName).HandleTask();
            }
        }

        private static async ValueTask DownloadAsync(string sha1, string myGameId, string mapName)
        {
            Logger.Log("MapSharer: Preparing to download map " + sha1 + " with name: " + mapName);

            try
            {
                Logger.Log("MapSharer: MapDownloadStarted");
                MapDownloadStarted?.Invoke(null, new SHA1EventArgs(sha1, mapName));
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, "MapSharer ERROR");
            }

            (string error, bool success) = await DownloadMainAsync(sha1, myGameId, mapName).ConfigureAwait(false);

            lock (locker)
            {
                if (success)
                {
                    Logger.Log("MapSharer: Download of map " + sha1 + " completed succesfully.");
                    MapDownloadComplete?.Invoke(null, new SHA1EventArgs(sha1, mapName));
                }
                else
                {
                    Logger.Log("MapSharer: Download of map " + sha1 + "failed! Reason: " + error);
                    MapDownloadFailed?.Invoke(null, new SHA1EventArgs(sha1, mapName));
                }

                MapDownloadQueue.Remove(sha1);

                if (MapDownloadQueue.Any())
                {
                    Logger.Log("MapSharer: Continuing custom map downloads.");
                    DownloadAsync(MapDownloadQueue[0], myGameId, mapName).HandleTask();
                }
            }
        }

        public static string GetMapFileName(string sha1, string mapName)
            => FormattableString.Invariant($"{mapName}_{sha1}");

        private static async ValueTask<(string Error, bool Success)> DownloadMainAsync(string sha1, string myGame, string mapName)
        {
            string customMapsDirectory = SafePath.CombineDirectoryPath(ProgramConstants.GamePath, "Maps", "Custom");
            string mapFileName = GetMapFileName(sha1, mapName);
            string newFile = SafePath.CombineFilePath(customMapsDirectory, FormattableString.Invariant($"{mapFileName}.map"));
            Stream stream;

            try
            {
                string address = FormattableString.Invariant($"{MAPDB_URL}{myGame}/{sha1}.zip");
                Logger.Log($"MapSharer: Downloading URL: {MAPDB_URL}{address})");
                stream = await Constants.CnCNetHttpClient.GetStreamAsync(address).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex);

                return (ex.Message, false);
            }

            ExtractZipFile(stream, newFile);

            return (null, true);
        }

        private readonly record struct FileToUpload(string Name, string Filename, string ContentType, Stream Stream);
    }
}