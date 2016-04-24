using ClientGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI.DXControls;
using Rampastring.XNAUI;
using Rampastring.Tools;
using ClientCore;

namespace DTAClient.DXGUI.GameLobby
{
    /// <summary>
    /// A generic base for all game lobbies (Skirmish, LAN and CnCNet).
    /// Contains the common logic for parsing game options and handling player info.
    /// </summary>
    public abstract class GameLobbyBase : DXWindow
    {
        const int PLAYER_COUNT = 8;
        const int PLAYER_OPTION_VERTICAL_MARGIN = 3;
        const int PLAYER_OPTION_HORIZONTAL_MARGIN = 5;
        const int PLAYER_OPTION_CAPTION_Y = 5;
        const int DROP_DOWN_HEIGHT = 21;

        /// <summary>
        /// Creates a new instance of the game lobby base.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="iniName">The name of the lobby in GameOptions.ini.</param>
        public GameLobbyBase(Game game, string iniName) : base(game)
        {
            _iniSectionName = iniName;
        }

        string _iniSectionName;

        DXPanel PlayerOptionsPanel;

        DXPanel GameOptionsPanel;

        protected List<MultiplayerColor> MPColors = new List<MultiplayerColor>();

        protected List<GameLobbyCheckBox> CheckBoxes = new List<GameLobbyCheckBox>();

        protected DXDropDown[] ddPlayerNames;
        protected DXDropDown[] ddPlayerSides;
        protected DXDropDown[] ddPlayerColors;
        protected DXDropDown[] ddPlayerStarts;
        protected DXDropDown[] ddPlayerTeams;

        protected DXLabel lblName;
        protected DXLabel lblSide;
        protected DXLabel lblColor;
        protected DXLabel lblStart;
        protected DXLabel lblTeam;

        protected DXButton btnLeaveGame;
        protected DXLabel lblMapName;
        protected DXLabel lblMapAuthor;

        IniFile _gameOptionsIni;
        protected IniFile GameOptionsIni
        {
            get { return _gameOptionsIni; }
        }

        public override void Initialize()
        {
            ClientRectangle = new Rectangle(0, 0, WindowManager.Instance.RenderResolutionX, WindowManager.Instance.RenderResolutionY);

            btnLeaveGame = new DXButton(Game);
            btnLeaveGame.Name = "btnLeaveGame";
            btnLeaveGame.IdleTexture = AssetLoader.LoadTexture("133pxbtn.png");
            btnLeaveGame.HoverTexture = AssetLoader.LoadTexture("133pxbtn_c.png");
            btnLeaveGame.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnLeaveGame.ClientRectangle = new Rectangle(ClientRectangle.Width - 5, ClientRectangle.Height - 28, 133, 23);
            btnLeaveGame.FontIndex = 1;
            btnLeaveGame.Text = "Leave Game";

            GameOptionsPanel = new DXPanel(Game);
            GameOptionsPanel.Name = "GameOptionsPanel";
            GameOptionsPanel.BackgroundTexture = AssetLoader.LoadTexture("gamelobbyoptionspanelbg.png");
            GameOptionsPanel.ClientRectangle = new Rectangle(1, 1, 433, 235);

            PlayerOptionsPanel = new DXPanel(Game);
            PlayerOptionsPanel.Name = "PlayerOptionsPanel";
            PlayerOptionsPanel.BackgroundTexture = AssetLoader.LoadTexture("gamelobbypanelbg.png");
            PlayerOptionsPanel.ClientRectangle = new Rectangle(441, 1, 553, 235);

            _gameOptionsIni = new IniFile(ProgramConstants.GetBaseResourcePath() + "GameOptions.ini");

            // Load multiplayer colors
            List<string> colorKeys = GameOptionsIni.GetSectionKeys("MPColors");

            foreach (string key in colorKeys)
            {
                string[] values = GameOptionsIni.GetStringValue("MPColors", key, "255,255,255,0").Split(',');

                try
                {
                    MultiplayerColor mpColor = MultiplayerColor.CreateFromStringArray(key, values);

                    MPColors.Add(mpColor);
                }
                catch
                {
                    throw new Exception("Invalid MPColor specified in GameOptions.ini: " + key);
                }
            }

            string[] checkBoxes = GameOptionsIni.GetStringValue(_iniSectionName, "CheckBoxes", String.Empty).Split(',');

            foreach (string chkName in checkBoxes)
            {
                GameLobbyCheckBox chkBox = new GameLobbyCheckBox(Game);
                chkBox.Name = chkName;
                chkBox.GetAttributes(GameOptionsIni);
                GameOptionsPanel.AddChild(chkBox);
            }

            string[] labels = GameOptionsIni.GetStringValue(_iniSectionName, "Labels", String.Empty).Split(',');

            foreach (string labelName in labels)
            {
                DXLabel label = new DXLabel(Game);
                label.Name = labelName;
                label.GetAttributes(GameOptionsIni);
                GameOptionsPanel.AddChild(label);
            }

            string[] dropDowns = GameOptionsIni.GetStringValue(_iniSectionName, "DropDowns", String.Empty).Split(',');

            foreach (string ddName in dropDowns)
            {
                GameLobbyDropDown dropdown = new GameLobbyDropDown(Game);
                dropdown.Name = ddName;
                dropdown.GetAttributes(GameOptionsIni);
                GameOptionsPanel.AddChild(dropdown);
            }

            AddChild(GameOptionsPanel);
            AddChild(PlayerOptionsPanel);

            base.Initialize();
        }

        /// <summary>
        /// Initializes the player option drop-down controls.
        /// </summary>
        /// <param name="playerNameWidth">The width of the "player name" drop-down control.</param>
        /// <param name="sideWidth">The width of the "player side" drop-down control.</param>
        /// <param name="colorWidth">The width of the "player color" drop-down control.</param>
        /// <param name="startWidth">The width of the "player starting location" drop-down control.</param>
        /// <param name="teamWidth">The width of the "player team" drop-down control.</param>
        /// <param name="optionsPosition">The top-left base position of the player option controls.</param>
        protected void InitPlayerOptionDropdowns(int playerNameWidth, int sideWidth,
            int colorWidth, int startWidth, int teamWidth, Point optionsPosition)
        {
            ddPlayerNames = new DXDropDown[PLAYER_COUNT];
            ddPlayerSides = new DXDropDown[PLAYER_COUNT];
            ddPlayerColors = new DXDropDown[PLAYER_COUNT];
            ddPlayerStarts = new DXDropDown[PLAYER_COUNT];
            ddPlayerTeams = new DXDropDown[PLAYER_COUNT];

            string[] sides = GameOptionsIni.GetStringValue("General", "Sides", String.Empty).Split(',');

            for (int i = 0; i < PLAYER_COUNT; i++)
            {
                DXDropDown ddPlayerName = new DXDropDown(Game);
                ddPlayerName.Name = "ddPlayerName" + i;
                ddPlayerName.ClientRectangle = new Rectangle(optionsPosition.X,
                    optionsPosition.Y + (DROP_DOWN_HEIGHT + PLAYER_OPTION_VERTICAL_MARGIN) * i,
                    playerNameWidth, DROP_DOWN_HEIGHT);
                ddPlayerName.AddItem(String.Empty);
                ddPlayerName.AddItem("Easy AI");
                ddPlayerName.AddItem("Medium AI");
                ddPlayerName.AddItem("Hard AI");
                ddPlayerName.Enabled = false;

                DXDropDown ddPlayerSide = new DXDropDown(Game);
                ddPlayerSide.Name = "ddPlayerSide" + i;
                ddPlayerSide.ClientRectangle = new Rectangle(
                    ddPlayerName.ClientRectangle.Right + PLAYER_OPTION_HORIZONTAL_MARGIN,
                    ddPlayerName.ClientRectangle.Y, sideWidth, DROP_DOWN_HEIGHT);
                ddPlayerSide.AddItem("Random", AssetLoader.LoadTexture("randomicon.png"));
                foreach (string sideName in sides)
                    ddPlayerSide.AddItem(sideName, AssetLoader.LoadTexture(sideName + ".png"));
                ddPlayerSide.Enabled = false;

                DXDropDown ddPlayerColor = new DXDropDown(Game);
                ddPlayerColor.Name = "ddPlayerColor" + i;
                ddPlayerColor.ClientRectangle = new Rectangle(
                    ddPlayerSide.ClientRectangle.Right + PLAYER_OPTION_HORIZONTAL_MARGIN,
                    ddPlayerName.ClientRectangle.Y, colorWidth, DROP_DOWN_HEIGHT);
                foreach (MultiplayerColor mpColor in MPColors)
                    ddPlayerColor.AddItem(mpColor.Name, mpColor.XnaColor);
                ddPlayerColor.Enabled = false;

                DXDropDown ddPlayerStart = new DXDropDown(Game);
                ddPlayerStart.Name = "ddPlayerStart" + i;
                ddPlayerStart.ClientRectangle = new Rectangle(
                    ddPlayerColor.ClientRectangle.Right + PLAYER_OPTION_HORIZONTAL_MARGIN,
                    ddPlayerName.ClientRectangle.Y, startWidth, DROP_DOWN_HEIGHT);
                for (int j = 1; j < 9; j++)
                    ddPlayerStart.AddItem(j.ToString());
                ddPlayerStart.Enabled = false;

                DXDropDown ddPlayerTeam = new DXDropDown(Game);
                ddPlayerTeam.Name = "ddPlayerTeam" + i;
                ddPlayerTeam.ClientRectangle = new Rectangle(
                    ddPlayerStart.ClientRectangle.Right + PLAYER_OPTION_HORIZONTAL_MARGIN,
                    ddPlayerName.ClientRectangle.Y, teamWidth, DROP_DOWN_HEIGHT);
                ddPlayerTeam.AddItem("-");
                ddPlayerTeam.AddItem("A");
                ddPlayerTeam.AddItem("B");
                ddPlayerTeam.AddItem("C");
                ddPlayerTeam.AddItem("D");
                ddPlayerTeam.Enabled = false;

                ddPlayerNames[i] = ddPlayerName;
                ddPlayerSides[i] = ddPlayerSide;
                ddPlayerColors[i] = ddPlayerColor;
                ddPlayerStarts[i] = ddPlayerStart;
                ddPlayerTeams[i] = ddPlayerTeam;

                PlayerOptionsPanel.AddChild(ddPlayerName);
                PlayerOptionsPanel.AddChild(ddPlayerSide);
                PlayerOptionsPanel.AddChild(ddPlayerColor);
                PlayerOptionsPanel.AddChild(ddPlayerStart);
                PlayerOptionsPanel.AddChild(ddPlayerTeam);
            }

            lblName = new DXLabel(Game);
            lblName.Name = "lblName";
            lblName.Text = "NAME";
            lblName.ClientRectangle = new Rectangle(ddPlayerNames[0].ClientRectangle.X, PLAYER_OPTION_CAPTION_Y, 0, 0);

            lblSide = new DXLabel(Game);
            lblSide.Name = "lblSide";
            lblSide.Text = "SIDE";
            lblSide.ClientRectangle = new Rectangle(ddPlayerSides[0].ClientRectangle.X, PLAYER_OPTION_CAPTION_Y, 0, 0);

            lblColor = new DXLabel(Game);
            lblColor.Name = "lblColor";
            lblColor.Text = "COLOR";
            lblColor.ClientRectangle = new Rectangle(ddPlayerColors[0].ClientRectangle.X, PLAYER_OPTION_CAPTION_Y, 0, 0);

            lblStart = new DXLabel(Game);
            lblStart.Name = "lblStart";
            lblStart.Text = "START";
            lblStart.ClientRectangle = new Rectangle(ddPlayerStarts[0].ClientRectangle.X, PLAYER_OPTION_CAPTION_Y, 0, 0);

            lblTeam = new DXLabel(Game);
            lblTeam.Name = "lblTeam";
            lblTeam.Text = "TEAM";
            lblTeam.ClientRectangle = new Rectangle(ddPlayerTeams[0].ClientRectangle.X, PLAYER_OPTION_CAPTION_Y, 0, 0);

            PlayerOptionsPanel.AddChild(lblName);
            PlayerOptionsPanel.AddChild(lblSide);
            PlayerOptionsPanel.AddChild(lblColor);
            PlayerOptionsPanel.AddChild(lblStart);
            PlayerOptionsPanel.AddChild(lblTeam);
        }
    }
}
