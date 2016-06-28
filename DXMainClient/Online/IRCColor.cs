using Microsoft.Xna.Framework;

namespace DTAClient.Online
{
    public class IRCColor
    {
        public IRCColor(string name, bool selectable, Color xnaColor, int ircColorId)
        {
            Name = name;
            Selectable = selectable;
            XnaColor = xnaColor;
            IrcColorId = ircColorId;
        }

        public string Name { get; private set; }
        public bool Selectable { get; private set; }
        public Color XnaColor { get; private set; }
        public int IrcColorId { get; private set; }
    }
}
