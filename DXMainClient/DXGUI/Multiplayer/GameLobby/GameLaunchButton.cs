using ClientGUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    public class GameLaunchButton : XNAClientButton
    {
        public GameLaunchButton(WindowManager windowManager, Texture2D[] rankTextures) : base(windowManager)
        {
            starDisplay = new StarDisplay(windowManager, rankTextures);
            starDisplay.InputEnabled = false;
            ClientRectangleUpdated += (e, sender) => UpdateStarPosition();
        }

        private readonly StarDisplay starDisplay;

        public override void Initialize()
        {
            base.Initialize();

            AddChild(starDisplay);
        }

        public override string Text
        {
            get => base.Text;
            set { base.Text = value; UpdateStarPosition(); }
        }

        private void UpdateStarPosition()
        {
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
