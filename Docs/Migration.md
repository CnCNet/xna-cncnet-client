Migrating from older versions
-----------------------------

This document lists all the breaking changes and how to address them. Each section corresponds to the migration steps that are required to upgrade to the selected version. If you're skipping multiple versions in the upgrade process - you have to apply all corresponding migration steps.

> [!NOTE]
> You should always delete the `Binaries` and `BinariesNET8` folders when updating. See [How to update to latest client version](HowToUpdate.md) guide for a step-by-step process of updating the client binaries in your mod/game package.

## 2.12.12

- The `DTAConfig` library has been removed and its functionality merged into other parts of the client. Therefore, if using automatic updater, you must append the following lines to the `[Delete]` section of your `updateexec` file to prevent issues during the update process:

  In `updateexec`:
  ```ini
  [Delete]
  ; append those lines in the section
  Resources\Binaries\Windows\DTAConfig.dll
  Resources\Binaries\Windows\DTAConfig.pdb
  Resources\Binaries\OpenGL\DTAConfig.dll
  Resources\Binaries\OpenGL\DTAConfig.pdb
  Resources\Binaries\XNA\DTAConfig.dll
  Resources\Binaries\XNA\DTAConfig.pdb
  Resources\BinariesNET8\Windows\DTAConfig.dll
  Resources\BinariesNET8\Windows\DTAConfig.pdb
  Resources\BinariesNET8\OpenGL\DTAConfig.dll
  Resources\BinariesNET8\OpenGL\DTAConfig.pdb
  Resources\BinariesNET8\UniversalGL\DTAConfig.dll
  Resources\BinariesNET8\UniversalGL\DTAConfig.pdb
  Resources\BinariesNET8\XNA\DTAConfig.dll
  Resources\BinariesNET8\XNA\DTAConfig.pdb
  ```

## 2.12.0

- The client now has unified different builds among game types. The game type must be defined in the `ClientDefinitions.ini` file. Please specify `ClientGameType` in `[Settings]` section of the `ClientDefinitions.ini` file, e.g., `ClientGameType=Ares`. See [this file](https://github.com/CnCNet/xna-cncnet-client/blob/0554d7974cb741170c881116568144265e6cbabb/ClientCore/Enums/ClientType.cs) for a list of available values.

- The `trbScrollRate` component in `GameOptionsPanel` was mistakenly named as `trbClientVolume` in the INI configuration file from the beginning. This has been fixed. No action is needed unless this component was explicitly modified in your configuration (rare).

## 2.11.0.0 and earlier

- `CustomSettingFileCheckBox` and `CustomSettingFileDropDown` have been renamed to simply `FileSettingCheckBox` and `FileSettingDropDown`. This requires adjusting the control names in `OptionsWindow.ini`. `FileSettingCheckBox` has a fallback to legacy behaviour if the control has any files defined with `FileX`.

- Updater no longer has hardcoded list of download mirrors or custom components. This information must now be set in `UpdaterConfig.ini` (example is included amongst default resources in client repository). For a reference, the previously hardcoded information can be found in format used by `UpdaterConfig.ini` [here](https://gist.github.com/Starkku/1d52f0040d7a00d79e57afc2fba5f97b).

- Second-stage updater no longer has hardcoded list of launcher executables to check for when restarting the client. It will now only check `ClientDefinitions.ini` for `LauncherExe` key, and it it fails to read and launch this the client will not automatically restart after updating.

- Updater DLL filename has been changed from `DTAUpdater.dll` to `ClientUpdater.dll` and second-stage updater from `clientupdt.dat` to `SecondStageUpdater.exe` for .NET 4.8 and has been moved from base folder to `Resources/Binaries/Updater`.

    - **Note:** If you want end-users to be able to update via the old client, it is necessary to preserve a copy of the old second-stage updater (`clientupdt.dat`) in the client base directory. In other words, *don't* modify or delete `clientupdt.dat` with either of the [update server scripts](/Docs/Updater.md).

- Second-stage updater is now automatically copied to `Resources/Binaries/Updater` folder by build scripts.

- To support launching the game on Linux the file defined as `UnixGameExecutableName` (defaults to `wine-dta.sh`) in `ClientDefinitions.ini` must be set up correctly. E.g. for launching a game with wine the file could contain `wine gamemd-spawn.exe $*` where `gamemd-spawn.exe` is replaced with the game executable. Note that users might need to execute `chmod +x wine-dta.sh` once to allow it to be launched.

- The use of `*.cur` mouse cursor files is not supported on the cross-platform `UniversalGL` build. To ensure the intended cursor is shown instead of a missing texture (pink square) all themes need to contain a `cursor.png` file. Existing `*.cur` files will still be used by the Windows-only builds.

- The MonoGame MCGB editor will convert the MainMenuTheme to `MainMenuTheme.wma` when publishing for MonoGame WindowsDX. MonoGame DesktopGL only supports the `*.ogg` format. To ensure the MainMenuTheme is available on both the WindowsDX & DesktopGL client versions you need to manually convert and add the missing ogg format file to each theme. Each theme should then contain both `MainMenuTheme.wma` and `MainMenuTheme.ogg` files. The client will then switch out the correct MainMenuTheme format at runtime.

- Updated XNAUI [fixes a bug](https://github.com/Rampastring/Rampastring.XNAUI/commit/6857704734241895f9cbb2c79fbd0286c350c313) that causes the border might not be drawn. However, your mod might depends on this bug and therefore the unwanted border appears in window after upgrading. In this case, please manually specify `DrawBorders=false` for your window. For example, add the following lines to `GenericWindow.ini` to turn off borders in *some* windows like the message box. But you still need to specify this property for more windows in the ini file depending on your need.

  ```ini
  [GenericWindow]
  DrawBorders=false
  ```

- The [Tiberian Sun Client v6 Changes](https://github.com/CnCNet/xna-cncnet-client/pull/275) breaks compatibility. You need to reimplement the ini files for `SkirmishLobby`, `LANLobby`, and `CnCNetLobby` with the new `INItializableWindow` format. Also, add the `[$ExtraControls]` section in `GenericWindow.ini` file if you rely on `[ExtraControls]`. Define constants in `[ParserConstants]` section in `DTACnCNetClient.ini` file, which might be used from the `INItializableWindow` configuration. See [this guide](/Docs/Migration-INI.md) for details.

- The new [player status indicators feature](https://github.com/CnCNet/xna-cncnet-client/pull/251) replaces the old "player is ready" indicators in game lobby. This requires:
  - renaming `PlayerReadyBox*` tags into `PlayerStatusIndicator*` (which now have default values of `0` and `0` instead of `7` and `4` for `X` and `Y` respectively);
  - providing the following new textures (in `Resources` folder and/or theme subfolders, like in [this example](https://github.com/CnCNet/cncnet-yr-client-package/pull/61)):
    - `statusEmpty.png`;
    - `statusUnavailable.png`;
    - `statusAI.png`;
    - `statusClear.png`;
    - `statusOk.png`;
    - `statusInProgress.png`;
    - `statusWarning.png`;
    - `statusError.png`;

- The [Tiberian Sun Client v6 Changes](https://github.com/CnCNet/xna-cncnet-client/pull/275) changes the license to GPLv3. This means that if your client is a private fork, you must either stop releasing the modified client or provide the modified source code to public with GPLv3 license.

- `BtnSaveLoadGameOptions` in game lobbies was renamed to `btnSaveLoadGameOptions` for consistency. See [this change](https://github.com/CnCNet/cncnet-ts-client-package/commit/2ac97c68978431e94e320299e0168119f75a849f) to TSC for an example of addressing this.
