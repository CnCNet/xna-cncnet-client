# New Features

This document lists the optional, non-breaking changes. While not mandatory, implementing these changes enables new features of the client.

Breaking changes are not included in this file, but [Migration.md].

## 2.13.0

- The custom mission support and game mode updates offer some new features. Details will be provided later.
- The "Broadcast and filter game options" update offers some new features. Details will be provided later.

## 2.12.18

- The `MainMenuTheme` key in `[General]` section in `DTACnCNetClient.ini` (which might depend on `GlobalThemeSettings.ini` file) can now supports more than one background music files, separate by commas. The client will randomly select one.

## 2.12.17

- This version comes with a DirectDraw compatibility fixer, which will help users remove problematic compatibility settings on game executable files. Hence, it is advised to add game executable files to the `ClientDefinitions.ini` file. The example is shown below:
```ini
[Settings]
CompatibilityCheckExecutables=CnCNetYRLauncher.exe,gamemd.exe,gamemd-spawn.exe ; comma-separated list of strings, to check for DirectDraw compatibility mode issues
```

- A lobby settings update window is added, allowing the host to change the room name, max players, skill level, and password. To include this feature, please edit the `CnCNetGameLobby.ini` file. First, add `$CCMP100=btnGameLobbySettings:XNAClientButton` (the number may vary depending on your configurations) to the existing `[MultiplayerGameLobby]` section:
```ini
[MultiplayerGameLobby]
$CCMP100=btnGameLobbySettings:XNAClientButton
```
Then, add the following `[btnGameLobbySettings]` section:
```ini
[btnGameLobbySettings]
Text=Lobby Settings
Location=0,0
Size=133,23
DistanceFromBottomBorder=13
DistanceFromRightBorder=300
Visible=false
Enabled=false
```


## 2.12.15

- The client now supports long-path awareness to handle map files with a path longer than 260 characters, which can happen on downloading custom maps. However, the long-path awareness feature must **also** be enabled in the **player's machine**. If you use Inno Setup to distribute your mod, you can include the following in the inno setup script file:
```iss
[Registry]
Root: HKLM; Subkey: SYSTEM\CurrentControlSet\Control\FileSystem; ValueType: dword; ValueName: LongPathsEnabled; ValueData: 1; MinVersion: 10.0.14393
```
Otherwise, you can prompt players to do so by using the following `.reg` file:
```reg
Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem]
"LongPathsEnabled"=dword:00000001
```

## 2.12.13

- SpriteFont files are now revised. Please download new [SpriteFont0.xnb](../DXMainClient/Resources/DTA/SpriteFont0.xnb) and [SpriteFont1.xnb](../DXMainClient/Resources/DTA/SpriteFont1.xnb) files and override the old ones in `Resources` folder. For the rest font files (SpriteFont2, 3, 4, 5 ...), the client does not rely on them and you can remove them if they are not specified as `FontIndex` in your `.ini` files.

## 2.12.12

- `CampaignSelector` now supports game options and forced spawn options, available as `CampaignCheckBox` and `CampaignDropDown` components. `SaveSkirmishGameOptions` and `SaveCampaignGameOptions` are also available in `[Settings]` section of `ClientDefinitions.ini`.

## 2.12.10

- `VersionWriter.exe` is updated, providing new settings such as `ExcludeHiddenAndSystemFiles`, `ApplyTimestampOnVersion`, `NoCopyMode`, and `[ExcludeDirectories]`. See [Updater.md].

## 2.12.8

- You can create a `UserDefaults.ini` file in `Resources` folder to override default settings in Option window. For example:
```ini
[Video]
IntegerScaledClient=True
BorderlessWindowedClient=False

[Audio]
ClientVolume=0.3
PlayMainMenuMusic=False

[MultiPlayer]
NotifyOnUserListChange=False
```
- For `XNAClientColorDropDown` components, `DisabledItemTexture` key is now recognized.

## 2.12.7

- Random selectors defined in `[RandomSelectors]` section in `GameOptions.ini` file now accepts duplicate values, to allow adjusting the random weight of each side.

## 2.12.6

- A `MapEncoding` key can be specified in `Translation.ini` file. However **you should not specify it** unless you absolutely know what you are doing. This feature is mainly made for TS, and should never be applied to RA2.

- Three kinds of drawing mode are provided for `XNAClientColorDropDown` components. See `XNAColorDropDown` in [INISystem.md] file.

## 2.12.5

- The client has an inactive host check feature added. Specify `InactiveHostWarningMessageSeconds` and `InactiveHostKickSeconds` with a greater-than-zero integer in `[Settings]` section of `ClientDefinitions.ini` file to enable this feature.

## 2.12.4

- The client now prompts a warning before opening an unknown HTTP(s) link from a chat message. You can override the default trusted domains in `TrustedDomains` key in `[Settings]` section of `ClientDefinitions.ini` file. See [INISystem.md].

## 2.12.2

- The client can support randomly fetching one loading screen out of multiple load screen images. See `LoadingScreen` section in [INISystem.md] file.

## 2.11.7.0

- Previously the only way to restrict side selection was via co-op map settings, game mode settings or game option checkboxes. This version allows disabling specific sides for human or AI players only, by expanding the way modders can restrict selection (only for game modes) for human players and computer players separately, using `DisallowedHumanPlayerSides` and `DisallowedComputerPlayerSides`. Example in `INI\MPMaps.ini` file:
```ini
[Standard]                          ; any game mode section
; (...)
DisallowedPlayerSides=7             ; already exists - disallow sides for all players
DisallowedHumanPlayerSides=1,2,3    ; new - disallow sides for human players only
DisallowedComputerPlayerSides=4,5,6 ; new - disallow sides for computer players only
```
- The default CnCNet service URLs are upgraded to https. If you have non-https URLs in either `ClientDefinitions.ini` or `NetworkDefinitions.ini` file, try update it to https, especially if the domain ends in `cncnet.org` or `moddb.com`.

## 2.11.2.0

- Previously in 2.11.0.0 and 2.11.1.0, `ClientUpdater.xml` and `SecondStageUpdater.xml` files are released with the client binaries. These files are actually not necessary and can be safely removed.

## 2.11.1.0

- The client now provides some integer scaled resolutions from recommended solutions when the client is not in fullscreen mode. Modders are encouraged to re-specify `RecommendedResolutions` in `ClientDefinitions.ini` file, where the recommended resolutions should be no smaller than `{MinimumRenderWidth}x{MinimumRenderHeight}` and no larger than `{MaximumRenderWidth}x{MaximumRenderHeight}`.
- As updated in the documentation, we now encourage modders keeping `*.pdb` files that corresponds to `*.exe` or `*.dll` files, even when distributing the client to end users. For example, `clientdx.pdb`, `ClientCore.pdb`, etc. Keeping `*.pdb` files provides a more detailed error log and it's extremely helpful for troubleshooting.
- As updated in the documentation, Chinese translators are encouraged to use `zh-Hans` / `zh-Hant` as the name of the translation folder.

## 2.11.0.0

- The client now has a localization system implemented. See [Translation.md] file.
- The OpenGL variant of the client can load background music as an `.ogg` file. An ogg file can be placed near where the `.wma` file exists.
- Some network definitions can be customized. Example in `NetworkDefinitions.ini` file in `Resources` folder:
```ini
[Settings]
CnCNetTunnelListURL=https://cncnet.org/master-list
CnCNetPlayerCountURL=https://api.cncnet.org/status
CnCNetMapDBDownloadURL=https://mapdb.cncnet.org
CnCNetMapDBUploadURL=https://mapdb.cncnet.org/upload
DisableDiscordIntegration=False

[IRCServers]
1=irc.gamesurge.net|GameSurge|6667
2=LAN-Team.DE.EU.GameSurge.net|GameSurge Germany, IL|6660,6666,6667,6668,6669
3=Stockholm.SE.EU.GameSurge.net|GameSurge Newark, NJ|6666,6669,7000,8080
4=NuclearFallout.WA.US.GameSurge.net|GameSurge Seattle, WA|6667,5960
5=Prothid.NY.US.GameSurge.Net|GameSurge NYC, NY|5960,6660,6666,6667,6668,6669,6697
6=192.223.27.109|GameSurge IP 192.223.27.109|5960,6660,6666,6667,6668,6669
7=162.248.94.123|GameSurge IP 162.248.94.123|6667,5960
8=128.140.107.226|GameSurge IP 128.140.107.226|6660,6666,6667,6668,6669
9=188.240.145.60|GameSurge IP 188.240.145.60|6660,6666,6667,6668,6669
```
