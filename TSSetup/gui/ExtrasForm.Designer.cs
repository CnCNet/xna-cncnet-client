namespace dtasetup.gui
{
    partial class ExtrasForm
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
            this.btnExtraMapEditor = new ClientGUI.SwitchingImageButton();
            this.btnExtraStatistics = new ClientGUI.SwitchingImageButton();
            this.btnExtraCredits = new ClientGUI.SwitchingImageButton();
            this.btnExtraCancel = new ClientGUI.SwitchingImageButton();
            this.SuspendLayout();
            // 
            // btnExtraMapEditor
            // 
            this.btnExtraMapEditor.DefaultImage = null;
            this.btnExtraMapEditor.FlatAppearance.BorderSize = 0;
            this.btnExtraMapEditor.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExtraMapEditor.HoveredImage = null;
            this.btnExtraMapEditor.HoverSound = null;
            this.btnExtraMapEditor.Location = new System.Drawing.Point(76, 61);
            this.btnExtraMapEditor.Name = "btnExtraMapEditor";
            this.btnExtraMapEditor.Size = new System.Drawing.Size(133, 23);
            this.btnExtraMapEditor.TabIndex = 6;
            this.btnExtraMapEditor.Text = "Map Editor";
            this.btnExtraMapEditor.UseVisualStyleBackColor = true;
            this.btnExtraMapEditor.Click += new System.EventHandler(this.btnExtraMapEditor_Click);
            // 
            // btnExtraStatistics
            // 
            this.btnExtraStatistics.DefaultImage = null;
            this.btnExtraStatistics.FlatAppearance.BorderSize = 0;
            this.btnExtraStatistics.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExtraStatistics.HoveredImage = null;
            this.btnExtraStatistics.HoverSound = null;
            this.btnExtraStatistics.Location = new System.Drawing.Point(76, 17);
            this.btnExtraStatistics.Name = "btnExtraStatistics";
            this.btnExtraStatistics.Size = new System.Drawing.Size(133, 23);
            this.btnExtraStatistics.TabIndex = 7;
            this.btnExtraStatistics.Text = "Statistics";
            this.btnExtraStatistics.UseVisualStyleBackColor = true;
            this.btnExtraStatistics.Click += new System.EventHandler(this.btnExtraStatistics_Click);
            // 
            // btnExtraCredits
            // 
            this.btnExtraCredits.DefaultImage = null;
            this.btnExtraCredits.FlatAppearance.BorderSize = 0;
            this.btnExtraCredits.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExtraCredits.HoveredImage = null;
            this.btnExtraCredits.HoverSound = null;
            this.btnExtraCredits.Location = new System.Drawing.Point(76, 103);
            this.btnExtraCredits.Name = "btnExtraCredits";
            this.btnExtraCredits.Size = new System.Drawing.Size(133, 23);
            this.btnExtraCredits.TabIndex = 8;
            this.btnExtraCredits.Text = "Credits";
            this.btnExtraCredits.UseVisualStyleBackColor = true;
            this.btnExtraCredits.Click += new System.EventHandler(this.btnExtraCredits_Click);
            // 
            // btnExtraCancel
            // 
            this.btnExtraCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnExtraCancel.DefaultImage = null;
            this.btnExtraCancel.FlatAppearance.BorderSize = 0;
            this.btnExtraCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExtraCancel.HoveredImage = null;
            this.btnExtraCancel.HoverSound = null;
            this.btnExtraCancel.Location = new System.Drawing.Point(76, 190);
            this.btnExtraCancel.Name = "btnExtraCancel";
            this.btnExtraCancel.Size = new System.Drawing.Size(133, 23);
            this.btnExtraCancel.TabIndex = 9;
            this.btnExtraCancel.Text = "Cancel";
            this.btnExtraCancel.UseVisualStyleBackColor = true;
            this.btnExtraCancel.Click += new System.EventHandler(this.btnExtraCancel_Click);
            // 
            // ExtrasForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(284, 225);
            this.ControlBox = false;
            this.Controls.Add(this.btnExtraCancel);
            this.Controls.Add(this.btnExtraCredits);
            this.Controls.Add(this.btnExtraStatistics);
            this.Controls.Add(this.btnExtraMapEditor);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "ExtrasForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Extras";
            this.Load += new System.EventHandler(this.ExtrasForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private ClientGUI.SwitchingImageButton btnExtraMapEditor;
        private ClientGUI.SwitchingImageButton btnExtraStatistics;
        private ClientGUI.SwitchingImageButton btnExtraCredits;
        private ClientGUI.SwitchingImageButton btnExtraCancel;
    }
}