using System;
using System.Collections.Generic;
using System.Linq;
using DTAClient.Domain.Multiplayer;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer
{
    public class TeamStartMappingsPanel : XNAPanel
    {
        public event EventHandler MappingChanged;

        public TeamStartMappingsPanel(WindowManager windowManager) : base(windowManager)
        {
            DrawBorders = false;
        }

        public List<TeamStartMappingPanel> GetTeamStartMappingPanels() =>
            Children.Select(c => c as TeamStartMappingPanel).ToList();

        public void EnableControls(bool enable) =>
            GetTeamStartMappingPanels().ForEach(panel => panel.EnableControls(enable));

        public List<TeamStartMapping> GetTeamStartMappings()
        {
            return GetTeamStartMappingPanels()
                .Where(panel => panel.HasEnabledControls())
                .Select(panel => panel.GetTeamStartMapping())
                .ToList();
        }

        public void AddMappingPanel(TeamStartMappingPanel teamStartMappingPanel)
        {
            teamStartMappingPanel.OptionsChanged += (sender, args) => MappingChanged?.Invoke(sender, args);
            AddChild(teamStartMappingPanel);
        }

        public void SetTeamStartMappings(List<TeamStartMapping> teamStartMappings)
        {
            var teamStartMappingPanels = GetTeamStartMappingPanels();
            for (var i = 0; i < teamStartMappingPanels.Count; i++)
            {
                if (teamStartMappings.Count <= i)
                {
                    teamStartMappingPanels[i].ClearSelections();
                    continue;
                }

                var teamStartMapping = teamStartMappings[i];
                teamStartMappingPanels[i].SetTeamStartMapping(teamStartMapping);
            }
        }
    }
}
