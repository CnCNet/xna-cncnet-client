// Copyright 2022-2025 CnCNet
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.

namespace ClientUpdater;

using ClientCore.Extensions;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Threading;
using System.Threading.Tasks;

using ClientUpdater.Compression;

using Rampastring.Tools;

/// <summary>
/// Custom component.
/// </summary>
public class CustomComponent
{
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

    private readonly List<string> filesToCleanup = new();

    private int currentDownloadPercentage;

    private CancellationTokenSource downloadTaskCancelTokenSource;
    private CancellationToken downloadTaskCancelToken;

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
        GUIName = guiName.L10N($"INI:CustomComponents:{iniName}:UIName");
        ININame = iniName;
        LocalPath = localPath;
        DownloadPath = downloadPath;
        IsDownloadPathAbsolute = isDownloadPathAbsolute;
        NoArchiveExtensionForDownloadPath = noArchiveExtensionForDownloadPath;
    }

    /// <summary>
    /// Starts download for this custom component.
    /// </summary>
    public void DownloadComponent()
    {
        downloadTaskCancelTokenSource ??= new();

        downloadTaskCancelToken = downloadTaskCancelTokenSource.Token;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        DoDownloadComponentAsync(downloadTaskCancelToken);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }

    /// <summary>
    /// Stops downloading of this custom component.
    /// </summary>
    public void StopDownload()
    {
        if (downloadTaskCancelTokenSource is { IsCancellationRequested: false })
            downloadTaskCancelTokenSource.Cancel();
    }

    /// <summary>
    /// Handles downloading of the custom component.
    /// </summary>
    private async Task DoDownloadComponentAsync(CancellationToken cancellationToken)
    {
        ProgressMessageHandler progressMessageHandler = null;

        try
        {
            Logger.Log("CustomComponent: Initializing download of custom component: " + GUIName);

#if NETFRAMEWORK
            progressMessageHandler = new(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls |
                    System.Security.Authentication.SslProtocols.Tls11 |
                    System.Security.Authentication.SslProtocols.Tls12 |
                    System.Security.Authentication.SslProtocols.Tls13,
            });

            using var httpClient = new HttpClient(progressMessageHandler, true);
#else
            progressMessageHandler = new(new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                AutomaticDecompression = DecompressionMethods.All
            });

            using var httpClient = new HttpClient(progressMessageHandler, true)
            {
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
            };
#endif

            IsBeingDownloaded = true;
            currentDownloadPercentage = -1;
            string uniqueIdForFile;
            string uriString = Updater.CurrentUpdateServerURL + Updater.VERSION_FILE;
            string finalFileName = SafePath.CombineFilePath(Updater.GamePath, LocalPath);
            string finalFileNameTemp = FormattableString.Invariant($"{finalFileName}_u");
            string versionFileName = SafePath.CombineFilePath(Updater.GamePath, FormattableString.Invariant($"{Updater.VERSION_FILE}_cc"));
            Updater.CreatePath(finalFileName);
            Updater.UpdateUserAgent(httpClient);

            progressMessageHandler.HttpReceiveProgress += ProgressMessageHandlerOnHttpReceiveProgress;

            Logger.Log("CustomComponent: Downloading version info.");

            var versionFileStream = new FileStream(versionFileName, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);

            using (versionFileStream)
            {
                Stream stream = await httpClient.GetStreamAsync(new Uri(uriString)).ConfigureAwait(false);

                using (stream)
                {
                    await stream.CopyToAsync(versionFileStream, 81920, cancellationToken).ConfigureAwait(false);
                }
            }

            var version = new IniFile(versionFileName);
            string[] tmp = version.GetStringListValue("AddOns", ININame, string.Empty);
            Updater.GetArchiveInfo(version, LocalPath, out string archiveID, out int archiveSize);
            UpdaterFileInfo info = Updater.CreateFileInfo(finalFileName, tmp[0], Conversions.IntFromString(tmp[1], 0), archiveID, archiveSize);

            Logger.Log("CustomComponent: Version info parsed. Proceeding to download component.");
            int num = 0;
            Uri downloadUri = GetDownloadUri(DownloadPath, info);
            string downloadFileName = FormattableString.Invariant($"{GetArchivePath(finalFileName, info)}_u");
            Logger.Log("CustomComponent: Download URL for custom component " + GUIName + ": " + downloadUri.AbsoluteUri);

            while (true)
            {
                filesToCleanup.Clear();
                filesToCleanup.Add(versionFileName);
                filesToCleanup.Add(downloadFileName);

                num++;

                var downloadFileStream = new FileStream(downloadFileName, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
                using (downloadFileStream)
                {
                    Stream stream = await httpClient.GetStreamAsync(downloadUri).ConfigureAwait(false);

                    using (stream)
                    {
                        await stream.CopyToAsync(downloadFileStream, 81920, cancellationToken).ConfigureAwait(false);
                    }
                }

                Logger.Log("CustomComponent: Download of custom component " + GUIName + " finished - verifying.");

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
                        cancellationToken.ThrowIfCancellationRequested();

                        if (num > 2)
                            throw new("Too many retries for downloading component.");

                        Logger.Log("CustomComponent: Downloaded archive " + archiveLocalPath + "_u has a non-matching identifier: " + archiveIdentifier + " against " + info.ArchiveIdentifier + ". Retrying.");
                        Updater.DeleteFileAndWait(archivePathFileInfo.FullName);
                        continue;
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    Logger.Log("CustomComponent: Archive " + archiveLocalPath + "_u is intact. Unpacking...");
                    await CompressionHelper.DecompressFileAsync(archivePathFileInfo.FullName, finalFileNameTemp, downloadTaskCancelToken).ConfigureAwait(false);
                    archivePathFileInfo.Delete();
                }

                cancellationToken.ThrowIfCancellationRequested();
                uniqueIdForFile = Updater.GetUniqueIdForFile(FormattableString.Invariant($"{LocalPath}_u"));
                if (info.Identifier != uniqueIdForFile)
                {
                    if (num > 2)
                        throw new("Too many retries for downloading component.");

                    cancellationToken.ThrowIfCancellationRequested();
                    Logger.Log("CustomComponent: Incorrect custom component identifier for " + GUIName + ": " + uniqueIdForFile + " against " + info.Identifier + ". Retrying.");
                    continue;
                }

                break;
            }

            cancellationToken.ThrowIfCancellationRequested();
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
                    if (ei is TaskCanceledException or OperationCanceledException)
                    {
                        canceled = true;
                    }
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
                    {
                        HandleAfterCancelDownload();
                    }
                    else
                    {
                        IsBeingDownloaded = false;
                        CleanUpAfterDownload();
                        DoDownloadFinished(false);
                    }
                }

                return;
            }

            if (e is TaskCanceledException or OperationCanceledException)
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
            progressMessageHandler.HttpReceiveProgress -= ProgressMessageHandlerOnHttpReceiveProgress;
        }
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

        return new(NoArchiveExtensionForDownloadPath ? fullPath : GetArchivePath(fullPath, info));
    }

    private static string GetArchivePath(string path, UpdaterFileInfo info)
    {
        if (info.Archived)
            return path + Updater.ARCHIVE_FILE_EXTENSION;

        return path;
    }

    private void CleanUpAfterDownload()
    {
        try
        {
            foreach (string filename in filesToCleanup)
            {
                if (File.Exists(filename))
                {
                    new FileInfo(filename).IsReadOnly = false;
                    File.Delete(filename);
                }
            }
        }
        catch (Exception)
        {
        }
    }

    public event DownloadFinishedEventHandler DownloadFinished;

    public event DownloadProgressChangedEventHandler DownloadProgressChanged;

    public delegate void DownloadFinishedEventHandler(CustomComponent cc, bool success);

    public delegate void DownloadProgressChangedEventHandler(CustomComponent cc, int percentage);

    private void DoDownloadFinished(bool success) => DownloadFinished?.Invoke(this, success);

    private void ProgressMessageHandlerOnHttpReceiveProgress(object sender, HttpProgressEventArgs e)
    {
        if (e.ProgressPercentage != currentDownloadPercentage)
        {
            currentDownloadPercentage = e.ProgressPercentage;
            DownloadProgressChanged?.Invoke(this, currentDownloadPercentage);
        }
    }
}