# New Features

This document describes optional, non-breaking changes. While not mandatory, adopting these updates unlocks new client features.

Breaking changes are not covered here; see [Migration.md](Migration.md) instead.

## 2.13.0

- Custom mission support and game mode updates offer several new features. Details will be provided later.

- The following controls are now available to support broadcasting customized game options to the CnCNet lobby and displaying them in the game list and filters. `GameSessionCheckBox`, `GameLobbyCheckBox`, `GameSessionDropDown`, `GameLobbyDropDown`. See [INISystem.md](INISystem.md).

- The game icon in the game lobby list can be turned off. See `ShowGameIconInGameList` in [INISystem.md](INISystem.md).

## 2.12.18

- The `MainMenuTheme` key in the `[General]` section of `DTACnCNetClient.ini` (which may depend on the `GlobalThemeSettings.ini` file) now supports multiple background music files, separated by commas. The client will randomly select one.

## 2.12.17

- This version includes a DirectDraw compatibility fixer that helps users remove problematic compatibility settings from game executable files. It is therefore recommended to add game executable files to the `ClientDefinitions.ini` file. Example:

```ini
[Settings]
CompatibilityCheckExecutables=CnCNetYRLauncher.exe,gamemd.exe,gamemd-spawn.exe ; comma-separated list of strings to check for DirectDraw compatibility mode issues
```

- A lobby settings update window has been added, allowing the host to change the room name, maximum player count, skill level, and password. To enable this feature, edit the `CnCNetGameLobby.ini` file. First, add `$CCMP100=btnGameLobbySettings:XNAClientButton` (the number may vary depending on your configuration) to the existing `[MultiplayerGameLobby]` section:

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

- The client now supports long-path awareness to handle map files with paths longer than 260 characters, which can occur when downloading custom maps. However, long-path awareness must **also** be enabled on the **player’s machine**. If you use Inno Setup to distribute your mod, you can include the following in the Inno Setup script:

```iss
[Registry]
Root: HKLM; Subkey: SYSTEM\CurrentControlSet\Control\FileSystem; ValueType: dword; ValueName: LongPathsEnabled; ValueData: 1; MinVersion: 10.0.14393
```

Alternatively, you can instruct players to enable it by providing the following `.reg` file:

```reg
Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem]
"LongPathsEnabled"=dword:00000001
```

## 2.12.13

- SpriteFont files have been revised. Please download the new [SpriteFont0.xnb](/DXMainClient/Resources/DTA/SpriteFont0.xnb) and [SpriteFont1.xnb](/DXMainClient/Resources/DTA/SpriteFont1.xnb) files and replace the old ones in the `Resources` folder. The client no longer relies on the remaining font files (SpriteFont2, 3, 4, 5, …) unless they are explicitly specified as `FontIndex` in your `.ini` files; you may remove them if unused.

## 2.12.12

- `CampaignSelector` now supports game options and forced spawn options using `CampaignCheckBox` and `CampaignDropDown` components. The keys `SaveSkirmishGameOptions` and `SaveCampaignGameOptions` are also available in the `[Settings]` section of `ClientDefinitions.ini`.

## 2.12.10

- `VersionWriter.exe` has been updated with new settings: `ExcludeHiddenAndSystemFiles`, `ApplyTimestampOnVersion`, `NoCopyMode`, and the `[ExcludeDirectories]` section. See [Updater.md](Updater.md).

## 2.12.8

- You can create a `UserDefaults.ini` file in the `Resources` folder to override default settings in the Options window. Example:

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

We recommend specifying `IntegerScaledClient=True` as the default.

- For `XNAClientColorDropDown` components, the `DisabledItemTexture` key can be used.

## 2.12.7

- Random selectors defined in the `[RandomSelectors]` section of the `GameOptions.ini` file now accept duplicate values, allowing adjustment of the random weight for each side.

## 2.12.6

- A `MapEncoding` key can be specified in the `Translation.ini` file. However, **you should not specify it** unless you fully understand what you are doing. For example, you should **NOT** select GB2312/GBK/GB18030/BIG5 for a Chinese translation. This feature is primarily intended for Tiberian Sun and should never be used for Red Alert 2.

- Three drawing modes are now available for `XNAClientColorDropDown` components. See `XNAClientColorDropDown` in [INISystem.md](INISystem.md).

## 2.12.5

- An inactive host detection feature has been added. To enable it, specify `InactiveHostWarningMessageSeconds` and `InactiveHostKickSeconds` with positive integer values in the `[Settings]` section of `ClientDefinitions.ini`.

## 2.12.4

- The client now displays a warning before opening unknown HTTP/HTTPS links from chat messages. You can override the default list of trusted domains using the `TrustedDomains` key in the `[Settings]` section of `ClientDefinitions.ini`. See [INISystem.md](INISystem.md).

## 2.12.2

- The client now supports randomly selecting one loading screen from multiple images. See the `LoadingScreen` section in [INISystem.md](INISystem.md).

## 2.11.7.0

- Previously, side selection could only be restricted via co-op map settings, game mode settings, or game option checkboxes. This version allows disabling specific sides for human players or AI players separately by using `DisallowedHumanPlayerSides` and `DisallowedComputerPlayerSides` in game mode sections. Example in `INI\MPMaps.ini`:

```ini
[Standard]                          ; any game mode section
; (...)
DisallowedPlayerSides=7             ; already exists - disallows sides for all players
DisallowedHumanPlayerSides=1,2,3    ; new - disallows sides for human players only
DisallowedComputerPlayerSides=4,5,6 ; new - disallows sides for computer players only
```

- The default CnCNet service URLs have been upgraded to HTTPS. If you are using non-HTTPS URLs in `ClientDefinitions.ini` or `NetworkDefinitions.ini`, especially for domains ending in `cncnet.org` or `moddb.com`, please update them to HTTPS.

## 2.11.2.0

- In versions 2.11.0.0 and 2.11.1.0, `ClientUpdater.xml` and `SecondStageUpdater.xml` files were released with the client binaries. These files are not necessary and can be safely removed.

## 2.11.1.0

- The client now offers several integer-scaled resolutions from the recommended list when not in fullscreen mode. Modders are encouraged to update the `RecommendedResolutions` setting in `ClientDefinitions.ini` so that listed resolutions are no smaller than `{MinimumRenderWidth}x{MinimumRenderHeight}` and no larger than `{MaximumRenderWidth}x{MaximumRenderHeight}`.

- Documentation has been updated to encourage modders to retain `*.pdb` files corresponding to `*.exe` and `*.dll` files even when distributing to end users (e.g., `clientdx.pdb`, `ClientCore.pdb`, etc.). Keeping these files provides significantly more detailed error logs and greatly aids troubleshooting.

- Documentation has been updated to recommend that Chinese translators use `zh-Hans` or `zh-Hant` as the name of the translation folder.

## 2.11.0.0

- A localization system has been implemented. See [Translation.md](Translation.md).

- The OpenGL variant of the client can now load background music from an `.ogg` file that is placed alongside the corresponding `.wma` file.

- Several network-related definitions can now be customized via `NetworkDefinitions.ini` file in the `Resources` folder. An example is shown below.

```ini
[Settings]
CnCNetTunnelListURL=https://cncnet.org/master-list
CnCNetPlayerCountURL=https://api.cncnet.org/status
CnCNetMapDBDownloadURL=https://mapdb.cncnet.org
CnCNetMapDBUploadURL=https://mapdb.cncnet.org/upload
DisableDiscordIntegration=False

; https://gamesurge.net/servers
[IRCServers]
1=irc.gamesurge.net|GameSurge|6667
2=LAN-Team.DE.EU.GameSurge.net|GameSurge Nuremberg, Germany|6660,6666,6667,6668,6669
3=Stockholm.SE.EU.GameSurge.net|GameSurge Stockholm, Sweden|6666,6669,7000,8080
4=NuclearFallout.WA.US.GameSurge.net|GameSurge Seattle, WA|6667,5960
5=Prothid.NY.US.GameSurge.Net|GameSurge NYC, NY|5960,6660,6666,6667,6668,6669,6697
6=192.223.27.109|GameSurge IP 192.223.27.109|5960,6660,6666,6667,6668,6669
7=162.248.94.123|GameSurge IP 162.248.94.123|6667,5960
8=128.140.107.226|GameSurge IP 128.140.107.226|6660,6666,6667,6668,6669
9=188.240.145.60|GameSurge IP 188.240.145.60|6660,6666,6667,6668,6669
```