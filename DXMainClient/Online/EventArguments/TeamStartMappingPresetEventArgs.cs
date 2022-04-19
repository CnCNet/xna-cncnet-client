using System;
using DTAClient.Domain.Multiplayer;

namespace DTAClient.Online.EventArguments
{
    public class TeamStartMappingPresetEventArgs : EventArgs
    {
        public Map Map { get; set; }

        public TeamStartMappingPreset Preset { get; }

        public TeamStartMappingPresetEventArgs(Map map, TeamStartMappingPreset preset)
        {
            Map = map;
            Preset = preset;
        }
    }
}
