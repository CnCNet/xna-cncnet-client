namespace DTAClient.Online.EventArguments
{
    public class GameOptionPresetEventArgs
    {
        public string PresetName { get; }

        public GameOptionPresetEventArgs(string presetName)
        {
            PresetName = presetName;
        }
    }
}
