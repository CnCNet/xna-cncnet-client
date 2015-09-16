namespace dtasetup.gui
{
    partial class MsgBoxForm
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
            this.lblTitle = new System.Windows.Forms.Label();
            this.line1 = new System.Windows.Forms.Label();
            this.lblMsgText = new System.Windows.Forms.Label();
            this.btnOK = new ClientGUI.SwitchingImageButton();
            this.btnCancel = new ClientGUI.SwitchingImageButton();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblTitle.Font = new System.Drawing.Font("Trebuchet MS", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.ForeColor = System.Drawing.Color.LimeGreen;
            this.lblTitle.Location = new System.Drawing.Point(2, 3);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(87, 18);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Message title";
            this.lblTitle.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MsgBoxForm_MouseDown);
            this.lblTitle.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MsgBoxForm_MouseMove);
            this.lblTitle.MouseUp += new System.Windows.Forms.MouseEventHandler(this.MsgBoxForm_MouseUp);
            // 
            // line1
            // 
            this.line1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.line1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.line1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.line1.Location = new System.Drawing.Point(6, 22);
            this.line1.Name = "line1";
            this.line1.Size = new System.Drawing.Size(280, 1);
            this.line1.TabIndex = 18;
            // 
            // lblMsgText
            // 
            this.lblMsgText.AutoSize = true;
            this.lblMsgText.BackColor = System.Drawing.Color.Transparent;
            this.lblMsgText.Font = new System.Drawing.Font("Trebuchet MS", 8.25F);
            this.lblMsgText.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(185)))), ((int)(((byte)(0)))));
            this.lblMsgText.Location = new System.Drawing.Point(3, 33);
            this.lblMsgText.Name = "lblMsgText";
            this.lblMsgText.Size = new System.Drawing.Size(75, 16);
            this.lblMsgText.TabIndex = 19;
            this.lblMsgText.Text = "Message text";
            this.lblMsgText.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MsgBoxForm_MouseDown);
            this.lblMsgText.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MsgBoxForm_MouseMove);
            this.lblMsgText.MouseUp += new System.Windows.Forms.MouseEventHandler(this.MsgBoxForm_MouseUp);
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnOK.BackColor = System.Drawing.Color.White;
            this.btnOK.DefaultImage = null;
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.FlatAppearance.BorderColor = System.Drawing.Color.DimGray;
            this.btnOK.FlatAppearance.BorderSize = 0;
            this.btnOK.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Goldenrod;
            this.btnOK.FlatAppearance.MouseOverBackColor = System.Drawing.Color.LimeGreen;
            this.btnOK.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOK.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOK.ForeColor = System.Drawing.Color.LimeGreen;
            this.btnOK.HoveredImage = null;
            this.btnOK.HoverSound = null;
            this.btnOK.Location = new System.Drawing.Point(43, 231);
            this.btnOK.Name = "btnOK";
            this.btnOK.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnOK.Size = new System.Drawing.Size(97, 23);
            this.btnOK.TabIndex = 53;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = false;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCancel.BackColor = System.Drawing.Color.White;
            this.btnCancel.DefaultImage = null;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.DimGray;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Goldenrod;
            this.btnCancel.FlatAppearance.MouseOverBackColor = System.Drawing.Color.LimeGreen;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCancel.ForeColor = System.Drawing.Color.LimeGreen;
            this.btnCancel.HoveredImage = null;
            this.btnCancel.HoverSound = null;
            this.btnCancel.Location = new System.Drawing.Point(173, 231);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(97, 23);
            this.btnCancel.TabIndex = 54;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            // 
            // MsgBoxForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(292, 266);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.lblMsgText);
            this.Controls.Add(this.line1);
            this.Controls.Add(this.lblTitle);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(185)))), ((int)(((byte)(0)))));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.KeyPreview = true;
            this.Name = "MsgBoxForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "MsgBoxForm";
            this.Load += new System.EventHandler(this.MsgBoxForm_Load);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.MsgBoxForm_KeyPress);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MsgBoxForm_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MsgBoxForm_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.MsgBoxForm_MouseUp);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label line1;
        private System.Windows.Forms.Label lblMsgText;
        private ClientGUI.SwitchingImageButton btnOK;
        private ClientGUI.SwitchingImageButton btnCancel;
    }
}