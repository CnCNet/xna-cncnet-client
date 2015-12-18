namespace dtasetup.gui
{
    partial class CheaterForm
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
            this.pbFacepalm = new System.Windows.Forms.PictureBox();
            this.lblDescription = new System.Windows.Forms.Label();
            this.btnCancel = new ClientGUI.SwitchingImageButton();
            this.btnYes = new ClientGUI.SwitchingImageButton();
            ((System.ComponentModel.ISupportInitialize)(this.pbFacepalm)).BeginInit();
            this.SuspendLayout();
            // 
            // pbFacepalm
            // 
            this.pbFacepalm.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbFacepalm.Location = new System.Drawing.Point(15, 80);
            this.pbFacepalm.Name = "pbFacepalm";
            this.pbFacepalm.Size = new System.Drawing.Size(288, 180);
            this.pbFacepalm.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbFacepalm.TabIndex = 0;
            this.pbFacepalm.TabStop = false;
            // 
            // lblDescription
            // 
            this.lblDescription.AutoSize = true;
            this.lblDescription.BackColor = System.Drawing.Color.Transparent;
            this.lblDescription.Location = new System.Drawing.Point(12, 9);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(283, 52);
            this.lblDescription.TabIndex = 4;
            this.lblDescription.Text = "You\'re running the game with unofficial modifications. They\r\ncould make the missi" +
    "on different from intended.\r\n\r\nAre you sure you want to play the mission?";
            // 
            // btnCancel
            // 
            this.btnCancel.DefaultImage = null;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.No;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.HoveredImage = null;
            this.btnCancel.HoverSound = null;
            this.btnCancel.Location = new System.Drawing.Point(170, 266);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(133, 23);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "No";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnYes
            // 
            this.btnYes.DefaultImage = null;
            this.btnYes.DialogResult = System.Windows.Forms.DialogResult.Yes;
            this.btnYes.FlatAppearance.BorderSize = 0;
            this.btnYes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnYes.HoveredImage = null;
            this.btnYes.HoverSound = null;
            this.btnYes.Location = new System.Drawing.Point(15, 266);
            this.btnYes.Name = "btnYes";
            this.btnYes.Size = new System.Drawing.Size(133, 23);
            this.btnYes.TabIndex = 7;
            this.btnYes.Text = "Yes";
            this.btnYes.UseVisualStyleBackColor = true;
            // 
            // CheaterForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(317, 301);
            this.ControlBox = false;
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnYes);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.pbFacepalm);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "CheaterForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Cheater!";
            this.Load += new System.EventHandler(this.CheaterForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pbFacepalm)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pbFacepalm;
        private System.Windows.Forms.Label lblDescription;
        private ClientGUI.SwitchingImageButton btnCancel;
        private ClientGUI.SwitchingImageButton btnYes;
    }
}