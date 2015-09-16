namespace dtasetup.gui
{
    partial class CampaignSelector
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
            this.lbCampaignList = new System.Windows.Forms.ListBox();
            this.lblSelectCampaign = new System.Windows.Forms.Label();
            this.lblMissionDescription = new System.Windows.Forms.Label();
            this.lblSelectDifficulty = new System.Windows.Forms.Label();
            this.btnLaunch = new ClientGUI.SwitchingImageButton();
            this.btnCancel = new ClientGUI.SwitchingImageButton();
            this.line2 = new System.Windows.Forms.Label();
            this.tbDifficultyLevel = new System.Windows.Forms.TrackBar();
            this.lblMedium = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lblEasy = new System.Windows.Forms.Label();
            this.lblHard = new System.Windows.Forms.Label();
            this.lblMissDescr = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.tbDifficultyLevel)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lbCampaignList
            // 
            this.lbCampaignList.BackColor = System.Drawing.Color.Black;
            this.lbCampaignList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lbCampaignList.ForeColor = System.Drawing.Color.LimeGreen;
            this.lbCampaignList.FormattingEnabled = true;
            this.lbCampaignList.Location = new System.Drawing.Point(15, 25);
            this.lbCampaignList.Name = "lbCampaignList";
            this.lbCampaignList.Size = new System.Drawing.Size(287, 171);
            this.lbCampaignList.TabIndex = 0;
            this.lbCampaignList.SelectedIndexChanged += new System.EventHandler(this.lbCampaignList_SelectedIndexChanged);
            this.lbCampaignList.MouseDown += new System.Windows.Forms.MouseEventHandler(this.CampaignSelector_MouseDown);
            this.lbCampaignList.MouseMove += new System.Windows.Forms.MouseEventHandler(this.CampaignSelector_MouseMove);
            this.lbCampaignList.MouseUp += new System.Windows.Forms.MouseEventHandler(this.CampaignSelector_MouseUp);
            // 
            // lblSelectCampaign
            // 
            this.lblSelectCampaign.AutoSize = true;
            this.lblSelectCampaign.BackColor = System.Drawing.Color.Transparent;
            this.lblSelectCampaign.Location = new System.Drawing.Point(12, 9);
            this.lblSelectCampaign.Name = "lblSelectCampaign";
            this.lblSelectCampaign.Size = new System.Drawing.Size(50, 13);
            this.lblSelectCampaign.TabIndex = 1;
            this.lblSelectCampaign.Text = "Missions:";
            this.lblSelectCampaign.MouseDown += new System.Windows.Forms.MouseEventHandler(this.CampaignSelector_MouseDown);
            this.lblSelectCampaign.MouseMove += new System.Windows.Forms.MouseEventHandler(this.CampaignSelector_MouseMove);
            this.lblSelectCampaign.MouseUp += new System.Windows.Forms.MouseEventHandler(this.CampaignSelector_MouseUp);
            // 
            // lblMissionDescription
            // 
            this.lblMissionDescription.AutoSize = true;
            this.lblMissionDescription.BackColor = System.Drawing.Color.Transparent;
            this.lblMissionDescription.Location = new System.Drawing.Point(3, 0);
            this.lblMissionDescription.Name = "lblMissionDescription";
            this.lblMissionDescription.Size = new System.Drawing.Size(0, 13);
            this.lblMissionDescription.TabIndex = 2;
            this.lblMissionDescription.MouseDown += new System.Windows.Forms.MouseEventHandler(this.CampaignSelector_MouseDown);
            this.lblMissionDescription.MouseMove += new System.Windows.Forms.MouseEventHandler(this.CampaignSelector_MouseMove);
            this.lblMissionDescription.MouseUp += new System.Windows.Forms.MouseEventHandler(this.CampaignSelector_MouseUp);
            // 
            // lblSelectDifficulty
            // 
            this.lblSelectDifficulty.AutoSize = true;
            this.lblSelectDifficulty.BackColor = System.Drawing.Color.Transparent;
            this.lblSelectDifficulty.Location = new System.Drawing.Point(16, 307);
            this.lblSelectDifficulty.Name = "lblSelectDifficulty";
            this.lblSelectDifficulty.Size = new System.Drawing.Size(79, 13);
            this.lblSelectDifficulty.TabIndex = 4;
            this.lblSelectDifficulty.Text = "Difficulty Level:";
            this.lblSelectDifficulty.MouseDown += new System.Windows.Forms.MouseEventHandler(this.CampaignSelector_MouseDown);
            this.lblSelectDifficulty.MouseMove += new System.Windows.Forms.MouseEventHandler(this.CampaignSelector_MouseMove);
            this.lblSelectDifficulty.MouseUp += new System.Windows.Forms.MouseEventHandler(this.CampaignSelector_MouseUp);
            // 
            // btnLaunch
            // 
            this.btnLaunch.DefaultImage = null;
            this.btnLaunch.FlatAppearance.BorderSize = 0;
            this.btnLaunch.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLaunch.HoveredImage = null;
            this.btnLaunch.HoverSound = null;
            this.btnLaunch.Location = new System.Drawing.Point(18, 395);
            this.btnLaunch.Name = "btnLaunch";
            this.btnLaunch.Size = new System.Drawing.Size(133, 23);
            this.btnLaunch.TabIndex = 5;
            this.btnLaunch.Text = "Launch";
            this.btnLaunch.UseVisualStyleBackColor = true;
            this.btnLaunch.Click += new System.EventHandler(this.btnLaunch_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DefaultImage = null;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.HoveredImage = null;
            this.btnCancel.HoverSound = null;
            this.btnCancel.Location = new System.Drawing.Point(168, 395);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(133, 23);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // line2
            // 
            this.line2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.line2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.line2.Location = new System.Drawing.Point(15, 294);
            this.line2.Name = "line2";
            this.line2.Size = new System.Drawing.Size(287, 1);
            this.line2.TabIndex = 79;
            // 
            // tbDifficultyLevel
            // 
            this.tbDifficultyLevel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(10)))), ((int)(((byte)(11)))), ((int)(((byte)(12)))));
            this.tbDifficultyLevel.LargeChange = 1;
            this.tbDifficultyLevel.Location = new System.Drawing.Point(15, 323);
            this.tbDifficultyLevel.Maximum = 2;
            this.tbDifficultyLevel.Name = "tbDifficultyLevel";
            this.tbDifficultyLevel.Size = new System.Drawing.Size(287, 45);
            this.tbDifficultyLevel.TabIndex = 0;
            this.tbDifficultyLevel.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.tbDifficultyLevel.Value = 1;
            // 
            // lblMedium
            // 
            this.lblMedium.AutoSize = true;
            this.lblMedium.BackColor = System.Drawing.Color.Transparent;
            this.lblMedium.Location = new System.Drawing.Point(135, 371);
            this.lblMedium.Name = "lblMedium";
            this.lblMedium.Size = new System.Drawing.Size(44, 13);
            this.lblMedium.TabIndex = 7;
            this.lblMedium.Text = "Medium";
            this.lblMedium.MouseDown += new System.Windows.Forms.MouseEventHandler(this.CampaignSelector_MouseDown);
            this.lblMedium.MouseMove += new System.Windows.Forms.MouseEventHandler(this.CampaignSelector_MouseMove);
            this.lblMedium.MouseUp += new System.Windows.Forms.MouseEventHandler(this.CampaignSelector_MouseUp);
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Black;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.lblMissionDescription);
            this.panel1.Location = new System.Drawing.Point(14, 215);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(287, 76);
            this.panel1.TabIndex = 80;
            // 
            // lblEasy
            // 
            this.lblEasy.AutoSize = true;
            this.lblEasy.BackColor = System.Drawing.Color.Transparent;
            this.lblEasy.Location = new System.Drawing.Point(16, 371);
            this.lblEasy.Name = "lblEasy";
            this.lblEasy.Size = new System.Drawing.Size(30, 13);
            this.lblEasy.TabIndex = 9;
            this.lblEasy.Text = "Easy";
            // 
            // lblHard
            // 
            this.lblHard.AutoSize = true;
            this.lblHard.BackColor = System.Drawing.Color.Transparent;
            this.lblHard.Location = new System.Drawing.Point(271, 371);
            this.lblHard.Name = "lblHard";
            this.lblHard.Size = new System.Drawing.Size(30, 13);
            this.lblHard.TabIndex = 8;
            this.lblHard.Text = "Hard";
            // 
            // lblMissDescr
            // 
            this.lblMissDescr.AutoSize = true;
            this.lblMissDescr.BackColor = System.Drawing.Color.Transparent;
            this.lblMissDescr.Location = new System.Drawing.Point(12, 199);
            this.lblMissDescr.Name = "lblMissDescr";
            this.lblMissDescr.Size = new System.Drawing.Size(101, 13);
            this.lblMissDescr.TabIndex = 3;
            this.lblMissDescr.Text = "Mission Description:";
            // 
            // CampaignSelector
            // 
            this.AcceptButton = this.btnLaunch;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(317, 421);
            this.ControlBox = false;
            this.Controls.Add(this.lblMissDescr);
            this.Controls.Add(this.lblEasy);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.lblHard);
            this.Controls.Add(this.line2);
            this.Controls.Add(this.lblSelectDifficulty);
            this.Controls.Add(this.lblMedium);
            this.Controls.Add(this.tbDifficultyLevel);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnLaunch);
            this.Controls.Add(this.lblSelectCampaign);
            this.Controls.Add(this.lbCampaignList);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(185)))), ((int)(((byte)(0)))));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "CampaignSelector";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select Mission";
            this.Load += new System.EventHandler(this.CampaignSelector_Load);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.CampaignSelector_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.CampaignSelector_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.CampaignSelector_MouseUp);
            ((System.ComponentModel.ISupportInitialize)(this.tbDifficultyLevel)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox lbCampaignList;
        private System.Windows.Forms.Label lblSelectCampaign;
        private System.Windows.Forms.Label lblMissionDescription;
        private System.Windows.Forms.Label lblSelectDifficulty;
        private ClientGUI.SwitchingImageButton btnLaunch;
        private ClientGUI.SwitchingImageButton btnCancel;
        private System.Windows.Forms.Label line2;
        private System.Windows.Forms.TrackBar tbDifficultyLevel;
        private System.Windows.Forms.Label lblMedium;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lblEasy;
        private System.Windows.Forms.Label lblHard;
        private System.Windows.Forms.Label lblMissDescr;
    }
}