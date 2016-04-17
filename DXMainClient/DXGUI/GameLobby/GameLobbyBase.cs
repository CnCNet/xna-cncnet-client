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

namespace dtasetup.DXGUI.GameLobby
{
    public abstract class GameLobbyBase : DXWindow
    {
        public GameLobbyBase(Game game, string iniName) : base(game)
        {
            _iniSectionName = iniName;
        }

        string _iniSectionName;

        DXPanel PlayerOptionsPanel;

        DXPanel GameOptionsPanel;

        protected List<MultiplayerColor> MPColors = new List<MultiplayerColor>();

        protected List<GameLobbyCheckBox> CheckBoxes = new List<GameLobbyCheckBox>();

        public override void Initialize()
        {
            GameOptionsPanel = new DXPanel(Game);
            GameOptionsPanel.Name = "GameOptionsPanel";
            GameOptionsPanel.BackgroundTexture = AssetLoader.LoadTexture("gamelobbyoptionspanelbg.png");
            GameOptionsPanel.ClientRectangle = new Rectangle(1, 1, 433, 235);

            PlayerOptionsPanel = new DXPanel(Game);
            PlayerOptionsPanel.Name = "PlayerOptionsPanel";
            PlayerOptionsPanel.BackgroundTexture = AssetLoader.LoadTexture("gamelobbypanelbg.png");
            PlayerOptionsPanel.ClientRectangle = new Rectangle(441, 1, 553, 235);

            IniFile gameOptionsIni = new IniFile(ProgramConstants.GetBaseResourcePath() + "GameOptions.ini");

            // Load multiplayer colors
            List<string> colorKeys = gameOptionsIni.GetSectionKeys("MPColors");

            foreach (string key in colorKeys)
            {
                string[] values = gameOptionsIni.GetStringValue("MPColors", key, "255,255,255,0").Split(',');

                try
                {
                    MultiplayerColor mpColor = new MultiplayerColor()
                    {
                        Name = key,
                        R = Math.Min(255, Int32.Parse(values[0])),
                        G = Math.Min(255, Int32.Parse(values[1])),
                        B = Math.Min(255, Int32.Parse(values[2])),
                        GameColorIndex = Int32.Parse(values[3])
                    };

                    MPColors.Add(mpColor);
                }
                catch
                {
                    throw new Exception("Invalid MPColor specified in GameOptions.ini: " + key);
                }
            }

            string[] checkBoxes = gameOptionsIni.GetStringValue(_iniSectionName, "CheckBoxes", String.Empty).Split(',');

            foreach (string chkName in checkBoxes)
            {
                GameLobbyCheckBox chkBox = new GameLobbyCheckBox(Game);
                chkBox.Name = chkName;
                chkBox.GetAttributes(gameOptionsIni);
                GameOptionsPanel.AddChild(chkBox);
            }

            string[] labels = gameOptionsIni.GetStringValue(_iniSectionName, "Labels", String.Empty).Split(',');

            foreach (string labelName in labels)
            {
                DXLabel label = new DXLabel(Game);
                label.Name = labelName;
                label.GetAttributes(gameOptionsIni);
                GameOptionsPanel.AddChild(label);
            }

            string[] dropDowns = gameOptionsIni.GetStringValue(_iniSectionName, "DropDowns", String.Empty).Split(',');



            AddChild(GameOptionsPanel);
            AddChild(PlayerOptionsPanel);

            base.Initialize();
        }
    }
}
