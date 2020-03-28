namespace DTAConfig.CustomSettings
{
    interface ICustomSetting
    {
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
