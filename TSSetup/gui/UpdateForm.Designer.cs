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
            this.progressBar1.Location = new System.Drawing.Point(12, 110);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(428, 23);
            this.progressBar1.TabIndex = 0;
            // 
            // progressBar2
            // 
            this.progressBar2.BackColor = System.Drawing.Color.Black;
            this.progressBar2.Location = new System.Drawing.Point(12, 183);
            this.progressBar2.Name = "progressBar2";
            this.progressBar2.Size = new System.Drawing.Size(428, 23);
            this.progressBar2.TabIndex = 1;
            // 
            // lblFileProgress
            // 
            this.lblFileProgress.AutoSize = true;
            this.lblFileProgress.BackColor = System.Drawing.Color.Transparent;
            this.lblFileProgress.Location = new System.Drawing.Point(12, 94);
            this.lblFileProgress.Name = "lblFileProgress";
            this.lblFileProgress.Size = new System.Drawing.Size(172, 13);
            this.lblFileProgress.TabIndex = 3;
            this.lblFileProgress.Text = "Progress percentage of current file:";
            // 
            // lblTotalProgress
            // 
            this.lblTotalProgress.AutoSize = true;
            this.lblTotalProgress.BackColor = System.Drawing.Color.Transparent;
            this.lblTotalProgress.Location = new System.Drawing.Point(12, 167);
            this.lblTotalProgress.Name = "lblTotalProgress";
            this.lblTotalProgress.Size = new System.Drawing.Size(134, 13);
            this.lblTotalProgress.TabIndex = 4;
            this.lblTotalProgress.Text = "Total progress percentage:";
            // 
            // lblFileProgressValue
            // 
            this.lblFileProgressValue.AutoSize = true;
            this.lblFileProgressValue.BackColor = System.Drawing.Color.Transparent;
            this.lblFileProgressValue.Location = new System.Drawing.Point(402, 94);
            this.lblFileProgressValue.Name = "lblFileProgressValue";
            this.lblFileProgressValue.Size = new System.Drawing.Size(33, 13);
            this.lblFileProgressValue.TabIndex = 5;
            this.lblFileProgressValue.Text = "100%";
            // 
            // lblTotalProgressValue
            // 
            this.lblTotalProgressValue.AutoSize = true;
            this.lblTotalProgressValue.BackColor = System.Drawing.Color.Transparent;
            this.lblTotalProgressValue.Location = new System.Drawing.Point(402, 167);
            this.lblTotalProgressValue.Name = "lblTotalProgressValue";
            this.lblTotalProgressValue.Size = new System.Drawing.Size(33, 13);
            this.lblTotalProgressValue.TabIndex = 6;
            this.lblTotalProgressValue.Text = "100%";
            // 
            // lblUpdateDescription
            // 
            this.lblUpdateDescription.AutoSize = true;
            this.lblUpdateDescription.BackColor = System.Drawing.Color.Transparent;
            this.lblUpdateDescription.Location = new System.Drawing.Point(12, 9);
            this.lblUpdateDescription.Name = "lblUpdateDescription";
            this.lblUpdateDescription.Size = new System.Drawing.Size(273, 65);
            this.lblUpdateDescription.TabIndex = 7;
            this.lblUpdateDescription.Text = resources.GetString("lblUpdateDescription.Text");
            // 
            // btnCancel
            // 
            this.btnCancel.DefaultImage = null;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.HoveredImage = null;
            this.btnCancel.HoverSound = null;
            this.btnCancel.Location = new System.Drawing.Point(307, 224);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(133, 23);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // lblStatusText
            // 
            this.lblStatusText.AutoSize = true;
            this.lblStatusText.BackColor = System.Drawing.Color.Transparent;
            this.lblStatusText.Location = new System.Drawing.Point(12, 224);
            this.lblStatusText.Name = "lblStatusText";
            this.lblStatusText.Size = new System.Drawing.Size(99, 13);
            this.lblStatusText.TabIndex = 9;
            this.lblStatusText.Text = "Downloading files...";
            // 
            // lblCurrFileName
            // 
            this.lblCurrFileName.AutoSize = true;
            this.lblCurrFileName.BackColor = System.Drawing.Color.Transparent;
            this.lblCurrFileName.Location = new System.Drawing.Point(12, 136);
            this.lblCurrFileName.Name = "lblCurrFileName";
            this.lblCurrFileName.Size = new System.Drawing.Size(60, 13);
            this.lblCurrFileName.TabIndex = 10;
            this.lblCurrFileName.Text = "Current file:";
            // 
            // UpdateForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(450, 257);
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
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UpdateForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "TS Updater";
            this.Load += new System.EventHandler(this.UpdateForm_Load);
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