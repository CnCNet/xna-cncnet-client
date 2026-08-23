#nullable enable
using System.Reflection;

using SixLabors.ImageSharp;

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

        protected override Image? LoadImage()
        {
            using var stream = assembly.GetManifestResourceStream(iconResourceName);

            if (stream == null)
                return null;

            return Image.Load(stream);
        }
    }
}
