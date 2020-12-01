namespace DTAConfig.CustomSettings
{
    interface ICustomSetting
    {
        /// <summary>
        /// Determines if this option requires the client to be restarted
        /// in order to be correctly applied.
        /// </summary>
        bool RestartRequired { get; }

        /// <summary>
        /// Determines if the option availability is checked on runtime.
        /// </summary>
        bool CheckAvailability { get; }

        /// <summary>
        /// Determines if the client would adjust the setting value automatically
        /// if the current value becomes unavailable.
        /// </summary>
        bool ResetUnavailableValue { get; }

        /// <summary>
        /// Loads the current value for the custom setting.
        /// </summary>
        void Load();

        /// <summary>
        /// Refreshes the setting to account for possible
        /// changes that could affect it's functionality.
        /// </summary>
        /// <returns>A bool that determines whether the 
        /// setting's value was changed.</returns>
        bool RefreshSetting();

        /// <summary>
        /// Applies operations based on current setting state.
        /// </summary>
        /// <returns>A bool that determines whether the 
        /// client needs to restart for changes to apply.</returns>
        bool Save();
    }
}
