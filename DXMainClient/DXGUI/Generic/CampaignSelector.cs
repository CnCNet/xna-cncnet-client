using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClientCore;
using ClientGUI;
using ClientUpdater;
using ClientCore.Extensions;
using DTAClient.Domain;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System.Diagnostics;
using System.Globalization;

namespace DTAClient.DXGUI.Generic
{
    public class CampaignSelector : XNAWindow
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

        public CampaignSelector(WindowManager windowManager, DiscordHandler discordHandler) : base(windowManager)
        {
            this.discordHandler = discordHandler;
        }

        private DiscordHandler discordHandler;

        private List<Mission> lbCampaignListMissions = new List<Mission>();
        private XNAListBox lbCampaignList;
        private XNAClientButton btnLaunch;
        private XNATextBlock tbMissionDescription;
        private XNATrackbar trbDifficultySelector;

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
            BackgroundTexture = AssetLoader.LoadTexture("missionselectorbg.png");
            ClientRectangle = new Rectangle(0, 0, DEFAULT_WIDTH, DEFAULT_HEIGHT);
            BorderColor = UISettings.ActiveSettings.PanelBorderColor;

            Name = "CampaignSelector";

            var lblSelectCampaign = new XNALabel(WindowManager);
            lblSelectCampaign.Name = "lblSelectCampaign";
            lblSelectCampaign.FontIndex = 1;
            lblSelectCampaign.ClientRectangle = new Rectangle(12, 12, 0, 0);
            lblSelectCampaign.Text = "MISSIONS:".L10N("Client:Main:Missions");

            lbCampaignList = new XNAListBox(WindowManager);
            lbCampaignList.Name = "lbCampaignList";
            lbCampaignList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            lbCampaignList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbCampaignList.ClientRectangle = new Rectangle(12,
                lblSelectCampaign.Bottom + 6, 300, 516);
            lbCampaignList.SelectedIndexChanged += LbCampaignList_SelectedIndexChanged;

            var lblMissionDescriptionHeader = new XNALabel(WindowManager);
            lblMissionDescriptionHeader.Name = "lblMissionDescriptionHeader";
            lblMissionDescriptionHeader.FontIndex = 1;
            lblMissionDescriptionHeader.ClientRectangle = new Rectangle(
                lbCampaignList.Right + 12,
                lblSelectCampaign.Y, 0, 0);
            lblMissionDescriptionHeader.Text = "MISSION DESCRIPTION:".L10N("Client:Main:MissionDescription");

            tbMissionDescription = new XNATextBlock(WindowManager);
            tbMissionDescription.Name = "tbMissionDescription";
            tbMissionDescription.ClientRectangle = new Rectangle(
                lblMissionDescriptionHeader.X,
                lblMissionDescriptionHeader.Bottom + 6,
                Width - 24 - lbCampaignList.Right, 430);
            tbMissionDescription.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            tbMissionDescription.Alpha = 1.0f;

            tbMissionDescription.BackgroundTexture = AssetLoader.CreateTexture(AssetLoader.GetColorFromString(ClientConfiguration.Instance.AltUIBackgroundColor),
                tbMissionDescription.Width, tbMissionDescription.Height);

            var lblDifficultyLevel = new XNALabel(WindowManager);
            lblDifficultyLevel.Name = "lblDifficultyLevel";
            lblDifficultyLevel.Text = "DIFFICULTY LEVEL".L10N("Client:Main:DifficultyLevel");
            lblDifficultyLevel.FontIndex = 1;
            Vector2 textSize = Renderer.GetTextDimensions(lblDifficultyLevel.Text, lblDifficultyLevel.FontIndex);
            lblDifficultyLevel.ClientRectangle = new Rectangle(
                tbMissionDescription.X + (tbMissionDescription.Width - (int)textSize.X) / 2,
                tbMissionDescription.Bottom + 12, (int)textSize.X, (int)textSize.Y);

            trbDifficultySelector = new XNATrackbar(WindowManager);
            trbDifficultySelector.Name = "trbDifficultySelector";
            trbDifficultySelector.ClientRectangle = new Rectangle(
                tbMissionDescription.X, lblDifficultyLevel.Bottom + 6,
                tbMissionDescription.Width, 30);
            trbDifficultySelector.MinValue = 0;
            trbDifficultySelector.MaxValue = 2;
            trbDifficultySelector.BackgroundTexture = AssetLoader.CreateTexture(
                new Color(0, 0, 0, 128), 2, 2);
            trbDifficultySelector.ButtonTexture = AssetLoader.LoadTextureUncached(
                "trackbarButton_difficulty.png");

            var lblEasy = new XNALabel(WindowManager);
            lblEasy.Name = "lblEasy";
            lblEasy.FontIndex = 1;
            lblEasy.Text = "EASY".L10N("Client:Main:DifficultyEasy");
            lblEasy.ClientRectangle = new Rectangle(trbDifficultySelector.X,
                trbDifficultySelector.Bottom + 6, 1, 1);

            var lblNormal = new XNALabel(WindowManager);
            lblNormal.Name = "lblNormal";
            lblNormal.FontIndex = 1;
            lblNormal.Text = "NORMAL".L10N("Client:Main:DifficultyNormal");
            textSize = Renderer.GetTextDimensions(lblNormal.Text, lblNormal.FontIndex);
            lblNormal.ClientRectangle = new Rectangle(
                tbMissionDescription.X + (tbMissionDescription.Width - (int)textSize.X) / 2,
                lblEasy.Y, (int)textSize.X, (int)textSize.Y);

            var lblHard = new XNALabel(WindowManager);
            lblHard.Name = "lblHard";
            lblHard.FontIndex = 1;
            lblHard.Text = "HARD".L10N("Client:Main:DifficultyHard");
            lblHard.ClientRectangle = new Rectangle(
                tbMissionDescription.Right - lblHard.Width,
                lblEasy.Y, 1, 1);

            btnLaunch = new XNAClientButton(WindowManager);
            btnLaunch.Name = "btnLaunch";
            btnLaunch.ClientRectangle = new Rectangle(12, Height - 35, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnLaunch.Text = "Launch".L10N("Client:Main:ButtonLaunch");
            btnLaunch.AllowClick = false;
            btnLaunch.LeftClick += BtnLaunch_LeftClick;

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(Width - 145,
                btnLaunch.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnCancel.Text = "Cancel".L10N("Client:Main:ButtonCancel");
            btnCancel.LeftClick += BtnCancel_LeftClick;

            AddChild(lblSelectCampaign);
            AddChild(lblMissionDescriptionHeader);
            AddChild(lbCampaignList);
            AddChild(tbMissionDescription);
            AddChild(lblDifficultyLevel);
            AddChild(btnLaunch);
            AddChild(btnCancel);
            AddChild(trbDifficultySelector);
            AddChild(lblEasy);
            AddChild(lblNormal);
            AddChild(lblHard);

            // Set control attributes from INI file
            base.Initialize();

            // Center on screen
            CenterOnParent();

            trbDifficultySelector.Value = UserINISettings.Instance.Difficulty;

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

        private void BtnLaunch_LeftClick(object sender, EventArgs e)
        {
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
                spawnIniSettings.AddKey("Scenario", scenario);

            // No one wants to play missions on Fastest, so we'll change it to Faster
            if (UserINISettings.Instance.GameSpeed == 0)
                UserINISettings.Instance.GameSpeed.Value = 1;

            spawnIniSettings.AddKey("CampaignID", mission.CampaignID.ToString(CultureInfo.InvariantCulture));
            spawnIniSettings.AddKey("GameSpeed", UserINISettings.Instance.GameSpeed.ToString());
#if YR || ARES
            spawnIniSettings.AddKey("Ra2Mode", (!mission.RequiredAddon).ToString(CultureInfo.InvariantCulture));
#else
            spawnIniSettings.AddKey("Firestorm", mission.RequiredAddon.ToString(CultureInfo.InvariantCulture));
#endif

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

            if (mission.IsCustomMission && mission.CustomMission_MissionMdIniSection is not null)
            {
                // copy an IniSection
                IniSection spawnIniMissionIniSection = new(scenario);
                foreach (var kvp in mission.CustomMission_MissionMdIniSection.Keys)
                {
                    spawnIniMissionIniSection.AddKey(kvp.Key, kvp.Value);
                }

                // append the new IniSection
                spawnIni.AddSection(spawnIniMissionIniSection);
            }

            spawnIni.WriteIniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "spawn.ini"));

            var difficultyIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, DifficultyIniPaths[trbDifficultySelector.Value]));
            string difficultyName = DifficultyNames[trbDifficultySelector.Value];

            if (copyMapsToSpawnmapINI)
            {
                var mapIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, mission.Scenario));
                IniFile.ConsolidateIniFiles(mapIni, difficultyIni);
                mapIni.WriteIniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "spawnmap.ini"));
            }

            UserINISettings.Instance.Difficulty.Value = trbDifficultySelector.Value;
            UserINISettings.Instance.SaveSettings();

            Disable();

            discordHandler.UpdatePresence(mission.UntranslatedGUIName, difficultyName, mission.IconPath, true);
            GameProcessLogic.GameProcessExited += GameProcessExited_Callback;

            GameProcessLogic.StartGameProcess(WindowManager);
        }

        private int GetComputerDifficulty() =>
            Math.Abs(trbDifficultySelector.Value - 2);

        private void GameProcessExited_Callback()
        {
            WindowManager.AddCallback(new Action(GameProcessExited), null);
        }

        protected virtual void GameProcessExited()
        {
            GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;

            CustomMissionHelper.DeleteSupplementalMissionFiles();

            // Logger.Log("GameProcessExited: Updating Discord Presence.");
            discordHandler.UpdatePresence();
        }

        private void ReadMissionList()
        {
            ParseBattleIni("INI/Battle.ini");

            if (AllMissions.Count == 0)
                ParseBattleIni("INI/" + ClientConfiguration.Instance.BattleFSFileName);

            LoadCustomMissions();

            LoadMissionsWithFilter(null);
        }

        private void LoadCustomMissions()
        {
            string customMissionsDirectory = SafePath.CombineDirectoryPath(ProgramConstants.GamePath, ClientConfiguration.Instance.CustomMissionPath);
            if (!Directory.Exists(customMissionsDirectory))
                return;

            string[] mapFiles = Directory.GetFiles(customMissionsDirectory, "*.map");
            foreach (string mapFilePath in mapFiles)
            {
                var mapFile = new IniFile(mapFilePath);

                IniSection missionSection = mapFile.GetSection("CNCNET:MISSION:BATTLE.INI");
                if (missionSection is null)
                    continue;

                IniSection? missionMdIniSection = mapFile.GetSection("CNCNET:MISSION:MISSION.INI");

                string filename = new FileInfo(mapFilePath).Name;
                string scenario = SafePath.CombineFilePath(ClientConfiguration.Instance.CustomMissionPath, filename);
                Mission mission = Mission.NewCustomMission(missionSection, missionCodeName: filename.ToUpperInvariant(), scenario, missionMdIniSection);
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
        public void LoadMissionsWithFilter(ISet<string> selectedTags = null)
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
