using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Media;
using System.IO;
using System.Threading;
using System.Net;
using System.Globalization;
using dtasetup.gui;
using dtasetup.domain;
using dtasetup.domain.cncnet5;
using dtasetup.persistence;
using Updater;
using DTAConfig;
using ClientCore;
using ClientGUI;

namespace dtasetup.gui
{
    public partial class MainMenu : MovableForm
    {
        private delegate void NoParamCallback();
        private delegate void SetExceptionCallback(Exception ex);
        public delegate void SetStringCallback(string d);

        public MainMenu()
        {
            Logger.Log("Loading main menu.");
            UserAgentHandler.ChangeUserAgent();
            InitializeComponent();
            this.Icon = dtasetup.Properties.Resources.dtasetup_icon;
        }

        Color updateStatusForeColor;
        bool isYR = false;
        bool versMismatch = false;

        List<PictureBox> extraPictureBoxes = new List<PictureBox>();

        private void MainMenu_Load(object sender, EventArgs e)
        {
            //AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(HandleExcept);

            isYR = DomainController.Instance().GetDefaultGame().ToUpper() == "YR";

            if (isYR)
                btnCnCNet.Enabled = false;

            this.lblCnCNetStatus.Text = string.Format(MCDomainController.Instance().GetCnCNetGameCountStatusText(),
                MCDomainController.Instance().GetShortGameName());

            ApplyTheme();

            Thread thread = new Thread(new ThreadStart(ScheduleCnCNetStatusUpdating));
            thread.Start();

            this.Location = new Point((Screen.PrimaryScreen.Bounds.Width - this.Size.Width) / 2,
                (Screen.PrimaryScreen.Bounds.Height - this.Size.Height) / 2);

            if (MCDomainController.Instance().GetModModeStatus() && !isYR)
            {
                lblUpdateStatus.Visible = false;
                lblUpdateStatus.Enabled = false;
                return;
            }

            CUpdater.OnVersionStateChanged += new CUpdater.NoParamEventHandler(Updater_OnVersionStateChanged);
            CUpdater.OnUpdateFailed += new CUpdater.SetExceptionCallback(Updater_OnUpdateFailed);
            CUpdater.FileIdentifiersUpdated += new CUpdater.NoParamEventHandler(Updater_FileIdentifiersUpdated);
            CUpdater.OnCustomComponentsOutdated += new CUpdater.NoParamEventHandler(Updater_OnCustomComponentsOutdated);

            Updater_OnVersionStateChanged();

            // check if game installation is modified
            if (CUpdater.IsVersionMismatch && !MainClientConstants.IgnoreVersionMismatch)
            {
                if (CUpdater.UPDATEMIRRORS.Count == 0)
                    return;

                MsgBoxForm msForm = new MsgBoxForm(string.Format(
                    "Some files of your {0} installation are not original, which" + Environment.NewLine +
                    "makes your installation incompatible for online play." + Environment.NewLine + Environment.NewLine +
                    "If you wish to play online it's recommended to repair your installation " + Environment.NewLine +
                    "so that it will be compatible with the latest version of {0}." + Environment.NewLine + Environment.NewLine +
                    "Do you wish to repair your {0} installation?" + Environment.NewLine +
                    "The size of the incompatible data is: {1} MB",
                    MainClientConstants.GAME_NAME_SHORT, Math.Round(CUpdater.VersionMismatchSize / 1000.0, 1)),
                    "Version mismatch detected", MessageBoxButtons.YesNo);
                DialogResult dr = System.Windows.Forms.DialogResult.OK;
                if (!isYR)
                {
                    dr = msForm.ShowDialog();
                    msForm.Dispose();
                }

                if (dr == System.Windows.Forms.DialogResult.OK)
                {
                    versMismatch = true;
                    VisitPage(StatisticsPageAddresses.GetUpdateStatsPageAddress());
                    CUpdater.DoVersionCheck();
                    new Thread(new ThreadStart(CUpdater.PerformUpdate)).Start();
                    ShowUpdateForm();
                }
            }
            else if (MCDomainController.Instance().getAutomaticUpdateStatus())
            {
                lblUpdateStatus_Click(this, EventArgs.Empty);
            }
        }

        private void VisitPage(string url)
        {
            Logger.Log("Sending pageview: " + url);
            wbDataCollector.Navigate(url);
        }

        private void ShowUpdateForm()
        {
            UpdateForm uf = new UpdateForm();
            uf.ShowDialog();
            uf.Dispose();
        }

        private void ApplyTheme()
        {
            SoundPlayer sPlayer = new SoundPlayer(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + "MainMenu\\button.wav");
            this.BackgroundImage = SharedUILogic.LoadImage("MainMenu\\mainmenubg.png");
            btnNewCampaign.DefaultImage = SharedUILogic.LoadImage("MainMenu\\campaign.png");
            btnNewCampaign.HoveredImage = SharedUILogic.LoadImage("MainMenu\\campaign_c.png");
            btnNewCampaign.RefreshSize();
            btnNewCampaign.HoverSound = sPlayer;
            btnLoadGame.DefaultImage = SharedUILogic.LoadImage("MainMenu\\loadmission.png");
            btnLoadGame.HoveredImage = SharedUILogic.LoadImage("MainMenu\\loadmission_c.png");
            btnLoadGame.RefreshSize();
            btnLoadGame.HoverSound = sPlayer;
            btnCnCNet.DefaultImage = SharedUILogic.LoadImage("MainMenu\\cncnet.png");
            btnCnCNet.HoveredImage = SharedUILogic.LoadImage("MainMenu\\cncnet_c.png");
            btnCnCNet.RefreshSize();
            btnCnCNet.HoverSound = sPlayer;
            btnLan.DefaultImage = SharedUILogic.LoadImage("MainMenu\\lan.png");
            btnLan.HoveredImage = SharedUILogic.LoadImage("MainMenu\\lan_c.png");
            btnLan.HoverSound = sPlayer;
            btnStatistics.DefaultImage = SharedUILogic.LoadImage("MainMenu\\statistics.png");
            btnStatistics.HoveredImage = SharedUILogic.LoadImage("MainMenu\\statistics_c.png");
            btnStatistics.RefreshSize();
            btnStatistics.HoverSound = sPlayer;
            btnSkirmish.DefaultImage = SharedUILogic.LoadImage("MainMenu\\skirmish.png");
            btnSkirmish.HoveredImage = SharedUILogic.LoadImage("MainMenu\\skirmish_c.png");
            btnSkirmish.RefreshSize();
            btnSkirmish.HoverSound = sPlayer;
            btnOptions.DefaultImage = SharedUILogic.LoadImage("MainMenu\\options.png");
            btnOptions.HoveredImage = SharedUILogic.LoadImage("MainMenu\\options_c.png");
            btnOptions.RefreshSize();
            btnOptions.HoverSound = sPlayer;
            btnMapEditor.DefaultImage = SharedUILogic.LoadImage("MainMenu\\mapeditor.png");
            btnMapEditor.HoveredImage = SharedUILogic.LoadImage("MainMenu\\mapeditor_c.png");
            btnMapEditor.RefreshSize();
            btnMapEditor.HoverSound = sPlayer;
            btnExit.DefaultImage = SharedUILogic.LoadImage("MainMenu\\exitgame.png");
            btnExit.HoveredImage = SharedUILogic.LoadImage("MainMenu\\exitgame_c.png");
            btnExit.RefreshSize();
            btnExit.HoverSound = sPlayer;
            btnCredits.DefaultImage = SharedUILogic.LoadImage("MainMenu\\credits.png");
            btnCredits.HoveredImage = SharedUILogic.LoadImage("MainMenu\\credits_c.png");
            btnCredits.RefreshSize();
            btnCredits.HoverSound = sPlayer;
            btnExtras.DefaultImage = SharedUILogic.LoadImage("MainMenu\\extras.png");
            btnExtras.HoveredImage = SharedUILogic.LoadImage("MainMenu\\extras_c.png");
            btnExtras.RefreshSize();
            btnExtras.HoverSound = sPlayer;

            this.Icon = Icon.ExtractAssociatedIcon(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + "mainclienticon.ico");

            this.Text = MainClientConstants.GAME_NAME_LONG;

            if (File.Exists(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + "mainmenuanim.gif"))
                pictureBox1.Image = Utilities.LoadImageFromFile(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + "mainmenuanim.gif");
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            updateStatusForeColor = lblUpdateStatus.ForeColor;
        }

        private void MainMenu_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            Logger.Log("Exiting.");
            Environment.Exit(1);
        }

        private void btnCnCNet_Click(object sender, EventArgs e)
        {
            SaveSettings();

            UserAgentHandler.ChangeUserAgent();
            VisitPage(StatisticsPageAddresses.GetCnCNetStatsPageAddress());

            ProcessStartInfo startInfo = new ProcessStartInfo(MainClientConstants.gamepath + "cncnetclient.dat");
            startInfo.Arguments = "\"-RESDIR=" + ProgramConstants.RESOURCES_DIR.Remove(ProgramConstants.RESOURCES_DIR.Length - 1) + "\"";
            startInfo.Arguments = startInfo.Arguments + " -VER" + CUpdater.GameVersion;

            startInfo.UseShellExecute = false;

            Process clientProcess = new Process();
            clientProcess.EnableRaisingEvents = true;
            clientProcess.StartInfo = startInfo;

            this.Visible = false;
            clientProcess.Start();

            clientProcess.WaitForExit();

            if (clientProcess.ExitCode == 1337)
            {
                Logger.Log("The CnCNet client was switched - exiting.");
                Environment.Exit(0);
            }
            else if (clientProcess.ExitCode == 1338)
            {
                MessageBox.Show("Editing game files while connected to CnCNet is not allowed."
                    + Environment.NewLine + Environment.NewLine +
                    "If you got this message without editing files, visit " + Environment.NewLine +
                    MainClientConstants.SUPPORT_URL + " for troubleshooting tips and support.");

                if (isYR)
                    Environment.Exit(0);
            }

            MCDomainController.Instance().ReloadSettings();

            if (CUpdater.LocalFileInfos.Find(fi => fi.Name == "INI\\Default.ini") == null)
                File.Delete(ProgramConstants.gamepath + "INI\\Default.ini");

            if (isYR)
                CUpdater.DoVersionCheck();

            this.Show();
        }

        private void btnLan_Click(object sender, EventArgs e)
        {
            SaveSettings();

            ProcessStartInfo startInfo = new ProcessStartInfo(MainClientConstants.gamepath + "cncnetclient.dat");
            startInfo.Arguments = "\"-RESDIR=" + ProgramConstants.RESOURCES_DIR.Remove(ProgramConstants.RESOURCES_DIR.Length - 1) + "\"";
            startInfo.Arguments = startInfo.Arguments + " -VER" + CUpdater.GameVersion + " -LAN";
            startInfo.UseShellExecute = false;

            Process clientProcess = new Process();
            clientProcess.StartInfo = startInfo;

            this.Hide();
            clientProcess.Start();

            clientProcess.WaitForExit();

            this.Show();

            MCDomainController.Instance().ReloadSettings();
        }

        private void btnSkirmish_Click(object sender, EventArgs e)
        {
            SaveSettings();

            ProcessStartInfo startInfo = new ProcessStartInfo(MainClientConstants.gamepath + "cncnetclient.dat");
            startInfo.Arguments = "\"-RESDIR=" + ProgramConstants.RESOURCES_DIR.Remove(ProgramConstants.RESOURCES_DIR.Length - 1) + "\"";
            startInfo.Arguments = startInfo.Arguments + " -VER" + CUpdater.GameVersion + " -SKIRMISH";
            startInfo.UseShellExecute = false;

            Process clientProcess = new Process();
            clientProcess.StartInfo = startInfo;

            this.Hide();
            clientProcess.Start();

            clientProcess.WaitForExit();

            this.Show();

            MCDomainController.Instance().ReloadSettings();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Logger.Log("Exiting.");

            if (isYR)
            {
                File.Delete(ProgramConstants.gamepath + "ddraw.dll");
            }

            Environment.Exit(0);
        }

        private void btnCredits_Click(object sender, EventArgs e)
        {
            Process.Start(MainClientConstants.CREDITS_URL);
        }

        private void btnNewCampaign_Click(object sender, EventArgs e)
        {
            SaveSettings();
            Logger.Log("Opening mission selector.");
            DialogResult dr = new CampaignSelector().ShowDialog();

            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                WatchDTA();

                this.Show();
            }
        }

        private void btnMapEditor_Click(object sender, EventArgs e)
        {
            Process.Start(ProgramConstants.gamepath + MCDomainController.Instance().GetMapEditorExePath());
        }

        private void btnLoadGame_Click(object sender, EventArgs e)
        {
            SaveSettings();
            DialogResult dr = new LoadMissionForm().ShowDialog();

            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                WatchDTA();

                this.Show();
            }
        }

        private void btnStatistics_Click(object sender, EventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(ProgramConstants.gamepath + "statistics.dat");
            startInfo.Arguments = "\"-RESDIR=" + ProgramConstants.RESOURCES_DIR.Remove(ProgramConstants.RESOURCES_DIR.Length - 1) + "\"";
            startInfo.UseShellExecute = false;

            Logger.Log("Starting DTAScore viewer.");

            Process process = new Process();
            process.StartInfo = startInfo;
            this.Hide();

            process.Start();

            Logger.Log("Waiting for DTAScore viewer to exit.");

            process.EnableRaisingEvents = true;
            process.WaitForExit();

            Logger.Log("DTAScore viewer has exited - displaying menu.");

            this.Show();
        }

        private void btnOptions_Click(object sender, EventArgs e)
        {
            if (CUpdater.UPDATEMIRRORS == null)
                CUpdater.Initialize();

            new OptionsForm().ShowDialog();
            DomainController.Instance().ReloadSettings();
            int themeId = DomainController.Instance().GetSelectedThemeId();
            string resDir = "Resources\\" + DomainController.Instance().GetThemeInfoFromIndex(themeId)[1];
            
            if (ProgramConstants.RESOURCES_DIR != resDir)
            {
                // The theme was changed; clear current theme and apply the new one

                for (int i = 0; i < extraPictureBoxes.Count;)
                {
                    pictureBox1.Controls.Remove(extraPictureBoxes[0]);
                    extraPictureBoxes.RemoveAt(0);
                }

                ProgramConstants.RESOURCES_DIR = resDir;
                DomainController.Instance().ReloadSettings();
                ApplyTheme();
                Reinitialize();
                SharedUILogic.SetControlStyle(new IniFile(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + "MainMenu.ini"), this);

                if (File.Exists(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + "mainmenuanim.gif"))
                    pictureBox1.Image = Utilities.LoadImageFromFile(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + "mainmenuanim.gif");
                else
                    pictureBox1.Image = null;
            }
            
            MCDomainController.Instance().ReloadSettings();
        }

        private void btnExtras_Click(object sender, EventArgs e)
        {
            ExtrasForm ef = new ExtrasForm();
            ef.ShowDialog();
            ef.Dispose();
        }

        private void Updater_FileIdentifiersUpdated()
        {
            if (lblUpdateStatus.InvokeRequired)
            {
                NoParamCallback d = new NoParamCallback(Updater_FileIdentifiersUpdated);
                this.Invoke(d, null);
            }
            else
            {
                if (CUpdater.GameVersion == CUpdater.ServerGameVersion)
                {
                    lblUpdateStatus.Text = MainClientConstants.GAME_NAME_SHORT + " is up to date (v. " + CUpdater.GameVersion + ").";
                }
                else
                {
                    if (!this.Visible)
                        return;

                    FormCollection collection = Application.OpenForms;
                    foreach (Form form in collection)
                    {
                        switch (form.Name)
                        {
                            case "CampaignSelector":
                                return;
                            case "LoadMissionForm":
                                return;
                            case "UpdateQueryForm":
                                return;
                        }
                    }

                    if (versMismatch)
                        return;

                    UpdateQueryForm uqf = new UpdateQueryForm(CUpdater.ServerGameVersion, CUpdater.UpdateSizeInKb);

                    DialogResult dr = DialogResult.OK;

                    if (!isYR)
                    {
                        dr = uqf.ShowDialog();
                        uqf.Dispose();
                    }

                    if (dr == System.Windows.Forms.DialogResult.OK)
                    {
                        this.Hide();
                        Thread thread = new Thread(new ThreadStart(CUpdater.PerformUpdate));
                        thread.Start();
                        UpdateForm uf = new UpdateForm();
                        uf.ShowDialog();
                        uf.Dispose();
                        this.Show();
                    }
                    else
                    {
                        lblUpdateStatus.Text = "Click here to download the update.";
                        lblUpdateStatus.ForeColor = Color.Goldenrod;
                    }
                }
            }
        }

        void Updater_OnCustomComponentsOutdated()
        {
            if (this.InvokeRequired)
            {
                NoParamCallback d = new NoParamCallback(Updater_OnCustomComponentsOutdated);
                this.Invoke(d, null);
            }
            else
            {
                if (!this.Visible)
                    return;

                FormCollection collection = Application.OpenForms;
                foreach (Form form in collection)
                {
                    switch (form.Name)
                    {
                        case "OptionsForm":
                            return;
                        case "CampaignSelector":
                            return;
                        case "LoadMissionForm":
                            return;
                    }
                }

                DialogResult dr = new MsgBoxForm("Updates for custom components are available. Do you wish to open the" + Environment.NewLine +
                                                "options menu where you can update the outdated custom components?",
                                                "Custom Component Updates Available", MessageBoxButtons.YesNo).ShowDialog();

                if (dr == System.Windows.Forms.DialogResult.OK)
                    new OptionsForm().ShowDialog();
            }
        }

        private void Updater_OnUpdateFailed(Exception ex)
        {
            if (this.InvokeRequired)
            {
                SetExceptionCallback d = new SetExceptionCallback(Updater_OnUpdateFailed);
                this.Invoke(d, new object[1] { ex });
            }
            else
            {
                versMismatch = false;
                new MsgBoxForm(string.Format("Error getting updates: " + ex.Message + Environment.NewLine +
                    "Please see the file client.log for more details." + Environment.NewLine + Environment.NewLine +
                    "If you are connected to the Internet and your firewall isn't blocking" + Environment.NewLine +
                    "{0}, and the issue is reproducable, contact us at {1}",
                    CUpdater.CURRENT_LAUNCHER_NAME, MainClientConstants.SUPPORT_URL_SHORT),
                    "Updater Error", MessageBoxButtons.OK).ShowDialog();
            }
        }

        private void Updater_OnVersionStateChanged()
        {
            if (lblUpdateStatus.InvokeRequired)
            {
                NoParamCallback d = new NoParamCallback(Updater_OnVersionStateChanged);
                this.Invoke(d, null);
            }
            else
            {
                lblVersion.Text = "Version: " + CUpdater.GameVersion;
                lblUpdateStatus.ForeColor = updateStatusForeColor;

                //Logger.Log("Version state changed.");
                //Logger.Log(CUpdater.DTAVersionState.ToString());

                if (isYR)
                    btnCnCNet.Enabled = true;

                switch (CUpdater.DTAVersionState)
                {
                    case VersionState.UNKNOWN:
                        if (CUpdater.HasVersionBeenChecked)
                        {
                            lblUpdateStatus.Text = "Error. See file client.log for details.";
                        }
                        else
                        {
                            lblUpdateStatus.ForeColor = Color.Goldenrod;
                            lblUpdateStatus.Text = "Click here to check for updates.";
                        }
                        break;
                    case VersionState.UPDATECHECKINPROGRESS:
                        lblUpdateStatus.Text = "Checking for updates...";
                        break;
                    case VersionState.UPDATEINPROGRESS:
                        lblUpdateStatus.Text = "An update is in progress...";
                        break;
                    case VersionState.UPTODATE:
                        versMismatch = false;
                        lblUpdateStatus.Text = MainClientConstants.GAME_NAME_SHORT + " is up to date.";
                        if (isYR)
                            btnCnCNet.Enabled = true;
                        break;
                    case VersionState.OUTDATED:
                        lblUpdateStatus.Text = "An update is available.";
                        lblUpdateStatus.ForeColor = Color.Goldenrod;
                        break;
                    case VersionState.MISMATCHED:
                        lblUpdateStatus.Text = "Version mismatch. Updating is recommended.";
                        lblUpdateStatus.ForeColor = Color.Goldenrod;
                        break;
                }
            }
        }

        private void WatchDTA()
        {
            this.Hide();

            string processName = MainClientConstants.QRES_EXECUTABLE;

            Logger.Log("Waiting for qres.dat to exit.");

            Process[] qresName = Process.GetProcessesByName(processName);

            if (qresName.Length > 0)
                qresName[0].WaitForExit();

            string mainExe = DomainController.Instance().GetGameExecutableName(0);

            Logger.Log("Waiting for " + mainExe + " to exit.");

            Process[] pname = Process.GetProcessesByName(mainExe);

            if (pname.Length > 0)
                pname[0].WaitForExit();

            Logger.Log("Both processes have exited; returning back to menu.");

            Logger.Log("Reloading settings.");
            MCDomainController.Instance().ReloadSettings();
        }

        private void SaveSettings()
        {
            if (!File.Exists(MainClientConstants.gamepath + MainClientConstants.LANGUAGE_DLL))
            {
                Logger.Log("Language.dll doesn't exist - writing default 800x600 DLL.");
                LanguageDllId langdll = LanguageDllId.DTA800X600;
                GameFileManagement.setLanguageDll(langdll);
            }

            OptionsForm of = new OptionsForm();
            of.UpdateSettings();
        }

        private void ScheduleCnCNetStatusUpdating()
        {
            Logger.Log("Scheduling CnCNet live status parsing.");
            SetCnCNetStatus(CnCNetInfoController.getCnCNetGameCount());

            while (true)
            {
                long currentTicks = DateTime.Now.Ticks;

                while (true)
                {
                    long newTicks = DateTime.Now.Ticks;
                    if ((newTicks - currentTicks) > 100000000)
                        break;
                    else
                        Thread.Sleep(1000);
                }

                SetCnCNetStatus(CnCNetInfoController.getCnCNetGameCount());
            }
        }

        private void SetCnCNetStatus(int playercount)
        {
            if (playercount == -1)
            {
                Logger.Log("An error occured while trying to check the status of CnCNet.");
                setCnCNetStatusText("Connection error");
            }
            else if (playercount == 1)
            {
                setCnCNetStatusText("1");
            }
            else
            {
                setCnCNetStatusText(Convert.ToString(playercount));
            }
        }

        private void setCnCNetStatusText(string text)
        {
            if (lblCnCNetNumPlayers.InvokeRequired)
            {
                SetStringCallback d = new SetStringCallback(setCnCNetStatusText);
                this.Invoke(d, new Object[] { text });
            }
            else
                lblCnCNetNumPlayers.Text = text;
        }

        private void lblUpdateStatus_Click(object sender, EventArgs e)
        {
            if (CUpdater.DTAVersionState == VersionState.UPDATEINPROGRESS || CUpdater.DTAVersionState == VersionState.UPDATECHECKINPROGRESS)
            {
                return;
            }

            lblUpdateStatus.ForeColor = updateStatusForeColor;
            VisitPage(StatisticsPageAddresses.GetUpdateStatsPageAddress());
            Thread thread = new Thread(new ThreadStart(CUpdater.DoVersionCheck));
            thread.Start();
        }

        private void HandleExcept(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            MessageBox.Show(string.Format("{0} has crashed. If you were in the middle of doing something, the operation has been canceled." + Environment.NewLine + Environment.NewLine +
                ex.Message + Environment.NewLine + "Please see launcher.log for more info." + Environment.NewLine +
                "If the issue is repeatable, contact Rampastring at {1} (or Rampastring if you happen to know him).",
                MainClientConstants.GAME_NAME_LONG, MainClientConstants.SUPPORT_URL_SHORT),
            "KABOOOOOOOM", MessageBoxButtons.OK);

            Logger.Log("An unhandled exception has occured. Info:");
            Logger.Log("Message: " + ex.Message);
            Logger.Log("Source: " + ex.Source);
            Logger.Log("TargetSite.Name: " + ex.TargetSite.Name);
            Logger.Log("Stacktrace: " + ex.StackTrace);
        }

        private void lblUpdateStatus_MouseEnter(object sender, EventArgs e)
        {
            if (CUpdater.DTAVersionState == VersionState.MISMATCHED)
            {
                lblUpdateStatus.Text = "Click here to check for updates.";
                lblUpdateStatus.ForeColor = Color.Goldenrod;
            }
            else if (CUpdater.DTAVersionState == VersionState.OUTDATED)
            {
                lblUpdateStatus.Text = "Click here to download the update.";
                lblUpdateStatus.ForeColor = Color.Goldenrod;
            }
        }
    }
}
