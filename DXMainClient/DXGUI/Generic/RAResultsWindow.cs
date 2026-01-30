using System;
using System.Collections.Generic;
using ClientCore;
using ClientCore.Statistics;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System.Linq;


namespace DTAClient.DXGUI.Generic
{
    /// <summary>
    /// Window that displays the results at the end of a Red Alert match.
    /// </summary>
    public class RAResultsWindow : XNAWindow
    {
        private readonly StatsDumpParser stats;

        // UI elements
        private XNAPanel playerListPanel;
        private List<XNALabel> playerLabels = new List<XNALabel>();

        public RAResultsWindow(WindowManager windowManager, StatsDumpParser stats)
            : base(windowManager)
        {
            this.stats = stats ?? throw new ArgumentNullException(nameof(stats));
        }

        public override void Initialize()
        {
            base.Initialize();

            // Window setup
            this.Width = 600;
            this.Height = 400;
            this.Text = "Match Results";

            // Panel to hold player stats
            playerListPanel = new XNAPanel(WindowManager)
            {
                X = 10,
                Y = 40,
                Width = this.Width - 20,
                Height = this.Height - 50,
            };
            AddChild(playerListPanel);

            PopulatePlayerStats();
        }

        /// <summary>
        /// Populates the player list panel with stats from StatsDumpParser
        /// </summary>
        private void PopulatePlayerStats()
        {
            // Remove all children safely
            for (int i = playerListPanel.Children.Count - 1; i >= 0; i--)
            {
                playerListPanel.RemoveChild(playerListPanel.Children[i]);
            }
            playerLabels.Clear();

            int yOffset = 10;

            foreach (var player in stats.Players)
            {
                // Name label
                XNALabel nameLabel = new XNALabel(WindowManager)
                {
                    Text = player.Name,
                    X = 10,
                    Y = yOffset,
                    Width = 150,
                    Height = 20
                };
                playerListPanel.AddChild(nameLabel);
                playerLabels.Add(nameLabel);

                // Kills label
                XNALabel killsLabel = new XNALabel(WindowManager)
                {
                    Text = $"Kills: {player.Kills}",
                    X = 170,
                    Y = yOffset,
                    Width = 80,
                    Height = 20
                };
                playerListPanel.AddChild(killsLabel);
                playerLabels.Add(killsLabel);

                // Deaths label
                XNALabel deathsLabel = new XNALabel(WindowManager)
                {
                    Text = $"Deaths: {player.Deaths}",
                    X = 260,
                    Y = yOffset,
                    Width = 80,
                    Height = 20
                };
                playerListPanel.AddChild(deathsLabel);
                playerLabels.Add(deathsLabel);

                // Score label
                XNALabel scoreLabel = new XNALabel(WindowManager)
                {
                    Text = $"Score: {player.Score}",
                    X = 350,
                    Y = yOffset,
                    Width = 100,
                    Height = 20
                };
                playerListPanel.AddChild(scoreLabel);
                playerLabels.Add(scoreLabel);

                yOffset += 25;
            }
        }

        public override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            base.Update(gameTime);
            // Could add animations here later if needed
        }
    }

    /// <summary>
    /// Represents a parsed match stats dump.
    /// You should already have this class; included here for clarity.
    /// </summary>
    public class StatsDumpParser
    {
        public List<PlayerStats> Players { get; private set; } = new List<PlayerStats>();

        public StatsDumpParser(List<PlayerStats> players)
        {
            Players = players ?? throw new ArgumentNullException(nameof(players));
        }
    }

    public class PlayerStats
    {
        public string Name { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Score { get; set; }

        public PlayerStats(string name, int kills, int deaths, int score)
        {
            Name = name;
            Kills = kills;
            Deaths = deaths;
            Score = score;
        }
    }
}