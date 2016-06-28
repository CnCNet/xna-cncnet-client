using ClientCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientGUI.DirectX
{
    public class Cursor : DrawableGameComponent
    {
        public Cursor(Game game) : base(game)
        {
            previousMouseState = Mouse.GetState();
            instance = this;
        }

        static Cursor instance;

        public static Cursor Instance()
        {
            return instance;
        }

        public Point Location { get; set; }
        Point DrawnLocation { get; set; }

        public bool HasMoved { get; set; }

        public Rectangle ExclusiveAccessArea { get; set; }

        public Texture2D[] Textures;

        public CursorImage CursorImage;

        MouseState previousMouseState;
        public bool LeftClicked { get; set; }

        public bool RightClicked { get; set; }

        public bool HasFocus { get; set; }

        public bool Disabled { get; set; }

        public int ScrollWheelValue { get; set; }

        public override void Initialize()
        {
            Textures = new Texture2D[]
            {
                AssetLoader.LoadTexture("cursor.png")
            };
        }

        public override void Update(GameTime gameTime)
        {
            LeftClicked = false;
            RightClicked = false;

            MouseState ms = Mouse.GetState();

            DrawnLocation = new Point((int)((ms.X - WindowManager.Instance.SceneXPosition) / WindowManager.Instance.ScaleRatio),
                (int)(ms.Y / WindowManager.Instance.ScaleRatio));

            if (!HasFocus || Disabled)
            {
                return;
            }

            HasMoved = (DrawnLocation != Location);

            Location = DrawnLocation;

            ScrollWheelValue = (ms.ScrollWheelValue - previousMouseState.ScrollWheelValue) / 120;

            if (ScrollWheelValue != 0)
            {
                ClearExlusiveAccessArea();
            }

            if (ms.LeftButton == ButtonState.Released && previousMouseState.LeftButton == ButtonState.Pressed)
            {
                LeftClicked = true;

                ClearExlusiveAccessArea();
            }

            if (ms.RightButton == ButtonState.Released && previousMouseState.RightButton == ButtonState.Pressed)
            {
                RightClicked = true;

                ClearExlusiveAccessArea();
            }

            previousMouseState = ms;
        }

        void ClearExlusiveAccessArea()
        {
            if (!ExclusiveAccessArea.Contains(Location))
                ExclusiveAccessArea = Rectangle.Empty;
        }

        public override void Draw(GameTime gameTime)
        {
            Texture2D texture = Textures[(int)CursorImage.DEFAULT];

            Renderer.DrawTexture(texture,
                new Rectangle(DrawnLocation, new Point(texture.Width, texture.Height)), Color.White);
        }
    }

    public enum CursorImage
    {
        DEFAULT = 0,
        HAND = 1
    }
}
