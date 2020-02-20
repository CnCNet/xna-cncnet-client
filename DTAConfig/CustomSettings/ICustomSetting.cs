namespace DTAConfig.FileSettings
{
    interface ICustomSetting
    {
        void Load();

        /// <summary>
        /// Applies file operations based on current setting state.
        /// Returns a bool that determines whether the 
        /// client needs to restart for changes to apply.
        /// </summary>
        bool Save();
    }
}
