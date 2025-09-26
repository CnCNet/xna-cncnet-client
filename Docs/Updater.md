# Instructions on how to use the updater functionality of the XNA CnCNet client

Updater-Related Files
-------------------

### Developer Files
**These files are needed only by the mod developer and aren't meant to be redistributed to others!** 
- **Version File Writer**: Software that writes a version file for updater. Executable and example config file are [included in client repository](../AdditionalFiles/VersionFileWriter). Source code of the program is available [here](https://github.com/Starkku/VersionWriter).
- **Update Server Scripts** (`preupdateexec` and `updateexec`, example files [included in client repository](../AdditionalFiles/UpdateServerScripts)): Script files that can be used to rename, move or delete files & directories. They are downloaded and executed by updater before and after the update, respectively. They can be put on the server in the same folder specified in download mirrors. Note that changes made by `preupdateexec` **will not** be reverted even if the update process itself fails afterwards. Additionally both of the scripts are executed regardless of current local or server version state & info.

### Distributable Files
- **Updater Configuration File** (`Resources/UpdaterConfig.ini`, included with [default resources](../DXMainClient/Resources/DTA) in the client repository): Client [updater configuration](#updater-configuration) file which sets the download mirrors for the updater and available custom component info. If no such file is found, client falls back to using legacy `updateconfig.ini` which uses a different syntax and does not allow setting custom component info.
- **Second-Stage Updater** (`Resources/Binaries/Updater/SecondStageUpdater.exe`, now belongs to a part of the client binaries: A second-stage updater executable that copies the files to their correct places after they've all been downloaded and then launches the client again after it is done. Client launcher executable is read from `LauncherExe` key in `Resources/ClientDefinitions.ini`, if it is not present or cannot be read for any other reason the client will not automatically restart after the second-stage updater has finished.

Basic Usage
-----------

## Quick Guide
1. Have a web server set up and create a publicly accessible directory from which to download your updates from.
2. On your client configuration, add URL of the aforementioned directory to list of available download mirrors in `Resources/UpdaterConfig.ini`. 
3. Make changes to files and `VersionConfig.ini`.
4. Run `VersionWriter.exe`.
5. Upload the contents of the `VersionWriter_CopiedFiles` and update server scripts to the aforementioned directory on the web server.

## Detailed Instructions
To have automatic updates via XNA CnCNet client an update web server needs to be set up which would then allow the update files to be downloaded by the client during the update process. The URL path to the file (sans update location part) has to replicate the local path to the file relative to mod folder in order to be succesfully downloaded (for example, with update location `https://your.test/location/of/updates/` the file `Resources/Binaries/Windows/clientdx.dll` would need to be accessible at `https://your.test/location/of/updates/Resources/Binaries/Windows/clientdx.dll` URL). Besides the update server scripts, the updater does not explicitly require any other files or specific software to exist or run on the update web server.

To set up an update information needed to produce the files to upload on a server edit `VersionConfig.ini` file to include all of the redistributed files (or updated files only if you're saving on bandwidth and don't want to allow full downloads). Each time you need to push an update to your players (also if you change something in `VersionConfig.ini`) you have to change the version key under `[Version]` section in aforementioned configuration file so the CnCNet client prompts for an update. In case you need to force users to download an update manually you can change a key under `[UpdaterVersion]` section. After that run `VersionWriter.exe` and upload the contents of the `VersionWriter_CopiedFiles` to your update server along with updater scripts.

Refer below for a more comprehensive explanation of both version writer's and updater's features & configuration files.

Features
-------

### Version File Writer
Version file writer is a program that writes the `version` file used by the client and its updater. It reads a file called `VersionConfig.ini` from its working directory for settings and list of files to include.

The example `VersionConfig.ini` included with the version file writer in client repository contains comments explaining most of the functionality and features.

`VersionWriter.exe` accepts command-line arguments that start with `/` or `-` as switches. Following switches are accepted:
- `-LOG`: Generates log file in the program directory.
- `-QUIET`: Does not generate console output.
- `-SUPRESSINPUTS`: Does not ask for user input to confirm actions.

 Additionally a single non-switch argument can be provided that can be used to set the program's working directory - this allows running VersionWriter from outside the mod directory itself.

#### Options
These are set under `[Options]` in `VersionConfig.ini`.
- `EnableExtendedUpdaterFeatures`: If set, enables additional updater features such as compressed archives, updater version and manual download URL.
- `RecursiveDirectorySearch`: If set, will go through every subdirectory recursively for directories given in `[Include]`.
- `IncludeOnlyChangedFiles`: If set, version file writer will always create two version files - one with everything included (`version_base`) and the proper, actual version file with only changed files (`version`). Note that `version_base` should be kept around as it is used to compare which files have been changed next time version file writer is ran.
- `CopyArchivedOriginalFiles`: If set, original versions of archived files will also be copied to copied files directory.
- `ExcludeHiddenAndSystemFiles`: If set, any directories (including all files and subdirectories in them, regardless of any other settings) and files flagged as hidden or system protected will be excluded. This also defaults to `true`.
- `ApplyTimestampOnVersion`: If set, the mod version string is treated as [.NET timestamp/datetime format string](https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings) with current local time applied on it.
- `NoCopyMode`: If set, no files will be copied whatsoever, only version file(s) are generated. Setting this also disables archived files feature regardless of other settings.

#### Updater Version & Manual Download URL
Setting `[UpdaterVersion]` in `VersionConfig.ini` writes this information to the `version` file and allows developers to control which versions are allowed to download files from the version info through the client. Mismatching updater versions between local and server version files will suggest users to download update manually through updater status message. Absent or malformed updater version (both local & server) is equivalent to `N/A` and updater will bypass the mismatch check entirely if server  updater version is set to this or absent.

Additionally setting `[ManualDownloadURL]` will, in addition to displaying the updater status message, also bring up a notification dialog with the provided URL as a download link in case a updater version mismatch occurs.

#### Compressed Archives
The updater supports downloading and uncompressing LZMA-compressed data archives. Files that are to be compressed should be included under `[ArchiveFiles]` in `VersionConfig.ini`. Note that they still need to be included through `[Include]` in the first place. As a result there would be information in the `version` file which allows the client, to figure out it is supposed to download the archive instead, and instead of the original files the compressed files with `.lzma` extension are placed to the `VersionWriter_CopiedFiles` folder.

#### Custom Components
Custom components are available even with the original XNA CnCnet Client, but since the IDs and filenames are hardcoded in the updater, their usage is limited. Custom component info for the updater can be set in `Resources/UpdaterConfig.ini`, see below for more info. For version file writer, any custom components should be included under `[AddOns]`, using syntax `ID=filename` as shown in the example `VersionConfig.ini`. Custom component filenames **should not** be listed under `[Include]`. The filenames can be listed under `[ArchiveFiles]` to enable use of compressed archives.

- Custom component download file path (in `Resources/UpdaterConfig.ini`) accepts absolute URLs and uses them properly, so it's possible to define custom components which have to be downloaded from elsewhere.

### Updater Configuration
The example `Resources/UpdaterConfig.ini` included with client files contains comments explaining most of the functionality and features.

The only currently supported global updater setting under `[Settings]` is `IgnoreMasks` that allows customizing the list filenames that are exempted from file integrity checks even if they are included in `version` file.

#### Download Mirrors
List of available download mirrors from which to download version info and files from. Listed as comma-separated values under `[DownloadMirrors]`, containing URL, UI display name and location. Location is optional and can be omitted.

Updater and Updater & Component options in client options will be unavailable if no download mirrors are found.

#### Custom Components
List of custom components available for the updater. Listed as comma-separated values under `[CustomComponents]`, containing custom component ID used in the `version` file, download path / URL, local filename, flag that disables archive file extensions for download path / URL.

Download path / URL supports absolute URLs, allowing custom components to be downloaded from location outside the current update server but also restricts it to one download location instead of one per each download mirror.

Download path archive file extension disable flag is a boolean value (yes/no, true/false), is optional and defaults to false.

Custom components and the Components tab in client options will be unavailable if no custom component info is found.
