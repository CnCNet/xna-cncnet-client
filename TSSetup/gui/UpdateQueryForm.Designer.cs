namespace dtasetup.gui
{
    partial class UpdateQueryForm
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
            this.lblUpdateInfo = new System.Windows.Forms.Label();
            this.changelogLL = new System.Windows.Forms.LinkLabel();
            this.btnAccept = new ClientGUI.SwitchingImageButton();
            this.btnDeny = new ClientGUI.SwitchingImageButton();
            this.lblUpdateSize = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblUpdateInfo
            // 
            this.lblUpdateInfo.AutoSize = true;
            this.lblUpdateInfo.BackColor = System.Drawing.Color.Transparent;
            this.lblUpdateInfo.Location = new System.Drawing.Point(12, 9);
            this.lblUpdateInfo.Name = "lblUpdateInfo";
            this.lblUpdateInfo.Size = new System.Drawing.Size(181, 26);
            this.lblUpdateInfo.TabIndex = 1;
            this.lblUpdateInfo.Text = "Version {0} is available for download.\r\nDo you wish to install it?";
            // 
            // changelogLL
            // 
            this.changelogLL.AutoSize = true;
            this.changelogLL.BackColor = System.Drawing.Color.Transparent;
            this.changelogLL.Font = new System.Drawing.Font("Trebuchet MS", 8.25F);
            this.changelogLL.ForeColor = System.Drawing.Color.Goldenrod;
            this.changelogLL.LinkColor = System.Drawing.Color.Goldenrod;
            this.changelogLL.Location = new System.Drawing.Point(12, 52);
            this.changelogLL.Name = "changelogLL";
            this.changelogLL.Size = new System.Drawing.Size(165, 16);
            this.changelogLL.TabIndex = 2;
            this.changelogLL.TabStop = true;
            this.changelogLL.Text = "Click here to see the changelog";
            this.changelogLL.VisitedLinkColor = System.Drawing.Color.Red;
            this.changelogLL.MouseClick += new System.Windows.Forms.MouseEventHandler(this.changelogLL_MouseClick);
            this.changelogLL.MouseEnter += new System.EventHandler(this.changelogLL_MouseEnter);
            this.changelogLL.MouseLeave += new System.EventHandler(this.changelogLL_MouseLeave);
            // 
            // btnAccept
            // 
            this.btnAccept.DefaultImage = null;
            this.btnAccept.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnAccept.FlatAppearance.BorderSize = 0;
            this.btnAccept.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAccept.HoveredImage = null;
            this.btnAccept.HoverSound = null;
            this.btnAccept.Location = new System.Drawing.Point(13, 110);
            this.btnAccept.Name = "btnAccept";
            this.btnAccept.Size = new System.Drawing.Size(75, 23);
            this.btnAccept.TabIndex = 3;
            this.btnAccept.Text = "Yes";
            this.btnAccept.UseVisualStyleBackColor = true;
            // 
            // btnDeny
            // 
            this.btnDeny.DefaultImage = null;
            this.btnDeny.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnDeny.FlatAppearance.BorderSize = 0;
            this.btnDeny.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDeny.HoveredImage = null;
            this.btnDeny.HoverSound = null;
            this.btnDeny.Location = new System.Drawing.Point(147, 110);
            this.btnDeny.Name = "btnDeny";
            this.btnDeny.Size = new System.Drawing.Size(75, 23);
            this.btnDeny.TabIndex = 4;
            this.btnDeny.Text = "No";
            this.btnDeny.UseVisualStyleBackColor = true;
            // 
            // lblUpdateSize
            // 
            this.lblUpdateSize.AutoSize = true;
            this.lblUpdateSize.BackColor = System.Drawing.Color.Transparent;
            this.lblUpdateSize.Location = new System.Drawing.Point(12, 79);
            this.lblUpdateSize.Name = "lblUpdateSize";
            this.lblUpdateSize.Size = new System.Drawing.Size(148, 13);
            this.lblUpdateSize.TabIndex = 5;
            this.lblUpdateSize.Text = "Update size would be: {0} MB";
            // 
            // UpdateQueryForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(235, 147);
            this.ControlBox = false;
            this.Controls.Add(this.lblUpdateSize);
            this.Controls.Add(this.btnDeny);
            this.Controls.Add(this.btnAccept);
            this.Controls.Add(this.changelogLL);
            this.Controls.Add(this.lblUpdateInfo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UpdateQueryForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Updates are available";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblUpdateInfo;
        private System.Windows.Forms.LinkLabel changelogLL;
        private ClientGUI.SwitchingImageButton btnAccept;
        private ClientGUI.SwitchingImageButton btnDeny;
        private System.Windows.Forms.Label lblUpdateSize;
    }
}