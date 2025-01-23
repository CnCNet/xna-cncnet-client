using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Rampastring.XNAUI.Input;
using Rampastring.XNAUI.XNAControls;

namespace ClientCore.IME;

public abstract class IMEHandler : IIMEHandler
{
    protected class XNATextBoxIMEStatus
    {
        public bool TextCompositionDelay = true;
        public Action<char> HandleChatInput = null;
    }

    public abstract bool Enabled { get; protected set; }

    public XNAControl IMEFocus { get; set; }

    private string _composition = string.Empty;

    public string Composition
    {
        get => _composition;
        set
        {
            string old = _composition;
            _composition = value;
            OnCompositionChanged(old, value);
        }
    }

    public bool CompositionEmpty => string.IsNullOrEmpty(_composition);

    private void OnCompositionChanged(string oldValue, string newValue)
    {
        // TODO: IMEFocus is always XNATextBox (or null)
        if (IMEFocus is XNATextBox textBox)
        {
            XNATextBoxIMEStatus status = GetOrNewXNATextBoxIMEStatus(textBox);

            if (!string.IsNullOrEmpty(oldValue) && string.IsNullOrEmpty(newValue))
                status.TextCompositionDelay = true;
        }
    }

    protected Dictionary<XNATextBox, XNATextBoxIMEStatus> IMEStatus = new Dictionary<XNATextBox, XNATextBoxIMEStatus>();

    protected XNATextBoxIMEStatus GetOrNewXNATextBoxIMEStatus(XNATextBox textBox)
    {
        if (!IMEStatus.ContainsKey(textBox))
            IMEStatus[textBox] = new XNATextBoxIMEStatus();

        return IMEStatus[textBox];
    }

    public virtual int CompositionCursorPosition { get; set; }

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
    {
        // TODO: IMEFocus is always XNATextBox (or null)
        if (IMEFocus is XNATextBox textBox)
        {
            var status = GetOrNewXNATextBoxIMEStatus(textBox);
            status.HandleChatInput?.Invoke(character);
        }
    }

    private void SetIMETextInputRectangle(XNATextBox sender)
    {
        var rect = sender.RenderRectangle();
        rect.X += sender.WindowManager.SceneXPosition;
        rect.Y += sender.WindowManager.SceneYPosition;
        SetTextInputRectangle(rect);
    }

    public void OnXNATextBoxSelectedChanged(XNATextBox sender)
    {
        if (sender.WindowManager.SelectedControl is XNATextBox textBox)
        {
            if (textBox.Enabled && textBox.Visible)
            {
                if (!Enabled)
                {
                    IMEFocus = sender;
                    StartTextComposition();
                    SetIMETextInputRectangle(sender);
                }
                else
                {
                    // do nothing?
                }
            }
            else
            {
                // do nothing?
            }
        }
        else
        {
            StopTextComposition();
        }
    }

    public bool ShouldIMEHandleCharacterInput(XNATextBox sender)
    {
        return Enabled;
    }

    public bool ShouldIMEHandleScrollKey(XNATextBox sender)
    {
        return !CompositionEmpty;
    }

    public bool ShouldIMEHandleBackspaceOrDeleteKey_WithSideEffect(XNATextBox sender)
    {
        XNATextBoxIMEStatus status = GetOrNewXNATextBoxIMEStatus(sender);
        bool prevCompositionDelay = status.TextCompositionDelay;

        // TODO: properly handle TextCompositionDelay
        // Note: ShouldIMEHandleBackspaceOrDeleteKey_WithSideEffect() and OnCompositionChanged() may come in different order. We must properly deal with either case.

        // status.TextCompositionDelay = CompositionEmpty;
        status.TextCompositionDelay = false;

        if (!CompositionEmpty)
            return true;
        else
            return !prevCompositionDelay;
    }

    public void RegisterXNATextBox(XNATextBox sender, Action<char> handleCharInput)
    {
        XNATextBoxIMEStatus status = GetOrNewXNATextBoxIMEStatus(sender);
        status.HandleChatInput = handleCharInput;
    }

    public void KillXNATextBox(XNATextBox sender)
    {
        IMEStatus.Remove(sender);
    }

    public bool ShouldDrawCompositionText(XNATextBox sender, out string composition, out int compositionCursorPosition)
    {
        if (IMEFocus != sender || CompositionEmpty)
        {
            composition = null;
            compositionCursorPosition = 0;
            return false;
        }

        composition = Composition;
        compositionCursorPosition = CompositionCursorPosition;
        return true;
    }
}
