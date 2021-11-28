using System;

namespace DTAClient.Online.EventArguments
{
    public class GameOptionPresetEventArgs : EventArgs
    {
        public string PresetName { get; }

        public GameOptionPresetEventArgs(string presetName)
        {
            PresetName = presetName;
        }
    }
}
