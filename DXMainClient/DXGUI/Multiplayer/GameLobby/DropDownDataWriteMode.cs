namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    /// <summary>
    /// An enum for controlling how the game lobbies'
    /// drop-down controls' data should be written into the spawn INI.
    /// </summary>
    public enum DropDownDataWriteMode
    {
        /// <summary>
        /// The 0-based selected index of the drop-down control will
        /// be written into the INI.
        /// </summary>
        INDEX,

        /// <summary>
        /// If index 0 is selected, "false" will be written.
        /// Otherwise the client will write "true".
        /// </summary>
        BOOLEAN,

        /// <summary>
        /// The dropdown value displayed in the UI will
        /// be written into the INI.
        /// </summary>
        STRING,

        /// <summary>
        /// The dropdown value is filename of a mapcode INI file, which will be applied to the map. 
        /// Nothing is written to spawn INI.
        /// </summary>
        MAPCODE
    }
}
