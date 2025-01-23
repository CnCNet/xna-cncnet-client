using System;

using Microsoft.Xna.Framework;

using Rampastring.XNAUI.Input;
using Rampastring.XNAUI.XNAControls;

namespace ClientCore.IME;

public abstract class IMEHandler : IIMEHandler
{
    private string _composition = string.Empty;

    public abstract bool Enabled { get; protected set; }

    public XNAControl IMEFocus { get; set; }

    public string Composition
    {
        get => _composition;
        set
        {
            string old = _composition;
            _composition = value;
            CompositionChanged?.Invoke(this, new(old, value));
        }
    }

    public virtual int CompositionCursorPosition { get; set; }

    public event EventHandler<CharacterEventArgs> CharInput;
    public event EventHandler<CompositionChangedEventArgs> CompositionChanged;
    public static IMEHandler Create(Game game)
    {
#if !GL
        return new WinFormsIMEHandler(game);
#else
        return new SdlIMEHandler(game);
#endif
    }

    public virtual void SetTextInputRectangle(Rectangle rectangle)
    {
    }

    public abstract void StartTextComposition();

    public abstract void StopTextComposition();

    protected virtual void OnTextInput(char character)
        => CharInput?.Invoke(this, new(character));
}
