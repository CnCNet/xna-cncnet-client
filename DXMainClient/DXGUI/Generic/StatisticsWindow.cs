using ClientCore;
using ClientCore.Statistics;
using ClientGUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DTAClient.DXGUI.Generic
{
    public class StatisticsWindow : XNAWindow
    {
        public StatisticsWindow(WindowManager windowManager) : base(windowManager) { }

        XNAPanel panelGameStatistics;
        XNAPanel panelTotalStatistics;

        XNADropDown cmbGameModeFilter;
        XNADropDown cmbGameClassFilter;

        XNACheckBox chkIncludeSpectatedGames;

        XNATabControl tabControl;

        // Controls for game statistics

        XNAMultiColumnListBox lbGameList;
        XNAMultiColumnListBox lbGameStatistics;

        Texture2D[] sideTextures;

        // *****************************

        const int TOTAL_STATS_LOCATION_X1 = 40;
        const int TOTAL_STATS_VALUE_LOCATION_X1 = 240;
        const int TOTAL_STATS_LOCATION_X2 = 380;
        const int TOTAL_STATS_VALUE_LOCATION_X2 = 580;
        const int TOTAL_STATS_Y_INCREASE = 45;
        const int TOTAL_STATS_FIRST_ITEM_Y = 20;

        // Controls for total statistics

        XNALabel lblGamesStartedValue;
        XNALabel lblGamesFinishedValue;
        XNALabel lblWinsValue;
        XNALabel lblLossesValue;
        XNALabel lblWinLossRatioValue;
        XNALabel lblAverageGameLengthValue;
        XNALabel lblTotalTimePlayedValue;
        XNALabel lblAverageEnemyCountValue;
        XNALabel lblAverageAllyCountValue;
        XNALabel lblTotalKillsValue;
        XNALabel lblKillsPerGameValue;
        XNALabel lblTotalLossesValue;
        XNALabel lblLossesPerGameValue;
        XNALabel lblKillLossRatioValue;
        XNALabel lblTotalScoreValue;
        XNALabel lblAverageEconomyValue;
        XNALabel lblFavouriteSideValue;
        XNALabel lblAverageAILevelValue;

        // *****************************

        StatisticsManager sm;
        List<int> listedGameIndexes = new List<int>();

        string[] sides;

        public override void Initialize()
        {
            sm = StatisticsManager.Instance;

            Name = "StatisticsWindow";
            BackgroundTexture = AssetLoader.LoadTexture("scoreviewerbg.png");
            ClientRectangle = new Rectangle(0, 0, 700, 521);

            tabControl = new XNATabControl(WindowManager);
            tabControl.Name = "tabControl";
            tabControl.ClientRectangle = new Rectangle(12, 10, 0, 0);
            tabControl.SoundOnClick = AssetLoader.LoadSound("button.wav");
            tabControl.FontIndex = 1;
            tabControl.AddTab("Game Statistics", AssetLoader.LoadTexture("133pxbtn.png"),
                AssetLoader.LoadTexture("133pxbtn_c.png"), true);
            tabControl.AddTab("Total Statistics", AssetLoader.LoadTexture("133pxbtn.png"),
                AssetLoader.LoadTexture("133pxbtn_c.png"), true);
            tabControl.SelectedIndexChanged += tabControl_SelectedIndexChanged;

            XNALabel lblFilter = new XNALabel(WindowManager);
            lblFilter.Name = "lblFilter";
            lblFilter.FontIndex = 1;
            lblFilter.Text = "FILTER:";
            lblFilter.ClientRectangle = new Rectangle(527, 12, 0, 0);

            cmbGameClassFilter = new XNADropDown(WindowManager);
            cmbGameClassFilter.ClientRectangle = new Rectangle(585, 11, 105, 21);
            cmbGameClassFilter.AddItem("All games");
            cmbGameClassFilter.AddItem("Online games");
            cmbGameClassFilter.AddItem("Online PvP");
            cmbGameClassFilter.AddItem("Online Co-Op");
            cmbGameClassFilter.AddItem("Skirmish");
            cmbGameClassFilter.SelectedIndex = 0;
            cmbGameClassFilter.ClickSoundEffect = AssetLoader.LoadSound("dropdown.wav");
            cmbGameClassFilter.SelectedIndexChanged += CmbGameClassFilter_SelectedIndexChanged;

            XNALabel lblGameMode = new XNALabel(WindowManager);
            lblGameMode.Name = "lblGameMode";
            lblGameMode.FontIndex = 1;
            lblGameMode.Text = "GAME MODE:";
            lblGameMode.ClientRectangle = new Rectangle(294, 12, 0, 0);

            cmbGameModeFilter = new XNADropDown(WindowManager);
            cmbGameModeFilter.ClientRectangle = new Rectangle(381, 11, 114, 21);
            cmbGameModeFilter.ClickSoundEffect = AssetLoader.LoadSound("dropdown.wav");
            cmbGameModeFilter.SelectedIndexChanged += CmbGameModeFilter_SelectedIndexChanged;

            XNAButton btnReturnToMenu = new XNAButton(WindowManager);
            btnReturnToMenu.Name = "btnReturnToMenu";
            btnReturnToMenu.IdleTexture = AssetLoader.LoadTexture("160pxbtn.png");
            btnReturnToMenu.HoverTexture = AssetLoader.LoadTexture("160pxbtn_c.png");
            btnReturnToMenu.HoverSoundEffect = AssetLoader.LoadSound("button.wav");
            btnReturnToMenu.ClientRectangle = new Rectangle(270, 486, 160, 23);
            btnReturnToMenu.FontIndex = 1;
            btnReturnToMenu.Text = "Return to Main Menu";
            btnReturnToMenu.LeftClick += BtnReturnToMenu_LeftClick;

            chkIncludeSpectatedGames = new XNACheckBox(WindowManager);

            AddChild(chkIncludeSpectatedGames);
            chkIncludeSpectatedGames.Text = "Include spectated games";
            chkIncludeSpectatedGames.Checked = true;
            chkIncludeSpectatedGames.ClientRectangle = new Rectangle(
                ClientRectangle.Width - chkIncludeSpectatedGames.ClientRectangle.Width - 12,
                cmbGameModeFilter.ClientRectangle.Bottom + 3,
                chkIncludeSpectatedGames.ClientRectangle.Width, 
                chkIncludeSpectatedGames.ClientRectangle.Height);
            chkIncludeSpectatedGames.CheckedChanged += ChkIncludeSpectatedGames_CheckedChanged;

            #region Match statistics

            panelGameStatistics = new XNAPanel(WindowManager);
            panelGameStatistics.Name = "panelGameStatistics";
            panelGameStatistics.BackgroundTexture = AssetLoader.LoadTexture("scoreviewerpanelbg.png");
            panelGameStatistics.ClientRectangle = new Rectangle(10, 55, 680, 425);

            AddChild(panelGameStatistics);

            XNALabel lblMatches = new XNALabel(WindowManager);
            lblMatches.Text = "GAMES:";
            lblMatches.FontIndex = 1;
            lblMatches.ClientRectangle = new Rectangle(4, 2, 0, 0);

            lbGameList = new XNAMultiColumnListBox(WindowManager);
            lbGameList.BackgroundTexture = AssetLoader.CreateTexture(UISettings.BackgroundColor, 100, 100);
            lbGameList.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbGameList.AddColumn("DATE / TIME", 130);
            lbGameList.AddColumn("MAP", 200);
            lbGameList.AddColumn("GAME MODE", 130);
            lbGameList.AddColumn("FPS", 50);
            lbGameList.AddColumn("DURATION", 76);
            lbGameList.AddColumn("COMPLETED", 90);
            lbGameList.ClientRectangle = new Rectangle(2, 25, 676, 250);
            lbGameList.SelectedIndexChanged += LbGameList_SelectedIndexChanged;

            lbGameStatistics = new XNAMultiColumnListBox(WindowManager);
            lbGameStatistics.BackgroundTexture = AssetLoader.CreateTexture(UISettings.BackgroundColor, 1, 1);
            lbGameStatistics.DrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbGameStatistics.AddColumn("NAME", 130);
            lbGameStatistics.AddColumn("KILLS", 78);
            lbGameStatistics.AddColumn("LOSSES", 78);
            lbGameStatistics.AddColumn("ECONOMY", 80);
            lbGameStatistics.AddColumn("SCORE", 100);
            lbGameStatistics.AddColumn("WON", 50);
            lbGameStatistics.AddColumn("SIDE", 100);
            lbGameStatistics.AddColumn("TEAM", 60);
            lbGameStatistics.ClientRectangle = new Rectangle(2, 280, 676, 143);

            panelGameStatistics.AddChild(lblMatches);
            panelGameStatistics.AddChild(lbGameList);
            panelGameStatistics.AddChild(lbGameStatistics);

            #endregion

            #region Total statistics

            panelTotalStatistics = new XNAPanel(WindowManager);
            panelTotalStatistics.Name = "panelTotalStatistics";
            panelTotalStatistics.BackgroundTexture = AssetLoader.LoadTexture("scoreviewerpanelbg.png");
            panelTotalStatistics.ClientRectangle = new Rectangle(10, 55, 680, 425);

            AddChild(panelTotalStatistics);
            panelTotalStatistics.Visible = false;
            panelTotalStatistics.Enabled = false;

            int locationY = TOTAL_STATS_FIRST_ITEM_Y;

            AddTotalStatisticsLabel("lblGamesStarted", "Games started:", new Point(TOTAL_STATS_LOCATION_X1, locationY));

            lblGamesStartedValue = new XNALabel(WindowManager);
            lblGamesStartedValue.Name = "lblGamesStartedValue";
            lblGamesStartedValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X1, locationY, 0, 0);
            lblGamesStartedValue.RemapColor = UISettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblGamesFinished", "Games finished:", new Point(TOTAL_STATS_LOCATION_X1, locationY));

            lblGamesFinishedValue = new XNALabel(WindowManager);
            lblGamesFinishedValue.Name = "lblGamesFinishedValue";
            lblGamesFinishedValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X1, locationY, 0, 0);
            lblGamesFinishedValue.RemapColor = UISettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblWins", "Wins:", new Point(TOTAL_STATS_LOCATION_X1, locationY));

            lblWinsValue = new XNALabel(WindowManager);
            lblWinsValue.Name = "lblWinsValue";
            lblWinsValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X1, locationY, 0, 0);
            lblWinsValue.RemapColor = UISettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblLosses", "Losses:", new Point(TOTAL_STATS_LOCATION_X1, locationY));

            lblLossesValue = new XNALabel(WindowManager);
            lblLossesValue.Name = "lblLossesValue";
            lblLossesValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X1, locationY, 0, 0);
            lblLossesValue.RemapColor = UISettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblWinLossRatio", "Win / Loss ratio:", new Point(TOTAL_STATS_LOCATION_X1, locationY));

            lblWinLossRatioValue = new XNALabel(WindowManager);
            lblWinLossRatioValue.Name = "lblWinLossRatioValue";
            lblWinLossRatioValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X1, locationY, 0, 0);
            lblWinLossRatioValue.RemapColor = UISettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblAverageGameLength", "Average game length:", new Point(TOTAL_STATS_LOCATION_X1, locationY));

            lblAverageGameLengthValue = new XNALabel(WindowManager);
            lblAverageGameLengthValue.Name = "lblAverageGameLengthValue";
            lblAverageGameLengthValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X1, locationY, 0, 0);
            lblAverageGameLengthValue.RemapColor = UISettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblTotalTimePlayed", "Total time played:", new Point(TOTAL_STATS_LOCATION_X1, locationY));

            lblTotalTimePlayedValue = new XNALabel(WindowManager);
            lblTotalTimePlayedValue.Name = "lblTotalTimePlayedValue";
            lblTotalTimePlayedValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X1, locationY, 0, 0);
            lblTotalTimePlayedValue.RemapColor = UISettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblAverageEnemyCount", "Average number of enemies:", new Point(TOTAL_STATS_LOCATION_X1, locationY));

            lblAverageEnemyCountValue = new XNALabel(WindowManager);
            lblAverageEnemyCountValue.Name = "lblAverageEnemyCountValue";
            lblAverageEnemyCountValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X1, locationY, 0, 0);
            lblAverageEnemyCountValue.RemapColor = UISettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblAverageAllyCount", "Average number of allies:", new Point(TOTAL_STATS_LOCATION_X1, locationY));

            lblAverageAllyCountValue = new XNALabel(WindowManager);
            lblAverageAllyCountValue.Name = "lblAverageAllyCountValue";
            lblAverageAllyCountValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X1, locationY, 0, 0);
            lblAverageAllyCountValue.RemapColor = UISettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            // SECOND COLUMN

            locationY = TOTAL_STATS_FIRST_ITEM_Y;

            AddTotalStatisticsLabel("lblTotalKills", "Total kills:", new Point(TOTAL_STATS_LOCATION_X2, locationY));

            lblTotalKillsValue = new XNALabel(WindowManager);
            lblTotalKillsValue.Name = "lblTotalKillsValue";
            lblTotalKillsValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X2, locationY, 0, 0);
            lblTotalKillsValue.RemapColor = UISettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblKillsPerGame", "Kills / game:", new Point(TOTAL_STATS_LOCATION_X2, locationY));

            lblKillsPerGameValue = new XNALabel(WindowManager);
            lblKillsPerGameValue.Name = "lblKillsPerGameValue";
            lblKillsPerGameValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X2, locationY, 0, 0);
            lblKillsPerGameValue.RemapColor = UISettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblTotalLosses", "Total losses:", new Point(TOTAL_STATS_LOCATION_X2, locationY));

            lblTotalLossesValue = new XNALabel(WindowManager);
            lblTotalLossesValue.Name = "lblTotalLossesValue";
            lblTotalLossesValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X2, locationY, 0, 0);
            lblTotalLossesValue.RemapColor = UISettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblLossesPerGame", "Losses / game:", new Point(TOTAL_STATS_LOCATION_X2, locationY));

            lblLossesPerGameValue = new XNALabel(WindowManager);
            lblLossesPerGameValue.Name = "lblLossesPerGameValue";
            lblLossesPerGameValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X2, locationY, 0, 0);
            lblLossesPerGameValue.RemapColor = UISettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblKillLossRatio", "Kill / loss ratio:", new Point(TOTAL_STATS_LOCATION_X2, locationY));

            lblKillLossRatioValue = new XNALabel(WindowManager);
            lblKillLossRatioValue.Name = "lblKillLossRatioValue";
            lblKillLossRatioValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X2, locationY, 0, 0);
            lblKillLossRatioValue.RemapColor = UISettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblTotalScore", "Total score:", new Point(TOTAL_STATS_LOCATION_X2, locationY));

            lblTotalScoreValue = new XNALabel(WindowManager);
            lblTotalScoreValue.Name = "lblTotalScoreValue";
            lblTotalScoreValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X2, locationY, 0, 0);
            lblTotalScoreValue.RemapColor = UISettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblAverageEconomy", "Average economy:", new Point(TOTAL_STATS_LOCATION_X2, locationY));

            lblAverageEconomyValue = new XNALabel(WindowManager);
            lblAverageEconomyValue.Name = "lblAverageEconomyValue";
            lblAverageEconomyValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X2, locationY, 0, 0);
            lblAverageEconomyValue.RemapColor = UISettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblFavouriteSide", "Favourite side:", new Point(TOTAL_STATS_LOCATION_X2, locationY));

            lblFavouriteSideValue = new XNALabel(WindowManager);
            lblFavouriteSideValue.Name = "lblFavouriteSideValue";
            lblFavouriteSideValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X2, locationY, 0, 0);
            lblFavouriteSideValue.RemapColor = UISettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblAverageAILevel", "Average AI level:", new Point(TOTAL_STATS_LOCATION_X2, locationY));

            lblAverageAILevelValue = new XNALabel(WindowManager);
            lblAverageAILevelValue.Name = "lblAverageAILevelValue";
            lblAverageAILevelValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X2, locationY, 0, 0);
            lblAverageAILevelValue.RemapColor = UISettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            panelTotalStatistics.AddChild(lblGamesStartedValue);
            panelTotalStatistics.AddChild(lblGamesFinishedValue);
            panelTotalStatistics.AddChild(lblWinsValue);
            panelTotalStatistics.AddChild(lblLossesValue);
            panelTotalStatistics.AddChild(lblWinLossRatioValue);
            panelTotalStatistics.AddChild(lblAverageGameLengthValue);
            panelTotalStatistics.AddChild(lblTotalTimePlayedValue);
            panelTotalStatistics.AddChild(lblAverageEnemyCountValue);
            panelTotalStatistics.AddChild(lblAverageAllyCountValue);

            panelTotalStatistics.AddChild(lblTotalKillsValue);
            panelTotalStatistics.AddChild(lblKillsPerGameValue);
            panelTotalStatistics.AddChild(lblTotalLossesValue);
            panelTotalStatistics.AddChild(lblLossesPerGameValue);
            panelTotalStatistics.AddChild(lblKillLossRatioValue);
            panelTotalStatistics.AddChild(lblTotalScoreValue);
            panelTotalStatistics.AddChild(lblAverageEconomyValue);
            panelTotalStatistics.AddChild(lblFavouriteSideValue);
            panelTotalStatistics.AddChild(lblAverageAILevelValue);

            #endregion

            AddChild(tabControl);
            AddChild(lblFilter);
            AddChild(cmbGameClassFilter);
            AddChild(lblGameMode);
            AddChild(cmbGameModeFilter);
            AddChild(btnReturnToMenu);

            base.Initialize();

            CenterOnParent();

            sides = DomainController.Instance().GetSides().Split(',');

            sideTextures = new Texture2D[sides.Length + 1];
            for (int i = 0; i < sides.Length; i++)
                sideTextures[i] = AssetLoader.LoadTexture(sides[i] + "icon.png");

            sideTextures[sides.Length] = AssetLoader.LoadTexture("spectatoricon.png");

            ReadStatistics();
            ListGameModes();
            ListGames();

            StatisticsManager.Instance.GameAdded += Instance_GameAdded;
        }

        private void Instance_GameAdded(object sender, EventArgs e)
        {
            ListGames();
        }

        private void ChkIncludeSpectatedGames_CheckedChanged(object sender, EventArgs e)
        {
            ListGames();
        }

        void AddTotalStatisticsLabel(string name, string text, Point location)
        {
            XNALabel label = new XNALabel(WindowManager);
            label.Name = name;
            label.Text = text;
            label.ClientRectangle = new Rectangle(location.X, location.Y, 0, 0);
            panelTotalStatistics.AddChild(label);
        }

        void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == 1)
            {
                panelGameStatistics.Visible = false;
                panelGameStatistics.Enabled = false;
                panelTotalStatistics.Visible = true;
                panelTotalStatistics.Enabled = true;
            }
            else
            {
                panelGameStatistics.Visible = true;
                panelGameStatistics.Enabled = true;
                panelTotalStatistics.Visible = false;
                panelTotalStatistics.Enabled = false;
            }
        }

        private void CmbGameClassFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListGames();
        }

        private void CmbGameModeFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListGames();
        }

        private void LbGameList_SelectedIndexChanged(object sender, EventArgs e)
        {
            lbGameStatistics.ClearItems();

            if (lbGameList.SelectedIndex == -1)
                return;

            MatchStatistics ms = sm.GetMatchByIndex(listedGameIndexes[lbGameList.SelectedIndex]);

            List<PlayerStatistics> players = new List<PlayerStatistics>();

            for (int i = 0; i < ms.GetPlayerCount(); i++)
            {
                players.Add(ms.GetPlayer(i));
            }

            players = players.OrderBy(p => p.Score).Reverse().ToList();

            Color textColor = UISettings.AltColor;

            for (int i = 0; i < ms.GetPlayerCount(); i++)
            {
                PlayerStatistics ps = players[i];

                //List<string> items = new List<string>();
                List<XNAListBoxItem> items = new List<XNAListBoxItem>();

                if (ps.IsAI)
                {
                    items.Add(new XNAListBoxItem(AILevelToString(ps.AILevel)));
                }
                else
                    items.Add(new XNAListBoxItem(ps.Name));

                if (ps.WasSpectator)
                {
                    // Player was a spectator
                    items.Add(new XNAListBoxItem("-"));
                    items.Add(new XNAListBoxItem("-"));
                    items.Add(new XNAListBoxItem("-"));
                    items.Add(new XNAListBoxItem("-"));
                    items.Add(new XNAListBoxItem("-"));
                    XNAListBoxItem spectatorItem = new XNAListBoxItem();
                    spectatorItem.Text = "Spectator";
                    spectatorItem.TextColor = textColor;
                    spectatorItem.Texture = sideTextures[sideTextures.Length - 1];
                    items.Add(spectatorItem);
                    items.Add(new XNAListBoxItem("-"));
                }
                else
                { 
                    if (!ms.SawCompletion)
                    {
                        // The game wasn't completed - we don't know the stats
                        items.Add(new XNAListBoxItem("-"));
                        items.Add(new XNAListBoxItem("-"));
                        items.Add(new XNAListBoxItem("-"));
                        items.Add(new XNAListBoxItem("-"));
                        items.Add(new XNAListBoxItem("-"));
                    }
                    else
                    {
                        // The game was completed and the player was actually playing
                        items.Add(new XNAListBoxItem(ps.Kills.ToString()));
                        items.Add(new XNAListBoxItem(ps.Losses.ToString()));
                        items.Add(new XNAListBoxItem(ps.Economy.ToString()));
                        items.Add(new XNAListBoxItem(ps.Score.ToString()));
                        items.Add(new XNAListBoxItem(Conversions.BooleanToString(ps.Won, BooleanStringStyle.YESNO)));
                    }

                    if (ps.Side == 0 || ps.Side > sides.Length)
                        items.Add(new XNAListBoxItem("Unknown"));
                    else
                    {
                        XNAListBoxItem sideItem = new XNAListBoxItem();
                        sideItem.Text = sides[ps.Side - 1];
                        sideItem.TextColor = textColor;
                        sideItem.Texture = sideTextures[ps.Side - 1];
                        items.Add(sideItem);
                    }

                    items.Add(new XNAListBoxItem(TeamIndexToString(ps.Team)));
                }

                if (!ps.IsLocalPlayer)
                {
                    lbGameStatistics.AddItem(items);

                    items.ForEach(item => item.Selectable = false);
                }
                else
                {
                    lbGameStatistics.AddItem(items);
                    lbGameStatistics.SelectedIndex = i;
                }

            }
        }

        string TeamIndexToString(int teamIndex)
        {
            switch (teamIndex)
            {
                case 1:
                    return "A";
                case 2:
                    return "B";
                case 3:
                    return "C";
                case 4:
                    return "D";
                default:
                    return "-";
            }
        }

        string AILevelToString(int aiLevel)
        {
            switch (aiLevel)
            {
                case 2:
                    return "Hard AI";
                case 1:
                    return "Medium AI";
                case 0:
                default:
                    return "Easy AI";
            }
        }

        #region Statistics reading / game listing code

        void ReadStatistics()
        {
            StatisticsManager sm = StatisticsManager.Instance;

            sm.ReadStatistics(ProgramConstants.GamePath);
        }

        void ListGameModes()
        {
            int gameCount = sm.GetMatchCount();

            List<string> gameModes = new List<string>();

            cmbGameModeFilter.Items.Clear();

            cmbGameModeFilter.AddItem("All");

            for (int i = 0; i < gameCount; i++)
            {
                MatchStatistics ms = sm.GetMatchByIndex(i);
                if (!gameModes.Contains(ms.GameMode))
                    gameModes.Add(ms.GameMode);
            }

            gameModes.Sort();

            foreach (string gm in gameModes)
                cmbGameModeFilter.AddItem(gm);

            cmbGameModeFilter.SelectedIndex = 0;
        }

        void ListGames()
        {
            lbGameList.SelectedIndex = -1;
            lbGameList.SetTopIndex(0);

            lbGameStatistics.ClearItems();
            lbGameList.ClearItems();
            listedGameIndexes.Clear();

            switch (cmbGameClassFilter.SelectedIndex)
            {
                case 0:
                    ListAllGames();
                    break;
                case 1:
                    ListOnlineGames();
                    break;
                case 2:
                    ListPvPGames();
                    break;
                case 3:
                    ListCoOpGames();
                    break;
                case 4:
                    ListSkirmishGames();
                    break;
            }

            listedGameIndexes.Reverse();

            SetTotalStatistics();

            foreach (int gameIndex in listedGameIndexes)
            {
                MatchStatistics ms = sm.GetMatchByIndex(gameIndex);
                string dateTime = ms.DateAndTime.ToShortDateString() + " " + ms.DateAndTime.ToShortTimeString();
                List<string> info = new List<string>();
                info.Add(dateTime);
                info.Add(ms.MapName);
                info.Add(ms.GameMode);
                if (ms.AverageFPS == 0)
                    info.Add("-");
                else
                    info.Add(ms.AverageFPS.ToString());
                info.Add(TimeSpan.FromSeconds(ms.LengthInSeconds).ToString());
                info.Add(Conversions.BooleanToString(ms.SawCompletion, BooleanStringStyle.YESNO));
                lbGameList.AddItem(info, true);
            }
        }

        private void ListAllGames()
        {
            int gameCount = sm.GetMatchCount();

            for (int i = 0; i < gameCount; i++)
            {
                ListGameIndexIfPrerequisitesMet(i);
            }
        }

        private void ListOnlineGames()
        {
            int gameCount = sm.GetMatchCount();

            for (int i = 0; i < gameCount; i++)
            {
                MatchStatistics ms = sm.GetMatchByIndex(i);

                int pCount = ms.GetPlayerCount();
                int hpCount = 0;

                for (int j = 0; j < pCount; j++)
                {
                    PlayerStatistics ps = ms.GetPlayer(j);

                    if (!ps.IsAI)
                    {
                        hpCount++;

                        if (hpCount > 1)
                        {
                            ListGameIndexIfPrerequisitesMet(i);
                            break;
                        }
                    }
                }
            }
        }

        private void ListPvPGames()
        {
            int gameCount = sm.GetMatchCount();

            for (int i = 0; i < gameCount; i++)
            {
                MatchStatistics ms = sm.GetMatchByIndex(i);

                int pCount = ms.GetPlayerCount();
                int pTeam = -1;

                for (int j = 0; j < pCount; j++)
                {
                    PlayerStatistics ps = ms.GetPlayer(j);

                    if (!ps.IsAI && !ps.WasSpectator)
                    {
                        // If we find a single player on a different team than another player,
                        // we'll count the game as a PvP game
                        if (pTeam > -1 && (ps.Team != pTeam || ps.Team == 0))
                        {
                            ListGameIndexIfPrerequisitesMet(i);
                            break;
                        }

                        pTeam = ps.Team;
                    }
                }
            }
        }

        private void ListCoOpGames()
        {
            int gameCount = sm.GetMatchCount();

            for (int i = 0; i < gameCount; i++)
            {
                MatchStatistics ms = sm.GetMatchByIndex(i);

                int pCount = ms.GetPlayerCount();
                int hpCount = 0;
                int pTeam = -1;
                bool add = true;

                for (int j = 0; j < pCount; j++)
                {
                    PlayerStatistics ps = ms.GetPlayer(j);

                    if (!ps.IsAI && !ps.WasSpectator)
                    {
                        hpCount++;

                        if (pTeam > -1 && (ps.Team != pTeam || ps.Team == 0))
                        {
                            add = false;
                            break;
                        }

                        pTeam = ps.Team;
                    }
                }

                if (add && hpCount > 1)
                {
                    ListGameIndexIfPrerequisitesMet(i);
                }
            }
        }

        private void ListSkirmishGames()
        {
            int gameCount = sm.GetMatchCount();

            for (int i = 0; i < gameCount; i++)
            {
                MatchStatistics ms = sm.GetMatchByIndex(i);

                int pCount = ms.GetPlayerCount();
                int hpCount = 0;
                bool add = true;

                foreach (PlayerStatistics ps in ms.Players)
                {
                    if (!ps.IsAI)
                    {
                        hpCount++;

                        if (hpCount > 1)
                        {
                            add = false;
                            break;
                        }
                    }
                }

                if (add)
                {
                    ListGameIndexIfPrerequisitesMet(i);
                }
            }
        }

        private void ListGameIndexIfPrerequisitesMet(int gameIndex)
        {
            MatchStatistics ms = sm.GetMatchByIndex(gameIndex);

            if (cmbGameModeFilter.SelectedIndex != 0)
            {
                string gameMode = cmbGameModeFilter.Items[cmbGameModeFilter.SelectedIndex].Text;

                if (ms.GameMode != gameMode)
                    return;
            }

            PlayerStatistics ps = ms.Players.Find(p => p.IsLocalPlayer);

            if (ps != null && !chkIncludeSpectatedGames.Checked)
            {
                if (ps.WasSpectator)
                    return;
            }

            listedGameIndexes.Add(gameIndex);
        }

        /// <summary>
        /// Adjusts the labels on the "Total statistics" tab.
        /// </summary>
        private void SetTotalStatistics()
        {
            int gamesStarted = 0;
            int gamesFinished = 0;
            int gamesPlayed = 0;
            int wins = 0;
            int gameLosses = 0;
            TimeSpan timePlayed = TimeSpan.Zero;
            int numEnemies = 0;
            int numAllies = 0;
            int totalKills = 0;
            int totalLosses = 0;
            int totalScore = 0;
            int totalEconomy = 0;
            int[] sideGameCounts = new int[sides.Length];
            int numEasyAIs = 0;
            int numMediumAIs = 0;
            int numHardAIs = 0;

            foreach (int gameIndex in listedGameIndexes)
            {
                MatchStatistics ms = sm.GetMatchByIndex(gameIndex);

                gamesStarted++;

                if (ms.SawCompletion)
                    gamesFinished++;

                timePlayed += TimeSpan.FromSeconds(ms.LengthInSeconds);

                PlayerStatistics localPlayer = FindLocalPlayer(ms);

                if (!localPlayer.WasSpectator)
                {
                    totalKills += localPlayer.Kills;
                    totalLosses += localPlayer.Losses;
                    totalScore += localPlayer.Score;
                    totalEconomy += localPlayer.Economy;

                    if (localPlayer.Side > 0 && localPlayer.Side <= sides.Length)
                        sideGameCounts[localPlayer.Side - 1]++;

                    if (!ms.SawCompletion)
                        continue;

                    if (localPlayer.Won)
                        wins++;
                    else
                        gameLosses++;

                    gamesPlayed++;

                    for (int i = 0; i < ms.GetPlayerCount(); i++)
                    {
                        PlayerStatistics ps = ms.GetPlayer(i);

                        if (!ps.WasSpectator && (!ps.IsLocalPlayer || ps.IsAI))
                        {
                            if (ps.Team == 0 || localPlayer.Team != ps.Team)
                                numEnemies++;
                            else
                                numAllies++;

                            if (ps.IsAI)
                            {
                                if (ps.AILevel == 0)
                                    numEasyAIs++;
                                else if (ps.AILevel == 1)
                                    numMediumAIs++;
                                else
                                    numHardAIs++;
                            }
                        }
                    }
                }
            }

            lblGamesStartedValue.Text = gamesStarted.ToString();
            lblGamesFinishedValue.Text = gamesFinished.ToString();
            lblWinsValue.Text = wins.ToString();
            lblLossesValue.Text = gameLosses.ToString();

            if (gameLosses > 0)
            {
                lblWinLossRatioValue.Text = Math.Round(wins / (double)gameLosses, 2).ToString();
            }
            else
                lblWinLossRatioValue.Text = "-";

            if (gamesStarted > 0)
            {
                lblAverageGameLengthValue.Text = TimeSpan.FromSeconds((int)timePlayed.TotalSeconds / gamesStarted).ToString();
            }
            else
                lblAverageGameLengthValue.Text = "-";

            if (gamesPlayed > 0)
            {
                lblAverageEnemyCountValue.Text = Math.Round(numEnemies / (double)gamesPlayed, 2).ToString();
                lblAverageAllyCountValue.Text = Math.Round(numAllies / (double)gamesPlayed, 2).ToString();
                lblKillsPerGameValue.Text = (totalKills / gamesPlayed).ToString();
                lblLossesPerGameValue.Text = (totalLosses / gamesPlayed).ToString();
                lblAverageEconomyValue.Text = (totalEconomy / gamesPlayed).ToString();
            }
            else
            {
                lblAverageEnemyCountValue.Text = "-";
                lblAverageAllyCountValue.Text = "-";
                lblKillsPerGameValue.Text = "-";
                lblLossesPerGameValue.Text = "-";
                lblAverageEconomyValue.Text = "-";
            }

            if (totalLosses > 0)
            {
                lblKillLossRatioValue.Text = Math.Round(totalKills / (double)totalLosses, 2).ToString();
            }
            else
                lblKillLossRatioValue.Text = "-";

            lblTotalTimePlayedValue.Text = timePlayed.ToString();
            lblTotalKillsValue.Text = totalKills.ToString();
            lblTotalLossesValue.Text = totalLosses.ToString();
            lblTotalScoreValue.Text = totalScore.ToString();
            lblFavouriteSideValue.Text = sides[GetHighestIndex(sideGameCounts)];

            if (numEasyAIs >= numMediumAIs && numEasyAIs >= numHardAIs)
                lblAverageAILevelValue.Text = "Easy";
            else if (numMediumAIs >= numEasyAIs && numMediumAIs >= numHardAIs)
                lblAverageAILevelValue.Text = "Medium";
            else
                lblAverageAILevelValue.Text = "Hard";
        }

        private PlayerStatistics FindLocalPlayer(MatchStatistics ms)
        {
            int pCount = ms.GetPlayerCount();

            for (int pId = 0; pId < pCount; pId++)
            {
                PlayerStatistics ps = ms.GetPlayer(pId);

                if (!ps.IsAI && ps.IsLocalPlayer)
                    return ps;
            }

            return null;
        }

        private int GetHighestIndex(int[] t)
        {
            int highestIndex = -1;
            int highest = Int32.MinValue;

            for (int i = 0; i < t.Length; i++)
            {
                if (t[i] > highest)
                {
                    highest = t[i];
                    highestIndex = i;
                }
            }

            return highestIndex;
        }

        #endregion

        private void BtnReturnToMenu_LeftClick(object sender, EventArgs e)
        {
            // To hide the control, just set Enabled=false
            // and MainMenuDarkeningPanel will deal with the rest
            Enabled = false;
        }
    }
}
