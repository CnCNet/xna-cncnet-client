using System;
using System.Drawing;

namespace ClientCore.CnCNet5
{
    public class MessageInfo
    {
        public MessageInfo(Color _color, string _message)
        {
            color = _color;
            message = _message;
            time = DateTime.Now;
        }

        Color color;
        string message;
        DateTime time;

        public Color Color
        {
            get { return color; }
            set { color = value; }
        }

        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        public DateTime Time
        {
            get { return time; }
            set { time = value; }
        }
    }
}
