using ClientGUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    public class GameLaunchButton : XNAClientButton
    {
        public GameLaunchButton(WindowManager windowManager) : base(windowManager)
        {
        }

        private StarDisplay starDisplay;

        public void InitStarDisplay(Texture2D[] rankTextures)
        {
            if (starDisplay != null)
                throw new InvalidOperationException("The star display is already initialized!");

            starDisplay = new StarDisplay(WindowManager, rankTextures);
            starDisplay.InputEnabled = false;
            AddChild(starDisplay);
            ClientRectangleUpdated += (e, sender) => UpdateStarPosition();
            UpdateStarPosition();
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override string Text
        {
            get => base.Text;
            set { base.Text = value; UpdateStarPosition(); }
        }

        private void UpdateStarPosition()
        {
            if (starDisplay == null)
                return;

            starDisplay.Y = (Height - starDisplay.Height) / 2;
            starDisplay.X = (Width / 2) + (int)(Renderer.GetTextDimensions(Text, FontIndex).X / 2) + 3;
        }

        public void SetRank(int rank)
        {
            starDisplay.Rank = rank;
            UpdateStarPosition();
        }
    }

    class StarDisplay : XNAControl
    {
        public StarDisplay(WindowManager windowManager, Texture2D[] rankTextures) : base(windowManager)
        {
            Name = "StarDisplay";
            this.rankTextures = rankTextures;
            Width = rankTextures[1].Width;
            Height = rankTextures[1].Height;
        }

        private readonly Texture2D[] rankTextures;

        public int Rank { get; set; }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Draw(GameTime gameTime)
        {
            DrawTexture(rankTextures[Rank], Point.Zero, Color.White);
            base.Draw(gameTime);
        }
    }
}
