#nullable enable
using System;
using System.Reflection;

using Microsoft.Xna.Framework.Graphics;

using Rampastring.XNAUI;

using SixLabors.ImageSharp;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A <see cref="CnCNetGame"/> that loads its texture from a custom icon file, or falls back
    /// to the unknown game icon embedded in the assembly.
    /// </summary>
    internal sealed class CustomCnCNetGame : CnCNetGame
    {
        private static readonly Lazy<Image> lazyFallbackImage = new(() =>
            Image.Load(
                Assembly.GetAssembly(typeof(CustomCnCNetGame))!
                .GetManifestResourceStream("DTAClient.Icons.unknownicon.png")));
        private static Image FallbackImage => lazyFallbackImage.Value;

        private readonly string iconFilename;

        public CustomCnCNetGame(string iconFilename)
        {
            this.iconFilename = iconFilename;
        }

        protected override Image? LoadImage() => FallbackImage;

        protected override Texture2D? LoadTexture()
        {
            if (AssetLoader.AssetExists(iconFilename))
                return AssetLoader.LoadTexture(iconFilename);

            return base.LoadTexture();
        }
    }
}
