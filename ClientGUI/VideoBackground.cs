#nullable enable
using System;
using System.Runtime.InteropServices;

using LibVLCSharp.Shared;

using Microsoft.Xna.Framework.Graphics;

namespace DXMainClient.Domain
{
    public class VideoBackground : IDisposable
    {
        private static LibVLC? _libVLC = null;

        private MediaPlayer? _mediaPlayer;
        private Media? _media;
        private Texture2D? _texture;
        private byte[] _videoBuffer;
        private readonly object _lock = new();

        public uint VideoWidth { get; private set; }
        public uint VideoHeight { get; private set; }

        public Texture2D Texture => _texture ?? throw new ObjectDisposedException(nameof(VideoBackground));

        /// <summary>
        /// Video playback for the main menu panel
        /// </summary>
        /// <param name="graphicsDevice">Graphic device resolution</param>
        /// <param name="videoPath">Video path location</param>
        public VideoBackground(GraphicsDevice graphicsDevice, string videoPath, int width, int height)
        {
            if (_libVLC == null)
                _libVLC = new LibVLC("--no-xlib", "--drop-late-frames", "--skip-frames");

            _mediaPlayer = new MediaPlayer(_libVLC);

            VideoWidth = (uint)width;
            VideoHeight = (uint)height;

            _videoBuffer = new byte[VideoWidth * VideoHeight * 4];
            _texture = new Texture2D(graphicsDevice, (int)VideoWidth, (int)VideoHeight, false, SurfaceFormat.Color);

            _mediaPlayer.SetVideoCallbacks(
                lockCb: (opaque, planes) =>
                {
                    Marshal.WriteIntPtr(planes, Marshal.UnsafeAddrOfPinnedArrayElement(_videoBuffer, 0));
                    return IntPtr.Zero;
                },
                unlockCb: (opaque, picture, planes) => { },
                displayCb: (opaque, picture) =>
                {
                    lock (_lock)
                    {
                        _texture.SetData(_videoBuffer);
                    }
                });

            _mediaPlayer.SetVideoFormat("RGBA", (uint)VideoWidth, (uint)VideoHeight, (uint)VideoWidth * 4); // do not use RV32, use RGBA instead, else the color will be messed up as if it were blue

            _media = new Media(_libVLC, videoPath, FromType.FromPath);

            // add more option if you want to edit the video via VLC
            _media.AddOption(":scale=0.5"); // compress the video (a little help with the lower end devices.||_ ps. its a 50/50 in the lower end device)
            _media.AddOption(":no-audio"); // disables the audio
            _media.AddOption(":input-repeat=65535"); // this means looping the video 65535 times, its stupid but it works

            // play the media
            _mediaPlayer.Play(_media);
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
