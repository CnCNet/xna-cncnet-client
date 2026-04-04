#nullable enable
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A <see cref="CnCNetGame"/> that loads its texture from a custom icon file, or falls back
    /// to the unknown game icon embedded in the assembly.
    /// </summary>
    internal sealed class CustomCnCNetGame : CnCNetGame
    {
        private static readonly Assembly assembly = Assembly.GetAssembly(typeof(CustomCnCNetGame))!;

        private readonly string iconFilename;

        public CustomCnCNetGame(string iconFilename)
        {
            this.iconFilename = iconFilename;
        }

        protected override SixLabors.ImageSharp.Image? LoadImage()
        {
            using var stream = assembly.GetManifestResourceStream("DTAClient.Icons.unknownicon.png");

            if (stream == null)
                return null;

            return SixLabors.ImageSharp.Image.Load(stream);
        }

        protected override Texture2D? LoadTexture()
        {
            if (AssetLoader.AssetExists(iconFilename))
                return AssetLoader.LoadTexture(iconFilename);

            return base.LoadTexture();
        }
    }
}
