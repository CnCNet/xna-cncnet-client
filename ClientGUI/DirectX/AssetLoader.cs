using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ClientCore;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework;

namespace ClientGUI.DirectX
{
    /// <summary>
    /// A static class used for loading assets.
    /// </summary>
    public static class AssetLoader
    {
        static GraphicsDevice GraphicsDevice;

        public static void Initialize(GraphicsDevice gd)
        {
            GraphicsDevice = gd;
        }

        public static Texture2D LoadTexture(string name)
        {
            if (File.Exists(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + name))
            {
                using (FileStream fs = File.OpenRead(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + name))
                    return Texture2D.FromStream(GraphicsDevice, fs);
            }
            else if (File.Exists(ProgramConstants.gamepath + ProgramConstants.BASE_RESOURCE_PATH + name))
            {
                using (FileStream fs = File.OpenRead(ProgramConstants.gamepath + ProgramConstants.BASE_RESOURCE_PATH + name))
                    return Texture2D.FromStream(GraphicsDevice, fs);
            }
            else if (File.Exists(ProgramConstants.gamepath + name))
            {
                using (FileStream fs = File.OpenRead(ProgramConstants.gamepath + name))
                    return Texture2D.FromStream(GraphicsDevice, fs);
            }

            using (MemoryStream ms = new MemoryStream())
            {
                Properties.Resources.hotbutton.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return Texture2D.FromStream(GraphicsDevice, ms);
            }
        }

        /// <summary>
        /// Creates a one-colored texture.
        /// </summary>
        /// <param name="color">The color of the texture.</param>
        /// <param name="width">The width of the texture in pixels.</param>
        /// <param name="height">The height of the texture in pixels.</param>
        /// <returns>A texture.</returns>
        public static Texture2D CreateTexture(Color color, int width, int height)
        {
            Texture2D texture = new Texture2D(GraphicsDevice, width, height, false, SurfaceFormat.Color);

            Color[] colorArray = new Color[width * height];

            for (int i = 0; i < colorArray.Length; i++)
                colorArray[i] = color;

            texture.SetData(colorArray);

            return texture;
        }

        public static SoundEffect LoadSound(string name)
        {
            if (File.Exists(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + name))
            {
                using (FileStream fs = File.OpenRead(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + name))
                    return SoundEffect.FromStream(fs);
            }
            else if (File.Exists(ProgramConstants.gamepath + ProgramConstants.BASE_RESOURCE_PATH + name))
            {
                using (FileStream fs = File.OpenRead(ProgramConstants.gamepath + ProgramConstants.BASE_RESOURCE_PATH + name))
                    return SoundEffect.FromStream(fs);
            }
            else if (File.Exists(ProgramConstants.gamepath + name))
            {
                using (FileStream fs = File.OpenRead(ProgramConstants.gamepath + name))
                    return SoundEffect.FromStream(fs);
            }

            return null;
        }

        /// <summary>
        /// Creates a color based on a color string in the form "R,G,B". All values must be between 0 and 255.
        /// </summary>
        /// <param name="colorString">The color string in the form "R,G,B". All values must be between 0 and 255.</param>
        /// <returns>A XNA Color struct based on the given string.</returns>
        public static Color GetColorFromString(string colorString)
        {
            try
            {
                string[] colorArray = colorString.Split(',');
                Color color = new Color(Convert.ToByte(colorArray[0]), Convert.ToByte(colorArray[1]), Convert.ToByte(colorArray[2]));
                return color;
            }
            catch
            {
                throw new Exception("AssetLoader.GetColorFromString: Failed to convert " + colorString + " to a valid color!");
            }
        }
    }
}
