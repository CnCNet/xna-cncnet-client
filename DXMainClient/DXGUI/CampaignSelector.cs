using ClientCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DTAClient.domain;
using System.IO;
using Updater;
using ClientGUI;
using Rampastring.XNAUI.DXControls;
using Rampastring.XNAUI;
using Rampastring.Tools;
using System.Diagnostics;

namespace DTAClient.DXGUI
{
    public class CampaignSelector : DXWindow
    {
        const int DEFAULT_WIDTH = 327;
        const int DEFAULT_HEIGHT = 463;

        public CampaignSelector(WindowManager windowManager) : base(windowManager)
        {

        }

        List<Mission> Missions = new List<Mission>();
        DXListBox lbCampaignList;
        DXLabel lblMissionDescriptionValue;
        DXButton btnLaunch;
        DXTrackbar trbDifficultySelector;

        public override void Initialize()
        {
            BackgroundTexture = AssetLoader.LoadTexture("missionselectorbg.png");
            ClientRectangle = new Rectangle(0, 0, DEFAULT_WIDTH, DEFAULT_HEIGHT);
            BorderColor = UISettings.WindowBorderColor;

            Name = "CampaignSelector";

            DXLabel lblSelectCampaign = new DXLabel(WindowManager);
            lblSelectCampaign.Name = "lblSelectCampaign";
            lblSelectCampaign.FontIndex = 1;
            lblSelectCampaign.Text = "MISSIONS:";
            lblSelectCampaign.ClientRectangle = new Rectangle(12, 9, 0, 0);

            DXLabel lblMissionDescriptionHeader = new DXLabel(WindowManager);
            lblMissionDescriptionHeader.Name = "lblMissionDescriptionHeader";
            lblMissionDescriptionHeader.FontIndex = 1;
            lblMissionDescriptionHeader.Text = "MISSION DESCRIPTION:";
            lblMissionDescriptionHeader.ClientRectangle = new Rectangle(12, 219, 0, 0);

            lbCampaignList = new DXListBox(WindowManager);
            lbCampaignList.Name = "lbCampaignList";
            lbCampaignList.FontIndex = 0;
            lbCampaignList.ItemAlphaRate = 1.0f;
            lbCampaignList.DefaultItemColor = AssetLoader.GetColorFromString(DomainController.Instance().GetUIAltColor());
            lbCampaignList.ClientRectangle = new Rectangle(12, 25, 300, 184);
            lbCampaignList.SelectedIndexChanged += LbCampaignList_SelectedIndexChanged;

            ParseBattleIni("INI\\Battle.ini");
            ParseBattleIni("INI\\" + MCDomainController.Instance.GetBattleFSFileName());

            DXPanel panelMissionDescription = new DXPanel(WindowManager);
            panelMissionDescription.Name = "panelMissionDescription";
            panelMissionDescription.ClientRectangle = new Rectangle(12, 235, 300, 76);
            panelMissionDescription.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            panelMissionDescription.Alpha = 1.0f;

            lblMissionDescriptionValue = new DXLabel(WindowManager);
            lblMissionDescriptionValue.Name = "lblMissionDescriptionValue";
            lblMissionDescriptionValue.FontIndex = 0;
            lblMissionDescriptionValue.Text = " ";
            lblMissionDescriptionValue.ClientRectangle = new Rectangle(3, 2, 0, 0);

            lbCampaignList.BackgroundTexture = AssetLoader.CreateTexture(AssetLoader.GetColorFromString(DomainController.Instance().GetUIAltBackgroundColor()),
                lbCampaignList.ClientRectangle.Width, lbCampaignList.ClientRectangle.Height);

            panelMissionDescription.BackgroundTexture = AssetLoader.CreateTexture(AssetLoader.GetColorFromString(DomainController.Instance().GetUIAltBackgroundColor()),
                panelMissionDescription.ClientRectangle.Width, panelMissionDescription.ClientRectangle.Height);

            DXLabel lblDifficultyLevel = new DXLabel(WindowManager);
            lblDifficultyLevel.Name = "lblDifficultyLevel";
            lblDifficultyLevel.Text = "DIFFICULTY LEVEL";
            lblDifficultyLevel.FontIndex = 1;
            Vector2 textSize = Renderer.GetTextDimensions(lblDifficultyLevel.Text, lblDifficultyLevel.FontIndex);
            lblDifficultyLevel.ClientRectangle = new Rectangle(0, 0, (int)textSize.X, (int)textSize.Y);

            trbDifficultySelector = new DXTrackbar(WindowManager);
            trbDifficultySelector.Name = "trbDifficultySelector";
            trbDifficultySelector.ClientRectangle = new Rectangle(12, 340, 300, 45);
            trbDifficultySelector.MinValue = 0;
            trbDifficultySelector.MaxValue = 2;
            trbDifficultySelector.BackgroundTexture = AssetLoader.LoadTexture("trackbarbg.png");

            DXLabel lblEasy = new DXLabel(WindowManager);
            lblEasy.Name = "lblEasy";
            lblEasy.FontIndex = 1;
            lblEasy.Text = "EASY";
            lblEasy.ClientRectangle = new Rectangle(12, 390, 1, 1);

            DXLabel lblNormal = new DXLabel(WindowManager);
            lblNormal.Name = "lblNormal";
            lblNormal.FontIndex = 1;
            lblNormal.Text = "NORMAL";
            textSize = Renderer.GetTextDimensions(lblNormal.Text, lblNormal.FontIndex);
            lblNormal.ClientRectangle = new Rectangle(0, 0, (int)textSize.X, (int)textSize.Y);

            DXLabel lblHard = new DXLabel(WindowManager);
            lblHard.Name = "lblHard";
            lblHard.FontIndex = 1;
            lblHard.Text = "HARD";
            lblHard.ClientRectangle = new Rectangle(280, 390, 1, 1);

            btnLaunch = new DXButton(WindowManager);
            btnLaunch.Name = "btnLaunch";
            btnLaunch.IdleTexture = AssetLoader.LoadTexture("133pxbtn.png");
            btnLaunch.HoverTexture = AssetLoader.LoadTexture("133pxbtn_c.png");
            btnLaunch.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnLaunch.ClientRectangle = new Rectangle(12, 424, 133, 23);
            btnLaunch.FontIndex = 1;
            btnLaunch.Text = "Launch";
            btnLaunch.AllowClick = false;
            btnLaunch.LeftClick += BtnLaunch_LeftClick;

            DXButton btnCancel = new DXButton(WindowManager);
            btnCancel.Name = "btnCancel";
            btnCancel.IdleTexture = AssetLoader.LoadTexture("133pxbtn.png");
            btnCancel.HoverTexture = AssetLoader.LoadTexture("133pxbtn_c.png");
            btnCancel.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnCancel.ClientRectangle = new Rectangle(178, 424, 133, 23);
            btnCancel.FontIndex = 1;
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

            trbDifficultySelector.Value = MCDomainController.Instance.GetDifficultyMode();
        }

        private void LbCampaignList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbCampaignList.SelectedIndex == -1)
            {
                lblMissionDescriptionValue.Text = " ";
                btnLaunch.AllowClick = false;
                return;
            }

            Mission mission = Missions[lbCampaignList.SelectedIndex];

            if (String.IsNullOrEmpty(mission.Scenario))
            {
                lblMissionDescriptionValue.Text = " ";
                btnLaunch.AllowClick = false;
                return;
            }

            lblMissionDescriptionValue.Text = mission.GUIDescription;
            btnLaunch.AllowClick = true;
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            ((MainMenuDarkeningPanel)Parent).Hide();
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

            LaunchMission(mission.Scenario, mission.Side, mission.RequiredAddon);
        }

        /// <summary>
        /// Starts a singleplayer mission.
        /// </summary>
        /// <param name="scenario">The internal name of the scenario.</param>
        /// <param name="requiresAddon">True if the mission is for Firestorm / Enhanced Mode.</param>
        private void LaunchMission(string scenario, int side, bool requiresAddon)
        {
            Logger.Log("About to write spawn.ini.");
            StreamWriter swriter = new StreamWriter(MainClientConstants.gamepath + "spawn.ini");
            swriter.WriteLine("; Generated by DTA Client");
            swriter.WriteLine("[Settings]");
            swriter.WriteLine("Scenario=spawnmap.ini");
            swriter.WriteLine("GameSpeed=" + MCDomainController.Instance.GetGameSpeed());
            swriter.WriteLine("Firestorm=" + requiresAddon);
            swriter.WriteLine("CustomLoadScreen=" + LoadingScreenController.GetLoadScreenName(side));
            swriter.WriteLine("IsSinglePlayer=Yes");
            swriter.WriteLine("SidebarHack=" + MCDomainController.Instance.GetSidebarHackStatus());
            swriter.WriteLine("Side=" + side);

            IniFile difficultyIni;

            MCDomainController.Instance.SaveSingleplayerSettings(trbDifficultySelector.Value);
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

            IniFile mapIni = new IniFile(ProgramConstants.GamePath + scenario);
            IniFile.ConsolidateIniFiles(mapIni, difficultyIni);
            mapIni.WriteIniFile(ProgramConstants.GamePath + "spawnmap.ini");

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

                int cd = battle_ini.GetIntValue(battleSection, "CD", 0);
                int side = battle_ini.GetIntValue(battleSection, "Side", 0);
                string scenario = battle_ini.GetStringValue(battleSection, "Scenario", String.Empty);
                string guiName = battle_ini.GetStringValue(battleSection, "Description", "Undefined mission");
                string sideName = battle_ini.GetStringValue(battleSection, "SideName", String.Empty);
                string guiDescription = battle_ini.GetStringValue(battleSection, "LongDescription", String.Empty);
                string finalMovie = battle_ini.GetStringValue(battleSection, "FinalMovie", "none");
                bool requiredAddon = battle_ini.GetBooleanValue(battleSection, "RequiredAddon", false);

                guiDescription = guiDescription.Replace("@", Environment.NewLine);

                Mission mission = new Mission(cd, side, scenario, guiName, guiDescription, finalMovie, requiredAddon);
                Missions.Add(mission);
                DXListBoxItem item = new DXListBoxItem();
                item.Text = guiName;
                if (String.IsNullOrEmpty(scenario))
                {
                    item.TextColor = AssetLoader.GetColorFromString(DomainController.Instance().GetListBoxHeaderColor());
                    item.Selectable = false;
                }
                else
                {
                    item.TextColor = lbCampaignList.DefaultItemColor;

                    if (!String.IsNullOrEmpty(sideName))
                        item.Texture = AssetLoader.LoadTexture(sideName + "icon.png");
                }

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
