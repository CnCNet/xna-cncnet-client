# Migrating from older versions - INI configuration

Migrating to client version [2.11.0.0][client_2.11] or [2.12.0][client_2.12] from pre-2.11.0.0.

This guide uses [YR mod base][mod_base] configuration as an example by migrating from commit [`6ce7db7`](https://github.com/Starkku/cncnet-client-mod-base/commit/6ce7db7fd753df329fb435c3aa5ba90505e5f3a2) to [`34efc04`](https://github.com/Starkku/cncnet-client-mod-base/commit/34efc0454c64e4af28e8177e63f3d9546cbbc6fb). The majority of changes also applies to non-YR client configurations.

It is **highly recommended** to make a complete backup of your game/mod before starting.

## Edit `ClientDefinitions.ini`

Since v2.12, the client has unified different builds among game types. The game type must be defined in `ClientDefinitions.ini` now.

- Add `[Settings]->ClientGameType=YR` (defines client behaviour by game. Allowed options: TS, YR, Ares)

The way the client is launched on Unix systems has changed.

1. Add `[Settings]->UnixLauncherExe=YRLauncher.sh` (script file name can be anything)
2. Create `YRLauncher.sh` in game directory:

```sh
#!/bin/sh

cd "$(dirname "$0")"
dotnet Resources/BinariesNET8/UniversalGL/clientogl.dll "$@"
```

3. **OPTIONAL** Add these entries in `[Settings]` (fill with your required/forbidden mod files):

```ini
; Comma-separated list of files required to run the game / mod that are not included in the installation.
RequiredFiles=
; Comma-separated list of files that cannot be present to run the game / mod without problems.
ForbiddenFiles=
```

## Add `GameLobbyBase.ini`

Unlike in previous versions, skirmish and multiplayer lobbies share a common, abstract base layout. This file is the base layout of all game lobbies (skirmish, LAN, CnCNet). **Game options have been moved from `GameOptions.ini` to this file**.

See example configuration below or in [YR mod base][mod_base]:

<details>
<summary>Click to show file content</summary>

```ini
[INISystem]
BasedOn=GenericWindow.ini

[GameLobbyBase]
PlayerOptionLocationX=12        ; def=25
PlayerOptionLocationY=25        ; def=24
PlayerOptionVerticalMargin=9    ; def=12
PlayerOptionHorizontalMargin=5  ; def=3
PlayerOptionCaptionLocationY=6  ; def=6
PlayerNameWidth=127             ; def=136
SideWidth=120                   ; def=91
ColorWidth=80                   ; def=79
StartWidth=50                   ; def=49
TeamWidth=50                    ; def=46

; controls
$CC00=btnLaunchGame:GameLaunchButton
$CC01=btnLeaveGame:XNAClientButton
$CC03=MapPreviewBox:MapPreviewBox
$CC04=GameOptionsPanel:XNAPanel
$CC05=PlayerOptionsPanel:XNAPanel
$CC06=lblMapName:XNALabel
$CC07=lblMapAuthor:XNALabel
$CC08=lblGameMode:XNALabel
$CC09=lblMapSize:XNALabel
$CC12=lbMapList:XNAMultiColumnListBox
$CC13=ddGameMode:XNAClientDropDown
$CC14=lblGameModeSelect:XNALabel
$CC15=btnPickRandomMap:XNAClientButton
$CC16=tbMapSearch:XNASuggestionTextBox
$CC17=PlayerExtraOptionsPanel:PlayerExtraOptionsPanel
$CC18=lbChatMessages:ChatListBox
$CC19=tbChatInput:XNAChatTextBox
$CC20=panelBorderTop:XNAExtraPanel
$CC21=panelBorderBottom:XNAExtraPanel
$CC22=panelBorderLeft:XNAExtraPanel
$CC23=panelBorderRight:XNAExtraPanel
$CC24=panelBorderCornerTL:XNAExtraPanel
$CC25=panelBorderCornerTR:XNAExtraPanel
$CC26=panelBorderCornerBL:XNAExtraPanel
$CC27=panelBorderCornerBR:XNAExtraPanel

[lblName]
Text=Players; in the game its Players, makes more sense than Name actually, eh

[lblSide]
Text=Side

[lblStart]
Text=Start
Visible=true

[lblColor]
Text=Color

[lblTeam]
Text=Team

[ddPlayerStartBase]
Enabled=true
Visible=true

[ddPlayerStart0]
$BaseSection=ddPlayerStartBase

[ddPlayerStart1]
$BaseSection=ddPlayerStartBase

[ddPlayerStart2]
$BaseSection=ddPlayerStartBase

[ddPlayerStart3]
$BaseSection=ddPlayerStartBase

[ddPlayerStart4]
$BaseSection=ddPlayerStartBase

[ddPlayerStart5]
$BaseSection=ddPlayerStartBase

[ddPlayerStart6]
$BaseSection=ddPlayerStartBase

[ddPlayerStart7]
$BaseSection=ddPlayerStartBase

[lbMapList]
$X=LOBBY_EMPTY_SPACE_SIDES
$Y=EMPTY_SPACE_TOP + 33
$Width=getWidth($ParentControl) - (getX($Self) + (getWidth(MapPreviewBox) + LOBBY_EMPTY_SPACE_SIDES + LOBBY_PANEL_SPACING))
$Height=getBottom(MapPreviewBox) - getY($Self)
SolidColorBackgroundTexture=0,0,0,128

[ddGameMode]
$Width=180
$Height=DEFAULT_CONTROL_HEIGHT
$X=getRight(lbMapList) - getWidth($Self)
$Y=getY(lbMapList) - getHeight($Self) - EMPTY_SPACE_TOP

[lblGameModeSelect]
Text=Game mode:
$X=getX(ddGameMode) - getWidth($Self) - LABEL_SPACING
$Y=getY(ddGameMode) + 1

[btnMapSortAlphabetically]
Visible=false
Enabled=false

[btnLaunchGame]
Text=Launch Game
$Width=BUTTON_WIDTH_133
$Height=DEFAULT_BUTTON_HEIGHT
$X=LOBBY_EMPTY_SPACE_SIDES
$Y=getHeight($ParentControl) - getHeight($Self) - EMPTY_SPACE_BOTTOM

[btnPickRandomMap]
Text=Pick Random Map
$Width=BUTTON_WIDTH_133
$Height=DEFAULT_BUTTON_HEIGHT
$X=LOBBY_EMPTY_SPACE_SIDES
$Y=getY(btnLaunchGame) - getHeight($Self) - LOBBY_PANEL_SPACING

[tbMapSearch]
Suggestion=Search map...
$Width=getRight(lbMapList) - getRight(btnPickRandomMap) - LOBBY_PANEL_SPACING
$Height=DEFAULT_BUTTON_HEIGHT ;DEFAULT_CONTROL_HEIGHT
$X=getRight(btnPickRandomMap) + LOBBY_PANEL_SPACING
$Y=getY(btnPickRandomMap) ; + 1
BackColor=255,255,255
;SolidColorBackgroundTexture=0,0,0,128

[MapPreviewBox]
$Width=812
$Height=380
$X=getWidth($ParentControl) - getWidth($Self) - LOBBY_EMPTY_SPACE_SIDES
$Y=292
SolidColorBackgroundTexture=0,0,0,128

[lblMapName]
$Height=DEFAULT_LBL_HEIGHT
$X=getX(MapPreviewBox)
$Y=getBottom(MapPreviewBox) + LABEL_SPACING

[lblMapAuthor]
$TextAnchor=LEFT
$AnchorPoint=getRight(MapPreviewBox),getY(lblMapName)

[lblGameMode]
$Height=DEFAULT_LBL_HEIGHT
$X=getX(lblMapName)
$Y=getBottom(lblMapName) + LABEL_SPACING

[lblMapSize]
$Height=DEFAULT_LBL_HEIGHT
$X=getX(lblGameMode)
$Y=getBottom(lblGameMode) + LABEL_SPACING

[btnLeaveGame]
Text=Leave Game
$Width=BUTTON_WIDTH_133
$Height=DEFAULT_BUTTON_HEIGHT
$X=getWidth($ParentControl) - getWidth($Self) - LOBBY_EMPTY_SPACE_SIDES
$Y=getY(btnLaunchGame)

[PlayerOptionsPanel]
$X=getX(MapPreviewBox)
$Y=EMPTY_SPACE_TOP
$Width=getWidth($ParentControl) - (getX($Self) + (getWidth(GameOptionsPanel) + LOBBY_EMPTY_SPACE_SIDES + LOBBY_PANEL_SPACING))
$Height=getHeight(GameOptionsPanel)
SolidColorBackgroundTexture=0,0,0,128

$CC00=btnPlayerExtraOptionsOpen:XNAClientButton

[PlayerExtraOptionsPanel]
$Width=238
$Height=247
$X=getRight(PlayerOptionsPanel) - getWidth($Self)
$Y=getY(PlayerOptionsPanel)
SolidColorBackgroundTexture=0,0,0,128

[btnPlayerExtraOptionsOpen]
$Width=OPEN_BUTTON_WIDTH
$Height=OPEN_BUTTON_HEIGHT
$X=getWidth($ParentControl) - getWidth($Self)
$Y=0
IdleTexture=optionsButton.png
HoverTexture=optionsButton_c.png

[GameOptionsPanel]
$Width=330
$Height=270
$X=getWidth($ParentControl) - getWidth($Self) - LOBBY_EMPTY_SPACE_SIDES
$Y=EMPTY_SPACE_TOP
SolidColorBackgroundTexture=0,0,0,128

; Left column checkboxes
$CC-GO01=chkShortGame:GameLobbyCheckBox
$CC-GO02=chkRedeplMCV:GameLobbyCheckBox
$CC-GO03=chkSuperWeapons:GameLobbyCheckBox
$CC-GO04=chkCrates:GameLobbyCheckBox
$CC-GO05=chkBuildOffAlly:GameLobbyCheckBox
$CC-GO06=chkMultiEngineer:GameLobbyCheckBox

; Right column checkboxes
$CC-GO07=chkIngameAllying:GameLobbyCheckBox
$CC-GO08=chkStolenTech:GameLobbyCheckBox
$CC-GO09=chkNavalCombat:GameLobbyCheckBox
$CC-GO10=chkDestroyableBridges:GameLobbyCheckBox
$CC-GO11=chkBrutalAI:GameLobbyCheckBox
$CC-GO12=chkNoSpawnPreview:GameLobbyCheckBox

; Dropdowns
$CC-GODD01=cmbCredits:GameLobbyDropDown
$CC-GODD02=lblCredits:XNALabel
; $CC-GODD03=cmbGameSpeedCap:GameLobbyDropDown ; different in MP and SP
$CC-GODD03PH=cmbGameSpeedCapPlaceholder:XNAControl
$CC-GODD04=lblGameSpeedCap:XNALabel
$CC-GODD05=cmbTechLevel:GameLobbyDropDown
$CC-GODD06=lblTechLevel:XNALabel
$CC-GODD07=cmbStartingUnits:GameLobbyDropDown
$CC-GODD08=lblStartingUnits:XNALabel

$CC01=BtnSaveLoadGameOptions:XNAClientButton

[BtnSaveLoadGameOptions]
$Width=OPEN_BUTTON_WIDTH
$Height=OPEN_BUTTON_HEIGHT
$X=getWidth($ParentControl) - getWidth($Self)
$Y=0
IdleTexture=optionsButton.png
HoverTexture=optionsButton_c.png

;============================
; LEFT Column Checkboxes
;============================

[chkShortGame]
Text=Short Game
SpawnIniOption=ShortGame
Checked=True
ToolTip=Players win when all enemy buildings are destroyed.
$X=EMPTY_SPACE_SIDES
$Y=EMPTY_SPACE_TOP

[chkRedeplMCV]
Text=MCV Repacks
SpawnIniOption=MCVRedeploy
Checked=True
ToolTip=Players have the ability to move their command center after it's deployed.
$X=getX(chkShortGame)
$Y=getBottom(chkShortGame) + GAME_OPTION_ROW_SPACING

[chkSuperWeapons]
Text=Superweapons
SpawnIniOption=Superweapons
Checked=False
ToolTip=Allow superweapons to be built.
$X=getX(chkShortGame)
$Y=getBottom(chkRedeplMCV) + GAME_OPTION_ROW_SPACING

[chkCrates]
Text=Crates Appear
SpawnIniOption=Crates
Checked=False
ToolTip=Random power-up crates will appear.
$X=getX(chkShortGame)
$Y=getBottom(chkSuperWeapons) + GAME_OPTION_ROW_SPACING

[chkBuildOffAlly]
Text=Build Off Allies
SpawnIniOption=BuildOffAlly
Checked=True
ToolTip=Allow players to build near their allies' Construction Yards.
$X=getX(chkShortGame)
$Y=getBottom(chkCrates) + GAME_OPTION_ROW_SPACING

[chkMultiEngineer]
Text=Multi Engineer
SpawnIniOption=MultiEngineer
Checked=False
ToolTip=Capturing structures requires 3 Engineers instead of 1.
$X=getX(chkShortGame)
$Y=getBottom(chkBuildOffAlly) + GAME_OPTION_ROW_SPACING

;============================
; Right Column Checkboxes
;============================

[chkIngameAllying]
Text=Ingame Allying
SpawnIniOption=AlliesAllowed
CustomIniPath=INI/Game Options/AlliesAllowed.ini
Checked=True
ToolTip=Players can form and break alliances in game.
$X=getX(chkShortGame) + GAME_OPTION_COLUMN_SPACING
$Y=getY(chkShortGame)

[chkStolenTech]
Text=Stolen Tech
CustomIniPath=INI/Game Options/StolenTech.ini
Checked=True
ToolTip=Allow infiltration of battle labs for stolen tech infantry.
Reversed=yes
$X=getX(chkIngameAllying)
$Y=getBottom(chkIngameAllying) + GAME_OPTION_ROW_SPACING

[chkNavalCombat]
Text=Naval Combat
CustomIniPath=INI/Game Options/NavalCombat.ini
Checked=True
ToolTip=Allow shipyards and naval units to be built.
Reversed=yes
$X=getX(chkIngameAllying)
$Y=getBottom(chkStolenTech) + GAME_OPTION_ROW_SPACING

[chkDestroyableBridges]
Text=Destroyable Bridges
CustomIniPath=INI/Game Options/DestroyableBridges.ini
Checked=True
Location=1038,86
ToolTip=Allow bridges to be destroyed by conventional weapons & force firing.
Reversed=yes
$X=getX(chkIngameAllying)
$Y=getBottom(chkNavalCombat) + GAME_OPTION_ROW_SPACING

[chkBrutalAI]
Text=Brutal AI
CustomIniPath=INI/Game Options/BrutalAI.ini
Checked=False
Location=1038,107
ToolTip=Makes the AI harder across all levels.
$X=getX(chkIngameAllying)
$Y=getBottom(chkDestroyableBridges) + GAME_OPTION_ROW_SPACING

[chkNoSpawnPreview]
Text=No Spawn Previews
CustomIniPath=INI/Game Options/NoSpawnPreview.ini
Checked=True
Location=1038,128
ToolTip=Do not display players' starting locations in loading screen map preview.
$X=getX(chkIngameAllying)
$Y=getBottom(chkBrutalAI) + GAME_OPTION_ROW_SPACING


;============================
; Dropdowns
;============================

[lblCredits]
Text=Credits:
$Height=DEFAULT_LBL_HEIGHT
$X=getX(cmbCredits)
$Y=getY(cmbCredits) - LABEL_SPACING - DEFAULT_LBL_HEIGHT

[cmbCredits]
OptionName=Starting Credits
Items=50000,45000,40000,35000,30000,25000,20000,15000,10000
DefaultIndex=7
SpawnIniOption=Credits
DataWriteMode=String
ToolTip=Changes how many credits players start with.
$Width=GAME_OPTION_DD_WIDTH
$Height=GAME_OPTION_DD_HEIGHT
$X=EMPTY_SPACE_SIDES
$Y=getHeight($ParentControl) - (getHeight($Self) + EMPTY_SPACE_BOTTOM)

[lblGameSpeedCap]
Text=Game Speed:
$Height=DEFAULT_LBL_HEIGHT
$X=getX(cmbGameSpeedCapPlaceholder)
$Y=getY(cmbGameSpeedCapPlaceholder) - LABEL_SPACING - DEFAULT_LBL_HEIGHT

[cmbGameSpeedCapPlaceholder]
Visible=false
Enabled=false
$Width=GAME_OPTION_DD_WIDTH
$Height=GAME_OPTION_DD_HEIGHT
$X=getX(cmbCredits)
$Y=getY(lblCredits) - LOBBY_PANEL_SPACING - GAME_OPTION_DD_HEIGHT

; not actually a control in this file, used for inheritance
[cmbGameSpeedCap]
OptionName=Game Speed
; Items= ...
DefaultIndex=2
SpawnIniOption=GameSpeed
DataWriteMode=Index
ToolTip=Changes game speed cap.
$Width=getWidth(cmbGameSpeedCapPlaceholder)
$Height=getHeight(cmbGameSpeedCapPlaceholder)
$X=getX(cmbGameSpeedCapPlaceholder)
$Y=getY(cmbGameSpeedCapPlaceholder)

[lblTechLevel]
Text=Tech Level:
$X=getX(cmbTechLevel)
$Y=getY(cmbTechLevel) - LABEL_SPACING - DEFAULT_LBL_HEIGHT
Enabled=no
Visible=no

[cmbTechLevel]
OptionName=Tech Level
Items=10,9,8,7,6,5,4,3,2,1
DefaultIndex=0
SpawnIniOption=TechLevel
DataWriteMode=String
ToolTip=Changes maximum tech level for all players.
$Width=GAME_OPTION_DD_WIDTH
$Height=GAME_OPTION_DD_HEIGHT
$X=EMPTY_SPACE_SIDES + GAME_OPTION_COLUMN_SPACING
$Y=getY(cmbCredits)
Enabled=no
Visible=no

[lblStartingUnits]
Text=Starting Units:
$X=getX(cmbStartingUnits)
$Y=getY(cmbStartingUnits) - LABEL_SPACING - DEFAULT_LBL_HEIGHT

[cmbStartingUnits]
OptionName=Starting Units
Items=10,9,8,7,6,5,4,3,2,1,0
DefaultIndex=10
SpawnIniOption=UnitCount
DataWriteMode=String
ToolTip=Changes how many infantry units players start with.
$Width=GAME_OPTION_DD_WIDTH
$Height=GAME_OPTION_DD_HEIGHT
$X=getX(cmbTechLevel)
$Y=getY(lblTechLevel) - LOBBY_PANEL_SPACING - GAME_OPTION_DD_HEIGHT

; Window Border Sides

[panelBorderTop]
Location=0,-8
BackgroundTexture=border-top.png
DrawMode=Stretched
Size=0,9
FillWidth=0

[panelBorderBottom]
Location=0,999
BackgroundTexture=border-bottom.png
DrawMode=Stretched
Size=0,9
FillWidth=0
DistanceFromBottomBorder=-8

[panelBorderLeft]
Location=-8,0
BackgroundTexture=border-left.png
DrawMode=Stretched
Size=9,0
FillHeight=0

[panelBorderRight]
Location=999,0
BackgroundTexture=border-right.png
DrawMode=Stretched
Size=9,0
FillHeight=0
DistanceFromRightBorder=-8

; Window Border Corners

[panelBorderCornerTL]
Location=-8,-8
BackgroundTexture=border-corner-tl.png
Size=9,9

[panelBorderCornerTR]
Location=999,-8
BackgroundTexture=border-corner-tr.png
Size=9,9
DistanceFromRightBorder=-8

[panelBorderCornerBL]
Location=-8,999
BackgroundTexture=border-corner-bl.png
Size=9,9
DistanceFromBottomBorder=-8

[panelBorderCornerBR]
Location=999,999
BackgroundTexture=border-corner-br.png
Size=9,9
DistanceFromRightBorder=-8
DistanceFromRightBorder=-8
DistanceFromBottomBorder=-8
```

</details>

### Port custom game options

If your game/mod has custom game options, you have to port them yourself. To add controls in the game options panel, add `$CC-GO` prefixed list entries in `[GameOptionsPanel]`, then create their own sections.

Example option in `GameOptions.ini` in previous versions:

```ini
[MultiplayerGameLobby]
CheckBoxes=chkNewOption...

[SkirmishLobby]
CheckBoxes=chkNewOption...

[chkNewOption]
Text=My New Option
CustomIniPath=INI/Game Options/MyNewOption.ini
ToolTip=Enable this new option.
Checked=False
Location=1126,79
```

Example option in `GameLobbyBase.ini` in the new version:

```ini
[GameOptionsPanel]
$CC-GONEW=chkNewOption:GameLobbyCheckBox

[chkNewOption]
Text=My New Option
CustomIniPath=INI/Game Options/MyNewOption.ini
ToolTip=Enable this new option.
Checked=False
Location=1126,79 ; $X and $Y are recommended instead
```

## Edit `SkirmishLobby.ini`

This file extends the game lobby base with skirmish-specific controls. **Remove (or port) previous content of this file.** If you have a modified `[SkirmishLobby]` section in `GameOptions.ini`, move it here instead of using the example one below. Remove `CheckBoxes`,`DropDowns` and`Labels` entries; if you have custom game options, see section [Port custom game options](#port-custom-game-options) on how to port them.

```ini
[INISystem]
BasedOn=GameLobbyBase.ini

[SkirmishLobby]
$BaseSection=GameLobbyBase

[GameOptionsPanel]
$CC-GODD03=cmbGameSpeedCapSkirmish:GameLobbyDropDown

[cmbGameSpeedCapSkirmish]
$BaseSection=cmbGameSpeedCap
Items=Fastest (MAX),Faster (60 FPS),Fast (30 FPS),Medium (20 FPS),Slow (15 FPS),Slower (12 FPS),Slowest (10 FPS)
```

## Edit `MultiplayerGameLobby.ini`

This file extends the game lobby base with multiplayer-specific controls, such as the chat box and lock and ready buttons. **Remove (or port) previous content of this file.** If you have a modified `[MultiplayerGameLobby]` section in `GameOptions.ini`, move it here instead of using the example one below. Remove `CheckBoxes`,`DropDowns` and`Labels` entries; if you have custom game options, see section [Port custom game options](#port-custom-game-options) on how to port them.

<details>
<summary>Click to show file content</summary>

```ini
[INISystem]
BasedOn=GameLobbyBase.ini

[MultiplayerGameLobby]
$BaseSection=GameLobbyBase
PlayerOptionLocationX=36
PlayerOptionLocationY=25        ; def=24
PlayerOptionVerticalMargin=9    ; def=12
PlayerOptionHorizontalMargin=5    ; def=3
PlayerOptionCaptionLocationY=6    ; def=6
PlayerStatusIndicatorX=8
PlayerStatusIndicatorY=0
PlayerNameWidth=135             ; def=136
SideWidth=110                    ; def=91
ColorWidth=80                    ; def=79
StartWidth=45                    ; def=49
TeamWidth=35                    ; def=46

; controls
$CCMP00=btnLockGame:XNAClientButton
$CCMP01=chkAutoReady:XNAClientCheckBox

[lbMapList]
$Height=291

[btnPickRandomMap]
$Y=getBottom(lbMapList) + LOBBY_PANEL_SPACING

[tbMapSearch]
$X=getRight(btnPickRandomMap) + LOBBY_PANEL_SPACING
$Y=getY(btnPickRandomMap)

[lbChatMessagesBase]
SolidColorBackgroundTexture=0,0,0,128
$Width=getWidth(lbMapList)
$X=LOBBY_EMPTY_SPACE_SIDES

[lbChatMessages_Host]
$BaseSection=lbChatMessagesBase
$Y=getBottom(btnPickRandomMap) + LOBBY_PANEL_SPACING
$Height=getBottom(MapPreviewBox) - (getBottom(btnPickRandomMap) + LOBBY_PANEL_SPACING)

[lbChatMessages_Player]
$BaseSection=lbChatMessagesBase
$Y=EMPTY_SPACE_TOP
$Height=getBottom(MapPreviewBox) - getY($Self)

[tbChatInputBase]
Suggestion=Type here to chat...
$Width=getWidth(lbMapList)
$Height=DEFAULT_CONTROL_HEIGHT
$X=LOBBY_EMPTY_SPACE_SIDES
$Y=getBottom(MapPreviewBox) + LOBBY_PANEL_SPACING

[tbChatInput_Host]
$BaseSection=tbChatInputBase

[tbChatInput_Player]
$BaseSection=tbChatInputBase

[btnLockGame]
$Width=BUTTON_WIDTH_133
$Height=DEFAULT_BUTTON_HEIGHT
$X=getRight(btnLaunchGame) + LOBBY_PANEL_SPACING
$Y=getY(btnLaunchGame)

[chkAutoReady]
Text=Auto-Ready
$X=getRight(btnLaunchGame) + LOBBY_PANEL_SPACING
$Y=getY(btnLaunchGame) + 2
Enabled=true
Visible=true

[GameOptionsPanel]
$CC-GODD03=cmbGameSpeedCapMultiplayer:GameLobbyDropDown

[cmbGameSpeedCapMultiplayer]
$BaseSection=cmbGameSpeedCap
Items=Fastest (60 FPS),Faster (45 FPS),Fast (30 FPS),Medium (20 FPS),Slow (15 FPS),Slower (12 FPS),Slowest (10 FPS)
```

</details>

## Create `CnCNetGameLobby.ini`

This file extends the multiplayer game lobby with CnCNet-specific controls, like the change tunnel button. **Remove (or port) previous content of this file.**

```ini
[INISystem]
BasedOn=MultiplayerGameLobby.ini

[MultiplayerGameLobby]
$CCMP99=btnChangeTunnel:XNAClientButton

[btnChangeTunnel]
Text=Change Tunnel
$Width=BUTTON_WIDTH_133
$Height=DEFAULT_BUTTON_HEIGHT
$X=getX(btnLeaveGame) - getWidth($Self) - LOBBY_PANEL_SPACING
$Y=getY(btnLeaveGame)
```

## Create `LANGameLobby.ini`

This stub file can extend the multiplayer lobby with LAN-specifc controls. **Remove (or port) previous content of this file.**

```ini
[INISystem]
BasedOn=MultiplayerGameLobby.ini
```

## Edit `GameOptions.ini`

After adding all game lobby options to `GameLobbyBase.ini`, remove them here. Remove `[SkirmishLobby]` and `[MultiplayerGameLobby]` sections, too.

## Edit `GenericWindow.ini`

Replace `[SkirmishLobby]` and `[MultiplayerGameLobby]` with this:

```ini
[GameLobbyBase]
BackgroundTexture=gamelobbybg.png
DrawBorders=true
Size=1230,750
```

## Edit `GlobalThemeSettings.ini`

This file now also contains the `ParserConstants` section, which lists user-defined constants used for positioning controls within panels and windows. **Without this section, the client will crash with new `GameLobbyBase.ini` layout**.

Add the following:

```ini
[ParserConstants]
DEFAULT_LBL_HEIGHT=12
DEFAULT_CONTROL_HEIGHT=21
DEFAULT_BUTTON_HEIGHT=23

BUTTON_WIDTH_133=133

OPEN_BUTTON_WIDTH=18
OPEN_BUTTON_HEIGHT=22 ;18

EMPTY_SPACE_TOP=12
EMPTY_SPACE_BOTTOM=12
EMPTY_SPACE_SIDES=12
BUTTON_SPACING=12
LABEL_SPACING=6
CHECKBOX_SPACING=24

LOBBY_EMPTY_SPACE_SIDES=12
LOBBY_PANEL_SPACING=10

GAME_OPTION_COLUMN_SPACING=160
GAME_OPTION_ROW_SPACING=6
GAME_OPTION_DD_WIDTH=132
GAME_OPTION_DD_HEIGHT=22
```

## Create `ManualUpdateQueryWindow.ini`

It is now possible to force a manual query for game/mod updates, which displays a new window.

```ini
[INISystem]
BasedOn=GenericWindow.ini

[btnClose]
Location=176,110
```

## Edit `OptionsWindow.ini`

New checkboxes have been added in the options window.

1. Add sections:

```ini
[lblPlayerName]
Location=12,195

[tbPlayerName]
Location=113,193

[lblNotice]
Location=12,220

[btnConfigureHotkeys]
Location=12,290

[chkDisablePrivateMessagePopup]
Location=12,138
Text=Disable private message pop-ups

[chkAllowGameInvitesFromFriendsOnly]
Location=276,68
Text=Only receive game invitations@from friends

[lblAllPrivateMessagesFrom]
Location=276,138

[ddAllowPrivateMessagesFrom]
Location=470,137

[gameListPanel]
Location=0,200

[btnForceUpdate]
```

2. **OPTIONAL** Add sections:

```ini
[DisplayOptionsPanelExtraControls]
0=chkMEDDraw:FileSettingCheckBox

[chkMEDDraw]
Location=285,147
Text=Enable DDWrapper for map editor
ToolTip=Enables DirectDraw wrapper & emulation for map editor.@Turning this option on can help if you are encountering problems with editor viewport not displaying or being laggy. 
EnabledFile0=Resources/Compatibility/DLL/ddwrapper.dll,Map Editor/ddraw32.dll,OverwriteOnMismatch
EnabledFile1=Resources/Compatibility/Configs/aqrit.cfg,Map Editor/aqrit.cfg,KeepChanges
DefaultValue=false
SettingSection=Video
SettingKey=UseDDWrapperForMapEditor
```

3. **OPTIONAL (YR+Phobos)** Add sections:

```ini
[GameOptionsPanelExtraControls]
; Only available with Phobos
0=chkTooltipsExtra:SettingCheckBox
1=chkPrioritySelection:SettingCheckBox
2=chkBuildingPlacement:SettingCheckBox

[chkTooltipsExtra]
Location=24,151, ;12,151
Text=Sidebar Tooltip Descriptions
ToolTip=Enables additional information in sidebar tooltips.
DefaultValue=true
ParentCheckBoxName=chkTooltips
ParentCheckBoxRequiredValue=true
SettingSection=Phobos
SettingKey=ToolTipDescriptions

[chkPrioritySelection]
Location=242,54
Text=Mass Selection Filtering
ToolTip=If enabled, non-combat units are not selected if mass-selecting together with combat units.
DefaultValue=false
SettingSection=Phobos
SettingKey=PrioritySelectionFiltering

[chkBuildingPlacement]
Location=242,78
Text=Show Building Placement Preview
ToolTip=If enabled, shows a preview image of the building when placing it.
DefaultValue=false
SettingSection=Phobos
SettingKey=ShowBuildingPlacementPreview
```

## Create new `PlayerExtraOptionsPanel.ini`

A new panel that allows for convenient match setup has been added in the game lobby.

```ini
[btnClose]
Location=220,0
Size=18,18

[lblHeader]
Location=12,6

[chkBoxForceRandomSides]
Location=12,28

[chkBoxForceRandomColors]
Location=12,50

[chkBoxForceRandomTeams]
Location=12,72

[chkBoxForceRandomStarts]
Location=12,94

[chkBoxUseTeamStartMappings]
Location=12,130

[btnHelp]
Location=160,130

[lblPreset]
Location=12,156

[ddTeamStartMappingPreset]
Size=157,21
Location=65,154

[teamStartMappingsPanel]
Location=12,189
```

## Appendix

For completion's sake, below are additional steps required for a complete migration (beyond INI changes) to client version [2.11.0.0][client_2.11] from pre-2.11.0.0.

### Update client binary files

1. Replace `clientdx.exe`, `clientogl.exe` and `clientxna.exe` in `Resources` with new files. Compiled `.pdb` and `.config` files are optional.
2. Replace contents of `Resources/Binaries` with new files. This directory contains the .NET Framework 4.8 version of the client.
3. **OPTIONAL** Copy contents of downloaded `BinariesNET8` into a new directory `Resources/BinariesNET8`. This directory contains the .NET 8 version of the client that enabled experimental cross-platform Unix support.

The `Resources` directory should look like this (omitting configuration files and assets):

```plaintext
<game dir>/Resources     # override the `Resources` folder to update the client binaries
├── Binaries             # this folder contains partial .NET 4.8 client files
├── BinariesNET8         # this folder contains .NET 8.0 client files, where modders can either delete it, or keep it for an experimental cross-platform support
├── clientdx.exe         # .NET 4.8 client main executable
├── clientdx.exe.config  # distributed along with `.exe` file. Can be removed but it is better to keep it.
├── clientdx.pdb         # .pdb file contains debug symbols. It can be either deleted or retained.
├── clientogl.exe        # .NET 4.8 client main executable
├── clientogl.exe.config # same as above
├── clientogl.pdb        # same as above
├── clientxna.exe        # .NET 4.8 client main executable
├── clientxna.exe.config # same as above
└── clientxna.pdb        # same as above
```

### Update the client launcher

The client launcher (that resides in the game directory) has been updated. You can replace the old one with the latest version [here](https://github.com/CnCNet/xna-cncnet-client-launcher/releases). Remember to rename it from `CncNetLauncherStub.exe` to your launcher name, i.e. `YRLauncher.exe`, `MentalOmegaLauncher.exe`. Rename the `.config` file appropriately, i.e. `YRLauncher.exe.config`, `MentalOmegaLauncher.exe.config`.

### Keep the old second-stage updater

The second-stage updater (formerly `clientupdt.dat`) has been reworked as `SecondStageUpdater.exe`, and will be automatically copied to `Resources/Binaries/Updater` directory by the build script. The old updater will still work, but is no longer maintained. However, don't remove the old updater (`clientupdt.dat`) so that end-users are able to update via the old client.

### Add new assets

Every file here can be placed either in `Resources` or in theme directories:

- `favActive.png` and `favInactive.png`, 21x21 pixels
- `optionsButton.png`, `optionsButton_c.png`, `optionsButtonActive.png`, `optionsButtonActive_c.png`, `optionsButtonClose.png` and `optionsButtonClose_c.png`, 18x18 pixels
- `questionMark.png` and `questionMark_c.png`, 18x18 pixels
- `sortAlphaAsc.png`, `sortAlphaDesc.png` and `sortAlphaNone.png`, 21x21 pixels
- `statusAI.png`, `statusClear.png`, `statusEmpty.png`, `statusError.png`, `statusInProgress.png`, `statusOk.png`, `statusUnavailable.png`, `statusWarning.png`, 21x21 pixels

You can find example assets in the [YR mod base][mod_base].

[client_2.11]: https://github.com/CnCNet/xna-cncnet-client/releases/tag/2.11.0.0
[client_2.12]: https://github.com/CnCNet/xna-cncnet-client/releases/tag/2.12.0
[mod_base]: https://github.com/Starkku/cncnet-client-mod-base
