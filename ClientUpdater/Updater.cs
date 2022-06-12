using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ClientUpdater.Compression;
using Rampastring.Tools;

namespace ClientUpdater
{
    public static class Updater
    {
        public delegate void LocalFileCheckProgressChangedCallback(int checkedFileCount, int totalFileCount);

        public delegate void NoParamEventHandler();

        public delegate void SetExceptionCallback(Exception ex);

        public delegate void UpdateProgressChangedCallback(string currFileName, int currFilePercentage, int totalPercentage);

        public delegate void FileDownloadCompletedEventHandler(string archiveName);

        public static readonly string CURRENT_CLIENT_EXECUTABLE = Path.GetFileName(Application.ExecutablePath);

        public const string SECOND_STAGE_UPDATER = "SecondStageUpdater.exe";

        public static string VERSION_FILE = "version";

        public const string ARCHIVE_FILE_EXTENSION = ".lzma";

        private static VersionState _versionState = VersionState.UNKNOWN;

        private static int currentUpdateMirrorIndex;

        private static IniFile settingsINI;

        private static List<CustomComponent> customComponents;

        private static List<UpdateMirror> updateMirrors;

        private static string[] ignoreMasks = new string[4] { ".rtf", ".txt", "Theme.ini", "gui_settings.xml" };

        private static readonly List<UpdaterFileInfo> FileInfosToDownload = new List<UpdaterFileInfo>();

        private static readonly List<UpdaterFileInfo> ServerFileInfos = new List<UpdaterFileInfo>();

        public static readonly List<UpdaterFileInfo> LocalFileInfos = new List<UpdaterFileInfo>();

        private static bool terminateUpdate = false;

        private static string currentFilename;

        private static int currentFileSize;

        private static int totalDownloadedKbs;

        private static Exception currentDownloadException;

        public static string GamePath { get; private set; } = string.Empty;


        public static string ResourcePath { get; private set; } = string.Empty;


        public static string LocalGame { get; private set; } = "None";


        public static ReadOnlyCollection<CustomComponent> CustomComponents => customComponents?.AsReadOnly();

        public static ReadOnlyCollection<UpdateMirror> UpdateMirrors => updateMirrors?.AsReadOnly();

        public static string CurrentUpdateServerURL
        {
            get
            {
                if (updateMirrors == null || updateMirrors.Count <= 0)
                {
                    return null;
                }
                return updateMirrors[currentUpdateMirrorIndex].URL;
            }
        }

        public static VersionState VersionState
        {
            get
            {
                return _versionState;
            }
            private set
            {
                _versionState = value;
                DoOnVersionStateChanged();
            }
        }

        public static bool ManualUpdateRequired { get; private set; } = false;


        public static string ManualDownloadURL { get; private set; } = string.Empty;


        public static string UpdaterVersion { get; private set; } = "N/A";


        public static string GameVersion { get; private set; } = "N/A";


        public static string ServerGameVersion { get; private set; } = "N/A";


        public static int UpdateSizeInKb { get; private set; }

        public static event NoParamEventHandler FileIdentifiersUpdated;

        public static event LocalFileCheckProgressChangedCallback LocalFileCheckProgressChanged;

        public static event NoParamEventHandler OnCustomComponentsOutdated;

        public static event NoParamEventHandler OnLocalFileVersionsChecked;

        public static event NoParamEventHandler OnUpdateCompleted;

        public static event SetExceptionCallback OnUpdateFailed;

        public static event NoParamEventHandler OnVersionStateChanged;

        public static event FileDownloadCompletedEventHandler OnFileDownloadCompleted;

        public static event EventHandler Restart;

        public static event UpdateProgressChangedCallback UpdateProgressChanged;

        public static void Initialize(string gamePath, string resourcePath, string settingsIniName, string localGame)
        {
            Logger.Log("Updater: Initializing updater.");
            GamePath = gamePath;
            ResourcePath = resourcePath;
            settingsINI = new IniFile(GamePath + settingsIniName);
            LocalGame = localGame;
            ReadUpdaterConfig();
            Logger.Log("Updater: Update mirror count: " + updateMirrors.Count);
            Logger.Log("Updater: Running from: " + CURRENT_CLIENT_EXECUTABLE);
            List<UpdateMirror> list = new List<UpdateMirror>();
            List<string> sectionKeys = settingsINI.GetSectionKeys("DownloadMirrors");
            if (sectionKeys != null)
            {
                foreach (string item in sectionKeys)
                {
                    string value = settingsINI.GetStringValue("DownloadMirrors", item, string.Empty);
                    UpdateMirror updateMirror = updateMirrors.Find((UpdateMirror um) => um.Name == value);
                    if (updateMirror != null && !list.Contains(updateMirror))
                    {
                        list.Add(updateMirror);
                    }
                }
            }
            foreach (UpdateMirror updateMirror2 in updateMirrors)
            {
                if (!list.Contains(updateMirror2))
                {
                    list.Add(updateMirror2);
                }
            }
            updateMirrors = list;
        }

        public static void CheckForUpdates()
        {
            Logger.Log("Updater: Checking for updates.");
            if (VersionState != VersionState.UPDATECHECKINPROGRESS && VersionState != VersionState.UPDATEINPROGRESS)
            {
                new Thread(DoVersionCheck).Start();
            }
        }

        public static void CheckLocalFileVersions()
        {
            Logger.Log("Updater: Checking local file versions.");
            LocalFileInfos.Clear();
            IniFile iniFile = new IniFile(GamePath + VERSION_FILE);
            GameVersion = iniFile.GetStringValue("DTA", "Version", "N/A");
            UpdaterVersion = iniFile.GetStringValue("DTA", "UpdaterVersion", "N/A");
            List<string> sectionKeys = iniFile.GetSectionKeys("FileVersions");
            if (sectionKeys != null)
            {
                foreach (string item2 in sectionKeys)
                {
                    char[] separator = new char[1] { ',' };
                    string[] array = iniFile.GetStringValue("FileVersions", item2, string.Empty).Split(separator);
                    string[] array2 = iniFile.GetStringValue("ArchivedFiles", item2, string.Empty).Split(separator);
                    bool flag = array2 != null && array2.Length >= 2;
                    if (array.Length >= 2)
                    {
                        UpdaterFileInfo item = new UpdaterFileInfo
                        {
                            Filename = item2.Replace('\\', '/'),
                            Identifier = array[0],
                            Size = Conversions.IntFromString(array[1], 0),
                            ArchiveIdentifier = (flag ? array2[0] : string.Empty),
                            ArchiveSize = (flag ? Conversions.IntFromString(array2[1], 0) : 0)
                        };
                        LocalFileInfos.Add(item);
                    }
                    else
                    {
                        Logger.Log("Updater: Warning: Malformed file info in local version information: " + item2);
                    }
                }
            }
            Updater.OnLocalFileVersionsChecked?.Invoke();
        }

        public static void StartUpdate()
        {
            new Thread(PerformUpdate).Start();
        }

        public static void StopUpdate()
        {
            terminateUpdate = true;
        }

        public static void ClearVersionInfo()
        {
            LocalFileInfos.Clear();
            ServerFileInfos.Clear();
            FileInfosToDownload.Clear();
            GameVersion = "N/A";
            VersionState = VersionState.UNKNOWN;
        }

        public static bool IsFileNonexistantOrOriginal(string filePath)
        {
            UpdaterFileInfo updaterFileInfo = LocalFileInfos.Find((UpdaterFileInfo f) => f.Filename.ToLower() == filePath.ToLower());
            if (updaterFileInfo == null)
            {
                return true;
            }
            string uniqueIdForFile = GetUniqueIdForFile(updaterFileInfo.Filename);
            return updaterFileInfo.Identifier == uniqueIdForFile;
        }

        public static void MoveMirrorDown(int mirrorIndex)
        {
            if (mirrorIndex <= updateMirrors.Count - 2 && mirrorIndex >= 0)
            {
                UpdateMirror value = updateMirrors[mirrorIndex + 1];
                updateMirrors[mirrorIndex + 1] = updateMirrors[mirrorIndex];
                updateMirrors[mirrorIndex] = value;
            }
        }

        public static void MoveMirrorUp(int mirrorIndex)
        {
            if (updateMirrors.Count > mirrorIndex && mirrorIndex >= 1)
            {
                UpdateMirror value = updateMirrors[mirrorIndex - 1];
                updateMirrors[mirrorIndex - 1] = updateMirrors[mirrorIndex];
                updateMirrors[mirrorIndex] = value;
            }
        }

        public static bool IsComponentDownloadInProgress()
        {
            if (customComponents == null)
            {
                return false;
            }
            return customComponents.Any((CustomComponent c) => c.IsBeingDownloaded);
        }

        public static int GetComponentIndex(string componentName)
        {
            if (customComponents == null)
            {
                return -1;
            }
            return customComponents.FindIndex((CustomComponent c) => c.ININame == componentName);
        }

        internal static void GetArchiveInfo(IniFile versionFile, string filename, out string archiveID, out int archiveSize)
        {
            string[] array = versionFile.GetStringValue("ArchivedFiles", filename, "").Split(',');
            bool flag = array != null && array.Length >= 2;
            archiveID = (flag ? array[0] : "");
            archiveSize = (flag ? Conversions.IntFromString(array[1], 0) : 0);
        }

        internal static UpdaterFileInfo CreateFileInfo(string filename, string identifier, int size, string archiveIdentifier = null, int archiveSize = 0)
        {
            return new UpdaterFileInfo
            {
                Filename = filename.Replace('\\', '/'),
                Identifier = identifier,
                Size = size,
                ArchiveIdentifier = archiveIdentifier,
                ArchiveSize = archiveSize
            };
        }

        internal static string GetUserAgentString()
        {
            string text = "";
            if (UpdaterVersion != "N/A")
            {
                text = " Updater/" + UpdaterVersion;
            }
            return LocalGame + text + " Game/" + GameVersion + " Client/" + Assembly.GetEntryAssembly().GetName().Version;
        }

        internal static void DeleteFileAndWait(string filepath, int timeout = 10000)
        {
            using FileSystemWatcher fileSystemWatcher = new FileSystemWatcher(Path.GetDirectoryName(filepath), Path.GetFileName(filepath));
            ManualResetEventSlim mre = new ManualResetEventSlim();
            try
            {
                fileSystemWatcher.EnableRaisingEvents = true;
                fileSystemWatcher.Deleted += delegate
                {
                    mre.Set();
                };
                File.Delete(filepath);
                mre.Wait(timeout);
            }
            finally
            {
                if (mre != null)
                {
                    ((IDisposable)mre).Dispose();
                }
            }
        }

        internal static void CreatePath(string filePath)
        {
            string directoryName = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
        }

        internal static string GetUniqueIdForFile(string filePath)
        {
            MD5 mD = MD5.Create();
            mD.Initialize();
            FileStream fileStream = new FileStream(GamePath + filePath, FileMode.Open, FileAccess.Read);
            mD.ComputeHash(fileStream);
            StringBuilder stringBuilder = new StringBuilder();
            byte[] hash = mD.Hash;
            foreach (byte b in hash)
            {
                stringBuilder.Append(b.ToString());
            }
            fileStream.Close();
            mD.Clear();
            return stringBuilder.ToString();
        }

        private static void ReadUpdaterConfig()
        {
            List<UpdateMirror> list = new List<UpdateMirror>();
            List<CustomComponent> list2 = new List<CustomComponent>();
            string text = ResourcePath + "UpdaterConfig.ini";
            if (!File.Exists(text))
            {
                Logger.Log("Updater config file not found - attempting to read legacy updateconfig.ini.");
                ReadLegacyUpdaterConfig(list);
            }
            else
            {
                IniFile iniFile = new IniFile(text);
                ignoreMasks = iniFile.GetStringValue("Settings", "IgnoreMasks", string.Join(",", ignoreMasks)).Split(',');
                List<string> sectionKeys = iniFile.GetSectionKeys("DownloadMirrors");
                if (sectionKeys != null)
                {
                    foreach (string item in sectionKeys)
                    {
                        if (!string.IsNullOrEmpty(item))
                        {
                            string text2 = iniFile.GetStringValue("DownloadMirrors", item, string.Empty);
                            if (string.IsNullOrEmpty(text2))
                            {
                                text2 = item;
                            }
                            string[] values = text2.Split(',');
                            if (values.Length >= 2 && list.FindIndex((UpdateMirror i) => i.URL == values[0].Trim()) < 0)
                            {
                                list.Add(new UpdateMirror(values[0].Trim(), values[1].Trim(), (values.Length > 2) ? values[2].Trim() : ""));
                            }
                        }
                    }
                }
                sectionKeys = iniFile.GetSectionKeys("CustomComponents");
                if (sectionKeys != null)
                {
                    foreach (string item2 in sectionKeys)
                    {
                        if (string.IsNullOrEmpty(item2))
                        {
                            continue;
                        }
                        string text3 = iniFile.GetStringValue("CustomComponents", item2, string.Empty);
                        if (string.IsNullOrEmpty(text3))
                        {
                            text3 = item2;
                        }
                        string[] array = text3.Split(',');
                        if (array.Length < 4)
                        {
                            continue;
                        }
                        string ID = array[1].Trim();
                        if (list2.FindIndex((CustomComponent i) => i.ININame == ID) < 0)
                        {
                            string guiName = array[0].Trim();
                            string text4 = array[2].Trim();
                            string localPath = array[3].Trim();
                            bool noArchiveExtensionForDownloadPath = false;
                            if (array.Length > 4)
                            {
                                noArchiveExtensionForDownloadPath = Conversions.BooleanFromString(array[4], defaultValue: false);
                            }
                            bool isDownloadPathAbsolute = Uri.IsWellFormedUriString(text4, UriKind.Absolute);
                            list2.Add(new CustomComponent(guiName, ID, text4, localPath, isDownloadPathAbsolute, noArchiveExtensionForDownloadPath));
                        }
                    }
                }
            }
            updateMirrors = list;
            customComponents = list2;
            if (updateMirrors.Count < 1)
            {
                Logger.Log("Warning: No download mirrors found in updater config file or the built-in game info.");
            }
        }

        private static void ReadLegacyUpdaterConfig(List<UpdateMirror> updateMirrors)
        {
            if (!File.Exists(GamePath + "updateconfig.ini"))
            {
                return;
            }
            string[] array;
            try
            {
                array = File.ReadAllLines(GamePath + "updateconfig.ini");
            }
            catch (Exception ex)
            {
                Logger.Log("Error: Could not read legacy format updateconfig.ini. Message:" + ex.Message);
                return;
            }
            string[] array2 = array;
            foreach (string text in array2)
            {
                if (!string.IsNullOrWhiteSpace(text) && !text.Trim().StartsWith(";"))
                {
                    string[] array3 = text.Split(',');
                    if (array3.Length >= 3)
                    {
                        string url = array3[0].Trim();
                        string name = array3[1].Trim();
                        string location = array3[2].Trim();
                        updateMirrors.Add(new UpdateMirror(url, name, location));
                    }
                }
            }
        }

        private static void DoVersionCheck()
        {
            Logger.Log("Updater: Doing version file check.");
            ServerFileInfos.Clear();
            FileInfosToDownload.Clear();
            UpdateSizeInKb = 0;
            try
            {
                VersionState = VersionState.UPDATECHECKINPROGRESS;
                if (updateMirrors.Count == 0)
                {
                    Logger.Log("Updater: There are no update mirrors!");
                    return;
                }
                Logger.Log("Updater: Checking version on the server.");
                WebClient webClient = new WebClient
                {
                    CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore),
                    Encoding = Encoding.GetEncoding("Windows-1252")
                };
                webClient.Headers.Add(HttpRequestHeader.UserAgent, GetUserAgentString());
                while (currentUpdateMirrorIndex < updateMirrors.Count)
                {
                    try
                    {
                        Logger.Log("Updater: Trying to connect to update mirror " + updateMirrors[currentUpdateMirrorIndex].URL);
                        webClient.DownloadFile(updateMirrors[currentUpdateMirrorIndex].URL + VERSION_FILE, GamePath + VERSION_FILE + "_u");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("Updater: Error connecting to update mirror. Error message: " + ex.Message);
                        Logger.Log("Updater: Seeking other mirrors...");
                        currentUpdateMirrorIndex++;
                        if (currentUpdateMirrorIndex >= updateMirrors.Count)
                        {
                            currentUpdateMirrorIndex = 0;
                            throw new Exception("Unable to connect to update servers.");
                        }
                        continue;
                    }
                    break;
                }
                webClient.Dispose();
                Logger.Log("Updater: Downloaded version information.");
                IniFile iniFile = new IniFile(GamePath + VERSION_FILE + "_u");
                string stringValue = iniFile.GetStringValue("DTA", "Version", "");
                string stringValue2 = iniFile.GetStringValue("DTA", "UpdaterVersion", "N/A");
                string stringValue3 = iniFile.GetStringValue("DTA", "ManualDownloadURL", "");
                if (iniFile.SectionExists("FileVersions"))
                {
                    foreach (string sectionKey in iniFile.GetSectionKeys("FileVersions"))
                    {
                        string[] array = iniFile.GetStringValue("FileVersions", sectionKey, "").Split(',');
                        if (array.Length < 2)
                        {
                            Logger.Log("Updater: Warning: Malformed file info in downloaded version information: " + sectionKey);
                            continue;
                        }
                        GetArchiveInfo(iniFile, sectionKey, out var archiveID, out var archiveSize);
                        UpdaterFileInfo item = CreateFileInfo(sectionKey, array[0], Conversions.IntFromString(array[1], 0), archiveID, archiveSize);
                        ServerFileInfos.Add(item);
                    }
                }
                if (iniFile.SectionExists("AddOns"))
                {
                    foreach (string sectionKey2 in iniFile.GetSectionKeys("AddOns"))
                    {
                        string[] array2 = iniFile.GetStringValue("AddOns", sectionKey2, "").Split(',');
                        if (array2.Length < 2)
                        {
                            Logger.Log("Updater: Warning: Malformed addon info in downloaded version information: " + sectionKey2);
                            continue;
                        }
                        UpdaterFileInfo updaterFileInfo = CreateFileInfo(sectionKey2, array2[0], Conversions.IntFromString(array2[1], 0), "");
                        int componentIndex = GetComponentIndex(sectionKey2);
                        if (componentIndex == -1)
                        {
                            Logger.Log("Updater: Warning: Invalid custom component ID " + sectionKey2);
                            continue;
                        }
                        CustomComponent customComponent = customComponents[componentIndex];
                        customComponent.Initialized = false;
                        Logger.Log("Updater: Setting custom component info for " + sectionKey2);
                        GetArchiveInfo(iniFile, customComponent.LocalPath, out var archiveID2, out var archiveSize2);
                        updaterFileInfo.ArchiveIdentifier = archiveID2;
                        updaterFileInfo.ArchiveSize = archiveSize2;
                        customComponent.RemoteSize = updaterFileInfo.Size * 1024;
                        customComponent.RemoteArchiveSize = (updaterFileInfo.Archived ? (updaterFileInfo.ArchiveSize * 1024) : 0);
                        customComponent.RemoteIdentifier = updaterFileInfo.Identifier;
                        customComponent.Archived = updaterFileInfo.Archived;
                        if (File.Exists(GamePath + customComponent.LocalPath))
                        {
                            customComponent.LocalIdentifier = GetUniqueIdForFile(customComponent.LocalPath);
                        }
                        customComponent.Initialized = true;
                    }
                }
                if (string.IsNullOrEmpty(stringValue))
                {
                    throw new Exception("Update server integrity error while checking for updates.");
                }
                Logger.Log("Updater: Server game version is " + stringValue + ", local version is " + GameVersion);
                ServerGameVersion = stringValue;
                if (stringValue == GameVersion)
                {
                    VersionState = VersionState.UPTODATE;
                    File.Delete(GamePath + VERSION_FILE + "_u");
                    DoFileIdentifiersUpdatedEvent();
                    if (AreCustomComponentsOutdated())
                    {
                        DoCustomComponentsOutdatedEvent();
                    }
                }
                else if (stringValue2 != "N/A" && UpdaterVersion != stringValue2)
                {
                    Logger.Log("Updater: Server update system version is set to " + stringValue2 + " and is different to local update system version " + UpdaterVersion + ". Manual update required.");
                    VersionState = VersionState.OUTDATED;
                    ManualUpdateRequired = true;
                    ManualDownloadURL = stringValue3;
                    File.Delete(GamePath + VERSION_FILE + "_u");
                    DoFileIdentifiersUpdatedEvent();
                }
                else
                {
                    VersionCheckHandle();
                }
            }
            catch (Exception ex2)
            {
                VersionState = VersionState.UNKNOWN;
                Logger.Log("Updater: An error occured while performing version check: " + ex2.Message);
                DoFileIdentifiersUpdatedEvent();
            }
        }

        private static bool AreCustomComponentsOutdated()
        {
            Logger.Log("Updater: Checking if custom components are outdated.");
            foreach (CustomComponent customComponent in customComponents)
            {
                if (File.Exists(GamePath + customComponent.LocalPath) && customComponent.RemoteIdentifier != customComponent.LocalIdentifier)
                {
                    return true;
                }
            }
            return false;
        }

        private static void ExecuteAfterUpdateScript()
        {
            Logger.Log("Updater: Downloading updateexec.");
            try
            {
                WebClient webClient = new WebClient
                {
                    CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore),
                    Encoding = Encoding.GetEncoding("Windows-1252")
                };
                webClient.Headers.Add(HttpRequestHeader.UserAgent, GetUserAgentString());
                webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                webClient.DownloadFile(updateMirrors[currentUpdateMirrorIndex].URL + "updateexec", GamePath + "updateexec");
                webClient.CancelAsync();
                webClient.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log("Updater: Warning: Downloading updateexec failed: " + ex.Message);
                return;
            }
            ExecuteScript("updateexec");
        }

        private static bool ExecutePreUpdateScript()
        {
            Logger.Log("Updater: Downloading preupdateexec.");
            try
            {
                WebClient webClient = new WebClient
                {
                    CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore),
                    Encoding = Encoding.GetEncoding("Windows-1252")
                };
                webClient.Headers.Add(HttpRequestHeader.UserAgent, GetUserAgentString());
                webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                webClient.DownloadFile(updateMirrors[currentUpdateMirrorIndex].URL + "preupdateexec", GamePath + "preupdateexec");
                webClient.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log("Updater: Warning: Downloading preupdateexec failed: " + ex.Message);
                return false;
            }
            ExecuteScript("preupdateexec");
            return true;
        }

        private static void ExecuteScript(string fileName)
        {
            Logger.Log("Updater: Executing " + fileName + ".");
            IniFile iniFile = new IniFile(GamePath + fileName);
            foreach (string key in GetKeys(iniFile, "Delete"))
            {
                Logger.Log("Updater: " + fileName + ": Deleting file " + key);
                try
                {
                    File.Delete(GamePath + key);
                }
                catch
                {
                }
            }
            foreach (string key2 in GetKeys(iniFile, "Rename"))
            {
                string stringValue = iniFile.GetStringValue("Rename", key2, "");
                if (!string.IsNullOrWhiteSpace(stringValue))
                {
                    try
                    {
                        Logger.Log("Updater: " + fileName + ": Renaming file '" + key2 + "' to '" + stringValue + "'");
                        File.Move(GamePath + key2, GamePath + stringValue);
                    }
                    catch
                    {
                    }
                }
            }
            foreach (string key3 in GetKeys(iniFile, "RenameFolder"))
            {
                string stringValue2 = iniFile.GetStringValue("RenameFolder", key3, "");
                if (!string.IsNullOrWhiteSpace(stringValue2))
                {
                    try
                    {
                        Logger.Log("Updater: " + fileName + ": Renaming directory '" + key3 + "' to '" + stringValue2 + "'");
                        Directory.Move(GamePath + key3, GamePath + stringValue2);
                    }
                    catch
                    {
                    }
                }
            }
            foreach (string key4 in GetKeys(iniFile, "RenameAndMerge"))
            {
                string text = key4;
                string stringValue3 = iniFile.GetStringValue("RenameAndMerge", key4, "");
                if (string.IsNullOrWhiteSpace(stringValue3))
                {
                    continue;
                }
                try
                {
                    Logger.Log("Updater: " + fileName + ": Merging directory '" + text + "' with '" + stringValue3 + "'");
                    if (!Directory.Exists(GamePath + stringValue3))
                    {
                        Logger.Log("Updater: " + fileName + ": Destination directory '" + stringValue3 + "' does not exist, renaming.");
                        Directory.Move(GamePath + text, GamePath + stringValue3);
                        continue;
                    }
                    Logger.Log("Updater: " + fileName + ": Destination directory '" + stringValue3 + "' exists, performing selective merging.");
                    string[] files = Directory.GetFiles(GamePath + text);
                    for (int i = 0; i < files.Length; i++)
                    {
                        string fileName2 = Path.GetFileName(files[i]);
                        string text2 = Path.Combine(GamePath, text, fileName2);
                        string text3 = Path.Combine(GamePath, stringValue3, fileName2);
                        if (File.Exists(text3))
                        {
                            Logger.Log("Updater: " + fileName + ": Destination file '" + stringValue3 + "/" + fileName2 + "' exists, removing original source file " + text + "/" + fileName2);
                            File.Delete(text2);
                        }
                        else
                        {
                            Logger.Log("Updater: " + fileName + ": Destination file '" + stringValue3 + "/" + fileName2 + "' does not exist, moving original source file " + text + "/" + fileName2);
                            File.Move(text2, text3);
                        }
                    }
                }
                catch
                {
                }
            }
            string[] array = new string[2] { "DeleteFolder", "ForceDeleteFolder" };
            foreach (string sectionName in array)
            {
                foreach (string key5 in GetKeys(iniFile, sectionName))
                {
                    try
                    {
                        Logger.Log("Updater: " + fileName + ": Deleting directory '" + key5 + "'");
                        Directory.Delete(GamePath + key5, recursive: true);
                    }
                    catch
                    {
                    }
                }
            }
            foreach (string key6 in GetKeys(iniFile, "DeleteFolderIfEmpty"))
            {
                try
                {
                    Logger.Log("Updater: " + fileName + ": Deleting directory '" + key6 + "' if it's empty.");
                    if (Directory.Exists(key6))
                    {
                        if (Directory.GetFiles(GamePath + key6).Length == 0)
                        {
                            Directory.Delete(GamePath + key6);
                            continue;
                        }
                        Logger.Log("Updater: " + fileName + ": Directory '" + key6 + "' is not empty!");
                    }
                    else
                    {
                        Logger.Log("Updater: " + fileName + ": Specified directory does not exist.");
                    }
                }
                catch
                {
                }
            }
            foreach (string key7 in GetKeys(iniFile, "CreateFolder"))
            {
                try
                {
                    if (!Directory.Exists(GamePath + key7))
                    {
                        Logger.Log("Updater: " + fileName + ": Creating directory '" + key7 + "'");
                        Directory.CreateDirectory(GamePath + key7);
                    }
                    else
                    {
                        Logger.Log("Updater: " + fileName + ": Directory '" + key7 + "' already exists.");
                    }
                }
                catch
                {
                }
            }
            File.Delete(GamePath + fileName);
        }

        private static void VersionCheckHandle()
        {
            Logger.Log("Updater: Gathering list of files to be downloaded. Server file info count: " + ServerFileInfos.Count);
            FileInfosToDownload.Clear();
            for (int i = 0; i < ServerFileInfos.Count; i++)
            {
                string identifier = ServerFileInfos[i].Identifier;
                bool flag = false;
                for (int j = 0; j < LocalFileInfos.Count; j++)
                {
                    UpdaterFileInfo updaterFileInfo = LocalFileInfos[j];
                    if (ServerFileInfos[i].Filename == updaterFileInfo.Filename)
                    {
                        flag = true;
                        if (!File.Exists(GamePath + ServerFileInfos[i].Filename))
                        {
                            Logger.Log("Updater: File " + ServerFileInfos[i].Filename + " not found. Adding it to the download queue.");
                            FileInfosToDownload.Add(ServerFileInfos[i]);
                        }
                        else if (updaterFileInfo.Identifier != identifier)
                        {
                            Logger.Log("Updater: Local file " + updaterFileInfo.Filename + " is different, adding it to the download queue.");
                            FileInfosToDownload.Add(ServerFileInfos[i]);
                        }
                    }
                }
                if (flag)
                {
                    continue;
                }
                Logger.Log("Updater: File " + ServerFileInfos[i].Filename + " doesn't exist on local version information - checking if it exists in the directory.");
                if (File.Exists(GamePath + ServerFileInfos[i].Filename))
                {
                    if (TryGetUniqueId(ServerFileInfos[i].Filename) != identifier)
                    {
                        Logger.Log("Updater: File " + ServerFileInfos[i].Filename + " is out of date. Adding it to the download queue.");
                        FileInfosToDownload.Add(ServerFileInfos[i]);
                    }
                    else
                    {
                        Logger.Log("Updater: File " + ServerFileInfos[i].Filename + " exists in the directory and is up to date.");
                    }
                }
                else
                {
                    Logger.Log("Updater: File " + ServerFileInfos[i].Filename + " not found. Adding it to the download queue.");
                    FileInfosToDownload.Add(ServerFileInfos[i]);
                }
            }
            UpdateSizeInKb = 0;
            for (int k = 0; k < FileInfosToDownload.Count; k++)
            {
                UpdateSizeInKb += (FileInfosToDownload[k].Archived ? FileInfosToDownload[k].ArchiveSize : FileInfosToDownload[k].Size);
            }
            VersionState = VersionState.OUTDATED;
            ManualUpdateRequired = false;
            DoFileIdentifiersUpdatedEvent();
        }

        private static void VerifyLocalFileVersions()
        {
            Logger.Log("Verifying local file versions. Count: " + LocalFileInfos.Count);
            for (int i = 0; i < LocalFileInfos.Count; i++)
            {
                UpdaterFileInfo updaterFileInfo = LocalFileInfos[i];
                if (ContainsAnyMask(updaterFileInfo.Filename))
                {
                    continue;
                }
                if (File.Exists(GamePath + updaterFileInfo.Filename))
                {
                    string uniqueIdForFile = GetUniqueIdForFile(updaterFileInfo.Filename);
                    if (uniqueIdForFile != updaterFileInfo.Identifier)
                    {
                        Logger.Log("Invalid unique identifier for " + updaterFileInfo.Filename + "!");
                        updaterFileInfo.Identifier = uniqueIdForFile;
                    }
                }
                else
                {
                    Logger.Log("File " + updaterFileInfo.Filename + " does not exist!");
                    LocalFileInfos.RemoveAt(i);
                    i--;
                }
                if (LocalFileInfos.Count > 0)
                {
                    Updater.LocalFileCheckProgressChanged?.Invoke(i + 1, LocalFileInfos.Count);
                }
            }
        }

        private static void PerformUpdate()
        {
            Logger.Log("Updater: Starting update.");
            VersionState = VersionState.UPDATEINPROGRESS;
            try
            {
                if (!ExecutePreUpdateScript())
                {
                    throw new Exception("Executing preupdateexec failed.");
                }
                VerifyLocalFileVersions();
                VersionCheckHandle();
                if (string.IsNullOrEmpty(ServerGameVersion) || ServerGameVersion == "N/A" || VersionState != VersionState.OUTDATED)
                {
                    throw new Exception("Update server integrity error.");
                }
                VersionState = VersionState.UPDATEINPROGRESS;
                totalDownloadedKbs = 0;
                if (terminateUpdate)
                {
                    Logger.Log("Updater: Terminating update because of user request.");
                    VersionState = VersionState.OUTDATED;
                    ManualUpdateRequired = false;
                    terminateUpdate = false;
                    return;
                }
                WebClient webClient = new WebClient
                {
                    CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore),
                    Encoding = Encoding.GetEncoding("Windows-1252")
                };
                webClient.Headers.Add(HttpRequestHeader.UserAgent, GetUserAgentString());
                webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
                foreach (UpdaterFileInfo item in FileInfosToDownload)
                {
                    bool flag = false;
                    int num = 0;
                    if (terminateUpdate)
                    {
                        Logger.Log("Updater: Terminating update because of user request.");
                        VersionState = VersionState.OUTDATED;
                        ManualUpdateRequired = false;
                        terminateUpdate = false;
                        return;
                    }
                    while (true)
                    {
                        currentFilename = (item.Archived ? (item.Filename + ".lzma") : item.Filename);
                        currentFileSize = (item.Archived ? item.ArchiveSize : item.Size);
                        flag = DownloadFile(webClient, item);
                        if (terminateUpdate)
                        {
                            Logger.Log("Updater: Terminating update because of user request.");
                            VersionState = VersionState.OUTDATED;
                            ManualUpdateRequired = false;
                            terminateUpdate = false;
                            return;
                        }
                        if (flag)
                        {
                            break;
                        }
                        num++;
                        if (num == 2)
                        {
                            Logger.Log("Updater: Too many retries for downloading file " + (item.Archived ? (item.Filename + ".lzma") : item.Filename) + ". Update halted.");
                            throw new Exception("Too many retries for downloading file " + (item.Archived ? (item.Filename + ".lzma") : item.Filename));
                        }
                    }
                    totalDownloadedKbs += (item.Archived ? item.ArchiveSize : item.Size);
                }
                webClient.Dispose();
                if (terminateUpdate)
                {
                    Logger.Log("Updater: Terminating update because of user request.");
                    VersionState = VersionState.OUTDATED;
                    ManualUpdateRequired = false;
                    terminateUpdate = false;
                    return;
                }
                Logger.Log("Updater: Downloading files finished - copying from temporary updater directory.");
                ExecuteAfterUpdateScript();
                Logger.Log("Updater: Cleaning up.");
                if (Directory.Exists(GamePath + "Updater"))
                {
                    File.Move(GamePath + VERSION_FILE + "_u", GamePath + "Updater/" + VERSION_FILE);
                }
                else
                {
                    File.Move(GamePath + VERSION_FILE + "_u", GamePath + VERSION_FILE);
                }
                if (File.Exists(GamePath + "Theme_c.ini"))
                {
                    Logger.Log("Updater: Theme_c.ini exists -- copying it.");
                    File.Copy(GamePath + "Theme_c.ini", GamePath + "INI/Theme.ini", overwrite: true);
                    Logger.Log("Updater: Theme.ini copied succesfully.");
                }
                if (Directory.Exists(GamePath + "Updater"))
                {
                    if (File.Exists(GamePath + "Updater/Resources/SecondStageUpdater.exe"))
                    {
                        DeleteFileAndWait(ResourcePath + "SecondStageUpdater.exe");
                        File.Move(GamePath + "Updater/Resources/SecondStageUpdater.exe", ResourcePath + "SecondStageUpdater.exe");
                    }
                    Logger.Log("Updater: Launching second-stage updater executable SecondStageUpdater.exe.");
                    Process process = new Process();
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = ResourcePath + "SecondStageUpdater.exe";
                    process.StartInfo.Arguments = CURRENT_CLIENT_EXECUTABLE + " \"" + GamePath + "\"";
                    process.Start();
                    Updater.Restart?.Invoke(null, EventArgs.Empty);
                }
                else
                {
                    Logger.Log("Updater: Update completed succesfully.");
                    totalDownloadedKbs = 0;
                    UpdateSizeInKb = 0;
                    CheckLocalFileVersions();
                    ServerGameVersion = "N/A";
                    VersionState = VersionState.UPTODATE;
                    DoUpdateCompleted();
                    if (AreCustomComponentsOutdated())
                    {
                        DoCustomComponentsOutdatedEvent();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Updater: An error occured during the update. Message: " + ex.Message);
                VersionState = VersionState.UNKNOWN;
                DoOnUpdateFailed(ex);
            }
        }

        private static bool DownloadFile(WebClient client, UpdaterFileInfo fileInfo)
        {
            Logger.Log("Updater: Initiliazing download of file " + fileInfo.Filename);
            UpdateDownloadProgress(0);
            currentDownloadException = null;
            string filename = fileInfo.Filename;
            string text = "Updater/";
            try
            {
                int size = fileInfo.Size;
                string text2 = "";
                int index = currentUpdateMirrorIndex;
                string text3 = (fileInfo.Archived ? ".lzma" : "");
                text2 = (updateMirrors[index].URL + filename + text3).Replace("\\", "/");
                CreatePath(GamePath + filename);
                CreatePath(GamePath + text + filename + text3);
                if (File.Exists(GamePath + text + filename + text3) && (fileInfo.Archived ? fileInfo.Identifier : fileInfo.ArchiveIdentifier) == GetUniqueIdForFile(text + filename + text3))
                {
                    Logger.Log("Updater: File " + filename + " has already been downloaded, skipping downloading.");
                }
                else
                {
                    Logger.Log("Updater: Downloading file " + filename + text3);
                    client.DownloadFileAsync(new Uri(text2), GamePath + text + filename + text3);
                    while (client.IsBusy)
                    {
                        Thread.Sleep(10);
                    }
                    if (currentDownloadException != null)
                    {
                        throw currentDownloadException;
                    }
                    Updater.OnFileDownloadCompleted?.Invoke(fileInfo.Archived ? (filename + text3) : null);
                    Logger.Log("Updater: Download of file " + filename + text3 + " finished - verifying.");
                    if (fileInfo.Archived)
                    {
                        Logger.Log("Updater: File is an archive.");
                        string text4 = CheckFileIdentifiers(filename, text + filename + text3, fileInfo.ArchiveIdentifier);
                        if (!string.IsNullOrEmpty(text4))
                        {
                            Logger.Log("Updater: Downloaded archive " + filename + text3 + " has a non-matching identifier: " + text4 + " against " + fileInfo.ArchiveIdentifier);
                            DeleteFileAndWait(GamePath + text + filename + text3);
                            return false;
                        }
                        Logger.Log("Updater: Archive " + filename + text3 + " is intact. Unpacking...");
                        CompressionHelper.DecompressFile(GamePath + text + filename + text3, GamePath + text + filename);
                        File.Delete(GamePath + text + filename + text3);
                    }
                    client.CancelAsync();
                }
                string text5 = CheckFileIdentifiers(filename, text + filename, fileInfo.Identifier);
                if (string.IsNullOrEmpty(text5))
                {
                    Logger.Log("Updater: File " + filename + " is intact.");
                    return true;
                }
                Logger.Log("Updater: Downloaded file " + filename + " has a non-matching identifier: " + text5 + " against " + fileInfo.Identifier);
                DeleteFileAndWait(GamePath + text + filename);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Log("Updater: An error occured while downloading file " + filename + ": " + ex.Message);
                DeleteFileAndWait(GamePath + text + filename);
                client.CancelAsync();
                return false;
            }
        }

        private static void UpdateDownloadProgress(int progressPercentage)
        {
            double num = (double)currentFileSize * ((double)progressPercentage / 100.0);
            double num2 = (double)totalDownloadedKbs + num;
            int totalPercentage = 0;
            if (UpdateSizeInKb > 0 && UpdateSizeInKb < int.MaxValue)
            {
                totalPercentage = (int)(num2 / (double)UpdateSizeInKb * 100.0);
            }
            DownloadProgressChanged(currentFilename, progressPercentage, totalPercentage);
        }

        private static bool ContainsAnyMask(string filePath)
        {
            string[] array = ignoreMasks;
            foreach (string text in array)
            {
                if (filePath.ToUpper().Contains(text.ToUpper()))
                {
                    return true;
                }
            }
            return false;
        }

        private static List<string> GetKeys(IniFile iniFile, string sectionName)
        {
            List<string> sectionKeys = iniFile.GetSectionKeys(sectionName);
            if (sectionKeys != null)
            {
                return sectionKeys;
            }
            return new List<string>();
        }

        private static string TryGetUniqueId(string filePath)
        {
            try
            {
                return GetUniqueIdForFile(filePath);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string CheckFileIdentifiers(string fileInfoFilename, string localFilename, string fileInfoIdentifier)
        {
            string text = ((!ContainsAnyMask(fileInfoFilename)) ? GetUniqueIdForFile(localFilename) : fileInfoIdentifier);
            if (fileInfoIdentifier == text)
            {
                return null;
            }
            return text;
        }

        private static void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            UpdateDownloadProgress(e.ProgressPercentage);
        }

        private static void WebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            currentDownloadException = e.Error;
        }

        private static void DownloadProgressChanged(string currFileName, int currentFilePercentage, int totalPercentage)
        {
            Updater.UpdateProgressChanged?.Invoke(currFileName, currentFilePercentage, totalPercentage);
        }

        private static void DoCustomComponentsOutdatedEvent()
        {
            Updater.OnCustomComponentsOutdated?.Invoke();
        }

        private static void DoFileIdentifiersUpdatedEvent()
        {
            Logger.Log("Updater: File identifiers updated.");
            Updater.FileIdentifiersUpdated?.Invoke();
        }

        private static void DoOnUpdateFailed(Exception ex)
        {
            Updater.OnUpdateFailed?.Invoke(ex);
        }

        private static void DoOnVersionStateChanged()
        {
            Updater.OnVersionStateChanged?.Invoke();
        }

        private static void DoUpdateCompleted()
        {
            Updater.OnUpdateCompleted?.Invoke();
        }
    }
}