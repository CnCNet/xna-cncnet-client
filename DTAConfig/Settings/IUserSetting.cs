namespace DTAConfig.Settings
{
    interface IUserSetting
    {

        /// <summary>
        /// INI section name in user settings file this setting's value is stored in.
        /// </summary>
        string SettingSection { get; }

        /// <summary>
        /// INI key name in user settings file this setting's value is stored in.
        /// </summary>
        string SettingKey { get; }

        /// <summary>
        /// Determines if this setting requires the client to be restarted
        /// in order to be correctly applied.
        /// </summary>
        bool RestartRequired { get; }

        /// <summary>
        /// Loads the current value for the user setting.
        /// </summary>
        void Load();

        /// <summary>
        /// Applies operations based on current setting state.
        /// </summary>
        /// <returns>A bool that determines whether the 
        /// client needs to restart for changes to apply.</returns>
        bool Save();
    }
}
