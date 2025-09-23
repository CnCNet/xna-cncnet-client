namespace DTAConfig.Settings
{
    interface IFileSetting : IUserSetting
    {
        /// <summary>
        /// Determines if the setting availability is checked on runtime.
        /// </summary>
        bool CheckAvailability { get; }

        /// <summary>
        /// Determines if the client would adjust the setting value automatically
        /// if the current value becomes unavailable.
        /// </summary>
        bool ResetUnavailableValue { get; }

        /// <summary>
        /// Refreshes the setting to account for possible
        /// changes that could affect it's functionality.
        /// </summary>
        /// <returns>A bool that determines whether the 
        /// setting's value was changed.</returns>
        bool RefreshSetting();
    }
}
