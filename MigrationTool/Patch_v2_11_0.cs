using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rampastring.Tools;
namespace MigrationTool;

internal class Patch_v2_11_0 : Patch
{
    public Patch_v2_11_0 (string clientPath) : base (clientPath)
    {
        ClientVersion = Version.v2_11_0;
    }

    public override Patch Apply()
    {
        base.Apply();

        // Remove Rampastring.Tools from Resources directory (not recursive)
        Logger.Log("Remove Resources\\Rampastring.Tools.* (* -- dll, pdb, xml)");
        SafePath.DeleteFileIfExists(ResouresDir.FullName, "Rampastring.Tools.dll");
        SafePath.DeleteFileIfExists(ResouresDir.FullName, "Rampastring.Tools.pdb");
        SafePath.DeleteFileIfExists(ResouresDir.FullName, "Rampastring.Tools.xml");
        
        // Add GenericWindow.ini->[GenericWindow]->DrawBorders=false
        var genericWindowIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "GenericWindow.ini"));
        AddKeyWithLog(genericWindowIni, "GenericWindow", "DrawBorders", "false");
        if (genericWindowIni.SectionExists("ExtraControls"))
            genericWindowIni.GetSection("ExtraControls").SectionName = "$ExtraControls";
        genericWindowIni.WriteIniFile();
        
        // Rename OptionsWindow.ini->[*]->{CustomSettingFileCheckBox -- > FileSettingCheckBox & CustomSettingFileDropDown --> FileSettingDropDown}
        IniFile optionsWindowIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "OptionsWindow.ini"));
        foreach (var section in optionsWindowIni.GetSections())
            foreach (var pair in optionsWindowIni.GetSection(section).Keys)
            {
                if (pair.Value.Contains(":CustomSettingFileCheckBox"))
                {
                    pair.Value.Replace(":CustomSettingFileCheckBox", ":FileSettingCheckBox");
                    continue;
                }
        
                if (pair.Value.Contains(":CustomSettingFileDropDown"))
                {
                    pair.Value.Replace(":CustomSettingFileDropDown", ":FileSettingDropDown");
                    continue;
                }
            }

        // Add new sections into OptionsWindow.ini
        {
            var addKey = (string section, string key, string value) => AddKeyWithLog(optionsWindowIni, section, key, value);
            addKey("lblPlayerName",                      "Location", "12,195");
            addKey("tbPlayerName",                       "Location", "113,193");
            addKey("lblNotice",                          "Location", "12,220");
            addKey("btnConfigureHotkeys",                "Location", "12,290");
            addKey("chkDisablePrivateMessagePopup",      "Location", "12,138");
            addKey("chkDisablePrivateMessagePopup",      "Text",     "Disable private message pop-ups");
            addKey("chkAllowGameInvitesFromFriendsOnly", "Location", "276,68");
            addKey("chkAllowGameInvitesFromFriendsOnly", "Text",     "Only receive game invitations@from friends");
            addKey("lblAllPrivateMessagesFrom",          "Location", "276,138");
            addKey("ddAllowPrivateMessagesFrom",         "Location", "470,137");
            addKey("gameListPanel",                      "Location", "0,200");
            addKey("btnForceUpdate",                     "Location", "407,213");
            addKey("btnForceUpdate",                     "Size",     "133,23");
        }
        optionsWindowIni.WriteIniFile();
        
        // Add GlobalThemeSettings.ini
        {
            IniFile globalThemeSettingsIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "GlobalThemeSettings.ini"));
            var addKey = (string key, string value) => AddKeyWithLog(globalThemeSettingsIni, "ParserConstants", key, value);
            addKey("DEFAULT_LBL_HEIGHT",         "12");
            addKey("DEFAULT_CONTROL_HEIGHT",     "21");
            addKey("DEFAULT_BUTTON_HEIGHT",      "23");
            addKey("BUTTON_WIDTH_133",           "133");
            addKey("OPEN_BUTTON_WIDTH",          "18");
            addKey("OPEN_BUTTON_HEIGHT",         "22");
            addKey("EMPTY_SPACE_TOP",            "12");
            addKey("EMPTY_SPACE_BOTTOM",         "12");
            addKey("EMPTY_SPACE_SIDES",          "12");
            addKey("BUTTON_SPACING",             "12");
            addKey("LABEL_SPACING",              "6");
            addKey("CHECKBOX_SPACING",           "24");
            addKey("LOBBY_EMPTY_SPACE_SIDES",    "12");
            addKey("LOBBY_PANEL_SPACING",        "10");
            addKey("GAME_OPTION_COLUMN_SPACING", "160");
            addKey("GAME_OPTION_ROW_SPACING",    "6");
            addKey("GAME_OPTION_DD_WIDTH",       "132");
            addKey("GAME_OPTION_DD_HEIGHT",      "22");
            globalThemeSettingsIni.WriteIniFile();
        }

        // Add PlayerExtraOptionsPanel.ini
        {
            IniFile playerExtraOptionsPanelIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "PlayerExtraOptionsPanel.ini"));
            var addKey = (string section, string key, string value) => AddKeyWithLog(playerExtraOptionsPanelIni, section, key, value);
            addKey("btnClose",                   "Location", "220,0");
            addKey("btnClose",                   "Size",     "18,18");
            addKey("lblHeader",                  "Location", "12,6");
            addKey("chkBoxForceRandomSides",     "Location", "12,28");
            addKey("chkBoxForceRandomColors",    "Location", "12,50");
            addKey("chkBoxForceRandomTeams",     "Location", "12,72");
            addKey("chkBoxForceRandomStarts",    "Location", "12,94");
            addKey("chkBoxUseTeamStartMappings", "Location", "12,130");
            addKey("btnHelp",                    "Location", "160,130");
            addKey("lblPreset",                  "Location", "12,156");
            addKey("ddTeamStartMappingPreset",   "Location", "65,154");
            addKey("ddTeamStartMappingPreset",   "Size",     "157,21");
            addKey("teamStartMappingsPanel",     "Location", "12,189");
            playerExtraOptionsPanelIni.WriteIniFile();
        }

        // Rework skirmish/lan/cncnet lobbies ini's
        if (!File.Exists(SafePath.CombineFilePath(ResouresDir.FullName, "GameLobbyBase.ini")))
        {
            // Old configs
            IniFile skirmishLobbyIni_old = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "SkirmishLobby.ini"));
            IniFile multiplayerGameLobby_old = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "MultiplayerGameLobby.ini"));
            IniFile gameOptionsIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "GameOptions.ini"));

            // New configs
            IniFile gameLobbyBaseIni        = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "GameLobbyBase.ini"));
            IniFile skirmishLobbyIni        = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "SkirmishLobby_New.ini"));
            IniFile multiplayerGameLobbyIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "MultiplayerGameLobby_New.ini"));
            IniFile lanGameLobbyIni         = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "LANGameLobby.ini"));
            IniFile cncnetGameLobbyIni      = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "CnCNetGameLobby.ini"));

            // Add random color to the GameOptions.ini
            AddKeyWithLog(gameOptionsIni, "General", "RandomColor", "168,168,168");

            // Add inheritance
            AddKeyWithLog(gameLobbyBaseIni,        "INISystem", "BasedOn", "GenericWindow");
            AddKeyWithLog(skirmishLobbyIni,        "INISystem", "BasedOn", "GameLobbyBase");
            AddKeyWithLog(multiplayerGameLobbyIni, "INISystem", "BasedOn", "GameLobbyBase");
            AddKeyWithLog(lanGameLobbyIni,         "INISystem", "BasedOn", "MultiplayerGameLobby");
            AddKeyWithLog(cncnetGameLobbyIni,      "INISystem", "BasedOn", "MultiplayerGameLobby");

            // Configure GameLobbyBase.ini
            AddKeyWithLog(gameLobbyBaseIni, "SkirmishLobby", "PlayerOptionLocationX",        gameOptionsIni.GetStringValue("SkirmishLobby", "PlayerOptionLocationX", string.Empty));
            AddKeyWithLog(gameLobbyBaseIni, "SkirmishLobby", "PlayerOptionLocationY",        gameOptionsIni.GetStringValue("SkirmishLobby", "PlayerOptionLocationY", string.Empty));
            AddKeyWithLog(gameLobbyBaseIni, "SkirmishLobby", "PlayerOptionVerticalMargin",   gameOptionsIni.GetStringValue("SkirmishLobby", "PlayerOptionVerticalMargin", string.Empty));
            AddKeyWithLog(gameLobbyBaseIni, "SkirmishLobby", "PlayerOptionHorizontalMargin", gameOptionsIni.GetStringValue("SkirmishLobby", "PlayerOptionHorizontalMargin", string.Empty));
            AddKeyWithLog(gameLobbyBaseIni, "SkirmishLobby", "PlayerOptionCaptionLocationY", gameOptionsIni.GetStringValue("SkirmishLobby", "PlayerOptionCaptionLocationY", string.Empty));
            AddKeyWithLog(gameLobbyBaseIni, "SkirmishLobby", "PlayerNameWidth",              gameOptionsIni.GetStringValue("SkirmishLobby", "PlayerNameWidth", string.Empty));
            AddKeyWithLog(gameLobbyBaseIni, "SkirmishLobby", "SideWidth",                    gameOptionsIni.GetStringValue("SkirmishLobby", "SideWidth", string.Empty));
            AddKeyWithLog(gameLobbyBaseIni, "SkirmishLobby", "ColorWidth",                   gameOptionsIni.GetStringValue("SkirmishLobby", "ColorWidth", string.Empty));
            AddKeyWithLog(gameLobbyBaseIni, "SkirmishLobby", "StartWidth",                   gameOptionsIni.GetStringValue("SkirmishLobby", "StartWidth", string.Empty));
            AddKeyWithLog(gameLobbyBaseIni, "SkirmishLobby", "TeamWidth",                    gameOptionsIni.GetStringValue("SkirmishLobby", "TeamWidth", string.Empty));
            AddKeyWithLog(gameLobbyBaseIni, "SkirmishLobby", "$CC-GOP",                      "GameOptionsPanel:XNAPanel");

            AddKeyWithLog(gameLobbyBaseIni, "GameOptionsPanel", "SolidColorBackgroundTexture", "0,0,0,192");
            AddKeyWithLog(gameLobbyBaseIni, "GameOptionsPanel", "DrawBorders",                 "yes");
            AddKeyWithLog(gameLobbyBaseIni, "GameOptionsPanel", "$Width",                      "427");
            AddKeyWithLog(gameLobbyBaseIni, "GameOptionsPanel", "$Height",                     "266");
            AddKeyWithLog(gameLobbyBaseIni, "GameOptionsPanel", "$X",                          "getWidth($ParentControl) - getWidth($Self) - EMPTY_SPACE_SIDES");
            AddKeyWithLog(gameLobbyBaseIni, "GameOptionsPanel", "$Y",                          "EMPTY_SPACE_TOP");

            int outerIndex = 0;
            foreach (var itemName in new string[] { "CheckBoxes", "DropDowns", "Labels" })
            {
                string itemType = itemName switch
                {
                    "CheckBoxes" => "GameLobbyCheckBox",
                    "DropDowns"  => "GameLobbyDropDown",
                    "Labels"     => "XNALabel",
                    _            => throw new Exception($"Unknown type of elements {itemName}")
                };

                var items = gameOptionsIni.GetStringValue("SkirmishLobby", itemName, string.Empty).Split(',');
                for (int i = 0; i < items.Length; i++)
                {
                    var item = items[i];
                    AddKeyWithLog(gameLobbyBaseIni, "GameOptionsPanel", $"$CC_{i + outerIndex}", $"{item}:{itemType}");
                    TransferKeys(gameOptionsIni, item, gameLobbyBaseIni);
                }

                outerIndex += items.Length;
            }

            foreach (var section in skirmishLobbyIni_old.GetSections())
            {

            }

            // Configure MultiplayerGameLobby.ini


            // Configure CnCNetGameLobby.ini
            AddKeyWithLog(cncnetGameLobbyIni, "MultiplayerGameLobby", "$CCMP99", "btnChangeTunnel:XNAClientButton");
            AddKeyWithLog(cncnetGameLobbyIni, "btnChangeTunnel",      "$Width",  "133");
            AddKeyWithLog(cncnetGameLobbyIni, "btnChangeTunnel",      "$X",      "getX(btnLeaveGame) - getWidth($Self) - BUTTON_SPACING");
            AddKeyWithLog(cncnetGameLobbyIni, "btnChangeTunnel",      "$Y",      "getY(btnLaunchGame)");
            AddKeyWithLog(cncnetGameLobbyIni, "btnChangeTunnel",      "Text",    "Change Tunnel");

            // Replace new configs and delete placeholders
            skirmishLobbyIni.WriteIniFile(SafePath.CombineFilePath(ResouresDir.FullName, skirmishLobbyIni_old.FileName));
            multiplayerGameLobbyIni.WriteIniFile(SafePath.CombineFilePath(ResouresDir.FullName, multiplayerGameLobby_old.FileName));
            gameLobbyBaseIni.WriteIniFile();
            lanGameLobbyIni.WriteIniFile();
            cncnetGameLobbyIni.WriteIniFile();
            SafePath.DeleteFileIfExists(SafePath.CombineFilePath(ResouresDir.FullName, "SkirmishLobby_New.ini"));
            SafePath.DeleteFileIfExists(SafePath.CombineFilePath(ResouresDir.FullName, "MultiplayerGameLobby_New.ini"));
        }

        // Add new texture files

        return this;
    }
}
