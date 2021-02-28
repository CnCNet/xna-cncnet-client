using Microsoft.Xna.Framework.Input;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientGUI
{
    /// <summary>
    /// A text box that stores entered messages and allows viewing them
    /// with the arrow keys.
    /// </summary>
    public class XNAChatTextBox : XNASuggestionTextBox
    {
        public XNAChatTextBox(WindowManager windowManager) : base(windowManager)
        {
            EnterPressed += XNAChatTextBox_EnterPressed;
        }

        private LinkedList<string> enteredMessages = new LinkedList<string>();
        private LinkedListNode<string> currentNode;

        private void XNAChatTextBox_EnterPressed(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Text))
                enteredMessages.AddFirst(Text);
        }

        protected override bool HandleKeyPress(Keys key)
        {
            if (key == Keys.Up)
            {
                if (currentNode == null)
                {
                    if (enteredMessages.First == null)
                        return true;

                    currentNode = enteredMessages.First;
                }
                else
                {
                    if (currentNode.Next != null)
                        currentNode = currentNode.Next;
                }

                Text = currentNode.Value;
            }
            else if (key == Keys.Down)
            {
                if (currentNode == null || currentNode.Previous == null)
                    return true;

                currentNode = currentNode.Previous;
                Text = currentNode.Value;
            }
            else
            {
                currentNode = null;

                return base.HandleKeyPress(key);
            }

            return true;
        }
    }
}
