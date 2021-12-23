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
        private const int ddWidth = 35;
        private XNAClientDropDown ddStarts;
        private XNAClientDropDown ddTeams;

        public event EventHandler OptionsChanged;

        public TeamStartMappingPanel(WindowManager windowManager) : base(windowManager)
        {
            DrawBorders = false;
        }

        public override void Initialize()
        {
            base.Initialize();

            ddTeams = new XNAClientDropDown(WindowManager);
            ddTeams.Name = nameof(ddTeams);
            ddTeams.ClientRectangle = new Rectangle(0, 0, ddWidth, 22);
            ProgramConstants.TEAMS.ForEach(ddTeams.AddItem);
            AddChild(ddTeams);

            ddStarts = new XNAClientDropDown(WindowManager);
            ddStarts.Name = nameof(ddStarts);
            ddStarts.ClientRectangle = new Rectangle(ddTeams.Right + 4, ddTeams.Y, ddWidth, 22);
            AddChild(ddStarts);

            ddTeams.SelectedIndexChanged += DD_SelectedItemChanged;
            ddStarts.SelectedIndexChanged += DD_SelectedItemChanged;
        }

        public void UpdateStartCount(int startCount)
        {
            ddStarts.Items.Clear();
            for (var i = 1; i <= startCount; i++)
                ddStarts.AddItem(i.ToString());
        }

        private void DD_SelectedItemChanged(object sender, EventArgs e)
        {
            OptionsChanged?.Invoke(sender, e);
        }

        public void SetTeamStartMapping(TeamStartMapping teamStartMapping)
        {
            var teamIndex = teamStartMapping?.TeamIndex ?? -1;
            var startIndex = teamStartMapping?.StartIndex ?? -1;
            
            ddTeams.SelectedIndex = teamIndex >= 0 && teamIndex < ddTeams.Items.Count ? 
                teamIndex : -1;
            ddStarts.SelectedIndex = startIndex >= 0 && startIndex < ddStarts.Items.Count ? 
                startIndex : -1;
        }

        public void EnableControls(bool enable)
        {
            ddTeams.AllowDropDown = enable;
            ddStarts.AllowDropDown = enable;
        }

        public void ClearSelections()
        {
            ddTeams.SelectedIndex = -1;
            ddStarts.SelectedIndex = -1;
        }

        public TeamStartMapping GetTeamStartMapping()
        {
            return new TeamStartMapping()
            {
                Team = ddTeams.SelectedItem?.Text,
                Start = ddStarts.SelectedIndex + 1
            };
        }

        public bool HasEnabledControls() => ddTeams.AllowDropDown && ddStarts.AllowDropDown;
    }
}
