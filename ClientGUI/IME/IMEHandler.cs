#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Xna.Framework;

using Rampastring.XNAUI.Input;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI.IME;
public abstract class IMEHandler : IIMEHandler
{
    protected class XNATextBoxIMEStatus
    {
        private bool _lastActionIMEChatInput = false;
        public bool LastActionIMEChatInput
        {
            get => _lastActionIMEChatInput;
            set
            {
                _lastActionIMEChatInput = value;
                HasEverBeenReceivedIMEChatInput &= value;
            }
        }

        public bool HasEverBeenReceivedIMEChatInput { get; private set; } = false;
        public Action<char>? HandleChatInput = null;
    }

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

    private void OnCompositionChanged(string oldValue, string newValue)
    {
        Debug.WriteLine($"OnCompositionChanged: {newValue.Length - oldValue.Length}");

        if (IMEFocus != null)
        {
            XNATextBoxIMEStatus status = GetOrNewXNATextBoxIMEStatus(IMEFocus);
            // It seems that OnIMETextInput() is always triggered after OnCompositionChanged(). We expect such a behavior.
            status.LastActionIMEChatInput = false;
        }
    }

    protected Dictionary<XNATextBox, XNATextBoxIMEStatus> IMEStatus = [];

    protected XNATextBoxIMEStatus GetOrNewXNATextBoxIMEStatus(XNATextBox textBox)
    {
        if (textBox == null)
            throw new ArgumentNullException(nameof(textBox));

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

    protected virtual void OnIMETextInput(char character)
    {
        Debug.WriteLine($"OnIMETextInput: {character} {((byte)character)}; IMEFocus is null? {IMEFocus == null}");

        if (IMEFocus != null)
        {
            var status = GetOrNewXNATextBoxIMEStatus(IMEFocus);
            status.LastActionIMEChatInput = true;
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

    public void OnSelectedChanged(XNATextBox sender)
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

    public void RegisterXNATextBox(XNATextBox sender, Action<char>? handleCharInput)
    {
        XNATextBoxIMEStatus status = GetOrNewXNATextBoxIMEStatus(sender);
        status.HandleChatInput = handleCharInput;
    }

    public void KillXNATextBox(XNATextBox sender)
    {
        IMEStatus.Remove(sender);
    }

    public bool HandleScrollLeftKey(XNATextBox sender)
    {
        return !CompositionEmpty;
    }

    public bool HandleScrollRightKey(XNATextBox sender)
    {
        return !CompositionEmpty;
    }

    public bool HandleBackspaceKey(XNATextBox sender)
    {
        XNATextBoxIMEStatus status = GetOrNewXNATextBoxIMEStatus(sender);
        bool handled = !status.LastActionIMEChatInput;
        status.LastActionIMEChatInput = false;
        return handled;
    }

    public bool HandleDeleteKey(XNATextBox sender)
    {
        XNATextBoxIMEStatus status = GetOrNewXNATextBoxIMEStatus(sender);
        bool handled = !status.LastActionIMEChatInput;
        status.LastActionIMEChatInput = false;
        return handled;
    }

    public bool GetDrawCompositionText(XNATextBox sender, out string composition, out int compositionCursorPosition)
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

    public bool HandleCharInput(XNATextBox sender, char input)
    {
        return TextCompositionEnabled;
    }

    public bool HandleEnterKey(XNATextBox sender)
    {
        return false;
    }

    public bool HandleEscapeKey(XNATextBox sender)
    {
        XNATextBoxIMEStatus status = GetOrNewXNATextBoxIMEStatus(sender);
        return status.HasEverBeenReceivedIMEChatInput;
    }

    public void OnTextChanged(XNATextBox sender)
    {
    }
}
