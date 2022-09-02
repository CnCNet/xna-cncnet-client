using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ClientCore;
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

        private const string MAPDB_URL = "https://mapdb.cncnet.org/upload";

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
                    UploadAsync(map, myGame.ToLower());
            }
        }

        private static async Task UploadAsync(Map map, string myGameId)
        {
            MapUploadStarted?.Invoke(null, new MapEventArgs(map));

            Logger.Log("MapSharer: Starting upload of " + map.BaseFilePath);

            (string message, bool success) = await MapUploadAsync(MAPDB_URL, map, myGameId);

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

                    UploadAsync(nextMap, myGameId);
                }
            }
        }

        private static async Task<(string Message, bool Success)> MapUploadAsync(string address, Map map, string gameName)
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
                string response = await UploadFilesAsync(address, files, values);

                if (!response.Contains("Upload succeeded!"))
                    return (response, false);

                Logger.Log("MapSharer: Upload response: " + response);

                return (string.Empty, true);
            }
            catch (Exception ex)
            {
                PreStartup.LogException(ex);
                return (ex.Message, false);
            }
        }

        private static async Task<string> UploadFilesAsync(string address, List<FileToUpload> files, NameValueCollection values)
        {
            using HttpClient client = GetHttpClient();

            var multipartFormDataContent = new MultipartFormDataContent();

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

            HttpResponseMessage httpResponseMessage = await client.PostAsync(address, multipartFormDataContent);

            return await httpResponseMessage.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
        }

        private static HttpClient GetHttpClient()
        {
            var httpClientHandler = new HttpClientHandler
            {
#if NETFRAMEWORK
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
#else
                AutomaticDecompression = DecompressionMethods.All
#endif
            };

            return new HttpClient(httpClientHandler, true)
            {
                Timeout = TimeSpan.FromMilliseconds(10000),
#if !NETFRAMEWORK
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
#endif
            };
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
                    DownloadAsync(sha1, myGame.ToLower(), mapName);
            }
        }

        private static async Task DownloadAsync(string sha1, string myGameId, string mapName)
        {
            Logger.Log("MapSharer: Preparing to download map " + sha1 + " with name: " + mapName);

            try
            {
                Logger.Log("MapSharer: MapDownloadStarted");
                MapDownloadStarted?.Invoke(null, new SHA1EventArgs(sha1, mapName));
            }
            catch (Exception ex)
            {
                PreStartup.LogException(ex, "MapSharer ERROR");
            }

            (string error, bool success) = await DownloadMainAsync(sha1, myGameId, mapName);

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
                    DownloadAsync(MapDownloadQueue[0], myGameId, mapName);
                }
            }
        }

        public static string GetMapFileName(string sha1, string mapName)
            => mapName + "_" + sha1;

        private static async Task<(string Error, bool Success)> DownloadMainAsync(string sha1, string myGame, string mapName)
        {
            string customMapsDirectory = SafePath.CombineDirectoryPath(ProgramConstants.GamePath, "Maps", "Custom");
            string mapFileName = GetMapFileName(sha1, mapName);
            string newFile = SafePath.CombineFilePath(customMapsDirectory, FormattableString.Invariant($"{mapFileName}.map"));
            using HttpClient client = GetHttpClient();
            Stream stream;

            try
            {
                string address = "https://mapdb.cncnet.org/" + myGame + "/" + sha1 + ".zip";
                Logger.Log("MapSharer: Downloading URL: " + address);
                stream = await client.GetStreamAsync(address);
            }
            catch (Exception ex)
            {
                PreStartup.LogException(ex);

                return (ex.Message, false);
            }

            ExtractZipFile(stream, newFile);

            return (null, true);
        }

#if NETFRAMEWORK
        private sealed class FileToUpload
        {
            public FileToUpload(string name, string filename, string contentType, Stream stream)
            {
                Name = name;
                Filename = filename;
                ContentType = contentType;
                Stream = stream;
            }

            public string Name { get; set; }
            public string Filename { get; set; }
            public string ContentType { get; set; }
            public Stream Stream { get; set; }
        }
#else
        private readonly record struct FileToUpload(string Name, string Filename, string ContentType, Stream Stream);
#endif
    }
}