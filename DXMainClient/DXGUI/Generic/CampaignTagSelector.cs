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
        private string _iniSectionName = nameof(CampaignTagSelector);
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
            CampaignSelector = new CampaignSelector(WindowManager, discordHandler);
            DarkeningPanel.AddAndInitializeWithControl(WindowManager, CampaignSelector);
            CampaignSelector.Disable();

            Name = _iniSectionName;

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
                CampaignSelector.LoadMissionsWithFilter(null);
                CampaignSelector.Enable();
                Disable();
            };

            const string TagButtonsPrefix = "ButtonTag_";
            var tagButtons = FindChildrenStartWith<XNAClientButton>(TagButtonsPrefix);
            foreach (var tagButton in tagButtons)
            {
                string tagName = tagButton.Name.Substring(TagButtonsPrefix.Length);
                tagButton.LeftClick += (sender, e) =>
                {
                    CampaignSelector.LoadMissionsWithFilter(new HashSet<string>() { tagName });
                    CampaignSelector.Enable();
                    Disable();
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

        private CampaignSelector CampaignSelector;
    }
}