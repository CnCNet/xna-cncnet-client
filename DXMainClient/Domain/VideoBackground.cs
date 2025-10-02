using Microsoft.Xna.Framework.Graphics;
using LibVLCSharp.Shared;
using System;
using System.IO;

namespace DXMainClient.Domain
{
    public class VideoBackground : IDisposable
    {
        private static LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;
        private Media _media;
        private Texture2D _texture;
        private byte[] _videoBuffer;
        private object _lock = new object();

        public uint _videoWidth { get; private set; }
        public uint _videoHeight { get; private set; }

        public Texture2D Texture => _texture;

        /// <summary>
        /// Video playback for the main menu panel
        /// </summary>
        /// <param name="graphicsDevice">Graphic device resolution</param>
        /// <param name="videoPath">Video path location</param>
        public VideoBackground(GraphicsDevice graphicsDevice, string videoPath)
        {
            
            if (_libVLC == null)
                _libVLC = new LibVLC("--no-xlib", "--drop-late-frames", "--skip-frames"); 
            _mediaPlayer = new MediaPlayer(_libVLC);

            // video height and width can be edited here, can be improved by automatically read the video file resolution
            _videoWidth = 1360;
            _videoHeight = 720;


            _videoBuffer = new byte[_videoWidth * _videoHeight * 4];
            _texture = new Texture2D(graphicsDevice, (int)_videoWidth, (int)_videoHeight, false, SurfaceFormat.Color);

            _mediaPlayer.SetVideoCallbacks(Lock, Unlock, Display);
            _mediaPlayer.SetVideoFormat("RGBA", (uint)_videoWidth, (uint)_videoHeight, (uint)_videoWidth * 4); // do not use RV32, use RGBA instead, else it'll go full Allied mode

            _media = new Media(_libVLC, videoPath, FromType.FromPath);

            // add more option if you want to edit the video via VLC
            _media.AddOption(":scale=0.5"); // compress the video (a little help with the lower end devices.||_ ps. its a 50/50 in the lower end device)
            _media.AddOption(":no-audio"); // disables the audio
            _media.AddOption(":input-repeat=65535"); // this means looping the video 65.535 times, its stupid but it works

            // play the media
            _mediaPlayer.Play(_media);
        }

        /// <summary>
        /// Get current theme rom RA2MD.ini
        /// </summary>
        /// <param name="iniPath">RA2 YR ONLY, read the content of RA2MD.ini to get the theme</param>
        /// <returns></returns>
        public static string GetTheme(string iniPath)
        {
            String defaultTheme = "Default Theme";
            if (!File.Exists(iniPath))
                return defaultTheme; // fallback default

            var lines = File.ReadAllLines(iniPath);
            bool inMultiPlayer = false;

            foreach (var raw in lines)
            {
                var line = raw.Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";"))
                    continue;

                if (line.Equals("[MultiPlayer]", StringComparison.OrdinalIgnoreCase))
                {
                    inMultiPlayer = true;
                    continue;
                }

                if (inMultiPlayer && line.StartsWith("[") && line.EndsWith("]"))
                    break;

                if (inMultiPlayer && line.StartsWith("Theme=", StringComparison.OrdinalIgnoreCase))
                {
                    return line.Substring("Theme=".Length).Trim();
                }
            }

            return defaultTheme; // fallback if not found
        }

        private IntPtr Lock(IntPtr opaque, IntPtr planes)
        {
            System.Runtime.InteropServices.Marshal.WriteIntPtr(planes, System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(_videoBuffer, 0));
            return IntPtr.Zero;
        }

        private void Unlock(IntPtr opaque, IntPtr picture, IntPtr planes)
        {
        }

        private void Display(IntPtr opaque, IntPtr picture)
        {
            lock (_lock)
            {
                _texture.SetData(_videoBuffer);
            }
        }

        public void Dispose()
        {
            if (_mediaPlayer != null)
            {
                if (_mediaPlayer.IsPlaying)
                    _mediaPlayer.Stop();

                _mediaPlayer.Dispose();
                _mediaPlayer = null;
            }

            if (_media != null)
            {
                _media.Dispose();
                _media = null;
            }

            _texture?.Dispose();
            _texture = null;
        }

        // call this once on game exit ---- *BUGS!!! weird stuff can happens when exiting the client, and im not sure how and why
        public static void ShutdownLibVLC()
        {
            _libVLC?.Dispose();
            _libVLC = null;
        }
    }
}
