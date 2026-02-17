#nullable enable
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

using Microsoft.Xna.Framework;

using Rampastring.XNAUI;
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

    /// <summary>
    /// Indicates whether an IME event has been received ever. Used to distinguish IME users from non-IME users.
    /// </summary>
    protected bool IMEEventReceived = false;

    protected bool LastActionIMEChatInput = true;

    private void OnCompositionChanged(string oldValue, string newValue)
    {
        //Debug.WriteLine($"IME: OnCompositionChanged: {newValue.Length - oldValue.Length}");

        IMEEventReceived = true;
        // It seems that OnIMETextInput() is always triggered after OnCompositionChanged(). We expect such a behavior.
        LastActionIMEChatInput = false;
    }

    protected ConcurrentDictionary<XNATextBox, Action<char>?> TextBoxHandleChatInputCallbacks = [];

    public virtual int CompositionCursorPosition { get; set; }

    public static IMEHandler Create(Game game)
    {
#if DX
        return new WinFormsIMEHandler(game);
#elif XNA
        // Warning: Think carefully before enabling WinFormsIMEHandler for XNA builds!
        // It *might* occasionally crash due to an unknown stack overflow issue.
        // This *might* be caused by both ImeSharp and XNAUI hooking into WndProc.
        // ImeSharp: https://github.com/ryancheung/ImeSharp/blob/dc2243beff9ef48eb37e398c506c905c965f8e68/ImeSharp/InputMethod.cs#L170
        // XNAUI: https://github.com/Rampastring/Rampastring.XNAUI/blob/9a7d5bb3e47ea50286ee05073d0a6723bc6d764d/Input/KeyboardEventInput.cs#L79
        //
        // That said, you can try returning a WinFormsIMEHandler and test if it is stable enough now. Who knows?
        return new DummyIMEHandler();
#elif GL
        return new SdlIMEHandler(game);
#else
#error Unknown variant
#endif
    }

    public abstract void SetTextInputRectangle(Rectangle rectangle);

    public abstract void StartTextComposition();

    public abstract void StopTextComposition();

    protected virtual void OnIMETextInput(char character)
    {
        //Debug.WriteLine($"IME: OnIMETextInput: {character} {(short)character}; IMEFocus is null? {IMEFocus == null}");

        LastActionIMEChatInput = true;

        if (IMEFocus != null)
        {
            TextBoxHandleChatInputCallbacks.TryGetValue(IMEFocus, out var handleChatInput);
            handleChatInput?.Invoke(character);
        }
    }

    public void SetIMETextInputRectangle(WindowManager manager)
    {
        // When the client window resizes, we should call SetIMETextInputRectangle()
        if (manager.SelectedControl is XNATextBox textBox)
            SetIMETextInputRectangle(textBox);
    }

    private void SetIMETextInputRectangle(XNATextBox sender)
    {
        WindowManager windowManager = sender.WindowManager;

        Rectangle textBoxRect = sender.RenderRectangle();
        double scaleRatio = windowManager.ScaleRatio;

        Rectangle rect = new()
        {
            X = (int)(textBoxRect.X * scaleRatio + windowManager.SceneXPosition),
            Y = (int)(textBoxRect.Y * scaleRatio + windowManager.SceneYPosition),
            Width = (int)(textBoxRect.Width * scaleRatio),
            Height = (int)(textBoxRect.Height * scaleRatio)
        };

        // The following code returns a more accurate location based on the current InputPosition.
        // However, as SetIMETextInputRectangle() does not automatically update with changes in InputPosition
        // (e.g., due to scrolling or mouse clicks altering the textbox's input position without shifting focus),
        // accuracy becomes inconsistent. Sometimes it's precise, other times it's off,
        // which is arguably worse than a consistent but manageable inaccuracy.
        // This inconsistency could lead to a confusing user experience,
        // as the input rectangle's position may not reliably reflect the current input position.
        // Therefore, unless whenever InputPosition is changed, SetIMETextInputRectangle() is raised
        // -- which requires more time to investigate and test, it's commented out for now.
        //var vec = Renderer.GetTextDimensions(
        //    sender.Text.Substring(sender.TextStartPosition, sender.InputPosition),
        //    sender.FontIndex);
        //rect.X += (int)(vec.X * scaleRatio);

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

    void IIMEHandler.RegisterXNATextBox(XNATextBox sender, Action<char>? handleCharInput)
        => TextBoxHandleChatInputCallbacks[sender] = handleCharInput;

    void IIMEHandler.KillXNATextBox(XNATextBox sender)
        => TextBoxHandleChatInputCallbacks.TryRemove(sender, out _);

    bool IIMEHandler.HandleScrollLeftKey(XNATextBox sender)
        => !CompositionEmpty;

    bool IIMEHandler.HandleScrollRightKey(XNATextBox sender)
        => !CompositionEmpty;

    bool IIMEHandler.HandleBackspaceKey(XNATextBox sender)
    {
        bool handled = !LastActionIMEChatInput;
        LastActionIMEChatInput = true;
        //Debug.WriteLine($"IME: HandleBackspaceKey: handled: {handled}");
        return handled;
    }

    bool IIMEHandler.HandleDeleteKey(XNATextBox sender)
    {
        bool handled = !LastActionIMEChatInput;
        LastActionIMEChatInput = true;
        //Debug.WriteLine($"IME: HandleDeleteKey: handled: {handled}");
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

    bool IIMEHandler.HandleCharInput(XNATextBox sender, char input)
        => TextCompositionEnabled;

    bool IIMEHandler.HandleEnterKey(XNATextBox sender)
        => false;

    bool IIMEHandler.HandleEscapeKey(XNATextBox sender)
    {
        //Debug.WriteLine($"IME: HandleEscapeKey: handled: {IMEEventReceived}");

        // This method disables the ESC handling of the TextBox as long as the user has ever used IME.
        // This is because IME users often use ESC to cancel composition. Even if currently the composition is empty,
        // the user still expects ESC to cancel composition rather than deleting the whole sentence.
        // For example, the user might mistakenly hit ESC key twice to cancel composition -- deleting the whole sentence is definitely a heavy punishment for such a small mistake.

        // Note: "!CompositionEmpty => IMEEventReceived" should hold, but just in case

        return IMEEventReceived || !CompositionEmpty;
    }

    void IIMEHandler.OnTextChanged(XNATextBox sender) { }
}
