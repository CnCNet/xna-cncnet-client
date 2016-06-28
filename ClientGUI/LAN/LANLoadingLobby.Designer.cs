namespace ClientGUI.LAN
{
    partial class LANLoadingLobby
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LANLoadingLobby));
            this.sbLoadingLobbyChat = new CustomControls.CustomScrollbar();
            this.lblLoadingLobbyChat = new System.Windows.Forms.Label();
            this.tbLoadingLobbyChatInput = new System.Windows.Forms.TextBox();
            this.lblDescription = new System.Windows.Forms.Label();
            this.panelSGPlayers = new System.Windows.Forms.Panel();
            this.lblPlayerEight = new System.Windows.Forms.Label();
            this.lblPlayerSeven = new System.Windows.Forms.Label();
            this.lblPlayerSix = new System.Windows.Forms.Label();
            this.lblPlayerFive = new System.Windows.Forms.Label();
            this.lblPlayerFour = new System.Windows.Forms.Label();
            this.lblPlayerThree = new System.Windows.Forms.Label();
            this.lblPlayerTwo = new System.Windows.Forms.Label();
            this.lblPlayerOne = new System.Windows.Forms.Label();
            this.lblMapName = new System.Windows.Forms.Label();
            this.lblGameMode = new System.Windows.Forms.Label();
            this.lblGameModeValue = new System.Windows.Forms.Label();
            this.lblMapNameValue = new System.Windows.Forms.Label();
            this.btnLeaveLoadingLobby = new ClientGUI.SwitchingImageButton();
            this.lbLoadingLobbyChat = new ClientGUI.ScrollbarlessListBox();
            this.btnLoadMPGame = new ClientGUI.SwitchingImageButton();
            this.panelSGPlayers.SuspendLayout();
            this.SuspendLayout();
            // 
            // sbLoadingLobbyChat
            // 
            this.sbLoadingLobbyChat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.sbLoadingLobbyChat.ChannelColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(166)))), ((int)(((byte)(3)))));
            this.sbLoadingLobbyChat.DownArrowImage = ((System.Drawing.Image)(resources.GetObject("sbLoadingLobbyChat.DownArrowImage")));
            this.sbLoadingLobbyChat.LargeChange = 10;
            this.sbLoadingLobbyChat.Location = new System.Drawing.Point(550, 153);
            this.sbLoadingLobbyChat.Maximum = 100;
            this.sbLoadingLobbyChat.Minimum = 0;
            this.sbLoadingLobbyChat.MinimumSize = new System.Drawing.Size(15, 92);
            this.sbLoadingLobbyChat.Name = "sbLoadingLobbyChat";
            this.sbLoadingLobbyChat.Size = new System.Drawing.Size(16, 224);
            this.sbLoadingLobbyChat.SmallChange = 1;
            this.sbLoadingLobbyChat.TabIndex = 91;
            this.sbLoadingLobbyChat.ThumbBottomImage = ((System.Drawing.Image)(resources.GetObject("sbLoadingLobbyChat.ThumbBottomImage")));
            this.sbLoadingLobbyChat.ThumbBottomSpanImage = ((System.Drawing.Image)(resources.GetObject("sbLoadingLobbyChat.ThumbBottomSpanImage")));
            this.sbLoadingLobbyChat.ThumbMiddleImage = ((System.Drawing.Image)(resources.GetObject("sbLoadingLobbyChat.ThumbMiddleImage")));
            this.sbLoadingLobbyChat.ThumbTopImage = ((System.Drawing.Image)(resources.GetObject("sbLoadingLobbyChat.ThumbTopImage")));
            this.sbLoadingLobbyChat.ThumbTopSpanImage = ((System.Drawing.Image)(resources.GetObject("sbLoadingLobbyChat.ThumbTopSpanImage")));
            this.sbLoadingLobbyChat.UpArrowImage = ((System.Drawing.Image)(resources.GetObject("sbLoadingLobbyChat.UpArrowImage")));
            this.sbLoadingLobbyChat.Value = 0;
            // 
            // lblLoadingLobbyChat
            // 
            this.lblLoadingLobbyChat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblLoadingLobbyChat.AutoSize = true;
            this.lblLoadingLobbyChat.BackColor = System.Drawing.Color.Transparent;
            this.lblLoadingLobbyChat.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblLoadingLobbyChat.Location = new System.Drawing.Point(0, 137);
            this.lblLoadingLobbyChat.Name = "lblLoadingLobbyChat";
            this.lblLoadingLobbyChat.Size = new System.Drawing.Size(35, 13);
            this.lblLoadingLobbyChat.TabIndex = 90;
            this.lblLoadingLobbyChat.Text = "CHAT";
            // 
            // tbLoadingLobbyChatInput
            // 
            this.tbLoadingLobbyChatInput.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.tbLoadingLobbyChatInput.BackColor = System.Drawing.Color.Black;
            this.tbLoadingLobbyChatInput.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tbLoadingLobbyChatInput.ForeColor = System.Drawing.Color.Lime;
            this.tbLoadingLobbyChatInput.Location = new System.Drawing.Point(3, 383);
            this.tbLoadingLobbyChatInput.MaxLength = 300;
            this.tbLoadingLobbyChatInput.Name = "tbLoadingLobbyChatInput";
            this.tbLoadingLobbyChatInput.Size = new System.Drawing.Size(563, 20);
            this.tbLoadingLobbyChatInput.TabIndex = 89;
            this.tbLoadingLobbyChatInput.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbLoadingLobbyChatInput_KeyPress);
            this.tbLoadingLobbyChatInput.KeyUp += new System.Windows.Forms.KeyEventHandler(this.tbLoadingLobbyChatInput_KeyUp);
            // 
            // lblDescription
            // 
            this.lblDescription.AutoSize = true;
            this.lblDescription.BackColor = System.Drawing.Color.Transparent;
            this.lblDescription.Location = new System.Drawing.Point(13, 13);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(543, 13);
            this.lblDescription.TabIndex = 93;
            this.lblDescription.Text = "Wait for all players in the saved game to join in and get ready, then press Load " +
    "Game to load the multiplayer game.";
            // 
            // panelSGPlayers
            // 
            this.panelSGPlayers.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelSGPlayers.Controls.Add(this.lblPlayerEight);
            this.panelSGPlayers.Controls.Add(this.lblPlayerSeven);
            this.panelSGPlayers.Controls.Add(this.lblPlayerSix);
            this.panelSGPlayers.Controls.Add(this.lblPlayerFive);
            this.panelSGPlayers.Controls.Add(this.lblPlayerFour);
            this.panelSGPlayers.Controls.Add(this.lblPlayerThree);
            this.panelSGPlayers.Controls.Add(this.lblPlayerTwo);
            this.panelSGPlayers.Controls.Add(this.lblPlayerOne);
            this.panelSGPlayers.Location = new System.Drawing.Point(3, 30);
            this.panelSGPlayers.Name = "panelSGPlayers";
            this.panelSGPlayers.Size = new System.Drawing.Size(429, 104);
            this.panelSGPlayers.TabIndex = 94;
            // 
            // lblPlayerEight
            // 
            this.lblPlayerEight.AutoSize = true;
            this.lblPlayerEight.BackColor = System.Drawing.Color.Transparent;
            this.lblPlayerEight.Location = new System.Drawing.Point(188, 81);
            this.lblPlayerEight.Name = "lblPlayerEight";
            this.lblPlayerEight.Size = new System.Drawing.Size(45, 13);
            this.lblPlayerEight.TabIndex = 7;
            this.lblPlayerEight.Text = "Player 8";
            this.lblPlayerEight.Visible = false;
            // 
            // lblPlayerSeven
            // 
            this.lblPlayerSeven.AutoSize = true;
            this.lblPlayerSeven.BackColor = System.Drawing.Color.Transparent;
            this.lblPlayerSeven.Location = new System.Drawing.Point(188, 57);
            this.lblPlayerSeven.Name = "lblPlayerSeven";
            this.lblPlayerSeven.Size = new System.Drawing.Size(45, 13);
            this.lblPlayerSeven.TabIndex = 6;
            this.lblPlayerSeven.Text = "Player 7";
            this.lblPlayerSeven.Visible = false;
            // 
            // lblPlayerSix
            // 
            this.lblPlayerSix.AutoSize = true;
            this.lblPlayerSix.BackColor = System.Drawing.Color.Transparent;
            this.lblPlayerSix.Location = new System.Drawing.Point(188, 33);
            this.lblPlayerSix.Name = "lblPlayerSix";
            this.lblPlayerSix.Size = new System.Drawing.Size(45, 13);
            this.lblPlayerSix.TabIndex = 5;
            this.lblPlayerSix.Text = "Player 6";
            this.lblPlayerSix.Visible = false;
            // 
            // lblPlayerFive
            // 
            this.lblPlayerFive.AutoSize = true;
            this.lblPlayerFive.BackColor = System.Drawing.Color.Transparent;
            this.lblPlayerFive.Location = new System.Drawing.Point(188, 9);
            this.lblPlayerFive.Name = "lblPlayerFive";
            this.lblPlayerFive.Size = new System.Drawing.Size(45, 13);
            this.lblPlayerFive.TabIndex = 4;
            this.lblPlayerFive.Text = "Player 5";
            this.lblPlayerFive.Visible = false;
            // 
            // lblPlayerFour
            // 
            this.lblPlayerFour.AutoSize = true;
            this.lblPlayerFour.BackColor = System.Drawing.Color.Transparent;
            this.lblPlayerFour.Location = new System.Drawing.Point(9, 81);
            this.lblPlayerFour.Name = "lblPlayerFour";
            this.lblPlayerFour.Size = new System.Drawing.Size(45, 13);
            this.lblPlayerFour.TabIndex = 3;
            this.lblPlayerFour.Text = "Player 4";
            this.lblPlayerFour.Visible = false;
            // 
            // lblPlayerThree
            // 
            this.lblPlayerThree.AutoSize = true;
            this.lblPlayerThree.BackColor = System.Drawing.Color.Transparent;
            this.lblPlayerThree.Location = new System.Drawing.Point(9, 57);
            this.lblPlayerThree.Name = "lblPlayerThree";
            this.lblPlayerThree.Size = new System.Drawing.Size(45, 13);
            this.lblPlayerThree.TabIndex = 2;
            this.lblPlayerThree.Text = "Player 3";
            this.lblPlayerThree.Visible = false;
            // 
            // lblPlayerTwo
            // 
            this.lblPlayerTwo.AutoSize = true;
            this.lblPlayerTwo.BackColor = System.Drawing.Color.Transparent;
            this.lblPlayerTwo.Location = new System.Drawing.Point(9, 33);
            this.lblPlayerTwo.Name = "lblPlayerTwo";
            this.lblPlayerTwo.Size = new System.Drawing.Size(45, 13);
            this.lblPlayerTwo.TabIndex = 1;
            this.lblPlayerTwo.Text = "Player 2";
            this.lblPlayerTwo.Visible = false;
            // 
            // lblPlayerOne
            // 
            this.lblPlayerOne.AutoSize = true;
            this.lblPlayerOne.BackColor = System.Drawing.Color.Transparent;
            this.lblPlayerOne.Location = new System.Drawing.Point(9, 9);
            this.lblPlayerOne.Name = "lblPlayerOne";
            this.lblPlayerOne.Size = new System.Drawing.Size(45, 13);
            this.lblPlayerOne.TabIndex = 0;
            this.lblPlayerOne.Text = "Player 1";
            this.lblPlayerOne.Visible = false;
            // 
            // lblMapName
            // 
            this.lblMapName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblMapName.AutoSize = true;
            this.lblMapName.BackColor = System.Drawing.Color.Transparent;
            this.lblMapName.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblMapName.Location = new System.Drawing.Point(439, 30);
            this.lblMapName.Name = "lblMapName";
            this.lblMapName.Size = new System.Drawing.Size(33, 13);
            this.lblMapName.TabIndex = 95;
            this.lblMapName.Text = "MAP";
            // 
            // lblGameMode
            // 
            this.lblGameMode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblGameMode.AutoSize = true;
            this.lblGameMode.BackColor = System.Drawing.Color.Transparent;
            this.lblGameMode.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblGameMode.Location = new System.Drawing.Point(439, 68);
            this.lblGameMode.Name = "lblGameMode";
            this.lblGameMode.Size = new System.Drawing.Size(76, 13);
            this.lblGameMode.TabIndex = 96;
            this.lblGameMode.Text = "GAME MODE";
            // 
            // lblGameModeValue
            // 
            this.lblGameModeValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblGameModeValue.AutoSize = true;
            this.lblGameModeValue.BackColor = System.Drawing.Color.Transparent;
            this.lblGameModeValue.Location = new System.Drawing.Point(439, 81);
            this.lblGameModeValue.Name = "lblGameModeValue";
            this.lblGameModeValue.Size = new System.Drawing.Size(68, 13);
            this.lblGameModeValue.TabIndex = 105;
            this.lblGameModeValue.Text = "Game Mode:";
            // 
            // lblMapNameValue
            // 
            this.lblMapNameValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblMapNameValue.AutoSize = true;
            this.lblMapNameValue.BackColor = System.Drawing.Color.Transparent;
            this.lblMapNameValue.Location = new System.Drawing.Point(439, 43);
            this.lblMapNameValue.Name = "lblMapNameValue";
            this.lblMapNameValue.Size = new System.Drawing.Size(31, 13);
            this.lblMapNameValue.TabIndex = 106;
            this.lblMapNameValue.Text = "Map:";
            // 
            // btnLeaveLoadingLobby
            // 
            this.btnLeaveLoadingLobby.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnLeaveLoadingLobby.DefaultImage = null;
            this.btnLeaveLoadingLobby.FlatAppearance.BorderSize = 0;
            this.btnLeaveLoadingLobby.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLeaveLoadingLobby.ForeColor = System.Drawing.Color.LimeGreen;
            this.btnLeaveLoadingLobby.HoveredImage = null;
            this.btnLeaveLoadingLobby.HoverSound = null;
            this.btnLeaveLoadingLobby.Location = new System.Drawing.Point(433, 416);
            this.btnLeaveLoadingLobby.Name = "btnLeaveLoadingLobby";
            this.btnLeaveLoadingLobby.Size = new System.Drawing.Size(133, 23);
            this.btnLeaveLoadingLobby.TabIndex = 92;
            this.btnLeaveLoadingLobby.TabStop = false;
            this.btnLeaveLoadingLobby.Text = "Leave Game";
            this.btnLeaveLoadingLobby.UseVisualStyleBackColor = true;
            this.btnLeaveLoadingLobby.Click += new System.EventHandler(this.btnLeaveLoadingLobby_Click);
            // 
            // lbLoadingLobbyChat
            // 
            this.lbLoadingLobbyChat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lbLoadingLobbyChat.BackColor = System.Drawing.Color.Black;
            this.lbLoadingLobbyChat.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lbLoadingLobbyChat.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.lbLoadingLobbyChat.ForeColor = System.Drawing.Color.Lime;
            this.lbLoadingLobbyChat.FormattingEnabled = true;
            this.lbLoadingLobbyChat.IntegralHeight = false;
            this.lbLoadingLobbyChat.ItemHeight = 16;
            this.lbLoadingLobbyChat.Location = new System.Drawing.Point(3, 153);
            this.lbLoadingLobbyChat.Name = "lbLoadingLobbyChat";
            this.lbLoadingLobbyChat.ShowScrollbar = false;
            this.lbLoadingLobbyChat.Size = new System.Drawing.Size(553, 224);
            this.lbLoadingLobbyChat.TabIndex = 88;
            this.lbLoadingLobbyChat.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.lbLoadingLobbyChat_DrawItem);
            this.lbLoadingLobbyChat.MeasureItem += new System.Windows.Forms.MeasureItemEventHandler(this.lbLoadingLobbyChat_MeasureItem);
            // 
            // btnLoadMPGame
            // 
            this.btnLoadMPGame.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnLoadMPGame.DefaultImage = null;
            this.btnLoadMPGame.FlatAppearance.BorderSize = 0;
            this.btnLoadMPGame.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLoadMPGame.ForeColor = System.Drawing.Color.LimeGreen;
            this.btnLoadMPGame.HoveredImage = null;
            this.btnLoadMPGame.HoverSound = null;
            this.btnLoadMPGame.Location = new System.Drawing.Point(3, 416);
            this.btnLoadMPGame.Name = "btnLoadMPGame";
            this.btnLoadMPGame.Size = new System.Drawing.Size(133, 23);
            this.btnLoadMPGame.TabIndex = 87;
            this.btnLoadMPGame.TabStop = false;
            this.btnLoadMPGame.Text = "Load Game";
            this.btnLoadMPGame.UseVisualStyleBackColor = true;
            this.btnLoadMPGame.Click += new System.EventHandler(this.btnLoadMPGame_Click);
            // 
            // LANLoadingLobby
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(569, 444);
            this.Controls.Add(this.lblMapNameValue);
            this.Controls.Add(this.lblGameModeValue);
            this.Controls.Add(this.lblGameMode);
            this.Controls.Add(this.lblMapName);
            this.Controls.Add(this.panelSGPlayers);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.btnLeaveLoadingLobby);
            this.Controls.Add(this.sbLoadingLobbyChat);
            this.Controls.Add(this.lblLoadingLobbyChat);
            this.Controls.Add(this.tbLoadingLobbyChatInput);
            this.Controls.Add(this.lbLoadingLobbyChat);
            this.Controls.Add(this.btnLoadMPGame);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "LANLoadingLobby";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Load LAN Game";
            this.Load += new System.EventHandler(this.GameLoadingLobby_Load);
            this.panelSGPlayers.ResumeLayout(false);
            this.panelSGPlayers.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private CustomControls.CustomScrollbar sbLoadingLobbyChat;
        private System.Windows.Forms.Label lblLoadingLobbyChat;
        private System.Windows.Forms.TextBox tbLoadingLobbyChatInput;
        private ScrollbarlessListBox lbLoadingLobbyChat;
        private SwitchingImageButton btnLoadMPGame;
        private SwitchingImageButton btnLeaveLoadingLobby;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.Panel panelSGPlayers;
        private System.Windows.Forms.Label lblPlayerEight;
        private System.Windows.Forms.Label lblPlayerSeven;
        private System.Windows.Forms.Label lblPlayerSix;
        private System.Windows.Forms.Label lblPlayerFive;
        private System.Windows.Forms.Label lblPlayerFour;
        private System.Windows.Forms.Label lblPlayerThree;
        private System.Windows.Forms.Label lblPlayerTwo;
        private System.Windows.Forms.Label lblPlayerOne;
        private System.Windows.Forms.Label lblMapName;
        private System.Windows.Forms.Label lblGameMode;
        private System.Windows.Forms.Label lblGameModeValue;
        private System.Windows.Forms.Label lblMapNameValue;
    }
}