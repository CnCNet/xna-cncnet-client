using ClientCore;
using DTAClient.Domain.Multiplayer.CnCNet;
using ClientGUI;
using DTAClient.Domain;
using DTAClient.Domain.LAN;
using DTAClient.Domain.Multiplayer;
using DTAClient.Domain.Multiplayer.LAN;
using DTAClient.DXGUI.Multiplayer.GameLobby;
using DTAClient.Online;
using ClientCore.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace DTAClient.DXGUI.Multiplayer.LAN
{
    public class LANLobby : XNAWindow
    {
        private XNAListBox lbGameList;
        private XNALabel lblRA1_1v1;
        private XNALabel lblRA1_2v2;

        private string ra1_1v1_top3 = "??? ??? ???";
        private string ra1_2v2_top3 = "??? ??? ???";

        public LANLobby(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            // Game list
            lbGameList = new XNAListBox(WindowManager);
            lbGameList.Name = "lbGameList";
            lbGameList.ClientRectangle = new Rectangle(12, 40, 400, 200);
            lbGameList.FontIndex = 1;
            AddChild(lbGameList);

            // --- RA1 Ladder Info ---
            lblRA1_1v1 = new XNALabel(WindowManager);
            lblRA1_1v1.Name = "lblRA1_1v1";
            lblRA1_1v1.ClientRectangle = new Rectangle(12, lbGameList.Bottom + 10, 300, 20);
            lblRA1_1v1.FontIndex = 1;
            lblRA1_1v1.Text = "RA1 1v1: " + ra1_1v1_top3;
            AddChild(lblRA1_1v1);

            lblRA1_2v2 = new XNALabel(WindowManager);
            lblRA1_2v2.Name = "lblRA1_2v2";
            lblRA1_2v2.ClientRectangle = new Rectangle(12, lblRA1_1v1.Bottom + 5, 300, 20);
            lblRA1_2v2.FontIndex = 1;
            lblRA1_2v2.Text = "RA1 2v2: " + ra1_2v2_top3;
            AddChild(lblRA1_2v2);

            // --- Fetch ladder data ---
            UpdateLadderRankings();
        }

        private void UpdateLadderRankings()
        {
            try
            {
                string ra1_1v1_data = new WebClient().DownloadString("https://ladder.cncnet.org/ladder/10-2025/ra");
                string ra1_2v2_data = new WebClient().DownloadString("https://ladder.cncnet.org/ladder/10-2025/ra-2v2");

                ra1_1v1_top3 = ExtractTop3(ra1_1v1_data);
                ra1_2v2_top3 = ExtractTop3(ra1_2v2_data);
            }
            catch
            {
                ra1_1v1_top3 = "??? ??? ???";
                ra1_2v2_top3 = "??? ??? ???";
            }

            lblRA1_1v1.Text = "RA1 1v1: " + ra1_1v1_top3;
            lblRA1_2v2.Text = "RA1 2v2: " + ra1_2v2_top3;
        }

        private string ExtractTop3(string html)
        {
            // Quick and simple HTML scrape to find top 3 names
            try
            {
                List<string> names = new List<string>();
                int index = 0;

                while (names.Count < 3)
                {
                    index = html.IndexOf("/players/", index);
                    if (index == -1) break;

                    int start = html.IndexOf(">", index + 1);
                    int end = html.IndexOf("<", start + 1);

                    if (start != -1 && end != -1)
                    {
                        string name = html.Substring(start + 1, end - start - 1).Trim();
                        if (!string.IsNullOrEmpty(name) && !names.Contains(name))
                            names.Add(name);
                    }

                    index = end + 1;
                }

                while (names.Count < 3)
                    names.Add("???");

                return string.Join(" ", names);
            }
            catch
            {
                return "??? ??? ???";
            }
        }

        // -------------------------------------------------------
        // Dummy methods for compile (do nothing for now)
        // -------------------------------------------------------

        private void BtnNewGame_LeftClick(object sender, EventArgs e) { }
        private void BtnJoinGame_LeftClick(object sender, EventArgs e) { }
        private void BtnMainMenu_LeftClick(object sender, EventArgs e) { }
        private void LbGameList_DoubleLeftClick(object sender, EventArgs e) { }
        private void TbChatInput_EnterPressed(object sender, EventArgs e) { }
        private void DdColor_SelectedIndexChanged(object sender, EventArgs e) { }
        private void SetChatColor() { }
        private void GameCreationWindow_NewGame(object sender, EventArgs e) { }
        private void GameCreationWindow_LoadGame(object sender, EventArgs e) { }
    }
}