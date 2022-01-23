using System;
using H.Hooks;
using WindowsInput;

namespace ClientGUI
{
    /// <summary>
    /// This class can be used to enable auto clicking in the game.
    /// It works by subscribing to low level mouse events and detecting shift-left-clicks.
    /// When this event occurs, it will automatically send additional clicks (configured).
    /// </summary>
    public static class AutoClickSubscriber
    {
        private static LowLevelMouseHook _mouseHook;
        private static InputSimulator inputSimulator;

        public static void Subscribe()
        {
            try
            {
                if (inputSimulator == null)
                    inputSimulator = new InputSimulator();

                _mouseHook = new LowLevelMouseHook();
                _mouseHook.AddKeyboardKeys = true; // add ctrl/shift/alt modifiers
                _mouseHook.Down += MouseMessageReceived;
                _mouseHook.Start();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public static void Unsubscribe()
        {
            try
            {
                _mouseHook?.Stop();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private static void MouseMessageReceived(object sender, MouseEventArgs mouseEventArgs)
        {
            // only act on shift-left-clicks
            if (!mouseEventArgs.Keys.IsShift || !mouseEventArgs.Keys.IsMouseLeft)
                return;

            // TODO make this count configurable
            DoMouseClick(2);
        }

        private static void DoMouseClick(int count)
        {
            _mouseHook.Down -= MouseMessageReceived;

            for (int i = 0; i < count; i++)
                inputSimulator.Mouse.LeftButtonClick();

            _mouseHook.Down += MouseMessageReceived;
        }
    }
}
