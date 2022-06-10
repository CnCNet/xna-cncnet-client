using ClientCore;
using ClientCore.Statistics;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DTAClient.DXGUI.Generic
{
    public class StatisticsWindow : XNAWindow
    {
        public StatisticsWindow(WindowManager windowManager) : base(windowManager) { }

        private XNAPanel panelGameStatistics;
        private XNAPanel panelTotalStatistics;

        private XNAClientDropDown cmbGameModeFilter;
        private XNAClientDropDown cmbGameClassFilter;

        private XNAClientCheckBox chkIncludeSpectatedGames;

        private XNAClientTabControl tabControl;

        // Controls for game statistics

        private XNAMultiColumnListBox lbGameList;
        private XNAMultiColumnListBox lbGameStatistics;

        private Texture2D[] sideTextures;

        // *****************************

        private const int TOTAL_STATS_LOCATION_X1 = 40;
        private const int TOTAL_STATS_VALUE_LOCATION_X1 = 240;
        private const int TOTAL_STATS_LOCATION_X2 = 380;
        private const int TOTAL_STATS_VALUE_LOCATION_X2 = 580;
        private const int TOTAL_STATS_Y_INCREASE = 45;
        private const int TOTAL_STATS_FIRST_ITEM_Y = 20;

        // Controls for total statistics

        private XNALabel lblGamesStartedValue;
        private XNALabel lblGamesFinishedValue;
        private XNALabel lblWinsValue;
        private XNALabel lblLossesValue;
        private XNALabel lblWinLossRatioValue;
        private XNALabel lblAverageGameLengthValue;
        private XNALabel lblTotalTimePlayedValue;
        private XNALabel lblAverageEnemyCountValue;
        private XNALabel lblAverageAllyCountValue;
        private XNALabel lblTotalKillsValue;
        private XNALabel lblKillsPerGameValue;
        private XNALabel lblTotalLossesValue;
        private XNALabel lblLossesPerGameValue;
        private XNALabel lblKillLossRatioValue;
        private XNALabel lblTotalScoreValue;
        private XNALabel lblAverageEconomyValue;
        private XNALabel lblFavouriteSideValue;
        private XNALabel lblAverageAILevelValue;

        // *****************************

        private StatisticsManager sm;
        private List<int> listedGameIndexes = new List<int>();

        private string[] sides;

        private List<MultiplayerColor> mpColors;

        public override void Initialize()
        {
            sm = StatisticsManager.Instance;

            string strLblEconomy = "ECONOMY".L10N("UI:Main:StatisticEconomy");
            string strLblAvgEconomy = "Average economy:".L10N("UI:Main:StatisticEconomyAvg");
            if (ClientConfiguration.Instance.UseBuiltStatistic)
            {
                strLblEconomy = "BUILT".L10N("UI:Main:StatisticBuildCount");
                strLblAvgEconomy = "Avg. number of objects built:".L10N("UI:Main:StatisticBuildCountAvg");
            }

            Name = "StatisticsWindow";
            BackgroundTexture = AssetLoader.LoadTexture("scoreviewerbg.png");
            ClientRectangle = new Rectangle(0, 0, 700, 521);

            tabControl = new XNAClientTabControl(WindowManager);
            tabControl.Name = "tabControl";
            tabControl.ClientRectangle = new Rectangle(12, 10, 0, 0);
            tabControl.ClickSound = new EnhancedSoundEffect("button.wav");
            tabControl.FontIndex = 1;
            tabControl.AddTab("Game Statistics".L10N("UI:Main:GameStatistic"), UIDesignConstants.BUTTON_WIDTH_133);
            tabControl.AddTab("Total Statistics".L10N("UI:Main:TotalStatistic"), UIDesignConstants.BUTTON_WIDTH_133);
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            XNALabel lblFilter = new XNALabel(WindowManager);
            lblFilter.Name = "lblFilter";
            lblFilter.FontIndex = 1;
            lblFilter.Text = "FILTER:".L10N("UI:Main:Filter");
            lblFilter.ClientRectangle = new Rectangle(527, 12, 0, 0);

            cmbGameClassFilter = new XNAClientDropDown(WindowManager);
            cmbGameClassFilter.ClientRectangle = new Rectangle(585, 11, 105, 21);
            cmbGameClassFilter.Name = "cmbGameClassFilter";
            cmbGameClassFilter.AddItem("All games".L10N("UI:Main:FilterAll"));
            cmbGameClassFilter.AddItem("Online games".L10N("UI:Main:FilterOnline"));
            cmbGameClassFilter.AddItem("Online PvP".L10N("UI:Main:FilterPvP"));
            cmbGameClassFilter.AddItem("Online Co-Op".L10N("UI:Main:FilterCoOp"));
            cmbGameClassFilter.AddItem("Skirmish".L10N("UI:Main:FilterSkirmish"));
            cmbGameClassFilter.SelectedIndex = 0;
            cmbGameClassFilter.SelectedIndexChanged += CmbGameClassFilter_SelectedIndexChanged;

            XNALabel lblGameMode = new XNALabel(WindowManager);
            lblGameMode.Name = nameof(lblGameMode);
            lblGameMode.FontIndex = 1;
            lblGameMode.Text = "GAME MODE:".L10N("UI:Main:GameMode");
            lblGameMode.ClientRectangle = new Rectangle(294, 12, 0, 0);

            cmbGameModeFilter = new XNAClientDropDown(WindowManager);
            cmbGameModeFilter.Name = nameof(cmbGameModeFilter);
            cmbGameModeFilter.ClientRectangle = new Rectangle(381, 11, 114, 21);
            cmbGameModeFilter.SelectedIndexChanged += CmbGameModeFilter_SelectedIndexChanged;

            var btnReturnToMenu = new XNAClientButton(WindowManager);
            btnReturnToMenu.Name = nameof(btnReturnToMenu);
            btnReturnToMenu.ClientRectangle = new Rectangle(270, 486, UIDesignConstants.BUTTON_WIDTH_160, UIDesignConstants.BUTTON_HEIGHT);
            btnReturnToMenu.Text = "Return to Main Menu".L10N("UI:Main:ReturnToMainMenu");
            btnReturnToMenu.LeftClick += BtnReturnToMenu_LeftClick;

            var btnClearStatistics = new XNAClientButton(WindowManager);
            btnClearStatistics.Name = nameof(btnClearStatistics);
            btnClearStatistics.ClientRectangle = new Rectangle(12, 486, UIDesignConstants.BUTTON_WIDTH_160, UIDesignConstants.BUTTON_HEIGHT);
            btnClearStatistics.Text = "Clear Statistics".L10N("UI:Main:ClearStatistics");
            btnClearStatistics.LeftClick += BtnClearStatistics_LeftClick;
            btnClearStatistics.Visible = false;

            chkIncludeSpectatedGames = new XNAClientCheckBox(WindowManager);

            AddChild(chkIncludeSpectatedGames);
            chkIncludeSpectatedGames.Name = nameof(chkIncludeSpectatedGames);
            chkIncludeSpectatedGames.Text = "Include spectated games".L10N("UI:Main:IncludeSpectated");
            chkIncludeSpectatedGames.Checked = true;
            chkIncludeSpectatedGames.ClientRectangle = new Rectangle(
                Width - chkIncludeSpectatedGames.Width - 12,
                cmbGameModeFilter.Bottom + 3,
                chkIncludeSpectatedGames.Width, 
                chkIncludeSpectatedGames.Height);
            chkIncludeSpectatedGames.CheckedChanged += ChkIncludeSpectatedGames_CheckedChanged;

            #region Match statistics

            panelGameStatistics = new XNAPanel(WindowManager);
            panelGameStatistics.Name = "panelGameStatistics";
            panelGameStatistics.BackgroundTexture = AssetLoader.LoadTexture("scoreviewerpanelbg.png");
            panelGameStatistics.ClientRectangle = new Rectangle(10, 55, 680, 425);

            AddChild(panelGameStatistics);

            XNALabel lblGames = new XNALabel(WindowManager);
            lblGames.Name = nameof(lblGames);
            lblGames.Text = "GAMES:".L10N("UI:Main:GameMatches");
            lblGames.FontIndex = 1;
            lblGames.ClientRectangle = new Rectangle(4, 2, 0, 0);

            lbGameList = new XNAMultiColumnListBox(WindowManager);
            lbGameList.Name = nameof(lbGameList);
            lbGameList.ClientRectangle = new Rectangle(2, 25, 676, 250);
            lbGameList.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbGameList.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbGameList.AddColumn("DATE / TIME".L10N("UI:Main:GameMatchDateTimeColumnHeader"), 130);
            lbGameList.AddColumn("MAP".L10N("UI:Main:GameMatchMapColumnHeader"), 200);
            lbGameList.AddColumn("GAME MODE".L10N("UI:Main:GameMatchGameModeColumnHeader"), 130);
            lbGameList.AddColumn("FPS".L10N("UI:Main:GameMatchFPSColumnHeader"), 50);
            lbGameList.AddColumn("DURATION".L10N("UI:Main:GameMatchDurationColumnHeader"), 76);
            lbGameList.AddColumn("COMPLETED".L10N("UI:Main:GameMatchCompletedColumnHeader"), 90);
            lbGameList.SelectedIndexChanged += LbGameList_SelectedIndexChanged;
            lbGameList.AllowKeyboardInput = true;

            lbGameStatistics = new XNAMultiColumnListBox(WindowManager);
            lbGameStatistics.Name = nameof(lbGameStatistics);
            lbGameStatistics.ClientRectangle = new Rectangle(2, 280, 676, 143);
            lbGameStatistics.BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 128), 1, 1);
            lbGameStatistics.PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;
            lbGameStatistics.AddColumn("NAME".L10N("UI:Main:StatisticsName"), 130);
            lbGameStatistics.AddColumn("KILLS".L10N("UI:Main:StatisticsKills"), 78);
            lbGameStatistics.AddColumn("LOSSES".L10N("UI:Main:StatisticsLosses"), 78);
            lbGameStatistics.AddColumn(strLblEconomy, 80);
            lbGameStatistics.AddColumn("SCORE".L10N("UI:Main:StatisticsScore"), 100);
            lbGameStatistics.AddColumn("WON".L10N("UI:Main:StatisticsWon"), 50);
            lbGameStatistics.AddColumn("SIDE".L10N("UI:Main:StatisticsSide"), 100);
            lbGameStatistics.AddColumn("TEAM".L10N("UI:Main:StatisticsTeam"), 60);

            panelGameStatistics.AddChild(lblGames);
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

            AddTotalStatisticsLabel("lblGamesStarted", "Games started:".L10N("UI:Main:StatisticsGamesStarted"), new Point(TOTAL_STATS_LOCATION_X1, locationY));

            lblGamesStartedValue = new XNALabel(WindowManager);
            lblGamesStartedValue.Name = "lblGamesStartedValue";
            lblGamesStartedValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X1, locationY, 0, 0);
            lblGamesStartedValue.RemapColor = UISettings.ActiveSettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblGamesFinished", "Games finished:".L10N("UI:Main:StatisticsGamesFinished"), new Point(TOTAL_STATS_LOCATION_X1, locationY));

            lblGamesFinishedValue = new XNALabel(WindowManager);
            lblGamesFinishedValue.Name = "lblGamesFinishedValue";
            lblGamesFinishedValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X1, locationY, 0, 0);
            lblGamesFinishedValue.RemapColor = UISettings.ActiveSettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblWins", "Wins:".L10N("UI:Main:StatisticsGamesWins"), new Point(TOTAL_STATS_LOCATION_X1, locationY));

            lblWinsValue = new XNALabel(WindowManager);
            lblWinsValue.Name = "lblWinsValue";
            lblWinsValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X1, locationY, 0, 0);
            lblWinsValue.RemapColor = UISettings.ActiveSettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblLosses", "Losses:".L10N("UI:Main:StatisticsGamesLosses"), new Point(TOTAL_STATS_LOCATION_X1, locationY));

            lblLossesValue = new XNALabel(WindowManager);
            lblLossesValue.Name = "lblLossesValue";
            lblLossesValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X1, locationY, 0, 0);
            lblLossesValue.RemapColor = UISettings.ActiveSettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblWinLossRatio", "Win / Loss ratio:".L10N("UI:Main:StatisticsGamesWinLossRatio"), new Point(TOTAL_STATS_LOCATION_X1, locationY));

            lblWinLossRatioValue = new XNALabel(WindowManager);
            lblWinLossRatioValue.Name = "lblWinLossRatioValue";
            lblWinLossRatioValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X1, locationY, 0, 0);
            lblWinLossRatioValue.RemapColor = UISettings.ActiveSettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblAverageGameLength", "Average game length:".L10N("UI:Main:StatisticsGamesLengthAvg"), new Point(TOTAL_STATS_LOCATION_X1, locationY));

            lblAverageGameLengthValue = new XNALabel(WindowManager);
            lblAverageGameLengthValue.Name = "lblAverageGameLengthValue";
            lblAverageGameLengthValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X1, locationY, 0, 0);
            lblAverageGameLengthValue.RemapColor = UISettings.ActiveSettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblTotalTimePlayed", "Total time played:".L10N("UI:Main:StatisticsTotalTimePlayed"), new Point(TOTAL_STATS_LOCATION_X1, locationY));

            lblTotalTimePlayedValue = new XNALabel(WindowManager);
            lblTotalTimePlayedValue.Name = "lblTotalTimePlayedValue";
            lblTotalTimePlayedValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X1, locationY, 0, 0);
            lblTotalTimePlayedValue.RemapColor = UISettings.ActiveSettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblAverageEnemyCount", "Average number of enemies:".L10N("UI:Main:StatisticsEnemiesAvg"), new Point(TOTAL_STATS_LOCATION_X1, locationY));

            lblAverageEnemyCountValue = new XNALabel(WindowManager);
            lblAverageEnemyCountValue.Name = "lblAverageEnemyCountValue";
            lblAverageEnemyCountValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X1, locationY, 0, 0);
            lblAverageEnemyCountValue.RemapColor = UISettings.ActiveSettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblAverageAllyCount", "Average number of allies:".L10N("UI:Main:StatisticsAlliesAvg"), new Point(TOTAL_STATS_LOCATION_X1, locationY));

            lblAverageAllyCountValue = new XNALabel(WindowManager);
            lblAverageAllyCountValue.Name = "lblAverageAllyCountValue";
            lblAverageAllyCountValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X1, locationY, 0, 0);
            lblAverageAllyCountValue.RemapColor = UISettings.ActiveSettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            // SECOND COLUMN

            locationY = TOTAL_STATS_FIRST_ITEM_Y;

            AddTotalStatisticsLabel("lblTotalKills", "Total kills:".L10N("UI:Main:StatisticsTotalKills"), new Point(TOTAL_STATS_LOCATION_X2, locationY));

            lblTotalKillsValue = new XNALabel(WindowManager);
            lblTotalKillsValue.Name = "lblTotalKillsValue";
            lblTotalKillsValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X2, locationY, 0, 0);
            lblTotalKillsValue.RemapColor = UISettings.ActiveSettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblKillsPerGame", "Kills / game:".L10N("UI:Main:StatisticsKillsPerGame"), new Point(TOTAL_STATS_LOCATION_X2, locationY));

            lblKillsPerGameValue = new XNALabel(WindowManager);
            lblKillsPerGameValue.Name = "lblKillsPerGameValue";
            lblKillsPerGameValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X2, locationY, 0, 0);
            lblKillsPerGameValue.RemapColor = UISettings.ActiveSettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblTotalLosses", "Total losses:".L10N("UI:Main:StatisticsTotalLosses"), new Point(TOTAL_STATS_LOCATION_X2, locationY));

            lblTotalLossesValue = new XNALabel(WindowManager);
            lblTotalLossesValue.Name = "lblTotalLossesValue";
            lblTotalLossesValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X2, locationY, 0, 0);
            lblTotalLossesValue.RemapColor = UISettings.ActiveSettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblLossesPerGame", "Losses / game:".L10N("UI:Main:StatisticsLossesPerGame"), new Point(TOTAL_STATS_LOCATION_X2, locationY));

            lblLossesPerGameValue = new XNALabel(WindowManager);
            lblLossesPerGameValue.Name = "lblLossesPerGameValue";
            lblLossesPerGameValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X2, locationY, 0, 0);
            lblLossesPerGameValue.RemapColor = UISettings.ActiveSettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblKillLossRatio", "Kill / loss ratio:".L10N("UI:Main:StatisticsKillLossRatio"), new Point(TOTAL_STATS_LOCATION_X2, locationY));

            lblKillLossRatioValue = new XNALabel(WindowManager);
            lblKillLossRatioValue.Name = "lblKillLossRatioValue";
            lblKillLossRatioValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X2, locationY, 0, 0);
            lblKillLossRatioValue.RemapColor = UISettings.ActiveSettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblTotalScore", "Total score:".L10N("UI:Main:TotalScore"), new Point(TOTAL_STATS_LOCATION_X2, locationY));

            lblTotalScoreValue = new XNALabel(WindowManager);
            lblTotalScoreValue.Name = "lblTotalScoreValue";
            lblTotalScoreValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X2, locationY, 0, 0);
            lblTotalScoreValue.RemapColor = UISettings.ActiveSettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblAverageEconomy", strLblAvgEconomy, new Point(TOTAL_STATS_LOCATION_X2, locationY));

            lblAverageEconomyValue = new XNALabel(WindowManager);
            lblAverageEconomyValue.Name = "lblAverageEconomyValue";
            lblAverageEconomyValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X2, locationY, 0, 0);
            lblAverageEconomyValue.RemapColor = UISettings.ActiveSettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblFavouriteSide", "Favourite side:".L10N("UI:Main:FavouriteSide"), new Point(TOTAL_STATS_LOCATION_X2, locationY));

            lblFavouriteSideValue = new XNALabel(WindowManager);
            lblFavouriteSideValue.Name = "lblFavouriteSideValue";
            lblFavouriteSideValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X2, locationY, 0, 0);
            lblFavouriteSideValue.RemapColor = UISettings.ActiveSettings.AltColor;
            locationY += TOTAL_STATS_Y_INCREASE;

            AddTotalStatisticsLabel("lblAverageAILevel", "Average AI level:".L10N("UI:Main:AvgAILevel"), new Point(TOTAL_STATS_LOCATION_X2, locationY));

            lblAverageAILevelValue = new XNALabel(WindowManager);
            lblAverageAILevelValue.Name = "lblAverageAILevelValue";
            lblAverageAILevelValue.ClientRectangle = new Rectangle(TOTAL_STATS_VALUE_LOCATION_X2, locationY, 0, 0);
            lblAverageAILevelValue.RemapColor = UISettings.ActiveSettings.AltColor;
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
            AddChild(btnClearStatistics);

            base.Initialize();

            CenterOnParent();

            sides = ClientConfiguration.Instance.Sides.Split(',');

            sideTextures = new Texture2D[sides.Length + 1];
            for (int i = 0; i < sides.Length; i++)
                sideTextures[i] = AssetLoader.LoadTexture(sides[i] + "icon.png");

            sideTextures[sides.Length] = AssetLoader.LoadTexture("spectatoricon.png");

            mpColors = MultiplayerColor.LoadColors();

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

        private void AddTotalStatisticsLabel(string name, string text, Point location)
        {
            XNALabel label = new XNALabel(WindowManager);
            label.Name = name;
            label.Text = text;
            label.ClientRectangle = new Rectangle(location.X, location.Y, 0, 0);
            panelTotalStatistics.AddChild(label);
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
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

            Color textColor = UISettings.ActiveSettings.AltColor;

            for (int i = 0; i < ms.GetPlayerCount(); i++)
            {
                PlayerStatistics ps = players[i];

                //List<string> items = new List<string>();
                List<XNAListBoxItem> items = new List<XNAListBoxItem>();

                if (ps.Color > -1 && ps.Color < mpColors.Count)
                    textColor = mpColors[ps.Color].XnaColor;

                if (ps.IsAI)
                {
                    items.Add(new XNAListBoxItem(ProgramConstants.GetAILevelName(ps.AILevel), textColor));
                }
                else
                    items.Add(new XNAListBoxItem(ps.Name, textColor));

                if (ps.WasSpectator)
                {
                    // Player was a spectator
                    items.Add(new XNAListBoxItem("-", textColor));
                    items.Add(new XNAListBoxItem("-", textColor));
                    items.Add(new XNAListBoxItem("-", textColor));
                    items.Add(new XNAListBoxItem("-", textColor));
                    items.Add(new XNAListBoxItem("-", textColor));
                    XNAListBoxItem spectatorItem = new XNAListBoxItem();
                    spectatorItem.Text = "Spectator".L10N("UI:Main:Spectator");
                    spectatorItem.TextColor = textColor;
                    spectatorItem.Texture = sideTextures[sideTextures.Length - 1];
                    items.Add(spectatorItem);
                    items.Add(new XNAListBoxItem("-", textColor));
                }
                else
                { 
                    if (!ms.SawCompletion)
                    {
                        // The game wasn't completed - we don't know the stats
                        items.Add(new XNAListBoxItem("-", textColor));
                        items.Add(new XNAListBoxItem("-", textColor));
                        items.Add(new XNAListBoxItem("-", textColor));
                        items.Add(new XNAListBoxItem("-", textColor));
                        items.Add(new XNAListBoxItem("-", textColor));
                    }
                    else
                    {
                        // The game was completed and the player was actually playing
                        items.Add(new XNAListBoxItem(ps.Kills.ToString(), textColor));
                        items.Add(new XNAListBoxItem(ps.Losses.ToString(), textColor));
                        items.Add(new XNAListBoxItem(ps.Economy.ToString(), textColor));
                        items.Add(new XNAListBoxItem(ps.Score.ToString(), textColor));
                        items.Add(new XNAListBoxItem(
                            Conversions.BooleanToString(ps.Won, BooleanStringStyle.YESNO), textColor));
                    }

                    if (ps.Side == 0 || ps.Side > sides.Length)
                        items.Add(new XNAListBoxItem("Unknown".L10N("UI:Main:UnknownSide"), textColor));
                    else
                    {
                        XNAListBoxItem sideItem = new XNAListBoxItem();
                        sideItem.Text = sides[ps.Side - 1];
                        sideItem.TextColor = textColor;
                        sideItem.Texture = sideTextures[ps.Side - 1];
                        items.Add(sideItem);
                    }

                    items.Add(new XNAListBoxItem(TeamIndexToString(ps.Team), textColor));
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

        private string TeamIndexToString(int teamIndex)
        {
            if (teamIndex < 1 || teamIndex >= ProgramConstants.TEAMS.Count)
                return "-";

            return ProgramConstants.TEAMS[teamIndex - 1];
        }

        #region Statistics reading / game listing code

        private void ReadStatistics()
        {
            StatisticsManager sm = StatisticsManager.Instance;

            sm.ReadStatistics(ProgramConstants.GamePath);
        }

        private void ListGameModes()
        {
            int gameCount = sm.GetMatchCount();

            List<string> gameModes = new List<string>();

            cmbGameModeFilter.Items.Clear();

            cmbGameModeFilter.AddItem("All".L10N("UI:Main:All"));

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

        private void ListGames()
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
                info.Add(Renderer.GetSafeString(dateTime, lbGameList.FontIndex));
                info.Add(ms.MapName);
                info.Add(ms.GameMode);
                if (ms.AverageFPS == 0)
                    info.Add("-");
                else
                    info.Add(ms.AverageFPS.ToString());
                info.Add(Renderer.GetSafeString(TimeSpan.FromSeconds(ms.LengthInSeconds).ToString(), lbGameList.FontIndex));
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
                lblAverageAILevelValue.Text = "Easy".L10N("UI:Main:EasyAI");
            else if (numMediumAIs >= numEasyAIs && numMediumAIs >= numHardAIs)
                lblAverageAILevelValue.Text = "Medium".L10N("UI:Main:MediumAI");
            else
                lblAverageAILevelValue.Text = "Hard".L10N("UI:Main:HardAI");
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

        private void ClearAllStatistics()
        {
            StatisticsManager.Instance.ClearDatabase();
            ReadStatistics();
            ListGameModes();
            ListGames();
        }

        #endregion

        private void BtnReturnToMenu_LeftClick(object sender, EventArgs e)
        {
            // To hide the control, just set Enabled=false
            // and MainMenuDarkeningPanel will deal with the rest
            Enabled = false;
        }

        private void BtnClearStatistics_LeftClick(object sender, EventArgs e)
        {
            var msgBox = new XNAMessageBox(WindowManager, "Clear all statistics".L10N("UI:Main:ClearStatisticsTitle"),
                ("All statistics data will be cleared from the database." +
                Environment.NewLine + Environment.NewLine +
                "Are you sure you want to continue?").L10N("UI:Main:ClearStatisticsText"), XNAMessageBoxButtons.YesNo);
            msgBox.Show();
            msgBox.YesClickedAction = ClearStatisticsConfirmation_YesClicked;
        }

        private void ClearStatisticsConfirmation_YesClicked(XNAMessageBox messageBox)
        {
            ClearAllStatistics();
        }
    }
}
