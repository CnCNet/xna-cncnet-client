using Microsoft.Xna.Framework.Graphics;

namespace ClientCore.CnCNet5
{
    /// <summary>
    /// A class for games supported on CnCNet (DTA, TI, TS, RA1/2, etc.)
    /// </summary>
    public class CnCNetGame
    {
        /// <summary>
        /// The name of the game that is displayed on the user-interface.
        /// </summary>
        public string UIName { get; set; }

        /// <summary>
        /// The internal name (suffix) of the game.
        /// </summary>
        public string InternalName { get; set; }

        /// <summary>
        /// The IRC chat channel ID of the game.
        /// </summary>
        public string ChatChannel { get; set; }

        /// <summary>
        /// The IRC game broadcasting channel ID of the game.
        /// </summary>
        public string GameBroadcastChannel { get; set; }

        /// <summary>
        /// The executable name of the game's client.
        /// </summary>
        public string ClientExecutableName { get; set; }

        public Texture2D Texture { get; set; }

        /// <summary>
        /// The location where to read the game's installation path from the registry.
        /// </summary>
        public string RegistryInstallPath { get; set; }

        private bool supported = true;

        /// <summary>
        /// Determines if the game is properly supported by this client.
        /// Defaults to true.
        /// </summary>
        public bool Supported
        {
            get { return supported; }
            set { supported = value; }
        }

        /// <summary>
        /// If true, the client should always be connected to this game's chat channel.
        /// </summary>
        public bool AlwaysEnabled { get; set; }
    }
}
