using ClientGUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.GameLobby
{
    public class GameLaunchButton : XNAClientButton
    {
        public GameLaunchButton(WindowManager windowManager, Texture2D[] rankTextures) : base(windowManager)
        {
            starDisplay = new StarDisplay(windowManager, rankTextures);
            ClientRectangleUpdated += (e, sender) => UpdateStarPosition();
        }

        private readonly StarDisplay starDisplay;
        private bool UseStarDisplay = true;

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
            if (UseStarDisplay)
            {
                starDisplay.Visible = true;
                starDisplay.Y = (Height - starDisplay.Height) / 2;
                starDisplay.X = (Width / 2) + (int)(Renderer.GetTextDimensions(Text, FontIndex).X / 2) + 3;
            }
            else
            {
                starDisplay.Visible = false;
            }
        }

        public void SetRank(int rank)
        {
            starDisplay.Rank = rank;
            UpdateStarPosition();
        }

        protected override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
        {
            switch (key)
            {
                case "UseStarDisplay":
                    UseStarDisplay = Conversions.BooleanFromString(value, true);
                    return;
            }

            base.ParseAttributeFromINI(iniFile, key, value);
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
