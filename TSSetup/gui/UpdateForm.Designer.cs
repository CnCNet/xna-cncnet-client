namespace dtasetup.gui
{
    partial class UpdateForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpdateForm));
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.progressBar2 = new System.Windows.Forms.ProgressBar();
            this.lblFileProgress = new System.Windows.Forms.Label();
            this.lblTotalProgress = new System.Windows.Forms.Label();
            this.lblFileProgressValue = new System.Windows.Forms.Label();
            this.lblTotalProgressValue = new System.Windows.Forms.Label();
            this.lblUpdateDescription = new System.Windows.Forms.Label();
            this.btnCancel = new ClientGUI.SwitchingImageButton();
            this.lblStatusText = new System.Windows.Forms.Label();
            this.lblCurrFileName = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // progressBar1
            // 
            this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar1.Location = new System.Drawing.Point(6, 106);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(428, 23);
            this.progressBar1.TabIndex = 0;
            this.progressBar1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseDown);
            this.progressBar1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseMove);
            this.progressBar1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseUp);
            // 
            // progressBar2
            // 
            this.progressBar2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar2.BackColor = System.Drawing.Color.Black;
            this.progressBar2.Location = new System.Drawing.Point(6, 179);
            this.progressBar2.Name = "progressBar2";
            this.progressBar2.Size = new System.Drawing.Size(428, 23);
            this.progressBar2.TabIndex = 1;
            this.progressBar2.MouseDown += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseDown);
            this.progressBar2.MouseMove += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseMove);
            this.progressBar2.MouseUp += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseUp);
            // 
            // lblFileProgress
            // 
            this.lblFileProgress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFileProgress.AutoSize = true;
            this.lblFileProgress.BackColor = System.Drawing.Color.Transparent;
            this.lblFileProgress.Location = new System.Drawing.Point(6, 90);
            this.lblFileProgress.Name = "lblFileProgress";
            this.lblFileProgress.Size = new System.Drawing.Size(172, 13);
            this.lblFileProgress.TabIndex = 3;
            this.lblFileProgress.Text = "Progress percentage of current file:";
            this.lblFileProgress.MouseDown += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseDown);
            this.lblFileProgress.MouseMove += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseMove);
            this.lblFileProgress.MouseUp += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseUp);
            // 
            // lblTotalProgress
            // 
            this.lblTotalProgress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTotalProgress.AutoSize = true;
            this.lblTotalProgress.BackColor = System.Drawing.Color.Transparent;
            this.lblTotalProgress.Location = new System.Drawing.Point(6, 163);
            this.lblTotalProgress.Name = "lblTotalProgress";
            this.lblTotalProgress.Size = new System.Drawing.Size(134, 13);
            this.lblTotalProgress.TabIndex = 4;
            this.lblTotalProgress.Text = "Total progress percentage:";
            this.lblTotalProgress.MouseDown += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseDown);
            this.lblTotalProgress.MouseMove += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseMove);
            this.lblTotalProgress.MouseUp += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseUp);
            // 
            // lblFileProgressValue
            // 
            this.lblFileProgressValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblFileProgressValue.AutoSize = true;
            this.lblFileProgressValue.BackColor = System.Drawing.Color.Transparent;
            this.lblFileProgressValue.Location = new System.Drawing.Point(396, 90);
            this.lblFileProgressValue.Name = "lblFileProgressValue";
            this.lblFileProgressValue.Size = new System.Drawing.Size(33, 13);
            this.lblFileProgressValue.TabIndex = 5;
            this.lblFileProgressValue.Text = "100%";
            this.lblFileProgressValue.MouseDown += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseDown);
            this.lblFileProgressValue.MouseMove += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseMove);
            this.lblFileProgressValue.MouseUp += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseUp);
            // 
            // lblTotalProgressValue
            // 
            this.lblTotalProgressValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTotalProgressValue.AutoSize = true;
            this.lblTotalProgressValue.BackColor = System.Drawing.Color.Transparent;
            this.lblTotalProgressValue.Location = new System.Drawing.Point(396, 163);
            this.lblTotalProgressValue.Name = "lblTotalProgressValue";
            this.lblTotalProgressValue.Size = new System.Drawing.Size(33, 13);
            this.lblTotalProgressValue.TabIndex = 6;
            this.lblTotalProgressValue.Text = "100%";
            this.lblTotalProgressValue.MouseDown += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseDown);
            this.lblTotalProgressValue.MouseMove += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseMove);
            this.lblTotalProgressValue.MouseUp += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseUp);
            // 
            // lblUpdateDescription
            // 
            this.lblUpdateDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblUpdateDescription.AutoSize = true;
            this.lblUpdateDescription.BackColor = System.Drawing.Color.Transparent;
            this.lblUpdateDescription.Location = new System.Drawing.Point(6, 5);
            this.lblUpdateDescription.Name = "lblUpdateDescription";
            this.lblUpdateDescription.Size = new System.Drawing.Size(273, 65);
            this.lblUpdateDescription.TabIndex = 7;
            this.lblUpdateDescription.Text = resources.GetString("lblUpdateDescription.Text");
            this.lblUpdateDescription.MouseDown += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseDown);
            this.lblUpdateDescription.MouseMove += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseMove);
            this.lblUpdateDescription.MouseUp += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseUp);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DefaultImage = null;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.HoveredImage = null;
            this.btnCancel.HoverSound = null;
            this.btnCancel.Location = new System.Drawing.Point(301, 220);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(133, 23);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // lblStatusText
            // 
            this.lblStatusText.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStatusText.AutoSize = true;
            this.lblStatusText.BackColor = System.Drawing.Color.Transparent;
            this.lblStatusText.Location = new System.Drawing.Point(6, 220);
            this.lblStatusText.Name = "lblStatusText";
            this.lblStatusText.Size = new System.Drawing.Size(99, 13);
            this.lblStatusText.TabIndex = 9;
            this.lblStatusText.Text = "Downloading files...";
            this.lblStatusText.MouseDown += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseDown);
            this.lblStatusText.MouseMove += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseMove);
            this.lblStatusText.MouseUp += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseUp);
            // 
            // lblCurrFileName
            // 
            this.lblCurrFileName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCurrFileName.AutoSize = true;
            this.lblCurrFileName.BackColor = System.Drawing.Color.Transparent;
            this.lblCurrFileName.Location = new System.Drawing.Point(6, 132);
            this.lblCurrFileName.Name = "lblCurrFileName";
            this.lblCurrFileName.Size = new System.Drawing.Size(60, 13);
            this.lblCurrFileName.TabIndex = 10;
            this.lblCurrFileName.Text = "Current file:";
            this.lblCurrFileName.MouseDown += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseDown);
            this.lblCurrFileName.MouseMove += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseMove);
            this.lblCurrFileName.MouseUp += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseUp);
            // 
            // UpdateForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(441, 246);
            this.ControlBox = false;
            this.Controls.Add(this.lblCurrFileName);
            this.Controls.Add(this.lblStatusText);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.lblUpdateDescription);
            this.Controls.Add(this.lblTotalProgressValue);
            this.Controls.Add(this.lblFileProgressValue);
            this.Controls.Add(this.lblTotalProgress);
            this.Controls.Add(this.lblFileProgress);
            this.Controls.Add(this.progressBar2);
            this.Controls.Add(this.progressBar1);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(185)))), ((int)(((byte)(0)))));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "UpdateForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "TS Updater";
            this.Load += new System.EventHandler(this.UpdateForm_Load);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.UpdateForm_MouseUp);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.ProgressBar progressBar2;
        private System.Windows.Forms.Label lblFileProgress;
        private System.Windows.Forms.Label lblTotalProgress;
        private System.Windows.Forms.Label lblFileProgressValue;
        private System.Windows.Forms.Label lblTotalProgressValue;
        private System.Windows.Forms.Label lblUpdateDescription;
        private ClientGUI.SwitchingImageButton btnCancel;
        private System.Windows.Forms.Label lblStatusText;
        private System.Windows.Forms.Label lblCurrFileName;
    }
}