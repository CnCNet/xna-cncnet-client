using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClientCore;
using ClientGUI;
using ClientUpdater;
using ClientCore.Extensions;
using ClientCore.Enums;
using DTAClient.Domain;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System.Diagnostics;
using System.Globalization;
using DTAConfig.Settings;

namespace DTAClient.DXGUI.Generic
{
    public class CampaignSelector : INItializableWindow
    {
        private const int DEFAULT_WIDTH = 650;
        private const int DEFAULT_HEIGHT = 600;

        private static string[] DifficultyNames = new string[] { "Easy", "Medium", "Hard" };

        private static string[] DifficultyIniPaths = new string[]
        {
            "INI/Map Code/Difficulty Easy.ini",
            "INI/Map Code/Difficulty Medium.ini",
            "INI/Map Code/Difficulty Hard.ini"
        };

        public CampaignSelector(WindowManager windowManager, DiscordHandler discordHandler, CampaignTagSelector campaignTagSelector) : base(windowManager)
        {
            this.discordHandler = discordHandler;
            this.campaignTagSelector = campaignTagSelector;
        }

        private DiscordHandler discordHandler;
        private CampaignTagSelector campaignTagSelector;

        private List<Mission> lbCampaignListMissions = new List<Mission>();
        private XNAListBox lbCampaignList;
        private XNAClientButton btnLaunch;
        private XNAClientButton btnCancel;
        private XNAClientButton btnReturn;
        private XNATextBlock tbMissionDescription;
        private XNATrackbar trbDifficultySelector;
        private List<IUserSetting> userSettings = new List<IUserSetting>();

        private CheaterWindow cheaterWindow;

        private string[] filesToCheck = new string[]
        {
            "INI/AI.ini",
            "INI/AIE.ini",
            "INI/Art.ini",
            "INI/ArtE.ini",
            "INI/Enhance.ini",
            "INI/Rules.ini",
            "INI/Map Code/Difficulty Hard.ini",
            "INI/Map Code/Difficulty Medium.ini",
            "INI/Map Code/Difficulty Easy.ini"
        };

        private Mission missionToLaunch;

        private List<Mission> _allMissions = [];
        public IReadOnlyCollection<Mission> AllMissions { get => _allMissions; }

        private Dictionary<int, Mission> _uniqueIDToMissions = new();
        public IReadOnlyDictionary<int, Mission> UniqueIDToMissions => _uniqueIDToMissions;

        private void AddMission(Mission mission)
        {
            // no matter whether the key is duplicated, the mission is always added to AllMissions
            _allMissions.Add(mission);

            // but only the first mission is recorded in UniqueIDToMissions
            if (_uniqueIDToMissions.ContainsKey(mission.CustomMissionID))
            {
                Logger.Log($"CampaignSelector: duplicated mission. CodeName: {mission.CodeName}. ID: {mission.CustomMissionID}. Description: {mission.UntranslatedGUIName}.");
                if (!string.IsNullOrEmpty(mission.Scenario))
                    mission.Enabled = false;
            }
            else
            {
                _uniqueIDToMissions.Add(mission.CustomMissionID, mission);
            }
        }

        public override void Initialize()
        {
            Name = nameof(CampaignSelector);
            BackgroundTexture = AssetLoader.LoadTexture("missionselectorbg.png");
            ClientRectangle = new Rectangle(0, 0, DEFAULT_WIDTH, DEFAULT_HEIGHT);
            BorderColor = UISettings.ActiveSettings.PanelBorderColor;

            base.Initialize();
            WindowManager.CenterControlOnScreen(this);

            lbCampaignList = FindChild<XNAListBox>(nameof(lbCampaignList));
            lbCampaignList.SelectedIndexChanged += LbCampaignList_SelectedIndexChanged;

            tbMissionDescription = FindChild<XNATextBlock>(nameof(tbMissionDescription));

            if (tbMissionDescription.BackgroundTexture == null)
            {
                tbMissionDescription.BackgroundTexture = AssetLoader.CreateTexture(AssetLoader.GetColorFromString(ClientConfiguration.Instance.AltUIBackgroundColor),
                    tbMissionDescription.Width, tbMissionDescription.Height);
            }

            trbDifficultySelector = FindChild<XNATrackbar>(nameof(trbDifficultySelector));
            trbDifficultySelector.ButtonTexture = AssetLoader.LoadTextureUncached(
                "trackbarButton_difficulty.png");

            btnLaunch = FindChild<XNAClientButton>(nameof(btnLaunch));
            btnLaunch.AllowClick = false;
            btnLaunch.LeftClick += BtnLaunch_LeftClick;

            btnCancel = FindChild<XNAClientButton>("btnCancel");
            btnCancel.LeftClick += BtnCancel_LeftClick;

            if (ClientConfiguration.Instance.CampaignTagSelectorEnabled)
            {
                btnReturn = FindChild<XNAClientButton>("btnReturn");
                btnReturn.LeftClick += BtnReturn_LeftClick;
            }

            trbDifficultySelector.Value = UserINISettings.Instance.Difficulty;

            foreach (var cb in userSettings)
            {
                cb.Load();
            }

            ReadMissionList();

            cheaterWindow = new CheaterWindow(WindowManager);
            var dp = new DarkeningPanel(WindowManager);
            dp.AddChild(cheaterWindow);
            AddChild(dp);
            dp.CenterOnParent();
            cheaterWindow.CenterOnParent();
            cheaterWindow.YesClicked += CheaterWindow_YesClicked;
            cheaterWindow.Disable();
        }

        private void LbCampaignList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbCampaignList.SelectedIndex == -1)
            {
                tbMissionDescription.Text = string.Empty;
                btnLaunch.AllowClick = false;
                return;
            }

            Mission mission = lbCampaignListMissions[lbCampaignList.SelectedIndex];

            if (string.IsNullOrEmpty(mission.Scenario))
            {
                tbMissionDescription.Text = string.Empty;
                btnLaunch.AllowClick = false;
                return;
            }

            tbMissionDescription.Text = mission.GUIDescription;

            if (!mission.Enabled)
            {
                btnLaunch.AllowClick = false;
                return;
            }

            btnLaunch.AllowClick = true;
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Disable();
        }

        private void BtnReturn_LeftClick(object sender, EventArgs e)
        {
            campaignTagSelector.NoFadeSwitch();
        }

        private void BtnLaunch_LeftClick(object sender, EventArgs e)
        {
            userSettings.ForEach(c => c.Save());

            int selectedMissionId = lbCampaignList.SelectedIndex;

            Mission mission = lbCampaignListMissions[selectedMissionId];

            if (!ClientConfiguration.Instance.ModMode &&
                (!Updater.IsFileNonexistantOrOriginal(mission.Scenario) || AreFilesModified()))
            {
                // Confront the user by showing the cheater screen
                missionToLaunch = mission;
                cheaterWindow.Enable();
                return;
            }

            LaunchMission(mission);
        }

        private bool AreFilesModified()
        {
            foreach (string filePath in filesToCheck)
            {
                if (!Updater.IsFileNonexistantOrOriginal(filePath))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Called when the user wants to proceed to the mission despite having
        /// being called a cheater.
        /// </summary>
        private void CheaterWindow_YesClicked(object sender, EventArgs e)
        {
            LaunchMission(missionToLaunch);
        }

        /// <summary>
        /// Starts a singleplayer mission.
        /// </summary>
        private void LaunchMission(Mission mission)
        {
            CustomMissionHelper.DeleteSupplementalMissionFiles();
            CustomMissionHelper.CopySupplementalMissionFiles(mission);

            string scenario = mission.Scenario;

            bool copyMapsToSpawnmapINI = ClientConfiguration.Instance.CopyMissionsToSpawnmapINI;

            Logger.Log("About to write spawn.ini.");
            IniFile spawnIni = new()
            {
                Comment = "Generated by CnCNet Client"
            };
            IniSection spawnIniSettings = new("Settings");

            if (copyMapsToSpawnmapINI)
                spawnIniSettings.AddKey("Scenario", "spawnmap.ini");
            else
                spawnIniSettings.AddKey("Scenario", scenario.ToUpperInvariant());

            // No one wants to play missions on Fastest, so we'll change it to Faster
            if (UserINISettings.Instance.GameSpeed == 0)
                UserINISettings.Instance.GameSpeed.Value = 1;

            spawnIniSettings.AddKey("CampaignID", mission.CampaignID.ToString(CultureInfo.InvariantCulture));
            spawnIniSettings.AddKey("GameSpeed", UserINISettings.Instance.GameSpeed.ToString());

            if (ClientConfiguration.Instance.ClientGameType == ClientType.YR ||
                ClientConfiguration.Instance.ClientGameType == ClientType.Ares)
                spawnIniSettings.AddKey("Ra2Mode", (!mission.RequiredAddon).ToString(CultureInfo.InvariantCulture));
            else
                spawnIniSettings.AddKey("Firestorm", mission.RequiredAddon.ToString(CultureInfo.InvariantCulture));

            spawnIniSettings.AddKey("CustomLoadScreen", LoadingScreenController.GetLoadScreenName(mission.Side.ToString()));

            spawnIniSettings.AddKey("IsSinglePlayer", "Yes");
            spawnIniSettings.AddKey("SidebarHack", ClientConfiguration.Instance.SidebarHack.ToString(CultureInfo.InvariantCulture));
            spawnIniSettings.AddKey("Side", mission.Side.ToString(CultureInfo.InvariantCulture));
            spawnIniSettings.AddKey("BuildOffAlly", mission.BuildOffAlly.ToString(CultureInfo.InvariantCulture));

            UserINISettings.Instance.Difficulty.Value = trbDifficultySelector.Value;

            spawnIniSettings.AddKey("DifficultyModeHuman", mission.PlayerAlwaysOnNormalDifficulty ? "1" : trbDifficultySelector.Value.ToString(CultureInfo.InvariantCulture));
            spawnIniSettings.AddKey("DifficultyModeComputer", GetComputerDifficulty().ToString(CultureInfo.InvariantCulture));

            if (mission.IsCustomMission)
            {
                spawnIniSettings.AddKey("CustomMissionID", mission.CustomMissionID.ToString(CultureInfo.InvariantCulture));
            }

            spawnIni.AddSection(spawnIniSettings);

            bool hasGameMissionData = false;
            string scenarioPath = SafePath.CombineFilePath(ProgramConstants.GamePath, mission.Scenario);

            if (!mission.IsCustomMission && File.Exists(scenarioPath)) 
            {
                var mapIni = new IniFile(scenarioPath);
                mission.GameMissionConfigSection = mapIni.GetSection("GameMissionConfig");

                if (mission.GameMissionConfigSection is not null)
                    hasGameMissionData = true;
            }

            if (mission.IsCustomMission && mission.GameMissionConfigSection is not null || hasGameMissionData)
            {
                // copy an IniSection
                IniSection spawnIniMissionIniSection = new(scenario.ToUpperInvariant());
                foreach (var kvp in mission.GameMissionConfigSection.Keys)
                {
                    spawnIniMissionIniSection.AddKey(kvp.Key, kvp.Value);
                }

                // append the new IniSection
                spawnIni.AddSection(spawnIniMissionIniSection);
                spawnIniSettings.AddKey("ReadMissionSection", "Yes");
            }

            spawnIni.WriteIniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "spawn.ini"));

            var difficultyIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, DifficultyIniPaths[trbDifficultySelector.Value]));
            string difficultyName = DifficultyNames[trbDifficultySelector.Value];

            if (copyMapsToSpawnmapINI)
            {
                var mapIni = new IniFile(scenarioPath);
                IniFile.ConsolidateIniFiles(mapIni, difficultyIni);
                mapIni.WriteIniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "spawnmap.ini"));
            }

            UserINISettings.Instance.Difficulty.Value = trbDifficultySelector.Value;
            UserINISettings.Instance.SaveSettings();

            if (ClientConfiguration.Instance.ReturnToMainMenuOnMissionLaunch)
                Disable();
            else
                ToggleControls(false);

            discordHandler.UpdatePresence(mission.UntranslatedGUIName, difficultyName, mission.IconPath, true);
            GameProcessLogic.GameProcessExited += GameProcessExited_Callback;

            GameProcessLogic.StartGameProcess(WindowManager);
        }

        private void ToggleControls(bool enabled)
        {
            btnLaunch.AllowClick = enabled;
            btnCancel.AllowClick = enabled;
            lbCampaignList.Enabled = enabled;
            trbDifficultySelector.Enabled = enabled;

            if (btnReturn is not null)
                btnReturn.AllowClick = enabled;

            foreach (IUserSetting setting in userSettings)
            {
                if (setting is SettingCheckBoxBase cb)
                    cb.AllowChecking = enabled;
                else if (setting is SettingDropDownBase dd)
                    dd.AllowDropDown = enabled;
            }
        }

        private int GetComputerDifficulty() =>
            Math.Abs(trbDifficultySelector.Value - 2);

        private void GameProcessExited_Callback()
        {
            WindowManager.AddCallback(new Action(GameProcessExited), null);

            foreach (IUserSetting setting in userSettings)
            {
                if (!setting.ResetToDefaultOnGameExit)
                    continue;

                if (setting is SettingCheckBoxBase cb)
                    cb.Checked = cb.DefaultValue;
                else if (setting is SettingDropDownBase dd)
                    dd.SelectedIndex = dd.DefaultValue;

                setting.Save();
            }

        }

        protected virtual void GameProcessExited()
        {
            GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;

            CustomMissionHelper.DeleteSupplementalMissionFiles();

            // Logger.Log("GameProcessExited: Updating Discord Presence.");
            discordHandler.UpdatePresence();

            if (!ClientConfiguration.Instance.ReturnToMainMenuOnMissionLaunch)
                ToggleControls(true);
        }

        private void ReadMissionList()
        {
            ParseBattleIni("INI/Battle.ini");

            if (AllMissions.Count == 0)
                ParseBattleIni("INI/" + ClientConfiguration.Instance.BattleFSFileName);

            LoadCustomMissions();

            LoadMissionsWithFilter(null, disableCustomMissions: true, disableOfficialMissions: false);
        }

        private void LoadCustomMissions()
        {
            string customMissionsDirectory = SafePath.CombineDirectoryPath(ProgramConstants.GamePath, ClientConfiguration.Instance.CustomMissionPath);
            if (!Directory.Exists(customMissionsDirectory))
                return;

            string[] mapFiles = Directory.GetFiles(customMissionsDirectory, "*.map");
            if (mapFiles.Length == 0)
                return;

            // The codes below are disabled, in favor of being defined in a `Battle.ini` file.
            // // Add a dummy mission to separate custom missions from official missions
            // IniSection customMissionSeparatorSection = new();
            // customMissionSeparatorSection.AddKey("Description", "-------- Custom Scenarios --------".L10N("Client:Main:CustomMissionSeparator"));
            // Mission separator = Mission.NewCustomMission(customMissionSeparatorSection, "__XCUSTOM", string.Empty, null);
            // AddMission(separator);

            foreach (string mapFilePath in mapFiles)
            {
                var mapFile = new IniFile(mapFilePath);

                IniSection clientMissionDataSection = mapFile.GetSection("ClientMissionConfig");

                if (clientMissionDataSection is null)
                    continue;

                IniSection? gameMissionDataSection = mapFile.GetSection("GameMissionConfig");

                string filename = new FileInfo(mapFilePath).Name;
                string scenario = SafePath.CombineFilePath(ClientConfiguration.Instance.CustomMissionPath, filename);
                Mission mission = Mission.NewCustomMission(clientMissionDataSection, missionCodeName: filename.ToUpperInvariant(), scenario, gameMissionDataSection);
                AddMission(mission);
            }
        }

        /// <summary>
        /// Parses a Battle(E).ini file. Returns true if succesful (file found), otherwise false.
        /// </summary>
        /// <param name="path">The path of the file, relative to the game directory.</param>
        /// <returns>True if succesful, otherwise false.</returns>
        private bool ParseBattleIni(string path)
        {
            Logger.Log("Attempting to parse " + path + " to populate mission list.");

            FileInfo battleIniFileInfo = SafePath.GetFile(ProgramConstants.GamePath, path);
            if (!battleIniFileInfo.Exists)
            {
                Logger.Log("File " + path + " not found. Ignoring.");
                return false;
            }

            if (lbCampaignListMissions.Count > 0)
            {
                throw new InvalidOperationException("Loading multiple Battle*.ini files is not supported anymore.");
            }

            var battleIni = new IniFile(battleIniFileInfo.FullName);

            List<string> battleKeys = battleIni.GetSectionKeys("Battles");

            if (battleKeys == null)
                return false; // File exists but [Battles] doesn't

            for (int i = 0; i < battleKeys.Count; i++)
            {
                string battleEntry = battleKeys[i];
                string battleSection = battleIni.GetStringValue("Battles", battleEntry, "NOT FOUND");

                if (!battleIni.SectionExists(battleSection))
                    continue;

                var mission = new Mission(battleIni.GetSection(battleSection), missionCodeName: battleEntry);
                AddMission(mission);
            }

            Logger.Log("Finished parsing " + path + ".");
            return true;
        }

        /// <summary>
        /// Load or re-load missons with selected tags.
        /// </summary>
        /// <param name="selectedTags">Missions with at lease one of which tags to be shown. As an exception, null means show all missions.</param>
        /// <param name="loadCustomMissions">True means show official missions. False means show custom missions.</param>
        public void LoadMissionsWithFilter(ISet<string> selectedTags, bool disableCustomMissions = true, bool disableOfficialMissions = false)
        {
            lbCampaignListMissions.Clear();

            lbCampaignList.IsChangingSize = true;

            lbCampaignList.Clear();
            lbCampaignList.SelectedIndex = -1;

            // The following two lines are handled by LbCampaignList_SelectedIndexChanged
            // tbMissionDescription.Text = string.Empty;
            // btnLaunch.AllowClick = false;

            // Select missions with the filter
            IEnumerable<Mission> missions = AllMissions;
            if (disableCustomMissions && disableOfficialMissions)
            {
                // do nothing
            }
            else if (disableCustomMissions)
            {
                missions = missions.Where(mission => !mission.IsCustomMission);
            }
            else if (disableOfficialMissions)
            {
                missions = missions.Where(mission => mission.IsCustomMission);
            }                
            else
            {
                // do nothing
            }

            if (selectedTags != null)
                missions = missions.Where(mission => mission.Tags.Intersect(selectedTags).Any()).ToList();
            lbCampaignListMissions = missions.ToList();

            // Update lbCampaignList with selected missions
            foreach (Mission mission in lbCampaignListMissions)
            {
                var item = new XNAListBoxItem();
                item.Text = mission.GUIName;
                if (!mission.Enabled)
                {
                    item.TextColor = UISettings.ActiveSettings.DisabledItemColor;
                }
                else if (string.IsNullOrEmpty(mission.Scenario))
                {
                    item.TextColor = AssetLoader.GetColorFromString(
                        ClientConfiguration.Instance.ListBoxHeaderColor);
                    item.IsHeader = true;
                    item.Selectable = false;
                }
                else
                {
                    item.TextColor = lbCampaignList.DefaultItemColor;
                }

                if (!string.IsNullOrEmpty(mission.IconPath))
                    item.Texture = AssetLoader.LoadTexture(mission.IconPath + "icon.png");

                lbCampaignList.AddItem(item);
            }

            lbCampaignList.IsChangingSize = false;

            lbCampaignList.TopIndex = 0;
        }
        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }
    }
}
