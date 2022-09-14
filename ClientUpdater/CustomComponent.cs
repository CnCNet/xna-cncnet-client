/*
Copyright 2022 CnCNet

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rampastring.Tools;
using ClientUpdater.Compression;

namespace ClientUpdater
{
    /// <summary>
    /// Custom component.
    /// </summary>
    public class CustomComponent
    {
        #region public_properties

        /// <summary>
        /// UI name of custom component.
        /// </summary>
        public string GUIName { get; internal set; }

        /// <summary>
        /// INI name of custom component.
        /// </summary>
        public string ININame { get; internal set; }

        /// <summary>
        /// Local file system path of custom component.
        /// </summary>
        public string LocalPath { get; internal set; }

        /// <summary>
        /// Download path of custom component.
        /// </summary>
        public string DownloadPath { get; internal set; }

        /// <summary>
        /// Is download path treated as an absolute URL?
        /// </summary>
        public bool IsDownloadPathAbsolute { get; internal set; }

        /// <summary>
        /// If set, no archive extension is used for download file path.
        /// </summary>
        public bool NoArchiveExtensionForDownloadPath { get; internal set; }

        /// <summary>
        /// Is this custom component currently being downloaded?
        /// </summary>
        public bool IsBeingDownloaded { get; internal set; }

        /// <summary>
        /// File identifier from local version file.
        /// </summary>
        public string LocalIdentifier { get; internal set; }

        /// <summary>
        /// File identifier from server version file.
        /// </summary>
        public string RemoteIdentifier { get; internal set; }

        /// <summary>
        /// File size from server version file.
        /// </summary>
        public long RemoteSize { get; internal set; }

        /// <summary>
        /// Archive file size from server version file.
        /// </summary>
        public long RemoteArchiveSize { get; internal set; }

        /// <summary>
        /// Is custom component an archived file?
        /// </summary>
        public bool Archived { get; internal set; }

        /// <summary>
        /// Has custom component been initialized?
        /// </summary>
        public bool Initialized { get; internal set; }

        #endregion

        #region private_fields

        private readonly List<string> filesToCleanup = new List<string>();

        private int currentDownloadPercentage;
        private Exception currentDownloadException;

        private CancellationTokenSource downloadTaskCancelTokenSource;
        private CancellationToken downloadTaskCancelToken;

        #endregion

        /// <summary>
        /// Creates new custom component.
        /// </summary>
        public CustomComponent()
        {
        }

        /// <summary>
        /// Creates new custom component from given information.
        /// </summary>
        public CustomComponent(string guiName, string iniName, string downloadPath, string localPath, bool isDownloadPathAbsolute = false, bool noArchiveExtensionForDownloadPath = false)
        {
            GUIName = guiName;
            ININame = iniName;
            LocalPath = localPath;
            DownloadPath = downloadPath;
            IsDownloadPathAbsolute = isDownloadPathAbsolute;
            NoArchiveExtensionForDownloadPath = noArchiveExtensionForDownloadPath;
        }

        #region public_methods

        /// <summary>
        /// Starts download for this custom component.
        /// </summary>
        public void DownloadComponent()
        {
            if (downloadTaskCancelTokenSource == null)
                downloadTaskCancelTokenSource = new CancellationTokenSource();

            downloadTaskCancelToken = downloadTaskCancelTokenSource.Token;
            Task downloadTask = Task.Factory.StartNew(() => DoDownloadComponent(), downloadTaskCancelToken);
        }

        /// <summary>
        /// Stops downloading of this custom component.
        /// </summary>
        public void StopDownload()
        {
            if (downloadTaskCancelTokenSource != null && !downloadTaskCancelTokenSource.IsCancellationRequested)
                downloadTaskCancelTokenSource.Cancel();
        }

        #endregion

        #region private_methods

        /// <summary>
        /// Handles downloading of the custom component.
        /// </summary>
        private void DoDownloadComponent()
        {
            try
            {
                Logger.Log("CustomComponent: Initializing download of custom component: " + GUIName);
                currentDownloadException = null;
                IsBeingDownloaded = true;
                currentDownloadPercentage = -1;
                string uniqueIdForFile = "";
                string uriString = Updater.CurrentUpdateServerURL + Updater.VERSION_FILE;
                string finalFileName = SafePath.CombineFilePath(Updater.GamePath, LocalPath);
                string finalFileNameTemp = FormattableString.Invariant($"{finalFileName}_u");
                string versionFileName = SafePath.CombineFilePath(Updater.GamePath, FormattableString.Invariant($"{Updater.VERSION_FILE}_cc"));
                UpdaterFileInfo info = null;

                Updater.CreatePath(finalFileName);

                WebClient client = new WebClient
                {
                    CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore)
                };

                client.Headers.Add(HttpRequestHeader.UserAgent, Updater.GetUserAgentString());

                Logger.Log("CustomComponent: Downloading version info.");
                client.DownloadFile(new Uri(uriString), versionFileName);
                client.Dispose();
                IniFile version = new IniFile(versionFileName);
                string[] tmp = version.GetStringValue("AddOns", ININame, "").Split(',');
                Updater.GetArchiveInfo(version, LocalPath, out string archiveID, out int archiveSize);
                info = Updater.CreateFileInfo(finalFileName, tmp[0], Conversions.IntFromString(tmp[1], 0), archiveID, archiveSize);

                Logger.Log("CustomComponent: Version info parsed. Proceeding to download component.");
                int num = 0;
                Uri downloadUri = GetDownloadUri(DownloadPath, info);
                string downloadFileName = FormattableString.Invariant($"{GetArchivePath(finalFileName, info)}_u");
                Logger.Log("CustomComponent: Download URL for custom component " + GUIName + ": " + downloadUri.AbsoluteUri);
                CheckDownloadCancelStatus();

                while (true)
                {
                    filesToCleanup.Clear();
                    filesToCleanup.Add(versionFileName);
                    filesToCleanup.Add(downloadFileName);

                    WebClient clientFile = new WebClient
                    {
                        CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore)
                    };

                    clientFile.Headers.Add(HttpRequestHeader.UserAgent, Updater.GetUserAgentString());
                    clientFile.DownloadProgressChanged += DownloadClient_DownloadProgressChanged;
                    clientFile.DownloadFileCompleted += DownloadClient_DownloadFileCompleted;
                    num++;

                    CheckDownloadCancelStatus();
                    clientFile.DownloadFileAsync(downloadUri, downloadFileName);

                    while (clientFile.IsBusy)
                    {
                        Thread.Sleep(50);
                        if (downloadTaskCancelToken != null && downloadTaskCancelToken.IsCancellationRequested)
                        {
                            clientFile.CancelAsync();
                            clientFile.Dispose();
                            downloadTaskCancelToken.ThrowIfCancellationRequested();
                        }
                    }

                    clientFile.Dispose();

                    if (currentDownloadException != null)
                        throw currentDownloadException;

                    Logger.Log("CustomComponent: Download of custom component " + GUIName + " finished - verifying.");
                    CheckDownloadCancelStatus();

                    if (info.Archived)
                    {
                        filesToCleanup.Add(finalFileNameTemp);
                        string archiveLocalPath = GetArchivePath(LocalPath, info);
                        string archiveLocalPathTemp = FormattableString.Invariant($"{archiveLocalPath}_u");
                        FileInfo archivePathFileInfo = SafePath.GetFile(Updater.GamePath, archiveLocalPathTemp);
                        Logger.Log("CustomComponent: Custom component is an archive.");
                        string archiveIdentifier = Updater.GetUniqueIdForFile(archiveLocalPathTemp);

                        if (archiveIdentifier != info.ArchiveIdentifier)
                        {
                            CheckDownloadCancelStatus();

                            if (num > 2)
                                throw new Exception("Too many retries for downloading component.");

                            Logger.Log("CustomComponent: Downloaded archive " + archiveLocalPath + "_u has a non-matching identifier: " + archiveIdentifier + " against " + info.ArchiveIdentifier + ". Retrying.");
                            Updater.DeleteFileAndWait(archivePathFileInfo.FullName);
                            continue;
                        }
                        else
                        {
                            CheckDownloadCancelStatus();
                            Logger.Log("CustomComponent: Archive " + archiveLocalPath + "_u is intact. Unpacking...");
                            CompressionHelper.DecompressFile(archivePathFileInfo.FullName, finalFileNameTemp, downloadTaskCancelToken);
                            archivePathFileInfo.Delete();
                        }
                    }

                    CheckDownloadCancelStatus();
                    uniqueIdForFile = Updater.GetUniqueIdForFile(FormattableString.Invariant($"{LocalPath}_u"));
                    if (info.Identifier != uniqueIdForFile)
                    {
                        if (num > 2)
                            throw new Exception("Too many retries for downloading component.");

                        CheckDownloadCancelStatus();
                        Logger.Log("CustomComponent: Incorrect custom component identifier for " + GUIName + ": " + uniqueIdForFile + " against " + info.Identifier + ". Retrying.");
                        continue;
                    }

                    break;
                }

                CheckDownloadCancelStatus();
                Logger.Log("Downloaded custom component " + GUIName + " verified successfully.");
                File.Copy(finalFileNameTemp, finalFileName, true);
                LocalIdentifier = uniqueIdForFile;
                IsBeingDownloaded = false;
                CleanUpAfterDownload();
                DoDownloadFinished(true);
            }
            catch (Exception e)
            {
                if (e is AggregateException)
                {
                    bool canceled = false;
                    bool displayError = false;
                    foreach (Exception ei in (e as AggregateException).InnerExceptions)
                    {
                        if (ei is TaskCanceledException || ei is OperationCanceledException)
                            canceled = true;
                        else
                        {
                            if (!displayError)
                            {
                                Logger.Log("CustomComponent: One or more errors occurred while downloading custom component " + GUIName + ". The download has been aborted.");
                                displayError = true;
                            }

                            Logger.Log("Message: " + ei.Message);
                        }

                        if (canceled)
                            HandleAfterCancelDownload();
                        else
                        {
                            IsBeingDownloaded = false;
                            CleanUpAfterDownload();
                            DoDownloadFinished(false);
                        }
                    }

                    return;
                }

                if (e is TaskCanceledException || e is OperationCanceledException)
                {
                    HandleAfterCancelDownload();
                    return;
                }

                Logger.Log("CustomComponent: An error occurred while downloading custom component " + GUIName + ". The download has been aborted. Message: " + e.Message);
                IsBeingDownloaded = false;
                CleanUpAfterDownload();
                DoDownloadFinished(false);
            }
            finally
            {
                downloadTaskCancelTokenSource.Dispose();
                downloadTaskCancelTokenSource = null;
            }
        }

        private void CheckDownloadCancelStatus()
        {
            if (downloadTaskCancelToken != null)
                downloadTaskCancelToken.ThrowIfCancellationRequested();
        }

        private void HandleAfterCancelDownload()
        {
            Logger.Log("CustomComponent: Download of custom component " + GUIName + " canceled.");
            IsBeingDownloaded = false;
            DoDownloadFinished(false);
            CleanUpAfterDownload();
        }

        private Uri GetDownloadUri(string downloadPath, UpdaterFileInfo info)
        {
            string fullPath;

            if (!IsDownloadPathAbsolute)
                fullPath = Updater.CurrentUpdateServerURL + downloadPath;
            else
                fullPath = downloadPath;

            return new Uri(NoArchiveExtensionForDownloadPath ? fullPath : GetArchivePath(fullPath, info));
        }

        private string GetArchivePath(string path, UpdaterFileInfo info)
        {
            if (info.Archived)
                return path + Updater.ARCHIVE_FILE_EXTENSION;
            else
                return path;
        }

        private void CleanUpAfterDownload()
        {
            try
            {
                foreach (string filename in filesToCleanup)
                {
                    if (File.Exists(filename))
                        File.Delete(filename);
                }
            }
            catch (Exception)
            {
            }
        }

        #endregion

        #region events_and_delegates

        public event DownloadFinishedEventHandler DownloadFinished;
        public event DownloadProgressChangedEventHandler DownloadProgressChanged;

        public delegate void DownloadFinishedEventHandler(CustomComponent cc, bool success);
        public delegate void DownloadProgressChangedEventHandler(CustomComponent cc, int percentage);

        private void DoDownloadFinished(bool success) => DownloadFinished?.Invoke(this, success);

        private void DownloadClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage != currentDownloadPercentage)
            {
                currentDownloadPercentage = e.ProgressPercentage;
                DownloadProgressChanged?.Invoke(this, currentDownloadPercentage);
            }
        }

        private void DownloadClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
                CleanUpAfterDownload();

            currentDownloadException = e.Error;
        }

        #endregion
    }
}

