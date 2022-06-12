using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Threading;
using Rampastring.Tools;
using ClientCore;

namespace Updater
{
    public class CustomComponent
    {
        public delegate void DownloadFinishedEventHandler(CustomComponent cc, bool success);

        public delegate void DownloadProgressChangedEventHandler(CustomComponent cc, int percentage);

        private bool downloadStopped;

        private int percentage;

        private string aName;

        private string aShortName;

        private string aDownloadPath;

        private string aLocalPath;

        private long aRemoteSizeInBytes;

        private bool aDontUpdateToSmaller;

        public bool IsBeingDownloaded { get; set; }

        public string GUIName => aName;

        public string ININame => aShortName;

        public string DownloadPath => aDownloadPath;

        public string LocalPath => aLocalPath;

        public bool DoNotUpdateToSmaller => aDontUpdateToSmaller;

        public long RemoteSize
        {
            get
            {
                return aRemoteSizeInBytes;
            }
            set
            {
                aRemoteSizeInBytes = value;
            }
        }

        public string RemoteIdentifier { get; set; }

        public string LocalIdentifier { get; set; }

        public event DownloadFinishedEventHandler DownloadFinished;

        public event DownloadProgressChangedEventHandler DownloadProgressChanged;

        public static int getComponentId(string componentName)
        {
            for (int i = 0; i < CUpdater.CustomComponents.Length; i++)
            {
                if (componentName == CUpdater.CustomComponents[i].ININame)
                {
                    return i;
                }
            }
            return -1;
        }

        public static bool IsDownloadInProgress()
        {
            if (CUpdater.CustomComponents == null)
            {
                return false;
            }
            for (int i = 0; i < CUpdater.CustomComponents.Length; i++)
            {
                if (CUpdater.CustomComponents[i].IsBeingDownloaded)
                {
                    return true;
                }
            }
            return false;
        }

        private void ExecDownloadFinished(bool success)
        {
            this.DownloadFinished?.Invoke(this, success);
        }

        public void StopDownload()
        {
            downloadStopped = true;
        }

        public void DownloadComponent()
        {
            Logger.Log("Initializing download of custom component: " + GUIName);
            percentage = -1;
            try
            {
                CreatePath(ProgramConstants.GamePath + LocalPath);
                string text = ProgramConstants.GamePath + LocalPath;
                string uriString = CUpdater.GetUpdateServerUrl() + CUpdater.VERSION_FILE;
                string text2 = ProgramConstants.GamePath + CUpdater.VERSION_FILE + "_cc";
                WebClient webClient = new WebClient();
                Logger.Log("Downloading version info.");
                webClient.DownloadFile(new Uri(uriString), text2);
                INIReader iNIReader = new INIReader();
                iNIReader.InitINIReader(text2);
                DTAFileInfo dTAFileInfo = new DTAFileInfo();
                while (!iNIReader.ReaderClosed)
                {
                    iNIReader.ReadNextLine();
                    if (iNIReader.isLineReadable() && iNIReader.CurrentSection == "AddOns" && iNIReader.getCurrentKeyName() == ININame)
                    {
                        string value = iNIReader.GetValue3();
                        dTAFileInfo = CUpdater.ParseFileInfo(ProgramConstants.GamePath + LocalPath, value);
                        iNIReader.CloseINIReader();
                    }
                }
                webClient.Dispose();
                Logger.Log("Version info parsed. Proceeding to download component.");
                int num = 0;
                IsBeingDownloaded = true;
                string uniqueIdForFile;
                while (true)
                {
                    WebClient webClient2 = new WebClient();
                    webClient2.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
                    webClient2.DownloadProgressChanged += downloadClient_DownloadProgressChanged;
                    num++;
                    webClient2.DownloadFileAsync(new Uri(CUpdater.GetUpdateServerUrl() + DownloadPath), text + "_u");
                    while (webClient2.IsBusy)
                    {
                        Thread.Sleep(50);
                        if (downloadStopped)
                        {
                            webClient2.CancelAsync();
                            webClient2.Dispose();
                            downloadStopped = false;
                            IsBeingDownloaded = false;
                        }
                    }
                    webClient2.Dispose();
                    Logger.Log($"Download of custom component \"{GUIName}\" finished.");
                    uniqueIdForFile = CUpdater.GetUniqueIdForFile(LocalPath + "_u");
                    if (!(dTAFileInfo.Identifier != uniqueIdForFile))
                    {
                        break;
                    }
                    Logger.Log($"Incorrect custom component identifier for {GUIName}: {uniqueIdForFile} against {dTAFileInfo.Identifier}. Retrying.");
                    if (num > 2)
                    {
                        throw new Exception("Too many retries for downloading component.");
                    }
                }
                Logger.Log("Downloaded custom component " + GUIName + " verified succesfully.");
                File.Copy(text + "_u", text, overwrite: true);
                try
                {
                    File.Delete(text + "_u");
                }
                catch
                {
                }
                LocalIdentifier = uniqueIdForFile;
                ExecDownloadFinished(success: true);
                IsBeingDownloaded = false;
            }
            catch (Exception ex)
            {
                Logger.Log($"An error occured while downloading custom component \"{GUIName}\". The download has been aborted.");
                Logger.Log("Message: " + ex.Message);
                IsBeingDownloaded = false;
                ExecDownloadFinished(success: false);
            }
        }

        private void downloadClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage != percentage)
            {
                percentage = e.ProgressPercentage;
                this.DownloadProgressChanged?.Invoke(this, e.ProgressPercentage);
            }
        }

        private void CreatePath(string filePath)
        {
            int num = filePath.LastIndexOf("\\");
            if (num != -1)
            {
                string path = filePath.Substring(0, num);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
        }

        public CustomComponent()
        {
        }

        public CustomComponent(string guiName, string iniName, string downloadPath, string localPath, bool DoNotUpdateToSmaller)
        {
            aName = guiName;
            aShortName = iniName;
            aDownloadPath = downloadPath;
            aLocalPath = localPath;
            aDontUpdateToSmaller = DoNotUpdateToSmaller;
        }
    }
}