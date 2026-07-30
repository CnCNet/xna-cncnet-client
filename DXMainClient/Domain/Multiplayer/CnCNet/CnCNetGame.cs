#nullable enable
using System;
using System.Threading;

using Microsoft.Xna.Framework.Graphics;

using Rampastring.XNAUI;

using SixLabors.ImageSharp;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A class for games supported on CnCNet (DTA, TI, TS, RA1/2, etc.)
    /// </summary>
    public abstract class CnCNetGame
    {
        private readonly Lazy<Image?> lazyImage;
        private readonly Lazy<Texture2D?> lazyTexture;

        protected CnCNetGame()
        {
            lazyImage = new Lazy<Image?>(LoadImage, LazyThreadSafetyMode.ExecutionAndPublication);
            lazyTexture = new Lazy<Texture2D?>(LoadTexture, LazyThreadSafetyMode.None);
        }

        /// <summary>
        /// The name of the game that is displayed on the user-interface.
        /// </summary>
        public string? UIName { get; set; }

        /// <summary>
        /// The internal name (suffix) of the game.
        /// </summary>
        public string? InternalName { get; set; }

        /// <summary>
        /// The IRC chat channel ID of the game.
        /// </summary>
        public string? ChatChannel { get; set; }

        /// <summary>
        /// The IRC game broadcasting channel ID of the game.
        /// </summary>
        public string? GameBroadcastChannel { get; set; }

        /// <summary>
        /// The executable name of the game's client.
        /// </summary>
        public string? ClientExecutableName { get; set; }

        /// <summary>
        /// Gets the image for this game's icon. Loaded lazily and is thread-safe.
        /// </summary>
        public Image? Image => lazyImage.Value;

        /// <summary>
        /// Gets the texture for this game's icon. Loaded lazily; must be accessed only from the main (graphics) thread.
        /// </summary>
        public Texture2D? Texture => lazyTexture.Value;

        /// <summary>
        /// The location where to read the game's installation path from the registry.
        /// </summary>
        public string? RegistryInstallPath
        {
            get => field;
            set
            {
                string? hive = value?.Split('\\')[0].Trim();
                if (hive is not "HKLM" and not "HKCU")
                    throw new Exception($"Unexpected registry hive. Expected HKLM or HKCU. Got: {hive}");

                field = value;
            }
        }

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

        /// <summary>
        /// Loads the image for this game's icon. Thread-safe.
        /// </summary>
        protected abstract Image? LoadImage();

        /// <summary>
        /// Loads the texture for this game's icon. Must be called from the main (graphics) thread.
        /// Note: the <see cref="Image"/> instance is kept alive for the lifetime of this object,
        /// since the lazy value cannot be explicitly disposed after texture creation. For the small
        /// icon images used here this is an acceptable trade-off.
        /// </summary>
        protected virtual Texture2D? LoadTexture()
        {
            Image? image = Image;

            if (image == null)
                return null;

            return AssetLoader.TextureFromImage(image);
        }
    }
}
