using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Collections.Specialized;
using System.Globalization;
using System.Threading;
using Rampastring.Tools;
using ClientCore;
using System.IO.Compression;
using System.Linq;

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

        private volatile static List<string> MapDownloadQueue = new List<string>();
        private volatile static List<Map> MapUploadQueue = new List<Map>();
        private volatile static List<string> UploadedMaps = new List<string>();

        private static readonly object locker = new object();

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
                {
                    ParameterizedThreadStart pts = new ParameterizedThreadStart(Upload);
                    Thread thread = new Thread(pts);
                    object[] mapAndGame = new object[2];
                    mapAndGame[0] = map;
                    mapAndGame[1] = myGame.ToLower();
                    thread.Start(mapAndGame);
                }
            }
        }

        private static void Upload(object mapAndGame)
        {
            object[] mapGameArray = (object[])mapAndGame;

            Map map = (Map)mapGameArray[0];
            string myGameId = (string)mapGameArray[1];

            MapUploadStarted?.Invoke(null, new MapEventArgs(map));

            Logger.Log("MapSharer: Starting upload of " + map.BaseFilePath);

            if (string.IsNullOrWhiteSpace(ClientConfiguration.Instance.CnCNetMapDBUploadURL))
            {
                Logger.Log("MapSharer: Upload URL is not configured.");
                MapUploadFailed?.Invoke(null, new MapEventArgs(map));
                return;
            }

            string message = MapUpload(ClientConfiguration.Instance.CnCNetMapDBUploadURL, map, myGameId, out bool success);

            if (success)
            {
                MapUploadComplete?.Invoke(null, new MapEventArgs(map));

                lock (locker)
                {
                    UploadedMaps.Add(map.SHA1);
                }

                Logger.Log("MapSharer: Uploading map " + map.BaseFilePath + " completed succesfully.");
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

                    object[] array = new object[2];
                    array[0] = nextMap;
                    array[1] = myGameId;

                    Logger.Log("MapSharer: There are additional maps in the queue.");

                    Upload(array);
                }
            }
        }

        private static string MapUpload(string _URL, Map map, string gameName, out bool success)
        {
            ServicePointManager.Expect100Continue = false;

            FileInfo zipFile = SafePath.GetFile(ProgramConstants.GamePath, "Maps", "Custom", FormattableString.Invariant($"{map.SHA1}.zip"));

            if (zipFile.Exists) zipFile.Delete();

            string mapFileName = map.SHA1 + MapLoader.MAP_FILE_EXTENSION;

            File.Copy(SafePath.CombineFilePath(map.CompleteFilePath), SafePath.CombineFilePath(ProgramConstants.GamePath, mapFileName));

            CreateZipFile(mapFileName, zipFile.FullName);

            try
            {
                SafePath.DeleteFileIfExists(ProgramConstants.GamePath, mapFileName);
            }
            catch { }

            // Upload the file to the URI. 
            // The 'UploadFile(uriString,fileName)' method implicitly uses HTTP POST method. 

            try
            {
                using (FileStream stream = zipFile.Open(FileMode.Open))
                {
                    List<FileToUpload> files = new List<FileToUpload>();
                    //{
                    //    new FileToUpload
                    //    {
                    //        Name = "file",
                    //        Filename = Path.GetFileName(zipFile),
                    //        ContentType = "mapZip",
                    //        Stream = stream
                    //    };
                    //};

                    FileToUpload file = new FileToUpload()
                    {
                        Name = "file",
                        Filename = zipFile.Name,
                        ContentType = "mapZip",
                        Stream = stream
                    };

                    files.Add(file);

                    NameValueCollection values = new NameValueCollection
                {
                    { "game", gameName.ToLower() },
                };

                    byte[] responseArray = UploadFiles(_URL, files, values);
                    string response = Encoding.UTF8.GetString(responseArray);

                    if (!response.Contains("Upload succeeded!"))
                    {
                        success = false;
                        return response;
                    }
                    Logger.Log("MapSharer: Upload response: " + response);

                    //MessageBox.Show((response));

                    success = true;
                    return String.Empty;
                }
            }
            catch (Exception ex)
            {
                success = false;
                return ex.Message;
            }
        }

        private static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[32768];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
            }
        }

        private static byte[] UploadFiles(string address, List<FileToUpload> files, NameValueCollection values)
        {
            WebRequest request = WebRequest.Create(address);
            request.Method = "POST";
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x", NumberFormatInfo.InvariantInfo);
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            boundary = "--" + boundary;

            using (Stream requestStream = request.GetRequestStream())
            {
                // Write the values
                foreach (string name in values.Keys)
                {
                    byte[] buffer = Encoding.ASCII.GetBytes(boundary + Environment.NewLine);
                    requestStream.Write(buffer, 0, buffer.Length);

                    buffer = Encoding.ASCII.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\"{1}{1}", name, Environment.NewLine));
                    requestStream.Write(buffer, 0, buffer.Length);

                    buffer = Encoding.UTF8.GetBytes(values[name] + Environment.NewLine);
                    requestStream.Write(buffer, 0, buffer.Length);
                }

                // Write the files
                foreach (FileToUpload file in files)
                {
                    var buffer = Encoding.ASCII.GetBytes(boundary + Environment.NewLine);
                    requestStream.Write(buffer, 0, buffer.Length);

                    buffer = Encoding.UTF8.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"{2}", file.Name, file.Filename, Environment.NewLine));
                    requestStream.Write(buffer, 0, buffer.Length);

                    buffer = Encoding.ASCII.GetBytes(string.Format("Content-Type: {0}{1}{1}", file.ContentType, Environment.NewLine));
                    requestStream.Write(buffer, 0, buffer.Length);

                    CopyStream(file.Stream, requestStream);

                    buffer = Encoding.ASCII.GetBytes(Environment.NewLine);
                    requestStream.Write(buffer, 0, buffer.Length);
                }

                byte[] boundaryBuffer = Encoding.ASCII.GetBytes(boundary + "--");
                requestStream.Write(boundaryBuffer, 0, boundaryBuffer.Length);
            }

            using (WebResponse response = request.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (MemoryStream stream = new MemoryStream())
                    {

                        CopyStream(responseStream, stream);

                        return stream.ToArray();
                    }
                }
            }
        }

        private static void CreateZipFile(string file, string zipName)
        {
            using var zipFileStream = new FileStream(zipName, FileMode.CreateNew, FileAccess.Write);
            using var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(SafePath.CombineFilePath(ProgramConstants.GamePath, file), file);
        }

        private static string ExtractZipFile(string zipFile, string destDir)
        {
            using ZipArchive zipArchive = ZipFile.OpenRead(zipFile);

            // here, we extract every entry, but we could extract conditionally
            // based on entry name, size, date, checkbox status, etc.  
            zipArchive.ExtractToDirectory(destDir);

            return zipArchive.Entries.FirstOrDefault()?.Name;
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
                {
                    object[] details = new object[3];
                    details[0] = sha1;
                    details[1] = myGame.ToLower();
                    details[2] = mapName;

                    ParameterizedThreadStart pts = new ParameterizedThreadStart(Download);
                    Thread thread = new Thread(pts);
                    thread.Start(details);
                }
            }
        }

        private static void Download(object details)
        {
            object[] sha1AndGame = (object[])details;
            string sha1 = (string)sha1AndGame[0];
            string myGameId = (string)sha1AndGame[1];
            string mapName = (string)sha1AndGame[2];

            Logger.Log("MapSharer: Preparing to download map " + sha1 + " with name: " + mapName);

            bool success;

            try
            {
                Logger.Log("MapSharer: MapDownloadStarted");
                MapDownloadStarted?.Invoke(null, new SHA1EventArgs(sha1, mapName));
            }
            catch (Exception ex)
            {
                Logger.Log("MapSharer: ERROR " + ex.ToString());
            }

            string mapPath = DownloadMain(sha1, myGameId, mapName, out success);

            lock (locker)
            {
                if (success)
                {
                    Logger.Log("MapSharer: Download of map " + sha1 + " completed succesfully.");
                    MapDownloadComplete?.Invoke(null, new SHA1EventArgs(sha1, mapName));
                }
                else
                {
                    Logger.Log("MapSharer: Download of map " + sha1 + "failed! Reason: " + mapPath);
                    MapDownloadFailed?.Invoke(null, new SHA1EventArgs(sha1, mapName));
                }

                MapDownloadQueue.Remove(sha1);

                if (MapDownloadQueue.Count > 0)
                {
                    Logger.Log("MapSharer: Continuing custom map downloads.");

                    object[] array = new object[3];
                    array[0] = MapDownloadQueue[0];
                    array[1] = myGameId;
                    array[2] = mapName;

                    Download(array);
                }
            }
        }

        public static string GetMapFileName(string sha1, string mapName)
            => mapName + "_" + sha1;

        private static string DownloadMain(string sha1, string myGame, string mapName, out bool success)
        {
            string customMapsDirectory = SafePath.CombineDirectoryPath(ProgramConstants.GamePath, "Maps", "Custom");

            string mapFileName = GetMapFileName(sha1, mapName);

            FileInfo destinationFile = SafePath.GetFile(customMapsDirectory, FormattableString.Invariant($"{mapFileName}.zip"));

            // This string is up here so we can check that there isn't already a .map file for this download.
            // This prevents the client from crashing when trying to rename the unzipped file to a duplicate filename.
            FileInfo newFile = SafePath.GetFile(customMapsDirectory, FormattableString.Invariant($"{mapFileName}{MapLoader.MAP_FILE_EXTENSION}"));

            destinationFile.Delete();
            newFile.Delete();

            using (TWebClient webClient = new TWebClient())
            {
                // TODO enable proxy support for some users
                webClient.Proxy = null;

                if (string.IsNullOrWhiteSpace(ClientConfiguration.Instance.CnCNetMapDBDownloadURL))
                {
                    success = false;
                    Logger.Log("MapSharer: Download URL is not configured.");
                    return null;
                }

                string url = string.Format(CultureInfo.InvariantCulture, "{0}/{1}/{2}.zip", ClientConfiguration.Instance.CnCNetMapDBDownloadURL, myGame, sha1);

                try
                {
                    Logger.Log($"MapSharer: Downloading URL: {url}");
                    webClient.DownloadFile(url, destinationFile.FullName);
                }
                catch (Exception ex)
                {
                    /*                    if (ex.Message.Contains("404"))
                                        {
                                            string messageToSend = "NOTICE " + ChannelName + " " + CTCPChar1 + CTCPChar2 + "READY 1" + CTCPChar2;
                                            CnCNetData.ConnectionBridge.SendMessage(messageToSend);
                                        }
                                        else
                                        {
                                            //GlobalVars.WriteLogfile(ex.StackTrace.ToString(), DateTime.Now.ToString("hh:mm:ss") + " DownloadMap: " + ex.Message + _DestFile);
                                            MessageBox.Show("Download failed:" + _DestFile);
                                        }*/
                    success = false;
                    return ex.Message;
                }
            }

            destinationFile.Refresh();

            if (!destinationFile.Exists)
            {
                success = false;
                return null;
            }

            string extractedFile = ExtractZipFile(destinationFile.FullName, customMapsDirectory);

            if (String.IsNullOrEmpty(extractedFile))
            {
                success = false;
                return null;
            }

            // We can safely assume that there will not be a duplicate file due to deleting it
            // earlier if one already existed.
            File.Move(SafePath.CombineFilePath(customMapsDirectory, extractedFile), newFile.FullName);

            destinationFile.Delete();

            success = true;
            return extractedFile;
        }

        class FileToUpload
        {
            public FileToUpload()
            {
                ContentType = "application/octet-stream";
            }

            public string Name { get; set; }
            public string Filename { get; set; }
            public string ContentType { get; set; }
            public Stream Stream { get; set; }
        }

        class TWebClient : WebClient
        {
            private int Timeout = 10000;

            public TWebClient()
            {
                // TODO enable proxy support for some users
                this.Proxy = null;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var webRequest = base.GetWebRequest(address);
                webRequest.Timeout = Timeout;
                return webRequest;
            }
        }
    }
}
