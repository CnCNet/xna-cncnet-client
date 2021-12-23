using System;
using System.Collections.Generic;
using ClientCore;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer
{
    public class TeamStartMappingPanel : XNAPanel
    {
        private readonly int _start;
        private readonly int _defaultTeamIndex = -1;

        private const int ddWidth = 35;
        // private XNAClientDropDown ddStarts;
        private XNAClientDropDown ddTeams;

        public event EventHandler OptionsChanged;

        public TeamStartMappingPanel(WindowManager windowManager, int start) : base(windowManager)
        {
            _start = start;
            DrawBorders = false;
        }

        public override void Initialize()
        {
            base.Initialize();

            var startLabel = new XNALabel(WindowManager);
            startLabel.Text = _start.ToString();
            startLabel.ClientRectangle = new Rectangle(0, 0, 10, 22);
            AddChild(startLabel);

            ddTeams = new XNAClientDropDown(WindowManager);
            ddTeams.Name = nameof(ddTeams);
            ddTeams.ClientRectangle = new Rectangle(startLabel.Right, startLabel.Y - 3, ddWidth, 22);
            TeamStartMapping.TEAMS.ForEach(ddTeams.AddItem);
            AddChild(ddTeams);

            ddTeams.SelectedIndexChanged += DD_SelectedItemChanged;
        }

        private void DD_SelectedItemChanged(object sender, EventArgs e) => OptionsChanged?.Invoke(sender, e);

        public void SetTeamStartMapping(TeamStartMapping teamStartMapping)
        {
            var teamIndex = teamStartMapping?.TeamIndex ?? _defaultTeamIndex;
            
            ddTeams.SelectedIndex = teamIndex >= 0 && teamIndex < ddTeams.Items.Count ? 
                teamIndex : -1;
        }

        public void EnableControls(bool enable) => ddTeams.AllowDropDown = enable;

        public void ClearSelections() => ddTeams.SelectedIndex = _defaultTeamIndex;

        public TeamStartMapping GetTeamStartMapping()
        {
            return new TeamStartMapping()
            {
                Team = ddTeams.SelectedItem?.Text,
                Start = _start
            };
        }
    }
}
