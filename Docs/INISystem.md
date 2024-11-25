# Instructions on how to construct the UI using INI files.
> [!NOTE]
> _TODO work in progress_

## Constants

The `[ParserConstants]` section of the `GlobalThemeSettings.ini` file contains constants that can be used in other INI files.

### Predefined System Constants

`RESOLUTION_WIDTH`: the width of the window when it is initialized  
`RESOLUTION_HEIGHT`: the height of the window when it is initialized  

### User Defined Constants

```ini
MY_EXAMPLE_CONSTANT=15
```
The above user-defined or system constants can be used elsewhere as:
```ini
[MyExampleControl]
$X=MY_EXAMPLE_CONSTANT
```
_NOTE: Constants can only be used in [dynamic control properties](#dynamic-control-properties)_

### Data Types
- The `multi-line string`s use `@` as a line break. To write the real `@` character, use `\@`. Also as INI syntax uses `;` to denote comments, use `\semicolon` to write the real `;` character.
- The `color string` use form `R,G,B` or `R,G,B,A`. All values must be between `0` and `255`.
- The `boolean` string value parses as `true` if it contains one of these symbol as first character: `t`, `y`, `1`, `a`, `e`; and if first symbol is `n`, `f`, `0`, then it parses as `false`. 
- The `integer` type is actually `System.Int32`.
- The `comma separated integers` or `comma separated floats` is a `integer` or `float` type, but separated with `,` character without spaces, e.g. `0.0,0.0`
- The `float` type is actually `System.Single`.

## Control properties
Below lists basic and dynamic control properties. Ordering of properties is important. If there is a property that relies on the size of a control, the properties must set the size of that control first.

### Basic Control Properties
Basic control properties cannot use constants.

#### [XNAControl](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAControl.cs)

`X` = `{integer}` the X location of the control.  
`Y` = `{integer}` the Y location of the control.  
`Location` = `{comma separated integers}` the X and Y location of the control.  
`Width` = `{integer}` the Width of the control.  
`Height` = `{integer}` the Height of the control.  
`Size` = `{comma separated integers}` the Width and Height of the control.  
`Text` = `{multi-line string}` the text to display for the control (ex: buttons, labels, etc...).  
`Visible` = `{boolean}` whether or not the control should be visible by default.  
`Enabled` = `{boolean}` whether or not the control should be enabled by default.  
`DistanceFromRightBorder` = `{integer}` the distance of the right edge of this control from the right edge of its parent. This control MUST have a parent.  
`DistanceFromBottomBorder` = `{integer}` the distance of the bottom edge of this control from the bottom edge of its parent. This control MUST have a parent.  
`FillWidth` = `{integer}` this will set the width of this control to fill the parent/window MINUS this value, starting from the its X position.  
`FillHeight` = `{integer}` this will set the height of this control to fill the parent/window MINUS this value, starting from the its Y position.  
`DrawOrder`  = `{integer}` determine the layering order of the control within its parent control's list of child controls.
`UpdateOrder` = `{integer}` determine the layering order of the control within its parent control's list of child controls.
`RemapColor` = `{color string}` this will set a theme defined color based.
`ControlDrawMode` = `{string}` this will set render option to draw control on its own render target (`UniqueRenderTarget`) or to draw control on the same render target with its parent (`Normal`).

#### [XNAPanel](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAPanel.cs)
_(inherits [XNAControl](#XNAControl))_

`BorderColor` = `{color string}` this will set a border color based on a string in the form `R,G,B` or `R,G,B,A`. All values must be between 0 and 255.
`DrawMode` = `{string}` this will set draw mode for panel. Allowed values: `Tiled`, `Centered`, `Stretched` (default option).
`AlphaRate` = `{float}` the panel's transparency changing rate per 100 milliseconds. If the panel is transparent, it'll become non-transparent at this rate. Default value is `0.01`.
`BackgroundTexture` = `{string}` loads a texture with the specific file name with suffix. If the texture isn't found from any asset search path, returns a dummy texture.
`SolidColorBackgroundTexture` = `{color string}` this will set background color stretched texture instead of user defined picture.
`DrawBorders` = `{boolean}` enables or disables borders drawing for control. Borders enabled by default.
`Padding` = `{comma separated integers}` css-like panel padding in client window, i.e. `1,2,3,4` where `1` - left, `2` - top, `3` - right, `4` - bottom.

#### [XNAExtraPanel](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAExtraPanel.cs)
_(inherits [XNAPanel](#XNAPanel))_

`BackgroundTexture` = `{string}` same as [XNAPanel](#XNAControl)'s `BackgroundTexture`. If this key exists, `XNAExtraPanel` parse ignore others.

#### [XNALabel](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAButton.cs)
_(inherits [XNAControl](#XNAControl))_

`RemapColor` = `{color string}` same as [XNAControl](#XNAControl)'s `RemapColor`.
`TextColor` = `{color string}` determine color of the text in label.
`FontIndex` = `{integer}` the index of loaded from font list. Default value is `0`.
`AnchorPoint` = `{comma separated floats}`  this will set a label's text start drawing point. Default value is `0.0,0.0`
`TextAnchor` = `{string}` this will set a text anchor in label draw box. Available values are `NONE`, `LEFT`, `RIGHT`, `HORIZONTAL_CENTER`, `TOP`, `BOTTOM`, `VERTICAL_CENTER`
`TextShadowDistance` = `{float}` the distance between text and its shadow.

#### [XNAButton](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAButton.cs)
_(inherits [XNAControl](#XNAControl))_

`TextColorIdle` = `{color string}` the text color when cursor isn't above the button.
`TextColorHover` = `{color string}` the text color when cursor above the button.
`HoverSoundEffect` = `{string}` loads a sound with the specific file name with suffix as button hover sound.
`ClickSoundEffect` = `{string}` loads a sound with the specific file name with suffix as button click sound.
`AdaptiveText` = `{boolean}` specifies how the client should change the start text drawing position in the button to fill all the free space. Default value is `true`.
`AlphaRate` = `{float}` the button's transparency changing rate per 100 milliseconds. If the panel is transparent, it'll become non-transparent at this rate. Default value is `0.01`.
`FontIndex` = `{integer}` the index of loaded from font list. Default value is `0`.
`IdleTexture` = `{string}` loads a texture with the specific file name with suffix as button idle texture.
`HoverTexture` = `{string}` loads a texture with the specific file name with suffix as button hover texture.
`TextShadowDistance` = `{float}` the distance between text and its shadow.

#### [XNAClientButton](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAClientButton.cs)
_(inherits [XNAButton](#XNAButton))_

`MatchTextureSize` = `{boolean}` 
`ToolTip` = `{multi-line string}` 

#### [XNALinkButton](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNALinkButton.cs)
_(inherits [XNAClientButton](#XNAClientButton))_

`URL` = `{string}` the URL-link for OS Windows
`UnixURL` = `{string}` the URL-link for Unix-like OS
`Arguments` = `{string}` the arguments separated with space for URL-link

#### XNACheckbox
_(inherits [XNAControl](#XNAControl))_

`FontIndex`  
`IdleColor`  
`HighlightColor`  
`AlphaRate`  
`AllowChecking`  
`Checked`  

#### XNAClientCheckbox
_(inherits XNACheckBox)_

`ToolTip` = `{multi-line string}` tooltip for checkbox

#### XNADropDown
_(inherits [XNAControl](#XNAControl))_

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

`ToolTip` = `{multi-line string}` tooltip for checkbox 

#### XNATabControl
_(inherits [XNAControl](#XNAControl))_

`RemapColor`  
`TextColor`  
`TextColorDisabled`  
`RemoveTabIndexN`  

#### XNATextBox
_(inherits [XNAControl](#XNAControl))_

`MaximumTextLength`  

### Basic Control Property Examples
```ini
X=100
Y=100
Text=Text Sample
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

`$X` = `{integer}` the X location of the control  
`$Y` = `{integer}` the Y location of the control  
`$Width` = `{integer}` the Width of the control  
`$Height` = `{integer}` the Height of the control  
`$TextAnchor`  

### Dynamic Control Property Examples
```ini
$X=100
$X=MY_X_CONSTANT
$Y=100
$Y=MY_Y_CONSTANT
$Width=100
$Width=MY_WIDTH_CONSTANT
$Height=100
$Height=MY_HEIGHT_CONSTANT
```
