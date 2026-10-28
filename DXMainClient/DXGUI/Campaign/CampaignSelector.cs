using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

using ClientCore;
using ClientCore.Extensions;
using ClientCore.Launch;
using ClientCore.Launch.Spawner;

using ClientGUI;
using ClientGUI.Settings;

using ClientUpdater;

using DTAClient.Domain;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Campaign
{
    public class CampaignSelector : XNAWindow
    {
        private const int DEFAULT_WIDTH = 650;
        private const int DEFAULT_HEIGHT = 600;

        private const string SETTINGS_PATH = "Client/CampaignSettings.ini";

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

        private List<Mission> selectedMissions = [];

        private XNAPanel pnlMissionPreview;
        private bool pnlMissionPreviewBackgroundTextureNeedsDispose = false;
        private string missionPreviewFolder => SafePath.CombineDirectoryPath(ProgramConstants.GetBaseResourcePath(), "Mission Previews");
        private string defaultMissionPreviewPath => SafePath.CombineFilePath(missionPreviewFolder, "Default.png");
        private bool pnlMissionPreviewEnabled => File.Exists(defaultMissionPreviewPath);

        private XNAListBox lbCampaignList;
        private XNAClientButton btnLaunch;
        private XNAClientButton btnCancel;
        private XNAClientButton btnReturn;
        private XNATextBlock tbMissionDescription;
        private XNATrackbar trbDifficultySelector;
        private List<IUserSetting> userSettings = new List<IUserSetting>();

        private CheaterWindow cheaterWindow;

        public List<CampaignCheckBox> CheckBoxes { get; } = new();
        public List<CampaignDropDown> DropDowns { get; } = new();

        private IniFile gameOptionsIni;

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
            Name = "CampaignSelector";
            BackgroundTexture = AssetLoader.LoadTexture("missionselectorbg.png");
            ClientRectangle = new Rectangle(0, 0, DEFAULT_WIDTH, DEFAULT_HEIGHT);
            BorderColor = UISettings.ActiveSettings.PanelBorderColor;

            gameOptionsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(),
                ClientConfiguration.GAME_OPTIONS));

            var lblSelectCampaign = new XNALabel(WindowManager);
            lblSelectCampaign.Name = nameof(lblSelectCampaign);
            lblSelectCampaign.FontIndex = 1;
            lblSelectCampaign.ClientRectangle = new Rectangle(12, 12, 0, 0);
            lblSelectCampaign.Text = "MISSIONS:".L10N("Client:Main:Missions");

            lbCampaignList = new XNAListBox(WindowManager);
            lbCampaignList.Name = nameof(lbCampaignList);
            lbCampaignList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 2, 2);
            lbCampaignList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbCampaignList.ClientRectangle = new Rectangle(12,
                lblSelectCampaign.Bottom + 6, 300, 516);
            lbCampaignList.SelectedIndexChanged += LbCampaignList_SelectedIndexChanged;

            var lblMissionDescriptionHeader = new XNALabel(WindowManager);
            lblMissionDescriptionHeader.Name = nameof(lblMissionDescriptionHeader);
            lblMissionDescriptionHeader.FontIndex = 1;
            lblMissionDescriptionHeader.ClientRectangle = new Rectangle(
                lbCampaignList.Right + 12,
                lblSelectCampaign.Y, 0, 0);
            lblMissionDescriptionHeader.Text = "MISSION DESCRIPTION:".L10N("Client:Main:MissionDescription");

            tbMissionDescription = new XNATextBlock(WindowManager);
            tbMissionDescription.Name = nameof(tbMissionDescription);
            tbMissionDescription.ClientRectangle = new Rectangle(
                lblMissionDescriptionHeader.X,
                lblMissionDescriptionHeader.Bottom + 6,
                Width - 24 - lbCampaignList.Right,
                pnlMissionPreviewEnabled ? 430 - 200 - 12 : 430);
            tbMissionDescription.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            tbMissionDescription.Alpha = 1.0f;
            tbMissionDescription.BackgroundTexture = AssetLoader.CreateTexture(AssetLoader.GetColorFromString(ClientConfiguration.Instance.AltUIBackgroundColor),
                tbMissionDescription.Width, tbMissionDescription.Height);

            var lblDifficultyLevel = new XNALabel(WindowManager);
            lblDifficultyLevel.Name = nameof(lblDifficultyLevel);
            lblDifficultyLevel.Text = "DIFFICULTY LEVEL".L10N("Client:Main:DifficultyLevel");
            lblDifficultyLevel.FontIndex = 1;
            Vector2 textSize = Renderer.GetTextDimensions(lblDifficultyLevel.Text, lblDifficultyLevel.FontIndex);
            lblDifficultyLevel.ClientRectangle = new Rectangle(
                tbMissionDescription.X + (tbMissionDescription.Width - (int)textSize.X) / 2,
                tbMissionDescription.Bottom + 12, (int)textSize.X, (int)textSize.Y);

            trbDifficultySelector = new XNATrackbar(WindowManager);
            trbDifficultySelector.Name = nameof(trbDifficultySelector);
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
            lblEasy.Name = nameof(lblEasy);
            lblEasy.FontIndex = 1;
            lblEasy.Text = "EASY".L10N("Client:Main:DifficultyEasy");
            lblEasy.ClientRectangle = new Rectangle(trbDifficultySelector.X,
                trbDifficultySelector.Bottom + 6, 1, 1);

            var lblNormal = new XNALabel(WindowManager);
            lblNormal.Name = nameof(lblNormal);
            lblNormal.FontIndex = 1;
            lblNormal.Text = "NORMAL".L10N("Client:Main:DifficultyNormal");
            textSize = Renderer.GetTextDimensions(lblNormal.Text, lblNormal.FontIndex);
            lblNormal.ClientRectangle = new Rectangle(
                tbMissionDescription.X + (tbMissionDescription.Width - (int)textSize.X) / 2,
                lblEasy.Y, (int)textSize.X, (int)textSize.Y);

            var lblHard = new XNALabel(WindowManager);
            lblHard.Name = nameof(lblHard);
            lblHard.FontIndex = 1;
            lblHard.Text = "HARD".L10N("Client:Main:DifficultyHard");
            lblHard.ClientRectangle = new Rectangle(
                tbMissionDescription.Right - lblHard.Width,
                lblEasy.Y, 1, 1);

            btnLaunch = new XNAClientButton(WindowManager);
            btnLaunch.Name = nameof(btnLaunch);
            btnLaunch.ClientRectangle = new Rectangle(12, Height - 35, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnLaunch.Text = "Launch".L10N("Client:Main:ButtonLaunch");
            btnLaunch.AllowClick = false;
            btnLaunch.LeftClick += BtnLaunch_LeftClick;

            btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = nameof(btnCancel);
            btnCancel.ClientRectangle = new Rectangle(Width - 145,
                btnLaunch.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
            btnCancel.Text = "Cancel".L10N("Client:Main:ButtonCancel");
            btnCancel.LeftClick += BtnCancel_LeftClick;

            if (pnlMissionPreviewEnabled)
            {
                pnlMissionPreview = new XNAPanel(WindowManager);
                pnlMissionPreview.Name = nameof(pnlMissionPreview);
                pnlMissionPreview.ClientRectangle = new Rectangle(
                    tbMissionDescription.X,
                    tbMissionDescription.Bottom + 12,
                    tbMissionDescription.Width,
                    200);

                pnlMissionPreview.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

                pnlMissionPreview.BackgroundTexture = CreateLetterboxedTexture(AssetLoader.LoadTextureUncached(defaultMissionPreviewPath), pnlMissionPreview.Width, pnlMissionPreview.Height);
                pnlMissionPreviewBackgroundTextureNeedsDispose = true;
            }
            else
            {
                pnlMissionPreview = null;
            }

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

            if (pnlMissionPreview != null)
                AddChild(pnlMissionPreview);

            if (ClientConfiguration.Instance.CampaignTagSelectorEnabled)
            {
                btnReturn = new XNAClientButton(WindowManager);
                btnReturn.Name = nameof(btnReturn);
                btnReturn.ClientRectangle = new Rectangle(trbDifficultySelector.X,
                btnLaunch.Y, UIDesignConstants.BUTTON_WIDTH_133, UIDesignConstants.BUTTON_HEIGHT);
                btnReturn.Text = "Campaigns".L10N("Client:Main:ButtonReturnToCampaigns");
                btnReturn.LeftClick += BtnReturn_LeftClick;
                btnReturn.Disable();
                AddChild(btnReturn);
            }

            // Set control attributes from INI file
            base.Initialize();

            // Center on screen
            CenterOnParent();

            trbDifficultySelector.Value = UserINISettings.Instance.Difficulty;

            userSettings.AddRange(Children.OfType<IUserSetting>());

            ReadMissionList();

            cheaterWindow = new CheaterWindow(WindowManager);
            var dp = new DarkeningPanel(WindowManager);
            dp.AddChild(cheaterWindow);
            AddChild(dp);
            dp.CenterOnParent();
            cheaterWindow.CenterOnParent();
            cheaterWindow.YesClicked += CheaterWindow_YesClicked;
            cheaterWindow.Disable();

            LoadSettings();
        }

        private void LbCampaignList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbCampaignList.SelectedIndex == -1)
            {
                tbMissionDescription.Text = string.Empty;

                UpdateMissionPreview(string.Empty);

                btnLaunch.AllowClick = false;
                return;
            }

            Mission mission = selectedMissions[lbCampaignList.SelectedIndex];

            UpdateMissionPreview(mission.PreviewImage);

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

        // TODO: Modify XNAUI by adding PanelBackgroundImageDrawMode.LETTERBOXED as a new draw mode.
        private Texture2D CreateLetterboxedTexture(Texture2D sourceTexture, int targetWidth, int targetHeight, bool disposeSourceTexture = true)
        {
            // Calculate aspect ratios
            float sourceAspect = (float)sourceTexture.Width / sourceTexture.Height;
            float targetAspect = (float)targetWidth / targetHeight;

            int drawWidth, drawHeight, drawX, drawY;

            // Determine scaled dimensions while maintaining aspect ratio
            if (sourceAspect > targetAspect)
            {
                // Source is wider - fit to width
                drawWidth = targetWidth;
                drawHeight = (int)(targetWidth / sourceAspect);
                drawX = 0;
                drawY = (targetHeight - drawHeight) / 2;
            }
            else
            {
                // Source is taller - fit to height
                drawHeight = targetHeight;
                drawWidth = (int)(targetHeight * sourceAspect);
                drawX = (targetWidth - drawWidth) / 2;
                drawY = 0;
            }

            // Create the composite texture
            RenderTarget2D renderTarget = new RenderTarget2D(
                WindowManager.GraphicsDevice,
                targetWidth,
                targetHeight,
                false,
                SurfaceFormat.Color,
                DepthFormat.None);

            WindowManager.GraphicsDevice.SetRenderTarget(renderTarget);
            WindowManager.GraphicsDevice.Clear(AssetLoader.GetColorFromString(ClientConfiguration.Instance.AltUIBackgroundColor));

            var spriteBatch = new SpriteBatch(WindowManager.GraphicsDevice);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(
                sourceTexture,
                new Rectangle(drawX, drawY, drawWidth, drawHeight),
                Color.White);
            spriteBatch.End();
            spriteBatch.Dispose();

            WindowManager.GraphicsDevice.SetRenderTarget(null);

            if (disposeSourceTexture)
                sourceTexture.Dispose();

            return renderTarget;
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            SaveSettings();
            Disable();
        }

        private void BtnReturn_LeftClick(object sender, EventArgs e)
        {
            campaignTagSelector.NoFadeSwitch();
        }

        private void BtnLaunch_LeftClick(object sender, EventArgs e)
        {
            SaveSettings();

            int selectedMissionId = lbCampaignList.SelectedIndex;

            Mission mission = selectedMissions[selectedMissionId];

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
            CustomMissionHelper.CopySupplementalMissionFiles(mission);

            // No one wants to play missions on Fastest, so we'll change it to Faster
            if (UserINISettings.Instance.GameSpeed == 0)
                UserINISettings.Instance.GameSpeed.Value = 1;

            UserINISettings.Instance.Difficulty.Value = trbDifficultySelector.Value;

            MissionLaunchRequest request = BuildMissionRequest(mission);
            SpawnIniDialect dialect = SpawnIniDialect.ForClientType(ClientConfiguration.Instance.ClientGameType);

            SpawnFiles.WriteSpawnIni(SpawnIniWriter.WriteMission(request, dialect));

            if (request.SpawnMap != null)
                SpawnFiles.WriteSpawnMap(request.SpawnMap);

            string difficultyName = DifficultyNames[trbDifficultySelector.Value];

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

        /// <summary>
        /// Builds the request that launches the mission with the selected difficulty and options.
        /// </summary>
        private MissionLaunchRequest BuildMissionRequest(Mission mission)
        {
            bool copyMapsToSpawnmapINI = ClientConfiguration.Instance.CopyMissionsToSpawnmapINI;

            bool scenarioPathFound = mission.TryGetScenarioFilePath(out string scenarioPath);

            if (!scenarioPathFound)
            {
                Logger.Log($"CampaignSelector: mission scenario contains invalid path characters. Mission code name: {mission.CodeName}. Scenario: {mission.Scenario}. This mission will be launched without applying {nameof(ClientConfiguration.Instance.CopyMissionsToSpawnmapINI)}.");
                copyMapsToSpawnmapINI = false;
            }

            var lobbyOptions = new List<SpawnOption>();

            foreach (IGameSessionSetting setting in CheckBoxes.Cast<IGameSessionSetting>().Concat(DropDowns))
            {
                if (setting.TryGetSpawnIniOption(out string key, out string value))
                    lobbyOptions.Add(new SpawnOption(key, value));
            }

            List<SpawnOptionLayer> optionLayers =
            [
                new SpawnOptionLayer("Campaign options", lobbyOptions),
                GetCampaignForcedOptionLayer(gameOptionsIni),
            ];

            return new MissionLaunchRequest
            {
                SpawnMap = copyMapsToSpawnmapINI ? BuildSpawnMap(scenarioPath) : null,
                Scenario = copyMapsToSpawnmapINI ? ProgramConstants.SPAWNMAP_INI : mission.Scenario,
                CampaignID = mission.CampaignID,
                GameSpeed = UserINISettings.Instance.GameSpeed,
                RequiresAddon = mission.RequiredAddon,
                CustomLoadScreen = LoadingScreenController.GetLoadScreenName(mission.Side.ToString()),
                // Campaign maps may not contain a display name.
                UIMapName = mission.UntranslatedGUIName,
                SidebarHack = ClientConfiguration.Instance.SidebarHack,
                Side = mission.Side,
                BuildOffAlly = mission.BuildOffAlly,
                HumanDifficulty = mission.PlayerAlwaysOnNormalDifficulty ? 1 : trbDifficultySelector.Value,
                ComputerDifficulty = GetComputerDifficulty(),
                CustomMissionID = mission.IsCustomMission ? mission.CustomMissionID : null,
                MissionSection = GetMissionSection(mission),
                OptionLayers = optionLayers,
                Recording = ReplayManager.CreateRecording(optionLayers, mission.UntranslatedGUIName),
            };
        }

        /// <summary>
        /// Builds the mission map with the selected difficulty and the map code of the selected options.
        /// </summary>
        private IniFile BuildSpawnMap(string scenarioPath)
        {
            var difficultyIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, DifficultyIniPaths[trbDifficultySelector.Value]));
            var mapIni = new IniFile(scenarioPath);

            IniFile.ConsolidateIniFiles(mapIni, difficultyIni);

            foreach (CampaignCheckBox chkBox in CheckBoxes)
                chkBox.ApplyMapCode(mapIni, gameMode: null);

            foreach (CampaignDropDown dd in DropDowns)
                dd.ApplyMapCode(mapIni, gameMode: null);

            return mapIni;
        }

        /// <summary>
        /// Returns the spawn.ini settings GameOptions.ini forces for every campaign mission.
        /// </summary>
        public static SpawnOptionLayer GetCampaignForcedOptionLayer(IniFile gameOptionsIni)
        {
            const string forcedOptionsSection = "CampaignForcedSpawnIniOptions";
            List<string> forcedKeys = gameOptionsIni.GetSectionKeys(forcedOptionsSection) ?? new List<string>(0);
            List<SpawnOption> forcedOptions = forcedKeys
                .Select(key => new SpawnOption(key, gameOptionsIni.GetStringValue(forcedOptionsSection, key, string.Empty)))
                .ToList();

            return new SpawnOptionLayer("GameOptions.ini [" + forcedOptionsSection + "]", forcedOptions);
        }

        /// <summary>
        /// Returns the mission's [GameMissionConfig] settings for the game, or null if it has none.
        /// </summary>
        public static MissionSection GetMissionSection(Mission mission)
        {
            bool hasGameMissionData = false;

            bool scenarioPathFound = mission.TryGetScenarioFilePath(out string scenarioPath);
            if (!scenarioPathFound)
            {
                Logger.Log($"CampaignSelector: mission scenario contains invalid path characters. Mission code name: {mission.CodeName}. Scenario: {mission.Scenario}. This mission will be launched without mission section data.");
                return null;
            }

            if (!mission.IsCustomMission && File.Exists(scenarioPath))
            {
                var mapIni = new IniFile(scenarioPath);
                mission.GameMissionConfigSection = mapIni.GetSection("GameMissionConfig");

                if (mission.GameMissionConfigSection is not null)
                    hasGameMissionData = true;
            }

            if (!(mission.IsCustomMission && mission.GameMissionConfigSection is not null || hasGameMissionData))
                return null;

            // copy an IniSection
            IniSection spawnIniMissionIniSection = new(mission.Scenario);
            string loadingScreenName = string.Empty;
            string loadingScreenPalName = string.Empty;
            foreach (var kvp in mission.GameMissionConfigSection.Keys)
            {
                if (string.IsNullOrEmpty(kvp.Value))
                {
                    if (kvp.Key.Equals("LS640BkgdName", StringComparison.InvariantCulture) || kvp.Key.Equals("LS800BkgdName", StringComparison.InvariantCulture))
                        loadingScreenName = kvp.Value;
                    else if (kvp.Key.Equals("LS800BkgdPal", StringComparison.InvariantCulture))
                        loadingScreenPalName = kvp.Value;
                }

                spawnIniMissionIniSection.AddKey(kvp.Key, kvp.Value);
            }

            if (string.IsNullOrEmpty(loadingScreenName))
            {
                string lsFilename = CustomMissionHelper.CustomMissionSupplementDefinition.FirstOrDefault(x => x.extension.Equals("shp", StringComparison.InvariantCultureIgnoreCase)).filename;

                if (!string.IsNullOrEmpty(lsFilename))
                {
                    spawnIniMissionIniSection.AddOrReplaceKey("LS640BkgdName", lsFilename);
                    spawnIniMissionIniSection.AddOrReplaceKey("LS800BkgdName", lsFilename);
                }
            }
            if (string.IsNullOrEmpty(loadingScreenPalName))
            {
                string palFilename = CustomMissionHelper.CustomMissionSupplementDefinition.FirstOrDefault(x => x.extension.Equals("pal", StringComparison.InvariantCultureIgnoreCase)).filename;

                if (!string.IsNullOrEmpty(palFilename))
                    spawnIniMissionIniSection.AddOrReplaceKey("LS800BkgdPal", palFilename);
            }

            return new MissionSection(spawnIniMissionIniSection.SectionName,
                spawnIniMissionIniSection.Keys.Select(kvp => new SpawnOption(kvp.Key, kvp.Value)).ToList());
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
        }

        protected virtual void GameProcessExited()
        {
            GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;

            CustomMissionHelper.DeleteSupplementalMissionFiles();

            ReplayManager.Prune();

            // Logger.Log("GameProcessExited: Updating Discord Presence.");
            discordHandler.UpdatePresence();

            if (!ClientConfiguration.Instance.ReturnToMainMenuOnMissionLaunch)
                ToggleControls(true);

            // Handle ResetToDefaultOnGameExit
            {
                // Reset campaign checkboxes
                foreach (CampaignCheckBox cb in CheckBoxes)
                {
                    if (cb.ResetToDefaultOnGameExit)
                        cb.ResetToDefault();
                }

                // Reset user settings
                foreach (IUserSetting setting in userSettings)
                {
                    if (!setting.ResetToDefaultOnGameExit)
                        continue;

                    if (setting is SettingCheckBoxBase cb)
                        cb.Checked = cb.DefaultValue;
                    else if (setting is SettingDropDownBase dd)
                        dd.SelectedIndex = dd.DefaultValue;
                }

                SaveSettings();
            }

        }

        private void ReadMissionList()
        {
            Debug.Assert(AllMissions.Count == 0 && UniqueIDToMissions.Count == 0, "AllMissions and UniqueIDToMissions should be empty when ReadMissionList() is called. We didn't handle reloading missions yet.");

            if (!ClientConfiguration.Instance.IgnoreBattleIni && AllMissions.Count == 0)
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

            foreach (string mapFilePath in mapFiles)
            {
                var mapFile = new IniFile(mapFilePath);

                IniSection clientMissionDataSection = mapFile.GetSection("ClientMissionConfig");

                if (clientMissionDataSection is null)
                    continue;

                IniSection? gameMissionDataSection = mapFile.GetSection("GameMissionConfig");

                string filename = new FileInfo(mapFilePath).Name;
                string scenario = SafePath.CombineFilePath(ClientConfiguration.Instance.CustomMissionPath, filename);
                Mission mission = Mission.NewCustomMission(clientMissionDataSection, missionCodeName: filename, scenario, gameMissionDataSection);
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

            if (selectedMissions.Count > 0)
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
            selectedMissions.Clear();

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
            selectedMissions = missions.ToList();

            // Update lbCampaignList with selected missions
            foreach (Mission mission in selectedMissions)
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

        /// <summary>
        /// Saves settings to an INI file on the file system.
        /// </summary>
        private void SaveSettings()
        {
            SaveUserSettings();
            SaveCampaignSettings();
        }

        private void SaveUserSettings()
        {
            userSettings.ForEach(c => c.Save());
            UserINISettings.Instance.SaveSettings();
        }

        private void SaveCampaignSettings()
        {
            if (!ClientConfiguration.Instance.SaveCampaignGameOptions)
                return;

            try
            {
                FileInfo settingsFileInfo = SafePath.GetFile(ProgramConstants.GamePath, SETTINGS_PATH);

                settingsFileInfo.Delete();

                var settingsIni = new IniFile(settingsFileInfo.FullName);

                foreach (CampaignDropDown dd in DropDowns)
                    settingsIni.SetStringValue("GameOptions", dd.Name, dd.SelectedIndex.ToString());

                foreach (CampaignCheckBox cb in CheckBoxes)
                    settingsIni.SetStringValue("GameOptions", cb.Name, cb.Checked.ToString());

                settingsIni.WriteIniFile();
            }
            catch (Exception ex)
            {
                Logger.Log($"Saving campaign settings failed! Reason: {ex}");
            }
        }

        /// <summary>
        /// Loads settings from an INI file on the file system.
        /// </summary>
        private void LoadSettings()
        {
            LoadUserSettings();
            LoadCampaignSettings();
        }

        private void LoadUserSettings() => userSettings.ForEach(c => c.Load());

        private void LoadCampaignSettings()
        {
            if (!ClientConfiguration.Instance.SaveCampaignGameOptions)
                return;

            var settingsIni = new IniFile(SafePath.CombineFilePath(ProgramConstants.GamePath, SETTINGS_PATH));

            foreach (CampaignDropDown dd in DropDowns)
            {
                dd.SelectedIndex = settingsIni.GetIntValue("GameOptions", dd.Name, dd.SelectedIndex);

                if (dd.SelectedIndex > -1 && dd.SelectedIndex < dd.Items.Count)
                    dd.SelectedIndex = dd.SelectedIndex;
            }

            foreach (CampaignCheckBox cb in CheckBoxes)
                cb.Checked = settingsIni.GetBooleanValue("GameOptions", cb.Name, cb.Checked);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }

        private void UpdateMissionPreview(string missionPreviewFileName)
        {
            if (pnlMissionPreview == null)
                return;

            if (pnlMissionPreviewBackgroundTextureNeedsDispose)
            {
                Debug.Assert(pnlMissionPreview.BackgroundTexture != null, "Expected background texture to dispose, but it was null.");

                pnlMissionPreview.BackgroundTexture.Dispose();
                pnlMissionPreview.BackgroundTexture = null;
                pnlMissionPreviewBackgroundTextureNeedsDispose = false;
            }

            string previewFilePath = null;
            if (!string.IsNullOrEmpty(missionPreviewFileName))
                previewFilePath = SafePath.CombineFilePath(missionPreviewFolder, missionPreviewFileName);

            if (string.IsNullOrEmpty(missionPreviewFileName) || !File.Exists(previewFilePath))
            {
                pnlMissionPreview.BackgroundTexture = CreateLetterboxedTexture(
                    AssetLoader.LoadTextureUncached(defaultMissionPreviewPath), pnlMissionPreview.Width, pnlMissionPreview.Height);
            }
            else
            {
                pnlMissionPreview.BackgroundTexture = CreateLetterboxedTexture(
                    AssetLoader.LoadTextureUncached(previewFilePath), pnlMissionPreview.Width, pnlMissionPreview.Height);
            }

            pnlMissionPreviewBackgroundTextureNeedsDispose = true;
        }
    }
}
