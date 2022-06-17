Migrating from older versions
-----------------------------

## Migrating from pre-2.8.0.0

- `CustomSettingFileCheckBox` and `CustomSettingFileDropDown` have been renamed to simply `FileSettingCheckBox` and `FileSettingDropDown`. This requires adjusting the control names in `OptionsWindow.ini`. `FileSettingCheckBox` has a fallback to legacy behaviour if the control has any files defined with `FileX`.
- Updater no longer has hardcoded list of download mirrors or custom components. This information must now be set in `UpdaterConfig.ini` (example is included amongst default resources in client repository). For a reference, the previously hardcoded information can be found in format used by `UpdaterConfig.ini` [here](https://gist.github.com/Starkku/1d52f0040d7a00d79e57afc2fba5f97b).
- Second-stage updater no longer has hardcoded list of launcher executables to check for when restarting the client. It will now only check `ClientDefinitions.ini` for `LauncherExe` key, and it it fails to read and launch this the client will not automatically restart after updating.
- Updater DLL filename has been changed from `DTAUpdater.dll` to `ClientUpdater.dll` and second-stage updater from `clientupdt.dat` to `SecondStageUpdater.exe` and has been moved from base folder to `Resources`.