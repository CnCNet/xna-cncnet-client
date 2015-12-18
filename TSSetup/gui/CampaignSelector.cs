using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Media;
using System.IO;
using System.Diagnostics;
using dtasetup.domain;
using dtasetup.persistence;
using ClientCore;
using ClientGUI;
using Updater;

namespace dtasetup.gui
{
    /// <summary>
    /// A form for selecting and launching campaigns and missions.
    /// </summary>
    public partial class CampaignSelector : Form
    {
        /// <summary>
        /// Creates a new instance of the form.
        /// </summary>
        public CampaignSelector()
        {
            InitializeComponent();
            
            this.Icon = dtasetup.Properties.Resources.dtasetup_icon;
        }

        List<Mission> Missions;

        Color cListBoxFocusColor;

        /// <summary>
        /// Sets up the form's theme, loads settings and initializes the battle list.
        /// </summary>
        private void CampaignSelector_Load(object sender, EventArgs e)
        {
            Missions = new List<Mission>();

            SoundPlayer sPlayer = new SoundPlayer(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + "button.wav");

            this.ForeColor = Utilities.GetColorFromString(DomainController.Instance().GetUILabelColor());
            btnCancel.ForeColor = Utilities.GetColorFromString(DomainController.Instance().GetUIAltColor());
            btnCancel.DefaultImage = SharedUILogic.LoadImage("133pxbtn.png");
            btnCancel.HoveredImage = SharedUILogic.LoadImage("133pxbtn_c.png");
            btnCancel.HoverSound = sPlayer;
            btnLaunch.DefaultImage = btnCancel.DefaultImage;
            btnLaunch.HoveredImage = btnCancel.HoveredImage;
            btnLaunch.HoverSound = sPlayer;
            btnLaunch.ForeColor = btnCancel.ForeColor;
            lbCampaignList.ForeColor = btnCancel.ForeColor;

            cListBoxFocusColor = SharedUILogic.GetColorFromString(DomainController.Instance().GetListBoxFocusColor());

            this.Font = Utilities.GetFont(DomainController.Instance().GetCommonFont());
            lbCampaignList.BackColor = Utilities.GetColorFromString(DomainController.Instance().GetUIAltBackgroundColor());
            panel1.BackColor = lbCampaignList.BackColor;
            this.BackColor = lbCampaignList.BackColor;

            this.BackgroundImage = SharedUILogic.LoadImage("missionselectorbg.png");
            tbDifficultyLevel.BackColor = Utilities.GetColorFromString(DomainController.Instance().GetTrackBarBackColor());

            this.Icon = Icon.ExtractAssociatedIcon(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + "mainclienticon.ico");

            string[] difficultySettings = MCDomainController.Instance().GetDifficultySettings();
            lblEasy.Text = difficultySettings[0];
            lblMedium.Text = difficultySettings[1];
            lblHard.Text = difficultySettings[2];

            // Load Battle(E).ini contents
            ParseBattleIni("INI\\Battle.ini");
            ParseBattleIni("INI\\" + MCDomainController.Instance().GetBattleFSFileName());
            tbDifficultyLevel.Value = MCDomainController.Instance().getDifficultyMode();

            SharedUILogic.ParseClientThemeIni(this);
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
            if (File.Exists(battle_ini_path))
            {
                IniFile battle_ini = new IniFile(battle_ini_path);

                List<string> battleKeys = battle_ini.GetSectionKeys("Battles");

                foreach (string battleEntry in battleKeys)
                {
                    string battleSection = battle_ini.GetStringValue("Battles", battleEntry, "NOT FOUND");

                    if (battle_ini.SectionExists(battleSection))
                    {
                        int cd = battle_ini.GetIntValue(battleSection, "CD", 0);
                        int side = battle_ini.GetIntValue(battleSection, "Side", 0);
                        string scenario = battle_ini.GetStringValue(battleSection, "Scenario", String.Empty);
                        string guiName = battle_ini.GetStringValue(battleSection, "Description", "Undefined mission");
                        string guiDescription = battle_ini.GetStringValue(battleSection, "LongDescription", String.Empty);
                        string finalMovie = battle_ini.GetStringValue(battleSection, "FinalMovie", "none");
                        bool requiredAddon = battle_ini.GetBooleanValue(battleSection, "RequiredAddon", false);

                        guiDescription = guiDescription.Replace("@", Environment.NewLine);

                        Mission mission = new Mission(cd, side, scenario, guiName, guiDescription, finalMovie, requiredAddon);
                        Missions.Add(mission);
                        lbCampaignList.Items.Add(guiName);
                    }
                }

                Logger.Log("Finished parsing " + path + ".");
                return true;
            }

            Logger.Log("File " + path + " not found. Ignoring.");
            return false;
        }

        /// <summary>
        /// Refreshes the mission description when the user selects a battle from the list.
        /// </summary>
        private void lbCampaignList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbCampaignList.SelectedIndex > -1)
                lblMissionDescription.Text = Missions[lbCampaignList.SelectedIndex].GUIDescription;
            else
                lblMissionDescription.Text = String.Empty;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void btnLaunch_Click(object sender, EventArgs e)
        {
            int selectedMissionId = lbCampaignList.SelectedIndex;

            if (selectedMissionId == -1)
            {
                new MsgBoxForm("Please select a mission to play.", "No mission selected", MessageBoxButtons.OK).ShowDialog();
                return;
            }

            Mission mission = Missions[selectedMissionId];

            if (String.IsNullOrEmpty(mission.Scenario))
            {
                new MsgBoxForm("Please select a proper mission.", "Invalid mission", MessageBoxButtons.OK).ShowDialog();
                return;
            }

            if (CUpdater.IsVersionMismatch)
            {
                DialogResult dr = new CheaterForm().ShowDialog();

                if (dr == System.Windows.Forms.DialogResult.No)
                    return;
            }

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
            swriter.WriteLine("; generated by DTA Launcher");
            swriter.WriteLine("[Settings]");
            swriter.WriteLine("Scenario=spawnmap.ini");
            swriter.WriteLine("GameSpeed=" + MCDomainController.Instance().getGameSpeed());
            swriter.WriteLine("Firestorm=" + requiresAddon);
            int numLoadingScreens = ClientCore.DomainController.Instance().GetLoadScreenCount();
            swriter.WriteLine("CustomLoadScreen=" + ClientCore.LoadingScreenController.GetLoadScreenName(side, numLoadingScreens));
            swriter.WriteLine("IsSinglePlayer=Yes");
            swriter.WriteLine("SidebarHack=" + MCDomainController.Instance().GetSidebarHackStatus());
            swriter.WriteLine("Side=" + side);

            IniFile difficultyIni;

            MCDomainController.Instance().saveSingleplayerSettings(tbDifficultyLevel.Value);
            if (tbDifficultyLevel.Value == 0) // Easy
            {
                swriter.WriteLine("DifficultyModeHuman=0");
                swriter.WriteLine("DifficultyModeComputer=2");
                difficultyIni = new IniFile(ProgramConstants.gamepath + "INI\\Map Code\\Difficulty Easy.ini");
            }
            else if (tbDifficultyLevel.Value == 1) // Normal
            {
                swriter.WriteLine("DifficultyModeHuman=1");
                swriter.WriteLine("DifficultyModeComputer=1");
                difficultyIni = new IniFile(ProgramConstants.gamepath + "INI\\Map Code\\Difficulty Medium.ini");
            }
            else //if (tbDifficultyLevel.Value == 2) // Hard
            {
                swriter.WriteLine("DifficultyModeHuman=2");
                swriter.WriteLine("DifficultyModeComputer=0");
                difficultyIni = new IniFile(ProgramConstants.gamepath + "INI\\Map Code\\Difficulty Hard.ini");
            }
            swriter.WriteLine();
            swriter.WriteLine();
            swriter.WriteLine();
            swriter.Close();

            IniFile mapIni = new IniFile(ProgramConstants.gamepath + scenario);
            IniFile.ConsolidateIniFiles(mapIni, difficultyIni);
            mapIni.WriteIniFile(ProgramConstants.gamepath + "spawnmap.ini");

            Logger.Log("About to launch main executable.");

            if (MCDomainController.Instance().getWindowedStatus())
            {
                Logger.Log("Windowed mode is enabled - using QRes.");
                Process QResProcess = new Process();
                QResProcess.StartInfo.FileName = MainClientConstants.QRES_EXECUTABLE;
                QResProcess.StartInfo.UseShellExecute = false;
                QResProcess.StartInfo.Arguments = "c=16 /R " + "\"" + MainClientConstants.gamepath + DomainController.Instance().GetGameExecutableName(0) + "\"" + " -SPAWN";
                QResProcess.Start();

                if (Environment.ProcessorCount > 1)
                    QResProcess.ProcessorAffinity = (System.IntPtr)2;
            }
            else
            {
                Process DtaProcess = new Process();
                DtaProcess.StartInfo.FileName = DomainController.Instance().GetGameExecutableName(0);
                DtaProcess.StartInfo.UseShellExecute = false;
                DtaProcess.StartInfo.Arguments = "-SPAWN";
                DtaProcess.Start();

                if (Environment.ProcessorCount > 1)
                    DtaProcess.ProcessorAffinity = (System.IntPtr)2;
            }

            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        // Code for handling moving of the window with custom borders, added 13. 12. 2011
        // http://stackoverflow.com/questions/302680/custom-dialog-with-a-text-field-in-winmobile#305732

        private bool _Moving = false;
        private Point _Offset;

        private void CampaignSelector_MouseDown(object sender, MouseEventArgs e)
        {
            _Moving = true;
            _Offset = new Point(e.X, e.Y);
        }

        private void CampaignSelector_MouseMove(object sender, MouseEventArgs e)
        {
            if (_Moving)
            {
                Point newlocation = this.Location;
                newlocation.X += e.X - _Offset.X;
                newlocation.Y += e.Y - _Offset.Y;
                this.Location = newlocation;
            }
        }

        private void CampaignSelector_MouseUp(object sender, MouseEventArgs e)
        {
            if (_Moving)
            {
                _Moving = false;
            }
        }

        private void lbCampaignList_DrawItem(object sender, DrawItemEventArgs e)
        {
            ListBox lb = (ListBox)sender;

            if (e.Index > -1 && e.Index < lb.Items.Count)
            {
                if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                    e = new DrawItemEventArgs(e.Graphics,
                                              e.Font,
                                              e.Bounds,
                                              e.Index,
                                              e.State ^ DrawItemState.Selected,
                                              e.ForeColor,
                                              cListBoxFocusColor);

                e.DrawBackground();
                e.DrawFocusRectangle();

                Color foreColor = lb.ForeColor;
                e.Graphics.DrawString(lb.Items[e.Index].ToString(), e.Font, new SolidBrush(foreColor), e.Bounds);
            }
        }
    }
}
