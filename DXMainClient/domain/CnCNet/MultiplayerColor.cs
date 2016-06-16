using ClientCore;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.domain.CnCNet
{
    /// <summary>
    /// A color for the multiplayer game lobby.
    /// </summary>
    public class MultiplayerColor
    {
        public int GameColorIndex { get; set; }
        public string Name { get; set; }
        public Color XnaColor { get; set; }
        public bool Enabled { get; set; }

        /// <summary>
        /// Creates a new multiplayer color from data in a string array.
        /// </summary>
        /// <param name="name">The name of the color.</param>
        /// <param name="data">The input data. Needs to be in the format R,G,B,(game color index).</param>
        /// <returns>A new multiplayer color created from the given string array.</returns>
        public static MultiplayerColor CreateFromStringArray(string name, string[] data)
        {
            return new MultiplayerColor()
            {
                Name = name,
                XnaColor = new Color(Math.Min(255, Int32.Parse(data[0])),
                Math.Min(255, Int32.Parse(data[1])),
                Math.Min(255, Int32.Parse(data[2])), 255),
                GameColorIndex = Int32.Parse(data[3])
            };
        }

        public static List<MultiplayerColor> LoadColors()
        {
            IniFile gameOptionsIni = new IniFile(ProgramConstants.GetBaseResourcePath() + "GameOptions.ini");

            List<MultiplayerColor> mpColors = new List<MultiplayerColor>();

            List<string> colorKeys = gameOptionsIni.GetSectionKeys("MPColors");

            foreach (string key in colorKeys)
            {
                string[] values = gameOptionsIni.GetStringValue("MPColors", key, "255,255,255,0").Split(',');

                try
                {
                    MultiplayerColor mpColor = MultiplayerColor.CreateFromStringArray(key, values);

                    mpColors.Add(mpColor);
                }
                catch
                {
                    throw new Exception("Invalid MPColor specified in GameOptions.ini: " + key);
                }
            }

            return mpColors;
        }
    }
}
