Migrating from older versions
-----------------------------

## Migrating from pre-2.8.0.0

- `CustomSettingFileCheckBox` and `CustomSettingFileDropDown` have been renamed to simply `FileSettingCheckBox` and `FileSettingDropDown`. This requires adjusting the control names in `OptionsWindow.ini`. `FileSettingCheckBox` has a fallback to legacy behaviour if the control has any files defined with `FileX`.

- Updater no longer has hardcoded list of download mirrors or custom components. This information must now be set in `UpdaterConfig.ini` (example is included amongst default resources in client repository). For a reference, the previously hardcoded information can be found in format used by `UpdaterConfig.ini` [here](https://gist.github.com/Starkku/1d52f0040d7a00d79e57afc2fba5f97b).

- Second-stage updater no longer has hardcoded list of launcher executables to check for when restarting the client. It will now only check `ClientDefinitions.ini` for `LauncherExe` key, and it it fails to read and launch this the client will not automatically restart after updating.

- Updater DLL filename has been changed from `DTAUpdater.dll` to `ClientUpdater.dll` and second-stage updater from `clientupdt.dat` to `SecondStageUpdater.exe` and has been moved from base folder to `Resources`.

- Updated XNAUI [fixes a bug](https://github.com/Rampastring/Rampastring.XNAUI/commit/6857704734241895f9cbb2c79fbd0286c350c313) that causes the border might not be drawn. However, your mod might depends on this bug and therefore the unwanted border appears in window after upgrading. In this case, please manually specify `DrawBorders=false` for your window. For example, add the following lines to `GenericWindow.ini` to turn off borders in *some* windows like the message box. But you still need to specify this property for more windows in the ini file depending on your need.
  
  ```ini
  [GenericWindow]
  DrawBorders=false
  ```

- The [Tiberian Sun Client v6 Changes](https://github.com/CnCNet/xna-cncnet-client/pull/275) breaks compatibility. You need to reimplement the ini files for SkirmishLobby, LANLobby, and CnCNetLobby with the new `INItializableWindow` format. Also, add the `[$ExtraControls]` section in `GenericWindow.ini` file if you rely on `[ExtraControls]`. Define constants in `[ParserConstants]` section in `DTACnCNetClient.ini` file, which might be used from the INItializableWindow configuration.

- The [Tiberian Sun Client v6 Changes](https://github.com/CnCNet/xna-cncnet-client/pull/275) changes the license to GPLv3. This means that if your client is a private fork, you must either stop releasing the modified client or provide the modified source code to public with GPLv3 license.