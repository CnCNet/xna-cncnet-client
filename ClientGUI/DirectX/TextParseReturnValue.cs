using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientGUI.DirectX
{
    public class TextParseReturnValue
    {
        public int lineAmount = 1;
        public string text;

        public static TextParseReturnValue FixText(SpriteFont spriteFont, int width, string text)
        {
            string line = String.Empty;
            TextParseReturnValue returnValue = new TextParseReturnValue();
            returnValue.text = String.Empty;
            string[] wordArray = text.Split(' ');

            foreach (string word in wordArray)
            {
                if (spriteFont.MeasureString(line + word).Length() > width)
                {
                    returnValue.text = returnValue.text + line + '\n';
                    returnValue.lineAmount = returnValue.lineAmount + 1;
                    line = String.Empty;
                }

                line = line + word + " ";
            }

            returnValue.text = returnValue.text + line;
            return returnValue;
        }

        public static List<string> GetFixedTextLines(SpriteFont spriteFont, int width, string text)
        {
            string line = String.Empty;
            List<string> returnValue = new List<string>();
            string[] wordArray = text.Split(' ');

            foreach (string word in wordArray)
            {
                if (spriteFont.MeasureString(line + word).Length() > width)
                {
                    returnValue.Add(line);
                    line = String.Empty;
                }

                line = line + word + " ";
            }

            if (!String.IsNullOrEmpty(line) && line.Length > 1)
                returnValue.Add(line);
            return returnValue;
        }
    }
}
