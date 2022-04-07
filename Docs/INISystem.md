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
_(inherits XNACheckbox)_

`ToolTip` = {string} tooltip for checkbox. '@' can be used for newlines

#### GameLobbyCheckBox
_(inherits XNAClientCheckbox)_
These checkboxes are specific to the game options in a game lobby.

`SpawnIniOption`  
`EnabledSpawnIniValue`  
`DisabledSpawnIniValue`  
`CustomIniPath`  
`Reversed`  
`CheckedMP`  
`Checked`
`DisallowedSideIndices`  
`MapScoringMode`  
`GameOptionMessageIndex` = `{integer}` The order in which this option is sent in an IRC message to other players in the lobby. This can be used for backwards compatibility if reordering game options in the UI.  

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

#### GameLobbyDropDown
_(inherits XNAClientDropDown)_
These drop downs are specific to the game options in a game lobby.

`Items`  
`DataWriteMode`  
`SpawnIniOption`  
`DefaultIndex`  
`OptionName`  
`GameOptionMessageIndex` = `{integer}` The order in which this option is sent in an IRC message to other players in the lobby. This can be used for backwards compatibility if reordering game options in the UI.

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
