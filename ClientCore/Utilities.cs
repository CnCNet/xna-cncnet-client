using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Drawing;
using System.Globalization;

namespace ClientCore
{
    public static class Utilities
    {
        /// <summary>
        ///     Calculates and returns the MD5 hash value of a file.
        /// </summary>
        /// <returns>The MD5 hash.</returns>
        public static String calculateMD5ForFile(String path)
        {
            String returntext = null;
            try
            {
                FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();
                returntext = BytesToString(retVal);
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException || ex is DirectoryNotFoundException)
                    returntext = "File not found";
                else if (ex is UnauthorizedAccessException || ex is System.Security.SecurityException)
                    returntext = "Cannot access file";
            }
            return returntext;
        }

        public static String calculateMD5ForBytes(Byte[] buffer)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(buffer);
            return BytesToString(retVal);
        }

        private static String BytesToString(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("x2"));
            }
            return sb.ToString();

        }

        public static string CalculateSHA1ForFile(string path)
        {
            if (!File.Exists(path))
                return String.Empty;

            SHA1 sha1 = new SHA1CryptoServiceProvider();
            Stream stream = File.OpenRead(path);
            byte[] hash = sha1.ComputeHash(stream);
            return BytesToString(hash);
        }

        public static string CalculateSHA1ForString(string str)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] buffer = Encoding.ASCII.GetBytes(str);
            byte[] hash = sha1.ComputeHash(buffer);
            return BytesToString(hash);
        }

        /// <summary>
        /// Gets a color from a RGB color string (example: 255,255,255)
        /// </summary>
        /// <param name="colorString">The color string.</param>
        /// <returns>The color.</returns>
        public static Color GetColorFromString(string colorString)
        {
            string[] colorArray = colorString.Split(',');
            Color color = Color.FromArgb(Convert.ToByte(colorArray[0]), Convert.ToByte(colorArray[1]), Convert.ToByte(colorArray[2]));
            return color;
        }

        public static Image LoadImageFromFile(string path)
        {
            return Image.FromStream(new MemoryStream(File.ReadAllBytes(path)));
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
