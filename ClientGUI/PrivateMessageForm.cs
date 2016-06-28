using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ClientCore;
using ClientCore.CnCNet5;

namespace ClientGUI
{
    public partial class PrivateMessageForm : MovableForm
    {
        delegate void UserQuitDelegate(string userName);
        delegate void DualStringDelegate(string message, string sender);
        delegate void TripleStringDelegate(string str1, string str2, string str3);

        List<Color> MessageColors = new List<Color>();
        List<Color> UserColors = new List<Color>();
        List<string> AwayMessageReceivedFrom = new List<string>();
        Color cReceivedPMColor;
        Color cListBoxFocusColor;
        Color cAltUiColor;

        string defRecipient = String.Empty;

        public PrivateMessageForm(Color foreColor, string recipient)
        {
            InitializeComponent();
            lbPMChatMessages.ForeColor = foreColor;
            defRecipient = recipient;
        }

        private void PrivateMessageForm_Load(object sender, EventArgs e)
        {
            this.ActiveControl = tbPMChatMessage; // Give chat input the first focus

            this.Icon = Icon.ExtractAssociatedIcon(ProgramConstants.GamePath + ProgramConstants.RESOURCES_DIR + "pm.ico");
            this.BackgroundImage = SharedUILogic.LoadImage("privatemessagebg.png");

            this.Font = SharedLogic.GetCommonFont();

            string[] labelColor = DomainController.Instance().GetUILabelColor().Split(',');
            Color cLabelColor = Color.FromArgb(Convert.ToByte(labelColor[0]), Convert.ToByte(labelColor[1]), Convert.ToByte(labelColor[2]));

            string[] altUiColor = DomainController.Instance().GetUIAltColor().Split(',');
            cAltUiColor = Color.FromArgb(Convert.ToByte(altUiColor[0]), Convert.ToByte(altUiColor[1]), Convert.ToByte(altUiColor[2]));
            lbPMRecipients.ForeColor = cAltUiColor;
            tbPMChatMessage.ForeColor = cAltUiColor;
            lbPMChatMessages.ForeColor = cAltUiColor;
            btnPMSend.ForeColor = cAltUiColor;

            string[] backgroundColor = DomainController.Instance().GetUIAltBackgroundColor().Split(',');
            Color cBackColor = Color.FromArgb(Convert.ToByte(backgroundColor[0]), Convert.ToByte(backgroundColor[1]), Convert.ToByte(backgroundColor[2]));
            lbPMRecipients.BackColor = cBackColor;
            tbPMChatMessage.BackColor = cBackColor;
            lbPMChatMessages.BackColor = cBackColor;
            btnPMSend.BackColor = cBackColor;

            string[] receivedColor = DomainController.Instance().GetReceivedPMColor().Split(',');
            cReceivedPMColor = Color.FromArgb(Convert.ToByte(receivedColor[0]), Convert.ToByte(receivedColor[1]), Convert.ToByte(receivedColor[2]));

            string[] listBoxFocusColor = DomainController.Instance().GetListBoxFocusColor().Split(',');
            cListBoxFocusColor = Color.FromArgb(Convert.ToByte(listBoxFocusColor[0]), Convert.ToByte(listBoxFocusColor[1]), Convert.ToByte(listBoxFocusColor[2]));

            foreach (PrivateMessageInfo pmInfo in CnCNetData.PMInfos)
            {
                if (!lbPMRecipients.Items.Contains(pmInfo.UserName))
                {
                    lbPMRecipients.Items.Add(pmInfo.UserName);
                    UserColors.Add(cAltUiColor);
                }
            }

            int index = FindNameIndex(defRecipient);

            if (index == -1)
            {
                if (String.IsNullOrEmpty(defRecipient))
                {
                    if (lbPMRecipients.Items.Count > 0)
                        lbPMRecipients.SelectedIndex = 0;
                    else
                        lbPMRecipients.SelectedIndex = -1;
                }
                else
                {
                    lbPMRecipients.Items.Add(defRecipient);
                    UserColors.Add(cAltUiColor);
                    lbPMRecipients.SelectedIndex = lbPMRecipients.Items.Count - 1;
                }
            }
            else
                lbPMRecipients.SelectedIndex = index;


            CnCNetData.ConnectionBridge.PrivateMessageParsed += new RConnectionBridge.PrivateMessageParsedEventHandler(Instance_PrivateMessageParsed);
            CnCNetData.ConnectionBridge.PrivateMessageSent += new RConnectionBridge.PrivateMessageSentEventHandler(Instance_PrivateMessageSent);
            CnCNetData.ConnectionBridge.OnUserQuit += new RConnectionBridge.StringEventHandler(Instance_OnUserQuit);
            CnCNetData.ConnectionBridge.OnUserJoinedChannel += ConnectionBridge_OnUserJoinedChannel;
            CnCNetData.ConnectionBridge.OnAwayMessageReceived += Instance_OnAwayMessageReceived;
            //NCnCNetLobby.ConversationOpened += new NCnCNetLobby.ConversationOpenedCallback(NCnCNetLobby_ConversationOpened);
            Flash();

            SharedUILogic.ParseClientThemeIni(this);

            tbPMChatMessage.Focus();
        }

        private void ConnectionBridge_OnUserJoinedChannel(string channelName, string userName, string address)
        {
            if (this.InvokeRequired)
            {
                TripleStringDelegate d = new TripleStringDelegate(ConnectionBridge_OnUserJoinedChannel);
                this.BeginInvoke(d, channelName, userName, address);
                return;
            }

            if (channelName.ToLower() != "#cncnet")
                return;

            int index = CnCNetData.PMInfos.FindIndex(p => p.UserName == userName);

            if (index > -1)
            {
                UserColors[index] = cAltUiColor;
                lbPMRecipients.Refresh();
            }
        }

        private void Instance_OnAwayMessageReceived(string userName, string reason)
        {
            if (this.InvokeRequired)
            {
                DualStringDelegate d = new DualStringDelegate(Instance_OnAwayMessageReceived);
                this.BeginInvoke(d, userName, reason);
                return;
            }

            if (lbPMRecipients.SelectedIndex == -1)
                return;

            if (AwayMessageReceivedFrom.Contains(userName))
                return;

            if (FindNameIndex(userName) == lbPMRecipients.SelectedIndex)
            {
                lbPMChatMessages.Items.Add(userName + " is currently away: " + reason);
                MessageColors.Add(Color.White);
                lbPMChatMessages.SelectedIndex = lbPMChatMessages.Items.Count - 1;
                lbPMChatMessages.SelectedIndex = -1;
                AwayMessageReceivedFrom.Add(userName);
            }
        }

        private void NCnCNetLobby_ConversationOpened(string userName)
        {
            int itemIndex = FindNameIndex(userName);
            if (itemIndex == -1)
            {
                lbPMRecipients.Items.Add(userName);
                UserColors.Add(cAltUiColor);
                lbPMRecipients.SelectedIndex = lbPMRecipients.Items.Count - 1;
            }
            else
            {
                lbPMRecipients.SelectedIndex = itemIndex;
                if (this.WindowState == FormWindowState.Minimized)
                    this.WindowState = FormWindowState.Normal;
                this.Activate();
                WindowFlasherCC.FlashWindowEx(this);
            }
        }

        private int FindNameIndex(string name)
        {
            for (int itemId = 0; itemId < lbPMRecipients.Items.Count; itemId++)
            {
                if (lbPMRecipients.Items[itemId].ToString() == name)
                    return itemId;
            }

            return -1;
        }

        private void lbChatMessages_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = (int)e.Graphics.MeasureString(lbPMChatMessages.Items[e.Index].ToString(),
                lbPMChatMessages.Font, lbPMChatMessages.Width - 10).Height;
        }

        private void lbChatMessages_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index > -1 && e.Index < lbPMChatMessages.Items.Count)
            {
                if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                    e = new DrawItemEventArgs(e.Graphics,
                                              e.Font,
                                              e.Bounds,
                                              e.Index,
                                              e.State ^ DrawItemState.Selected,
                                              e.ForeColor,
                                              cListBoxFocusColor);

                e.DrawBackground();
                e.DrawFocusRectangle();

                e.Graphics.DrawString(lbPMChatMessages.Items[e.Index].ToString(), e.Font, new SolidBrush(MessageColors[e.Index]), e.Bounds);
            }
        }

        private void Instance_PrivateMessageParsed(string message, string sender)
        {
            if (this.InvokeRequired)
            {
                DualStringDelegate d = new DualStringDelegate(Instance_PrivateMessageParsed);
                this.BeginInvoke(d, message, sender);
                return;
            }

            if (lbPMRecipients.SelectedIndex == -1)
                return;

            if (sender == lbPMRecipients.Items[lbPMRecipients.SelectedIndex].ToString())
            {
                lbPMChatMessages.Items.Add("[" + DateTime.Now.ToShortTimeString() + "] " +
                    sender + ": " + message);
                MessageColors.Add(cReceivedPMColor);
                lbPMChatMessages.SelectedIndex = lbPMChatMessages.Items.Count - 1;
                lbPMChatMessages.SelectedIndex = -1;
                Flash();
            }
            else
            {
                if (!UserExists(sender))
                {
                    lbPMRecipients.Items.Add(sender);
                    UserColors.Add(Color.White);
                }
                int index = lbPMRecipients.Items.IndexOf(sender);
                if (index > -1)
                    UserColors[index] = Color.White;
                lbPMRecipients.Refresh();
                Flash();
            }
        }

        private bool UserExists(string userName)
        {
            for (int userId = 0; userId < lbPMRecipients.Items.Count; userId++)
            {
                if (userName == lbPMRecipients.Items[userId].ToString())
                    return true;
            }

            return false;
        }

        private void Instance_PrivateMessageSent(string message, string receiver)
        {
            if (this.InvokeRequired)
            {
                DualStringDelegate d = new DualStringDelegate(Instance_PrivateMessageSent);
                this.BeginInvoke(d, message, receiver);
                return;
            }

            if (lbPMRecipients.SelectedIndex == -1)
                return;

            if (receiver == lbPMRecipients.Items[lbPMRecipients.SelectedIndex].ToString())
            {
                lbPMChatMessages.Items.Add("[" + DateTime.Now.ToShortTimeString() + "] " +
                    ProgramConstants.PLAYERNAME + ": " + message);
                MessageColors.Add(lbPMChatMessages.ForeColor);
            }
        }

        private void Instance_OnUserQuit(string userName)
        {
            if (this.InvokeRequired)
            {
                UserQuitDelegate d = new UserQuitDelegate(Instance_OnUserQuit);
                this.BeginInvoke(d, userName);
                return;
            }

            if (lbPMRecipients.SelectedIndex == -1)
                return;

            int index = lbPMRecipients.Items.IndexOf(userName);
            if (index > -1)
            {
                UserColors[index] = Color.DarkGray;
                lbPMRecipients.Refresh();
            }

            if (userName == lbPMRecipients.Items[lbPMRecipients.SelectedIndex].ToString())
            {
                lbPMChatMessages.Items.Add(userName + " has quit CnCNet.");
                MessageColors.Add(Color.White);
                Flash();
            }
        }

        private void PrivateMessageForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            CnCNetData.ConnectionBridge.PrivateMessageParsed -= Instance_PrivateMessageParsed;
            CnCNetData.ConnectionBridge.PrivateMessageSent -= Instance_PrivateMessageSent;
            CnCNetData.ConnectionBridge.OnUserQuit -= Instance_OnUserQuit;
            CnCNetData.isPMWindowOpen = false;
        }

        private void lbPMRecipients_SelectedIndexChanged(object sender, EventArgs e)
        {
            lbPMChatMessages.Items.Clear();
            MessageColors.Clear();
            string recipientName = lbPMRecipients.Items[lbPMRecipients.SelectedIndex].ToString();

            int index = CnCNetData.PMInfos.FindIndex(c => c.UserName == recipientName);
            if (index > -1)
            {
                PrivateMessageInfo pmInfo = CnCNetData.PMInfos[index];

                if (UserColors[index] != Color.DarkGray)
                {
                    UserColors[index] = cAltUiColor;
                    lbPMRecipients.Refresh();
                }

                for (int msgId = 0; msgId < pmInfo.Messages.Count; msgId++)
                {
                    MessageInfo msgInfo = pmInfo.Messages[msgId];

                    if (pmInfo.IsSelfSent[msgId])
                    {
                        lbPMChatMessages.Items.Add("[" + msgInfo.Time.ToShortTimeString() + "] " +
                            ProgramConstants.PLAYERNAME + ": " + msgInfo.Message);
                        MessageColors.Add(msgInfo.Color);
                    }
                    else
                    {
                        lbPMChatMessages.Items.Add("[" + msgInfo.Time.ToShortTimeString() + "] " + 
                            recipientName + ": " + msgInfo.Message);
                        MessageColors.Add(msgInfo.Color);
                    }
                }
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (lbPMRecipients.SelectedIndex == -1)
            {
                lbPMChatMessages.Items.Add("No player selected!");
                MessageColors.Add(Color.White);
                return;
            }

            if (String.IsNullOrEmpty(tbPMChatMessage.Text))
            {
                lbPMChatMessages.Items.Add("Type your message into the field next to the Send button.");
                MessageColors.Add(Color.White);
                return;
            }

            string recipient = lbPMRecipients.Items[lbPMRecipients.SelectedIndex].ToString();
            CnCNetData.ConnectionBridge.SendChatMessage(recipient, -1, tbPMChatMessage.Text);
            lbPMChatMessages.Items.Add("[" + DateTime.Now.ToShortTimeString() + "] " + ProgramConstants.PLAYERNAME + ": " + tbPMChatMessage.Text);
            MessageColors.Add(lbPMChatMessages.ForeColor);

            lbPMChatMessages.SelectedIndex = lbPMChatMessages.Items.Count - 1;
            lbPMChatMessages.SelectedIndex = -1;

            int index = CnCNetData.PMInfos.FindIndex(c => c.UserName == recipient);

            if (index == -1)
            {
                PrivateMessageInfo pmInfo = new PrivateMessageInfo();
                pmInfo.UserName = recipient;
                pmInfo.Messages.Add(new MessageInfo(lbPMChatMessages.ForeColor, tbPMChatMessage.Text));
                pmInfo.IsSelfSent.Add(true);
                CnCNetData.PMInfos.Add(pmInfo);
            }
            else
            {
                CnCNetData.PMInfos[index].Messages.Add(new MessageInfo(lbPMChatMessages.ForeColor, tbPMChatMessage.Text));
                CnCNetData.PMInfos[index].IsSelfSent.Add(true);
            }

            tbPMChatMessage.Text = "";
        }

        private void Flash()
        {
            WindowFlasherCC.FlashWindowEx(this);
        }

        private void cmbPMRecipients_DrawItem(object sender, DrawItemEventArgs e)
        {
            LimitedComboBox comboBox = (LimitedComboBox)sender;
            e.DrawBackground();
            e.DrawFocusRectangle();
            if (e.Index > -1 && e.Index < comboBox.Items.Count)
            {
                if (comboBox.HoveredIndex != e.Index)
                    e.Graphics.DrawString(comboBox.Items[e.Index].ToString(), e.Font, new SolidBrush(comboBox.ForeColor), e.Bounds);
                else
                    e.Graphics.DrawString(comboBox.Items[e.Index].ToString(), e.Font, new SolidBrush(Color.White), e.Bounds);
            }
        }

        private void lbChatMessages_KeyDown(object sender, KeyEventArgs e)
        {
            if (lbPMChatMessages.SelectedIndex > -1)
            {
                if (e.KeyCode == Keys.C && e.Control)
                    Clipboard.SetText(lbPMChatMessages.SelectedItem.ToString());
            }
        }

        private void lbPMRecipients_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index > -1 && e.Index < lbPMRecipients.Items.Count)
            {
                if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
                    e = new DrawItemEventArgs(e.Graphics,
                                              e.Font,
                                              e.Bounds,
                                              e.Index,
                                              e.State ^ DrawItemState.Selected,
                                              e.ForeColor,
                                              cListBoxFocusColor);

                e.DrawBackground();
                e.DrawFocusRectangle();

                e.Graphics.DrawString(lbPMRecipients.Items[e.Index].ToString(), e.Font, new SolidBrush(UserColors[e.Index]), e.Bounds);
            }
        }
    }
}
