using ClientCore;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using System.Collections.Generic;
using DTAClient.Domain;
using DTAClient.Domain.Singleplayer;
using DTAClient.DXGUI.Singleplayer;
using System.IO;
using ClientGUI;
using Rampastring.XNAUI.XNAControls;
using Rampastring.XNAUI;
using Rampastring.Tools;
using ClientUpdater;
using ClientCore.Extensions;
using ClientCore.Enums;

namespace DTAClient.DXGUI.Generic
{
    public class CampaignSelector : INItializableWindow
    {
        private const int DEFAULT_WIDTH = 650;
        private const int DEFAULT_HEIGHT = 600;

        private static string[] DifficultyNames = new string[] { "Easy", "Medium", "Hard" };
        public CampaignSelector(WindowManager windowManager, DiscordHandler discordHandler) : base(windowManager)
        {
            this.discordHandler = discordHandler;
            panels = new Dictionary<string, XNAPanel>();
        }

        private DiscordHandler discordHandler;

        private Dictionary<string, XNAPanel> panels;

        private CheaterWindow cheaterWindow;

        protected XNAButton btnCancel;
        protected XNAButton btnLaunch;

        private int diffPlaceholder = 0;
        private Mission missionPlaceholder;

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

        public override void Initialize()
        {
            BackgroundTexture = AssetLoader.LoadTexture("missionselectorbg.png");
            ClientRectangle = new Rectangle(0, 0, DEFAULT_WIDTH, DEFAULT_HEIGHT);
            BorderColor = UISettings.ActiveSettings.PanelBorderColor;

            Name = "CampaignSelector";

            base.Initialize();

            // Center on screen
            CenterOnParent();

            CampaignHandler.Instance.SelectMission(0);
            missionPlaceholder = CampaignHandler.Instance.SelectedMission();

            // Cheater window nonsense
            cheaterWindow = new CheaterWindow(WindowManager);
            var dp = new DarkeningPanel(WindowManager);
            dp.AddChild(cheaterWindow);
            AddChild(dp);
            dp.CenterOnParent();
            cheaterWindow.CenterOnParent();
            cheaterWindow.YesClicked += CheaterWindow_YesClicked;
            cheaterWindow.Disable();

            //// Link Cancel Button
            btnCancel = FindChild<XNAClientButton>(nameof(btnCancel));
            if (btnCancel != null)
                btnCancel.LeftClick += BtnCancel_LeftClick;

            //// Link Launch Button
            //btnLaunch = FindChild<XNAClientButton>(nameof(btnLaunch));
            //if(btnLaunch != null)
            //    btnLaunch.LeftClick += BtnLaunch_LeftClick;

            InitChildPanels(Children.OfType<XNAPanel>().ToList());
            panels.FirstOrDefault().Value.Enable();
        }

        private void InitChildPanels(List<XNAPanel> list)
        {
            foreach (var panel in list)
            {
                if (panel is DarkeningPanel)
                    continue;
                panels.Add(panel.Name, panel);
                InitPanelControls(panel);
                panel.Disable();
            }
        }
        private void InitPanelControls(XNAPanel panel)
        {
            foreach(var control in panel.Children)
            {
                if(control is NavigationButton)
                    control.LeftClick += NavigationButton_LeftClick;
                if (control.Name == nameof(btnCancel))
                    control.LeftClick += BtnCancel_LeftClick;
            }
        }
        private void NavigationButton_LeftClick(object sender, EventArgs e)
        {
            NavigationButton button = sender as NavigationButton;
            XNAPanel next = panels[button.NextPanel];
            if (next != null)
            {
                button.Parent.Disable();
                next.Enable();
            }
            else
            {
                Logger.Log("Could not find XNAPanel: " + button.NextPanel);
            }
        }

        //private void lbCampaignList_EnabledChanged(object sender, EventArgs e)
        //{
        //    PopulateMissionList();
        //}

        //private void PopulateMissionList()
        //{
        //    lbCampaignList.Clear();

        //    foreach (var mission in CampaignHandler.Instance.Missions)
        //    {
        //        var item = new MissionListBoxItem(mission);

        //        if (!mission.Enabled)
        //        {
        //            item.TextColor = UISettings.ActiveSettings.DisabledItemColor;
        //        }
        //        else if (string.IsNullOrEmpty(mission.Scenario))
        //        {
        //            item.TextColor = AssetLoader.GetColorFromString(
        //                ClientConfiguration.Instance.ListBoxHeaderColor);
        //            item.IsHeader = true;
        //            item.Selectable = false;
        //        }
        //        else
        //        {
        //            item.TextColor = lbCampaignList.DefaultItemColor;
        //        }

        //        lbCampaignList.AddItem(item);
        //    }
        //}

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Disable();
        }

        private void BtnLaunch_LeftClick(object sender, EventArgs e)
        {
            if (!ClientConfiguration.Instance.ModMode &&
                (!Updater.IsFileNonexistantOrOriginal(missionPlaceholder.Scenario) || AreFilesModified()))
            {
                // Confront the user by showing the cheater screen
                cheaterWindow.Enable();
                return;
            }

            LaunchMission(missionPlaceholder);
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
            LaunchMission(missionPlaceholder);
        }

        /// <summary>
        /// Starts a singleplayer mission.
        /// </summary>
        private void LaunchMission(Mission mission)
        {
            CampaignHandler.Instance.StageMissionFiles(mission, diffPlaceholder);

            Disable();

            discordHandler.UpdatePresence(mission.UntranslatedGUIName, DifficultyNames[diffPlaceholder], mission.IconPath, true);
            GameProcessLogic.GameProcessExited += GameProcessExited_Callback;

            GameProcessLogic.StartGameProcess(WindowManager);
        }

        private int GetComputerDifficulty() =>
            Math.Abs(diffPlaceholder - 2);

        private void GameProcessExited_Callback()
        {
            WindowManager.AddCallback(new Action(GameProcessExited), null);
        }

        protected virtual void GameProcessExited()
        {
            GameProcessLogic.GameProcessExited -= GameProcessExited_Callback;
            // Logger.Log("GameProcessExited: Updating Discord Presence.");
            discordHandler.UpdatePresence();
        }
        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }
    }
}
