using System;

namespace ClientCore.Extensions
{
    public static class EnumExtensions
    {
        public static T Next<T>(this T src)
            where T : Enum
        {
            T[] values = GetValues(src);
            int nextIndex = Array.IndexOf(values, src) + 1;
            return values.Length == nextIndex ? values[0] : values[nextIndex];
        }

        private static T[] GetValues<T>(T src)
            where T : Enum
        {
            return (T[])Enum.GetValues(src.GetType());
        }
    }
}