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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ClientUpdater.Compression;
using ClientCore.Extensions;

using Rampastring.Tools;

public static class Updater
{
#if NETFRAMEWORK
    private const string SECOND_STAGE_UPDATER = "SecondStageUpdater.exe";
#else
    private const string SECOND_STAGE_UPDATER = "SecondStageUpdater.dll";
#endif
    private const string LEGACY_SECOND_STAGE_UPDATER = "clientupdt.dat";

    public const string VERSION_FILE = "version";
    public const string ARCHIVE_FILE_EXTENSION = ".lzma";

#if NETFRAMEWORK
    private const string BINARIES_FOLDER = "Binaries";
#else
    private const string BINARIES_FOLDER = "BinariesNET8";
#endif

    /// <summary>
    /// Currently set game path for the updater.
    /// </summary>
    public static string GamePath { get; private set; } = string.Empty;

    /// <summary>
    /// Currently set resource path for the updater.
    /// </summary>
    public static string ResourcePath { get; private set; } = string.Empty;

    /// <summary>
    /// Currently set local game ID for the updater.
    /// </summary>
    public static string LocalGame { get; private set; } = "None";

    /// <summary>
    /// Currently set calling executable file name for the updater.
    /// </summary>
    public static string CallingExecutableFileName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets read-only collection of all custom components.
    /// </summary>
    public static ReadOnlyCollection<CustomComponent> CustomComponents => customComponents?.AsReadOnly();

    /// <summary>
    /// Gets read-only collection of all update mirrors.
    /// </summary>
    public static ReadOnlyCollection<UpdateMirror> UpdateMirrors => updateMirrors?.AsReadOnly();

    /// <summary>
    /// Update server URL for current update mirror if available.
    /// </summary>
    public static string CurrentUpdateServerURL
        => updateMirrors is { Count: > 0 }
            ? updateMirrors[currentUpdateMirrorIndex].URL
            : null;

    private static VersionState _versionState = VersionState.UNKNOWN;

    /// <summary>
    /// Current version state of the updater.
    /// </summary>
    public static VersionState VersionState
    {
        get => _versionState;

        private set
        {
            _versionState = value;
            DoOnVersionStateChanged();
        }
    }

    /// <summary>
    /// Does the currently available update (if applicable) require manual download?
    /// </summary>
    public static bool ManualUpdateRequired { get; private set; }

    /// <summary>
    /// Manual download URL for currently available update, if available.
    /// </summary>
    public static string ManualDownloadURL { get; private set; } = string.Empty;

    /// <summary>
    /// Local version file updater version.
    /// </summary>
    public static string UpdaterVersion { get; private set; } = "N/A";

    /// <summary>
    /// Local version file game version.
    /// </summary>
    public static string GameVersion { get; private set; } = "N/A";

    /// <summary>
    /// Server version file game version.
    /// </summary>
    public static string ServerGameVersion { get; private set; } = "N/A";

    /// <summary>
    /// Size of current update in kilobytes.
    /// </summary>
    public static int UpdateSizeInKb { get; private set; }

    // Misc.
    private static int currentUpdateMirrorIndex;
    private static IniFile settingsINI;
    private static List<CustomComponent> customComponents;
    private static List<UpdateMirror> updateMirrors;
    private static string[] ignoreMasks = [".rtf", ".txt", "Theme.ini", "gui_settings.xml"];

    // File infos.
    private static readonly List<UpdaterFileInfo> FileInfosToDownload = new();
    private static readonly List<UpdaterFileInfo> ServerFileInfos = new();
    private static readonly List<UpdaterFileInfo> LocalFileInfos = new();

#if NETFRAMEWORK
    private static readonly ProgressMessageHandler SharedProgressMessageHandler = new(new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        SslProtocols = System.Security.Authentication.SslProtocols.Tls |
            System.Security.Authentication.SslProtocols.Tls11 |
            System.Security.Authentication.SslProtocols.Tls12 |
            System.Security.Authentication.SslProtocols.Tls13,
    });

    private static readonly HttpClient SharedHttpClient = new(SharedProgressMessageHandler, true);
#else
    private static readonly ProgressMessageHandler SharedProgressMessageHandler = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(15),
        AutomaticDecompression = DecompressionMethods.All
    });

    private static readonly HttpClient SharedHttpClient = new(SharedProgressMessageHandler, true)
    {
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
    };
#endif

    // Current update / download related.
    private static bool terminateUpdate;
    private static string currentFilename;
    private static int currentFileSize;
    private static int totalDownloadedKbs;

    /// <summary>
    /// Initializes the updater.
    /// </summary>
    /// <param name="gamePath">Path of the root client / game folder.</param>
    /// <param name="resourcePath">Path of the resource folder of client / game.</param>
    /// <param name="settingsIniName">Client settings INI filename.</param>
    /// <param name="localGame">Local game ID of the current game.</param>
    /// <param name="callingExecutableFileName">File name of the calling executable.</param>
    public static void Initialize(string gamePath, string resourcePath, string settingsIniName, string localGame, string callingExecutableFileName)
    {
        Logger.Log("Updater: Initializing updater.");

        GamePath = gamePath;
        ResourcePath = resourcePath;
        settingsINI = new(SafePath.CombineFilePath(GamePath, settingsIniName));
        LocalGame = localGame;
        CallingExecutableFileName = callingExecutableFileName;

        ReadUpdaterConfig();

        Logger.Log("Updater: Update mirror count: " + updateMirrors.Count);
        Logger.Log("Updater: Running from: " + CallingExecutableFileName);
        var list = new List<UpdateMirror>();
        List<string> sectionKeys = settingsINI.GetSectionKeys("DownloadMirrors");

        if (sectionKeys != null)
        {
            foreach (string str in sectionKeys)
            {
                string value = settingsINI.GetStringValue("DownloadMirrors", str, string.Empty);

                if (updateMirrors.Any(um => value.Equals(um.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    UpdateMirror item = updateMirrors.Single(um => value.Equals(um.Name, StringComparison.OrdinalIgnoreCase));

                    if (!list.Contains(item))
                        list.Add(item);
                }
            }
        }

        foreach (UpdateMirror mirror2 in updateMirrors)
        {
            if (!list.Contains(mirror2))
                list.Add(mirror2);
        }

        updateMirrors = list;
    }

    /// <summary>
    /// Checks if there are available updates.
    /// </summary>
    public static void CheckForUpdates()
    {
        Logger.Log("Updater: Checking for updates.");
        if (VersionState is not VersionState.UPDATECHECKINPROGRESS and not VersionState.UPDATEINPROGRESS)
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            DoVersionCheckAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }

    /// <summary>
    /// Checks version information of local files.
    /// </summary>
    public static void CheckLocalFileVersions()
    {
        Logger.Log("Updater: Checking local file versions.");

        LocalFileInfos.Clear();

        var file = new IniFile(SafePath.CombineFilePath(GamePath, VERSION_FILE));
        GameVersion = file.GetStringValue("DTA", "Version", "N/A");
        UpdaterVersion = file.GetStringValue("DTA", "UpdaterVersion", "N/A");
        List<string> sectionKeys = file.GetSectionKeys("FileVersions");

        if (sectionKeys != null)
        {
            char[] separator = new char[] { ',' };
            foreach (string str in sectionKeys)
            {
                string[] strArray = file.GetStringListValue("FileVersions", str, string.Empty, separator);
                string[] strArrayArch = file.GetStringListValue("ArchivedFiles", str, string.Empty, separator);
                bool archiveAvailable = strArrayArch is { Length: >= 2 };

                if (strArray.Length >= 2)
                {
                    var item = new UpdaterFileInfo(
                        SafePath.CombineFilePath(str), Conversions.IntFromString(strArray[1], 0))
                    {
                        Identifier = strArray[0],
                        ArchiveIdentifier = archiveAvailable ? strArrayArch[0] : string.Empty,
                        ArchiveSize = archiveAvailable ? Conversions.IntFromString(strArrayArch[1], 0) : 0
                    };

                    LocalFileInfos.Add(item);
                }
                else
                {
                    Logger.Log("Updater: Warning: Malformed file info in local version information: " + str);
                }
            }
        }

        OnLocalFileVersionsChecked?.Invoke();
    }

    /// <summary>
    /// Starts update process.
    /// </summary>
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    public static void StartUpdate() => PerformUpdateAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

    /// <summary>
    /// Stops current update process.
    /// </summary>
    public static void StopUpdate() => terminateUpdate = true;

    /// <summary>
    /// Clears current version file information.
    /// </summary>
    public static void ClearVersionInfo()
    {
        LocalFileInfos.Clear();
        ServerFileInfos.Clear();
        FileInfosToDownload.Clear();
        GameVersion = "N/A";
        VersionState = VersionState.UNKNOWN;
    }

    /// <summary>
    /// Checks if file
    /// </summary>
    public static bool IsFileNonexistantOrOriginal(string filePath)
    {
        UpdaterFileInfo info = LocalFileInfos.Find(f => f.Filename.Equals(filePath, StringComparison.OrdinalIgnoreCase));

        if (info == null)
            return true;

        string uniqueIdForFile = GetUniqueIdForFile(info.Filename);
        return info.Identifier == uniqueIdForFile;
    }

    /// <summary>
    /// Moves update mirror down in list of update mirrors.
    /// </summary>
    /// <param name="mirrorIndex">Index of mirror to move in the list.</param>
    public static void MoveMirrorDown(int mirrorIndex)
    {
        if (mirrorIndex > updateMirrors.Count - 2 || mirrorIndex < 0)
            return;

        (updateMirrors[mirrorIndex], updateMirrors[mirrorIndex + 1]) = (updateMirrors[mirrorIndex + 1], updateMirrors[mirrorIndex]);
    }

    /// <summary>
    /// Moves update mirror up in list of update mirrors.
    /// </summary>
    /// <param name="mirrorIndex">Index of mirror to move in the list.</param>
    public static void MoveMirrorUp(int mirrorIndex)
    {
        if (updateMirrors.Count <= mirrorIndex || mirrorIndex < 1)
            return;

        (updateMirrors[mirrorIndex], updateMirrors[mirrorIndex - 1]) = (updateMirrors[mirrorIndex - 1], updateMirrors[mirrorIndex]);
    }

    /// <summary>
    /// Returns whether or not there is a currently active custom component in progress.
    /// </summary>
    /// <returns>True if custom component download is in progress, otherwise false.</returns>
    public static bool IsComponentDownloadInProgress()
    {
        if (customComponents == null)
            return false;

        return customComponents.Any(c => c.IsBeingDownloaded);
    }

    /// <summary>
    /// Gets custom component index based on name.
    /// </summary>
    /// <param name="componentName">Name of custom component.</param>
    /// <returns>Component index if found, otherwise -1.</returns>
    public static int GetComponentIndex(string componentName)
    {
        if (customComponents == null)
            return -1;

        return customComponents.FindIndex(c => c.ININame == componentName);
    }

    /// <summary>
    /// Get archive info for a file from version file.
    /// </summary>
    /// <param name="versionFile">Version file.</param>
    /// <param name="filename">Filename.</param>
    /// <param name="archiveID">Set to archive ID.</param>
    /// <param name="archiveSize">Set to archive file size.</param>
    internal static void GetArchiveInfo(IniFile versionFile, string filename, out string archiveID, out int archiveSize)
    {
        string[] values = versionFile.GetStringValue("ArchivedFiles", filename, string.Empty).Split(',');
        bool archiveAvailable = values is { Length: >= 2 };
        archiveID = archiveAvailable ? values[0] : string.Empty;
        archiveSize = archiveAvailable ? Conversions.IntFromString(values[1], 0) : 0;
    }

    /// <summary>
    /// Creates file info instance from given information.
    /// </summary>
    /// <param name="filename">Filename.</param>
    /// <param name="identifier">File identifier.</param>
    /// <param name="size">File size.</param>
    /// <param name="archiveIdentifier">Archive file identifier.</param>
    /// <param name="archiveSize">Archive file size.</param>
    internal static UpdaterFileInfo CreateFileInfo(string filename, string identifier, int size, string archiveIdentifier = null, int archiveSize = 0)
    {
        return new(SafePath.CombineFilePath(filename), size)
        {
            Identifier = identifier,
            ArchiveIdentifier = archiveIdentifier,
            ArchiveSize = archiveSize
        };
    }

    internal static void UpdateUserAgent(HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.UserAgent.Clear();

        if (GameVersion != "N/A")
            httpClient.DefaultRequestHeaders.UserAgent.Add(new(LocalGame.Replace(' ', '-'), GameVersion.Replace(' ', '-')));

        if (UpdaterVersion != "N/A")
            httpClient.DefaultRequestHeaders.UserAgent.Add(new(nameof(Updater), UpdaterVersion.Replace(' ', '-')));

        httpClient.DefaultRequestHeaders.UserAgent.Add(new("Client", GitVersionInformation.AssemblySemVer));
    }

    /// <summary>
    /// Deletes file and waits until it has been deleted.
    /// </summary>
    /// <param name="filepath">File to delete.</param>
    /// <param name="timeout">Maximum time to wait in milliseconds.</param>
    internal static void DeleteFileAndWait(string filepath, int timeout = 10000)
    {
        FileInfo fileInfo = SafePath.GetFile(filepath);
        using var fw = new FileSystemWatcher(fileInfo.DirectoryName, fileInfo.Name);
        using var mre = new ManualResetEventSlim();

        fw.EnableRaisingEvents = true;
        fw.Deleted += (_, _) =>
        {
            mre.Set();
        };
        if (fileInfo.Exists)
            fileInfo.IsReadOnly = false;
        fileInfo.Delete();
        mre.Wait(timeout);
    }

    /// <summary>
    /// Creates all directories required for file path.
    /// </summary>
    /// <param name="filePath">File path.</param>
    internal static void CreatePath(string filePath)
    {
        FileInfo fileInfo = SafePath.GetFile(filePath);

        if (!fileInfo.Directory.Exists)
            fileInfo.Directory.Create();
    }

    internal static string GetUniqueIdForFile(string filePath)
    {
        using var md = MD5.Create();
        md.Initialize();
        using FileStream fs = SafePath.GetFile(GamePath, filePath).OpenRead();
        md.ComputeHash(fs);
        var builder = new StringBuilder();

        foreach (byte num2 in md.Hash)
            builder.Append(num2);

        md.Clear();
        return builder.ToString();
    }

    /// <summary>
    /// Parse updater configuration file.
    /// </summary>
    private static void ReadUpdaterConfig()
    {
        var mirrors = new List<UpdateMirror>();
        var customComponents = new List<CustomComponent>();

        FileInfo configFile = SafePath.GetFile(ResourcePath, "UpdaterConfig.ini");

        if (!configFile.Exists)
        {
            Logger.Log("Updater config file not found - attempting to read legacy updateconfig.ini.");
            ReadLegacyUpdaterConfig(mirrors);
        }
        else
        {
            var updaterConfig = new IniFile(configFile.FullName);
            string maskString = updaterConfig.GetStringValue("Settings", "IgnoreMasks", string.Join(",", ignoreMasks));
            ignoreMasks = maskString.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            List<string> keys = updaterConfig.GetSectionKeys("DownloadMirrors");

            if (keys != null)
            {
                foreach (string key in keys)
                {
                    if (string.IsNullOrEmpty(key))
                        continue;

                    string stringValue = updaterConfig.GetStringValue("DownloadMirrors", key, string.Empty);

                    if (string.IsNullOrEmpty(stringValue))
                        stringValue = key;

                    string[] values = stringValue.Split(',');

                    if (values.Length < 2)
                        continue;
                    if (mirrors.FindIndex(i => i.URL == values[0].Trim()) < 0)
                        mirrors.Add(new(values[0].Trim(), values[1].Trim(), values.Length > 2 ? values[2].Trim() : string.Empty));
                }
            }

            keys = updaterConfig.GetSectionKeys("CustomComponents");

            if (keys != null)
            {
                foreach (string key in keys)
                {
                    if (string.IsNullOrEmpty(key))
                        continue;

                    string stringValue = updaterConfig.GetStringValue("CustomComponents", key, string.Empty);

                    if (string.IsNullOrEmpty(stringValue))
                        stringValue = key;

                    string[] values = stringValue.Split(',');

                    if (values.Length < 4)
                        continue;

                    string ID = values[1].Trim();

                    if (customComponents.FindIndex(i => i.ININame == ID) < 0)
                    {
                        string Name = values[0].Trim();
                        string DownloadPath = values[2].Trim();
                        string LocalPath = values[3].Trim();
                        bool noArchiveExtensionForDownloadPath = false;

                        if (values.Length > 4)
                            noArchiveExtensionForDownloadPath = Conversions.BooleanFromString(values[4], false);

                        bool DownloadPathIsAbsolute = Uri.IsWellFormedUriString(DownloadPath, UriKind.Absolute);
                        customComponents.Add(new(Name, ID, DownloadPath, LocalPath, DownloadPathIsAbsolute, noArchiveExtensionForDownloadPath));
                    }
                }
            }
        }

        updateMirrors = mirrors;
        Updater.customComponents = customComponents;

        if (updateMirrors.Count < 1)
            Logger.Log("Warning: No download mirrors found in updater config file or the built-in game info.");
    }

    /// <summary>
    /// Parse legacy format updater configuration file.
    /// </summary>
    /// <param name="updateMirrors">List of update mirrors to add update mirrors to.</param>
    private static void ReadLegacyUpdaterConfig(List<UpdateMirror> updateMirrors)
    {
        FileInfo updateConfigFile = SafePath.GetFile(GamePath, "updateconfig.ini");

        if (!updateConfigFile.Exists)
            return;

        string[] lines;

        try
        {
            lines = File.ReadAllLines(updateConfigFile.FullName);
        }
        catch (Exception e)
        {
            Logger.Log("Error: Could not read legacy format updateconfig.ini. Message:" + e.Message);
            return;
        }

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith(';'))
                continue;

            string[] array = line.Split(new char[] { ',' });

            if (array.Length < 3)
                continue;

            string url = array[0].Trim();
            string name = array[1].Trim();
            string location = array[2].Trim();
            updateMirrors.Add(new(url, name, location));
        }
    }

    /// <summary>
    /// Performs a version file check on update server.
    /// </summary>
    private static async Task DoVersionCheckAsync()
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
            }
            else
            {
                Logger.Log("Updater: Checking version on the server.");

                UpdateUserAgent(SharedHttpClient);

                FileInfo versionFile = SafePath.GetFile(GamePath, FormattableString.Invariant($"{VERSION_FILE}_u"));

                while (currentUpdateMirrorIndex < updateMirrors.Count)
                {
                    try
                    {
                        Logger.Log("Updater: Trying to connect to update mirror " + updateMirrors[currentUpdateMirrorIndex].URL);

                        FileStream fileStream = new FileStream(versionFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);

                        using (fileStream)
                        {
                            Stream stream = await SharedHttpClient.GetStreamAsync(updateMirrors[currentUpdateMirrorIndex].URL + VERSION_FILE).ConfigureAwait(false);

                            using (stream)
                            {
                                await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                            }
                        }

                        break;
                    }
                    catch (Exception e)
                    {
                        Logger.Log("Updater: Error connecting to update mirror. Error message: " + e.ToString());
                        Logger.Log("Updater: Seeking other mirrors...");
                        currentUpdateMirrorIndex++;

                        if (currentUpdateMirrorIndex >= updateMirrors.Count)
                        {
                            currentUpdateMirrorIndex = 0;
                            throw new("Unable to connect to update servers.");
                        }
                    }
                }

                Logger.Log("Updater: Downloaded version information.");
                var version = new IniFile(versionFile.FullName);
                string versionString = version.GetStringValue("DTA", "Version", string.Empty);
                string updaterVersionString = version.GetStringValue("DTA", "UpdaterVersion", "N/A");
                string manualDownloadURLString = version.GetStringValue("DTA", "ManualDownloadURL", string.Empty);

                if (version.SectionExists("FileVersions"))
                {
                    foreach (string key in version.GetSectionKeys("FileVersions"))
                    {
                        string[] tmp = version.GetStringValue("FileVersions", key, string.Empty).Split(',');

                        if (tmp.Length < 2)
                        {
                            Logger.Log("Updater: Warning: Malformed file info in downloaded version information: " + key);
                            continue;
                        }

                        GetArchiveInfo(version, key, out string archiveID, out int archiveSize);
                        UpdaterFileInfo item = CreateFileInfo(key, tmp[0], Conversions.IntFromString(tmp[1], 0), archiveID, archiveSize);
                        ServerFileInfos.Add(item);
                    }
                }

                if (version.SectionExists("AddOns"))
                {
                    foreach (string key in version.GetSectionKeys("AddOns"))
                    {
                        string[] tmp = version.GetStringValue("AddOns", key, string.Empty).Split(',');

                        if (tmp.Length < 2)
                        {
                            Logger.Log("Updater: Warning: Malformed addon info in downloaded version information: " + key);
                            continue;
                        }

                        UpdaterFileInfo item = CreateFileInfo(key, tmp[0], Conversions.IntFromString(tmp[1], 0), string.Empty, 0);
                        int index = GetComponentIndex(key);

                        if (index == -1)
                        {
                            Logger.Log("Updater: Warning: Invalid custom component ID " + key);
                        }
                        else
                        {
                            CustomComponent component = customComponents[index];
                            component.Initialized = false;
                            Logger.Log("Updater: Setting custom component info for " + key);
                            GetArchiveInfo(version, component.LocalPath, out string archiveID, out int archiveSize);
                            item.ArchiveIdentifier = archiveID;
                            item.ArchiveSize = archiveSize;
                            component.RemoteSize = item.Size * 1024;
                            component.RemoteArchiveSize = item.Archived ? item.ArchiveSize * 1024 : 0;
                            component.RemoteIdentifier = item.Identifier;
                            component.Archived = item.Archived;

                            if (SafePath.GetFile(GamePath, component.LocalPath).Exists)
                                component.LocalIdentifier = GetUniqueIdForFile(component.LocalPath);

                            component.Initialized = true;
                        }
                    }
                }

                if (string.IsNullOrEmpty(versionString))
                    throw new("Update server integrity error while checking for updates.");

                Logger.Log("Updater: Server game version is " + versionString + ", local version is " + GameVersion);
                ServerGameVersion = versionString;

                if (versionString == GameVersion)
                {
                    VersionState = VersionState.UPTODATE;
                    versionFile.Delete();
                    DoFileIdentifiersUpdatedEvent();

                    if (AreCustomComponentsOutdated())
                        DoCustomComponentsOutdatedEvent();
                }
                else
                {
                    if (updaterVersionString != "N/A" && UpdaterVersion != updaterVersionString)
                    {
                        Logger.Log("Updater: Server update system version is set to " + updaterVersionString + " and is different to local update system version " + UpdaterVersion + ". Manual update required.");
                        VersionState = VersionState.OUTDATED;
                        ManualUpdateRequired = true;
                        ManualDownloadURL = manualDownloadURLString;
                        versionFile.Delete();
                        DoFileIdentifiersUpdatedEvent();
                    }
                    else
                    {
                        VersionCheckHandle();
                    }
                }
            }
        }
        catch (Exception exception)
        {
            VersionState = VersionState.UNKNOWN;
            Logger.Log("Updater: An error occured while performing version check: " + exception.Message);
            DoFileIdentifiersUpdatedEvent();
        }
    }

    /// <summary>
    /// Checks if custom components are outdated.
    /// </summary>
    /// <returns>True if custom components are outdated, otherwise false.</returns>
    private static bool AreCustomComponentsOutdated()
    {
        Logger.Log("Updater: Checking if custom components are outdated.");
        foreach (CustomComponent component in customComponents)
        {
            if (SafePath.GetFile(GamePath, component.LocalPath).Exists && component.RemoteIdentifier != component.LocalIdentifier)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Executes after-update script file.
    /// </summary>
    private static async ValueTask ExecuteAfterUpdateScriptAsync()
    {
        Logger.Log("Updater: Downloading updateexec.");
        try
        {
            string downloadFile = SafePath.CombineFilePath(GamePath, "updateexec");

            FileStream fileStream = new FileStream(downloadFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);

            using (fileStream)
            {
                Stream stream = await SharedHttpClient.GetStreamAsync(updateMirrors[currentUpdateMirrorIndex].URL + "updateexec").ConfigureAwait(false);

                using (stream)
                {
                    await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                }
            }
        }
        catch (Exception exception)
        {
            Logger.Log("Updater: Warning: Downloading updateexec failed: " + exception.Message);
            return;
        }

        ExecuteScript("updateexec");
    }

    /// <summary>
    /// Executes pre-update script file.
    /// </summary>
    /// <returns>True if succesful, otherwise false.</returns>
    private static async ValueTask<bool> ExecutePreUpdateScriptAsync()
    {
        Logger.Log("Updater: Downloading preupdateexec.");
        try
        {
            string downloadFile = SafePath.CombineFilePath(GamePath, "preupdateexec");

            FileStream fileStream = new FileStream(downloadFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);

            using (fileStream)
            {
                Stream stream = await SharedHttpClient.GetStreamAsync(updateMirrors[currentUpdateMirrorIndex].URL + "preupdateexec").ConfigureAwait(false);

                using (stream)
                {
                    await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                }
            }
        }
        catch (Exception exception)
        {
            Logger.Log("Updater: Warning: Downloading preupdateexec failed: " + exception.Message);
            return false;
        }

        ExecuteScript("preupdateexec");
        return true;
    }

    /// <summary>
    /// Executes a script file.
    /// </summary>
    /// <param name="fileName">Filename of the script file.</param>
    private static void ExecuteScript(string fileName)
    {
        Logger.Log("Updater: Executing " + fileName + ".");
        FileInfo scriptFileInfo = SafePath.GetFile(GamePath, fileName);
        var script = new IniFile(scriptFileInfo.FullName);

        // Delete files.
        foreach (string key in GetKeys(script, "Delete"))
        {
            Logger.Log("Updater: " + fileName + ": Deleting file " + key);

            try
            {
                FileInfo fileInfo = SafePath.GetFile(GamePath, key);

                if (fileInfo.Exists)
                {
                    fileInfo.IsReadOnly = false;
                    fileInfo.Delete();
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Updater: " + fileName + ": Deleting file " + key + "failed: " + ex.Message);
            }
        }

        // Rename files.
        foreach (string key in GetKeys(script, "Rename"))
        {
            string newFilename = SafePath.CombineFilePath(script.GetStringValue("Rename", key, string.Empty));
            if (string.IsNullOrWhiteSpace(newFilename))
                continue;
            try
            {
                Logger.Log("Updater: " + fileName + ": Renaming file '" + key + "' to '" + newFilename + "'");

                FileInfo srcFile = SafePath.GetFile(GamePath, key);

                if (srcFile.Exists)
                {
                    bool isSrcReadOnly = srcFile.IsReadOnly;
                    srcFile.IsReadOnly = false;

                    {
                        FileInfo destFile = SafePath.GetFile(GamePath, newFilename);
                        if (destFile.Exists)
                        {
                            destFile.IsReadOnly = false;
                            destFile.Delete();
                        }
                    }

                    srcFile.MoveTo(SafePath.CombineFilePath(GamePath, newFilename));

                    if (isSrcReadOnly)
                    {
                        FileInfo destFile = SafePath.GetFile(GamePath, newFilename);
                        destFile.IsReadOnly = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Updater: " + fileName + ": Renaming file '" + key + "' to '" + newFilename + "' failed: " + ex.Message);
            }
        }

        // Rename folders.
        foreach (string key in GetKeys(script, "RenameFolder"))
        {
            string newDirectoryName = script.GetStringValue("RenameFolder", key, string.Empty);
            if (string.IsNullOrWhiteSpace(newDirectoryName))
                continue;
            try
            {
                Logger.Log("Updater: " + fileName + ": Renaming directory '" + key + "' to '" + newDirectoryName + "'");

                DirectoryInfo srcDirectory = SafePath.GetDirectory(GamePath, key);

                if (srcDirectory.Exists)
                    srcDirectory.MoveTo(SafePath.CombineDirectoryPath(GamePath, newDirectoryName));
            }
            catch (Exception ex)
            {
                Logger.Log("Updater: " + fileName + ": Renaming directory '" + key + "' to '" + newDirectoryName + "' failed: " + ex.Message);
            }
        }

        // Rename & merge files / folders.
        foreach (string key in GetKeys(script, "RenameAndMerge"))
        {
            string directoryName = key;
            string directoryNameToMergeInto = script.GetStringValue("RenameAndMerge", key, string.Empty);
            if (string.IsNullOrWhiteSpace(directoryNameToMergeInto))
                continue;
            try
            {
                Logger.Log("Updater: " + fileName + ": Merging directory '" + directoryName + "' with '" + directoryNameToMergeInto + "'");
                DirectoryInfo directoryToMergeInto = SafePath.GetDirectory(GamePath, directoryNameToMergeInto);
                DirectoryInfo gameDirectory = SafePath.GetDirectory(GamePath, directoryName);

                if (!gameDirectory.Exists)
                    continue;

                if (!directoryToMergeInto.Exists)
                {
                    Logger.Log("Updater: " + fileName + ": Destination directory '" + directoryNameToMergeInto + "' does not exist, renaming.");
                    gameDirectory.MoveTo(directoryToMergeInto.FullName);
                }
                else
                {
                    Logger.Log("Updater: " + fileName + ": Destination directory '" + directoryNameToMergeInto + "' exists, performing selective merging.");
                    FileInfo[] files = gameDirectory.GetFiles();
                    foreach (FileInfo file in files)
                    {
                        bool isSrcReadOnly = file.IsReadOnly;
                        file.IsReadOnly = false;

                        FileInfo fileToMergeInto = SafePath.GetFile(directoryToMergeInto.FullName, file.Name);
                        if (fileToMergeInto.Exists)
                        {
                            Logger.Log("Updater: " + fileName + ": Destination file '" + directoryNameToMergeInto + "/" + file.Name +
                                "' exists, removing original source file " + directoryName + "/" + file.Name);

                            // Note: Previously, the incorrect file was deleted as of commit fc939a06ff978b51daa6563eaa15a28cf48319ec.

                            // Remove the original source file
                            file.Delete();
                        }
                        else
                        {
                            Logger.Log("Updater: " + fileName + ": Destination file '" + directoryNameToMergeInto + "/" + file.Name +
                                "' does not exist, moving original source file " + directoryName + "/" + file.Name);
                            file.MoveTo(fileToMergeInto.FullName);

                            // Resume the read-only property
                            fileToMergeInto.Refresh();
                            fileToMergeInto.IsReadOnly = isSrcReadOnly;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Updater: " + fileName + ": Merging directory '" + directoryName + "' with '" + directoryNameToMergeInto + "' failed: " + ex.Message);
            }
        }

        // Delete folders.
        foreach (string sectionName in new string[] { "DeleteFolder", "ForceDeleteFolder" })
        {
            foreach (string key in GetKeys(script, sectionName))
            {
                try
                {
                    Logger.Log("Updater: " + fileName + ": Deleting directory '" + key + "'");

                    DirectoryInfo directoryInfo = SafePath.GetDirectory(GamePath, key);
                    if (directoryInfo.Exists)
                    {
                        // Unset read-only attribute from all files in the directory.
                        foreach (FileInfo file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
                        {
                            file.IsReadOnly = false;
                        }

                        directoryInfo.Delete(true);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log("Updater: " + fileName + ": Deleting directory '" + key + "' failed: " + ex.Message);
                }
            }
        }

        // Delete folders, if empty.
        foreach (string key in GetKeys(script, "DeleteFolderIfEmpty"))
        {
            try
            {
                Logger.Log("Updater: " + fileName + ": Deleting directory '" + key + "' if it's empty.");

                DirectoryInfo directoryInfo = SafePath.GetDirectory(GamePath, key);

                if (directoryInfo.Exists)
                {
                    if (!directoryInfo.EnumerateFiles().Any())
                    {
                        directoryInfo.Delete();
                    }
                    else
                    {
                        Logger.Log("Updater: " + fileName + ": Directory '" + key + "' is not empty!");
                    }
                }
                else
                {
                    Logger.Log("Updater: " + fileName + ": Specified directory does not exist.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Updater: " + fileName + ": Deleting directory '" + key + "' if it's empty failed: " + ex.Message);
            }
        }

        // Create folders.
        foreach (string key in GetKeys(script, "CreateFolder"))
        {
            try
            {
                DirectoryInfo directoryInfo = SafePath.GetDirectory(GamePath, key);
                if (!directoryInfo.Exists)
                {
                    Logger.Log("Updater: " + fileName + ": Creating directory '" + key + "'");
                    directoryInfo.Create();
                }
                else
                {
                    Logger.Log("Updater: " + fileName + ": Directory '" + key + "' already exists.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Updater: " + fileName + ": Creating directory '" + key + "' failed: " + ex.Message);
            }
        }

        scriptFileInfo.Delete();
    }

    /// <summary>
    /// Handle version check.
    /// </summary>
    private static void VersionCheckHandle()
    {
        Logger.Log("Updater: Gathering list of files to be downloaded. Server file info count: " + ServerFileInfos.Count);

        FileInfosToDownload.Clear();

        for (int i = 0; i < ServerFileInfos.Count; i++)
        {
            string identifier = ServerFileInfos[i].Identifier;
            bool flag = false;
            FileInfo serverFileInfo = SafePath.GetFile(GamePath, ServerFileInfos[i].Filename);

            for (int k = 0; k < LocalFileInfos.Count; k++)
            {
                UpdaterFileInfo info = LocalFileInfos[k];

                if (ServerFileInfos[i].Filename == info.Filename)
                {
                    flag = true;

                    if (!serverFileInfo.Exists)
                    {
                        Logger.Log("Updater: File " + ServerFileInfos[i].Filename + " not found. Adding it to the download queue.");
                        FileInfosToDownload.Add(ServerFileInfos[i]);
                    }
                    else if (info.Identifier != identifier)
                    {
                        Logger.Log("Updater: Local file " + info.Filename + " is different, adding it to the download queue.");
                        FileInfosToDownload.Add(ServerFileInfos[i]);
                    }
                }
            }

            if (!flag)
            {
                Logger.Log("Updater: File " + ServerFileInfos[i].Filename + " doesn't exist on local version information - checking if it exists in the directory.");

                if (serverFileInfo.Exists)
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
        }

        UpdateSizeInKb = 0;

        for (int j = 0; j < FileInfosToDownload.Count; j++)
        {
            UpdateSizeInKb += FileInfosToDownload[j].Archived ? FileInfosToDownload[j].ArchiveSize : FileInfosToDownload[j].Size;
        }

        VersionState = VersionState.OUTDATED;
        ManualUpdateRequired = false;
        DoFileIdentifiersUpdatedEvent();
    }

    /// <summary>
    /// Verifies local file version info.
    /// </summary>
    private static void VerifyLocalFileVersions()
    {
        Logger.Log("Verifying local file versions. Count: " + LocalFileInfos.Count);
        for (int i = 0; i < LocalFileInfos.Count; i++)
        {
            UpdaterFileInfo info = LocalFileInfos[i];
            if (!ContainsAnyMask(info.Filename))
            {
                if (SafePath.GetFile(GamePath, info.Filename).Exists)
                {
                    string uniqueIdForFile = GetUniqueIdForFile(info.Filename);
                    if (uniqueIdForFile != info.Identifier)
                    {
                        Logger.Log("Invalid unique identifier for " + info.Filename + "!");
                        info.Identifier = uniqueIdForFile;
                    }
                }
                else
                {
                    Logger.Log("File " + info.Filename + " does not exist!");
                    LocalFileInfos.RemoveAt(i);
                    i--;
                }

                if (LocalFileInfos.Count > 0)
                    LocalFileCheckProgressChanged?.Invoke(i + 1, LocalFileInfos.Count);
            }
        }
    }

    /// <summary>
    /// Downloads files required for update and starts second-stage updater.
    /// </summary>
    private static async Task PerformUpdateAsync()
    {
        Logger.Log("Updater: Starting update.");
        VersionState = VersionState.UPDATEINPROGRESS;

        try
        {
            UpdateUserAgent(SharedHttpClient);

            SharedProgressMessageHandler.HttpReceiveProgress += ProgressMessageHandlerOnHttpReceiveProgress;

            if (!await ExecutePreUpdateScriptAsync().ConfigureAwait(false))
                throw new("Executing preupdateexec failed.");

            VerifyLocalFileVersions();
            VersionCheckHandle();

            if (string.IsNullOrEmpty(ServerGameVersion) || ServerGameVersion == "N/A" || VersionState != VersionState.OUTDATED)
                throw new("Update server integrity error.");

            VersionState = VersionState.UPDATEINPROGRESS;

            totalDownloadedKbs = 0;

            if (terminateUpdate)
            {
                Logger.Log("Updater: Terminating update because of user request.");
                VersionState = VersionState.OUTDATED;
                ManualUpdateRequired = false;
                terminateUpdate = false;
            }
            else
            {
                foreach (UpdaterFileInfo info in FileInfosToDownload)
                {
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
                        currentFilename = info.Archived ? info.Filename + ARCHIVE_FILE_EXTENSION : info.Filename;
                        currentFileSize = info.Archived ? info.ArchiveSize : info.Size;
                        string errorMessage = await DownloadFileAsync(info).ConfigureAwait(false);

                        if (terminateUpdate)
                        {
                            Logger.Log("Updater: Terminating update because of user request.");
                            VersionState = VersionState.OUTDATED;
                            ManualUpdateRequired = false;
                            terminateUpdate = false;
                            return;
                        }

                        if (errorMessage is null)
                        {
                            totalDownloadedKbs += info.Archived ? info.ArchiveSize : info.Size;
                            break;
                        }

                        num++;

                        if (num == 2)
                        {
                            Logger.Log("Updater: Too many retries for downloading file " +
                                (info.Archived ? info.Filename + ARCHIVE_FILE_EXTENSION : info.Filename) + ". Update halted.");

                            string extraMsg = Environment.NewLine + Environment.NewLine + "Download error message: " + errorMessage;

                            throw new("Too many retries for downloading file " +
                                      (info.Archived ? info.Filename + ARCHIVE_FILE_EXTENSION : info.Filename) + extraMsg);
                        }
                    }
                }

                if (terminateUpdate)
                {
                    Logger.Log("Updater: Terminating update because of user request.");
                    VersionState = VersionState.OUTDATED;
                    ManualUpdateRequired = false;
                    terminateUpdate = false;
                }
                else
                {
                    Logger.Log("Updater: Downloading files finished - copying from temporary updater directory.");
                    await ExecuteAfterUpdateScriptAsync().ConfigureAwait(false);
                    Logger.Log("Updater: Cleaning up.");

                    // this folder contains incoming files that needs to be updated by second stage updater
                    DirectoryInfo incomingDirectoryInfo = SafePath.GetDirectory(GamePath, "Updater");
                    FileInfo versionFile = SafePath.GetFile(GamePath, VERSION_FILE);
                    FileInfo versionFileTemp = SafePath.GetFile(GamePath, FormattableString.Invariant($"{VERSION_FILE}_u"));

                    if (incomingDirectoryInfo.Exists)
                    {
                        versionFileTemp.MoveTo(SafePath.CombineFilePath(incomingDirectoryInfo.FullName, VERSION_FILE));

                        // make sure the existing version file do not exist, to make the legacy "clientupdt.exe" second stage updater happy
                        SafePath.DeleteFileIfExists(versionFile.FullName);
                    }
                    else
                    {
                        // since second stage updater will not be launched, just override the existing version file
                        SafePath.DeleteFileIfExists(versionFile.FullName);
                        versionFileTemp.MoveTo(versionFile.FullName);
                    }

                    FileInfo themeFileInfo = SafePath.GetFile(GamePath, "Theme_c.ini");

                    if (themeFileInfo.Exists)
                    {
                        Logger.Log("Updater: Theme_c.ini exists -- copying it.");
                        themeFileInfo.CopyTo(SafePath.CombineFilePath(GamePath, "INI", "Theme.ini"), true);
                        Logger.Log("Updater: Theme.ini copied successfully.");
                    }

                    incomingDirectoryInfo.Refresh();

                    if (incomingDirectoryInfo.Exists)
                    {
                        // update legacy second stage updater
                        DirectoryInfo currentLegacySecondStageUpdaterDirectory = SafePath.GetDirectory(GamePath);
                        FileInfo currentLegacySecondStageUpdaterExecutable = SafePath.GetFile(currentLegacySecondStageUpdaterDirectory.FullName, LEGACY_SECOND_STAGE_UPDATER);
                        DirectoryInfo incomingLegacySecondStageUpdaterDirectory = SafePath.GetDirectory(incomingDirectoryInfo.FullName);
                        FileInfo incomingLegacySecondStageUpdaterExecutable = SafePath.GetFile(incomingLegacySecondStageUpdaterDirectory.FullName, LEGACY_SECOND_STAGE_UPDATER);
                        if (incomingLegacySecondStageUpdaterExecutable.Exists)
                        {
                            SafePath.DeleteFileIfExists(currentLegacySecondStageUpdaterExecutable.FullName);
                            incomingLegacySecondStageUpdaterExecutable.MoveTo(currentLegacySecondStageUpdaterExecutable.FullName);
                            currentLegacySecondStageUpdaterExecutable.Refresh();
                        }

                        #region update-second-stage-updater

                        // the second stage updater is placed at "Resources\Binaries\Updater" directory.
                        DirectoryInfo currentSecondStageUpdaterDirectory = SafePath.GetDirectory(ResourcePath, BINARIES_FOLDER, "Updater");
                        if (!currentSecondStageUpdaterDirectory.Exists)
                            currentSecondStageUpdaterDirectory.Create();

                        FileInfo secondStageUpdaterExecutable = SafePath.GetFile(currentSecondStageUpdaterDirectory.FullName, SECOND_STAGE_UPDATER);

                        // update the new second stage updater before other files
                        DirectoryInfo incomingSecondStageUpdaterDirectory = SafePath.GetDirectory(incomingDirectoryInfo.FullName, "Resources", BINARIES_FOLDER, "Updater");
                        if (incomingSecondStageUpdaterDirectory.Exists)
                        {
                            Logger.Log("Updater: Checking & moving second-stage updater files.");

                            // copy SecondStageUpdater
                            IEnumerable<FileInfo> updaterFiles = incomingSecondStageUpdaterDirectory.EnumerateFiles(Path.GetFileNameWithoutExtension(SECOND_STAGE_UPDATER) + ".*");

                            foreach (FileInfo updaterFile in updaterFiles)
                            {
                                FileInfo updaterFileResource = SafePath.GetFile(currentSecondStageUpdaterDirectory.FullName, updaterFile.Name);

                                Logger.Log("Updater: Moving second-stage updater file " + updaterFile.Name + ".");

                                SafePath.DeleteFileIfExists(updaterFileResource.FullName);
                                updaterFile.MoveTo(updaterFileResource.FullName);
                            }

                            // copy SecondStageUpdater dependencies
                            // warning: for unknown reasons, `System.Runtime.CompilerServices.Unsafe.dll` file is not listed here.
                            // Therefore, Polyfill (requiring this dll file) is excluded from the second-stage updater.
                            AssemblyName[] assemblies = Assembly.LoadFrom(secondStageUpdaterExecutable.FullName).GetReferencedAssemblies();

                            foreach (AssemblyName assembly in assemblies)
                            {
                                FileInfo incomingAssemblyFile = SafePath.GetFile(incomingSecondStageUpdaterDirectory.FullName, FormattableString.Invariant($"{assembly.Name}.dll"));

                                if (!incomingAssemblyFile.Exists)
                                {
                                    Logger.Log("Updater: Missing assembly file required by second-stage updater: " + incomingAssemblyFile.Name + ".");
                                    continue;
                                }

                                FileInfo currentAssemblyFile = SafePath.GetFile(currentSecondStageUpdaterDirectory.FullName, incomingAssemblyFile.Name);

                                Logger.Log("Updater: Moving second-stage updater file " + incomingAssemblyFile.Name + ".");

                                SafePath.DeleteFileIfExists(currentAssemblyFile.FullName);
                                incomingAssemblyFile.MoveTo(currentAssemblyFile.FullName);
                            }
                        }
                        #endregion

                        Logger.Log("Updater: Launching second-stage updater executable " + secondStageUpdaterExecutable.FullName + ".");

                        // fallback to the old "clientupdt.dat" file if the new second-stage updater does not exist
                        bool runNativeWindowsExe = true;
#if !NETFRAMEWORK
                        runNativeWindowsExe = false;
#endif

                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !secondStageUpdaterExecutable.Exists)
                        {
                            Logger.Log("Updater: Missing second-stage updater executable " + secondStageUpdaterExecutable.FullName + ".");
                            if (currentLegacySecondStageUpdaterExecutable.Exists)
                            {
                                Logger.Log("Updater: Falling back to legacy second-stage updater executable " + currentLegacySecondStageUpdaterExecutable.FullName + ".");
                                secondStageUpdaterExecutable = currentLegacySecondStageUpdaterExecutable;
                                runNativeWindowsExe = true;
                            }
                        }

                        ProcessStartInfo secondStageUpdaterStartInfo;
                        if (runNativeWindowsExe)
                        {
                            // e.g. C:\Game\Resources\SecondStageUpdater.exe clientogl.exe "C:\Game\"
                            secondStageUpdaterStartInfo = new ProcessStartInfo
                            {
                                FileName = secondStageUpdaterExecutable.FullName,
                                Arguments = CallingExecutableFileName + " \"" + GamePath + "\"",
                                UseShellExecute = false,
                            };
                        }
                        else
                        {
                            // e.g. dotnet "C:\Game\Resources\SecondStageUpdater.dll" clientogl.dll "C:\Game\"
                            secondStageUpdaterStartInfo = new ProcessStartInfo
                            {
                                FileName = "dotnet",
                                Arguments = "\"" + secondStageUpdaterExecutable.FullName + "\" " + CallingExecutableFileName + " \"" + GamePath + "\"",
                                UseShellExecute = true,
                            };
                        }

                        Logger.Log("Updater: Launching second-stage updater executable.");
                        Logger.Log("Updater: FileName = " + secondStageUpdaterStartInfo.FileName);
                        Logger.Log("Updater: Arguments = " + secondStageUpdaterStartInfo.Arguments);
                        Logger.Log("Updater: UseShellExecute = " + secondStageUpdaterStartInfo.UseShellExecute);
                        using var _ = Process.Start(secondStageUpdaterStartInfo);

                        Restart?.Invoke(null, EventArgs.Empty);
                    }
                    else
                    {
                        Logger.Log("Updater: Update completed successfully.");
                        totalDownloadedKbs = 0;
                        UpdateSizeInKb = 0;
                        CheckLocalFileVersions();
                        ServerGameVersion = "N/A";
                        VersionState = VersionState.UPTODATE;
                        DoUpdateCompleted();

                        if (AreCustomComponentsOutdated())
                            DoCustomComponentsOutdatedEvent();
                    }
                }
            }
        }
        catch (Exception exception)
        {
            Logger.Log("Updater: An error occurred during the update. Message: " + exception.Message);
            VersionState = VersionState.UNKNOWN;
            DoOnUpdateFailed(exception);
        }
        finally
        {
            SharedProgressMessageHandler.HttpReceiveProgress -= ProgressMessageHandlerOnHttpReceiveProgress;
        }
    }

    /// <summary>
    /// Downloads and handles individual file.
    /// </summary>
    /// <param name="fileInfo">File info for the file.</param>
    /// <returns>Error message if something went wrong, otherwise null.</returns>
    private static async ValueTask<string> DownloadFileAsync(UpdaterFileInfo fileInfo)
    {
        Logger.Log("Updater: Initializing download of file " + fileInfo.Filename);

        UpdateDownloadProgress(0);

        string filename = fileInfo.Filename;
        const string prefixPath = "Updater";
        FileInfo decompressedFile = SafePath.GetFile(GamePath, prefixPath, filename);

        try
        {
            string uriString = string.Empty;
            int currentUpdateMirrorId = Updater.currentUpdateMirrorIndex;
            string extraExtension = fileInfo.Archived ? ARCHIVE_FILE_EXTENSION : string.Empty;
            string fileRelativePath = SafePath.CombineFilePath(prefixPath, FormattableString.Invariant($"{filename}{extraExtension}"));
            uriString = (updateMirrors[currentUpdateMirrorId].URL + filename + extraExtension).Replace('\\', '/');
            FileInfo downloadFile = SafePath.GetFile(GamePath, fileRelativePath);
            CreatePath(SafePath.CombineFilePath(GamePath, filename));
            CreatePath(downloadFile.FullName);

            if (downloadFile.Exists &&
                (fileInfo.Archived ? fileInfo.Identifier : fileInfo.ArchiveIdentifier) == GetUniqueIdForFile(fileRelativePath))
            {
                Logger.Log("Updater: File " + filename + " has already been downloaded, skipping downloading.");
            }
            else
            {
                Logger.Log("Updater: Downloading file " + filename + extraExtension);

                FileStream fileStream = new FileStream(downloadFile.FullName, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
                using (fileStream)
                {
                    Stream stream = await SharedHttpClient.GetStreamAsync(new Uri(uriString)).ConfigureAwait(false);
                    using (stream)
                    {
                        await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                    }
                }

                OnFileDownloadCompleted?.Invoke(fileInfo.Archived ? filename + extraExtension : null);
                Logger.Log("Updater: Download of file " + filename + extraExtension + " finished - verifying.");

                if (fileInfo.Archived)
                {
                    Logger.Log("Updater: File is an archive.");
                    string archiveIdentifier = CheckFileIdentifiers(filename, fileRelativePath, fileInfo.ArchiveIdentifier);

                    if (string.IsNullOrEmpty(archiveIdentifier))
                    {
                        Logger.Log("Updater: Archive " + filename + extraExtension + " is intact. Unpacking...");
                        await CompressionHelper.DecompressFileAsync(downloadFile.FullName, decompressedFile.FullName).ConfigureAwait(false);
                        downloadFile.Delete();
                    }
                    else
                    {
                        string errorMsg = "Downloaded archive " + filename + extraExtension + " has a non-matching identifier: " + archiveIdentifier + " against " + fileInfo.ArchiveIdentifier;
                        Logger.Log("Updater: " + errorMsg);
                        DeleteFileAndWait(downloadFile.FullName);

                        return errorMsg;
                    }
                }
#if !NETFRAMEWORK

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && downloadFile.Extension.Equals(".sh", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Log($"Updater: File {downloadFile.Name} is a script, adding execute permission. Current permission flags: " + downloadFile.UnixFileMode);

                    downloadFile.Refresh();

                    downloadFile.UnixFileMode |= UnixFileMode.UserExecute;

                    downloadFile.Refresh();

                    Logger.Log($"Updater: File {downloadFile.Name} execute permission added. Current permission flags: " + downloadFile.UnixFileMode);
                }
#endif
            }

            string fileIdentifier = CheckFileIdentifiers(filename, SafePath.CombineFilePath(prefixPath, filename), fileInfo.Identifier);
            if (string.IsNullOrEmpty(fileIdentifier))
            {
                Logger.Log("Updater: File " + filename + " is intact.");

                return null;
            }

            string msg = "Downloaded file " + filename + " has a non-matching identifier: " + fileIdentifier + " against " + fileInfo.Identifier;
            Logger.Log("Updater: " + msg);
            DeleteFileAndWait(decompressedFile.FullName);

            return msg;
        }
        catch (Exception exception)
        {
            Logger.Log("Updater: An error occurred while downloading file " + filename + ": " + exception.Message);
            DeleteFileAndWait(decompressedFile.FullName);

            return exception.Message;
        }
    }

    /// <summary>
    /// Updates download progress.
    /// </summary>
    /// <param name="progressPercentage">Progress percentage.</param>
    private static void UpdateDownloadProgress(int progressPercentage)
    {
        double num = currentFileSize * (progressPercentage / 100.0);
        double num2 = totalDownloadedKbs + num;

        int totalPercentage = 0;

        if (UpdateSizeInKb is > 0 and < int.MaxValue)
            totalPercentage = (int)(num2 / UpdateSizeInKb * 100.0);

        DownloadProgressChanged(currentFilename, progressPercentage, totalPercentage);
    }

    /// <summary>
    /// Checks if file path contains ignore masks.
    /// </summary>
    /// <param name="filePath">File path to check.</param>
    /// <returns>True if path contains any ignore masks, otherwise false.</returns>
    private static bool ContainsAnyMask(string filePath)
    {
        foreach (string mask in ignoreMasks)
        {
            if (filePath.Contains(mask, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets keys from INI file section.
    /// </summary>
    /// <param name="iniFile">INI file.</param>
    /// <param name="sectionName">Section name.</param>
    /// <returns>List of keys or empty list if section does not exist or no keys were found.</returns>
    private static List<string> GetKeys(IniFile iniFile, string sectionName)
    {
        List<string> keys = iniFile.GetSectionKeys(sectionName);

        if (keys != null)
            return keys;

        return new();
    }

    /// <summary>
    /// Attempts to get file identifier for a file.
    /// </summary>
    /// <param name="filePath">File path of file.</param>
    /// <returns>File identifier if successful, otherwise empty string.</returns>
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

    /// <summary>
    /// Checks file identifiers to see if file is intact.
    /// </summary>
    /// <param name="fileInfoFilename">Filename in file info.</param>
    /// <param name="localFilename">Filename on system.</param>
    /// <param name="fileInfoIdentifier">Current file identifier.</param>
    /// <returns>File identifier if check is successful, otherwise null.</returns>
    private static string CheckFileIdentifiers(string fileInfoFilename, string localFilename, string fileInfoIdentifier)
    {
        if (ContainsAnyMask(fileInfoFilename))
            return null;
        
        string identifier;  identifier = GetUniqueIdForFile(localFilename);
        return fileInfoIdentifier == identifier ? null : identifier;
    }

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

    public delegate void LocalFileCheckProgressChangedCallback(int checkedFileCount, int totalFileCount);

    public delegate void NoParamEventHandler();

    public delegate void SetExceptionCallback(Exception ex);

    public delegate void UpdateProgressChangedCallback(string currFileName, int currFilePercentage, int totalPercentage);

    public delegate void FileDownloadCompletedEventHandler(string archiveName);

    private static void ProgressMessageHandlerOnHttpReceiveProgress(object sender, HttpProgressEventArgs e) => UpdateDownloadProgress(e.ProgressPercentage);

    private static void DownloadProgressChanged(string currFileName, int currentFilePercentage, int totalPercentage) => UpdateProgressChanged?.Invoke(currFileName, currentFilePercentage, totalPercentage);

    private static void DoCustomComponentsOutdatedEvent() => OnCustomComponentsOutdated?.Invoke();

    private static void DoFileIdentifiersUpdatedEvent()
    {
        Logger.Log("Updater: File identifiers updated.");
        FileIdentifiersUpdated?.Invoke();
    }

    private static void DoOnUpdateFailed(Exception ex) => OnUpdateFailed?.Invoke(ex);

    private static void DoOnVersionStateChanged() => OnVersionStateChanged?.Invoke();

    private static void DoUpdateCompleted() => OnUpdateCompleted?.Invoke();
}
