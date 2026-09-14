#nullable enable

using System;

namespace DTAClient.Domain.Multiplayer.CnCNet.Matchmaking
{
    public class MatchmakingSettings
    {
        private static MatchmakingSettings? instance;

        public static MatchmakingSettings Instance => instance ??= new MatchmakingSettings();

        public bool Enabled => MatchmakingConfig.Instance.Enabled;

        public string ApiUrl => MatchmakingConfig.Instance.ApiUrl;

        public string Ladder => MatchmakingConfig.Instance.GetLadderForCurrentMode();

        public bool Casual => MatchmakingConfig.Instance.Casual;

        public int DefaultSide => MatchmakingConfig.Instance.DefaultSide;

        private MatchmakingSettings()
        {
        }

        public void Initialize()
        {
            MatchmakingConfig.Instance.Initialize();
        }
    }
}
