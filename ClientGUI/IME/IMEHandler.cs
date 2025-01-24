#nullable enable
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

using Microsoft.Xna.Framework;

using Rampastring.XNAUI.Input;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI.IME;
public abstract class IMEHandler : IIMEHandler
{
    bool IIMEHandler.TextCompositionEnabled => TextCompositionEnabled;
    public abstract bool TextCompositionEnabled { get; protected set; }

    private XNATextBox? _IMEFocus = null;
    public XNATextBox? IMEFocus
    {
        get => _IMEFocus;
        protected set
        {
            _IMEFocus = value;
            Debug.Assert(!_IMEFocus?.IMEDisabled ?? true, "IME focus should not be assigned from a textbox with IME disabled");
        }
    }

    private string _composition = string.Empty;

    public string Composition
    {
        get => _composition;
        protected set
        {
            string old = _composition;
            _composition = value;
            OnCompositionChanged(old, value);
        }
    }

    public bool CompositionEmpty => string.IsNullOrEmpty(_composition);

    protected bool IMEEventReceived = false;
    protected bool LastActionIMEChatInput = true;

    private void OnCompositionChanged(string oldValue, string newValue)
    {
        Debug.WriteLine($"IME: OnCompositionChanged: {newValue.Length - oldValue.Length}");

        IMEEventReceived = true;
        // It seems that OnIMETextInput() is always triggered after OnCompositionChanged(). We expect such a behavior.
        LastActionIMEChatInput = false;
    }

    protected ConcurrentDictionary<XNATextBox, Action<char>?> TextBoxHandleChatInputCallbacks = [];

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

    protected virtual void OnIMETextInput(char character)
    {
        Debug.WriteLine($"IME: OnIMETextInput: {character} {(short)character}; IMEFocus is null? {IMEFocus == null}");

        IMEEventReceived = true;
        LastActionIMEChatInput = true;

        if (IMEFocus != null)
        {
            TextBoxHandleChatInputCallbacks.TryGetValue(IMEFocus, out var handleChatInput);
            handleChatInput?.Invoke(character);
        }
    }

    private void SetIMETextInputRectangle(XNATextBox sender)
    {
        var rect = sender.RenderRectangle();
        rect.X += sender.WindowManager.SceneXPosition;
        rect.Y += sender.WindowManager.SceneYPosition;

        // Following the input cursor
        // This requires SetIMETextInputRectangle() be called whenever InputPosition is changed.

        //var vec = Renderer.GetTextDimensions(
        //    sender.Text.Substring(sender.TextStartPosition, sender.TextEndPosition - sender.InputPosition),
        //    sender.FontIndex);
        //rect.X += (int)vec.X;

        SetTextInputRectangle(rect);
    }

    void IIMEHandler.OnSelectedChanged(XNATextBox sender)
    {
        if (sender.WindowManager.SelectedControl == sender)
        {
            StopTextComposition();

            if (!sender.IMEDisabled && sender.Enabled && sender.Visible)
            {
                IMEFocus = sender;

                // Update the location of IME based on the textbox
                SetIMETextInputRectangle(sender);

                StartTextComposition();
            }
            else
            {
                IMEFocus = null;
            }
        }
        else if (sender.WindowManager.SelectedControl is not XNATextBox)
        {
            // Disable IME since the current selected control is not XNATextBox
            IMEFocus = null;
            StopTextComposition();
        }

        // Note: if sender.WindowManager.SelectedControl != sender and is XNATextBox,
        // another OnSelectedChanged() will be triggered,
        // so we do not need to handle this case
    }

    void IIMEHandler.RegisterXNATextBox(XNATextBox sender, Action<char>? handleCharInput) =>
        TextBoxHandleChatInputCallbacks[sender] = handleCharInput;

    void IIMEHandler.KillXNATextBox(XNATextBox sender) =>
        TextBoxHandleChatInputCallbacks.TryRemove(sender, out _);

    bool IIMEHandler.HandleScrollLeftKey(XNATextBox sender) =>
        !CompositionEmpty;

    bool IIMEHandler.HandleScrollRightKey(XNATextBox sender) =>
        !CompositionEmpty;

    bool IIMEHandler.HandleBackspaceKey(XNATextBox sender)
    {
        bool handled = !LastActionIMEChatInput;
        LastActionIMEChatInput = true;
        Debug.WriteLine($"IME: HandleBackspaceKey: handled: {handled}");
        return handled;
    }

    bool IIMEHandler.HandleDeleteKey(XNATextBox sender)
    {
        bool handled = !LastActionIMEChatInput;
        LastActionIMEChatInput = true;
        Debug.WriteLine($"IME: HandleDeleteKey: handled: {handled}");
        return handled;
    }

    bool IIMEHandler.GetDrawCompositionText(XNATextBox sender, out string composition, out int compositionCursorPosition)
    {
        if (IMEFocus != sender || CompositionEmpty)
        {
            composition = string.Empty;
            compositionCursorPosition = 0;
            return false;
        }

        composition = Composition;
        compositionCursorPosition = CompositionCursorPosition;
        return true;
    }

    bool IIMEHandler.HandleCharInput(XNATextBox sender, char input) =>
        TextCompositionEnabled;

    bool IIMEHandler.HandleEnterKey(XNATextBox sender)
        => false;

    bool IIMEHandler.HandleEscapeKey(XNATextBox sender)
    {
        Debug.WriteLine($"IME: HandleEscapeKey: handled: {IMEEventReceived}");
        return IMEEventReceived;
    }

    void IIMEHandler.OnTextChanged(XNATextBox sender)
    {
    }
}
