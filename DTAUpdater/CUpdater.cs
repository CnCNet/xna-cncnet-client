using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Rampastring.Tools;
using ClientCore;
using System.Windows.Forms;

namespace Updater
{
    public static class CUpdater
    {
        public delegate void NoParamEventHandler();

        public delegate void UpdateProgressChangedCallback(string currFileName, int currFilePercentage, int totalPercentage);

        public delegate void SetExceptionCallback(Exception ex);

        public delegate void LocalFileCheckProgressChangedCallback(int checkedFileCount, int totalFileCount);

        private static readonly object locker = new object();

        private static readonly object locker2 = new object();

        private static string _gameVersion = "N/A";

        private static string _serverGameVersion = "N/A";

        private static string StatisticsURL = string.Empty;

        public static CustomComponent[] CustomComponents;

        private static string[] IgnoreMasks = new string[4] { ".rtf", ".txt", "Theme.ini", "gui_settings.xml" };

        public static List<UpdateMirror> UPDATEMIRRORS;

        private static int CurrentUpdateMirrorId = 0;

        public static bool LocalFileVersionsChecked = false;

        public static List<DTAFileInfo> LocalFileInfos = new List<DTAFileInfo>();

        private static List<DTAFileInfo> ServerFileInfos = new List<DTAFileInfo>();

        private static List<DTAFileInfo> FileInfosToDownload = new List<DTAFileInfo>();

        public static volatile bool HasVersionBeenChecked = false;

        private static string CurrentFileName = string.Empty;

        public static volatile int UpdateSizeInKb = 0;

        private static volatile int TotalDownloadedKbs = 0;

        private static int CurrentFileSize = 0;

        private static volatile VersionState _versionState = VersionState.UNKNOWN;

        public static volatile bool TerminateUpdate = false;

        public static bool CreateShortcutOnInstall = false;

        public const string LAUNCHER_UPDATER = "clientupdt.dat";

        public static string NEW_LAUNCHER_NAME = "Launcher.exe_u";

        public static string CURRENT_LAUNCHER_NAME = Path.GetFileName(Application.ExecutablePath);

        public static string VERSION_FILE = "version";

        public static string GameVersion
        {
            get
            {
                lock (locker)
                {
                    return _gameVersion;
                }
            }
            set
            {
                lock (locker)
                {
                    _gameVersion = value;
                }
            }
        }

        public static string ServerGameVersion
        {
            get
            {
                lock (locker2)
                {
                    return _serverGameVersion;
                }
            }
            set
            {
                lock (locker2)
                {
                    _serverGameVersion = value;
                }
            }
        }

        public static VersionState DTAVersionState => _versionState;

        public static event NoParamEventHandler FileIdentifiersUpdated;

        public static event NoParamEventHandler BeforeSelfUpdate;

        public static event NoParamEventHandler OnVersionStateChanged;

        public static event NoParamEventHandler OnUpdateCompleted;

        public static event NoParamEventHandler OnCustomComponentsOutdated;

        public static event SetExceptionCallback OnUpdateFailed;

        public static event UpdateProgressChangedCallback UpdateProgressChanged;

        public static event LocalFileCheckProgressChangedCallback LocalFileCheckProgressChanged;

        public static event NoParamEventHandler OnLocalFileVersionsChecked;

        public static event EventHandler Restart;

        private static void DoFileIdentifiersUpdatedEvent()
        {
            Logger.Log("File identifiers updated.");
            CUpdater.FileIdentifiersUpdated?.Invoke();
        }

        private static void DownloadProgressChanged(string currFileName, int currentFilePercentage, int totalPercentage)
        {
            CUpdater.UpdateProgressChanged?.Invoke(currFileName, currentFilePercentage, totalPercentage);
        }

        private static void OnBeforeSelfUpdate()
        {
            CUpdater.BeforeSelfUpdate?.Invoke();
        }

        private static void DoOnUpdateFailed(Exception ex)
        {
            if (CUpdater.OnUpdateFailed != null)
            {
                CUpdater.OnUpdateFailed(ex);
            }
        }

        private static void DoOnVersionStateChanged()
        {
            if (CUpdater.OnVersionStateChanged != null)
            {
                CUpdater.OnVersionStateChanged();
            }
        }

        private static void DoUpdateCompleted()
        {
            if (CUpdater.OnUpdateCompleted != null)
            {
                CUpdater.OnUpdateCompleted();
            }
        }

        private static void DoCustomComponentsOutdatedEvent()
        {
            if (CUpdater.OnCustomComponentsOutdated != null)
            {
                CUpdater.OnCustomComponentsOutdated();
            }
        }

        public static string GetUpdateServerUrl()
        {
            return UPDATEMIRRORS[CurrentUpdateMirrorId].Url;
        }

        public static void Initialize(string game)
        {
            UPDATEMIRRORS = new List<UpdateMirror>();
            switch (game)
            {
                case "TS":
                    if (!File.Exists(ProgramConstants.GamePath + "updateconfig.ini"))
                    {
                        UPDATEMIRRORS.Add(new UpdateMirror("http://dta.cnc-comm.com/ts-client/updates/", "CnCNet", "Europe"));
                        UPDATEMIRRORS.Add(new UpdateMirror("http://dta.ppmsite.com/ts-client/updates/", "Project Perfect Mod", "Europe"));
                    }
                    else
                    {
                        ParseMirrors();
                    }
                    CustomComponents = new CustomComponent[5]
                    {
                new CustomComponent("Tiberian Sun Music", "TSMUSIC", "MIX/SCORES.MIX", "MIX/SCORES.MIX", DoNotUpdateToSmaller: false),
                new CustomComponent("Firestorm Music", "FSMUSIC", "MIX/SCORES01.MIX", "MIX/SCORES01.MIX", DoNotUpdateToSmaller: false),
                new CustomComponent("TS GDI Campaign Videos", "MOVIES01", "MIX/Movies01.mix", "MIX/Movies01.mix", DoNotUpdateToSmaller: false),
                new CustomComponent("TS Nod Campaign Videos", "MOVIES02", "MIX/Movies02.mix", "MIX/Movies02.mix", DoNotUpdateToSmaller: false),
                new CustomComponent("Firestorm Campaign Videos", "MOVIES03", "MIX/Movies03.mix", "MIX/Movies03.mix", DoNotUpdateToSmaller: false)
                    };
                    break;
                case "DTA":
                    if (!File.Exists(ProgramConstants.GamePath + "updateconfig.ini"))
                    {
                        UPDATEMIRRORS.Add(new UpdateMirror("http://dta.cnc-comm.com/updates/", "CnCNet", "Europe"));
                        UPDATEMIRRORS.Add(new UpdateMirror("http://dta.ppmsite.com/updates/", "Project Perfect Mod", "Europe"));
                    }
                    else
                    {
                        ParseMirrors();
                    }
                    CustomComponents = new CustomComponent[5]
                    {
                new CustomComponent("Tiberian Dawn Music", "TDSCORES", "MIX/Scores.mix", "MIX/Scores.mix", DoNotUpdateToSmaller: false),
                new CustomComponent("Red Alert Music", "RASCORES", "MIX/Scores01.mix", "MIX/Scores01.mix", DoNotUpdateToSmaller: false),
                new CustomComponent("Sole Survivor Music", "SSSCORES", "MIX/Expand98.mix", "MIX/Expand98.mix", DoNotUpdateToSmaller: false),
                new CustomComponent("Vocal Music Variants", "VSCORES", "MIX/Expand99.mix", "MIX/Expand99.mix", DoNotUpdateToSmaller: false),
                new CustomComponent("Ingame Videos", "FMVs", "MIX/Movies.mix", "MIX/Movies.mix", DoNotUpdateToSmaller: false)
                    };
                    break;
                case "TI":
                    if (!File.Exists(ProgramConstants.GamePath + "updateconfig.ini"))
                    {
                        UPDATEMIRRORS.Add(new UpdateMirror("http://ti.ppmsite.com/updates/", "Project Perfect Mod", "Europe"));
                    }
                    else
                    {
                        ParseMirrors();
                    }
                    CustomComponents = new CustomComponent[2]
                    {
                new CustomComponent("Low-res ingame videos (VQA)", "VQAFMVs", "MIX/MOVIES.MIX", "MIX/MOVIES.MIX", DoNotUpdateToSmaller: false),
                new CustomComponent("High-res ingame videos (Bink)", "BinkFMVs", "MIX/EXPAND03.MIX", "MIX/EXPAND03.MIX", DoNotUpdateToSmaller: false)
                    };
                    break;
                case "YR":
                    if (!File.Exists(ProgramConstants.GamePath + "updateconfig.ini"))
                    {
                        UPDATEMIRRORS.Add(new UpdateMirror("http://downloads.cncnet.org/updates/yr/", "CnCNet", "Europe"));
                    }
                    else
                    {
                        ParseMirrors();
                    }
                    CustomComponents = new CustomComponent[0];
                    break;
                case "MO":
                    if (!File.Exists(ProgramConstants.GamePath + "updateconfig.ini"))
                    {
                        UPDATEMIRRORS.Add(new UpdateMirror("http://mentalomega.com/game/update/", "MO Update Server", "mentalomega.com"));
                    }
                    else
                    {
                        ParseMirrors();
                    }
                    CustomComponents = new CustomComponent[0];
                    break;
                case "PP":
                    if (!File.Exists(ProgramConstants.GamePath + "updateconfig.ini"))
                    {
                        UPDATEMIRRORS.Add(new UpdateMirror("http://projectphantom.net/updates/", "Project Phantom Update Server", "projectphantom.net"));
                    }
                    else
                    {
                        ParseMirrors();
                    }
                    CustomComponents = new CustomComponent[0];
                    break;
                case "CNCR":
                    if (!File.Exists(ProgramConstants.GamePath + "updateconfig.ini"))
                    {
                        UPDATEMIRRORS.Add(new UpdateMirror("http://reloaded.cncguild.net/cncr_updates/", "C&C Guild", "cncguild.net"));
                    }
                    else
                    {
                        ParseMirrors();
                    }
                    CustomComponents = new CustomComponent[0];
                    break;
                default:
                    Logger.Log("Updater: Unknown game " + game);
                    if (File.Exists(ProgramConstants.GamePath + "updateconfig.ini"))
                    {
                        ParseMirrors();
                    }
                    CustomComponents = new CustomComponent[0];
                    break;
            }
            Logger.Log("Update mirror count: " + UPDATEMIRRORS.Count);
            Logger.Log("Running from: " + CURRENT_LAUNCHER_NAME);
            List<UpdateMirror> list = new List<UpdateMirror>();
            IniFile iniFile = new IniFile(ProgramConstants.GamePath + ClientConfiguration.Instance.SettingsIniName);
            List<string> sectionKeys = iniFile.GetSectionKeys("DownloadMirrors");
            if (sectionKeys != null)
            {
                foreach (string item in sectionKeys)
                {
                    string value = iniFile.GetStringValue("DownloadMirrors", item, string.Empty);
                    UpdateMirror updateMirror = UPDATEMIRRORS.Find((UpdateMirror um) => um.Name == value);
                    if (updateMirror != null && !list.Contains(updateMirror))
                    {
                        list.Add(updateMirror);
                    }
                }
            }
            foreach (UpdateMirror uPDATEMIRROR in UPDATEMIRRORS)
            {
                if (!list.Contains(uPDATEMIRROR))
                {
                    list.Add(uPDATEMIRROR);
                }
            }
            UPDATEMIRRORS = list;
        }

        public static void ClearVersionInfo()
        {
            LocalFileInfos.Clear();
            ServerFileInfos.Clear();
            FileInfosToDownload.Clear();
            GameVersion = "N/A";
            _versionState = VersionState.UNKNOWN;
            CUpdater.OnVersionStateChanged?.Invoke();
        }

        private static void ParseMirrors()
        {
            INIReader iNIReader = new INIReader();
            iNIReader.InitINIReader(ProgramConstants.GamePath + "updateconfig.ini");
            while (!iNIReader.ReaderClosed)
            {
                iNIReader.ReadNextLine();
                if (iNIReader.isLineReadable())
                {
                    string currentLine = iNIReader.CurrentLine;
                    _ = new string[3];
                    string[] array = currentLine.Split(',');
                    string url = array[0].Trim();
                    string name = array[1].Trim();
                    string location = array[2].Trim();
                    UPDATEMIRRORS.Add(new UpdateMirror(url, name, location));
                }
            }
            iNIReader.CloseINIReader();
        }

        public static void CheckLocalFileVersions()
        {
            LocalFileInfos.Clear();
            GameVersion = "N/A";
            Logger.Log("Checking local file versions.");
            IniFile iniFile = new IniFile(ProgramConstants.GamePath + VERSION_FILE);
            GameVersion = iniFile.GetStringValue("DTA", "Version", "N/A");
            List<string> sectionKeys = iniFile.GetSectionKeys("FileVersions");
            new List<DTAFileInfo>();
            if (sectionKeys != null)
            {
                foreach (string item in sectionKeys)
                {
                    string[] array = iniFile.GetStringValue("FileVersions", item, string.Empty).Split(',');
                    if (array.Length == 2)
                    {
                        DTAFileInfo dTAFileInfo = new DTAFileInfo();
                        dTAFileInfo.Name = item.Replace('\\', '/');
                        dTAFileInfo.Identifier = array[0];
                        int result = 0;
                        if (int.TryParse(array[1], out result))
                        {
                            dTAFileInfo.Size = result;
                            LocalFileInfos.Add(dTAFileInfo);
                        }
                    }
                }
            }
            CUpdater.OnLocalFileVersionsChecked?.Invoke();
            LocalFileVersionsChecked = true;
        }

        public static bool IsFileNonexistantOrOriginal(string filePath)
        {
            DTAFileInfo dTAFileInfo = LocalFileInfos.Find((DTAFileInfo f) => f.Name.ToLower() == filePath.ToLower());
            if (dTAFileInfo == null)
            {
                return true;
            }
            string uniqueIdForFile = GetUniqueIdForFile(dTAFileInfo.Name);
            return dTAFileInfo.Identifier == uniqueIdForFile;
        }

        private static void VerifyLocalFileVersions()
        {
            Logger.Log("Verifying local file versions. Count: " + LocalFileInfos.Count);
            for (int i = 0; i < LocalFileInfos.Count; i++)
            {
                DTAFileInfo dTAFileInfo = LocalFileInfos[i];
                if (ContainsAnyMask(dTAFileInfo.Name))
                {
                    continue;
                }
                if (File.Exists(ProgramConstants.GamePath + dTAFileInfo.Name))
                {
                    string uniqueIdForFile = GetUniqueIdForFile(dTAFileInfo.Name);
                    if (uniqueIdForFile != dTAFileInfo.Identifier)
                    {
                        Logger.Log("Invalid unique identifier for " + dTAFileInfo.Name + "!");
                        dTAFileInfo.Identifier = uniqueIdForFile;
                    }
                }
                else
                {
                    Logger.Log("File " + dTAFileInfo.Name + " does not exist!");
                    LocalFileInfos.RemoveAt(i);
                    i--;
                }
                CUpdater.LocalFileCheckProgressChanged?.Invoke(i + 1, LocalFileInfos.Count);
            }
        }

        private static bool ContainsAnyMask(string str)
        {
            string[] ignoreMasks = IgnoreMasks;
            foreach (string text in ignoreMasks)
            {
                if (str.ToUpper().Contains(text.ToUpper()))
                {
                    return true;
                }
            }
            return false;
        }

        public static void CheckForUpdates()
        {
            Logger.Log("CheckForUpdates()");
            if (_versionState != VersionState.UPDATECHECKINPROGRESS && _versionState != VersionState.UPDATEINPROGRESS)
            {
                new Thread(DoVersionCheck).Start();
            }
        }

        private static void DoVersionCheck()
        {
            ServerFileInfos.Clear();
            FileInfosToDownload.Clear();
            UpdateSizeInKb = 0;
            HasVersionBeenChecked = true;
            try
            {
                _versionState = VersionState.UPDATECHECKINPROGRESS;
                DoOnVersionStateChanged();
                if (UPDATEMIRRORS.Count == 0)
                {
                    Logger.Log("There are no update mirrors!");
                    return;
                }
                Logger.Log("Checking version on the server.");
                WebClient webClient = new WebClient();
                webClient.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
#if NET48
                webClient.Encoding = Encoding.GetEncoding(1252);
#endif

                while (CurrentUpdateMirrorId < UPDATEMIRRORS.Count)
                {
                    try
                    {
                        Logger.Log("Trying to connect to update mirror " + UPDATEMIRRORS[CurrentUpdateMirrorId].Url);
                        webClient.DownloadFile(UPDATEMIRRORS[CurrentUpdateMirrorId].Url + VERSION_FILE, ProgramConstants.GamePath + VERSION_FILE + "_u");
                    }
                    catch
                    {
                        Logger.Log("Error connecting to update mirror, seeking for other mirrors");
                        CurrentUpdateMirrorId++;
                        if (CurrentUpdateMirrorId >= UPDATEMIRRORS.Count)
                        {
                            CurrentUpdateMirrorId = 0;
                            throw new Exception("Unable to connect to update servers.");
                        }
                        continue;
                    }
                    break;
                }
                webClient.Dispose();
                Logger.Log("Downloaded version information, parsing.");
                INIReader iNIReader = new INIReader();
                iNIReader.InitINIReader(ProgramConstants.GamePath + VERSION_FILE + "_u");
                string text = "";
                do
                {
                    iNIReader.ReadNextLine();
                    if (!iNIReader.isLineReadable())
                    {
                        continue;
                    }
                    if (iNIReader.CurrentSection == "DTA")
                    {
                        if (iNIReader.getCurrentKeyName() == "Version")
                        {
                            text = iNIReader.GetValue3();
                        }
                    }
                    else if (iNIReader.CurrentSection == "FileVersions")
                    {
                        DTAFileInfo item = ParseFileInfo(iNIReader.getCurrentKeyName(), iNIReader.GetValue3());
                        ServerFileInfos.Add(item);
                    }
                    else
                    {
                        if (!(iNIReader.CurrentSection == "AddOns"))
                        {
                            continue;
                        }
                        string currentKeyName = iNIReader.getCurrentKeyName();
                        DTAFileInfo dTAFileInfo = ParseFileInfo(currentKeyName, iNIReader.GetValue3());
                        int componentId = CustomComponent.getComponentId(currentKeyName);
                        if (componentId == -1)
                        {
                            Logger.Log("Warning: Updater: Invalid custom component ID " + currentKeyName);
                            continue;
                        }
                        CustomComponent customComponent = CustomComponents[componentId];
                        Logger.Log("Updater: Setting custom component info for " + currentKeyName);
                        customComponent.RemoteSize = dTAFileInfo.Size * 1024;
                        customComponent.RemoteIdentifier = dTAFileInfo.Identifier;
                        if (File.Exists(ProgramConstants.GamePath + customComponent.LocalPath))
                        {
                            customComponent.LocalIdentifier = GetUniqueIdForFile(customComponent.LocalPath);
                        }
                    }
                }
                while (!iNIReader.ReaderClosed);
                if (string.IsNullOrEmpty(text))
                {
                    throw new Exception("Update server integrity error while checking for updates.");
                }
                Logger.Log("Server game version is " + text + ", local version is " + GameVersion);
                ServerGameVersion = text;
                if (text == GameVersion)
                {
                    _versionState = VersionState.UPTODATE;
                    DoOnVersionStateChanged();
                    File.Delete(ProgramConstants.GamePath + VERSION_FILE + "_u");
                    DoFileIdentifiersUpdatedEvent();
                    if (AreCustomComponentsOutdated())
                    {
                        DoCustomComponentsOutdatedEvent();
                    }
                }
                else
                {
                    VersionCheckHandle();
                }
            }
            catch (Exception ex)
            {
                _versionState = VersionState.UNKNOWN;
                Logger.Log("An error occured while performing version check: " + ex.Message);
                DoOnVersionStateChanged();
                DoFileIdentifiersUpdatedEvent();
            }
        }

        private static void VersionCheckHandle()
        {
            Logger.Log("Gathering list of files to be downloaded. Server file info count: " + ServerFileInfos.Count);
            FileInfosToDownload.Clear();
            for (int i = 0; i < ServerFileInfos.Count; i++)
            {
                _ = ServerFileInfos[i].Name;
                string identifier = ServerFileInfos[i].Identifier;
                _ = ServerFileInfos[i].Size;
                bool flag = false;
                for (int j = 0; j < LocalFileInfos.Count; j++)
                {
                    DTAFileInfo dTAFileInfo = LocalFileInfos[j];
                    if (ServerFileInfos[i].Name == dTAFileInfo.Name)
                    {
                        flag = true;
                        if (dTAFileInfo.Identifier != identifier)
                        {
                            Logger.Log("Local file " + dTAFileInfo.Name + " is different, adding it to the download queue.");
                            FileInfosToDownload.Add(ServerFileInfos[i]);
                        }
                    }
                }
                if (flag)
                {
                    continue;
                }
                Logger.Log("File " + ServerFileInfos[i].Name + " doesn't exist on local version information - checking if it exists in the directory.");
                if (File.Exists(ProgramConstants.GamePath + ServerFileInfos[i].Name))
                {
                    if (TryGetUniqueId(ServerFileInfos[i].Name) != identifier)
                    {
                        Logger.Log("File " + ServerFileInfos[i].Name + " is out of date. Adding it to the download queue.");
                        FileInfosToDownload.Add(ServerFileInfos[i]);
                    }
                    else
                    {
                        Logger.Log("File " + ServerFileInfos[i].Name + " exists in the directory and is up to date.");
                    }
                }
                else
                {
                    Logger.Log("File " + ServerFileInfos[i].Name + " not found. Adding it to the download queue.");
                    FileInfosToDownload.Add(ServerFileInfos[i]);
                }
            }
            UpdateSizeInKb = 0;
            for (int k = 0; k < FileInfosToDownload.Count; k++)
            {
                UpdateSizeInKb += FileInfosToDownload[k].Size;
            }
            _versionState = VersionState.OUTDATED;
            DoOnVersionStateChanged();
            DoFileIdentifiersUpdatedEvent();
        }

        public static void StartAsyncUpdate()
        {
            new Thread(PerformUpdate).Start();
        }

        private static void PerformUpdate()
        {
            Logger.Log("Starting update.");
            _versionState = VersionState.UPDATEINPROGRESS;
            DoOnVersionStateChanged();
            try
            {
                if (!ExecutePreUpdateexec())
                {
                    throw new Exception("Executing preupdateexec failed.");
                }
                VerifyLocalFileVersions();
                VersionCheckHandle();
                if (string.IsNullOrEmpty(ServerGameVersion) || ServerGameVersion == "N/A" || DTAVersionState != VersionState.OUTDATED)
                {
                    throw new Exception("Update server integrity error.");
                }
                _versionState = VersionState.UPDATEINPROGRESS;
                DoOnVersionStateChanged();
                TotalDownloadedKbs = 0;
                if (TerminateUpdate)
                {
                    Logger.Log("Terminating update because of user request.");
                    _versionState = VersionState.OUTDATED;
                    DoOnVersionStateChanged();
                    TerminateUpdate = false;
                    return;
                }
                foreach (DTAFileInfo item in FileInfosToDownload)
                {
                    bool flag = false;
                    int num = 0;
                    if (TerminateUpdate)
                    {
                        Logger.Log("Terminating update because of user request.");
                        _versionState = VersionState.OUTDATED;
                        DoOnVersionStateChanged();
                        TerminateUpdate = false;
                        return;
                    }
                    while (true)
                    {
                        CurrentFileName = item.Name;
                        CurrentFileSize = item.Size;
                        flag = UpdateFile(item);
                        if (TerminateUpdate)
                        {
                            Logger.Log("Terminating update because of user request.");
                            _versionState = VersionState.OUTDATED;
                            DoOnVersionStateChanged();
                            TerminateUpdate = false;
                            return;
                        }
                        if (flag)
                        {
                            break;
                        }
                        num++;
                        if (num == 2)
                        {
                            Logger.Log("Too many retries for downloading file " + item.Name + ". Update halted.");
                            throw new Exception("Too many retries for downloading file " + item.Name);
                        }
                    }
                    TotalDownloadedKbs += item.Size;
                }
                if (TerminateUpdate)
                {
                    Logger.Log("Terminating update because of user request.");
                    _versionState = VersionState.OUTDATED;
                    DoOnVersionStateChanged();
                    TerminateUpdate = false;
                    return;
                }
                Logger.Log("Downloading files finished - copying from temporary updater directory.");
                ExecuteAfterUpdateexec();
                Logger.Log("Cleaning up.");
                File.Delete(ProgramConstants.GamePath + VERSION_FILE);
                if (Directory.Exists(ProgramConstants.GamePath + "Updater"))
                {
                    File.Move(ProgramConstants.GamePath + VERSION_FILE + "_u", ProgramConstants.GamePath + "Updater/" + VERSION_FILE);
                }
                else
                {
                    File.Move(ProgramConstants.GamePath + VERSION_FILE + "_u", ProgramConstants.GamePath + VERSION_FILE);
                }
                if (File.Exists(ProgramConstants.GamePath + "Theme_c.ini"))
                {
                    Logger.Log("Theme_c.ini exists -- copying it.");
                    File.Copy(ProgramConstants.GamePath + "Theme_c.ini", ProgramConstants.GamePath + "INI/Theme.ini", overwrite: true);
                    Logger.Log("Theme.ini copied succesfully.");
                }
                if (Directory.Exists(ProgramConstants.GamePath + "Updater"))
                {
                    if (File.Exists(ProgramConstants.GamePath + "Updater/clientupdt.dat"))
                    {
                        File.Delete(ProgramConstants.GamePath + "clientupdt.dat");
                        File.Move(ProgramConstants.GamePath + "Updater/clientupdt.dat", ProgramConstants.GamePath + "clientupdt.dat");
                    }
                    Logger.Log("Executing self-updater.");
                    Process process = new Process();
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.FileName = "clientupdt.dat";
                    process.StartInfo.Arguments = NEW_LAUNCHER_NAME + " " + CURRENT_LAUNCHER_NAME;
                    process.Start();
                    CUpdater.Restart?.Invoke(null, EventArgs.Empty);
                }
                else
                {
                    Logger.Log("Update completed succesfully.");
                    TotalDownloadedKbs = 0;
                    UpdateSizeInKb = 0;
                    CheckLocalFileVersions();
                    ServerGameVersion = "N/A";
                    _versionState = VersionState.UPTODATE;
                    DoOnVersionStateChanged();
                    DoUpdateCompleted();
                    if (AreCustomComponentsOutdated())
                    {
                        DoCustomComponentsOutdatedEvent();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("An exception occured during the update. Message: " + ex.Message);
                _versionState = VersionState.UNKNOWN;
                DoOnVersionStateChanged();
                DoOnUpdateFailed(ex);
            }
        }

        private static bool UpdateFile(DTAFileInfo fileInfo)
        {
            Logger.Log("Initiliazing download of file " + fileInfo.Name);
            string text = "Updater/";
            try
            {
                _ = fileInfo.Size;
                string text2 = "";
                int currentUpdateMirrorId = CurrentUpdateMirrorId;
                text2 = GetFilePathForServer(UPDATEMIRRORS[currentUpdateMirrorId].Url + fileInfo.Name);
                CreatePath(ProgramConstants.GamePath + fileInfo.Name);
                CreatePath(ProgramConstants.GamePath + text + fileInfo.Name);
                WebClient webClient = new WebClient();
                webClient.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
                webClient.DownloadProgressChanged += webClient_DownloadProgressChanged;
#if NET48
                webClient.Encoding = Encoding.GetEncoding(1252);
#endif
                Logger.Log("Downloading file " + fileInfo.Name);
                if (fileInfo.Name.ToUpper() != CURRENT_LAUNCHER_NAME.ToUpper())
                {
                    webClient.DownloadFileAsync(new Uri(text2), ProgramConstants.GamePath + text + fileInfo.Name);
                }
                while (webClient.IsBusy)
                {
                    Thread.Sleep(10);
                }
                Logger.Log("Download of file " + fileInfo.Name + " finished - verifying.");
                webClient.CancelAsync();
                webClient.Dispose();
                GC.Collect();
                string empty = string.Empty;
                empty = (ContainsAnyMask(fileInfo.Name) ? fileInfo.Identifier : ((!(fileInfo.Name.ToUpper() == CURRENT_LAUNCHER_NAME.ToUpper())) ? GetUniqueIdForFile(text + fileInfo.Name) : GetUniqueIdForFile(NEW_LAUNCHER_NAME)));
                if (fileInfo.Identifier == empty)
                {
                    Logger.Log("File " + fileInfo.Name + " is intact.");
                    GC.WaitForPendingFinalizers();
                    return true;
                }
                Logger.Log("Downloaded file " + fileInfo.Name + " has a non-matching identifier: " + empty + " against " + fileInfo.Identifier);
                Thread.Sleep(1000);
                try
                {
                    File.Delete(ProgramConstants.GamePath + text + fileInfo.Name);
                }
                catch
                {
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.Log("An error occured while downloading file " + fileInfo.Name + ":" + ex.Message);
                File.Delete(ProgramConstants.GamePath + text + fileInfo.Name);
                return false;
            }
        }

        private static void webClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            UpdateDownloadProgress(e.ProgressPercentage);
        }

        private static void UpdateDownloadProgress(int progressPercentageOfCurrentFile)
        {
            double num = (double)CurrentFileSize * ((double)progressPercentageOfCurrentFile / 100.0);
            double num2 = (double)TotalDownloadedKbs + num;
            int totalPercentage = 0;
            if (UpdateSizeInKb > 0 && UpdateSizeInKb < int.MaxValue)
            {
                totalPercentage = (int)(num2 / (double)UpdateSizeInKb * 100.0);
            }
            DownloadProgressChanged(CurrentFileName, progressPercentageOfCurrentFile, totalPercentage);
        }

        private static bool ExecutePreUpdateexec()
        {
            Logger.Log("Downloading preupdateexec.");
            try
            {
                WebClient webClient = new WebClient();
                webClient.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
                webClient.DownloadProgressChanged += webClient_DownloadProgressChanged;
#if NET48
                webClient.Encoding = Encoding.GetEncoding(1252);
#endif
                webClient.DownloadFile(UPDATEMIRRORS[CurrentUpdateMirrorId].Url + "preupdateexec", ProgramConstants.GamePath + "preupdateexec");
                webClient.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log("Preupdateexec failed! Message: " + ex.Message);
                return false;
            }
            ExecuteScript("preupdateexec");
            return true;
        }

        private static void ExecuteAfterUpdateexec()
        {
            Logger.Log("Downloading updateexec.");
            WebClient webClient = new WebClient();
            webClient.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
            webClient.DownloadProgressChanged += webClient_DownloadProgressChanged;
#if NET48
            webClient.Encoding = Encoding.GetEncoding(1252);
#endif
            webClient.DownloadFile(UPDATEMIRRORS[CurrentUpdateMirrorId].Url + "updateexec", ProgramConstants.GamePath + "updateexec");
            webClient.CancelAsync();
            webClient.Dispose();
            ExecuteScript("updateexec");
        }

        private static void ExecuteScript(string fileName)
        {
            INIReader iNIReader = new INIReader();
            iNIReader.InitINIReader(ProgramConstants.GamePath + fileName);
            Logger.Log("Executing " + fileName);
            do
            {
                iNIReader.ReadNextLine();
                if (!iNIReader.isLineReadable())
                {
                    continue;
                }
                if (iNIReader.CurrentSection == "Delete")
                {
                    Logger.Log(fileName + ": deleting file " + iNIReader.CurrentLine);
                    try
                    {
                        File.Delete(ProgramConstants.GamePath + iNIReader.CurrentLine);
                    }
                    catch
                    {
                    }
                }
                else if (iNIReader.CurrentSection == "Rename")
                {
                    string currentKeyName = iNIReader.getCurrentKeyName();
                    string value = iNIReader.GetValue3();
                    try
                    {
                        Logger.Log(fileName + ": renaming file '" + currentKeyName + "' to '" + value + "'");
                        File.Move(ProgramConstants.GamePath + currentKeyName, ProgramConstants.GamePath + value);
                    }
                    catch
                    {
                    }
                }
                else if (iNIReader.CurrentSection == "RenameFolder")
                {
                    string currentKeyName2 = iNIReader.getCurrentKeyName();
                    string value2 = iNIReader.GetValue3();
                    try
                    {
                        Logger.Log(fileName + ": renaming directory '" + currentKeyName2 + "' to '" + value2 + "'");
                        Directory.Move(ProgramConstants.GamePath + currentKeyName2, ProgramConstants.GamePath + value2);
                    }
                    catch
                    {
                    }
                }
                else if (iNIReader.CurrentSection == "RenameAndMerge")
                {
                    string currentKeyName3 = iNIReader.getCurrentKeyName();
                    string value3 = iNIReader.GetValue3();
                    try
                    {
                        Logger.Log(fileName + ": merging directory '" + currentKeyName3 + "' with '" + value3 + "'");
                        if (!Directory.Exists(ProgramConstants.GamePath + value3))
                        {
                            Logger.Log(fileName + ": destination directory '" + value3 + "' does not exist, renaming.");
                            Directory.Move(ProgramConstants.GamePath + currentKeyName3, ProgramConstants.GamePath + value3);
                            continue;
                        }
                        Logger.Log(fileName + ": destination directory '" + value3 + "' exists, performing selective merging.");
                        string[] files = Directory.GetFiles(ProgramConstants.GamePath + currentKeyName3);
                        for (int i = 0; i < files.Length; i++)
                        {
                            string fileName2 = Path.GetFileName(files[i]);
                            string text = ProgramConstants.GamePath + currentKeyName3 + "\\" + fileName2;
                            string text2 = ProgramConstants.GamePath + value3 + "\\" + fileName2;
                            if (File.Exists(text2))
                            {
                                Logger.Log(fileName + ": destination file '" + value3 + "\\" + fileName2 + "' exists, removing original source file " + currentKeyName3 + "\\" + fileName2);
                                File.Delete(text);
                            }
                            else
                            {
                                Logger.Log(fileName + ": destination file '" + value3 + "\\" + fileName2 + "' does not exist, moving original source file " + currentKeyName3 + "\\" + fileName2);
                                File.Move(text, text2);
                            }
                        }
                    }
                    catch
                    {
                    }
                }
                else if (iNIReader.CurrentSection == "DeleteFolder" || iNIReader.CurrentSection == "ForceDeleteFolder")
                {
                    try
                    {
                        Logger.Log(fileName + ": deleting directory '" + iNIReader.CurrentLine + "'");
                        Directory.Delete(ProgramConstants.GamePath + iNIReader.CurrentLine, recursive: true);
                    }
                    catch
                    {
                    }
                }
                else if (iNIReader.CurrentSection == "DeleteFolderIfEmpty")
                {
                    try
                    {
                        Logger.Log(fileName + ": deleting directory '" + iNIReader.CurrentLine + "' if it's empty.");
                        if (Directory.Exists(iNIReader.CurrentLine))
                        {
                            if (Directory.GetFiles(ProgramConstants.GamePath + iNIReader.CurrentLine).Length == 0)
                            {
                                Directory.Delete(ProgramConstants.GamePath + iNIReader.CurrentLine);
                            }
                            else
                            {
                                Logger.Log(fileName + ": directory '" + iNIReader.CurrentLine + "' is not empty!");
                            }
                        }
                        else
                        {
                            Logger.Log(fileName + ": specified directory does not exist.");
                        }
                    }
                    catch
                    {
                    }
                }
                else
                {
                    if (!(iNIReader.CurrentSection == "CreateFolder"))
                    {
                        continue;
                    }
                    try
                    {
                        string currentLine = iNIReader.CurrentLine;
                        if (!Directory.Exists(ProgramConstants.GamePath + currentLine))
                        {
                            Logger.Log(fileName + ": Creating directory '" + currentLine + "'");
                            Directory.CreateDirectory(ProgramConstants.GamePath + currentLine);
                        }
                        else
                        {
                            Logger.Log(fileName + ": Directory '" + currentLine + "' already exists.");
                        }
                    }
                    catch
                    {
                    }
                }
            }
            while (!iNIReader.ReaderClosed);
            iNIReader.CloseINIReader();
            File.Delete(ProgramConstants.GamePath + fileName);
        }

        private static void CreatePath(string filePath)
        {
            filePath = filePath.Replace('\\', '/');
            int num = filePath.LastIndexOf('/');
            if (num != -1)
            {
                string path = filePath.Substring(0, num);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
        }

        private static string GetFilePathForServer(string filePath)
        {
            return filePath.Replace("\\", "/");
        }

        public static DTAFileInfo ParseFileInfo(string filePath, string versionId)
        {
            int num = versionId.IndexOf(',');
            string identifier = versionId.Substring(0, num);
            int size = Convert.ToInt32(versionId.Substring(num + 1));
            return new DTAFileInfo
            {
                Identifier = identifier,
                Size = size,
                Name = filePath.Replace('\\', '/')
            };
        }

        public static string GetUniqueIdForFile(string filePath)
        {
            MD5 mD = MD5.Create();
            mD.Initialize();
            mD.ComputeHash(new FileStream(ProgramConstants.GamePath + filePath, FileMode.Open, FileAccess.Read));
            StringBuilder stringBuilder = new StringBuilder();
            byte[] hash = mD.Hash;
            foreach (byte b in hash)
            {
                stringBuilder.Append(b.ToString());
            }
            mD.Clear();
            return stringBuilder.ToString();
        }

        public static string TryGetUniqueId(string filePath)
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

        private static bool AreCustomComponentsOutdated()
        {
            Logger.Log("Checking if custom components are outdated.");
            CustomComponent[] customComponents = CustomComponents;
            foreach (CustomComponent customComponent in customComponents)
            {
                if (File.Exists(ProgramConstants.GamePath + customComponent.LocalPath) && customComponent.RemoteIdentifier != customComponent.LocalIdentifier)
                {
                    return true;
                }
            }
            return false;
        }
    }
}