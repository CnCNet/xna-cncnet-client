#nullable enable
using System;

using ImeSharp;

using Microsoft.Xna.Framework;

using Rampastring.Tools;

namespace ClientGUI.IME;

/// <summary>
/// Integrate IME to XNA framework.
/// </summary>
internal class WinFormsIMEHandler : IMEHandler
{
    public override bool TextCompositionEnabled
    {
        get => InputMethod.Enabled;
        protected set
        {
            if (value != InputMethod.Enabled)
                InputMethod.Enabled = value;
        }
    }

    public WinFormsIMEHandler(Game game)
    {
        Logger.Log($"Initialize WinFormsIMEHandler.");
        if (game?.Window?.Handle == null)
            throw new Exception("The handle of game window should not be null");

        InputMethod.Initialize(game.Window.Handle);
        InputMethod.TextInputCallback = OnIMETextInput;
        InputMethod.TextCompositionCallback = (compositionText, cursorPosition) =>
        {
            Composition = compositionText.ToString();
            CompositionCursorPosition = cursorPosition;
        };
    }

    public override void StartTextComposition()
    {
        //Debug.WriteLine("IME: StartTextComposition");
        TextCompositionEnabled = true;
    }

    public override void StopTextComposition()
    {
        //Debug.WriteLine("IME: StopTextComposition");
        TextCompositionEnabled = false;
    }

    public override void SetTextInputRectangle(Rectangle rect)
        => InputMethod.SetTextInputRect(rect.X, rect.Y, rect.Width, rect.Height);
}
