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

- The `text` use `@` as a line break. To write the real `@` character, use `\@`. Also as INI syntax uses `;` to denote comments, use `\semicolon` to write the real `;` character.
- The `color` use string form `R,G,B` or `R,G,B,A`. All values must be between `0` and `255`. Example: `255,255,255`, `255,255,255,255`.
- The `boolean` string value parses as `true` if it contains one of these symbol as first character: `t`, `y`, `1`, `a`, `e`; and if first symbol is `n`, `f`, `0`, then it parses as `false`. 
- The `integer` type is actually `System.Int32`.
- The `float` type is actually `System.Single`.
- The `N integers` or `N floats` is a `integer` or `float` type values repeated `N` times, but separated with `,` character without spaces e.g., `0,0` or `0.0,0.0` for `2 integers` or `2 floats` respectively.
- The `comma-separated strings` is a string, but separated with `,` character without spaces e.g., `one,two,three`.
<!-- - The `comma separated integers` or `comma separated floats` is a `integer` or `float` type, but separated with `,` character without spaces e.g., `0,0` or `0.0,0.0` respectively. -->

## Control Properties

Below lists basic and dynamic control properties. Ordering of properties is important. If there is a property that relies on the size of a control, the properties must set the size of that control first.

### Basic Control Properties

Basic control properties cannot use constants.
> [!WARNING]
> Do not copy-paste ini-code below without edits because it won't work! It shows only how to work with properties.
> 
> For example,
> - `X` and `Y` are conflict with `Location`,
> - `BackgroundTexture` and `SolidColorBackgroundTexture` conflicts,
> - and many others.

#### [XNAControl](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAControl.cs)

- Basic class inherited by any other control element.

```ini
[SOMECONTROL]                      ; XNAControl
X=                                 ; integer,    the X location of the control.
Y=                                 ; integer,    the Y location of the control.
Location=                          ; 2 integers, the X and Y location of the control.
Width=                             ; integer,    the Width of the control.
Height=                            ; integer,    the Height of the control.
Size=                              ; 2 integers, the Width and Height of the control.
Text=                              ; text,       the text to display for the control (ex: buttons, labels, etc...).
Visible=true                       ; boolean,    whether or not the control should be visible by default.
Enabled=true                       ; boolean,    whether or not the control can be interacted with by default.
DistanceFromRightBorder=0          ; integer,    the distance of the right edge of this control from 
                                   ;             the right edge of its parent. This control MUST have a parent.
DistanceFromBottomBorder=0         ; integer,    the distance of the bottom edge of this control from the 
                                   ;             bottom edge of its parent. This control MUST have a parent.
FillWidth=0                        ; integer,    this will set the width of this control to fill 
                                   ;             the parent/window MINUS this value, starting from the its X position.
FillHeight=0                       ; integer,    this will set the height of this control to fill 
                                   ;             the parent/window MINUS this value, starting from the its Y position.
DrawOrder=0                        ; integer,    determine the layering order of the control within 
                                   ;             its parent control's list of child controls.
UpdateOrder=0                      ; integer,    determine the layering order of the control within 
                                   ;             its parent control's list of child controls.
RemapColor=255,255,255             ; color,      this will set a theme defined color based.
ControlDrawMode=UniqueRenderTarget ; enum (UniqueRenderTarget | Normal), 
                                   ;             this will set render option to draw control on its own render 
                                   ;             target (`UniqueRenderTarget`) or to draw control on 
                                   ;             the same render target with its parent (`Normal`).
```

#### [XNAIndicator](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAIndicator.cs)

_(inherits [XNAControl](#XNAControl))_

```ini
[SOMEINDICATOR]            ; XNAIndicator
FontIndex=0                ; integer, the index of font loaded from font list. Default value is `0`.
HighlightColor=255,255,255 ; color,   the text color when cursor above the `XNAIndicator`.
AlphaRate=0.1              ; float,   the indicator's transparency changing rate per 100 milliseconds. 
                           ;          If the indicator is transparent, it'll become non-transparent at this rate. 
```

#### [XNAPanel](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAPanel.cs)

_(inherits [XNAControl](#XNAControl))_

```ini
[SOMEPANEL]                  ; XNAPanel
BorderColor=196,196,196      ; color,      this will set a border color based.
AlphaRate=0.01               ; float,      the panel's transparency changing rate per 100 milliseconds.
                             ;             If the panel is transparent, it'll become non-transparent at this rate.
BackgroundTexture=           ; string,     loads a texture with the specific file name with suffix.
                             ;             If the texture isn't found from any asset search path,
                             ;             returns a dummy texture.
SolidColorBackgroundTexture= ; color,      this will set background color stretched texture instead of 
                             ;             user defined picture.
DrawBorders=true             ; boolean,    enables or disables borders drawing for control. 
                             ;             Borders enabled by default.
Padding=                     ; 4 integers, css-like panel padding in client window e.g.,
                             ;             `1,2,3,4` where `1` - left, `2` - top, `3` - right, `4` - bottom.
DrawMode=Stretched           ; enum (Tiled | Centered | Stretched), 
                             ;             this will set draw mode for panel.
```

#### [XNAExtraPanel](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAExtraPanel.cs)

_(inherits [XNAPanel](#XNAPanel))_

```ini
[SOMEEXTRAPANEL]   ; XNAExtraPanel
BackgroundTexture= ; string, same as XNAControl's `BackgroundTexture`.
```

#### [XNATextBlock](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNATextBlock.cs)

_(inherits [XNAPanel](#XNAPanel))_

```ini
[SOMETEXTBLOCK]       ; XNATextBlock
TextColor=196,196,196 ; color, defines text color for text block.
```

#### [XNAMultiColumnListBox](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAMultiColumnListBox.cs)

_(inherits [XNAPanel](#XNAPanel))_

```ini
[SOMEMULTICOLUMBLISTBOX]         ; XNAMultiColumnListBox
FontIndex=0                      ; integer,        the index of font loaded from font list.
DrawSelectionUnderScrollbar=yes  ; boolean,        enable/disable scroll bar, default value is `true`.
ColumnWidthN=                    ; integer,        the default columns width in pixels. `N` is integer column index.
ColumnX=                         ; string:integer, the column definition. `string` is a column header text. 
                                 ;                 `integer` is a column width in pixels. `X` is an any text.
ListBoxYAttribute:Attrname=Value ; string,         allows setting list box attributes. `Attrname` is column attribute.
                                 ;                 `Value` is column attribute value.
```

#### [XNATrackbar](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNATrackbar.cs)

_(inherits [XNAPanel](#XNAPanel))_

```ini
[SOMETRACKBAR] ; XNATrackbar
MinValue=0     ; integer, the minumum value available for XNATrackbar.
MaxValue=10    ; integer, the maximum value available for XNATrackbar.
Value=0        ; integer, the default value available for XNATrackbar.
ClickSound=    ; string,  loads a sound with the specific file name with suffix as XNATrackbar click sound.
```

#### [XNALabel](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNALabel.cs)

_(inherits [XNAControl](#XNAControl))_

```ini
[SOMELABEL]            ; XNALabel
RemapColor=255,255,255 ; color,    same as XNAControl's `RemapColor`.
TextColor=196,196,196  ; color,    determine color of the text in label.
FontIndex=0            ; integer,  the index of font loaded from font list.
AnchorPoint=0.0,0.0    ; 2 floats, this will set a label's text start drawing point.
TextShadowDistance=0.1 ; float,    the distance between text and its shadow.
TextAnchor=            ; enum (NONE | LEFT | RIGHT | HORIZONTAL_CENTER | TOP | BOTTOM | VERTICAL_CENTER),
                       ;           this will set a text anchor in label draw box.
```

#### [XNAButton](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNAButton.cs)

_(inherits [XNAControl](#XNAControl))_

```ini
[SOMEBUTTON]               ; XNAButton
TextColorIdle=255,255,255  ; color,   the text color when cursor isn't above the button.
TextColorHover=255,255,255 ; color,   the text color when cursor above the button.
HoverSoundEffect=          ; string,  loads a sound with the specific file name with suffix as button hover sound.
ClickSoundEffect=          ; string,  loads a sound with the specific file name with suffix as button click sound.
AdaptiveText=true          ; boolean, specifies how the client should change the start text drawing position 
                           ;          in the button to fill all the free space. Default value is `true`.
AlphaRate=0.01             ; float,   the button's transparency changing rate per 100 milliseconds. 
                           ;          If the button is transparent, it'll become non-transparent at this rate. 
FontIndex=0                ; integer, the index of loaded from font list.
IdleTexture=               ; string,  loads a texture with the specific file name with suffix as button idle texture.
HoverTexture=              ; string,  loads a texture with the specific file name with suffix as button hover texture.
TextShadowDistance=0.1     ; float,   the distance between text and its shadow.
```

#### [XNAClientButton](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAClientButton.cs)

_(inherits [XNAButton](#XNAButton))_

```ini
[SOMECLIENTBUTTON] ; XNAClientButton
MatchTextureSize=  ; boolean, the button's width and height will match its texture properties. 
ToolTip=           ; text,    the tooltip for button.
```

#### [XNAClientToggleButton](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAClientToggleButton.cs)

_(inherits [XNAButton](#XNAButton))_

```ini
[SOMECLIENTTOGGLEBUTTON] ; XNAClientToggleButton
CheckedTexture=          ; string, loads a texture with the specific file name with suffix as toggle button checked texture.
UncheckedTexture=        ; string, loads a texture with the specific file name with suffix as toggle button unchecked texture.
ToolTip=                 ; text, the tooltip for toggle button.
```

#### [XNALinkButton](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNALinkButton.cs)

_(inherits [XNAClientButton](#XNAClientButton))_

```ini
[SOMELINKBUTTON] ; XNALinkButton
URL=             ; string, the URL-link for OS Windows.
UnixURL=         ; string, the URL-link for Unix-like OS.
Arguments=       ; string, the arguments separated with space for URL-link.
```

#### [XNACheckbox](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNACheckBox.cs)

_(inherits [XNAControl](#XNAControl))_

```ini
[SOMECHECKBOX]             ; XNACheckbox
FontIndex=0                ; integer, the index of font loaded from font list.
IdleColor=196,196,196      ; color,   the the text color when cursor isn't above the checkbox.
HighlightColor=255,255,255 ; color,   the text color when cursor above the checkbox.
AlphaRate=0.1              ; float,   the checkbox's transparency changing rate per 100 milliseconds. 
                           ;          If the checkbox is transparent, it'll become non-transparent at this rate. 
AllowChecking=true         ; boolean, the allows user to check/uncheck checkbox.
Checked=true               ; boolean, the default checkbox status.
```

#### [XNAClientCheckbox](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAClientCheckBox.cs)

_(inherits [XNACheckBox](#XNACheckbox))_

```ini
[SOMECLIENTCHECKBOX] ; XNAClientCheckbox
ToolTip=             ; text, the tooltip for checkbox.
```

#### [XNADropDown](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNADropDown.cs)

_(inherits [XNAControl](#XNAControl))_

```ini
[SOMEDROPDOWN]                  ; XNADropDown
OpenUp=false                    ; boolean, defines open/close default status.
DropDownTexture=                ; string,  loads a texture with the specific file name with suffix as 
                                ;          texture when dropdown closed.
DropDownOpenTexture=            ; string,  loads a texture with the specific file name with suffix as 
                                ;          texture when dropdown opened.
ItemHeight=17                   ; integer, the height of each dropdown item in pixels.
ClickSoundEffect=               ; string,  loads a sound with the specific file name with suffix as 
                                ;          dropdown click sound.
FontIndex=0                     ; integer, the index of font loaded from font list.
BorderColor=196,196,196         ; color,   the color for dropdown's border line when it open.
FocusColor=64,64,64             ; color,   the color for dropdown item when cursore above it.
BackColor=0,0,0                 ; color,   the background color dropdown when it open.
DisabledItemColor=169,169,169   ; color,   the color for disabled dropdown item.
OptionX=                        ; string,  the text option for dropdown. `X` is an any text that helps to 
                                ;          describe this option e.g., `Option_FirstOption`.
; Option_FirstOption=1
; Option_SecondOption=two
; Option_ThirdOption=33333
```

#### [XNAClientDropDown](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAClientDropDown.cs)

_(inherits XNADropDown)_

```ini
[SOMECLIENTDROPDOWN] ; XNAClientDropDown
ToolTip=            ; text, tooltip for dropdown.
```

#### [XNAColorDropDown](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAColorDropDown.cs)

_(inherits XNAClientDropDown)_

```ini
[SOMECOLORDROPDOWN] ; XNAColorDropDown
ItemsDrawMode=TextAndIcon         ; enum (Text | Icon | TextAndIcon),
                                  ; this will set what combination of texture and text should client use.
RandomColorTexture=randomicon.png ; string, the file to load as texture for random color.
DisabledItemTexture=              ; string, the file to load as texture for disabled items, defaults to texture generated from disabled item color
ColorTextureHeight=               ; int, color icon height in pixels.
ColorTextureWidth=                ; int, color icon width in pixels.
```


#### [XNATabControl](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNATabControl.cs)

_(inherits [XNAControl](#XNAControl))_

```ini
[SOMETABCONTROL]              ; XNATabControl
RemapColor=255,255,255        ; color,   the tab text color.
TextColor=255,255,255         ; color,   the tab text color.
TextColorDisabled=169,169,169 ; color,   the color for disabled tab.
RemoveTabIndexN=false         ; boolean, `N` is `integer` equivalent of tab index.

; RemoveTabIndex0=true
```

#### [XNATextBox](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNATextBox.cs)

_(inherits [XNAControl](#XNAControl))_

```ini
[SOMETEXTBOX]                ; XNATextBox
MaximumTextLength=2147483647 ; integer, set maximum input string length.
```

#### [XNASuggestionTextBox](https://github.com/Rampastring/Rampastring.XNAUI/blob/master/XNAControls/XNASuggestionTextBox.cs)

_(inherits [XNATextBox](#XNATextBox))_

```ini
[SOMESUGGESTIONTEXTBOX] ; XNASuggestionTextBox
Suggestion=             ; string, set default background text when no text has typed.
```

### Basic Control Property Examples

```ini
[lblExample]
X=100
Y=100
Text=Text Sample
ToolTip=Big and beautiful tooltip@that help to undestand lblExample.
TextColor=255,255,255
Size=100,100
Visible=yes
Enabled=false
DistanceFromRightBorder=10
DistanceFromLeftBorder=10
FillWidth=10
FillHeight=10
```

### Special Controls & Their Properties

Some controls are only available under specific circumstances.

#### CoopBriefingBox

```ini
; GameLobbyBase.ini

[MapPreviewBox_CoopBriefingBox]
FontIndex=0
```

#### XNAOptionsPanel Controls

Following controls are only available as children of `XNAOptionsPanel` and derived controls. These currently use basic control properties only.

##### [SettingCheckBox](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DTAConfig/Settings/SettingCheckBox.cs)

_(inherits [XNAClientCheckBox](#XNAClientCheckBox))_

```ini
[SOMESETTINGCHECKBOX]            ; SettingCheckBox
DefaultValue=false               ; boolean, default state of the checkbox. Value of `Checked` will be used 
                                 ;          if it is set and this isn't. Otherwise defaults to `false`.
SettingSection=CustomSettings    ; string,  name of the section in settings INI the setting is saved to. 
SettingKey=                      ; string,  name of the key in settings INI the setting is saved to. 
                                 ;          Defaults to `CONTROLNAME_Value` if `WriteSettingValue` is set, 
                                 ;          otherwise `CONTROLNAME_Checked`.
WriteSettingValue=true           ; boolean, enable to write a specific string value to setting INI key 
                                 ;          instead of the checked state of the checkbox. Defaults to `false`.
EnabledSettingValue=             ; string,  value to write to setting INI key if `WriteSettingValue` 
                                 ;          is set and checkbox is checked.
DisabledSettingValue=            ; string,  value to write to setting INI key if `WriteSettingValue` 
                                 ;          is set and checkbox is not checked.
RestartRequired=false            ; boolean, whether or not this setting requires restarting the client to apply. 
ParentCheckBoxName=              ; string,  name of a `XNAClientCheckBox` control to use as a parent checkbox 
                                 ;          that is required to either be checked or unchecked, depending on value 
                                 ;          of ParentCheckBoxRequiredValue for this checkbox to be enabled. 
                                 ;          Only works if name can be resolved to an existing control belonging
                                 ;          to same parent as current checkbox.
ParentCheckBoxRequiredValue=true ; boolean, state required from the parent checkbox for this one to be enabled.
```

##### [FileSettingCheckBox](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DTAConfig/Settings/FileSettingCheckBox.cs)

_(inherits [XNAClientCheckBox](#XNAClientCheckBox))_

```ini
[SOMEFILESETTINGCHECKBOX]        ; FileSettingCheckBox
DefaultValue=false               ; boolean, default state of the checkbox. Value of `Checked` 
                                 ;          will be used if it is set and this isn't. Otherwise defaults to `false`.
SettingSection=                  ; string,  name of the section in settings INI the setting is saved to.
                                 ;          Defaults to `CustomSettings`.
SettingKey=                      ; string,  name of the key in settings INI the setting is saved to.
                                 ;          Defaults to `CONTROLNAME_Value` if `WriteSettingValue` is set,
                                 ;          otherwise `CONTROLNAME_Checked`.
RestartRequired=false            ; boolean, whether or not this setting requires restarting the client to apply. 
ParentCheckBoxName=              ; string,  name of a `XNAClientCheckBox` control to use as a parent checkbox that 
                                 ;          is required to either be checked or unchecked, depending on value of 
                                 ;          `ParentCheckBoxRequiredValue` for this checkbox to be enabled. 
                                 ;          Only works if name can be resolved to an existing control belonging
                                 ;          to same parent as current checkbox.
ParentCheckBoxRequiredValue=true ; boolean, state required from the parent checkbox for this one to be enabled.
CheckAvailability=false          ; boolean, if set, whether or not the checkbox can be (un)checked depends on if 
                                 ;          the files to copy are actually present.
ResetUnavailableValue=false      ; boolean, if set together with `CheckAvailability`, checkbox set to a value that 
                                 ;          is unavailable will be reset back to `DefaultValue`.
EnabledFileN=                    ; comma-separated strings, 
                                 ;          files to copy if checkbox is checked.
                                 ;          `N` starts from 0 and is incremented by 1 until no value is found. 
                                 ;          Expects 2-3 comma-separated strings in following format: 
                                 ;          source path relative to game root folder, destination path 
                                 ;          relative to game root folder and a file operation option 
                                 ;          (see #appendix-file-operation-options).
DisabledFileN=                   ; comma-separated strings, 
                                 ;          files to copy if checkbox is not checked. 
                                 ;          `N` starts from 0 and is incremented by 1 until no value is found. 
                                 ;          Expects 2-3 comma-separated strings in following format: 
                                 ;          source path relative to game root folder, destination path
                                 ;          relative to game root folder and a file operation option 
                                 ;          (see #appendix-file-operation-options).
```

##### [SettingDropDown](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DTAConfig/Settings/SettingDropDown.cs)

_(inherits [XNAClientDropDown](#XNAClientDropDown))_

```ini
[SOMESETTINGDROPDOWN]  ; SettingDropDown
Items=                 ; comma-separated strings,
                       ;          comma-separated list of strings to include as items to display on the dropdown control.
DefaultValue=0         ; integer, default item index of the dropdown.
SettingSection=        ; string,  name of the section in settings INI the setting is saved to. Defaults to `CustomSettings`.
SettingKey=            ; string,  name of the key in settings INI the setting is saved to. Defaults to `CONTROLNAME_Value` 
                       ;          if `WriteSettingValue` is set, otherwise `CONTROLNAME_SelectedIndex`.
WriteItemValue=false   ; boolean, enable to write selected item value to the setting INI key instead of the 
                       ;          checked state of the checkbox.
RestartRequired=true   ; boolean, whether or not this setting requires restarting the client to apply.
```

##### [FileSettingDropDown](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DTAConfig/Settings/FileSettingDropDown.cs)

_(inherits [XNAClientDropDown](#XNAClientDropDown))_

```ini
[SOMEFILESETTINGDROPDOWN]            ; FileSettingDropDown
Items=                               ; comma-separated strings,
                                     ;          comma-separated list of strings to include as items
                                     ;          to display on the dropdown control.
DefaultValue=0                       ; integer, default item index of the dropdown.
SettingSection=CustomSettings        ; string,  name of the section in settings INI the setting is saved to.
SettingKey=CONTROLNAME_SelectedIndex ; string,  name of the key in settings INI the setting is saved to. 
RestartRequired=false                ; boolean, whether or not this setting requires restarting the client to apply.
ResetUnavailableValue=false          ; boolean, determines if the client would adjust the setting value automatically
                                     ;          if the current value becomes unavailable.
ItemXFileN=                          ; comma-separated strings, 
                                     ;          files to copy when dropdown item `X` is selected. 
                                     ;          `N` starts from 0 and is incremented by 1 until no value is found. 
                                     ;          Expects 2-3 comma-separated strings in following format: 
                                     ;          source path relative to game root folder,
                                     ;          destination path relative to game root folder and a file operation option 
                                     ;          (see #appendix-file-operation-options).
```

##### Appendix: File Operation Options

Valid file operation options available for files defined for `FileSettingCheckBox` and `FileSettingDropDown` are as follows:

- `AlwaysOverwrite`: Always overwrites the destination file with source file.
- `OverwriteOnMismatch`: Overwrites the destination file with source file only if they are different.
- `DontOverwrite`: Never overwrites the destination file with source file if destination file is already present.
- `KeepChanges`: Carries over the destination file with any changes manually made to by caching the file if deleted by disabling the option and then re-enabling it.
- `AlwaysOverwrite_LinkAsReadOnly`: Try to make a hard link (will look the same as the file but the content of the file will be shared) to the source file (copies the file as a fallback if the linking fails). Recommended to use with any binary source files such as `opengl32.dll`, `d3d9.dll`, `dxgi.dll` and not recommended to use with text files. While link is established, source file and target file has property `Read-only` which protects original file and created link from edits.

### Dynamic Control Properties

Dynamic Control Properties CAN use constants.

These can ONLY be used in parent controls that inherit the `INItializableWindow` class.

```ini
$X=10            ; integer, the X location of the control  
$Y=20            ; integer, the Y location of the control  
$Width=50        ; integer, the Width of the control  
$Height=10       ; integer, the Height of the control  
$TextAnchor=LEFT ; enum (NONE | LEFT | RIGHT | HORIZONTAL_CENTER | TOP | BOTTOM | VERTICAL_CENTER),
                 ;          this will set a text anchor in label draw box.
```

### Dynamic Control Property Examples

```ini
[lblExample]
$X=100
$X=MY_X_CONSTANT
$Y=100
$Y=MY_Y_CONSTANT
$Width=100
$Width=MY_WIDTH_CONSTANT
$Height=100
$Height=MY_HEIGHT_CONSTANT
```

## Window Properties

Children of [XNAWindow](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientGUI/XNAWindow.cs) that define their own properties.

### [LoadingScreen](https://github.com/CnCNet/xna-cncnet-client/blob/develop/DXMainClient/DXGUI/Generic/LoadingScreen.cs)

```ini
; LoadingScreen.ini
[LoadingScreen]
RandomBackgroundTextures=  ; comma-separated list of strings,
                           ; paths of files to use randomly as BackgroundTexture
```

# Global Config Files

## [ClientDefinition](https://github.com/CnCNet/xna-cncnet-client/blob/develop/ClientCore/ClientConfiguration.cs)
> [!NOTE]
> _TODO work in progress_

The `ClientDefinitions.ini` file defines the client's global settings, including the game type, recommended resolutions and the executable file used to launch the game.

In `ClientDefinitions.ini`:
```ini
[Settings]
TrustedDomains=                ; comma-separated list of strings,
                               ; domain names to match links and prevent the message box from appearing before they open by default browser
                               ; example: cncnet.org,github.com,moddb.com
```

```ini
[Settings]
SaveSkirmishGameOptions=false  ; boolean, whether or not previously used game options in skirmish are saved across client sessions
SaveCampaignGameOptions=false  ; boolean, whether or not previously used game options in campaign are saved across client sessions
```

```ini
[Settings]
CompatibilityCheckExecutables=CnCNetYRLauncher.exe,gamemd.exe,gamemd-spawn.exe ; comma-separated list of strings, to check for DirectDraw compatibility mode issues
```
