using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClientUpdater.Compression;
using Rampastring.Tools;

namespace ClientUpdater
{
    public class CustomComponent
    {
        public delegate void DownloadFinishedEventHandler(CustomComponent cc, bool success);

        public delegate void DownloadProgressChangedEventHandler(CustomComponent cc, int percentage);

        private readonly List<string> filesToCleanup = new List<string>();

        private int currentDownloadPercentage;

        private Exception currentDownloadException;

        private CancellationTokenSource downloadTaskCancelTokenSource;

        private CancellationToken downloadTaskCancelToken;

        public string GUIName { get; internal set; }

        public string ININame { get; internal set; }

        public string LocalPath { get; internal set; }

        public string DownloadPath { get; internal set; }

        public bool IsDownloadPathAbsolute { get; internal set; }

        public bool NoArchiveExtensionForDownloadPath { get; internal set; }

        public bool IsBeingDownloaded { get; internal set; }

        public string LocalIdentifier { get; internal set; }

        public string RemoteIdentifier { get; internal set; }

        public long RemoteSize { get; internal set; }

        public long RemoteArchiveSize { get; internal set; }

        public bool Archived { get; internal set; }

        public bool Initialized { get; internal set; }

        public event DownloadFinishedEventHandler DownloadFinished;

        public event DownloadProgressChangedEventHandler DownloadProgressChanged;

        public CustomComponent()
        {
        }

        public CustomComponent(string guiName, string iniName, string downloadPath, string localPath, bool isDownloadPathAbsolute = false, bool noArchiveExtensionForDownloadPath = false)
        {
            GUIName = guiName;
            ININame = iniName;
            LocalPath = localPath.Replace('\\', '/');
            DownloadPath = downloadPath;
            IsDownloadPathAbsolute = isDownloadPathAbsolute;
            NoArchiveExtensionForDownloadPath = noArchiveExtensionForDownloadPath;
        }

        public void DownloadComponent()
        {
            if (downloadTaskCancelTokenSource == null)
            {
                downloadTaskCancelTokenSource = new CancellationTokenSource();
            }
            downloadTaskCancelToken = downloadTaskCancelTokenSource.Token;
            Task task = Task.Factory.StartNew(delegate
            {
                DoDownloadComponent();
            }, downloadTaskCancelToken);
        }

        public void StopDownload()
        {
            if (downloadTaskCancelTokenSource != null && !downloadTaskCancelTokenSource.IsCancellationRequested)
            {
                downloadTaskCancelTokenSource.Cancel();
            }
        }

        private void DoDownloadComponent()
        {
            try
            {
                Logger.Log("CustomComponent: Initializing download of custom component: " + GUIName);
                currentDownloadException = null;
                IsBeingDownloaded = true;
                currentDownloadPercentage = -1;
                string text = "";
                string uriString = Updater.CurrentUpdateServerURL + Updater.VERSION_FILE;
                string text2 = Updater.GamePath + LocalPath;
                string text3 = Updater.GamePath + Updater.VERSION_FILE + "_cc";
                UpdaterFileInfo updaterFileInfo = null;
                Updater.CreatePath(Updater.GamePath + LocalPath);
                WebClient webClient = new WebClient
                {
                    CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore),
                    Encoding = Encoding.GetEncoding("Windows-1252")
                };
                webClient.Headers.Add(HttpRequestHeader.UserAgent, Updater.GetUserAgentString());
                Logger.Log("CustomComponent: Downloading version info.");
                webClient.DownloadFile(new Uri(uriString), text3);
                webClient.Dispose();
                IniFile iniFile = new IniFile(text3);
                string[] array = iniFile.GetStringValue("AddOns", ININame, "").Split(',');
                Updater.GetArchiveInfo(iniFile, LocalPath, out var archiveID, out var archiveSize);
                updaterFileInfo = Updater.CreateFileInfo(Updater.GamePath + LocalPath, array[0], Conversions.IntFromString(array[1], 0), archiveID, archiveSize);
                Logger.Log("CustomComponent: Version info parsed. Proceeding to download component.");
                int num = 0;
                Uri downloadUri = GetDownloadUri(DownloadPath, updaterFileInfo);
                string text4 = GetArchivePath(text2, updaterFileInfo) + "_u";
                Logger.Log("CustomComponent: Download URL for custom component " + GUIName + ": " + downloadUri.AbsoluteUri);
                CheckDownloadCancelStatus();
                while (true)
                {
                    filesToCleanup.Clear();
                    filesToCleanup.Add(text3);
                    filesToCleanup.Add(text4);
                    WebClient webClient2 = new WebClient
                    {
                        CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore),
                        Encoding = Encoding.GetEncoding("Windows-1252")
                    };
                    webClient2.Headers.Add(HttpRequestHeader.UserAgent, Updater.GetUserAgentString());
                    webClient2.DownloadProgressChanged += DownloadClient_DownloadProgressChanged;
                    webClient2.DownloadFileCompleted += DownloadClient_DownloadFileCompleted;
                    num++;
                    CheckDownloadCancelStatus();
                    webClient2.DownloadFileAsync(downloadUri, text4);
                    while (webClient2.IsBusy)
                    {
                        Thread.Sleep(50);
                        _ = downloadTaskCancelToken;
                        if (downloadTaskCancelToken.IsCancellationRequested)
                        {
                            webClient2.CancelAsync();
                            webClient2.Dispose();
                            downloadTaskCancelToken.ThrowIfCancellationRequested();
                        }
                    }
                    webClient2.Dispose();
                    if (currentDownloadException != null)
                    {
                        throw currentDownloadException;
                    }
                    Logger.Log("CustomComponent: Download of custom component " + GUIName + " finished - verifying.");
                    CheckDownloadCancelStatus();
                    if (updaterFileInfo.Archived)
                    {
                        filesToCleanup.Add(text2 + "_u");
                        string archivePath = GetArchivePath(LocalPath, updaterFileInfo);
                        string text5 = Updater.GamePath + archivePath + "_u";
                        Logger.Log("CustomComponent: Custom component is an archive.");
                        string uniqueIdForFile = Updater.GetUniqueIdForFile(archivePath + "_u");
                        if (uniqueIdForFile != updaterFileInfo.ArchiveIdentifier)
                        {
                            CheckDownloadCancelStatus();
                            if (num > 2)
                            {
                                throw new Exception("Too many retries for downloading component.");
                            }
                            Logger.Log("CustomComponent: Downloaded archive " + archivePath + "_u has a non-matching identifier: " + uniqueIdForFile + " against " + updaterFileInfo.ArchiveIdentifier + ". Retrying.");
                            Updater.DeleteFileAndWait(text5);
                            continue;
                        }
                        CheckDownloadCancelStatus();
                        Logger.Log("CustomComponent: Archive " + archivePath + "_u is intact. Unpacking...");
                        CompressionHelper.DecompressFile(text5, text2 + "_u", downloadTaskCancelToken);
                        File.Delete(text5);
                    }
                    CheckDownloadCancelStatus();
                    text = Updater.GetUniqueIdForFile(LocalPath + "_u");
                    if (!(updaterFileInfo.Identifier != text))
                    {
                        break;
                    }
                    if (num > 2)
                    {
                        throw new Exception("Too many retries for downloading component.");
                    }
                    CheckDownloadCancelStatus();
                    Logger.Log("CustomComponent: Incorrect custom component identifier for " + GUIName + ": " + text + " against " + updaterFileInfo.Identifier + ". Retrying.");
                }
                CheckDownloadCancelStatus();
                Logger.Log("Downloaded custom component " + GUIName + " verified succesfully.");
                File.Copy(text2 + "_u", text2, overwrite: true);
                LocalIdentifier = text;
                IsBeingDownloaded = false;
                CleanUpAfterDownload();
                DoDownloadFinished(success: true);
            }
            catch (Exception ex)
            {
                if (ex is AggregateException)
                {
                    bool flag = false;
                    bool flag2 = false;
                    {
                        foreach (Exception innerException in (ex as AggregateException).InnerExceptions)
                        {
                            if (innerException is TaskCanceledException || innerException is OperationCanceledException)
                            {
                                flag = true;
                            }
                            else
                            {
                                if (!flag2)
                                {
                                    Logger.Log("CustomComponent: One or more errors occured while downloading custom component " + GUIName + ". The download has been aborted.");
                                    flag2 = true;
                                }
                                Logger.Log("Message: " + innerException.Message);
                            }
                            if (flag)
                            {
                                HandleAfterCancelDownload();
                                continue;
                            }
                            IsBeingDownloaded = false;
                            CleanUpAfterDownload();
                            DoDownloadFinished(success: false);
                        }
                        return;
                    }
                }
                if (ex is TaskCanceledException || ex is OperationCanceledException)
                {
                    HandleAfterCancelDownload();
                    return;
                }
                Logger.Log("CustomComponent: An error occured while downloading custom component " + GUIName + ". The download has been aborted. Message: " + ex.Message);
                IsBeingDownloaded = false;
                CleanUpAfterDownload();
                DoDownloadFinished(success: false);
            }
            finally
            {
                downloadTaskCancelTokenSource.Dispose();
                downloadTaskCancelTokenSource = null;
            }
        }

        private void CheckDownloadCancelStatus()
        {
            _ = downloadTaskCancelToken;
            downloadTaskCancelToken.ThrowIfCancellationRequested();
        }

        private void HandleAfterCancelDownload()
        {
            Logger.Log("CustomComponent: Download of custom component " + GUIName + " canceled.");
            IsBeingDownloaded = false;
            DoDownloadFinished(success: false);
            CleanUpAfterDownload();
        }

        private Uri GetDownloadUri(string downloadPath, UpdaterFileInfo info)
        {
            string text = (IsDownloadPathAbsolute ? downloadPath : (Updater.CurrentUpdateServerURL + downloadPath));
            return new Uri(NoArchiveExtensionForDownloadPath ? text : GetArchivePath(text, info));
        }

        private string GetArchivePath(string path, UpdaterFileInfo info)
        {
            if (info.Archived)
            {
                return path + ".lzma";
            }
            return path;
        }

        private void CleanUpAfterDownload()
        {
            try
            {
                foreach (string item in filesToCleanup)
                {
                    if (File.Exists(item))
                    {
                        File.Delete(item);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void DoDownloadFinished(bool success)
        {
            this.DownloadFinished?.Invoke(this, success);
        }

        private void DownloadClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage != currentDownloadPercentage)
            {
                currentDownloadPercentage = e.ProgressPercentage;
                this.DownloadProgressChanged?.Invoke(this, currentDownloadPercentage);
            }
        }

        private void DownloadClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                CleanUpAfterDownload();
            }
            currentDownloadException = e.Error;
        }
    }
}