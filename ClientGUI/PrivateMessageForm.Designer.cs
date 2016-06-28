namespace ClientGUI
{
    partial class PrivateMessageForm
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
            this.btnPMSend = new System.Windows.Forms.Button();
            this.tbPMChatMessage = new System.Windows.Forms.TextBox();
            this.lbPMChatMessages = new System.Windows.Forms.ListBox();
            this.lbPMRecipients = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // btnPMSend
            // 
            this.btnPMSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPMSend.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPMSend.Location = new System.Drawing.Point(441, 292);
            this.btnPMSend.Name = "btnPMSend";
            this.btnPMSend.Size = new System.Drawing.Size(75, 23);
            this.btnPMSend.TabIndex = 3;
            this.btnPMSend.Text = "Send";
            this.btnPMSend.UseVisualStyleBackColor = true;
            this.btnPMSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // tbPMChatMessage
            // 
            this.tbPMChatMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbPMChatMessage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.tbPMChatMessage.Location = new System.Drawing.Point(175, 294);
            this.tbPMChatMessage.Name = "tbPMChatMessage";
            this.tbPMChatMessage.Size = new System.Drawing.Size(260, 20);
            this.tbPMChatMessage.TabIndex = 4;
            // 
            // lbPMChatMessages
            // 
            this.lbPMChatMessages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbPMChatMessages.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lbPMChatMessages.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.lbPMChatMessages.FormattingEnabled = true;
            this.lbPMChatMessages.Location = new System.Drawing.Point(175, 13);
            this.lbPMChatMessages.Name = "lbPMChatMessages";
            this.lbPMChatMessages.Size = new System.Drawing.Size(341, 275);
            this.lbPMChatMessages.TabIndex = 5;
            this.lbPMChatMessages.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.lbChatMessages_DrawItem);
            this.lbPMChatMessages.MeasureItem += new System.Windows.Forms.MeasureItemEventHandler(this.lbChatMessages_MeasureItem);
            this.lbPMChatMessages.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lbChatMessages_KeyDown);
            // 
            // lbPMRecipients
            // 
            this.lbPMRecipients.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lbPMRecipients.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lbPMRecipients.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.lbPMRecipients.FormattingEnabled = true;
            this.lbPMRecipients.IntegralHeight = false;
            this.lbPMRecipients.Location = new System.Drawing.Point(2, 13);
            this.lbPMRecipients.Name = "lbPMRecipients";
            this.lbPMRecipients.Size = new System.Drawing.Size(167, 301);
            this.lbPMRecipients.TabIndex = 6;
            this.lbPMRecipients.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.lbPMRecipients_DrawItem);
            this.lbPMRecipients.SelectedIndexChanged += new System.EventHandler(this.lbPMRecipients_SelectedIndexChanged);
            // 
            // PrivateMessageForm
            // 
            this.AcceptButton = this.btnPMSend;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(519, 318);
            this.Controls.Add(this.lbPMRecipients);
            this.Controls.Add(this.lbPMChatMessages);
            this.Controls.Add(this.tbPMChatMessage);
            this.Controls.Add(this.btnPMSend);
            this.MinimumSize = new System.Drawing.Size(366, 357);
            this.Name = "PrivateMessageForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Private messaging";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PrivateMessageForm_FormClosing);
            this.Load += new System.EventHandler(this.PrivateMessageForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btnPMSend;
        private System.Windows.Forms.TextBox tbPMChatMessage;
        private System.Windows.Forms.ListBox lbPMChatMessages;
        private System.Windows.Forms.ListBox lbPMRecipients;
    }
}