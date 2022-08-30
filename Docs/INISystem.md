# Instructions on how to construct the UI using INI files.
*TODO work in progress*

## Constants

The `[ParserConstants]` section of the `GlobalThemeSettings.ini` file contains constants that can be used in other INI files.

### Predefined System Constants

`RESOLUTION_WIDTH`: the width of the window when it is initialized  
`RESOLUTION_HEIGHT`: the height of the window when it is initialized  

### User Defined Constants

```
MY_EXAMPLE_CONSTANT=15
```
The above user-defined or system constants can be used elsewhere as:
```
[MyExampleControl]
$X=MY_EXAMPLE_CONSTANT
```
_NOTE: Constants can only be used in [dynamic control properties](#dynamic-control-properties)_

## Control properties: 
Below lists basic and dynamic control properties. Ordering of properties is important. If there is a property that relies on the size of a control, the properties must set the size of that control first.


### Basic Control Properties
Basic control properties cannot use constants

#### XNAControl

`X` = `{integer}` the X location of the control  
`Y` = `{integer}` the Y location of the control  
`Location` = `{comma separated integers}` the X and Y location of the control.  
`Width` = `{integer}` the Width of the control  
`Height` = `{integer}` the Height of the control  
`Size` = `{comma separated integers}` the Width and Height of the control.  
`Text` = `{string}` the text to display for the control (ex: buttons, labels, etc...)  
`Visible` = `{true/false or yes/no}` whether or not the control should be visible by default  
`Enabled` = `{true/false or yes/no}` whether or not the control should be enabled by default  
`DistanceFromRightBorder` = `{integer}` the distance of the right edge of this control from the right edge of its parent. This control MUST have a parent.  
`DistanceFromBottomBorder` = `{integer}` the distance of the bottom edge of this control from the bottom edge of its parent. This control MUST have a parent.  
`FillWidth` = `{integer}` this will set the width of this control to fill the parent/window MINUS this value, starting from the its X position  
`FillHeight` = `{integer}` this will set the height of this control to fill the parent/window MINUS this value, starting from the its Y position  
`DrawOrder`  
`UpdateOrder`  
`RemapColor`  

#### XNAPanel
_(inherits XNAControl)_

`BorderColor`  
`DrawMode`  
`AlphaRate`  
`BackgroundTexture`  
`SolidColorBackgroundTexture`  
`DrawBorders`  
`Padding`  

#### XNAExtraPanel
_(inherits XNAPanel)_

`BackgroundTexture`  

#### XNALabel
_(inherits XNAControl)_

`RemapColor`  
`TextColor`  
`FontIndex`  
`AnchorPoint`  
`TextAnchor`  
`TextShadowDistance`  

#### XNAButton
_(inherits XNAControl)_

`TextColorIdle`  
`TextColorHover`  
`HoverSoundEffect`   
`ClickSoundEffect`  
`AdaptiveText`  
`AlphaRate`  
`FontIndex`  
`IdleTexture`  
`HoverTexture`  
`TextShadowDistance`  

#### XNAClientButton
_(inherits XNAButton)_

`MatchTextureSize`  

#### XNALinkButton
_(inherits XNAClientButton)_

`URL`  
`ToolTip` = {string} tooltip for checkbox. '@' can be used for newlines  

#### XNACheckbox
_(inherits XNAControl)_

`FontIndex`  
`IdleColor`  
`HighlightColor`  
`AlphaRate`  
`AllowChecking`  
`Checked`  

#### XNAClientCheckbox
_(inherits XNACheckBox)_

`ToolTip` = {string} tooltip for checkbox. '@' can be used for newlines

#### XNADropDown
_(inherits XNAControl)_

`OpenUp`  
`DropDownTexture`  
`DropDownOpenTexture`  
`ItemHeight`  
`ClickSoundEffect`  
`FontIndex`   
`BorderColor`  
`FocusColor`  
`BackColor`  
`~~DisabledItemColor~~`  
`OptionN`  

#### XNAClientDropDown
_(inherits XNADropDown)_

`ToolTip` = {string} tooltip for checkbox. '@' can be used for newlines  

#### XNATabControl
_(inherits XNAControl)_

`RemapColor`  
`TextColor`  
`TextColorDisabled`  
`RemoveTabIndexN`  

#### XNATextBox
_(inherits XNAControl)_

`MaximumTextLength`  

### Basic Control Property Examples
```
X=100
Y=100
Location=100,100
Width=100
Height=100
Size=100,100
Visible=true
Visible=yes
Enabled=true
Enabled=yes
DistanceFromRightBorder=10
DistanceFromLeftBorder=10
FillWidth=10
FillHeight=10
```

### Special Controls & Their Properties

Some controls are only available under specific circumstances

#### XNAOptionsPanel Controls

Following controls are only available as children of `XNAOptionsPanel` and derived controls. These currently use basic control properties only.

##### SettingCheckBox
_(inherits XNAClientCheckBox)_

`DefaultValue` = `{true/false or yes/no}` default state of the checkbox. Value of `Checked` will be used if it is set and this isn't. Otherwise defaults to `false`.  
`SettingSection` = `{string}` name of the section in settings INI the setting is saved to. Defaults to `CustomSettings`.  
`SettingKey` = `{string}` name of the key in settings INI the setting is saved to. Defaults to `CONTROLNAME_Value` if `WriteSettingValue` is set, otherwise `CONTROLNAME_Checked`.  
`WriteSettingValue` = `{true/false or yes/no}` enable to write a specific string value to setting INI key instead of the checked state of the checkbox. Defaults to `false`.  
`EnabledSettingValue` = `{string}` value to write to setting INI key if `WriteSettingValue` is set and checkbox is checked.  
`DisabledSettingValue` = `{string}` value to write to setting INI key if `WriteSettingValue` is set and checkbox is not checked.  
`RestartRequired` = `{true/false or yes/no}` whether or not this setting requires restarting the client to apply. Defaults to `false`.  
`ParentCheckBoxName` = `{string}` name of a `XNAClientCheckBox` control to use as a parent checkbox that is required to either be checked or unchecked, depending on value of `ParentCheckBoxRequiredValue` for this checkbox to be enabled. Only works if name can be resolved to an existing control belonging to same parent as current checkbox.  
`ParentCheckBoxRequiredValue` = `{true/false or yes/no}` state required from the parent checkbox for this one to be enabled. Defaults to `true`.  

##### FileSettingCheckBox
_(inherits XNAClientCheckBox)_

`DefaultValue` = `{true/false or yes/no}` default state of the checkbox. Value of `Checked` will be used if it is set and this isn't. Otherwise defaults to `false`.  
`SettingSection` = `{string}` name of the section in settings INI the setting is saved to. Defaults to `CustomSettings`.  
`SettingKey` = `{string}` name of the key in settings INI the setting is saved to. Defaults to `CONTROLNAME_Value` if `WriteSettingValue` is set, otherwise `CONTROLNAME_Checked`.  
`RestartRequired` = `{true/false or yes/no}` whether or not this setting requires restarting the client to apply. Defaults to `false`.  
`ParentCheckBoxName` = `{string}` name of a `XNAClientCheckBox` control to use as a parent checkbox that is required to either be checked or unchecked, depending on value of `ParentCheckBoxRequiredValue` for this checkbox to be enabled. Only works if name can be resolved to an existing control belonging to same parent as current checkbox.  
`ParentCheckBoxRequiredValue` = `{true/false or yes/no}` state required from the parent checkbox for this one to be enabled. Defaults to `true`.  
`CheckAvailability` = `{true/false or yes/no}` if set, whether or not the checkbox can be (un)checked depends on if the files to copy are actually present. Defaults to `false`.  
`ResetUnavailableValue` = `{true/false or yes/no}` if set together with `CheckAvailability`, checkbox set to a value that is unavailable will be reset back to `DefaultValue`. Defaults to `false`.  
`EnabledFileN` = `{comma-separated strings}` files to copy if checkbox is checked. N starts from 0 and is incremented by 1 until no value is found. Expects 2-3 comma-separated strings in following format: source path relative to game root folder, destination path relative to game root folder and a [file operation option](#appendix-file-operation-options).  
`DisabledFileN` = `{comma-separated strings}` files to copy if checkbox is not checked. N starts from 0 and is incremented by 1 until no value is found. Expects 2-3 comma-separated strings in following format: source path relative to game root folder, destination path relative to game root folder and a [file operation option](#appendix-file-operation-options).  

##### SettingDropDown
_(inherits XNAClientDropDown)_

`Items` = `{comma-separated strings}` comma-separated list of strings to include as items to display on the dropdown control.  
`DefaultValue` = `{integer}` default item index of the dropdown. Defaults to 0 (first item).  
`SettingSection` = `{string}` name of the section in settings INI the setting is saved to. Defaults to `CustomSettings`.  
`SettingKey` = `{string}` name of the key in settings INI the setting is saved to. Defaults to `CONTROLNAME_Value` if `WriteSettingValue` is set, otherwise `CONTROLNAME_SelectedIndex`.  
`WriteSettingValue` = `{true/false or yes/no}` enable to write selected item value to the setting INI key instead of the checked state of the checkbox. Defaults to `false`.  
`RestartRequired` = `{true/false or yes/no}` whether or not this setting requires restarting the client to apply. Defaults to `false`.  

##### FileSettingDropDown
_(inherits XNAClientDropDown)_

`Items` = `{comma-separated strings}` comma-separated list of strings to include as items to display on the dropdown control.  
`DefaultValue` = `{integer}` default item index of the dropdown. Defaults to 0 (first item).  
`SettingSection` = `{string}` name of the section in settings INI the setting is saved to. Defaults to `CustomSettings`.  
`SettingKey` = `{string}` name of the key in settings INI the setting is saved to. Defaults to `CONTROLNAME_SelectedIndex`.  
`RestartRequired` = `{true/false or yes/no}` whether or not this setting requires restarting the client to apply. Defaults to `false`.  
`ItemXFileN` = `{comma-separated strings}` files to copy when dropdown item X is selected. N starts from 0 and is incremented by 1 until no value is found. Expects 2-3 comma-separated strings in following format: source path relative to game root folder, destination path relative to game root folder and a [file operation option](#appendix-file-operation-options).  

##### Appendix: File Operation Options

Valid file operation options available for files defined for `FileSettingCheckBox` and `FileSettingDropDown` are as follows:

- `AlwaysOverwrite`: Always overwrites the destination file with source file.
- `OverwriteOnMismatch`: Overwrites the destination file with source file only if they are different.
- `DontOverwrite`: Never overwrites the destination file with source file if destination file is already present.
- `KeepChanges`: Carries over the destination file with any changes manually made to by caching the file if deleted by disabling the option and then re-enabling it.

### Dynamic Control Properties
Dynamic Control Properties CAN use constants

These can ONLY be used in parent controls that inherit the `INItializableWindow` class

`$X` = ``{integer}`` the X location of the control  
`$Y` = ``{integer}`` the Y location of the control  
`$Width` = ``{integer}`` the Width of the control  
`$Height` = ``{integer}`` the Height of the control  
`$TextAnchor`  

### Dynamic Control Property Examples
```
$X=100
$X=MY_X_CONSTANT
$Y=100
$Y=MY_Y_CONSTANT
$Width=100
$Width=MY_WIDTH_CONSTANT
$Height=100
$Height=MY_HEIGHT_CONSTANT
```
