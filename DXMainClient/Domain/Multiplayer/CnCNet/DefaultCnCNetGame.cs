#nullable enable
using System.Reflection;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// A <see cref="CnCNetGame"/> that loads its icon from an embedded assembly resource.
    /// </summary>
    internal sealed class DefaultCnCNetGame : CnCNetGame
    {
        private static readonly Assembly assembly = Assembly.GetAssembly(typeof(DefaultCnCNetGame))!;

        private readonly string iconResourceName;

        public DefaultCnCNetGame(string iconResourceName)
        {
            this.iconResourceName = iconResourceName;
        }

        protected override SixLabors.ImageSharp.Image? LoadImage()
        {
            using var stream = assembly.GetManifestResourceStream(iconResourceName);

            if (stream == null)
                return null;

            return SixLabors.ImageSharp.Image.Load(stream);
        }
    }
}
