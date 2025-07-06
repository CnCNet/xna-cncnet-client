using System;
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
        Log("Remove Resources\\Rampastring.Tools.* (* -- dll, pdb, xml)");
        SafePath.DeleteFileIfExists(ResouresDir.FullName, "Rampastring.Tools.dll");
        SafePath.DeleteFileIfExists(ResouresDir.FullName, "Rampastring.Tools.pdb");
        SafePath.DeleteFileIfExists(ResouresDir.FullName, "Rampastring.Tools.xml");
        
        // Add GlobalThemeSettings.ini
        IniFile globalThemeSettingsIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "GlobalThemeSettings.ini"));
        AddKeyWithLog(globalThemeSettingsIni, "ParserConstants", "DEFAULT_LBL_HEIGHT",         "12");
        AddKeyWithLog(globalThemeSettingsIni, "ParserConstants", "DEFAULT_CONTROL_HEIGHT",     "21");
        AddKeyWithLog(globalThemeSettingsIni, "ParserConstants", "DEFAULT_BUTTON_HEIGHT",      "23");
        AddKeyWithLog(globalThemeSettingsIni, "ParserConstants", "BUTTON_WIDTH_133",           "133");
        AddKeyWithLog(globalThemeSettingsIni, "ParserConstants", "OPEN_BUTTON_WIDTH",          "18");
        AddKeyWithLog(globalThemeSettingsIni, "ParserConstants", "OPEN_BUTTON_HEIGHT",         "22");
        AddKeyWithLog(globalThemeSettingsIni, "ParserConstants", "EMPTY_SPACE_TOP",            "12");
        AddKeyWithLog(globalThemeSettingsIni, "ParserConstants", "EMPTY_SPACE_BOTTOM",         "12");
        AddKeyWithLog(globalThemeSettingsIni, "ParserConstants", "EMPTY_SPACE_SIDES",          "12");
        AddKeyWithLog(globalThemeSettingsIni, "ParserConstants", "BUTTON_SPACING",             "12");
        AddKeyWithLog(globalThemeSettingsIni, "ParserConstants", "LABEL_SPACING",              "6");
        AddKeyWithLog(globalThemeSettingsIni, "ParserConstants", "CHECKBOX_SPACING",           "24");
        AddKeyWithLog(globalThemeSettingsIni, "ParserConstants", "LOBBY_EMPTY_SPACE_SIDES",    "12");
        AddKeyWithLog(globalThemeSettingsIni, "ParserConstants", "LOBBY_PANEL_SPACING",        "10");
        AddKeyWithLog(globalThemeSettingsIni, "ParserConstants", "GAME_OPTION_COLUMN_SPACING", "160");
        AddKeyWithLog(globalThemeSettingsIni, "ParserConstants", "GAME_OPTION_ROW_SPACING",    "6");
        AddKeyWithLog(globalThemeSettingsIni, "ParserConstants", "GAME_OPTION_DD_WIDTH",       "132");
        AddKeyWithLog(globalThemeSettingsIni, "ParserConstants", "GAME_OPTION_DD_HEIGHT",      "22");
        globalThemeSettingsIni.WriteIniFile();
        
        // Add PlayerExtraOptionsPanel.ini
        IniFile playerExtraOptionsPanelIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "PlayerExtraOptionsPanel.ini"));
        AddKeyWithLog(playerExtraOptionsPanelIni, "btnClose",                   "Location", "220,0");
        AddKeyWithLog(playerExtraOptionsPanelIni, "btnClose",                   "Size",     "18,18");
        AddKeyWithLog(playerExtraOptionsPanelIni, "lblHeader",                  "Location", "12,6");
        AddKeyWithLog(playerExtraOptionsPanelIni, "chkBoxForceRandomSides",     "Location", "12,28");
        AddKeyWithLog(playerExtraOptionsPanelIni, "chkBoxForceRandomColors",    "Location", "12,50");
        AddKeyWithLog(playerExtraOptionsPanelIni, "chkBoxForceRandomTeams",     "Location", "12,72");
        AddKeyWithLog(playerExtraOptionsPanelIni, "chkBoxForceRandomStarts",    "Location", "12,94");
        AddKeyWithLog(playerExtraOptionsPanelIni, "chkBoxUseTeamStartMappings", "Location", "12,130");
        AddKeyWithLog(playerExtraOptionsPanelIni, "btnHelp",                    "Location", "160,130");
        AddKeyWithLog(playerExtraOptionsPanelIni, "lblPreset",                  "Location", "12,156");
        AddKeyWithLog(playerExtraOptionsPanelIni, "ddTeamStartMappingPreset",   "Location", "65,154");
        AddKeyWithLog(playerExtraOptionsPanelIni, "ddTeamStartMappingPreset",   "Size",     "157,21");
        AddKeyWithLog(playerExtraOptionsPanelIni, "teamStartMappingsPanel",     "Location", "12,189");
        playerExtraOptionsPanelIni.WriteIniFile();
        
        // Add GenericWindow.ini->[GenericWindow]->DrawBorders=false
        var genericWindowIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "GenericWindow.ini"));
        AddKeyWithLog(playerExtraOptionsPanelIni, "GenericWindow", "DrawBorders", "false");
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
        AddKeyWithLog(optionsWindowIni, "lblPlayerName",                      "Location", "12,195");
        AddKeyWithLog(optionsWindowIni, "tbPlayerName",                       "Location", "113,193");
        AddKeyWithLog(optionsWindowIni, "lblNotice",                          "Location", "12,220");
        AddKeyWithLog(optionsWindowIni, "btnConfigureHotkeys",                "Location", "12,290");
        AddKeyWithLog(optionsWindowIni, "chkDisablePrivateMessagePopup",      "Location", "12,138");
        AddKeyWithLog(optionsWindowIni, "chkDisablePrivateMessagePopup",      "Text",     "Disable private message pop-ups");
        AddKeyWithLog(optionsWindowIni, "chkAllowGameInvitesFromFriendsOnly", "Location", "276,68");
        AddKeyWithLog(optionsWindowIni, "chkAllowGameInvitesFromFriendsOnly", "Text",     "Only receive game invitations@from friends");
        AddKeyWithLog(optionsWindowIni, "lblAllPrivateMessagesFrom",          "Location", "276,138");
        AddKeyWithLog(optionsWindowIni, "ddAllowPrivateMessagesFrom",         "Location", "470,137");
        AddKeyWithLog(optionsWindowIni, "gameListPanel",                      "Location", "0,200");
        AddKeyWithLog(optionsWindowIni, "btnForceUpdate",                     "Location", "407,213");
        AddKeyWithLog(optionsWindowIni, "btnForceUpdate",                     "Size",     "133,23");
        
        optionsWindowIni.WriteIniFile();
        // Add new texture files

        return this;
    }
}
