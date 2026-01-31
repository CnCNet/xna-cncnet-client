using System;
using System.Linq;

using Microsoft.Xna.Framework;

using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Generic
{
    public class RAResultsWindow : XNAWindow
    {
        private readonly MatchStats matchStats;

        private XNAPanel playerListPanel;

        public RAResultsWindow(WindowManager windowManager, MatchStats matchStats)
            : base(windowManager)
        {
            this.matchStats = matchStats ?? throw new ArgumentNullException(nameof(matchStats));
        }

        public override void Initialize()
        {
            base.Initialize();

            Width = 600;
            Height = 400;
            Text = "Match Results";

            playerListPanel = new XNAPanel(WindowManager)
            {
                X = 10,
                Y = 40,
                Width = Width - 20,
                Height = Height - 50
            };

            AddChild(playerListPanel);

            PopulatePlayerStats();
        }

        private void PopulatePlayerStats()
        {
            // Clear existing UI
            for (int i = playerListPanel.Children.Count - 1; i >= 0; i--)
            {
                playerListPanel.RemoveChild(playerListPanel.Children[i]);
            }

            int y = 10;

            // Headers
            AddHeader("Player", 10, y);
            AddHeader("Side", 170, y);
            AddHeader("Kills", 240, y);
            AddHeader("Credits", 320, y);
            AddHeader("Status", 420, y);

            y += 22;

            foreach (var player in matchStats.Players)
            {
                AddLabel(player.Name, 10, y);
                AddLabel(player.Side, 170, y);
                AddLabel(player.TotalKills.ToString(), 240, y);
                AddLabel(player.Credits.ToString(), 320, y);
                AddLabel(player.GetStatusText(), 420, y);

                y += 20;

                // Stop drawing if panel is full (no scrollbars by design)
                if (y > playerListPanel.Height - 20)
                    break;
            }
        }

        private void AddHeader(string text, int x, int y)
        {
            playerListPanel.AddChild(new XNALabel(WindowManager)
            {
                Text = text,
                X = x,
                Y = y,
                Width = 120,
                Height = 18,
                FontIndex = 1
            });
        }

        private void AddLabel(string text, int x, int y)
        {
            playerListPanel.AddChild(new XNALabel(WindowManager)
            {
                Text = text,
                X = x,
                Y = y,
                Width = 120,
                Height = 18
            });
        }
    }

    // =======================
    // DATA MODELS
    // =======================

    public class MatchStats
    {
        public System.Collections.Generic.List<PlayerStats> Players { get; } = new();
    }

    public class PlayerStats
    {
        public string Name { get; set; }
        public string Side { get; set; }

        public bool Dead { get; set; }
        public bool Resigned { get; set; }
        public bool Quit { get; set; }
        public bool Spectator { get; set; }

        public int Credits { get; set; }

        public System.Collections.Generic.Dictionary<string, int> VehiclesKilled { get; } = new();
        public System.Collections.Generic.Dictionary<string, int> InfantryKilled { get; } = new();
        public System.Collections.Generic.Dictionary<string, int> PlanesKilled { get; } = new();
        public System.Collections.Generic.Dictionary<string, int> BuildingsKilled { get; } = new();

        public int TotalKills =>
            VehiclesKilled.Values.Sum()
          + InfantryKilled.Values.Sum()
          + PlanesKilled.Values.Sum()
          + BuildingsKilled.Values.Sum();

        public string GetStatusText()
        {
            if (Spectator) return "Spectator";
            if (Resigned) return "Resigned";
            if (Quit) return "Quit";
            if (Dead) return "Defeated";
            return "Active";
        }
    }
}