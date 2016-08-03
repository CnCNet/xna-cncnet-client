/// @author Rami "Rampastring" Pasanen
/// http://www.moddb.com/members/rampastring

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;
using ClientCore.CnCNet5;
using Rampastring.Tools;

namespace ClientCore
{
    /// <summary>
    /// Includes static methods useful for both Skirmish and CnCNet lobbies.
    /// </summary>
    public static class SharedLogic
    {
        /// <summary>
        /// Gets the font used by the client based on font information given in DTACnCNetClient.ini.
        /// </summary>
        /// <returns>The font.</returns>
        public static Font GetCommonFont()
        {
            return GetFont(DomainController.Instance().GetCommonFont());
        }

        /// <summary>
        /// Parses a font based on the font information given in a string.
        /// </summary>
        /// <param name="fontString">The string that contains info about the font. Example: "Microsoft Sans Serif,Regular,8.25"</param>
        /// <returns>The font.</returns>
        public static Font GetFont(string fontString)
        {
            string[] font = fontString.Split(',');

            string fontName = font[0];
            string style = font[1];
            float emSize = 8.25f;
            if (font.Length > 2)
                emSize = Convert.ToSingle(font[2], CultureInfo.GetCultureInfo("en-US"));

            FontStyle fontStyle = FontStyle.Regular;

            switch (style)
            {
                case "Bold":
                    fontStyle = FontStyle.Bold;
                    break;
                case "Italic":
                    fontStyle = FontStyle.Italic;
                    break;
                case "Strikeout":
                    fontStyle = FontStyle.Strikeout;
                    break;
                case "Underline":
                    fontStyle = FontStyle.Underline;
                    break;
                case "BoldAndItalic":
                    fontStyle = FontStyle.Bold | FontStyle.Italic;
                    break;
                default:
                    fontStyle = FontStyle.Regular;
                    break;
            }

            return new System.Drawing.Font(fontName, emSize, fontStyle);
        }
    }
}
