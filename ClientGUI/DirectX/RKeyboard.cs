using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientGUI.DirectX
{
    public class RKeyboard : GameComponent
    {
        public RKeyboard(Game game) : base(game)
        {
            PressedKeys = new List<Keys>();
            KeyboardState = Keyboard.GetState();
            instance = this;
        }

        static RKeyboard instance;
        public static RKeyboard Instance()
        {
            return instance;
        }

        public delegate void KeyPressedEventHandler(object sender, KeyPressEventArgs eventArgs);
        public static event KeyPressedEventHandler OnKeyPressed;

        public static KeyboardState KeyboardState;

        Keys[] pressedKeys = new Keys[0];

        public bool HasFocus { get; set; }

        public static List<Keys> PressedKeys;

        public override void Update(GameTime gameTime)
        {
            KeyboardState = Keyboard.GetState();
            PressedKeys.Clear();

            foreach (Keys key in pressedKeys)
            {
                if (KeyboardState.IsKeyUp(key))
                {
                    DoKeyPress(key);
                    PressedKeys.Add(key);
                }
            }

            pressedKeys = KeyboardState.GetPressedKeys();
        }

        void DoKeyPress(Keys key)
        {
            if (OnKeyPressed != null)
                OnKeyPressed(this, new KeyPressEventArgs(key));
        }
    }

    public class KeyPressEventArgs : EventArgs
    {
        public KeyPressEventArgs(Keys key)
        {
            PressedKey = key;
        }

        public Keys PressedKey {get; set;}
    }
}
