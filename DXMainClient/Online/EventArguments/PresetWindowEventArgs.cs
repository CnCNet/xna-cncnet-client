using System;

namespace DTAClient.Online.EventArguments
{
    public class PresetWindowEventArgs : EventArgs
    {
        public string PresetName { get; }
        
        public bool IsNew { get; set; }

        public PresetWindowEventArgs(string presetName, bool isNew = false)
        {
            PresetName = presetName;
            IsNew = isNew;
        }
    }
}
