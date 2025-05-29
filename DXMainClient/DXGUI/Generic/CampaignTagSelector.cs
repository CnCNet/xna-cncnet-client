using System;
using System.Collections.Generic;
using ClientCore;
using ClientGUI;
using DTAClient.Domain;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Generic
{
    public class CampaignTagSelector : INItializableWindow
    {
        private const int DEFAULT_WIDTH = 576;
        private const int DEFAULT_HEIGHT = 475;
        private DiscordHandler discordHandler;

        public CampaignTagSelector(WindowManager windowManager, DiscordHandler discordHandler)
            : base(windowManager)
        {
            this.discordHandler = discordHandler;
        }

        public IReadOnlyDictionary<int, Mission> UniqueIDToMissions => this.CampaignSelector.UniqueIDToMissions;
        public IReadOnlyCollection<Mission> AllMissions => this.CampaignSelector.AllMissions;

        protected XNAClientButton btnCancel;
        protected XNAClientButton btnShowAllMission;
        protected XNAClientButton btnShowCustomMission;
        public override void Initialize()
        {
            CampaignSelector = new CampaignSelector(WindowManager, discordHandler, this);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, CampaignSelector);
            CampaignSelector.Disable();
            Name = nameof(CampaignTagSelector);

            if (!ClientConfiguration.Instance.CampaignTagSelectorEnabled)
                return;

            ClientRectangle = new Rectangle(0, 0, DEFAULT_WIDTH, DEFAULT_HEIGHT);
            BorderColor = UISettings.ActiveSettings.PanelBorderColor;

            base.Initialize();

            WindowManager.CenterControlOnScreen(this);

            btnCancel = FindChild<XNAClientButton>(nameof(btnCancel));
            btnCancel.LeftClick += BtnCancel_LeftClick;

            btnShowAllMission = FindChild<XNAClientButton>(nameof(btnShowAllMission));
            btnShowAllMission.LeftClick += (sender, e) =>
            {
                CampaignSelector.LoadMissionsWithFilter(null, disableCustomMissions: false, disableOfficialMissions: false);
                NoFadeSwitch();
            };

            // The following codes are disabled, in favor of a `ButtonTag_CUSTOM` button.
            // btnShowCustomMission = FindChild<XNAClientButton>(nameof(btnShowCustomMission));
            // btnShowCustomMission.LeftClick += (sender, e) =>
            // {
            //     CampaignSelector.LoadMissionsWithFilter(null, disableCustomMissions:false, disableOfficialMissions:true);
            //     CampaignSelector.Enable();
            //     Disable();
            // };

            const string TagButtonsPrefix = "ButtonTag_";
            var tagButtons = FindChildrenStartWith<XNAClientButton>(TagButtonsPrefix);
            foreach (var tagButton in tagButtons)
            {
                string tagName = tagButton.Name.Substring(TagButtonsPrefix.Length);
                tagButton.LeftClick += (sender, e) =>
                {
                    CampaignSelector.LoadMissionsWithFilter(new HashSet<string>() { tagName }, disableCustomMissions: false, disableOfficialMissions: false);
                    NoFadeSwitch();
                };
            }
        }

        private void BtnCancel_LeftClick(object sender, EventArgs e)
        {
            Disable();
        }

        public void Open()
        {
            if (ClientConfiguration.Instance.CampaignTagSelectorEnabled)
                Enable();
            else
                CampaignSelector.Enable();
        }

        public void NoFadeSwitch()
        {
            var dp = CampaignSelector.Parent as DarkeningPanel;
            dp?.ToggleFade(false);

             if (Visible)
                CampaignSelector.Enable();
            else
                CampaignSelector.Disable();

            dp?.ToggleFade(true);
            dp = Parent as DarkeningPanel;
            dp?.ToggleFade(false);

            if (Visible)
                Disable();
            else
                Enable();

            dp?.ToggleFade(true);
        }

        private CampaignSelector CampaignSelector;
    }
}