using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DTAConfig
{
    /// <summary>
    /// A single screen resolution.
    /// </summary>
    public sealed record ScreenResolution : IComparable<ScreenResolution>
    {

        /// <summary>
        /// The width of the resolution in pixels.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// The height of the resolution in pixels.
        /// </summary>
        public int Height { get; }

        public ScreenResolution(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public ScreenResolution(Rectangle rectangle)
        {
            Width = rectangle.Width;
            Height = rectangle.Height;
        }

        public ScreenResolution(string resolution)
        {
            List<int> resolutionList = resolution.Trim().Split('x').Take(2).Select(int.Parse).ToList();
            Width = resolutionList[0];
            Height = resolutionList[1];
        }

        public static implicit operator ScreenResolution(string resolution) => new(resolution);

        public sealed override string ToString() => Width + "x" + Height;

        public static implicit operator string(ScreenResolution resolution) => resolution.ToString();

        public void Deconstruct(out int width, out int height)
        {
            width = this.Width;
            height = this.Height;
        }

        public static implicit operator ScreenResolution((int Width, int Height) resolutionTuple) => new(resolutionTuple.Width, resolutionTuple.Height);

        public static implicit operator (int Width, int Height)(ScreenResolution resolution) => new(resolution.Width, resolution.Height);

        public bool Fits(ScreenResolution child) => this.Width >= child.Width && this.Height >= child.Height;

        public int CompareTo(ScreenResolution other) => (this.Width, this.Height).CompareTo(other);

        // Accessing GraphicsAdapter.DefaultAdapter requiring DXMainClient.GameClass has been constructed. Lazy loading prevents possible null reference issues for now.
        private static ScreenResolution _desktopResolution = null;

        /// <summary>
        /// The resolution of primary monitor.
        /// </summary>
        public static ScreenResolution DesktopResolution =>
            _desktopResolution ??= new(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height);

        // The default graphic profile supports resolution up to 4096x4096. The number gets even smaller in practice. Therefore, we select 3840 as the limit.
        public static ScreenResolution HiDefLimitResolution { get; } = "3840x3840";

        private static ScreenResolution _safeMaximumResolution = null;

        /// <summary>
        /// The resolution of primary monitor, or the maximum resolution supported by the graphic profile, whichever is smaller.
        /// </summary>
        public static ScreenResolution SafeMaximumResolution
        {
            get
            {
#if XNA
                return _safeMaximumResolution ??= HiDefLimitResolution.Fits(DesktopResolution) ? DesktopResolution : HiDefLimitResolution;
#else
                return _safeMaximumResolution ??= DesktopResolution;
#endif
            }
        }

        private static ScreenResolution _safeFullScreenResolution = null;

        /// <summary>
        /// The maximum resolution supported by the graphic profile, or the largest full screen resolution supported by the primary monitor, whichever is smaller.
        /// </summary>
        public static ScreenResolution SafeFullScreenResolution => _safeFullScreenResolution ??= GetFullScreenResolutions(minWidth: 800, minHeight: 600).Max();

        public static SortedSet<ScreenResolution> GetFullScreenResolutions(int minWidth, int minHeight) =>
            GetFullScreenResolutions(minWidth, minHeight, SafeMaximumResolution.Width, SafeMaximumResolution.Height);
        public static SortedSet<ScreenResolution> GetFullScreenResolutions(int minWidth, int minHeight, int maxWidth, int maxHeight)
        {
            SortedSet<ScreenResolution> screenResolutions = [];

            foreach (DisplayMode dm in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
            {
                if (dm.Width < minWidth || dm.Height < minHeight || dm.Width > maxWidth || dm.Height > maxHeight)
                    continue;

                var resolution = new ScreenResolution(dm.Width, dm.Height);

                // SupportedDisplayModes can include the same resolution multiple times
                // because it takes the refresh rate into consideration.
                // Which will be filtered out by HashSet

                screenResolutions.Add(resolution);
            }

            return screenResolutions;
        }

        public static readonly IReadOnlyList<ScreenResolution> OptimalWindowedResolutions =
        [
            "1024x600",
            "1024x720",
            "1280x600",
            "1280x720",
            "1280x768",
            "1280x800",
        ];

        public const int MAX_INT_SCALE = 9;

        public SortedSet<ScreenResolution> GetIntegerScaledResolutions() =>
            GetIntegerScaledResolutions(SafeMaximumResolution);
        public SortedSet<ScreenResolution> GetIntegerScaledResolutions(ScreenResolution maxResolution)
        {
            SortedSet<ScreenResolution> resolutions = [];
            for (int i = 1; i <= MAX_INT_SCALE; i++)
            {
                ScreenResolution scaledResolution = (this.Width * i, this.Height * i);

                if (maxResolution.Fits(scaledResolution))
                    resolutions.Add(scaledResolution);
                else
                    break;
            }

            return resolutions;
        }

        public static SortedSet<ScreenResolution> GetWindowedResolutions(int minWidth, int minHeight) =>
            GetWindowedResolutions(minWidth, minHeight, SafeMaximumResolution.Width, SafeMaximumResolution.Height);
        public static SortedSet<ScreenResolution> GetWindowedResolutions(IEnumerable<ScreenResolution> optimalResolutions, int minWidth, int minHeight) =>
            GetWindowedResolutions(OptimalWindowedResolutions, minWidth, minHeight, SafeMaximumResolution.Width, SafeMaximumResolution.Height);
        public static SortedSet<ScreenResolution> GetWindowedResolutions(int minWidth, int minHeight, int maxWidth, int maxHeight) =>
            GetWindowedResolutions(OptimalWindowedResolutions, minWidth, minHeight, maxWidth, maxHeight);
        public static SortedSet<ScreenResolution> GetWindowedResolutions(IEnumerable<ScreenResolution> optimalResolutions, int minWidth, int minHeight, int maxWidth, int maxHeight)
        {
            ScreenResolution maxResolution = (maxWidth, maxHeight);

            SortedSet<ScreenResolution> windowedResolutions = [];

            foreach (ScreenResolution optimalResolution in optimalResolutions)
            {
                if (optimalResolution.Width < minWidth || optimalResolution.Height < minHeight)
                    continue;

                if (!maxResolution.Fits(optimalResolution))
                    continue;

                windowedResolutions.Add(optimalResolution);
            }

            return windowedResolutions;
        }
    }
}
