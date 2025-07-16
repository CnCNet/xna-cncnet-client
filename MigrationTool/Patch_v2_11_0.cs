using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Rampastring.Tools;

namespace MigrationTool;

internal class Patch_v2_11_0 : Patch
{
    public Patch_v2_11_0(string clientPath) : base(clientPath)
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
        {
            var genericWindowIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "GenericWindow.ini"));
            AddKeyWithLog(genericWindowIni, "GenericWindow", "DrawBorders", "false");
            if (genericWindowIni.SectionExists("ExtraControls"))
                genericWindowIni.GetSection("ExtraControls").SectionName = "$ExtraControls";
            genericWindowIni.WriteIniFile();
        }

        // Rename OptionsWindow.ini->[*]->{CustomSettingFileCheckBox -- > FileSettingCheckBox & CustomSettingFileDropDown --> FileSettingDropDown}
        {
            IniFile optionsWindowIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "OptionsWindow.ini"));
            foreach (var section in optionsWindowIni.GetSections())
            {
                foreach (var key in optionsWindowIni.GetSectionKeys(section))
                {
                    var value = optionsWindowIni.GetStringValue(section, key, string.Empty);

                    if (value.Contains(":CustomSettingFileCheckBox"))
                    {
                        optionsWindowIni.SetStringValue(section, key, value.Replace(":CustomSettingFileCheckBox", ":FileSettingCheckBox"));
                        continue;
                    }

                    if (value.Contains(":CustomSettingFileDropDown"))
                    {
                        optionsWindowIni.SetStringValue(section, key, value.Replace(":CustomSettingFileDropDown", ":FileSettingDropDown"));
                        continue;
                    }
                }
            }

            // Add new sections into OptionsWindow.ini
            {
                var addKey = (string section, string key, string value) => AddKeyWithLog(optionsWindowIni, section, key, value);
                addKey("lblPlayerName", "Location", "12,195");
                addKey("tbPlayerName", "Location", "113,193");
                addKey("lblNotice", "Location", "12,220");
                addKey("btnConfigureHotkeys", "Location", "12,290");
                addKey("chkDisablePrivateMessagePopup", "Location", "12,138");
                addKey("chkDisablePrivateMessagePopup", "Text", "Disable private message pop-ups");
                addKey("chkAllowGameInvitesFromFriendsOnly", "Location", "276,68");
                addKey("chkAllowGameInvitesFromFriendsOnly", "Text", "Only receive game invitations@from friends");
                addKey("lblAllPrivateMessagesFrom", "Location", "276,138");
                addKey("ddAllowPrivateMessagesFrom", "Location", "470,137");
                addKey("gameListPanel", "Location", "0,200");
                addKey("btnForceUpdate", "Location", "407,213");
                addKey("btnForceUpdate", "Size", "133,23");
            }
            optionsWindowIni.WriteIniFile();
        }

        // Add DTACnCNetClient.ini
        {
            IniFile dtaCnCNetClientIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "DTACnCNetClient.ini"));
            var addKey = (string key, string value) => AddKeyWithLog(dtaCnCNetClientIni, "ParserConstants", key, value);
            addKey("DEFAULT_LBL_HEIGHT", "12");
            addKey("DEFAULT_CONTROL_HEIGHT", "21");
            addKey("DEFAULT_BUTTON_HEIGHT", "23");
            addKey("BUTTON_WIDTH_133", "133");
            addKey("OPEN_BUTTON_WIDTH", "18");
            addKey("OPEN_BUTTON_HEIGHT", "22");
            addKey("EMPTY_SPACE_TOP", "12");
            addKey("EMPTY_SPACE_BOTTOM", "12");
            addKey("EMPTY_SPACE_SIDES", "12");
            addKey("BUTTON_SPACING", "12");
            addKey("LABEL_SPACING", "6");
            addKey("CHECKBOX_SPACING", "24");
            addKey("LOBBY_EMPTY_SPACE_SIDES", "12");
            addKey("LOBBY_PANEL_SPACING", "10");
            addKey("GAME_OPTION_COLUMN_SPACING", "160");
            addKey("GAME_OPTION_ROW_SPACING", "6");
            addKey("GAME_OPTION_DD_WIDTH", "132");
            addKey("GAME_OPTION_DD_HEIGHT", "22");
            dtaCnCNetClientIni.WriteIniFile();
        }

        // Add PlayerExtraOptionsPanel.ini
        {
            IniFile playerExtraOptionsPanelIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "PlayerExtraOptionsPanel.ini"));
            var addKey = (string section, string key, string value) => AddKeyWithLog(playerExtraOptionsPanelIni, section, key, value);
            addKey("btnClose", "Location", "220,0");
            addKey("btnClose", "Size", "18,18");
            addKey("lblHeader", "Location", "12,6");
            addKey("chkBoxForceRandomSides", "Location", "12,28");
            addKey("chkBoxForceRandomColors", "Location", "12,50");
            addKey("chkBoxForceRandomTeams", "Location", "12,72");
            addKey("chkBoxForceRandomStarts", "Location", "12,94");
            addKey("chkBoxUseTeamStartMappings", "Location", "12,130");
            addKey("btnHelp", "Location", "160,130");
            addKey("lblPreset", "Location", "12,156");
            addKey("ddTeamStartMappingPreset", "Location", "65,154");
            addKey("ddTeamStartMappingPreset", "Size", "157,21");
            addKey("teamStartMappingsPanel", "Location", "12,189");
            playerExtraOptionsPanelIni.WriteIniFile();
        }

        string GameLobbyBase = nameof(GameLobbyBase);

        // Rework skirmish/lan/cncnet lobbies ini's
        if (File.Exists(SafePath.CombineFilePath(ResouresDir.FullName, $"{GameLobbyBase}.ini")))
        {
            Logger.Log($"Update lobbies has been aborted, {GameLobbyBase}.ini already exists");
        }
        else
        {
            string MultiplayerGameLobby = nameof(MultiplayerGameLobby);
            string SkirmishLobby = nameof(SkirmishLobby);
            string ExtraControls = nameof(ExtraControls);
            List<string> gameOptionsIniControlKeys = new() { "CheckBoxes", "DropDowns", "Labels" };

            // Old configs
            IniFile skirmishLobbyIni_old = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, $"{SkirmishLobby}.ini"));
            IniFile multiplayerGameLobbyIni_old = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, $"{MultiplayerGameLobby}.ini"));
            IniFile gameOptionsIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "GameOptions.ini"));

            // New configs
            IniFile gameLobbyBaseIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, $"{GameLobbyBase}.ini"));
            IniFile skirmishLobbyIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, $"{SkirmishLobby}_New.ini"));
            IniFile multiplayerGameLobbyIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, $"{MultiplayerGameLobby}_New.ini"));
            IniFile lanGameLobbyIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "LANGameLobby.ini"));
            IniFile cncnetGameLobbyIni = new IniFile(SafePath.CombineFilePath(ResouresDir.FullName, "CnCNetGameLobby.ini"));

            // Add random color to the GameOptions.ini
            AddKeyWithLog(gameOptionsIni, "General", "RandomColor", "168,168,168");

            // Delete old inheritance
            if (skirmishLobbyIni_old.SectionExists("INISystem"))
                skirmishLobbyIni_old.RemoveSection("INISystem");
            if (multiplayerGameLobbyIni_old.SectionExists("INISystem"))
                multiplayerGameLobbyIni_old.RemoveSection("INISystem");

            // Add inheritance
            //AddKeyWithLog(gameLobbyBaseIni,        "INISystem", "BasedOn", "GenericWindow.ini");
            AddKeyWithLog(skirmishLobbyIni, "INISystem", "BasedOn", $"{GameLobbyBase}.ini");
            AddKeyWithLog(multiplayerGameLobbyIni, "INISystem", "BasedOn", $"{GameLobbyBase}.ini");
            AddKeyWithLog(lanGameLobbyIni, "INISystem", "BasedOn", $"{MultiplayerGameLobby}.ini");
            AddKeyWithLog(cncnetGameLobbyIni, "INISystem", "BasedOn", $"{MultiplayerGameLobby}.ini");

            // Transfer old SkirmishLobby.ini->[ExtraControls] to new SkirmishLobby.ini->[$ExtraControls]
            if (skirmishLobbyIni_old.SectionExists($"{ExtraControls}"))
            {
                TransferKeys(skirmishLobbyIni_old, $"{ExtraControls}", skirmishLobbyIni, $"${ExtraControls}");
                skirmishLobbyIni_old.RemoveSection($"{ExtraControls}");

                foreach (var key in skirmishLobbyIni.GetSectionKeys($"${ExtraControls}"))
                {
                    var section = skirmishLobbyIni.GetStringValue($"${ExtraControls}", key, string.Empty).Split(':')[0];
                    TransferKeys(skirmishLobbyIni_old, section, skirmishLobbyIni);
                    skirmishLobbyIni_old.RemoveSection(section);
                }
            }

            // Configure GameLobbyBase.ini
            {
                // Add [SkirmishLobby]
                {
                    var addKey = (string key) => AddKeyWithLog(gameLobbyBaseIni, $"{SkirmishLobby}", key, gameOptionsIni.GetStringValue($"{SkirmishLobby}", key, string.Empty));
                    addKey("PlayerOptionLocationX");
                    addKey("PlayerOptionLocationY");
                    addKey("PlayerOptionVerticalMargin");
                    addKey("PlayerOptionHorizontalMargin");
                    addKey("PlayerOptionCaptionLocationY");
                    addKey("PlayerNameWidth");
                    addKey("SideWidth");
                    addKey("ColorWidth");
                    addKey("StartWidth");
                    addKey("TeamWidth");
                }
                TransferKeys(skirmishLobbyIni_old, $"{SkirmishLobby}", gameLobbyBaseIni);
                AddKeyWithLog(gameLobbyBaseIni, $"{SkirmishLobby}", "$CC-SK-GOP", "GameOptionsPanel:XNAPanel");
                skirmishLobbyIni_old.RemoveSection($"{SkirmishLobby}");

                TransferKeys(skirmishLobbyIni_old, "GameOptionsPanel", gameLobbyBaseIni);
                skirmishLobbyIni_old.RemoveSection("GameOptionsPanel");

                // Transfer checkboxes, dropdowns, labels from GameOptions.ini to GameLobbyBase.ini->[GameOptionsPanel]
                int outerIndex = 0;
                foreach (var itemName in gameOptionsIniControlKeys)
                {
                    string itemType = itemName switch
                    {
                        "CheckBoxes" => "GameLobbyCheckBox",
                        "DropDowns" => "GameLobbyDropDown",
                        "Labels" => "XNALabel",
                        _ => throw new Exception($"Unknown type of elements {itemName}")
                    };

                    string[] items = gameOptionsIni.GetStringValue($"{SkirmishLobby}", itemName, string.Empty).Split([","], StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < items.Length; i++)
                    {
                        var item = items[i];
                        AddKeyWithLog(gameLobbyBaseIni, "GameOptionsPanel", $"$CC_{i + outerIndex}", $"{item}:{itemType}");
                        TransferKeys(gameOptionsIni, item, gameLobbyBaseIni);
                        CalculatePositions(gameLobbyBaseIni, "GameOptionsPanel", item);
                    }

                    outerIndex += items.Length;
                }

                // Add other elements in GameLobbyBase.ini->[SkirmishLobby]
                {
                    var addControl = (string controlKey, string section, string controlType) =>
                    {
                        AddKeyWithLog(gameLobbyBaseIni, $"{SkirmishLobby}", controlKey, $"{section}:{controlType}");
                        try
                        {
                            TransferKeys(skirmishLobbyIni_old, section, gameLobbyBaseIni);
                        }
                        catch
                        {
                            gameLobbyBaseIni.AddSection(section);
                        }
                        skirmishLobbyIni_old.RemoveSection(section);
                    };

                    addControl("$CC-SK00", "btnLaunchGame", "GameLaunchButton");
                    addControl("$CC-SK01", "MapPreviewBox", "MapPreviewBox");
                    addControl("$CC-SK02", "PlayerOptionsPanel", "XNAPanel");
                    addControl("$CC-SK03", "ddGameMode", "XNAClientDropDown");
                    addControl("$CC-SK04", "tbMapSearch", "XNASuggestionTextBox");
                    addControl("$CC-SK05", "btnPickRandomMap", "XNAClientButton");
                    addControl("$CC-SK06", "lblGameModeSelect", "XNALabel");
                    addControl("$CC-SK07", "lbMapList", "XNAMultiColumnListBox");
                    addControl("$CC-SK08", "lblMapSize", "XNALabel");
                    addControl("$CC-SK09", "lblGameMode", "XNALabel");
                    addControl("$CC-SK10", "lblMapAuthor", "XNALabel");
                    addControl("$CC-SK11", "lblMapName", "XNALabel");
                    addControl("$CC-SK12", "btnLeaveGame", "XNAClientButton");

                    AddKeyWithLog(gameLobbyBaseIni, $"{SkirmishLobby}", "$CC-SK13", "btnSaveLoadGameOptions:XNAClientButton");
                    AddKeyWithLog(gameLobbyBaseIni, "btnSaveLoadGameOptions", "IdleTexture", "comboBoxArrow.png");
                    AddKeyWithLog(gameLobbyBaseIni, "btnSaveLoadGameOptions", "HoverTexture", "comboBoxArrow.png");
                    AddKeyWithLog(gameLobbyBaseIni, "btnSaveLoadGameOptions", "$Width", "18");
                    AddKeyWithLog(gameLobbyBaseIni, "btnSaveLoadGameOptions", "$Height", "21");
                    AddKeyWithLog(gameLobbyBaseIni, "btnSaveLoadGameOptions", "$X", "getRight(GameOptionsPanel) - getWidth($Self) - 1");
                    AddKeyWithLog(gameLobbyBaseIni, "btnSaveLoadGameOptions", "$Y", "getY(GameOptionsPanel) + 1");

                    skirmishLobbyIni_old.GetSections().ForEach(x => TransferKeys(skirmishLobbyIni_old, x, gameLobbyBaseIni));
                }
            }

            // Transfer old MultiplayerGameLobby.ini->[ExtraControls] to new MultiplayerGameLobby.ini->[$ExtraControls]
            if (multiplayerGameLobbyIni_old.SectionExists($"{ExtraControls}"))
            {
                TransferKeys(multiplayerGameLobbyIni_old, $"{ExtraControls}", multiplayerGameLobbyIni, $"${ExtraControls}");
                multiplayerGameLobbyIni_old.RemoveSection($"{ExtraControls}");
                foreach (var key in multiplayerGameLobbyIni.GetSectionKeys($"${ExtraControls}"))
                {
                    var value = multiplayerGameLobbyIni.GetStringValue($"${ExtraControls}", key, string.Empty).Split(':')[0];
                    TransferKeys(multiplayerGameLobbyIni_old, value, multiplayerGameLobbyIni);
                    multiplayerGameLobbyIni_old.RemoveSection(value);
                }
            }

            // Configure MultiplayerGameLobby.ini
            {
                AddKeyWithLog(multiplayerGameLobbyIni, $"{MultiplayerGameLobby}", "$BaseSection", $"{SkirmishLobby}");

                // Add keys into [MultiplayerGameLobby] if values are changed with comparison to [SkirmishLobby]
                foreach (var key in gameLobbyBaseIni.GetSectionKeys($"{SkirmishLobby}").Where(elem => !elem.StartsWith("$")))
                {
                    var valueSkirmish = gameLobbyBaseIni.GetStringValue($"{SkirmishLobby}", key, string.Empty);
                    var valueMultiplayer = gameOptionsIni.GetStringValue($"{MultiplayerGameLobby}", key, string.Empty);

                    if (valueMultiplayer != valueSkirmish)
                        AddKeyWithLog(multiplayerGameLobbyIni, $"{MultiplayerGameLobby}", key, valueMultiplayer);
                }

                // Find controls to exclude and include
                List<string> skirmishControls = new();
                List<string> multiplayerControls = new();
                gameOptionsIniControlKeys.ForEach(x => skirmishControls.AddRange(gameOptionsIni.GetStringValue($"{SkirmishLobby}", x, string.Empty).Split(',')));
                gameOptionsIniControlKeys.ForEach(x => multiplayerControls.AddRange(gameOptionsIni.GetStringValue($"{MultiplayerGameLobby}", x, string.Empty).Split(',')));
                var excludeControls = skirmishControls.Except(multiplayerControls).ToList();
                var addControls = multiplayerControls.Except(skirmishControls).ToList();

                // Disable skirmish lobby only controls from GameOptions.ini
                excludeControls.ForEach(x =>
                    AddKeyWithLog(multiplayerGameLobbyIni, x, "Visible", "false")
                    .AddKeyWithLog(multiplayerGameLobbyIni, x, "Enabled", "false"));

                // Add multiplayer lobby only controls from GameOptions.ini
                addControls.ForEach(control =>
                    AddKeyWithLog(
                        multiplayerGameLobbyIni,
                        "GameOptionsPanel",
                        $"$CC-M{addControls.IndexOf(control)}",
                        control + ':' + control.Substring(0, 3) switch
                        {
                            "chk" => "GameLobbyCheckBox",
                            "cmb" => "GameLobbyDropDown",
                            "lbl" => "XNALabel",
                            _ => throw new Exception($"GameOptions.ini contains unknown type of contol with name {control}")
                        })
                    .TransferKeys(gameOptionsIni, control, multiplayerGameLobbyIni)
                    .CalculatePositions(multiplayerGameLobbyIni, "GameOptionsPanel", control));

                // Add other elements in MultiplayerGameLobby.ini->[MultiplayerGameLobby]
                {
                    var addControl = (string controlKey, string section, string controlType) =>
                    {
                        AddKeyWithLog(multiplayerGameLobbyIni, $"{MultiplayerGameLobby}", controlKey, $"{section}:{controlType}");
                        try
                        {
                            TransferKeys(multiplayerGameLobbyIni_old, section, multiplayerGameLobbyIni);
                        }
                        catch
                        {
                            multiplayerGameLobbyIni.AddSection(section);
                        }
                        multiplayerGameLobbyIni_old.RemoveSection(section);
                    };

                    addControl("$CC-MP01", "btnLockGame", "XNAClientButton");
                    addControl("$CC-MP02", "lbChatMessages_Host", "ChatListBox");
                    addControl("$CC-MP03", "lbChatMessages_Player", "ChatListBox");
                    addControl("$CC-MP04", "tbChatInput_Host", "XNAChatTextBox");
                    addControl("$CC-MP05", "tbChatInput_Player", "XNAChatTextBox");
                    addControl("$CC-MP06", "chkAutoReady", "XNAClientCheckBox");

                    AddKeyWithLog(multiplayerGameLobbyIni, $"{MultiplayerGameLobby}", "$CC-MP07", "lbChatMessages:ChatListBox");
                    AddKeyWithLog(multiplayerGameLobbyIni, $"{MultiplayerGameLobby}", "$CC-MP08", "tbChatInput:XNAChatTextBox");
                    TransferKeys(multiplayerGameLobbyIni, "lbChatMessages_Player", multiplayerGameLobbyIni, "lbChatMessages");
                    TransferKeys(multiplayerGameLobbyIni, "tbChatInput_Player", multiplayerGameLobbyIni, "tbChatInput");

                    multiplayerGameLobbyIni_old.GetSections().ForEach(x => TransferKeys(multiplayerGameLobbyIni_old, x, multiplayerGameLobbyIni));
                }
            }

            // Configure CnCNetGameLobby.ini
            AddKeyWithLog(cncnetGameLobbyIni, $"{MultiplayerGameLobby}", "$CC-MP99", "btnChangeTunnel:XNAClientButton");
            AddKeyWithLog(cncnetGameLobbyIni, "btnChangeTunnel", "$Width", "133");
            AddKeyWithLog(cncnetGameLobbyIni, "btnChangeTunnel", "$X", "getX(btnLeaveGame) - getWidth($Self) - BUTTON_SPACING");
            AddKeyWithLog(cncnetGameLobbyIni, "btnChangeTunnel", "$Y", "getY(btnLaunchGame)");
            AddKeyWithLog(cncnetGameLobbyIni, "btnChangeTunnel", "Text", "Change Tunnel");

            // Remove empty keys
            foreach (var ini in new List<IniFile>() { gameLobbyBaseIni, multiplayerGameLobbyIni, skirmishLobbyIni, lanGameLobbyIni, cncnetGameLobbyIni })
            {
                foreach (var section in ini.GetSections())
                {
                    ini.GetSectionKeys(section)
                        .Where(key => string.IsNullOrWhiteSpace(ini.GetStringValue(section, key, string.Empty)))
                        .ToList()
                        .ForEach(key => ini.RemoveKey(section, key));
                }
            }

            // Replace old configs with new one, delete placeholders, delete redundant sections
            var sb = new StringBuilder();
            gameOptionsIniControlKeys
                .ForEach(x => sb.Append(gameOptionsIni.GetStringValue($"{SkirmishLobby}", x, string.Empty)).Append(','));
            gameOptionsIniControlKeys
                .ForEach(x => sb.Append(gameOptionsIni.GetStringValue($"{MultiplayerGameLobby}", x, string.Empty)).Append(','));
            sb.ToString()
                .Split(',')
                .Distinct()
                .Select(x => x = x.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList()
                .ForEach(x => gameOptionsIni.RemoveSection(x));

            gameOptionsIni.RemoveSection($"{SkirmishLobby}");
            gameOptionsIni.RemoveSection($"{MultiplayerGameLobby}");
            SafePath.DeleteFileIfExists(SafePath.CombineFilePath(ResouresDir.FullName, $"{SkirmishLobby}.ini"));
            SafePath.DeleteFileIfExists(SafePath.CombineFilePath(ResouresDir.FullName, $"{MultiplayerGameLobby}.ini"));
            skirmishLobbyIni.WriteIniFile(SafePath.CombineFilePath(ResouresDir.FullName, skirmishLobbyIni_old.FileName));
            multiplayerGameLobbyIni.WriteIniFile(SafePath.CombineFilePath(ResouresDir.FullName, multiplayerGameLobbyIni_old.FileName));

            gameOptionsIni.WriteIniFile();
            gameLobbyBaseIni.WriteIniFile();
            lanGameLobbyIni.WriteIniFile();
            cncnetGameLobbyIni.WriteIniFile();
        }

        // Add new texture files
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var resourceName in assembly.GetManifestResourceNames())
            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                var filename = resourceName.Replace($"{nameof(MigrationTool)}.Pictures.", string.Empty);
                var filepath = SafePath.CombineFilePath(ResouresDir.FullName, filename);

                if (!File.Exists(filepath))
                {
                    using (FileStream fileStream = new FileStream(filepath, FileMode.CreateNew))
                    {
                        Logger.Log($"Copy {filename} to the {ResouresDir.FullName}");
                        resourceStream.CopyTo(fileStream);
                    }
                }
            }

        return this;
    }
}
