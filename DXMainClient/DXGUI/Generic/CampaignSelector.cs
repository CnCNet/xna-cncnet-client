using ClientCore;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using DTAClient.Domain;
using System.IO;
using ClientGUI;
using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Rampastring.Tools;

namespace DTAClient.DXGUI.Generic
{
    public class CampaignSelector : XNAWindow
    {
        const int DEFAULT_WIDTH = 327;
        const int DEFAULT_HEIGHT = 463;

        public CampaignSelector(WindowManager windowManager) : base(windowManager)
        {

        }

        List<Mission> Missions = new List<Mission>();
        XNAListBox lbCampaignList;
        XNALabel lblMissionDescriptionValue;
        XNAClientButton btnLaunch;
        XNATrackbar trbDifficultySelector;

        public override void Initialize()
        {
            BackgroundTexture = AssetLoader.LoadTexture("missionselectorbg.png");
            ClientRectangle = new Rectangle(0, 0, DEFAULT_WIDTH, DEFAULT_HEIGHT);
            BorderColor = UISettings.WindowBorderColor;

            Name = "CampaignSelector";

            var lblSelectCampaign = new XNALabel(WindowManager);
            lblSelectCampaign.Name = "lblSelectCampaign";
            lblSelectCampaign.FontIndex = 1;
            lblSelectCampaign.Text = "MISSIONS:";
            lblSelectCampaign.ClientRectangle = new Rectangle(12, 9, 0, 0);

            var lblMissionDescriptionHeader = new XNALabel(WindowManager);
            lblMissionDescriptionHeader.Name = "lblMissionDescriptionHeader";
            lblMissionDescriptionHeader.FontIndex = 1;
            lblMissionDescriptionHeader.Text = "MISSION DESCRIPTION:";
            lblMissionDescriptionHeader.ClientRectangle = new Rectangle(12, 219, 0, 0);

            lbCampaignList = new XNAListBox(WindowManager);
            lbCampaignList.Name = "lbCampaignList";
            lbCampaignList.ItemAlphaRate = 1.0f;
            lbCampaignList.ClientRectangle = new Rectangle(12, 25, 300, 184);
            lbCampaignList.SelectedIndexChanged += LbCampaignList_SelectedIndexChanged;

            ParseBattleIni("INI\\Battle.ini");
            ParseBattleIni("INI\\" + MCDomainController.Instance.GetBattleFSFileName());

            var panelMissionDescription = new XNAPanel(WindowManager);
            panelMissionDescription.Name = "panelMissionDescription";
            panelMissionDescription.ClientRectangle = new Rectangle(12, 235, 300, 76);
            panelMissionDescription.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            panelMissionDescription.Alpha = 1.0f;

            lblMissionDescriptionValue = new XNALabel(WindowManager);
            lblMissionDescriptionValue.Name = "lblMissionDescriptionValue";
            lblMissionDescriptionValue.FontIndex = 0;
            lblMissionDescriptionValue.Text = " ";
            lblMissionDescriptionValue.ClientRectangle = new Rectangle(3, 2, 0, 0);

            lbCampaignList.BackgroundTexture = AssetLoader.CreateTexture(AssetLoader.GetColorFromString(DomainController.Instance().GetUIAltBackgroundColor()),
                lbCampaignList.ClientRectangle.Width, lbCampaignList.ClientRectangle.Height);

            panelMissionDescription.BackgroundTexture = AssetLoader.CreateTexture(AssetLoader.GetColorFromString(DomainController.Instance().GetUIAltBackgroundColor()),
                panelMissionDescription.ClientRectangle.Width, panelMissionDescription.ClientRectangle.Height);

            var lblDifficultyLevel = new XNALabel(WindowManager);
            lblDifficultyLevel.Name = "lblDifficultyLevel";
            lblDifficultyLevel.Text = "DIFFICULTY LEVEL";
            lblDifficultyLevel.FontIndex = 1;
            Vector2 textSize = Renderer.GetTextDimensions(lblDifficultyLevel.Text, lblDifficultyLevel.FontIndex);
            lblDifficultyLevel.ClientRectangle = new Rectangle(0, 0, (int)textSize.X, (int)textSize.Y);

            trbDifficultySelector = new XNATrackbar(WindowManager);
            trbDifficultySelector.Name = "trbDifficultySelector";
            trbDifficultySelector.ClientRectangle = new Rectangle(12, 340, 300, 45);
            trbDifficultySelector.MinValue = 0;
            trbDifficultySelector.MaxValue = 2;
            trbDifficultySelector.BackgroundTexture = AssetLoader.CreateTexture(
                new Color(0, 0, 0, 128), 2, 2);

            var lblEasy = new XNALabel(WindowManager);
            lblEasy.Name = "lblEasy";
            lblEasy.FontIndex = 1;
            lblEasy.Text = "EASY";
            lblEasy.ClientRectangle = new Rectangle(12, 390, 1, 1);

            var lblNormal = new XNALabel(WindowManager);
            lblNormal.Name = "lblNormal";
            lblNormal.FontIndex = 1;
            lblNormal.Text = "NORMAL";
            textSize = Renderer.GetTextDimensions(lblNormal.Text, lblNormal.FontIndex);
            lblNormal.ClientRectangle = new Rectangle(0, 0, (int)textSize.X, (int)textSize.Y);

            var lblHard = new XNALabel(WindowManager);
            lblHard.Name = "lblHard";
            lblHard.FontIndex = 1;
            lblHard.Text = "HARD";
            lblHard.ClientRectangle = new Rectangle(280, 390, 1, 1);

            btnLaunch = new XNAClientButton(WindowManager);
            btnLaunch.Name = "btnLaunch";
            btnLaunch.ClientRectangle = new Rectangle(12, 424, 133, 23);
            btnLaunch.Text = "Launch";
            btnLaunch.AllowClick = false;
            btnLaunch.LeftClick += BtnLaunch_LeftClick;

            var btnCancel = new XNAClientButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.ClientRectangle = new Rectangle(178, 424, 133, 23);
            btnCancel.Text = "Cancel";
            btnCancel.LeftClick += BtnCancel_LeftClick;

            AddChild(lblSelectCampaign);
            AddChild(lblMissionDescriptionHeader);
            AddChild(lbCampaignList);
            AddChild(panelMissionDescription);
            panelMissionDescription.AddChild(lblMissionDescriptionValue);
            AddChild(lblDifficultyLevel);
            AddChild(btnLaunch);
            AddChild(btnCancel);
            AddChild(trbDifficultySelector);
            AddChild(lblEasy);
            AddChild(lblNormal);
            AddChild(lblHard);

            lblDifficultyLevel.CenterOnParent();
            lblDifficultyLevel.ClientRectangle = new Rectangle(lblDifficultyLevel.ClientRectangle.X, 324,
                lblDifficultyLevel.ClientRectangle.Width, lblDifficultyLevel.ClientRectangle.Height);

            lblNormal.CenterOnParent();
            lblNormal.ClientRectangle = new Rectangle(lblNormal.ClientRectangle.X, 390,
                1, 1);

            // Set control attributes from INI file
            base.Initialize();

            // Center on screen
            CenterOnParent();

            trbDifficultySelector.Value = UserINISettings.Instance.Difficulty;
        }

        private void LbCampaignList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbCampaignList.SelectedIndex == -1)
            {
                lblMissionDescriptionValue.Text = string.Empty;
                btnLaunch.AllowClick = false;
                return;
            }

            Mission mission = Missions[lbCampaignList.SelectedIndex];

            if (string.IsNullOrEmpty(mission.Scenario))
            {
                lblMissionDescriptionValue.Text = string.Empty;
                btnLaunch.AllowClick = false;
                return;
            }

            lblMissionDescriptionValue.Text = mission.GUIDescription;

            if (!mission.Enabled)
            {
                btnLaunch.AllowClick = false;
                return;
            }

            btnLaunch.AllowClick = true;
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Enabled = false;
        }

        private void BtnLaunch_LeftClick(object sender, EventArgs e)
        {
            int selectedMissionId = lbCampaignList.SelectedIndex;

            Mission mission = Missions[selectedMissionId];

            //if (CUpdater.IsVersionMismatch)
            //{
                // Display cheater form when it's done
                // TODO actually compare Rules.ini and the mission identifier only
            //}

            LaunchMission(mission);
        }

        /// <summary>
        /// Starts a singleplayer mission.
        /// </summary>
        /// <param name="scenario">The internal name of the scenario.</param>
        /// <param name="requiresAddon">True if the mission is for Firestorm / Enhanced Mode.</param>
        private void LaunchMission(Mission mission)
        {
            bool copyMapsToSpawnmapINI = DomainController.Instance().CopyMissionsToSpawnmapINI;

            Logger.Log("About to write spawn.ini.");
            StreamWriter swriter = new StreamWriter(MainClientConstants.gamepath + "spawn.ini");
            swriter.WriteLine("; Generated by DTA Client");
            swriter.WriteLine("[Settings]");
            if (copyMapsToSpawnmapINI)
                swriter.WriteLine("Scenario=spawnmap.ini");
            else
                swriter.WriteLine("Scenario=" + mission.Scenario);
            swriter.WriteLine("GameSpeed=" + UserINISettings.Instance.GameSpeed);
            swriter.WriteLine("Firestorm=" + mission.RequiredAddon);
            swriter.WriteLine("CustomLoadScreen=" + LoadingScreenController.GetLoadScreenName(mission.Side));
            swriter.WriteLine("IsSinglePlayer=Yes");
            swriter.WriteLine("SidebarHack=" + MCDomainController.Instance.GetSidebarHackStatus());
            swriter.WriteLine("Side=" + mission.Side);

            IniFile difficultyIni;

            UserINISettings.Instance.Difficulty.Value = trbDifficultySelector.Value;
            if (trbDifficultySelector.Value == 0) // Easy
            {
                swriter.WriteLine("DifficultyModeHuman=0");
                swriter.WriteLine("DifficultyModeComputer=2");
                difficultyIni = new IniFile(ProgramConstants.GamePath + "INI\\Map Code\\Difficulty Easy.ini");
            }
            else if (trbDifficultySelector.Value == 1) // Normal
            {
                swriter.WriteLine("DifficultyModeHuman=1");
                swriter.WriteLine("DifficultyModeComputer=1");
                difficultyIni = new IniFile(ProgramConstants.GamePath + "INI\\Map Code\\Difficulty Medium.ini");
            }
            else //if (tbDifficultyLevel.Value == 2) // Hard
            {
                swriter.WriteLine("DifficultyModeHuman=2");
                swriter.WriteLine("DifficultyModeComputer=0");
                difficultyIni = new IniFile(ProgramConstants.GamePath + "INI\\Map Code\\Difficulty Hard.ini");
            }
            swriter.WriteLine();
            swriter.WriteLine();
            swriter.WriteLine();
            swriter.Close();

            if (copyMapsToSpawnmapINI)
            {
                IniFile mapIni = new IniFile(ProgramConstants.GamePath + mission.Scenario);
                IniFile.ConsolidateIniFiles(mapIni, difficultyIni);
                mapIni.WriteIniFile(ProgramConstants.GamePath + "spawnmap.ini");
            }

            UserINISettings.Instance.Difficulty.Value = trbDifficultySelector.Value;
            UserINISettings.Instance.SaveSettings();

            Logger.Log("About to launch main executable.");

            ((MainMenuDarkeningPanel)Parent).Hide();
            SharedUILogic.StartGameProcess(0);
        }

        /// <summary>
        /// Parses a Battle(E).ini file. Returns true if succesful (file found), otherwise false.
        /// </summary>
        /// <param name="path">The path of the file, relative to the game directory.</param>
        /// <returns>True if succesful, otherwise false.</returns>
        private bool ParseBattleIni(string path)
        {
            Logger.Log("Attempting to parse " + path + " to populate mission list.");

            string battle_ini_path = MainClientConstants.gamepath + path;
            if (!File.Exists(battle_ini_path))
            {
                Logger.Log("File " + path + " not found. Ignoring.");
                return false;
            }

            IniFile battle_ini = new IniFile(battle_ini_path);

            List<string> battleKeys = battle_ini.GetSectionKeys("Battles");

            foreach (string battleEntry in battleKeys)
            {
                string battleSection = battle_ini.GetStringValue("Battles", battleEntry, "NOT FOUND");

                if (!battle_ini.SectionExists(battleSection))
                    continue;

                var mission = new Mission(battle_ini, battleSection);

                Missions.Add(mission);
               
                XNAListBoxItem item = new XNAListBoxItem();
                item.Text = mission.GUIName;
                if (!mission.Enabled)
                {
                    item.TextColor = UISettings.DisabledButtonColor;
                }
                else if (string.IsNullOrEmpty(mission.Scenario))
                {
                    item.TextColor = AssetLoader.GetColorFromString(
                        DomainController.Instance().GetListBoxHeaderColor());
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

            Logger.Log("Finished parsing " + path + ".");
            return true;
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }
    }
}
