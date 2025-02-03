#nullable enable
using Microsoft.Xna.Framework;

namespace ClientGUI.IME;

/// <summary>
/// Integrate IME to DesktopGL(SDL2) platform.
/// </summary>
/// <remarks>
/// Note: We were unable to provide reliable input method support for
/// SDL2 due to the lack of a way to be able to stabilize hooks for
/// the SDL2 main loop.<br/>
/// Perhaps this requires some changes in Monogame.
/// </remarks>
internal sealed class SdlIMEHandler(Game game) : DummyIMEHandler
{
}