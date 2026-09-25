#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;

using Rampastring.Tools;

using Point = Microsoft.Xna.Framework.Point;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// One resolved preview for one view. No textures or cache leases are owned
    /// here. Projection metadata stays paired with the selected image, never
    /// on Map, which is shared by lobby and game-information views.
    /// </summary>
    internal sealed class MapPreviewSource
    {
        public Map Map { get; }
        public string? ImmediateImagePath { get; }
        public bool IsGenerated { get; }
        private readonly double[]? transform;

        public MapPreviewSource(Map map, string? immediateImagePath, bool generated = false, double[]? projection = null)
        {
            Map = map;
            ImmediateImagePath = immediateImagePath;
            IsGenerated = generated;
            transform = projection == null ? null : (double[])projection.Clone();
        }

        public List<Point> GetStartingLocationPreviewCoords(Point size)
        {
            if (transform == null || !MainClientConstants.USE_ISOMETRIC_CELLS)
                return Map.CalculateStartingLocationPreviewCoords(size);

            var result = new List<Point>(Map.waypoints.Count);
            foreach (string waypoint in Map.waypoints)
            {
                string[] parts = waypoint.Split(',');
                int split = parts[0].Length - 3;
                int y = Convert.ToInt32(parts[0].Substring(0, split), CultureInfo.InvariantCulture);
                int x = Convert.ToInt32(parts[0].Substring(split), CultureInfo.InvariantCulture);
                int level = parts.Length > 1 ? Conversions.IntFromString(parts[1], 0) : 0;
                result.Add(MapPointToMapPreviewPoint(new Point(x, y), size, level));
            }
            return result;
        }

        public Point MapPointToMapPreviewPoint(Point point, Point size, int level)
        {
            if (transform == null || !MainClientConstants.USE_ISOMETRIC_CELLS)
                return Map.MapPointToMapPreviewPoint(point, size, level);

            var t = transform;
            if (level == 0)
                for (int i = 9; i + 2 < t.Length; i += 3)
                    if ((int)t[i] == point.X && (int)t[i + 1] == point.Y)
                    {
                        level = (int)t[i + 2];
                        break;
                    }
            int width = Conversions.IntFromString(Map.actualSize[2], 0);
            double px = (point.X - point.Y + width - 1) * 30.0;
            double py = (point.X + point.Y - width - 1 - level) * 15.0;
            return new Point((int)Math.Round(((px - t[0]) * t[4] + t[5]) * size.X / t[7]),
                (int)Math.Round(((py - t[1]) * t[4] + t[6]) * size.Y / t[8]));
        }
    }
}